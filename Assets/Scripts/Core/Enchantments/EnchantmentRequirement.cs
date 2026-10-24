using System;
using UnityEngine;

namespace ShapeInventory.Core
{
    /// <summary>
    /// One thing a quest asks for: an enchantment type at a given tier.
    /// Tier 0 means "any tier of this type is fine". Serialized field names: type, tier.
    /// </summary>
    [Serializable]
    public struct EnchantmentRequirement
    {
        [SerializeField] private EnchantmentType type;
        [Tooltip("0 = any tier, otherwise 1..3 (the tier comes from the carrying item's cell count).")]
        [SerializeField, Range(0, EnchantmentTierRules.MaxTier)] private int tier;

        public EnchantmentRequirement(EnchantmentType type, int tier)
        {
            this.type = type;
            this.tier = tier;
        }

        public EnchantmentType Type => type;
        public int Tier => tier;
        public bool AnyTier => tier <= 0;

        public override string ToString() =>
            AnyTier ? type.ToString() : type + " " + EnchantmentTierRules.ToRoman(tier);
    }
}
