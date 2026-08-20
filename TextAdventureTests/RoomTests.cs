using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextAdventure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;

namespace TextAdventure.Tests
{
    [TestClass]
    public class RoomTests
    {
        private Room _room;
        private const string InitialName = "Testkamer";
        private const string InitialDesc = "Een stoffige testkamer.";

        [TestInitialize]
        public void Setup()
        {
            _room = new Room(InitialName, InitialDesc);
        }

        //

        [TestMethod]
        public void Constructor_SetsInitialValues()
        {
            Assert.AreEqual(InitialName, _room.Name);
            Assert.AreEqual(InitialDesc, _room.Description);
            Assert.IsFalse(_room.IsDeadly);
            Assert.IsFalse(_room.IsWin);
            Assert.IsFalse(_room.MonsterAlive);
            Assert.IsNull(_room.RequiredItem);
            Assert.IsNotNull(_room.Exits);
            Assert.AreEqual(0, _room.Exits.Count);
        }

        [TestMethod]
        public void AddExit_AddsRoomToExits()
        {
            var nextRoom = new Room("Andere Kamer", "Beschrijving");

            _room.AddExit(Direction.n, nextRoom);

            Assert.IsTrue(_room.Exits.ContainsKey(Direction.n));
            Assert.AreSame(nextRoom, _room.Exits[Direction.n]);
        }

        [TestMethod]
        public void TakeItem_ExistingItem_ReturnsItemAndRemovesIt()
        {
            var item = new Item("Sleutel", "Een gouden sleutel");
            _room.AddItem(item);

            var result = _room.TakeItem("Sleutel");

            Assert.IsNotNull(result);
            Assert.AreEqual("Sleutel", result.Name);

            // Check of het item echt weg is door het nogmaals te proberen
            var secondTry = _room.TakeItem("Sleutel");
            Assert.IsNull(secondTry);
        }

        [TestMethod]
        public void TakeItem_NonExistingItem_ReturnsNull()
        {
            var result = _room.TakeItem("Onbestaand");

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TakeItem_IsCaseInsensitive()
        {
            var item = new Item("Zwaard", "Scherp");
            _room.AddItem(item);

            var result = _room.TakeItem("ZWAARD");

            Assert.IsNotNull(result);
            Assert.AreEqual("Zwaard", result.Name);
        }

        [TestMethod]
        public void RoomProperties_CanBeSetViaInitializer()
        {
            var specialRoom = new Room("Gevaar", "Pas op!")
            {
                IsDeadly = true,
                IsWin = false,
                RequiredItem = "Schild"
            };

            Assert.IsTrue(specialRoom.IsDeadly);
            Assert.AreEqual("Schild", specialRoom.RequiredItem);
        }

        [TestMethod]
        public void MonsterAlive_CanBeChanged()
        {
            _room.MonsterAlive = true;
            Assert.IsTrue(_room.MonsterAlive);

            _room.MonsterAlive = false;
            Assert.IsFalse(_room.MonsterAlive);
        }

        [TestMethod]
        public void AddItem_ItemCanBeTakenAfterAdding()
        {
            var item = new Item("Sleutel", "Test");
            _room.AddItem(item);

            var result = _room.TakeItem("Sleutel");

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void AddExit_SameDirection_OverwritesPrevious()
        {
            var room1 = new Room("A", "A"); var room2 = new Room("B", "B"); _room.AddExit(Direction.n, room1); _room.AddExit(Direction.n, room2); Assert.AreSame(room2, _room.Exits[Direction.n]);
        }
    }
}