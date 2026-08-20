namespace TextAdventure;
// hier staat:
//   - in welke kamer de speler staat (CurrentRoom)
//   - wat de speler bij zich heeft (Inventory)
//   - of het spel voorbij of gewonnen is

// gameplay-logica:
//   - Move()
//   - Fight()
//   - ToggleNoclip(): admin-only cheat

// Gebruikt door:
//   - Program.cs (de game-loop roept Move/Fight/ToggleNoclip aan)
//   - GameSetup.CreateWorld()  -> bouwt en geeft de Building terug
//   - BuildingTests.cs en GameIntegrationTests.cs

public class Building
{
    public IRoom CurrentRoom { get; private set; }
    public IInventory Inventory { get; }

    // Cheat-vlag. Wordt aan/uit gezet via ToggleNoclip() en alleen door
    // admin-spelers gebruikt (zie Program.cs 'noclip' case).
    public bool NoclipActive { get; private set; }

    public Building(IRoom startRoom, IInventory inventory)
    {
        CurrentRoom = startRoom;
        Inventory = inventory;
    }

    public bool IsGameOver { get; private set; }
    public bool IsWon { get; private set; }


    public void Move(Direction dir)
    {
        // 1) Bestaat er een uitgang in die richting?
        if (!CurrentRoom.Exits.TryGetValue(dir, out var nextRoom))
        {
            Console.WriteLine("Je kan die kant niet op.");
            return;
        }

        // 2) Staat hier nog een levend monster?
        if (CurrentRoom.MonsterAlive)
        {
            Console.WriteLine("Je probeerde weg te rennen, maar het monster greep je!");
            IsGameOver = true;
            return;
        }

        // 3) Heeft de volgende kamer een sleutel-vereiste?
        //    Met noclip aan negeren we deze check (admin-cheat).
        if (nextRoom.RequiredItem != null && !Inventory.HasItem(nextRoom.RequiredItem) && !NoclipActive)
        {
            Console.WriteLine($"De deur zit op slot. Je hebt een {nextRoom.RequiredItem} nodig.");
            return;
        }

        // 4) Verplaatsing is OK -> nieuwe kamer wordt huidige kamer.
        CurrentRoom = nextRoom;
        if (CurrentRoom.IsDeadly) IsGameOver = true;
        if (CurrentRoom.IsWin) IsWon = true;

        // 5) Toon de beschrijving van de nieuwe kamer (of de simpele Description-tekst bij win/dood).
        if (!IsGameOver && !IsWon) CurrentRoom.ShowDescription(Inventory);
        else Console.WriteLine(CurrentRoom.Description);
    }

    public void Fight()
    {
        if (!CurrentRoom.MonsterAlive)
        {
            Console.WriteLine("Er is hier niets om tegen te vechten.");
            return;
        }

        // Met zwaard -> monster dood. Zonder -> game over.
        if (Inventory.HasItem("Zwaard"))
        {
            CurrentRoom.MonsterAlive = false;
            Console.WriteLine("Met je zwaard versla je het monster! De weg is veilig.");
        }
        else
        {
            Console.WriteLine("Zonder wapen ben je geen partij voor het monster...");
            IsGameOver = true;
        }
    }

    // Met noclip aan negeert Move() de RequiredItem-check. 
    public void ToggleNoclip()
    {
        NoclipActive = !NoclipActive;
        Console.WriteLine(NoclipActive ? "Noclip ingeschakeld." : "Noclip uitgeschakeld.");
    }
}
