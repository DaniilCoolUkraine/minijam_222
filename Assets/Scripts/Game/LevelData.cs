using ShapeInventory.Core;
using UnityEngine;

namespace ShapeInventory.Game
{
    /// <summary>
    /// One level: the object to fill and the items the player starts with.
    /// Items may repeat in the array; every entry becomes its own ItemInstance.
    /// </summary>
    [CreateAssetMenu(menuName = "ShapeInventory/Level", fileName = "Level")]
    public class LevelData : ScriptableObject
    {
        [SerializeField] private ObjectData targetObject;
        [SerializeField] private ItemData[] startingItems = new ItemData[0];

        public ObjectData TargetObject => targetObject;
        public ItemData[] StartingItems => startingItems;

        /// <summary>Runtime/editor factory.</summary>
        public static LevelData Create(string name, ObjectData target, ItemData[] items)
        {
            var level = CreateInstance<LevelData>();
            level.name = name;
            level.targetObject = target;
            level.startingItems = items ?? new ItemData[0];
            return level;
        }
    }
}
