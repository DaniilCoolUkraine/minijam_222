using System.Collections.Generic;
using UnityEngine;

namespace ShapeInventory.Core
{
    /// <summary>Result of the "is the object's shape filled" check. More fields can be added later.</summary>
    public sealed class ShapeFillResult
    {
        public bool IsFilled;
    }

    /// <summary>Decides whether the items placed in an object inventory fill the object's shape.</summary>
    public static class ShapeFillChecker
    {
        /// <summary>
        /// The object's shape is filled when EVERY silhouette cell is covered by a placed item.
        /// (CanPlace already guarantees that items stay inside the silhouette and never overlap.)
        /// </summary>
        public static ShapeFillResult IsShapeFilled(IObjectInventory inventory)
        {
            if (inventory == null) return new ShapeFillResult { IsFilled = false };

            var covered = new HashSet<Vector2Int>();
            IReadOnlyList<PlacedItem> placed = inventory.PlacedItems;
            for (int i = 0; i < placed.Count; i++)
            {
                if (placed[i].Item == null) continue;
                foreach (Vector2Int cell in placed[i].OccupiedCells()) covered.Add(cell);
            }

            bool any = false;
            for (int y = 0; y < inventory.Height; y++)
            {
                for (int x = 0; x < inventory.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!inventory.IsSilhouetteCell(cell)) continue;
                    any = true;
                    if (!covered.Contains(cell)) return new ShapeFillResult { IsFilled = false };
                }
            }

            return new ShapeFillResult { IsFilled = any };
        }

        /// <summary>
        /// Legacy placeholder: without the object's silhouette the check cannot be done, so it always reports "filled".
        /// Use <see cref="IsShapeFilled(IObjectInventory)"/>.
        /// </summary>
        public static ShapeFillResult IsShapeFilled(IReadOnlyList<PlacedItem> items)
        {
            return new ShapeFillResult { IsFilled = true };
        }
    }
}
