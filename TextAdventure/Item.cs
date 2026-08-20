namespace TextAdventure;


// Gebruikt door:
//   - Room._items           (Dictionary<string, Item>)     -> Room.cs
//   - Inventory._items      (Dictionary<string, Item>)     -> Inventory.cs
//   - GameSetup.CreateWorld() maakt "Sleutel" en "Zwaard"  -> GameSetup.cs
public record Item(string Name, string Description);
