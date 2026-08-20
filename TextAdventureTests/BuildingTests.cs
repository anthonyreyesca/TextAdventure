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
    public class BuildingTests
    {
        private Mock<IRoom> _startRoom;
        private Mock<IRoom> _nextRoom;
        private Mock<IInventory> _inventory;
        private Building _building;

        [TestInitialize]
        public void Setup()
        {
            _startRoom = new Mock<IRoom>();
            _nextRoom = new Mock<IRoom>();
            _inventory = new Mock<IInventory>();

            // Default gedrag
            _startRoom.Setup(r => r.Exits)
                .Returns(new Dictionary<Direction, IRoom>());

            _startRoom.SetupProperty(r => r.MonsterAlive, false);

            _building = new Building(_startRoom.Object, _inventory.Object);
        }

        // zorgt voor minder dubbele code
        private void SetupExit(Direction dir, IRoom target)
        {
            _startRoom.Setup(r => r.Exits)
                .Returns(new Dictionary<Direction, IRoom>
                {
                { dir, target }
                });
        }

        // testen

        [TestMethod]
        public void Constructor_SetsInitialValues()
        {
            Assert.AreSame(_startRoom.Object, _building.CurrentRoom);
            Assert.AreSame(_inventory.Object, _building.Inventory);
            Assert.IsFalse(_building.IsGameOver);
            Assert.IsFalse(_building.IsWon);
        }

        [TestMethod]
        public void Move_ValidDirection_MovesToNextRoom()
        {
            SetupExit(Direction.n, _nextRoom.Object);
            _nextRoom.Setup(r => r.RequiredItem).Returns((string?)null);

            _building.Move(Direction.n);

            Assert.AreSame(_nextRoom.Object, _building.CurrentRoom);
            Assert.IsFalse(_building.IsGameOver);
        }

        [TestMethod]
        public void Move_InvalidDirection_StaysInSameRoom()
        {
            _building.Move(Direction.n);

            Assert.AreSame(_startRoom.Object, _building.CurrentRoom);
        }

        [TestMethod]
        public void Move_MonsterAlive_GameOver()
        {
            SetupExit(Direction.n, _nextRoom.Object);
            _startRoom.SetupProperty(r => r.MonsterAlive, true);

            _building.Move(Direction.n);

            Assert.IsTrue(_building.IsGameOver);
            Assert.AreSame(_startRoom.Object, _building.CurrentRoom);
        }

        [TestMethod]
        public void Move_HasRequiredItem_Moves()
        {
            SetupExit(Direction.n, _nextRoom.Object);

            _nextRoom.Setup(r => r.RequiredItem).Returns("Sleutel");
            _inventory.Setup(i => i.HasItem("Sleutel")).Returns(true);

            _building.Move(Direction.n);

            Assert.AreSame(_nextRoom.Object, _building.CurrentRoom);
        }

        [TestMethod]
        public void Move_MissingRequiredItem_Stays()
        {
            SetupExit(Direction.n, _nextRoom.Object);

            _nextRoom.Setup(r => r.RequiredItem).Returns("Sleutel");
            _inventory.Setup(i => i.HasItem("Sleutel")).Returns(false);

            _building.Move(Direction.n);

            Assert.AreSame(_startRoom.Object, _building.CurrentRoom);
        }

        [TestMethod]
        public void Move_DeadlyRoom_GameOver()
        {
            SetupExit(Direction.n, _nextRoom.Object);

            _nextRoom.Setup(r => r.RequiredItem).Returns((string?)null);
            _nextRoom.Setup(r => r.IsDeadly).Returns(true);

            _building.Move(Direction.n);

            Assert.IsTrue(_building.IsGameOver);
            Assert.AreSame(_nextRoom.Object, _building.CurrentRoom);
        }

        [TestMethod]
        public void Move_WinRoom_SetsWon()
        {
            SetupExit(Direction.n, _nextRoom.Object);

            _nextRoom.Setup(r => r.RequiredItem).Returns((string?)null);
            _nextRoom.Setup(r => r.IsWin).Returns(true);

            _building.Move(Direction.n);

            Assert.IsTrue(_building.IsWon);
            Assert.IsFalse(_building.IsGameOver);
        }

        [TestMethod]
        public void Fight_WithSword_KillsMonster()
        {
            _startRoom.SetupProperty(r => r.MonsterAlive, true);
            _inventory.Setup(i => i.HasItem("Zwaard")).Returns(true);

            _building.Fight();

            Assert.IsFalse(_startRoom.Object.MonsterAlive);
            Assert.IsFalse(_building.IsGameOver);
        }

        [TestMethod]
        public void Fight_NoSword_GameOver()
        {
            _startRoom.SetupProperty(r => r.MonsterAlive, true);
            _inventory.Setup(i => i.HasItem("Zwaard")).Returns(false);

            _building.Fight();

            Assert.IsTrue(_building.IsGameOver);
            Assert.IsTrue(_startRoom.Object.MonsterAlive);
        }

        [TestMethod]
        public void Fight_NoMonster_DoesNothing()
        {
            _startRoom.SetupProperty(r => r.MonsterAlive, false);

            var before = _building.IsGameOver;

            _building.Fight();

            Assert.AreEqual(before, _building.IsGameOver);
        }
    }
}