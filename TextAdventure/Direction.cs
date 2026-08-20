namespace TextAdventure;
// Gebruikt door:
//   - Room.Exits           (Dictionary<Direction, IRoom>)  -> Room.cs
//   - Building.Move(dir)                                   -> Building.cs
//   - GameSetup.CreateWorld() (bij AddExit)                -> GameSetup.cs
//   - Program.cs (parsing van "go n/e/s/w" commando)

public enum Direction { n, e, s, w }
