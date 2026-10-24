using UnityEngine;

namespace ShapeInventory.Core
{
    /// <summary>Static definition of an item type (ScriptableObject).</summary>
    [CreateAssetMenu(menuName = "ShapeInventory/Item", fileName = "Item")]
    public class ItemData : ScriptableObject
    {
        [SerializeField] private string id = "item";
        [SerializeField] private string displayName = "Item";
        [SerializeField] private Sprite icon;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private EnchantmentType enchantment = EnchantmentType.None;

        [Tooltip("Top row first. '#' = filled cell, '.' = empty. Example: \"##\", \"#.\"")]
        [SerializeField] private string[] shapeRows = { "#" };

        private ItemShape _shape;

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public Color Color => color;
        public string[] ShapeRows => shapeRows;
        public ItemShape Shape => _shape ?? (_shape = ItemShape.FromRows(shapeRows));

        public EnchantmentType Enchantment => enchantment;
        public bool HasEnchantment => enchantment != EnchantmentType.None;

        /// <summary>Tier derived from the item's size (see EnchantmentTierRules). Meaningful only when HasEnchantment.</summary>
        public int Tier => EnchantmentTierRules.GetTier(Shape.CellCount);

        private void OnValidate() => _shape = null;

        /// <summary>Runtime/editor/test factory (ScriptableObject.CreateInstance + Initialize).</summary>
        public static ItemData Create(string id, string displayName, Color color, params string[] shapeRows)
        {
            var data = CreateInstance<ItemData>();
            data.id = id;
            data.displayName = displayName;
            data.color = color;
            data.shapeRows = shapeRows;
            data._shape = null;
            data.name = id;
            return data;
        }

        /// <summary>Same as the plain factory but with an enchantment.</summary>
        public static ItemData Create(string id, string displayName, Color color, EnchantmentType enchantment, params string[] shapeRows)
        {
            var data = Create(id, displayName, color, shapeRows);
            data.enchantment = enchantment;
            return data;
        }
    }
}
