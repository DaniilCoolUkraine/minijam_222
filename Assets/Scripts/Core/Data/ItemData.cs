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

        [Tooltip("Top row first. '#' = filled cell, '.' = empty. Example: \"##\", \"#.\"")]
        [SerializeField] private string[] shapeRows = { "#" };

        private ItemShape _shape;

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public Color Color => color;
        public string[] ShapeRows => shapeRows;
        public ItemShape Shape => _shape ?? (_shape = ItemShape.FromRows(shapeRows));

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
    }
}
