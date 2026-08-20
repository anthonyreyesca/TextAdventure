using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

// ============================================================================
// EncryptRooms.cs
// ----------------------------------------------------------------------------
// EENMALIG hulp-programma dat de versleutelde kamer-bestanden aanmaakt.
// Geen onderdeel van de game zelf — je voert dit los uit (bv. door dit
// tijdelijk als startup-project te zetten) en kopieert daarna de gegenereerde
// bestanden naar bin/Debug/net8.0/.
//
// Het gebruikt 'top-level statements' (geen Main-methode, geen class). De
// uitvoeringsvolgorde is letterlijk van boven naar beneden.
//
// Tegenhanger (decryptie):
//   - DecryptionService.cs  (zelfde key-formule, omgekeerde richting)
//   - Program.HandleUnlock()
//
// Gegenereerde bestanden:
//   kelder_geheim.enc         + kelder_geheim.iv
//   kelder_geheim_password.enc + kelder_geheim_password.iv
//   schatkamer_geheim.enc     + schatkamer_geheim.iv
//   schatkamer_geheim_password.enc + schatkamer_geheim_password.iv
//
// LET OP / overkill / risico:
//   - Wachtwoorden en keyshares staan letterlijk in deze broncode. Wie de
//     repo kan lezen, weet de "geheime" wachtwoorden. Voor een schoolproject
//     OK, maar in productie zou dit uit env vars of een secrets manager
//     moeten komen.
//   - EncryptPassword() gebruikt een lege passphrase ("") in de key-formule:
//     SHA256(keyshare + ":"). Dat is bewust zo: de API geeft enkel de
//     keyshare terug, en de client moet daarmee al het wachtwoord-bestand
//     kunnen ontsleutelen om het 'verwachte wachtwoord' te kennen.
//   - Of dit héle wachtwoord-bestand-mechanisme nodig is, is discutabel.
//     Het voegt een extra laag toe, maar uiteindelijk zit het 'geheim'
//     toch in de API + in de client-code. Zie ook Program.cs commentaar.
// ============================================================================

// ─── Kamer 1: keyshare moet matchen met TextAdventureAPI/Program.cs ────────
EncryptRoom(
    tekst: "In de geheime kelder vind je een oude kaart naar de eindbaas!",
    keyshare: "KS-ALPHA-7742",
    passphrase: "kerkergeheim",
    encFile: "kelder_geheim.enc",
    ivFile: "kelder_geheim.iv"
);

// ─── Kamer 2 ────────────────────────────────────────────────────────────────
EncryptRoom(
    tekst: "De geheime schatkamer bevat een diamanten zwaard. Je wint!",
    keyshare: "KS-BETA-3391",
    passphrase: "schatkamercode",
    encFile: "schatkamer_geheim.enc",
    ivFile: "schatkamer_geheim.iv"
);

// ─── Wachtwoord-bestanden ──────────────────────────────────────────────────
// Deze bestanden bevatten het verwachte wachtwoord (versleuteld met enkel
// de keyshare). De client haalt het op, ontsleutelt het, en vergelijkt met
// wat de speler intypt. Zie Program.HandleUnlock() voor de flow.
EncryptPassword(
    wachtwoord: "kerkergeheim",
    keyshare: "KS-ALPHA-7742",
    encFile: "kelder_geheim_password.enc",
    ivFile: "kelder_geheim_password.iv"
);

EncryptPassword(
    wachtwoord: "schatkamercode",
    keyshare: "KS-BETA-3391",
    encFile: "schatkamer_geheim_password.enc",
    ivFile: "schatkamer_geheim_password.iv"
);

Console.WriteLine("Klaar! Kopieer de .enc en .iv bestanden naar bin/Debug/net8.0/");


// ─── Helper: kamer-inhoud encrypten ────────────────────────────────────────
// Berekent dezelfde key als DecryptionService.GenerateKey():
//   key = SHA256(keyshare + ":" + passphrase)
// AES.Create() = AES-256-CBC met PKCS7 padding. De IV wordt automatisch
// random gegenereerd en apart opgeslagen (niet geheim — alleen onmisbaar
// om te kunnen decrypten).
static void EncryptRoom(string tekst, string keyshare, string passphrase, string encFile, string ivFile)
{
    byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes(keyshare + ":" + passphrase));

    using Aes aes = Aes.Create();
    aes.Key = key;
    aes.GenerateIV();

    File.WriteAllBytes(ivFile, aes.IV);

    byte[] plainBytes = Encoding.UTF8.GetBytes(tekst);
    using ICryptoTransform encryptor = aes.CreateEncryptor();
    byte[] encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
    File.WriteAllBytes(encFile, encrypted);

    Console.WriteLine($"Aangemaakt: {encFile} + {ivFile}");
}

// ─── Helper: wachtwoord-bestand encrypten ──────────────────────────────────
// Belangrijk verschil met EncryptRoom: de passphrase is LEEG ("").
// Reden: de client moet dit bestand kunnen ontsleutelen met ENKEL de
// keyshare (die hij van de API krijgt), nog vóór de speler het wachtwoord
// heeft ingegeven. Zie HandleUnlock stap 2.
static void EncryptPassword(string wachtwoord, string keyshare, string encFile, string ivFile)
{
    byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes(keyshare + ":"));

    using Aes aes = Aes.Create();
    aes.Key = key;
    aes.GenerateIV();

    File.WriteAllBytes(ivFile, aes.IV);

    byte[] plainBytes = Encoding.UTF8.GetBytes(wachtwoord);
    using ICryptoTransform encryptor = aes.CreateEncryptor();
    byte[] encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
    File.WriteAllBytes(encFile, encrypted);

    Console.WriteLine($"Aangemaakt: {encFile} + {ivFile}");
}
