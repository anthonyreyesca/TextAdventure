namespace TextAdventure
{
    
    // Implementatie:
    //   - Inventory.cs  (de echte klasse)
    //
    // Gebruikt door:
    //   - Building.Inventory (property)            -> Building.cs
    //   - IRoom.ShowDescription(IInventory inv)    -> IRoom.cs / Room.cs
    //   - Mock<IInventory> in BuildingTests.cs

    public interface IInventory
    {
        bool HasItem(string itemName);

        void AddItem(Item item);

        string GetDisplayList();
    }
}
