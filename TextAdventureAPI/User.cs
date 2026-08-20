namespace TextAdventureAPI;

// Server-side model voor een geregistreerde gebruiker. Wordt in-memory
// bijgehouden in de static lijst 'users' in TextAdventureAPI/Program.cs.

// Gebruikt door:
//   - Program.cs  (List<User> users)
//   - Program.cs  /register, /login, /me, /keyshare handlers


// info:
//   - Wachtwoorden worden alleen SHA256-gehashed (geen salt, geen bcrypt
//     /argon2). 
//   - De users-lijst zit in-memory: bij elke restart van de API ben je
//     alle accounts kwijt. Geen database.
//   - IsLockedOut wordt nooit ontgrendeld — eens geblokkeerd, voor altijd
//     geblokkeerd (tot een API-restart). 

public class User
{
    public string Username { get; set; } = null!;

    // SHA256-hash van het wachtwoord (base64 string). Zie Program.HashPassword().
    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = "Player";

    // Tracker voor de lockout-logica in /login.
    public int FailedLoginAttempts { get; set; }
    public bool IsLockedOut { get; set; }
}
