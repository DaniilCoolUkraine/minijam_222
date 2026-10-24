using NUnit.Framework;
using ShapeInventory.Core;

namespace ShapeInventory.Tests
{
    public class EnchantmentTierRulesTests
    {
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(4, 3)]
        [TestCase(5, 3)]
        [TestCase(9, 3)]
        public void GetTier_MapsCellCountToTier(int cells, int expectedTier)
        {
            Assert.AreEqual(expectedTier, EnchantmentTierRules.GetTier(cells));
        }

        [Test]
        public void GetTier_ResultIsWithinMinAndMaxTier()
        {
            for (int cells = 1; cells <= 20; cells++)
            {
                int tier = EnchantmentTierRules.GetTier(cells);
                Assert.GreaterOrEqual(tier, EnchantmentTierRules.MinTier);
                Assert.LessOrEqual(tier, EnchantmentTierRules.MaxTier);
            }
        }

        [Test]
        public void GetTier_IsNonDecreasingWithSize()
        {
            int previous = EnchantmentTierRules.GetTier(1);
            for (int cells = 2; cells <= 20; cells++)
            {
                int tier = EnchantmentTierRules.GetTier(cells);
                Assert.GreaterOrEqual(tier, previous);
                previous = tier;
            }
        }

        [Test]
        public void ToRoman_ValidTiers()
        {
            Assert.AreEqual("I", EnchantmentTierRules.ToRoman(1));
            Assert.AreEqual("II", EnchantmentTierRules.ToRoman(2));
            Assert.AreEqual("III", EnchantmentTierRules.ToRoman(3));
        }

        [Test]
        public void ToRoman_ZeroMeansAnyTierAndIsEmpty()
        {
            Assert.AreEqual(string.Empty, EnchantmentTierRules.ToRoman(0));
        }

        [Test]
        public void ToRoman_OutOfRangeIsEmpty()
        {
            Assert.AreEqual(string.Empty, EnchantmentTierRules.ToRoman(4));
            Assert.AreEqual(string.Empty, EnchantmentTierRules.ToRoman(-1));
        }
    }
}
