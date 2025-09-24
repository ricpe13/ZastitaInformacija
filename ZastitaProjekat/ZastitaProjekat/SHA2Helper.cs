using System;
using System.Security.Cryptography;

public static class SHA2Helper
{
    public static byte[] ComputeSHA256(byte[] data)
    {
        using SHA256 sha = SHA256.Create();
        return sha.ComputeHash(data);
    }

    public static bool VerifySHA256(byte[] data, byte[] expectedHash)
    {
        byte[] actualHash = ComputeSHA256(data);
        if (actualHash.Length != expectedHash.Length)
            return false;

        for (int i = 0; i < actualHash.Length; i++)
        {
            if (actualHash[i] != expectedHash[i])
                return false;
        }

        return true;
    }
}
