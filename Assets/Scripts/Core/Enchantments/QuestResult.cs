namespace ShapeInventory.Core
{
    public enum QuestOutcome
    {
        /// <summary>Nothing correct applied (nothing placed, or only unrequired types).</summary>
        Failure = 0,
        /// <summary>At least one requirement met, but not a Full result: something missing, a wrong tier, an unrequired
        /// type applied as well, or the shape not completely filled.</summary>
        Partial = 1,
        /// <summary>Applied enchantments equal the required ones exactly AND the object's shape is completely filled.</summary>
        Full = 2
    }

    public sealed class QuestResult
    {
        public QuestOutcome Outcome;

        /// <summary>Requirements satisfied (right type AND right tier), minus one per wrong type, never below 0.
        /// Only a Full result reaches MaxScore; a Failure always scores 0.</summary>
        public int Score;

        /// <summary>Total number of requirements (the maximum score).</summary>
        public int MaxScore;

        /// <summary>Number of applied enchantment TYPES that the quest did not ask for.</summary>
        public int WrongCount;
    }
}
