using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShapeInventory.Core
{
    /// <summary>OWNED BY AGENT A (Core logic). Stub - replace with the real implementation of IObjectInventory.</summary>
    public sealed class ObjectInventoryModel : IObjectInventory
    {
        public ObjectInventoryModel(ObjectData data)
        {
            Data = data;
        }

        public ObjectData Data { get; }
        public int Width => Data.Width;
        public int Height => Data.Height;
        public IReadOnlyList<PlacedItem> PlacedItems => throw new NotImplementedException();
        public event Action Changed;

        public bool IsSilhouetteCell(Vector2Int cell) => throw new NotImplementedException();
        public bool CanPlace(ItemInstance item, Vector2Int origin) => throw new NotImplementedException();
        public bool TryPlace(ItemInstance item, Vector2Int origin) => throw new NotImplementedException();
        public bool Remove(ItemInstance item) => throw new NotImplementedException();
        public bool TryGetPlaced(ItemInstance item, out PlacedItem placed) => throw new NotImplementedException();
        public ItemInstance GetItemAt(Vector2Int cell) => throw new NotImplementedException();
    }
}
