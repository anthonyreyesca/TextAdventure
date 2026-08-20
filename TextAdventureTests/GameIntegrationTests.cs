using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextAdventure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextAdventure.Tests
{
    [TestClass()]
    public class GameIntegrationTests
    {
        [TestMethod]
        public void Player_CanOpenExitDoor_AfterPickingUpKey()
        {
            // Arrange
            var world = GameSetup.CreateWorld();

            // ga naar kamer met sleutel (rechts) en neemt sleutel
            world.Move(Direction.e);
            var item = world.CurrentRoom.TakeItem("sleutel");
            world.Inventory.AddItem(item);

            // ga terug naar start en exit
            world.Move(Direction.w);
            world.Move(Direction.n);

            // Assert
            Assert.IsTrue(world.IsWon);
        }

        [TestMethod]
        public void Player_CannotEnterExitRoom_WithoutKey()
        {
            var world = GameSetup.CreateWorld();

            // probeer direct naar exit te gaan
            world.Move(Direction.n);

            // je blijft in start room
            Assert.AreEqual("Start", world.CurrentRoom.Name);
            Assert.IsFalse(world.IsWon);
        }

        [TestMethod]
        public void Player_CanDefeatMonster_WithSword()
        {
            var world = GameSetup.CreateWorld();

            // ga naar kelder rn nrrmt zwaart
            world.Move(Direction.s);
            var sword = world.CurrentRoom.TakeItem("zwaard");
            world.Inventory.AddItem(sword);

            // ga naar monsterkamer en vecht
            world.Move(Direction.s);
            world.Fight();

            Assert.IsFalse(world.CurrentRoom.MonsterAlive);
            Assert.IsFalse(world.IsGameOver);
        }

        [TestMethod]
        public void Player_Dies_WhenFightingMonster_WithoutSword()
        {
            var world = GameSetup.CreateWorld();

            world.Move(Direction.s);
            world.Move(Direction.s);

            world.Fight();

            Assert.IsTrue(world.IsGameOver);
        }
    }

}