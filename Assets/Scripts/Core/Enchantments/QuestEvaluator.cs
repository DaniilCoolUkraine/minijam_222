using System.Collections.Generic;

namespace ShapeInventory.Core
{
    /// <summary>
    /// Rules (frozen, this is the spec):
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
    ///    A Failure never scores: when the outcome is Failure, Score is reported as 0 (a type the quest did not ask for
    ///    ruins the job even if every requirement was met), so the shown score and the total never reward a failed quest.
    ///    Edge case: empty/null requirements -> Full when WrongCount == 0, otherwise Failure (MaxScore = 0).
    /// 5. Null/empty placed list is valid input (Failure when there are requirements).
    /// 6. Pure function: no allocations are required to be avoided, no UnityEngine state, no side effects.
    /// </summary>
    public static class QuestEvaluator
    {
        public static QuestResult Evaluate(IReadOnlyList<PlacedItem> placed, IReadOnlyList<EnchantmentRequirement> required)
        {
            // Highest tier per applied enchantment type. None / missing data is ignored.
            var applied = new Dictionary<EnchantmentType, int>();
            if (placed != null)
            {
                for (int i = 0; i < placed.Count; i++)
                {
                    ItemInstance instance = placed[i].Item;
                    if (instance == null || instance.Data == null) continue;

                    ItemData data = instance.Data;
                    if (data.Enchantment == EnchantmentType.None) continue;

                    int tier = data.Tier;
                    int best;
                    if (!applied.TryGetValue(data.Enchantment, out best) || tier > best)
                        applied[data.Enchantment] = tier;
                }
            }

            int maxScore = required != null ? required.Count : 0;
            int score = 0;
            var requiredTypes = new HashSet<EnchantmentType>();
            for (int i = 0; i < maxScore; i++)
            {
                EnchantmentRequirement req = required[i];
                requiredTypes.Add(req.Type);

                int appliedTier;
                if (!applied.TryGetValue(req.Type, out appliedTier)) continue;
                if (req.AnyTier || appliedTier == req.Tier) score++;
            }

            int wrongCount = 0;
            foreach (KeyValuePair<EnchantmentType, int> kv in applied)
            {
                if (!requiredTypes.Contains(kv.Key)) wrongCount++;
            }

            QuestOutcome outcome;
            if (wrongCount > 0) outcome = QuestOutcome.Failure;
            else if (score == maxScore) outcome = QuestOutcome.Full; // also covers empty requirements
            else if (score >= 1) outcome = QuestOutcome.Partial;
            else outcome = QuestOutcome.Failure;

            if (outcome == QuestOutcome.Failure) score = 0;

            return new QuestResult
            {
                Outcome = outcome,
                Score = score,
                MaxScore = maxScore,
                WrongCount = wrongCount
            };
        }
    }
}
