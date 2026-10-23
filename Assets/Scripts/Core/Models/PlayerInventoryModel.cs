using System;
using System.Collections.Generic;

namespace ShapeInventory.Core
{
    /// <summary>OWNED BY AGENT A (Core logic). Stub - replace with the real implementation of IPlayerInventory.</summary>
    public sealed class PlayerInventoryModel : IPlayerInventory
    {
        public PlayerInventoryModel(int columns, int rows)
        {
            Columns = columns;
            Rows = rows;
        }

        public int Columns { get; }
        public int Rows { get; }
        public int CellCount => Columns * Rows;
        public IReadOnlyList<ItemInstance> Items => throw new NotImplementedException();
        public event Action Changed;

        public ItemInstance GetAt(int cellIndex) => throw new NotImplementedException();
        public int IndexOf(ItemInstance item) => throw new NotImplementedException();
        public bool TryAddAt(ItemInstance item, int cellIndex) => throw new NotImplementedException();
        public bool TryAddFirstFree(ItemInstance item, out int cellIndex) => throw new NotImplementedException();
        public bool Remove(ItemInstance item) => throw new NotImplementedException();
    }
}
