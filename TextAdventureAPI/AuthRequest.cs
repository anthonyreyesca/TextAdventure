namespace TextAdventureAPI;

// Gebruikt door:
//   - TextAdventureAPI/Program.cs  (in de Map-handlers)

public class RegisterRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;

    // Default = "Player". De server normaliseert verder naar "Admin" of "Player" — alles wat geen "Admin" is wordt "Player".
    public string Role { get; set; } = "Player";
}

public class LoginRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}
