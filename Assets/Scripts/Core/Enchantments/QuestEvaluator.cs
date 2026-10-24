using System.Collections.Generic;

namespace ShapeInventory.Core
{
    /// <summary>
    /// STUB - implemented by Agent A. Rules (frozen, this is the spec):
    ///
    /// 1. Applied enchantments: for every placed item whose ItemData.Enchantment != None, take the item's
    ///    tier (ItemData.Tier). For each enchantment TYPE keep the HIGHEST tier among placed items of that type.
    ///    Items with Enchantment == None are neutral filler and are ignored completely.
    /// 2. A requirement is satisfied when its type is applied AND (requirement.AnyTier OR the applied tier of
    ///    that type == requirement.Tier exactly). Score = number of satisfied requirements. MaxScore = required.Count.
    /// 3. WrongCount = number of applied enchantment TYPES that do not appear in ANY requirement.
    /// 4. Outcome:
    ///      Full    : Score == MaxScore and WrongCount == 0.
    ///      Partial : Score >= 1 and WrongCount == 0 and not Full
    ///                (some requirements missing, or a required type applied at the wrong tier).
    ///      Failure : everything else (Score == 0, or WrongCount > 0, or nothing applied).
    ///    Edge case: empty/null requirements -> Full when WrongCount == 0, otherwise Failure (MaxScore = 0).
    /// 5. Null/empty placed list is valid input (Failure when there are requirements).
    /// 6. Pure function: no allocations are required to be avoided, no UnityEngine state, no side effects.
    /// </summary>
    public static class QuestEvaluator
    {
        public static QuestResult Evaluate(IReadOnlyList<PlacedItem> placed, IReadOnlyList<EnchantmentRequirement> required)
        {
            // TODO(Agent A)
            return new QuestResult
            {
                Outcome = QuestOutcome.Failure,
                Score = 0,
                MaxScore = required != null ? required.Count : 0,
                WrongCount = 0
            };
        }
    }
}
