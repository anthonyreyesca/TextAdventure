namespace TextAdventure;

// zet spelwereld in elkaar en als één 'Building' teruggeeft. Hier worden alle kamers, items, uitgangen en de twee geheime (versleutelde) kamers gedefinieerd.

// Gebruikt door:
//   - Program.cs    -> var world = GameSetup.CreateWorld();
//   - GameSetupTests.cs en GameIntegrationTests.cs
//
// Plattegrond van de wereld:
//
//                        ┌─────────────┐
//                        │  De Uitgang │  (IsWin, vereist "Sleutel")
//                        └──────n──────┘
//                               │
//   ┌──────────────┐      ┌─────┴────┐      ┌──────────┐      ┌──────────────────┐
//   │ Dodelijke    │──e──>│  Start   │<─w───│ Schatkamer│──n──>│ Geheime Schatk. │
//   │ Gang (deadly)│      │          │      │ (Sleutel) │      │ (versleuteld)    │
//   └──────────────┘      └─────s────┘      └──────────┘      └──────────────────┘
//                               │                                       
//                         ┌─────┴────┐──e──>┌──────────────────┐
//                         │  Kelder  │      │ Geheime Kelder   │
//                         │ (Zwaard) │      │ (versleuteld)    │
//                         └─────s────┘      └──────────────────┘
//                               │
//                         ┌─────┴──────┐
//                         │ Monsterkamer│ (MonsterAlive)
//                         └─────────────┘

public static class GameSetup
{
    public static Building CreateWorld()
    {
        // Gewone kamers
        var start = new Room("Start", "Je staat in het midden van een stoffige kerker.");
        var links = new Room("Dodelijke Gang", "Zodra je de kamer binnenstapt, valt het plafond naar beneden!") { IsDeadly = true };
        var rechts = new Room("Schatkamer", "Een kleine kamer met een kist op de grond.");
        var boven = new Room("De Uitgang", "Gefeliciteerd! Je hebt de weg naar buiten gevonden.") { IsWin = true, RequiredItem = "Sleutel" };
        var beneden = new Room("Kelder", "Het is hier koud en vochtig.");
        var monsterKamer = new Room("Monsterkamer", "Een donker hol dat naar rottend vlees stinkt.") { MonsterAlive = true };

        // Versleutelde kamers
        // De .enc en .iv bestanden worden gegenereerd door EncryptRooms.cs
        // (uitvoeren als apart project) en daarna gekopieerd naar
        // bin/Debug/net8.0/ zodat ze op runtime gevonden worden.
        
        // RoomId moet exact overeenkomen met de keys in de dictionary 'keyshares' van TextAdventureAPI/Program.cs, anders kan de API geen keyshare teruggeven.
        var geheimeKelder = new Room("Geheime Kelder", "???")
        {
            IsEncrypted = true,
            RoomId = "kelder_geheim",
            EncFilePath = "kelder_geheim.enc",
            // IV inladen als het bestand bestaat, anders null.
            // Null -> HandleUnlock geeft "kamergegevens ontbreken".
            EncIV = File.Exists("kelder_geheim.iv")
                ? File.ReadAllBytes("kelder_geheim.iv")
                : null,

            PasswordFilePath = "kelder_geheim_password.enc",

            PasswordIV = File.Exists("kelder_geheim_password.iv")
                ? File.ReadAllBytes("kelder_geheim_password.iv")
                : null
        };
        var geheimeSchatkamer = new Room("Geheime Schatkamer", "???")
        {
            IsEncrypted = true,
            RoomId = "schatkamer_geheim",
            EncFilePath = "schatkamer_geheim.enc",
            EncIV = File.Exists("schatkamer_geheim.iv")
                            ? File.ReadAllBytes("schatkamer_geheim.iv")
                            : null,

            PasswordFilePath = "schatkamer_geheim_password.enc",

            PasswordIV = File.Exists("schatkamer_geheim_password.iv")
                    ? File.ReadAllBytes("schatkamer_geheim_password.iv")
                    : null
        };

        //Items in kamers leggen
        rechts.AddItem(new Item("Sleutel", "Een gouden sleutel."));
        beneden.AddItem(new Item("Zwaard", "Een vlijmscherp zwaard."));

        // Connecties leggen (Direction -> Room) 
        start.AddExit(Direction.w, links);
        start.AddExit(Direction.e, rechts);
        start.AddExit(Direction.n, boven);
        start.AddExit(Direction.s, beneden);
        beneden.AddExit(Direction.n, start);
        beneden.AddExit(Direction.s, monsterKamer);
        beneden.AddExit(Direction.e, geheimeKelder);       // toegang tot geheime kamer
        monsterKamer.AddExit(Direction.n, beneden);
        rechts.AddExit(Direction.w, start);
        rechts.AddExit(Direction.n, geheimeSchatkamer);    // toegang tot geheime kamer
        geheimeKelder.AddExit(Direction.w, beneden);
        geheimeSchatkamer.AddExit(Direction.s, rechts);

        // Klaar: Building met startkamer + lege inventory 
        return new Building(start, new Inventory());
    }
}
