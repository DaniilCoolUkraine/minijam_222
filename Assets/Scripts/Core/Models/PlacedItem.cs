using System.Collections.Generic;
using UnityEngine;

namespace ShapeInventory.Core
{
    /// <summary>An item placed inside an object inventory. Origin is the grid cell that shape cell (0,0) lands on.</summary>
    public readonly struct PlacedItem
    {
        public ItemInstance Item { get; }
        public Vector2Int Origin { get; }

        public PlacedItem(ItemInstance item, Vector2Int origin)
        {
            Item = item;
            Origin = origin;
        }

        /// <summary>Absolute grid cells occupied by this placement.</summary>
        public IEnumerable<Vector2Int> OccupiedCells()
        {
            foreach (var c in Item.Shape.Cells)
                yield return Origin + c;
        }
    }
}
