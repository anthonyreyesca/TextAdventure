using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace TextAdventureAPI;

// ASP.NET Core Minimal API. Stelt 4 endpoints beschikbaar:

//   POST /api/auth/register      (anoniem)   — nieuwe gebruiker
//   POST /api/auth/login         (anoniem)   — JWT-token terug
//   GET  /api/auth/me            (auth)      — wie ben ik? (niet gebruikt door client)
//   GET  /api/keys/keyshare/{id} (auth+role) — AES-keyshare voor een kamer


public class Program
{
    // In-memory user-store. Alle accounts verdwijnen bij API-restart.
    static List<User> users = new();

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // JWT-claim mapping uitschakelen:
        // .NET hernoemt standaard "sub" naar "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" en "role" naar "...claims/role". Door dit op false te zetten blijven de korte namen ("sub", "role") behouden — handig omdat de client (ApiClient + Program.cs) ze met die korte namen leest.
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        // JWT instellingen uit appsettings.json lezen
        // Deze waarden moeten op zowel issuer-zijde (bij token aanmaken) als validatie-zijde (bij token controleren) identiek zijn.
        var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
        var jwtAudience = builder.Configuration["Jwt:Audience"]!;
        var jwtKey = builder.Configuration["Jwt:Key"]!;

        // JWT authenticatie service registreren
        // Hiermee weet ASP.NET hoe een binnenkomend "Authorization: Bearer ..." token gevalideerd moet worden.
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,     // token vervalt na 2u (zie GenerateJwtToken)
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

                    // Welke claim is de "username"?  -> "sub"
                    // Welke claim is de "role"?      -> "role"
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = "role"
                };
            });

        builder.Services.AddAuthorization();
        builder.Services.AddEndpointsApiExplorer();

        // Swagger configuratie met JWT-support
        // Maakt het mogelijk om in de Swagger-UI op "Authorize" te klikken en daar je JWT te plakken — dan worden alle requests met Authorization-header verstuurd. Handig om manueel te testen.
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "Geef je JWT token in"
            });

            options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        // Authentication MOET vóór Authorization.
        // Authentication: "wie ben je?" (decodeert het token)
        // Authorization:  "mag je dit?" (checkt rollen/policies)
        app.UseAuthentication();
        app.UseAuthorization();

        // Keyshares per kamer 
        // Deze IDs moeten EXACT overeenkomen met Room.RoomId in TextAdventure/GameSetup.cs, anders krijgt de client een 404. De keyshares zelf zijn dezelfde strings als in EncryptRooms.cs anders kan de client niet decrypten.
        
        var keyshares = new Dictionary<string, string>
        {
            { "kelder_geheim",     "KS-ALPHA-7742" },
            { "schatkamer_geheim", "KS-BETA-3391"  }
        };

        // anonGroup = geen auth nodig (login/register)
        // authGroup/keysGroup = JWT vereist
        var anonGroup = app.MapGroup("/api/auth");
        var authGroup = app.MapGroup("/api/auth").RequireAuthorization();
        var keysGroup = app.MapGroup("/api/keys").RequireAuthorization();


        /*REGISTER */
        anonGroup.MapPost("/register", (RegisterRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest("Username en password zijn verplicht.");

            // Geen duplicate usernames.
            if (users.Any(u => u.Username == request.Username))
                return Results.Conflict("Username bestaat al.");

            var newUser = new User
            {
                Username = request.Username,
                PasswordHash = HashPassword(request.Password),
                // Normaliseer de rol: alleen "Admin" of "Player".
                Role = string.Equals(request.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Player"
            };

            users.Add(newUser);
            return Results.Ok($"Gebruiker '{request.Username}' geregistreerd als {newUser.Role}.");
        })
        .WithName("Register")
        .WithOpenApi();


        /* LOGIN */
        // Bij succes: { token: "<jwt>" }. Token leeft 2 uur.
        anonGroup.MapPost("/login", (LoginRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest("Username en password zijn verplicht.");

            var user = users.FirstOrDefault(u => u.Username == request.Username);
            if (user == null)
                return Results.Unauthorized();   // 401: geen username-bestaat-niet leak

            // Lockout-check: na 3 mislukte pogingen blijft het account dicht
            // tot de API herstart wordt. Geen unlock-mechanisme.
            if (user.IsLockedOut)
                return Results.Json(
                    new { message = "Account geblokkeerd na 3 mislukte pogingen!" },
                    statusCode: StatusCodes.Status403Forbidden);

            if (HashPassword(request.Password) != user.PasswordHash)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 3)
                    user.IsLockedOut = true;
                return Results.Unauthorized();
            }

            // Succesvol -> reset teller, genereer JWT.
            user.FailedLoginAttempts = 0;
            string token = GenerateJwtToken(user, jwtKey, jwtIssuer, jwtAudience);
            return Results.Ok(new { token });
        })
        .WithName("Login")
        .WithOpenApi();


        /* ────────── ME ────────── */
        // GET /api/auth/me
        // Geeft de identity terug die in het meegestuurde JWT zit.
        //
        // ⚠ OVERKILL: dit endpoint wordt momenteel NIET door de client
        // aangeroepen. ApiClient.cs heeft er geen methode voor. Het is
        // vermoedelijk overgebleven van eerder werk / testen via Swagger.
        authGroup.MapGet("/me", (HttpContext ctx) =>
        {
            var username = ctx.User.Identity?.Name;     // = "sub" claim
            var role = ctx.User.FindFirst("role")?.Value;
            return Results.Ok(new { username, role });
        })
        .WithName("Me")
        .WithOpenApi();


        /* KEYSHARE*/
        // Geeft de AES-keyshare voor een kamer aan een ingelogde Player.
        // Aangeroepen door ApiClient.GetKeyshareAsync().
        keysGroup.MapGet("/keyshare/{roomId}", (string roomId, HttpContext ctx) =>
        {
            // ⚠ NB: alleen Player, niet Admin, krijgt een keyshare. Dat is
            // omgekeerd aan wat je zou verwachten. Admin krijgt 'noclip'
            var role = ctx.User.FindFirst("role")?.Value;
            if (role != "Player")
                return Results.Forbid();

            if (string.IsNullOrWhiteSpace(roomId))
                return Results.BadRequest("RoomId is verplicht.");

            if (!keyshares.TryGetValue(roomId, out var keyshare))
                return Results.NotFound($"Geen keyshare voor kamer '{roomId}'.");

            return Results.Ok(new { roomId, keyshare });
        })
        .WithName("GetKeyshare")
        .WithOpenApi();

        app.Run();
    }


    // HashPassword — SHA256 + base64.
    static string HashPassword(string password)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }


    // GenerateJwtToken — maakt een ondertekend JWT met username + role.
    static string GenerateJwtToken(User user, string key, string issuer, string audience)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Korte claim-namen ("sub" / "role") matchen met:
        //   - JwtSecurityTokenHandler.DefaultMapInboundClaims = false (boven)
        //   - NameClaimType / RoleClaimType in TokenValidationParameters
        //   - de client die jwt.Claims.FirstOrDefault(c => c.Type == "role") doet
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim("role", user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),    // 2 uur geldig
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
