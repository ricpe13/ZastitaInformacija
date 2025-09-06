using System;

public class CTR
{
    private const int BLOCK_SIZE = 16;

    public static byte[] Process(byte[] data, byte[] key, byte[] nonce) //i za sifrovanje i za desifrovanje jer je XOR sa istim keystream-om
    {
        if (key == null || key.Length != 16)
            throw new ArgumentException("Ključ mora biti 16 bajtova.");
        if (nonce == null || nonce.Length != 8)
            throw new ArgumentException("Nonce mora biti 8 bajtova.");

        byte[] output = new byte[data.Length]; //izlazni bafer da bude iste duzine kao ulazni
        int offset = 0; //pokazivac dokle smo stigli kroz data tj ulazne podatke
        ulong counter = 0; //pocetna vrednost brojaca

        while (offset < data.Length) //dok se ne obidju svi bajtovi ulaza
        {

            byte[] counterBlock = new byte[BLOCK_SIZE]; //blok od 16 bajtova, nonce plus counter
            Buffer.BlockCopy(nonce, 0, counterBlock, 0, 8); //upisuje nonce u prvih 8 bajtova bafera
            byte[] ctrBytes = BitConverter.GetBytes(counter); //pretvori counter u niz od 8 bajtova
            Buffer.BlockCopy(ctrBytes, 0, counterBlock, 8, 8); //upisuje counter u drugih 8 bajtova bafera iza nonce


            byte[] keystream = LEA.EncryptBlockRaw(counterBlock, key); //sifruje nonce counter LEA-om, dobija se 16B keystream

            int chunk = Math.Min(BLOCK_SIZE, data.Length - offset); //koliko bajtova obradjujemo u iteraciji
            for (int i = 0; i < chunk; i++)
                output[offset + i] = (byte)(data[offset + i] ^ keystream[i]); //XOR ulaznog bajta sa keystream bajtom

            offset += chunk; //pomera se pokazivac unapred za obradjeni deo
            counter++; //povecaj counter za sledeci blok
        }

        return output; //vrati rezultat (sifrovan ili desifrovan)
    }
}
