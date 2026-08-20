namespace TextAdventure;

// Implementeert:  IInventory  (zie IInventory.cs)

// Gebruikt door:
//   - Building.Inventory wordt gevuld via GameSetup (new Inventory())
//   - Program.cs: 'inventory' commando -> GetDisplayList()
//   - Program.cs: 'take' commando      -> AddItem(...)
//   - Building.Move()                  -> HasItem(RequiredItem)
//   - Building.Fight()                 -> HasItem("Zwaard")

public class Inventory : IInventory
{
    private readonly Dictionary<string, Item> _items = new();

    public void AddItem(Item item) => _items[item.Name.ToLower()] = item;

    public bool HasItem(string itemName) => _items.ContainsKey(itemName.ToLower());

    public string GetDisplayList() =>
        _items.Count > 0 ? string.Join(", ", _items.Values.Select(i => i.Name)) : "Niets";
}
