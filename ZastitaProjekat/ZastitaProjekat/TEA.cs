using System;
using System.IO;

public class TEA
{
    private const uint Delta = 0x9E3779B9;
    private const int Rounds = 32;
    private const int BlockSize = 8;

    public static byte[] Encrypt(byte[] data, byte[] key)  //prima podatak i kljuc
    {
        if (key == null || key.Length != 16)  //provera da li je kljuc 16 bajtova odnosno 128 bitova
            throw new ArgumentException("Ključ mora biti tačno 16 bajtova!");


        byte[] padded = PadPkcs7(data, BlockSize); //dodaje se padding da duzina bloka bude 8 bajtova

        using MemoryStream ms = new MemoryStream(); //privremena memorija za rezultat
        using BinaryWriter writer = new BinaryWriter(ms); //olaksava upis uint vrednosti //koristi little endian

        for (int i = 0; i < padded.Length; i += BlockSize) //prolazi se blok po blok (po 8 bajtova)
        {
            uint v0 = BitConverter.ToUInt32(padded, i); //prva 4 bajta jedan deo  //BitConverter koristi little endian
            uint v1 = BitConverter.ToUInt32(padded, i + 4); //druga 4 bajta drugi deo

            uint k0 = BitConverter.ToUInt32(key, 0); //ovo je podela kljuca na 4 dela po 23 bita
            uint k1 = BitConverter.ToUInt32(key, 4); 
            uint k2 = BitConverter.ToUInt32(key, 8);
            uint k3 = BitConverter.ToUInt32(key, 12);

            uint sum = 0; //suma za TEA runde (kod sifrovanja se krece od 0)
            for (int j = 0; j < Rounds; j++) //32 runde mesanja (Rounds sam definisao skroz gore)
            {
                sum += Delta; //svaku rundu povecavam sumu za vrednost Delta (Delta definisana skroz gore)
                v0 += ((v1 << 4) + k0) ^ (v1 + sum) ^ ((v1 >> 5) + k1); //sada manipulisem prvi delom podataka, radi se levo i desno pomeranje, sabiranje sa delovima kljuca i sum i radi se XOR
                v1 += ((v0 << 4) + k2) ^ (v0 + sum) ^ ((v0 >> 5) + k3); //sada manipulisem drugim delom podataka, radi se levo i desno pomeranje, sabiranje sa delovima kljuca i sum i radi se XOR
            }

            writer.Write(v0); //upis prvog dela (4 bajta)
            writer.Write(v1); //upis drugog dela i onda sa prvim zajedno cini 8 bajtova sifrovano
        }

        return ms.ToArray(); //vraca sve sifrovane blokove kao u bajtove
    }

    public static byte[] Decrypt(byte[] data, byte[] key) //prima sifrovani podatak i kljuc
    {
        if (key == null || key.Length != 16) //provera kljuca
            throw new ArgumentException("Ključ mora biti tačno 16 bajtova!");
        if (data == null || data.Length == 0 || (data.Length % BlockSize) != 0) //provera blokova
            throw new ArgumentException("Kodirani sadržaj nije validan (dužina nije višekratnik 8).");

        using MemoryStream ms = new MemoryStream(); //memorija za rezultat
        using BinaryWriter writer = new BinaryWriter(ms); //upisuje u tok

        for (int i = 0; i < data.Length; i += BlockSize) //prolazak kroz sifrovane blokove
        {
            uint v0 = BitConverter.ToUInt32(data, i); //prva polovina
            uint v1 = BitConverter.ToUInt32(data, i + 4); //druga polovina

            uint k0 = BitConverter.ToUInt32(key, 0); //delovi kljuca
            uint k1 = BitConverter.ToUInt32(key, 4);
            uint k2 = BitConverter.ToUInt32(key, 8);
            uint k3 = BitConverter.ToUInt32(key, 12);

            uint sum = unchecked(Delta * (uint)Rounds); //inicijalna suma za desifrovanje, krece se od krajnje vrednosti iz sifrovanja
            for (int j = 0; j < Rounds; j++) //opet 32 runde, sad isto kao za enkripciju samo idemo unazad
            {
                v1 -= ((v0 << 4) + k2) ^ (v0 + sum) ^ ((v0 >> 5) + k3); //kontra od sifrovanja idemo za prvi deo
                v0 -= ((v1 << 4) + k0) ^ (v1 + sum) ^ ((v1 >> 5) + k1); //kontra od sifrovanja idemo za drugi deo
                sum -= Delta; //suma se smanjuje, suprotno od sifrovanja
            }

            writer.Write(v0); //upis prvih desifrovanih 4 bajta
            writer.Write(v1); //upis drugih desifrovanih 4 bajta, ukupno 8 bajtova
        }


        byte[] plaintext = ms.ToArray(); //uzmi sve desifrovane bajtove
        return RemovePkcs7(plaintext, BlockSize); //skini padding i vrati cistu poruku
    }



    private static byte[] PadPkcs7(byte[] data, int blockSize) //dodaje padding do punog bloka
    {
        int padLen = blockSize - (data.Length % blockSize); //koliko bajtova fali do punog bloka
        if (padLen == 0) padLen = blockSize; //ako je vec tacno dodaje se ceo blok

        byte[] result = new byte[data.Length + padLen]; //original plus padding
        Buffer.BlockCopy(data, 0, result, 0, data.Length); //kopiram originalne bajtove
        for (int i = data.Length; i < result.Length; i++) //prolazi se kroz padding
            result[i] = (byte)padLen; //popunjava svaki dodati bajt prosirenjem

        return result; //vracaju se popunjeni bajtovi
    }

    private static byte[] RemovePkcs7(byte[] data, int blockSize) //uklanjanje paddinga
    {
        if (data.Length == 0 || (data.Length % blockSize) != 0) //duzina mora biti >0 u deljiva sa velicinom bloka
            throw new InvalidDataException("Neispravan PKCS#7: dužina nije višekratnik bloka.");

        int padLen = data[^1];
        if (padLen < 1 || padLen > blockSize) //provera paddinga da li je u opsegu od 1 do blockSize
            throw new InvalidDataException("Neispravan PKCS#7: vrednost paddinga van opsega.");


        for (int i = data.Length - padLen; i < data.Length; i++) //provera da li poslednji padLen bajtva ima istu vrednost padLen
        {
            if (data[i] != padLen)
                throw new InvalidDataException("Neispravan PKCS#7: nekonzistentan padding.");
        }

        byte[] result = new byte[data.Length - padLen]; //rezultat bez paddinga
        Buffer.BlockCopy(data, 0, result, 0, result.Length); //kopiranje ciste poruke (bez paddinga)
        return result; //vraca poruku bez paddinga
    }
}
