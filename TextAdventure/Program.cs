namespace TextAdventure;

// Entry point van de speler. Verzorgt:
//   1) Inlog/registratie via de API (ApiClient)
//   2) Game-loop: leest commando's en stuurt ze door naar de Building
//   3) Unlock-flow voor versleutelde kamers
//
// Roept aan:
//   - ApiClient.LoginAsync / RegisterAsync / GetKeyshareAsync
//   - GameSetup.CreateWorld()                  -> bouwt de wereld
//   - world.Move / Fight / ToggleNoclip        -> Building.cs
//   - DecryptionService.GenerateKey / TryDecrypt -> DecryptionService.cs

public class Program
{
    public static async Task Main()
    {
        var api = new ApiClient(AppConfig.ApiBaseUrl);
        string? jwtToken = null;


        /*Stap 1: Registreren of inloggen */
        Console.WriteLine("=== Secure Text Adventure ===");
        Console.WriteLine("1. Inloggen");
        Console.WriteLine("2. Registreren (nieuw account)");
        Console.Write("\nKies optie (1 of 2): ");

        string keuze = Console.ReadLine() ?? "";

        if (keuze == "2")
        {
            // TryRegister doet de registratie-flow; bij mislukken stoppen we.
            bool geregistreerd = await TryRegister(api);
            if (!geregistreerd)
            {
                Console.WriteLine("\nRegistratie mislukt. Start het programma opnieuw.");
                Console.WriteLine("Druk op een toets om af te sluiten...");
                Console.ReadKey();
                return;
            }
            Console.WriteLine("\nRegistratie gelukt! Je kunt nu inloggen.\n");
        }

        // Login-lus met max 3 pogingen 
        // De server heeft zelf óók een lockout-mechanisme na 3 mislukte pogingen (zie User.IsLockedOut). beetje overkill
        int pogingen = 0;
        while (jwtToken == null && pogingen < 3)
        {
            Console.Write("Gebruikersnaam: ");
            string username = Console.ReadLine() ?? "";

            Console.Write("Wachtwoord: ");
            string password = ReadPassword();    // verbergt input met *

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("Velden mogen niet leeg zijn.\n");
                pogingen++;
                continue;
            }

            jwtToken = await api.LoginAsync(username, password);

            if (jwtToken == null)
            {
                pogingen++;
                Console.WriteLine($"Login mislukt. ({pogingen}/3)\n");
            }
        }

        if (jwtToken == null)
        {
            Console.WriteLine("Te veel mislukte pogingen. Afsluiten.");
            return;
        }

        Console.WriteLine("Login geslaagd!\n");


        /*Stap 2: Rol bepalen uit het JWT-token */
        // Het token bevat claims (sub = username, role = Admin/Player). Server vult deze in GenerateJwtToken (TextAdventureAPI). We decoderen het token CLIENT-SIDE alleen om de UI aan te passen (admin-commando "noclip" tonen). De server blijft sowieso de echte authority — een client kan elke role in het JWT niet veranderen zonder de signing-key.
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(jwtToken);
        var role = jwt.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        bool isAdmin = role == "Admin";


        /* Stap 3: Spel starten */
        var world = GameSetup.CreateWorld();
        Console.WriteLine("Welkom bij de C# Text Adventure!");
        world.CurrentRoom.ShowDescription(world.Inventory);

        // Hoofdlus: lees commando, voer uit, herhaal
        while (!world.IsGameOver && !world.IsWon)
        {
            Console.Write("\n> ");
            var raw = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(raw)) continue;

            var input = raw.ToLower().Trim().Split(' ');
            string cmd = input[0];

            switch (cmd)
            {
                case "help":
                    Console.WriteLine("Commando's: help, look, inventory, go [n|e|s|w], take [item], fight, unlock, quit");
                    if (isAdmin)
                        Console.WriteLine("Admin commando's: noclip");
                    break;

                case "look":
                    // Toont kamernaam, beschrijving, items, uitgangen.
                    world.CurrentRoom.ShowDescription(world.Inventory);
                    break;

                case "inventory":
                    Console.WriteLine($"Je draagt: {world.Inventory.GetDisplayList()}");
                    break;

                case "go" when input.Length > 1:
                    // Parse de richting (n/e/s/w -> Direction-enum) en verplaats.
                    if (Enum.TryParse<Direction>(input[1], true, out var dir))
                        world.Move(dir);
                    else
                        Console.WriteLine("Ongeldige richting. Gebruik: n, e, s, w");
                    break;

                case "go":
                    Console.WriteLine("Welke richting? Gebruik: go n / go e / go s / go w");
                    break;

                case "take" when input.Length > 1:
                    // Probeer item op te pakken in huidige kamer.
                    var item = world.CurrentRoom.TakeItem(input[1]);
                    if (item != null) world.Inventory.AddItem(item);
                    Console.WriteLine(item != null ? $"Je pakt: {item.Name}" : "Dat ligt hier niet.");
                    break;

                case "take":
                    Console.WriteLine("Wat wil je pakken? Gebruik: take [item]");
                    break;

                case "fight":
                    world.Fight();
                    break;

                case "unlock":
                    // De zware flow: API call + AES-decryptie. Zie HandleUnlock().
                    await HandleUnlock(world.CurrentRoom, api, jwtToken);
                    break;

                case "noclip":
                    // Admin-only cheat. De server controleert dit niet, maar het token is ondertekend dus de role is betrouwbaar.
                    if (isAdmin)
                        world.ToggleNoclip();
                    else
                        Console.WriteLine("Je hebt geen toegang tot dit commando.");
                    break;

                case "quit":
                    return;

                default:
                    Console.WriteLine("Onbekend commando. Typ 'help'.");
                    break;
            }
        }

        Console.WriteLine(world.IsWon ? "\n--- GEWONNEN ---" : "\n--- GAME OVER ---");
    }


    // TryRegister: handelt de registratie-flow af.
    // Roept ApiClient.RegisterAsync() aan, die POST /api/auth/register stuurt.
    static async Task<bool> TryRegister(ApiClient api)
    {
        Console.WriteLine("\n=== NIEUW ACCOUNT AANMAKEN ===");
        Console.Write("Gebruikersnaam: ");
        string username = Console.ReadLine() ?? "";

        Console.Write("Wachtwoord: ");
        string password = ReadPassword();

        Console.Write("Rol (Player/Admin): ");
        string roleInput = Console.ReadLine() ?? "Player";

        // Validatie client side
        // De server doet óók een check op leeg / bestaand.
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("Fout: Gebruikersnaam en wachtwoord mogen niet leeg zijn.");
            return false;
        }

        if (username.Length < 3)
        {
            Console.WriteLine("Fout: Gebruikersnaam moet minimaal 3 tekens lang zijn.");
            return false;
        }

        if (password.Length < 4)
        {
            Console.WriteLine("Fout: Wachtwoord moet minimaal 4 tekens lang zijn.");
            return false;
        }

        // De server normaliseert de rol al naar "Admin" of "Player" (zie RegisterRequest handler in TextAdventureAPI/Program.cs). Deze client-side normalisatie is dus dubbel werk.
        string role = roleInput.ToLower() == "admin" ? "Admin" : "Player";

        bool success = await api.RegisterAsync(username, password, role);

        if (success)
        {
            Console.WriteLine($"✓ Account '{username}' succesvol aangemaakt als {role}!");
        }
        else
        {
            Console.WriteLine("✗ Registratie mislukt. Gebruikersnaam bestaat mogelijk al.");
        }

        return success;
    }


    // HandleUnlock: de "unlock"-flow voor versleutelde kamers.
    // 5 stappen:
    //   1) Keyshare ophalen via API (vereist login + Player-rol)
    //   2) Wachtwoord-bestand ontsleutelen met de keyshare (passphrase="")
    //   3) Speler typt wachtwoord in
    //   4) Vergelijken met het ontsleutelde wachtwoord
    //   5) Kamer-inhoud ontsleutelen en tonen
    
    static async Task HandleUnlock(IRoom currentRoom, ApiClient api, string jwtToken)
    {
        if (currentRoom is not Room room || !room.IsEncrypted)
        {
            Console.WriteLine("Deze kamer is niet vergrendeld.");
            return;
        }

        if (room.RoomId == null || room.PasswordFilePath == null || room.PasswordIV == null
            || room.EncFilePath == null || room.EncIV == null)
        {
            // Treedt op als de .iv-bestanden niet gekopieerd zijn naar bin/Debug/net8.0/. GameSetup laadt ze met File.Exists-check.
            Console.WriteLine("Fout: kamergegevens ontbreken.");
            return;
        }

        // Stap 1: sleutel ophalen via API (vereist login + juiste rol)
        Console.WriteLine("Sleutel ophalen via API...");
        string? keyshare = await api.GetKeyshareAsync(room.RoomId, jwtToken);

        if (keyshare == null)
        {
            Console.WriteLine("Geen toegang: controleer of je ingelogd bent met de juiste rol.");
            return;
        }

        // Stap 2: wachtwoord-bestand ontsleutelen met de API-sleutel
        // De key wordt berekend met passphrase="" — exact zoals EncryptRooms.EncryptPassword() het doet.
        byte[] passwordKey = DecryptionService.GenerateKey(keyshare, "");
        string? correctPassword = DecryptionService.TryDecrypt(room.PasswordFilePath, passwordKey, room.PasswordIV);

        if (correctPassword == null)
        {
            Console.WriteLine("Fout: wachtwoord-bestand kon niet worden gelezen.");
            return;
        }

        // Stap 3: speler geeft wachtwoord in
        Console.Write("Geef het wachtwoord voor deze kamer: ");
        string password = Console.ReadLine() ?? "";

        if (string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("Wachtwoord mag niet leeg zijn.");
            return;
        }

        // Stap 4: wachtwoord vergelijken
        if (password != correctPassword)
        {
            Console.WriteLine("Verkeerd wachtwoord. Toegang geweigerd.");
            return;
        }

        // Stap 5: kamerinhoud ontsleutelen en tonen
        byte[] contentKey = DecryptionService.GenerateKey(keyshare, password);
        string? inhoud = DecryptionService.TryDecrypt(room.EncFilePath, contentKey, room.EncIV);

        if (inhoud == null)
            Console.WriteLine("Decryptie mislukt. Beschadigd bestand.");
        else
        {
            Console.WriteLine("\n[ONTGRENDELD]");
            Console.WriteLine(inhoud);
        }
    }


    // ReadPassword: leest een wachtwoord met sterretjes-masking.

    // Console.IsInputRedirected = true bij tests of pipe-input. Dan kunnen we Console.ReadKey() niet gebruiken (die werkt enkel interactief),
    // dus fallback naar gewone ReadLin
    static string ReadPassword()
    {
        if (Console.IsInputRedirected)
            return Console.ReadLine() ?? "";

        var sb = new System.Text.StringBuilder();
        while (true)
        {
            // intercept: true -> toets wordt NIET zelf op het scherm gezet
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return sb.ToString();
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (sb.Length > 0)
                {
                    sb.Length--;
                    Console.Write("\b \b");   // cursor terug, spatie, cursor terug
                }
                continue;
            }
            // Skip control keys (pijltjes, F-toetsen, ...)
            if (char.IsControl(key.KeyChar)) continue;

            sb.Append(key.KeyChar);
            Console.Write('*');
        }
    }
}
