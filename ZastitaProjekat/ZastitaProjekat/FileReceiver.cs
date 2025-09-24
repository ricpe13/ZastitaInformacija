using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.ConstrainedExecution;

public class FileReceiver
{
    private readonly int port;
    private readonly string saveFolder;

    private Action<string>? _logger;
    private CancellationTokenSource? _guiCts;
    private TcpListener? _listener;
    private bool _autoDecryptEnabled = false;

    public class ReceivedFileInfo
    {
        public string FileName { get; }
        public string SavedPath { get; }
        public string? DetectedAlgorithm { get; }
        public string DefaultDecryptedPath { get; }

        public ReceivedFileInfo(string fileName, string savedPath, string? detectedAlgorithm, string defaultDecryptedPath)
        {
            FileName = fileName;
            SavedPath = savedPath;
            DetectedAlgorithm = detectedAlgorithm;
            DefaultDecryptedPath = defaultDecryptedPath;
        }
    }

    public class DecryptParams
    {
        public string Algorithm { get; }
        public byte[] Key { get; }
        public byte[]? Nonce { get; }
        public string? OutputPath { get; }

        public DecryptParams(string algorithm, byte[] key, byte[]? nonce, string? outputPath = null)
        {
            Algorithm = algorithm;
            Key = key;
            Nonce = nonce;
            OutputPath = outputPath;
        }
    }

    private Func<ReceivedFileInfo, DecryptParams?>? _requestParams;

    public FileReceiver(int port, string saveFolder)
    {
        this.port = port;
        this.saveFolder = saveFolder;
    }

    public void SetLogger(Action<string> logger) => _logger = logger;

    public void ConfigureAutoDecrypt(bool enabled, Func<ReceivedFileInfo, DecryptParams?>? requestParams)
    {
        _autoDecryptEnabled = enabled;
        _requestParams = requestParams;
    }



    public void StartGui()
    {
        StopGuiSilent();

        _guiCts = new CancellationTokenSource();
        var token = _guiCts.Token;

        Task.Run(() => ListenLoopGui(token), token);
        Log($"[Receiver] Čekaju se fajlovi na portu {port}...");
    }

    public void StopGui()
    {
        bool wasRunning = _guiCts != null || _listener != null;
        StopGuiSilent();
        if (wasRunning) Log("[Receiver] Zaustavljeno.");
    }

    private void StopGuiSilent()
    {
        try { _guiCts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
        _guiCts = null;
        _listener = null;
    }

    private void ListenLoopGui(CancellationToken token)
    {
        try
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            while (!token.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = _listener.AcceptTcpClient();
                }
                catch
                {
                    if (token.IsCancellationRequested) break;
                    else continue;
                }

                using (client)
                {
                    try
                    {
                        using NetworkStream stream = client.GetStream();
                        HandleIncomingFileGui(stream);
                    }
                    catch (Exception ex)
                    {
                        Log("[Receiver] Greška: " + ex.Message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log("[Receiver] Greška listenera: " + ex.Message);
        }
        finally
        {
            Log("[Receiver] Prijem fajlova je zaustavljen.");
        }
    }



    private void HandleIncomingFileGui(NetworkStream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        string fileName = reader.ReadString();
        long fileSize = reader.ReadInt64();
        int hashLength = reader.ReadInt32();
        byte[] expectedHash = reader.ReadBytes(hashLength);

        if (fileSize < 0)
            throw new InvalidDataException("Negativna veličina fajla nije dozvoljena.");

        Log($"\n[Receiver] Primljen zaglavlje: {fileName} ({fileSize} bajta)");

        Directory.CreateDirectory(saveFolder);

        string tempPath = Path.Combine(saveFolder, "." + Guid.NewGuid().ToString("N") + ".part");

        const int BUF = 64 * 1024;
        byte[] buffer = new byte[BUF];
        long remaining = fileSize;

        using var sha = SHA256.Create();
        using var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, BUF, FileOptions.SequentialScan);

        while (remaining > 0)
        {
            int toRead = remaining > BUF ? BUF : (int)remaining;
            int read = stream.Read(buffer, 0, toRead);
            if (read <= 0) throw new EndOfStreamException("Neočekivan kraj toka pri prijemu sadržaja.");

            fs.Write(buffer, 0, read);
            sha.TransformBlock(buffer, 0, read, null, 0);
            remaining -= read;
        }

        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        byte[] actualHash = sha.Hash!;

        bool ok = ConstantTimeEquals(actualHash, expectedHash);
        if (!ok)
        {
            fs.Close();
            try { File.Delete(tempPath); } catch { }
            Log("[Receiver] Heš NE ODGOVARA. Fajl je oštećen ili izmenjen! Privremeni fajl obrisan.");
            return;
        }

        string finalPath = Path.Combine(saveFolder, fileName);
        if (File.Exists(finalPath))
        {
            string name = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            finalPath = Path.Combine(saveFolder, $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");
        }
        fs.Close();
        File.Move(tempPath, finalPath);

        Log("[Receiver] Heš odgovara. Fajl je ispravno prenet.");
        Log($"[Receiver] Kodirani fajl sačuvan na: {finalPath}");

        if (!_autoDecryptEnabled || _requestParams == null)
            return;

        string? algo = DetectAlgorithmFromExtension(fileName);
        if (algo != null)
            Log($"[Receiver] Detektovan algoritam iz ekstenzije: {algo}");
        else
            Log("[Receiver] Algoritam nije moguće odrediti iz ekstenzije — tražiće se ručno.");

        string outName = StripAlgoExtension(fileName);
        if (outName == fileName) outName = fileName + ".decrypted";
        string defaultOutPath = Path.Combine(saveFolder, outName);

        var info = new ReceivedFileInfo(fileName, finalPath, algo, defaultOutPath);
        var dp = _requestParams.Invoke(info);
        if (dp == null)
        {
            Log("[Receiver] Dešifrovanje je otkazano od strane korisnika.");
            return;
        }

        string useAlgo = dp.Algorithm ?? algo ?? "";
        if (useAlgo != "TEA" && useAlgo != "LEA" && useAlgo != "LEA-CTR")
        {
            Log("[Receiver] Nepoznat algoritam. Preskačem dešifrovanje.");
            return;
        }

        if (dp.Key == null || dp.Key.Length != 16)
        {
            Log("[Receiver] Ključ nije 16 bajtova. Preskačem dešifrovanje.");
            return;
        }
        if (useAlgo == "LEA-CTR")
        {
            if (dp.Nonce == null || dp.Nonce.Length != 8)
            {
                Log("[Receiver] Nonce nije 8 bajtova za LEA-CTR. Preskačem dešifrovanje.");
                return;
            }
        }

        try
        {
            byte[] encryptedData = File.ReadAllBytes(finalPath);
            byte[] decrypted = useAlgo switch
            {
                "TEA" => TEA.Decrypt(encryptedData, dp.Key),
                "LEA" => LEA.Decrypt(encryptedData, dp.Key),
                "LEA-CTR" => CTR.Process(encryptedData, dp.Key, dp.Nonce!),
                _ => throw new InvalidOperationException("Nepoznat algoritam.")
            };

            string outPath = string.IsNullOrWhiteSpace(dp.OutputPath) ? defaultOutPath : dp.OutputPath!;
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
            File.WriteAllBytes(outPath, decrypted);

            Log($"[Receiver] Dešifrovan fajl sačuvan na: {outPath}");
        }
        catch (Exception ex)
        {
            Log("[Receiver] Greška pri dešifrovanju: " + ex.Message);
        }
    }



    private static bool ConstantTimeEquals(byte[] a, byte[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        int diff = 0;
        for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }

    private static string? DetectAlgorithmFromExtension(string fileName)
    {
        string ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext switch
        {
            ".tea" => "TEA",
            ".lea" => "LEA",
            ".ctr" => "LEA-CTR",
            _ => null
        };
    }

    private static string StripAlgoExtension(string fileName)
    {
        string ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (ext == ".tea" || ext == ".lea" || ext == ".ctr")
            return fileName.Substring(0, fileName.Length - ext.Length);
        return fileName;
    }

    private void Log(string text)
    {
        try
        {
            _logger?.Invoke(text);

        }
        catch { }
    }
}
