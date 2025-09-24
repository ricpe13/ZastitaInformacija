using System;
using System.IO;

public class LEA
{
    private const int ROUNDS = 24;
    private const int BLOCK_SIZE = 16;


    private static readonly uint[] DELTA = new uint[]
    {
        0xC3EFE9DB, 0x44626B02, 0x79E27C8A, 0x78DF30EC,
        0x715EA49E, 0xC785DA0A, 0xE04EF22A, 0xE5C40957
    };



    public static byte[] Encrypt(byte[] data, byte[] key)
    {
        if (key == null || key.Length != 16)
            throw new ArgumentException("LEA ključ mora biti tačno 16 bajtova (LEA-128).");

        byte[] padded = PadPkcs7(data, BLOCK_SIZE);
        var rk = ExpandRoundKeys128(key);

        using var ms = new MemoryStream(padded.Length);
        for (int off = 0; off < padded.Length; off += BLOCK_SIZE)
        {
            byte[] block = new byte[BLOCK_SIZE];
            Buffer.BlockCopy(padded, off, block, 0, BLOCK_SIZE);
            byte[] enc = EncryptBlockCore(block, rk);
            ms.Write(enc, 0, BLOCK_SIZE);
        }
        return ms.ToArray();
    }

    public static byte[] Decrypt(byte[] data, byte[] key)
    {
        if (key == null || key.Length != 16)
            throw new ArgumentException("LEA ključ mora biti tačno 16 bajtova (LEA-128).");
        if (data == null || data.Length == 0 || (data.Length % BLOCK_SIZE) != 0)
            throw new ArgumentException("Kodirani sadržaj nije validan (dužina nije višekratnik 16).");

        var rk = ExpandRoundKeys128(key);

        using var ms = new MemoryStream(data.Length);
        for (int off = 0; off < data.Length; off += BLOCK_SIZE)
        {
            byte[] block = new byte[BLOCK_SIZE];
            Buffer.BlockCopy(data, off, block, 0, BLOCK_SIZE);
            byte[] dec = DecryptBlockCore(block, rk);
            ms.Write(dec, 0, BLOCK_SIZE);
        }

        byte[] plain = ms.ToArray();
        return RemovePkcs7(plain, BLOCK_SIZE);
    }


    public static byte[] EncryptBlockRaw(byte[] block16, byte[] key)
    {
        if (block16 == null || block16.Length != BLOCK_SIZE)
            throw new ArgumentException("Blok mora biti tačno 16 bajtova.");
        if (key == null || key.Length != 16)
            throw new ArgumentException("LEA ključ mora biti 16 bajtova.");

        var rk = ExpandRoundKeys128(key);
        return EncryptBlockCore(block16, rk);
    }



    private static byte[] EncryptBlockCore(byte[] block, uint[] roundKeys)
    {
        unchecked
        {
            uint x0 = BitConverter.ToUInt32(block, 0);
            uint x1 = BitConverter.ToUInt32(block, 4);
            uint x2 = BitConverter.ToUInt32(block, 8);
            uint x3 = BitConverter.ToUInt32(block, 12);

            for (int r = 0; r < ROUNDS; r++)
            {
                int b = r * 6;
                uint oldX0 = x0;

                uint t0 = ROL((x0 ^ roundKeys[b + 0]) + (x1 ^ roundKeys[b + 1]), 9);
                uint t1 = ROR((x1 ^ roundKeys[b + 2]) + (x2 ^ roundKeys[b + 3]), 5);
                uint t2 = ROR((x2 ^ roundKeys[b + 4]) + (x3 ^ roundKeys[b + 5]), 3);

                x0 = t0;
                x1 = t1;
                x2 = t2;
                x3 = oldX0;
            }

            byte[] outBlock = new byte[BLOCK_SIZE];
            Buffer.BlockCopy(BitConverter.GetBytes(x0), 0, outBlock, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(x1), 0, outBlock, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(x2), 0, outBlock, 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(x3), 0, outBlock, 12, 4);
            return outBlock;
        }
    }

    private static byte[] DecryptBlockCore(byte[] block, uint[] roundKeys)
    {
        unchecked
        {
            uint x0 = BitConverter.ToUInt32(block, 0);
            uint x1 = BitConverter.ToUInt32(block, 4);
            uint x2 = BitConverter.ToUInt32(block, 8);
            uint x3 = BitConverter.ToUInt32(block, 12);

            for (int r = ROUNDS - 1; r >= 0; r--)
            {
                int b = r * 6;


                uint xi0 = x3;


                uint xi1 = ROR(x0, 9);
                xi1 = xi1 - (xi0 ^ roundKeys[b + 0]);
                xi1 ^= roundKeys[b + 1];


                uint xi2 = ROL(x1, 5);
                xi2 = xi2 - (xi1 ^ roundKeys[b + 2]);
                xi2 ^= roundKeys[b + 3];


                uint xi3 = ROL(x2, 3);
                xi3 = xi3 - (xi2 ^ roundKeys[b + 4]);
                xi3 ^= roundKeys[b + 5];

                x0 = xi0; x1 = xi1; x2 = xi2; x3 = xi3;
            }

            byte[] outBlock = new byte[BLOCK_SIZE];
            Buffer.BlockCopy(BitConverter.GetBytes(x0), 0, outBlock, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(x1), 0, outBlock, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(x2), 0, outBlock, 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(x3), 0, outBlock, 12, 4);
            return outBlock;
        }
    }



    private static uint[] ExpandRoundKeys128(byte[] key)
    {
        if (key.Length != 16)
            throw new ArgumentException("LEA-128 očekuje ključ od 16 bajtova.");

        unchecked
        {
            uint[] T = new uint[4];
            for (int i = 0; i < 4; i++)
                T[i] = BitConverter.ToUInt32(key, i * 4);

            uint[] rk = new uint[ROUNDS * 6];

            for (int i = 0; i < ROUNDS; i++)
            {
                uint d = DELTA[i & 3];
                uint d0 = ROL(d, i);
                uint d1 = ROL(d, i + 1);
                uint d2 = ROL(d, i + 2);
                uint d3 = ROL(d, i + 3);

                T[0] = ROL(T[0] + d0, 1);
                T[1] = ROL(T[1] + d1, 3);
                T[2] = ROL(T[2] + d2, 6);
                T[3] = ROL(T[3] + d3, 11);

                int b = i * 6;
                rk[b + 0] = T[0];
                rk[b + 1] = T[1];
                rk[b + 2] = T[2];
                rk[b + 3] = T[1];
                rk[b + 4] = T[3];
                rk[b + 5] = T[1];
            }

            return rk;
        }
    }


    private static uint ROL(uint x, int n) => (x << (n & 31)) | (x >> (32 - (n & 31)));
    private static uint ROR(uint x, int n) => (x >> (n & 31)) | (x << (32 - (n & 31)));

    private static byte[] PadPkcs7(byte[] data, int blockSize)
    {
        int pad = blockSize - (data.Length % blockSize);
        if (pad == 0) pad = blockSize;

        byte[] res = new byte[data.Length + pad];
        Buffer.BlockCopy(data, 0, res, 0, data.Length);
        for (int i = data.Length; i < res.Length; i++) res[i] = (byte)pad;
        return res;
    }

    private static byte[] RemovePkcs7(byte[] data, int blockSize)
    {
        if (data.Length == 0 || (data.Length % blockSize) != 0)
            throw new InvalidDataException("Neispravan PKCS#7: dužina nije višekratnik bloka.");

        int pad = data[^1];
        if (pad < 1 || pad > blockSize)
            throw new InvalidDataException("Neispravan PKCS#7: vrednost paddinga van opsega.");

        for (int i = data.Length - pad; i < data.Length; i++)
            if (data[i] != pad)
                throw new InvalidDataException("Neispravan PKCS#7: nekonzistentan padding.");

        byte[] res = new byte[data.Length - pad];
        Buffer.BlockCopy(data, 0, res, 0, res.Length);
        return res;
    }
}
