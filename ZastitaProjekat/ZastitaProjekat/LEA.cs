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

        byte[] padded = PadPkcs7(data, BLOCK_SIZE); //dodaje padding da duzina bude 16
        var rk = ExpandRoundKeys128(key); //iz glavnog kljuca generise podkljuceve (rundske kljuceve)

        using var ms = new MemoryStream(padded.Length); //memorija za rezultat
        for (int off = 0; off < padded.Length; off += BLOCK_SIZE) //prolazak kroz blokove
        {
            byte[] block = new byte[BLOCK_SIZE]; //privremeni bafer za jedan blok
            Buffer.BlockCopy(padded, off, block, 0, BLOCK_SIZE); //kopira 16 bajtova jedan blok iz ulaza
            byte[] enc = EncryptBlockCore(block, rk); //sifruje blok koriscenjem rundskih kljuceva
            ms.Write(enc, 0, BLOCK_SIZE); //upisuje sifrovani blok u izlazni tok
        }
        return ms.ToArray(); //vraca kompletno sifrovano kao niz bajtova
    }

    public static byte[] Decrypt(byte[] data, byte[] key)
    {
        if (key == null || key.Length != 16) //provera kljuca da li je 16B
            throw new ArgumentException("LEA ključ mora biti tačno 16 bajtova (LEA-128).");
        if (data == null || data.Length == 0 || (data.Length % BLOCK_SIZE) != 0) //proveraq duzine sifrovanog teksta
            throw new ArgumentException("Kodirani sadržaj nije validan (dužina nije višekratnik 16).");

        var rk = ExpandRoundKeys128(key); //generise podkljuceve iz glavnog kljuca

        using var ms = new MemoryStream(data.Length); //priprema izlaznog bafera
        for (int off = 0; off < data.Length; off += BLOCK_SIZE) //prolazak blok po blok
        {
            byte[] block = new byte[BLOCK_SIZE]; //privremeni blok
            Buffer.BlockCopy(data, off, block, 0, BLOCK_SIZE); //ucitava sifrovani blok
            byte[] dec = DecryptBlockCore(block, rk); //desifruje blok pomocu podkljuceva
            ms.Write(dec, 0, BLOCK_SIZE); //upis desifrovanog bloka
        }

        byte[] plain = ms.ToArray(); //pravi niz desifrovanih bajtova sa paddingom
        return RemovePkcs7(plain, BLOCK_SIZE); //skida padding i vraca cistu poruku
    }


    public static byte[] EncryptBlockRaw(byte[] block16, byte[] key) //za sifrovanje jednog bloka bez paddinga (ovo je za CTR)
    {
        if (block16 == null || block16.Length != BLOCK_SIZE)
            throw new ArgumentException("Blok mora biti tačno 16 bajtova.");
        if (key == null || key.Length != 16)
            throw new ArgumentException("LEA ključ mora biti 16 bajtova.");

        var rk = ExpandRoundKeys128(key); //priprema podkljuceva
        return EncryptBlockCore(block16, rk); //jezgro za sifrovanje jednnog bloka
    }



    private static byte[] EncryptBlockCore(byte[] block, uint[] roundKeys) //sifruje jedan 16B blok koristeci prosirene podkljuceve (koristi se 6 rundskih kljuceva)
    {
        unchecked //dozvoljava se prelivanje 32-bitnih sabiranja (LEA to trazi)
        {
            uint x0 = BitConverter.ToUInt32(block, 0); //ucitava prvu rec (32 bita) iz ulaznog bloka (little endian)
            uint x1 = BitConverter.ToUInt32(block, 4); //ucitava drugu rec iz bloka
            uint x2 = BitConverter.ToUInt32(block, 8); //ucitava trecu rec iz bloka
            uint x3 = BitConverter.ToUInt32(block, 12); //ucitava cetvrtu rec iz bloka

            for (int r = 0; r < ROUNDS; r++) //prolazak kroz 24 runde sifrovanja
            {
                int b = r * 6; //indeks u nizu rundskih kljuceva (po 6 po rundi)
                uint oldX0 = x0; //cuvam staro x0 jer treba za rotaciju registara

                uint t0 = ROL((x0 ^ roundKeys[b + 0]) + (x1 ^ roundKeys[b + 1]), 9); //prva grana - xor sa kljucem, sabiranje i rotacija ulevo 9 (nova vrednost za x0)
                uint t1 = ROR((x1 ^ roundKeys[b + 2]) + (x2 ^ roundKeys[b + 3]), 5); //druga grana - roratija udesno 5
                uint t2 = ROR((x2 ^ roundKeys[b + 4]) + (x3 ^ roundKeys[b + 5]), 3); //treca grana - rotacija udesno 3

                x0 = t0; //azuriranje registara - nov x0
                x1 = t1; //nov x1
                x2 = t2; //nov x2
                x3 = oldX0; //x3 postaje staro x0 (rotacija registara)
            }

            byte[] outBlock = new byte[BLOCK_SIZE]; //priprema izlazni blok
            Buffer.BlockCopy(BitConverter.GetBytes(x0), 0, outBlock, 0, 4); //upis x0
            Buffer.BlockCopy(BitConverter.GetBytes(x1), 0, outBlock, 4, 4); //upis x1
            Buffer.BlockCopy(BitConverter.GetBytes(x2), 0, outBlock, 8, 4); //upis x2
            Buffer.BlockCopy(BitConverter.GetBytes(x3), 0, outBlock, 12, 4); //upis x3
            return outBlock; //vraca sifrovani blok 16B
        }
    }

    private static byte[] DecryptBlockCore(byte[] block, uint[] roundKeys) //desifruje jedan 16B blok koristeci prosirene podkljuceve
    {
        unchecked
        {
            uint x0 = BitConverter.ToUInt32(block, 0); //ucitavanje reci
            uint x1 = BitConverter.ToUInt32(block, 4);
            uint x2 = BitConverter.ToUInt32(block, 8);
            uint x3 = BitConverter.ToUInt32(block, 12);

            for (int r = ROUNDS - 1; r >= 0; r--) //ide unazad, od poslednje do prve runde
            {
                int b = r * 6; //indeks runde


                uint xi0 = x3; //inverzna rotacija registara (x0 dolazi iz starog x3)


                uint xi1 = ROR(x0, 9); //da se odrotira suprotno od sifrovanja
                xi1 = xi1 - (xi0 ^ roundKeys[b + 0]); //oduzmi miks sa kljucem[b + 0] (suprotno od sabiranja)
                xi1 ^= roundKeys[b + 1]; //ponisti xor


                uint xi2 = ROL(x1, 5); //inverzna rotacija na drugu stranu
                xi2 = xi2 - (xi1 ^ roundKeys[b + 2]);
                xi2 ^= roundKeys[b + 3];


                uint xi3 = ROL(x2, 3);
                xi3 = xi3 - (xi2 ^ roundKeys[b + 4]);
                xi3 ^= roundKeys[b + 5];

                x0 = xi0; x1 = xi1; x2 = xi2; x3 = xi3;
            }

            byte[] outBlock = new byte[BLOCK_SIZE]; //priprema izlaza
            Buffer.BlockCopy(BitConverter.GetBytes(x0), 0, outBlock, 0, 4); //upis x0 desifrovan
            Buffer.BlockCopy(BitConverter.GetBytes(x1), 0, outBlock, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(x2), 0, outBlock, 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(x3), 0, outBlock, 12, 4);
            return outBlock; //vrati desifrovano
        }
    }



    private static uint[] ExpandRoundKeys128(byte[] key) //pravi rundske kljuceve iz 16B kljuca
    {
        if (key.Length != 16)
            throw new ArgumentException("LEA-128 očekuje ključ od 16 bajtova.");

        unchecked
        {
            uint[] T = new uint[4]; //interno stanje od 4 32-bitne reci (little endian)
            for (int i = 0; i < 4; i++)
                T[i] = BitConverter.ToUInt32(key, i * 4);

            uint[] rk = new uint[ROUNDS * 6]; //mesto za sve rundske kljuceve

            for (int i = 0; i < ROUNDS; i++)
            {
                uint d = DELTA[i & 3]; //bira jednu od 4 delta konstande ciklicno
                uint d0 = ROL(d, i);
                uint d1 = ROL(d, i + 1);
                uint d2 = ROL(d, i + 2);
                uint d3 = ROL(d, i + 3);

                T[0] = ROL(T[0] + d0, 1); //pravi varijante delte rotirane
                T[1] = ROL(T[1] + d1, 3);
                T[2] = ROL(T[2] + d2, 6);
                T[3] = ROL(T[3] + d3, 11);

                int b = i * 6;
                rk[b + 0] = T[0]; //upis 6 rundskih kljuceva za rundu i
                rk[b + 1] = T[1];
                rk[b + 2] = T[2];
                rk[b + 3] = T[1];
                rk[b + 4] = T[3];
                rk[b + 5] = T[1];
            }

            return rk;
        }
    }


    private static uint ROL(uint x, int n) => (x << (n & 31)) | (x >> (32 - (n & 31))); //rotacija ulevo (bitovi koji izadju levo vrate se desno)
    private static uint ROR(uint x, int n) => (x >> (n & 31)) | (x << (32 - (n & 31)));

    private static byte[] PadPkcs7(byte[] data, int blockSize) //dodaje padding
    {
        int pad = blockSize - (data.Length % blockSize); //koliko fali do punog bloka
        if (pad == 0) pad = blockSize; //ako je vec poravnato, dodaj ceo blok

        byte[] res = new byte[data.Length + pad]; //original plus padding
        Buffer.BlockCopy(data, 0, res, 0, data.Length);
        for (int i = data.Length; i < res.Length; i++) res[i] = (byte)pad; //pravi nov niz vrednosti
        return res;
    }

    private static byte[] RemovePkcs7(byte[] data, int blockSize) //skida padding
    {
        if (data.Length == 0 || (data.Length % blockSize) != 0)
            throw new InvalidDataException("Neispravan PKCS#7: dužina nije višekratnik bloka.");

        int pad = data[^1]; //poslednji bajt kaye koliko je paddinga
        if (pad < 1 || pad > blockSize)
            throw new InvalidDataException("Neispravan PKCS#7: vrednost paddinga van opsega.");

        for (int i = data.Length - pad; i < data.Length; i++)
            if (data[i] != pad)
                throw new InvalidDataException("Neispravan PKCS#7: nekonzistentan padding.");

        byte[] res = new byte[data.Length - pad]; //niy bey paddinga
        Buffer.BlockCopy(data, 0, res, 0, res.Length); //prekopira cisto
        return res; //vrati bey paddinga
    }
}
