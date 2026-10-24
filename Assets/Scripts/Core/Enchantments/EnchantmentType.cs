namespace ShapeInventory.Core
{
    /// <summary>
    /// Enchantment carried by an item. Values are serialized as ints in assets: ONLY append new values,
    /// never reorder or renumber existing ones.
    /// </summary>
    public enum EnchantmentType
    {
        /// <summary>Plain piece. Neutral filler: ignored by quest evaluation (never counts as right or wrong).</summary>
        None = 0,
        Durability = 1,
        Haste = 2,
        Sharpness = 3,
        Efficiency = 4,
        Luck = 5,
        Protection = 6
    }
}
