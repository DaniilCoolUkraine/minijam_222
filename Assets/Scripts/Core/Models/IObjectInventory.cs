using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShapeInventory.Core
{
    /// <summary>
    /// Object inventory: a grid shaped like the object's silhouette. Items keep their full Tetris shape.
    /// Grid coordinates: x right, y DOWN, (0,0) is the top-left of the silhouette bounding box.
    /// </summary>
    public interface IObjectInventory
    {
        ObjectData Data { get; }
        int Width { get; }
        int Height { get; }

        /// <summary>True if the cell is part of the silhouette (i.e. can hold an item cell).</summary>
        bool IsSilhouetteCell(Vector2Int cell);

        IReadOnlyList<PlacedItem> PlacedItems { get; }

        /// <summary>Raised after ANY change (place, move, remove).</summary>
        event Action Changed;

        /// <summary>
        /// True if every shape cell of the item, offset by origin, is inside the silhouette and not occupied
        /// by ANOTHER item. If the item is already placed here its own current cells are ignored (so it can be moved/nudged).
        /// </summary>
        bool CanPlace(ItemInstance item, Vector2Int origin);

        /// <summary>Places the item (or MOVES it if already placed here). False and no change if CanPlace is false.</summary>
        bool TryPlace(ItemInstance item, Vector2Int origin);

        /// <summary>Removes the item. False if it is not placed here.</summary>
        bool Remove(ItemInstance item);

        bool TryGetPlaced(ItemInstance item, out PlacedItem placed);

        /// <summary>Item covering the cell or null.</summary>
        ItemInstance GetItemAt(Vector2Int cell);
    }
}
