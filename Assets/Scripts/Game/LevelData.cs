using ShapeInventory.Core;
using UnityEngine;

namespace ShapeInventory.Game
{
    /// <summary>
    /// One level: the object to fill, the items the player starts with, and the quest the object's owner gives.
    /// Items may repeat in the array; every entry becomes its own ItemInstance.
    /// Serialized field names are used by editor tooling via SerializedObject - do not rename them:
    /// targetObject, startingItems, objectSprite, questDescription, requirements, successText, partialText, failureText.
    /// </summary>
    [CreateAssetMenu(menuName = "ShapeInventory/Level", fileName = "Level")]
    public class LevelData : ScriptableObject
    {
        [SerializeField] private ObjectData targetObject;
        [SerializeField] private ItemData[] startingItems = new ItemData[0];

        [Header("Quest")]
        [Tooltip("Drawn behind the silhouette grid of the object inventory.")]
        [SerializeField] private Sprite objectSprite;
        [TextArea(2, 5)]
        [Tooltip("What the owner says, e.g. \"My pickaxe is so dull nowadays...\". Hints at the enchantments needed.")]
        [SerializeField] private string questDescription = "";
        [Tooltip("Enchantments the quest needs. Tier 0 = any tier. All right = full success.")]
        [SerializeField] private EnchantmentRequirement[] requirements = new EnchantmentRequirement[0];

        [Header("Owner reactions")]
        [TextArea(1, 3)] [SerializeField] private string successText = "Yay, that's perfect!";
        [TextArea(1, 3)] [SerializeField] private string partialText = "Okay, that should do the work.";
        [TextArea(1, 3)] [SerializeField] private string failureText = "No, now it's even worse!";

        public ObjectData TargetObject => targetObject;
        public ItemData[] StartingItems => startingItems;

        public Sprite ObjectSprite => objectSprite;
        public string QuestDescription => questDescription;
        public EnchantmentRequirement[] Requirements => requirements;
        public string SuccessText => successText;
        public string PartialText => partialText;
        public string FailureText => failureText;

        /// <summary>Reaction text for an outcome.</summary>
        public string GetOutcomeText(QuestOutcome outcome)
        {
            switch (outcome)
            {
                case QuestOutcome.Full: return successText;
                case QuestOutcome.Partial: return partialText;
                default: return failureText;
            }
        }

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
