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
    ///    that type == requirement.Tier exactly). Satisfied = number of satisfied requirements. MaxScore = required.Count.
    /// 3. WrongCount = number of applied enchantment TYPES that do not appear in ANY requirement.
    /// 4. Outcome (shapeFilled = the object's silhouette is 100% covered, see ShapeFillChecker):
    ///      Full    : every requirement satisfied, WrongCount == 0 AND shapeFilled.
    ///      Partial : Satisfied >= 1 and not Full. This includes "everything right but a wrong type also applied"
    ///                (a partial failure) and "everything right but the shape is not filled".
    ///      Failure : Satisfied == 0 (nothing right, nothing applied, or only wrong types).
    ///    Edge case: empty/null requirements -> Full when WrongCount == 0 and shapeFilled, otherwise Failure (MaxScore = 0).
    /// 5. Score = Satisfied - WrongCount (each wrong type costs one point), never below 0. Only a Full result can reach
    ///    MaxScore: a Partial result is capped at MaxScore - 1, and a Failure always scores 0, so the shown score
    ///    never contradicts the reaction text.
    /// 6. Null/empty placed list is valid input (Failure when there are requirements).
    /// 7. Pure function: no UnityEngine state, no side effects.
    /// The overload without shapeFilled assumes the shape IS filled.
    /// </summary>
    public static class QuestEvaluator
    {
        /// <summary>Same as the three-argument overload with shapeFilled = true.</summary>
        public static QuestResult Evaluate(IReadOnlyList<PlacedItem> placed, IReadOnlyList<EnchantmentRequirement> required)
        {
            return Evaluate(placed, required, true);
        }

        public static QuestResult Evaluate(IReadOnlyList<PlacedItem> placed, IReadOnlyList<EnchantmentRequirement> required,
            bool shapeFilled)
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
            if (wrongCount == 0 && shapeFilled && score == maxScore) outcome = QuestOutcome.Full; // also covers empty requirements
            else if (score >= 1) outcome = QuestOutcome.Partial;
            else outcome = QuestOutcome.Failure;

            int shownScore = outcome == QuestOutcome.Failure ? 0 : System.Math.Max(0, score - wrongCount);
            if (outcome == QuestOutcome.Partial && maxScore > 0 && shownScore >= maxScore) shownScore = maxScore - 1;
            score = shownScore;

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
