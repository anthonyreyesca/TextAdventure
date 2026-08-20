namespace TextAdventure;

// Gebruikt door:
//   - GameSetup.CreateWorld()  -> maakt alle kamers en koppelt ze
//   - Building.CurrentRoom     -> "in welke kamer staat de speler"
//   - Program.HandleUnlock()   -> cast 'is Room room' om bij IsEncrypted /
//                                 RoomId / EncFilePath / PasswordFilePath /
//                                 EncIV / PasswordIV te kunnen
//   - RoomTests.cs en GameSetupTests.cs
public class Room : IRoom
{
    public string Name { get; }
    public string Description { get; }

    public bool IsDeadly { get; init; }    
    public string? RequiredItem { get; init; } 
    public bool IsWin { get; init; }       

    public bool MonsterAlive { get; set; }

    // Encryptie-uitbreiding (alleen op Room, niet op IRoom) 
    // Deze velden zijn nieuw t.o.v. de eenvoudige versie. Ze worden gebruikt door Program.HandleUnlock() om geheime kamers te ontsleutelen.
    public bool IsEncrypted { get; init; }      // Toont "[VERGRENDELD]" + blokkeert beschrijving
    public string? RoomId { get; init; }        // ID dat naar de API gestuurd wordt voor de keyshare
                                                // (moet matchen met TextAdventureAPI/Program.cs -> dictionary 'keyshares')
    public string? EncFilePath { get; init; }   // Pad naar het versleutelde verhaal-bestand (.enc)
    public byte[]? EncIV { get; init; }         // Initialization Vector voor AES (.iv-bestand, ingeladen door GameSetup)
    public string? PasswordFilePath { get; init; } // Pad naar het versleutelde wachtwoord-bestand
    public byte[]? PasswordIV { get; init; }    // IV voor het wachtwoord-bestand

    // Connecties met andere kamers 
    // Gevuld via AddExit() vanuit GameSetup.CreateWorld().
    public Dictionary<Direction, IRoom> Exits { get; } = new();

    // Items die in deze kamer liggen (private — buitenwereld ziet alleen via TakeItem/ShowDescription).
    private readonly Dictionary<string, Item> _items = new();

    public Room(string name, string description)
    {
        Name = name;
        Description = description;
    }

    // Setup-helpers (worden vanuit GameSetup gebruikt) 
    public void AddExit(Direction dir, IRoom room) => Exits[dir] = room;
    public void AddItem(Item item) => _items[item.Name.ToLower()] = item;

    public Item? TakeItem(string itemName)
    {
        if (_items.Remove(itemName.ToLower(), out var item)) return item;
        return null;
    }

    public void ShowDescription(IInventory inv)
    {
        Console.WriteLine($"\n--- {Name} ---");
        if (IsEncrypted)
        {
            // Belangrijk: de échte beschrijving wordt NIET getoond zolang de kamer vergrendeld is. De speler moet eerst 'unlock' doen (Program.HandleUnlock). De Description-property staat dan op "???" — zie GameSetup.cs.
            Console.WriteLine("[VERGRENDELD] Gebruik 'unlock' om deze kamer te openen.");
        }
        else
        {
            Console.WriteLine(Description);
            if (_items.Count > 0)
                Console.WriteLine($"Items hier: {string.Join(", ", _items.Values.Select(i => i.Name))}");
        }
        Console.WriteLine($"Uitgangen: {string.Join(", ", Exits.Keys)}");
        if (MonsterAlive) Console.WriteLine("PAS OP: Er staat een monster voor je!");
    }
}
