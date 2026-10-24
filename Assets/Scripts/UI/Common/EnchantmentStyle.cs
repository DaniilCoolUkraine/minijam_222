using ShapeInventory.Core;
using UnityEngine;

namespace ShapeInventory.UI.Common
{
    /// <summary>Display names, 3-letter codes and colours for enchantment types. Shared by UI and content generation.</summary>
    public static class EnchantmentStyle
    {
        public static string DisplayName(EnchantmentType type)
        {
            return type == EnchantmentType.None ? "Plain" : type.ToString();
        }

        /// <summary>3-letter badge code.</summary>
        public static string ShortName(EnchantmentType type)
        {
            switch (type)
            {
                case EnchantmentType.Durability: return "DUR";
                case EnchantmentType.Haste: return "HST";
                case EnchantmentType.Sharpness: return "SHP";
                case EnchantmentType.Efficiency: return "EFF";
                case EnchantmentType.Luck: return "LCK";
                case EnchantmentType.Protection: return "PRT";
                default: return string.Empty;
            }
        }

        public static Color GetColor(EnchantmentType type)
        {
            switch (type)
            {
                case EnchantmentType.Durability: return new Color(0.55f, 0.60f, 0.68f, 1f);
                case EnchantmentType.Haste: return new Color(0.98f, 0.82f, 0.25f, 1f);
                case EnchantmentType.Sharpness: return new Color(0.90f, 0.30f, 0.30f, 1f);
                case EnchantmentType.Efficiency: return new Color(0.30f, 0.78f, 0.45f, 1f);
                case EnchantmentType.Luck: return new Color(0.70f, 0.45f, 0.90f, 1f);
                case EnchantmentType.Protection: return new Color(0.30f, 0.60f, 0.95f, 1f);
                default: return new Color(0.80f, 0.80f, 0.80f, 1f);
            }
        }

        /// <summary>Badge text such as "SHP III" (tier 0 or None gives just the code / empty string).</summary>
        public static string Badge(EnchantmentType type, int tier)
        {
            string code = ShortName(type);
            if (code.Length == 0) return string.Empty;
            string roman = EnchantmentTierRules.ToRoman(tier);
            return roman.Length == 0 ? code : code + " " + roman;
        }
    }
}
