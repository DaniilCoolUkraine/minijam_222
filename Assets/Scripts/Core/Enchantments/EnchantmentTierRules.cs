namespace ShapeInventory.Core
{
    /// <summary>
    /// The tier of an enchantment is derived from the SIZE of the item that carries it (cell count).
    /// Change the thresholds here to rebalance; nothing else hard-codes them.
    /// </summary>
    public static class EnchantmentTierRules
    {
        public const int MinTier = 1;
        public const int MaxTier = 3;

        /// <summary>1-2 cells = tier I, 3 cells = tier II, 4 or more cells = tier III.</summary>
        public static int GetTier(int cellCount)
        {
            if (cellCount <= 2) return 1;
            if (cellCount == 3) return 2;
            return 3;
        }

        /// <summary>"I", "II", "III" for 1..3. Any other value (including 0 = "any tier") returns an empty string.</summary>
        public static string ToRoman(int tier)
        {
            switch (tier)
            {
                case 1: return "I";
                case 2: return "II";
                case 3: return "III";
                default: return string.Empty;
            }
        }
    }
}
