using System;
using System.Security.Cryptography;

public static class SHA2Helper
{
    public static byte[] ComputeSHA256(byte[] data) //racuna sha2 hes nad prosledjenim bajtovima i vraca rezultat 32 bajta
    {
        using SHA256 sha = SHA256.Create(); //kreira sha2 objekat preko using (using obezbedjuje automatski oslovadjanje resursa
        return sha.ComputeHash(data); //racuna i vraca 32 bajtni hes ulaznih podataka
    }

    public static bool VerifySHA256(byte[] data, byte[] expectedHash) //ponovo racuna sha2 nad data i provarava da li je identican prosledjenom exectedHash
    {
        byte[] actualHash = ComputeSHA256(data); //racuna sha2 hes pozivom prethodne funkcije
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
