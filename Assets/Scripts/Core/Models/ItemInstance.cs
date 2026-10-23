using System;

namespace ShapeInventory.Core
{
    /// <summary>A concrete item in the world. Reference equality identifies it.</summary>
    public sealed class ItemInstance
    {
        private static int _nextId = 1;

        public int InstanceId { get; }
        public ItemData Data { get; }
        public ItemShape Shape => Data.Shape;

        public ItemInstance(ItemData data)
        {
            Data = data != null ? data : throw new ArgumentNullException(nameof(data));
            InstanceId = _nextId++;
        }

        public override string ToString() => $"{Data.DisplayName}#{InstanceId}";
    }
}
