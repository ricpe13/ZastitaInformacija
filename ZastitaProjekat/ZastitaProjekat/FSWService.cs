using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using System.Threading.Tasks;

public class FSWService
{
    private readonly string targetFolder;
    private readonly string encryptedFolder;
    private readonly byte[] key;
    private readonly byte[] nonce;
    private readonly string algorithm;

    private FileSystemWatcher? watcher;

    private static readonly HashSet<string> _processing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static readonly object _gate = new object();


    private Action<string> _log = _ => { };
    private bool _guiRunning = false;

    public void SetLogger(Action<string> logger)
    {
        _log = logger ?? (_ => { });
    }

    private void Log(string msg) => _log?.Invoke(msg);

    public FSWService(string target, string encrypted, byte[] key, string algorithm, byte[]? nonce = null) //koji folder gleda, gde stavlja, kljuc i koji algoritam
    {
        this.targetFolder = target ?? throw new ArgumentNullException(nameof(target));
        this.encryptedFolder = encrypted ?? throw new ArgumentNullException(nameof(encrypted));
        this.key = key ?? throw new ArgumentNullException(nameof(key));
        this.algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
        this.nonce = nonce ?? Array.Empty<byte>();
    }



    public void StartForGui() //ukljucivanje
    {
        if (_guiRunning) return;
        SetupWatcher();
        watcher!.EnableRaisingEvents = true;
        _guiRunning = true;
        Log("[FSW] Pokrenuto nadgledanje (GUI): " + targetFolder);
    }

    public void StopForGui() //iskljucivanje
    {
        if (!_guiRunning) return;
        try
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                watcher = null;
            }
            Log("[FSW] Zaustavljeno (GUI).");
        }
        finally
        {
            _guiRunning = false;
        }
    }

    private void SetupWatcher() //prati samo nove fajlove
    {
        watcher = new FileSystemWatcher(targetFolder)
        {
            IncludeSubdirectories = false,
            Filter = "*.*",
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.Size
        };

        watcher.Created += OnFileCreated;
        watcher.Error += (s, e) =>
        {
            Log($"[FSW] Greška watchera: {e.GetException().Message}");
        };
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e) //reaguje na nov fajl, ignorise one privremene ekstenzije (tmp), pokrece glavnu obradu
    {
        string path = e.FullPath;

        string ext = Path.GetExtension(path)?.ToLowerInvariant() ?? "";
        if (ext == ".tmp" || ext == ".part" || ext == ".partial" || ext == ".crdownload")
            return;

        lock (_gate) //da ne krene u obradu dva puta
        {
            if (_processing.Contains(path))
                return;
            _processing.Add(path);
        }

        Task.Run(() => ProcessNewFileSafe(path));
    }

    private void ProcessNewFileSafe(string filePath)
    {
        try
        {
            if (!WaitForFileReady(filePath, maxWaitMs: 30000, settleMs: 400, stableReadsRequired: 2)) //ceka se da fajl legne na disk (da mu se ne menja velicina)
            {
                Log($"[FSW] Fajl nije stabilan ni posle timeout-a: {filePath}");
                return;
            }

            byte[] data = File.ReadAllBytes(filePath); //cita bajtove iz fajla

            byte[] encrypted;
            string outExt;

            switch (algorithm) //bira se algoritam 
            {
                case "TEA":
                    encrypted = TEA.Encrypt(data, key);
                    outExt = ".tea";
                    break;

                case "LEA":
                    encrypted = LEA.Encrypt(data, key);
                    outExt = ".lea";
                    break;

                case "CTR":
                case "LEA-CTR":
                    if (nonce == null || nonce.Length != 8)
                    {
                        Log("[FSW] Nonce nije validan (8 karaktera) za LEA-CTR. Fajl preskočen.");
                        return;
                    }
                    encrypted = CTR.Process(data, key, nonce);
                    outExt = ".ctr";
                    break;

                default:
                    Log("[FSW] Nepoznat algoritam! Fajl preskočen.");
                    return;
            }

            string fileName = Path.GetFileName(filePath);
            string outputPath = Path.Combine(encryptedFolder, fileName + outExt);

            Directory.CreateDirectory(encryptedFolder);

            File.WriteAllBytes(outputPath, encrypted);
            Log($"[FSW] Fajl '{fileName}' je šifrovan i sačuvan kao: {outputPath}");
        }
        catch (Exception ex)
        {
            Log("[FSW] Greška pri šifrovanju fajla: " + ex.Message);
        }
        finally
        {
            lock (_gate)
            {
                _processing.Remove(filePath);
            }
        }
    }

    private static bool WaitForFileReady(string path, int maxWaitMs, int settleMs, int stableReadsRequired) //max je duzina
    {//ceka da fajl legne celom duzinom na disk
        var sw = Stopwatch.StartNew();
        long lastLen = -1;
        int stableCount = 0;

        while (sw.ElapsedMilliseconds < maxWaitMs)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Thread.Sleep(settleMs);
                    continue;
                }

                FileInfo fi = new FileInfo(path);
                long len = fi.Length;

                if (len == lastLen)
                {
                    stableCount++;
                    if (stableCount >= stableReadsRequired)
                        return true;
                }
                else
                {
                    stableCount = 0;
                    lastLen = len;
                }
            }
            catch
            {

            }

            Thread.Sleep(settleMs);
        }

        return false;
    }
}
