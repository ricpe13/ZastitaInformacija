using System;

public class CTR
{
    private const int BLOCK_SIZE = 16;

    public static byte[] Process(byte[] data, byte[] key, byte[] nonce)
    {
        if (key == null || key.Length != 16)
            throw new ArgumentException("Ključ mora biti 16 bajtova.");
        if (nonce == null || nonce.Length != 8)
            throw new ArgumentException("Nonce mora biti 8 bajtova.");

        byte[] output = new byte[data.Length];
        int offset = 0;
        ulong counter = 0;

        while (offset < data.Length)
        {

            byte[] counterBlock = new byte[BLOCK_SIZE];
            Buffer.BlockCopy(nonce, 0, counterBlock, 0, 8);
            byte[] ctrBytes = BitConverter.GetBytes(counter);
            Buffer.BlockCopy(ctrBytes, 0, counterBlock, 8, 8);


            byte[] keystream = LEA.EncryptBlockRaw(counterBlock, key);

            int chunk = Math.Min(BLOCK_SIZE, data.Length - offset);
            for (int i = 0; i < chunk; i++)
                output[offset + i] = (byte)(data[offset + i] ^ keystream[i]);

            offset += chunk;
            counter++;
        }

        return output;
    }
}
