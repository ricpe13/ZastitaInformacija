using System;
using System.IO;

public class TEA
{
    private const uint Delta = 0x9E3779B9;
    private const int Rounds = 32;
    private const int BlockSize = 8;

    public static byte[] Encrypt(byte[] data, byte[] key)
    {
        if (key == null || key.Length != 16)
            throw new ArgumentException("Ključ mora biti tačno 16 bajtova!");


        byte[] padded = PadPkcs7(data, BlockSize);

        using MemoryStream ms = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(ms);

        for (int i = 0; i < padded.Length; i += BlockSize)
        {
            uint v0 = BitConverter.ToUInt32(padded, i);
            uint v1 = BitConverter.ToUInt32(padded, i + 4);

            uint k0 = BitConverter.ToUInt32(key, 0);
            uint k1 = BitConverter.ToUInt32(key, 4);
            uint k2 = BitConverter.ToUInt32(key, 8);
            uint k3 = BitConverter.ToUInt32(key, 12);

            uint sum = 0;
            for (int j = 0; j < Rounds; j++)
            {
                sum += Delta;
                v0 += ((v1 << 4) + k0) ^ (v1 + sum) ^ ((v1 >> 5) + k1);
                v1 += ((v0 << 4) + k2) ^ (v0 + sum) ^ ((v0 >> 5) + k3);
            }

            writer.Write(v0);
            writer.Write(v1);
        }

        return ms.ToArray();
    }

    public static byte[] Decrypt(byte[] data, byte[] key)
    {
        if (key == null || key.Length != 16)
            throw new ArgumentException("Ključ mora biti tačno 16 bajtova!");
        if (data == null || data.Length == 0 || (data.Length % BlockSize) != 0)
            throw new ArgumentException("Kodirani sadržaj nije validan (dužina nije višekratnik 8).");

        using MemoryStream ms = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(ms);

        for (int i = 0; i < data.Length; i += BlockSize)
        {
            uint v0 = BitConverter.ToUInt32(data, i);
            uint v1 = BitConverter.ToUInt32(data, i + 4);

            uint k0 = BitConverter.ToUInt32(key, 0);
            uint k1 = BitConverter.ToUInt32(key, 4);
            uint k2 = BitConverter.ToUInt32(key, 8);
            uint k3 = BitConverter.ToUInt32(key, 12);

            uint sum = unchecked(Delta * (uint)Rounds);
            for (int j = 0; j < Rounds; j++)
            {
                v1 -= ((v0 << 4) + k2) ^ (v0 + sum) ^ ((v0 >> 5) + k3);
                v0 -= ((v1 << 4) + k0) ^ (v1 + sum) ^ ((v1 >> 5) + k1);
                sum -= Delta;
            }

            writer.Write(v0);
            writer.Write(v1);
        }


        byte[] plaintext = ms.ToArray();
        return RemovePkcs7(plaintext, BlockSize);
    }



    private static byte[] PadPkcs7(byte[] data, int blockSize)
    {
        int padLen = blockSize - (data.Length % blockSize);
        if (padLen == 0) padLen = blockSize;

        byte[] result = new byte[data.Length + padLen];
        Buffer.BlockCopy(data, 0, result, 0, data.Length);
        for (int i = data.Length; i < result.Length; i++)
            result[i] = (byte)padLen;

        return result;
    }

    private static byte[] RemovePkcs7(byte[] data, int blockSize)
    {
        if (data.Length == 0 || (data.Length % blockSize) != 0)
            throw new InvalidDataException("Neispravan PKCS#7: dužina nije višekratnik bloka.");

        int padLen = data[^1];
        if (padLen < 1 || padLen > blockSize)
            throw new InvalidDataException("Neispravan PKCS#7: vrednost paddinga van opsega.");


        for (int i = data.Length - padLen; i < data.Length; i++)
        {
            if (data[i] != padLen)
                throw new InvalidDataException("Neispravan PKCS#7: nekonzistentan padding.");
        }

        byte[] result = new byte[data.Length - padLen];
        Buffer.BlockCopy(data, 0, result, 0, result.Length);
        return result;
    }
}
