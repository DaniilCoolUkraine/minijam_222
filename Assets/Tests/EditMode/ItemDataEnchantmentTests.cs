using System.Collections.Generic;
using NUnit.Framework;
using ShapeInventory.Core;
using UnityEngine;

namespace ShapeInventory.Tests
{
    public class ItemDataEnchantmentTests
    {
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created)
            {
                if (o != null) UnityEngine.Object.DestroyImmediate(o);
            }

            _created.Clear();
        }

        private ItemData Plain(params string[] rows)
        {
            var data = ItemData.Create("plain", "plain", Color.white, rows);
            _created.Add(data);
            return data;
        }

        private ItemData Enchanted(EnchantmentType type, params string[] rows)
        {
            var data = ItemData.Create("ench", "ench", Color.white, type, rows);
            _created.Add(data);
            return data;
        }

        [Test]
        public void OldCreateOverload_GivesNoEnchantment()
        {
            var data = Plain("##");

            Assert.AreEqual(EnchantmentType.None, data.Enchantment);
            Assert.IsFalse(data.HasEnchantment);
        }

        [Test]
        public void NewCreateOverload_WithNone_IsPlain()
        {
            var data = Enchanted(EnchantmentType.None, "##");

            Assert.AreEqual(EnchantmentType.None, data.Enchantment);
            Assert.IsFalse(data.HasEnchantment);
        }

        [Test]
        public void EnchantedItem_ReportsEnchantment()
        {
            var data = Enchanted(EnchantmentType.Sharpness, "##");

            Assert.AreEqual(EnchantmentType.Sharpness, data.Enchantment);
            Assert.IsTrue(data.HasEnchantment);
        }

        [Test]
        public void EnchantedCreate_KeepsBasicFieldsAndShape()
        {
            var data = ItemData.Create("blade", "Blade", Color.red, EnchantmentType.Haste, "#", "#", "#");

            _created.Add(data);

            Assert.AreEqual("blade", data.Id);
            Assert.AreEqual("Blade", data.DisplayName);
            Assert.AreEqual(Color.red, data.Color);
            Assert.AreEqual(3, data.Shape.CellCount);
            Assert.AreEqual(EnchantmentType.Haste, data.Enchantment);
        }

        [Test]
        public void Tier_OneCell_IsTierOne()
        {
            Assert.AreEqual(1, Enchanted(EnchantmentType.Luck, "#").Tier);
        }

        [Test]
        public void Tier_TwoCells_IsTierOne()
        {
            Assert.AreEqual(1, Enchanted(EnchantmentType.Luck, "##").Tier);
            Assert.AreEqual(1, Enchanted(EnchantmentType.Luck, "#", "#").Tier);
        }

        [Test]
        public void Tier_ThreeCells_IsTierTwo()
        {
            Assert.AreEqual(2, Enchanted(EnchantmentType.Luck, "###").Tier);
            Assert.AreEqual(2, Enchanted(EnchantmentType.Luck, "##", "#.").Tier);
        }

        [Test]
        public void Tier_FourOrMoreCells_IsTierThree()
        {
            Assert.AreEqual(3, Enchanted(EnchantmentType.Luck, "##", "##").Tier);
            Assert.AreEqual(3, Enchanted(EnchantmentType.Luck, "#####").Tier);
            Assert.AreEqual(3, Enchanted(EnchantmentType.Luck, "###", "###", "###").Tier);
        }

        [Test]
        public void Tier_EmptyCellsDoNotCount()
        {
            // 3 filled cells inside a 3x3 bounding box.
            var data = Enchanted(EnchantmentType.Durability, "#..", ".#.", "..#");

            Assert.AreEqual(3, data.Shape.CellCount);
            Assert.AreEqual(2, data.Tier);
        }

        [Test]
        public void Tier_MatchesTierRulesForCellCount()
        {
            var data = Enchanted(EnchantmentType.Protection, "####", "##..");

            Assert.AreEqual(EnchantmentTierRules.GetTier(data.Shape.CellCount), data.Tier);
        }

        [Test]
        public void PlainItem_StillHasSizeDerivedTier()
        {
            // Tier is only meaningful with an enchantment, but it must not throw for plain items.
            var data = Plain("##", "##");

            Assert.IsFalse(data.HasEnchantment);
            Assert.AreEqual(3, data.Tier);
        }
    }
}
