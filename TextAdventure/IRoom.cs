using System.Collections.Generic;

namespace TextAdventure
{
    // Gebruikt door:
    //   - Building.CurrentRoom                     -> Building.cs
    //   - Building.Move() leest Exits/MonsterAlive/IsDeadly/IsWin/RequiredItem
    //   - Mock<IRoom> in BuildingTests.cs.
    public interface IRoom
    {
        string Name { get; }
        string Description { get; }

        // Mapping van richting -> volgende kamer. Wordt opgevuld via Room.AddExit() in GameSetup.CreateWorld().
        Dictionary<Direction, IRoom> Exits { get; }

        bool MonsterAlive { get; set; }

        bool IsDeadly { get; }
        bool IsWin { get; }

        string? RequiredItem { get; }

        Item? TakeItem(string itemName);

        void ShowDescription(IInventory inventory);
    }
}
