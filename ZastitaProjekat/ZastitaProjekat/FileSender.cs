using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.ConstrainedExecution;
using System.Threading.Tasks;

public class FileSender
{
    private readonly string ip;
    private readonly int port;

    public FileSender(string ip, int port)
    {
        this.ip = ip;
        this.port = port;
    }


    public bool TrySend(string filePath, string algorithm, byte[]? key, byte[]? nonce, out string? error)
    {
        error = null;

        try
        {
            if (!File.Exists(filePath))
            {
                error = "[Sender] Fajl ne postoji!";
                return false;
            }

            string originalName = Path.GetFileName(filePath);
            string ext = Path.GetExtension(originalName)?.ToLowerInvariant();
            bool alreadyEncrypted = ext == ".tea" || ext == ".lea" || ext == ".ctr";

            byte[] payloadData;
            string fileNameToSend;

            if (alreadyEncrypted)
            {
                payloadData = File.ReadAllBytes(filePath);
                fileNameToSend = originalName;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(algorithm))
                {
                    error = "[Sender] Nije izabran algoritam za šifrovanje.";
                    return false;
                }

                if (key == null || key.Length != 16)
                {
                    error = "[Sender] Ključ (16 bajtova) je obavezan kada fajl nije već šifrovan.";
                    return false;
                }

                byte[] inputData = File.ReadAllBytes(filePath);
                byte[] encryptedData;

                switch (algorithm)
                {
                    case "TEA":
                        encryptedData = TEA.Encrypt(inputData, key);
                        fileNameToSend = originalName + ".tea";
                        break;

                    case "LEA":
                        encryptedData = LEA.Encrypt(inputData, key);
                        fileNameToSend = originalName + ".lea";
                        break;

                    case "LEA-CTR":
                        if (nonce == null || nonce.Length != 8)
                        {
                            error = "[Sender] Nonce (8 bajtova) je obavezan za LEA-CTR kada fajl nije šifrovan.";
                            return false;
                        }
                        encryptedData = CTR.Process(inputData, key, nonce);
                        fileNameToSend = originalName + ".ctr";
                        break;

                    default:
                        error = "[Sender] Nepoznat algoritam!";
                        return false;
                }

                payloadData = encryptedData;
            }


            byte[] hash = SHA2Helper.ComputeSHA256(payloadData);

            using TcpClient client = new TcpClient();
            var connectTask = client.ConnectAsync(ip, port);
            if (!connectTask.Wait(TimeSpan.FromSeconds(7)))
            {
                error = "[Sender] Nije moguće uspostaviti vezu (timeout). Proveri IP/port i da li je prijem pokrenut.";
                return false;
            }

            client.SendTimeout = 15000;
            client.ReceiveTimeout = 15000;

            using NetworkStream stream = client.GetStream();
            using BinaryWriter writer = new BinaryWriter(stream);


            writer.Write(fileNameToSend);
            writer.Write(payloadData.LongLength);
            writer.Write(hash.Length);
            writer.Write(hash);
            writer.Write(payloadData);
            writer.Flush();

            return true;
        }
        catch (Exception ex)
        {
            error = "[Sender] Greška pri slanju: " + ex.Message;
            return false;
        }
    }
}
