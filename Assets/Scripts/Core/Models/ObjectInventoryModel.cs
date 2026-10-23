using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShapeInventory.Core
{
    /// <summary>
    /// Inventory shaped like the object's silhouette. Items keep their full shape; placing an
    /// already placed item MOVES it (its own current cells never block itself).
    /// </summary>
    public sealed class ObjectInventoryModel : IObjectInventory
    {
        private readonly ItemShape _silhouette;
        private readonly List<PlacedItem> _placed = new List<PlacedItem>();
        private readonly Dictionary<Vector2Int, ItemInstance> _cellMap = new Dictionary<Vector2Int, ItemInstance>();
        private IReadOnlyList<PlacedItem> _placedSnapshot;

        public ObjectInventoryModel(ObjectData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            Data = data;
            _silhouette = data.Silhouette;
        }

        public ObjectData Data { get; }
        public int Width => _silhouette.Width;
        public int Height => _silhouette.Height;

        /// <summary>Read-only snapshot of the current placements. A new snapshot is created after every change.</summary>
        public IReadOnlyList<PlacedItem> PlacedItems
        {
            get
            {
                if (_placedSnapshot == null)
                    _placedSnapshot = new List<PlacedItem>(_placed).AsReadOnly();
                return _placedSnapshot;
            }
        }

        public event Action Changed;

        public bool IsSilhouetteCell(Vector2Int cell)
        {
            return _silhouette.Contains(cell);
        }

        public bool CanPlace(ItemInstance item, Vector2Int origin)
        {
            if (item == null) return false;

            var cells = item.Shape.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int abs = origin + cells[i];
                if (!_silhouette.Contains(abs)) return false;

                ItemInstance other;
                if (_cellMap.TryGetValue(abs, out other) && !ReferenceEquals(other, item)) return false;
            }

            return true;
        }

        public bool TryPlace(ItemInstance item, Vector2Int origin)
        {
            if (!CanPlace(item, origin)) return false;

            var cells = item.Shape.Cells;
            int existing = FindIndex(item);
            if (existing >= 0)
            {
                PlacedItem old = _placed[existing];
                if (old.Origin == origin) return true; // nothing moves, nothing changes

                RemoveCells(old);
                _placed[existing] = new PlacedItem(item, origin);
            }
            else
            {
                _placed.Add(new PlacedItem(item, origin));
            }

            for (int i = 0; i < cells.Count; i++)
                _cellMap[origin + cells[i]] = item;

            RaiseChanged();
            return true;
        }

        public bool Remove(ItemInstance item)
        {
            if (item == null) return false;

            int index = FindIndex(item);
            if (index < 0) return false;

            RemoveCells(_placed[index]);
            _placed.RemoveAt(index);
            RaiseChanged();
            return true;
        }

        public bool TryGetPlaced(ItemInstance item, out PlacedItem placed)
        {
            int index = item == null ? -1 : FindIndex(item);
            if (index < 0)
            {
                placed = default(PlacedItem);
                return false;
            }

            placed = _placed[index];
            return true;
        }

        public ItemInstance GetItemAt(Vector2Int cell)
        {
            ItemInstance item;
            return _cellMap.TryGetValue(cell, out item) ? item : null;
        }

        private int FindIndex(ItemInstance item)
        {
            for (int i = 0; i < _placed.Count; i++)
            {
                if (ReferenceEquals(_placed[i].Item, item)) return i;
            }

            return -1;
        }

        private void RemoveCells(PlacedItem placed)
        {
            var cells = placed.Item.Shape.Cells;
            for (int i = 0; i < cells.Count; i++)
                _cellMap.Remove(placed.Origin + cells[i]);
        }

        private void RaiseChanged()
        {
            _placedSnapshot = null;
            var handler = Changed;
            if (handler != null) handler();
        }
    }
}
