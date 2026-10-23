using System;
using System.Collections.Generic;

namespace ShapeInventory.Core
{
    /// <summary>
    /// Fixed Columns x Rows grid, exactly one item per cell (shape is ignored).
    /// Cell index = row * Columns + column. Re-adding an already stored item MOVES it.
    /// </summary>
    public sealed class PlayerInventoryModel : IPlayerInventory
    {
        private readonly ItemInstance[] _cells;
        private readonly Dictionary<ItemInstance, int> _indexByItem = new Dictionary<ItemInstance, int>();
        private IReadOnlyList<ItemInstance> _itemsSnapshot;

        public PlayerInventoryModel(int columns, int rows)
        {
            if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns), "Columns must be greater than zero.");
            if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows), "Rows must be greater than zero.");

            Columns = columns;
            Rows = rows;
            _cells = new ItemInstance[columns * rows];
        }

        public int Columns { get; }
        public int Rows { get; }
        public int CellCount => _cells.Length;

        /// <summary>Read-only snapshot in cell order. A new snapshot is created after every change, so a previously obtained list never changes.</summary>
        public IReadOnlyList<ItemInstance> Items
        {
            get
            {
                if (_itemsSnapshot == null)
                {
                    var list = new List<ItemInstance>(_indexByItem.Count);
                    for (int i = 0; i < _cells.Length; i++)
                    {
                        if (_cells[i] != null) list.Add(_cells[i]);
                    }

                    _itemsSnapshot = list.AsReadOnly();
                }

                return _itemsSnapshot;
            }
        }

        public event Action Changed;

        public ItemInstance GetAt(int cellIndex)
        {
            if (cellIndex < 0 || cellIndex >= _cells.Length) return null;
            return _cells[cellIndex];
        }

        public int IndexOf(ItemInstance item)
        {
            if (item == null) return -1;
            int index;
            return _indexByItem.TryGetValue(item, out index) ? index : -1;
        }

        public bool TryAddAt(ItemInstance item, int cellIndex)
        {
            if (item == null) return false;
            if (cellIndex < 0 || cellIndex >= _cells.Length) return false;

            var occupant = _cells[cellIndex];
            if (occupant != null)
            {
                // Dropping an item onto its own cell is accepted but is not a change.
                return ReferenceEquals(occupant, item);
            }

            int oldIndex;
            if (_indexByItem.TryGetValue(item, out oldIndex))
                _cells[oldIndex] = null;

            _cells[cellIndex] = item;
            _indexByItem[item] = cellIndex;
            RaiseChanged();
            return true;
        }

        public bool TryAddFirstFree(ItemInstance item, out int cellIndex)
        {
            cellIndex = -1;
            if (item == null) return false;

            // The item's own cell counts as free so an item already sitting in the first free slot stays there.
            for (int i = 0; i < _cells.Length; i++)
            {
                var occupant = _cells[i];
                if (occupant != null && !ReferenceEquals(occupant, item)) continue;

                cellIndex = i;
                if (occupant == null)
                    return TryAddAt(item, i);

                return true; // already stored exactly here, nothing to change
            }

            return false;
        }

        public bool Remove(ItemInstance item)
        {
            if (item == null) return false;

            int index;
            if (!_indexByItem.TryGetValue(item, out index)) return false;

            _cells[index] = null;
            _indexByItem.Remove(item);
            RaiseChanged();
            return true;
        }

        private void RaiseChanged()
        {
            _itemsSnapshot = null;
            var handler = Changed;
            if (handler != null) handler();
        }
    }
}
