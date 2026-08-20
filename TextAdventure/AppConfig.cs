namespace TextAdventure;

// AppConfig.cs
// Op dit moment staat hier enkel de URL waar de TextAdventureAPI draait.

//   - Program.cs  -> new ApiClient(AppConfig.ApiBaseUrl)
public static class AppConfig
{
    public static string ApiBaseUrl = "https://localhost:7065";
}
