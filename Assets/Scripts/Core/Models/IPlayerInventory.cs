using System;
using System.Collections.Generic;

namespace ShapeInventory.Core
{
    /// <summary>
    /// Plain grid inventory: every item occupies exactly ONE cell regardless of its shape.
    /// Cell index = row * Columns + column (row 0 is the top row).
    /// </summary>
    public interface IPlayerInventory
    {
        int Columns { get; }
        int Rows { get; }
        int CellCount { get; }

        /// <summary>All items currently stored (no particular order guarantee).</summary>
        IReadOnlyList<ItemInstance> Items { get; }

        /// <summary>Raised after ANY change (add, move, remove).</summary>
        event Action Changed;

        /// <summary>Item at the cell or null if empty / index out of range.</summary>
        ItemInstance GetAt(int cellIndex);

        /// <summary>Cell index of the item or -1 if it is not in this inventory.</summary>
        int IndexOf(ItemInstance item);

        /// <summary>
        /// Puts the item into the given EMPTY cell. If the item is already stored here this MOVES it
        /// (its old cell becomes empty). Returns false (and changes nothing) if the cell is out of range or occupied by another item.
        /// </summary>
        bool TryAddAt(ItemInstance item, int cellIndex);

        /// <summary>Puts the item into the first empty cell (moves it if already stored). False if the grid is full.</summary>
        bool TryAddFirstFree(ItemInstance item, out int cellIndex);

        /// <summary>Removes the item. False if it was not stored here.</summary>
        bool Remove(ItemInstance item);
    }
}
