using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextAdventure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextAdventure.Tests
{
    [TestClass]
    public class InventoryTests
    {

        [TestMethod]
        public void HasItem_NaAddItem_ReturnsTrue()
        {
            var inventory = new Inventory();
            var zwaard = new Item("Zwaard", "Een scherp zwaard");

            inventory.AddItem(zwaard);
            Assert.IsTrue(inventory.HasItem("Zwaard"));
        }

        [TestMethod]
        public void HasItem_ItemNietToegevoegd_ReturnsFalse()
        {

            var inventory = new Inventory();

            Assert.IsFalse(inventory.HasItem("Sleutel"));
        }

        [TestMethod]
        public void HasItem_CaseInsensitive_ReturnsTrue()
        {
            var inventory = new Inventory();
            var schild = new Item("Schild", "Een houten schild");


            inventory.AddItem(schild);
            Assert.IsTrue(inventory.HasItem("SCHILD"));
        }



        [TestMethod]
        public void GetDisplayList_LeegInventory_ReturnsNiets()
        {

            var inventory = new Inventory();


            var result = inventory.GetDisplayList();
            Assert.AreEqual("Niets", result);
        }

        [TestMethod]
        public void GetDisplayList_EenItem_ReturnsItemNaam()
        {

            var inventory = new Inventory();
            inventory.AddItem(new Item("Sleutel", "Een roestige sleutel"));

            var result = inventory.GetDisplayList();
            Assert.AreEqual("Sleutel", result);
        }

        [TestMethod]
        public void GetDisplayList_MeerdereItems_BevatAlleNamen()
        {
            var inventory = new Inventory();
            inventory.AddItem(new Item("Zwaard", "Een zwaard"));
            inventory.AddItem(new Item("Schild", "Een schild"));

            var result = inventory.GetDisplayList();
            StringAssert.Contains(result, "Zwaard");
            StringAssert.Contains(result, "Schild");
        }


        [TestMethod]
        public void AddItem_ZelfdeItemTweeKeer_BlijftEenItem()
        {
            var inventory = new Inventory();
            var sleutel = new Item("Sleutel", "Een sleutel");
            inventory.AddItem(sleutel);
            inventory.AddItem(sleutel);

            var result = inventory.GetDisplayList();
            int count = result.Split(',').Length;
            Assert.AreEqual(1, count);
        }

    }
}