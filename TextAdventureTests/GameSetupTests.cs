using TextAdventure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextAdventure.Tests
{
    [TestClass]
    public class GameSetupTests
    {
        /*
       
        We testen de methode: 'public static Building CreateWorld()'

        De methode geeft een 'Building' terug met:

        •   een correcte 'startRoom' (ingesteld als 'CurrentRoom')
        •   een bestaande 'Inventory'
        •   alle 'Room'-objecten die correct met elkaar verbonden zijn via 'Exits'
        •   'Item'-objecten die in de juiste 'Room' geplaatst zijn


        Wat testen we bij 'GameSetupTest'?

        •   De 'Building' wordt correct geïnitialiseerd met de juiste 'startRoom'
        •   De 'Inventory' bestaat en is niet null
        •   De 'Exits' van elke 'Room' zijn correct gekoppeld aan andere 'Room'-objecten
        •   De 'Item'-objecten bevinden zich in de juiste 'Room'
        •  De speciale properties van 'Room' zijn correct ingesteld (IsDeadly, IsWin, MonsterAlive, RequiredItem)
        
        */

        [TestMethod]
        public void CreateWorld_ReturnsValidBuilding()
        {
            // we maken de volledige game wereld aan
            var world = GameSetup.CreateWorld();

            // check: bestaat de Building zelf?
            Assert.IsNotNull(world);
            // check: heeft de Building een startkamer?
            Assert.IsNotNull(world.CurrentRoom);
            // check: heeft de Building een inventory?
            Assert.IsNotNull(world.Inventory);
        }

        [TestMethod]
        public void StartRoom_ShouldBeNamedStart()
        {
            var world = GameSetup.CreateWorld();
            // pak de huidige room (start room)
            var startRoom = world.CurrentRoom;
            // check: heet de startkamer echt "Start"?
            Assert.AreEqual("Start", startRoom.Name);
        }

        [TestMethod]
        public void StartRoom_ShouldHaveAllExits()
        {
            var world = GameSetup.CreateWorld();
            var start = world.CurrentRoom;

            // check: bestaan alle richtingen als exit?
            Assert.IsTrue(start.Exits.ContainsKey(Direction.n));
            Assert.IsTrue(start.Exits.ContainsKey(Direction.e));
            Assert.IsTrue(start.Exits.ContainsKey(Direction.s));
            Assert.IsTrue(start.Exits.ContainsKey(Direction.w));
        }

        [TestMethod]
        public void LeftRoom_ShouldBeDeadly()
        {
            var world = GameSetup.CreateWorld();
            var start = world.CurrentRoom;

            // ga naar linker kamer via west exit
            var leftRoom = start.Exits[Direction.w];

            // check: is deze kamer dodelijk?
            Assert.IsTrue(leftRoom.IsDeadly);
        }

        [TestMethod]
        public void NorthRoom_ShouldBeWinRoomWithKeyRequirement()
        {
            var world = GameSetup.CreateWorld();
            var start = world.CurrentRoom;

            // ga naar noordelijke room (exit room)
            var exitRoom = start.Exits[Direction.n];

            // check: heb je sleutel nodig om hier te winnen?
            Assert.AreEqual("Sleutel", exitRoom.RequiredItem);

            // check: is dit de win room?
            Assert.IsTrue(exitRoom.IsWin);
        }

        [TestMethod]
        public void EastRoom_ShouldContainKey()
        {
            var world = GameSetup.CreateWorld();
            var start = world.CurrentRoom;
            var rightRoom = start.Exits[Direction.e];
            var item = rightRoom.TakeItem("sleutel");

            // check: bestaat het item?
            Assert.IsNotNull(item);

            // check: is het echt de sleutel?
            Assert.AreEqual("Sleutel", item.Name);
        }

        [TestMethod]
        public void MonsterRoom_ShouldHaveMonster()
        {
            var world = GameSetup.CreateWorld();
            var start = world.CurrentRoom;
            var beneden = start.Exits[Direction.s];

            // ga daarna nog verder naar de monster room
            var monsterRoom = beneden.Exits[Direction.s];

            // check: leeft het monster daar?
            Assert.IsTrue(monsterRoom.MonsterAlive);
        }

        [TestMethod]
        public void Basement_ShouldContainSword()
        {
            var world = GameSetup.CreateWorld();
            var start = world.CurrentRoom;

            var beneden = start.Exits[Direction.s];

            var item = beneden.TakeItem("zwaard");

            Assert.IsNotNull(item);
            Assert.AreEqual("Zwaard", item.Name);
        }

        [TestMethod]
        public void Basement_ShouldLinkBackToStart()
        {
            var world = GameSetup.CreateWorld();
            var start = world.CurrentRoom;

            var beneden = start.Exits[Direction.s];

            //ga je terug naa start room.?
            Assert.AreSame(start, beneden.Exits[Direction.n]);
        }
    }
}
