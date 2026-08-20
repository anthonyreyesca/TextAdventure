using System.Net.Http.Json;

namespace TextAdventure;

// Inloggen, registreren en keyshares op te halen.
//   - Program.cs              -> new ApiClient(AppConfig.ApiBaseUrl)
//   - Program.TryRegister()   -> RegisterAsync()
//   - Program.Main() login    -> LoginAsync()
//   - Program.HandleUnlock()  -> GetKeyshareAsync()
//
// Tegenpartij (server):
//   - TextAdventureAPI/Program.cs
//       POST /api/auth/register
//       POST /api/auth/login
//       GET  /api/keys/keyshare/{roomId}

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public ApiClient(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        _http = new HttpClient(handler);
    }

    // Login:
    // Verstuurt {username, password} naar POST /api/auth/login.
    // Bij succes: server stuurt {token: "..."} terug — een JWT.
    public async Task<string?> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(
                $"{_baseUrl}/api/auth/login",
                new { username, password }
            );

            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            return result?.Token;
        }
        catch
        {
            return null; // nooit crashen als API niet bereikbaar is
        }
    }

    // Register
    // Stuurt {username, password, role} naar POST /api/auth/register.
    // (Zie TextAdventureAPI/Program.cs - de RegisterRequest handler.)
    public async Task<bool> RegisterAsync(string username, string password, string role)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(
                $"{_baseUrl}/api/auth/register",
                new { username, password, role }
            );

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false; // nooit crashen als API niet bereikbaar is
        }
    }

    // Keyshare ophalen 
    // Haalt de AES-keyshare op voor een bepaalde kamer.
    // GET /api/keys/keyshare/{roomId}, met JWT in de Authorization header.
    // De server (TextAdventureAPI) check:
    //   1) JWT geldig?           (RequireAuthorization)
    //   2) Rol == "Player"?       (anders Results.Forbid)
    //   3) RoomId bestaat?       (in de dictionary 'keyshares')
    public async Task<string?> GetKeyshareAsync(string roomId, string jwtToken)
    {
        try
        {
            // Authorization-header zetten: "Bearer <token>"
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            var result = await _http.GetFromJsonAsync<KeyshareResponse>(
                $"{_baseUrl}/api/keys/keyshare/{roomId}"
            );

            return result?.Keyshare;
        }
        catch
        {
            return null;
        }
    }

    private class LoginResponse { public string? Token { get; set; } }
    private class KeyshareResponse { public string? Keyshare { get; set; } }
}
