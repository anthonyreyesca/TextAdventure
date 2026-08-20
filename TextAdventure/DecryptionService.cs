using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace TextAdventure;

// Helper-klasse voor het ontsleutelen van .enc-bestanden met AES.
// Wordt door de CLIENT gebruikt nadat de keyshare bij de API is opgehaald.

// Gebruikt door:
//   - Program.HandleUnlock()  -> roept GenerateKey() + TryDecrypt() aan

// Tegenhanger (encryptie):
//   - EncryptRooms.cs  -> daar worden de .enc en .iv bestanden gemaakt met EXACT dezelfde key-formule.


public static class DecryptionService
{
    // De AES-sleutel = SHA256(keyshare + ":" + passphrase). Deze formule MOET identiek zijn aan die in EncryptRooms.cs — anders mismatch en krijg je terug "Decryptie mislukt".
    
    public static byte[] GenerateKey(string keyshare, string passphrase)
    {
        string combined = keyshare + ":" + passphrase;
        return SHA256.HashData(Encoding.UTF8.GetBytes(combined));
    }

    // Decryptie 
    public static string? TryDecrypt(string encFilePath, byte[] key, byte[] iv)
    {
        try
        {
            if (!File.Exists(encFilePath))
                return null;

            byte[] encryptedBytes = File.ReadAllBytes(encFilePath);

            using Aes aes = Aes.Create();  
            aes.Key = key;
            aes.IV = iv;

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] decrypted = decryptor.TransformFinalBlock(
                encryptedBytes, 0, encryptedBytes.Length);

            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return null; // nooit crashen bij foutieve sleutel
        }
    }
}
