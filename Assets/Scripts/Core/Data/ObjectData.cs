using UnityEngine;

namespace ShapeInventory.Core
{
    /// <summary>
    /// Static definition of an "object" - the thing that owns an inventory shaped like its silhouette.
    /// </summary>
    [CreateAssetMenu(menuName = "ShapeInventory/Object", fileName = "Object")]
    public class ObjectData : ScriptableObject
    {
        [SerializeField] private string id = "object";
        [SerializeField] private string displayName = "Object";
        [SerializeField] private Color silhouetteColor = new Color(0.25f, 0.25f, 0.3f, 1f);

        [Tooltip("Top row first. '#' = inventory cell, '.' = outside the silhouette.")]
        [SerializeField] private string[] silhouetteRows = { "###", "###" };

        private ItemShape _silhouette;

        public string Id => id;
        public string DisplayName => displayName;
        public Color SilhouetteColor => silhouetteColor;
        public string[] SilhouetteRows => silhouetteRows;

        /// <summary>Silhouette as a shape (normalised: leading empty rows/columns are trimmed).</summary>
        public ItemShape Silhouette => _silhouette ?? (_silhouette = ItemShape.FromRows(silhouetteRows));

        public int Width => Silhouette.Width;
        public int Height => Silhouette.Height;
        public int CellCount => Silhouette.CellCount;
        public bool IsCell(Vector2Int cell) => Silhouette.Contains(cell);

        private void OnValidate() => _silhouette = null;

        public static ObjectData Create(string id, string displayName, Color color, params string[] silhouetteRows)
        {
            var data = CreateInstance<ObjectData>();
            data.id = id;
            data.displayName = displayName;
            data.silhouetteColor = color;
            data.silhouetteRows = silhouetteRows;
            data._silhouette = null;
            data.name = id;
            return data;
        }
    }
}
