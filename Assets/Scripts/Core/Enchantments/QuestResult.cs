namespace ShapeInventory.Core
{
    public enum QuestOutcome
    {
        /// <summary>Nothing correct applied, or an unrequired enchantment type was applied.</summary>
        Failure = 0,
        /// <summary>At least one requirement met, no unrequired type applied, but not everything is right.</summary>
        Partial = 1,
        /// <summary>Applied enchantments equal the required ones exactly.</summary>
        Full = 2
    }

    public sealed class QuestResult
    {
        public QuestOutcome Outcome;

        /// <summary>Number of requirements satisfied (right type AND right tier).</summary>
        public int Score;

        /// <summary>Total number of requirements (the maximum score).</summary>
        public int MaxScore;

        /// <summary>Number of applied enchantment TYPES that the quest did not ask for.</summary>
        public int WrongCount;
    }
}
