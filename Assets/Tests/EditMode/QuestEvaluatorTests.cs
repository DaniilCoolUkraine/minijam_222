using System.Collections.Generic;
using NUnit.Framework;
using ShapeInventory.Core;
using UnityEngine;

namespace ShapeInventory.Tests
{
    public class QuestEvaluatorTests
    {
        private const string Small = "##";      // 2 cells -> tier I
        private const string Medium = "###";    // 3 cells -> tier II
        private const string Large = "####";    // 4 cells -> tier III

        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();
        private int _counter;

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created)
            {
                if (o != null) UnityEngine.Object.DestroyImmediate(o);
            }

            _created.Clear();
        }

        private PlacedItem Place(EnchantmentType type, string shapeRow)
        {
            _counter++;
            string id = "item" + _counter;
            var data = ItemData.Create(id, id, Color.white, type, shapeRow);
            _created.Add(data);
            return new PlacedItem(new ItemInstance(data), Vector2Int.zero);
        }

        private PlacedItem Plain(string shapeRow)
        {
            return Place(EnchantmentType.None, shapeRow);
        }

        private static List<PlacedItem> Items(params PlacedItem[] items)
        {
            return new List<PlacedItem>(items);
        }

        /// <summary>The example quest: Durability (any tier), Sharpness (any tier), Efficiency III.</summary>
        private static List<EnchantmentRequirement> ExampleRequirements()
        {
            return new List<EnchantmentRequirement>
            {
                new EnchantmentRequirement(EnchantmentType.Durability, 0),
                new EnchantmentRequirement(EnchantmentType.Sharpness, 0),
                new EnchantmentRequirement(EnchantmentType.Efficiency, 3)
            };
        }

        private static void AssertResult(QuestResult r, QuestOutcome outcome, int score, int maxScore, int wrong)
        {
            Assert.IsNotNull(r);
            Assert.AreEqual(outcome, r.Outcome, "Outcome");
            Assert.AreEqual(score, r.Score, "Score");
            Assert.AreEqual(maxScore, r.MaxScore, "MaxScore");
            Assert.AreEqual(wrong, r.WrongCount, "WrongCount");
        }

        // ---------------------------------------------------------------- full

        [Test]
        public void Full_ExactSetOfEnchantments()
        {
            var placed = Items(
                Place(EnchantmentType.Durability, Small),
                Place(EnchantmentType.Sharpness, Medium),
                Place(EnchantmentType.Efficiency, Large));

            var r = QuestEvaluator.Evaluate(placed, ExampleRequirements());

            AssertResult(r, QuestOutcome.Full, 3, 3, 0);
        }

        [Test]
        public void Full_AnyTierAcceptsEveryTier()
        {
            var requirements = new List<EnchantmentRequirement>
            {
                new EnchantmentRequirement(EnchantmentType.Luck, 0)
            };

            foreach (var row in new[] { "#", Small, Medium, Large })
            {
                var r = QuestEvaluator.Evaluate(Items(Place(EnchantmentType.Luck, row)), requirements);
                AssertResult(r, QuestOutcome.Full, 1, 1, 0);
            }
        }

        [Test]
        public void Full_PlainItemsAreNeutral()
        {
            var placed = Items(
                Plain("#"),
                Place(EnchantmentType.Durability, Small),
                Plain(Large),
                Place(EnchantmentType.Sharpness, Small),
                Plain(Medium),
                Place(EnchantmentType.Efficiency, Large));

            var r = QuestEvaluator.Evaluate(placed, ExampleRequirements());

            AssertResult(r, QuestOutcome.Full, 3, 3, 0);
        }

        [Test]
        public void Full_SeveralItemsOfSameRequiredType()
        {
            var placed = Items(
                Place(EnchantmentType.Durability, Small),
                Place(EnchantmentType.Durability, Large),
                Place(EnchantmentType.Sharpness, "#"),
                Place(EnchantmentType.Efficiency, Large),
                Place(EnchantmentType.Efficiency, Large));

            var r = QuestEvaluator.Evaluate(placed, ExampleRequirements());

            AssertResult(r, QuestOutcome.Full, 3, 3, 0);
        }

        // ---------------------------------------------------------------- partial

        [Test]
        public void Partial_OnlySomeRequirementsMet()
        {
            var placed = Items(
                Place(EnchantmentType.Durability, Small),
                Place(EnchantmentType.Efficiency, Large));

            var r = QuestEvaluator.Evaluate(placed, ExampleRequirements());

            AssertResult(r, QuestOutcome.Partial, 2, 3, 0);
        }

        [Test]
        public void Partial_SingleRightEnchantment()
        {
            var placed = Items(Place(EnchantmentType.Sharpness, Medium));

            var r = QuestEvaluator.Evaluate(placed, ExampleRequirements());

            AssertResult(r, QuestOutcome.Partial, 1, 3, 0);
        }

        [Test]
        public void Partial_RightTypeWrongTierPlusAnotherRight()
        {
            // Efficiency only tier II (needs III), but Durability is right.
            var placed = Items(
                Place(EnchantmentType.Efficiency, Medium),
                Place(EnchantmentType.Durability, Small));

            var r = QuestEvaluator.Evaluate(placed, ExampleRequirements());

            AssertResult(r, QuestOutcome.Partial, 1, 3, 0);
        }

        [Test]
        public void Partial_TypeAtHigherTierThanRequired_DoesNotSatisfy()
        {
            var requirements = new List<EnchantmentRequirement>
            {
                new EnchantmentRequirement(EnchantmentType.Haste, 1),
                new EnchantmentRequirement(EnchantmentType.Luck, 0)
            };
            var placed = Items(
                Place(EnchantmentType.Haste, Large),   // tier III, needs exactly I
                Place(EnchantmentType.Luck, Small));

            var r = QuestEvaluator.Evaluate(placed, requirements);

            AssertResult(r, QuestOutcome.Partial, 1, 2, 0);
        }

        [Test]
        public void Partial_AllTypesPresentButOneTierWrong()
        {
            var placed = Items(
                Place(EnchantmentType.Durability, Small),
                Place(EnchantmentType.Sharpness, Small),
                Place(EnchantmentType.Efficiency, Small));

            var r = QuestEvaluator.Evaluate(placed, ExampleRequirements());

            AssertResult(r, QuestOutcome.Partial, 2, 3, 0);
        }

        [Test]
        public void Partial_PlainItemsDoNotMakeItWorse()
        {
            var placed = Items(
                Plain("#"),
                Plain(Large),
                Place(EnchantmentType.Durability, Small));

            var r = QuestEvaluator.Evaluate(placed, ExampleRequirements());

            AssertResult(r, QuestOutcome.Partial, 1, 3, 0);
        }

        // ---------------------------------------------------------------- failure

        [Test]
        public void Failure_NothingPlaced()
        {
            var r = QuestEvaluator.Evaluate(new List<PlacedItem>(), ExampleRequirements());

            AssertResult(r, QuestOutcome.Failure, 0, 3, 0);
        }

        [Test]
        public void Failure_OnlyPlainItems()
        {
            var placed = Items(Plain("#"), Plain(Small), Plain(Large));

            var r = QuestEvaluator.Evaluate(placed, ExampleRequirements());

            AssertResult(r, QuestOutcome.Failure, 0, 3, 0);
        }

        [Test]
        public void Failure_OnlyWrongTypes()
        {
            var placed = Items(
                Place(EnchantmentType.Luck, Small),
                Place(EnchantmentType.Protection, Large));

            var r = QuestEvaluator.Evaluate(placed, ExampleRequirements());

            AssertResult(r, QuestOutcome.Failure, 0, 3, 2);
        }

        [Test]
        public void Partial_OneRightPlusOneUnrequiredType_ScoresZero()
        {
            var placed = Items(
                Place(EnchantmentType.Durability, Small),
                Place(EnchantmentType.Luck, Small));

            var r = QuestEvaluator.Evaluate(placed, ExampleRequirements());

            // one point for the right type, minus one point for the wrong one
            AssertResult(r, QuestOutcome.Partial, 0, 3, 1);
        }

        [Test]
        public void Partial_EverythingRightPlusOneUnrequiredType_LosesOnePoint()
        {
            var placed = Items(
                Place(EnchantmentType.Durability, Small),
                Place(EnchantmentType.Sharpness, Small),
                Place(EnchantmentType.Efficiency, Large),
                Place(EnchantmentType.Haste, "#"));

            var r = QuestEvaluator.Evaluate(placed, ExampleRequirements());

            AssertResult(r, QuestOutcome.Partial, 2, 3, 1);
        }

        [Test]
        public void Partial_TwoWrongTypesCostTwoPointsButNeverBelowZero()
        {
            var placed = Items(
                Place(EnchantmentType.Durability, Small),
                Place(EnchantmentType.Sharpness, Small),
                Place(EnchantmentType.Luck, Small),
                Place(EnchantmentType.Protection, Small));

            var r = QuestEvaluator.Evaluate(placed, ExampleRequirements());
            AssertResult(r, QuestOutcome.Partial, 0, 3, 2);
        }

        [Test]
        public void ShapeNotFilled_EverythingRight_IsPartialAndCappedBelowMax()
        {
            var placed = Items(
                Place(EnchantmentType.Durability, Small),
                Place(EnchantmentType.Sharpness, Small),
                Place(EnchantmentType.Efficiency, Large));

            var r = QuestEvaluator.Evaluate(placed, ExampleRequirements(), false);
            AssertResult(r, QuestOutcome.Partial, 2, 3, 0);

            var filled = QuestEvaluator.Evaluate(placed, ExampleRequirements(), true);
            AssertResult(filled, QuestOutcome.Full, 3, 3, 0);
        }

        [Test]
        public void ShapeNotFilled_NothingRight_StaysFailure()
        {
            var placed = Items(Place(EnchantmentType.Luck, Small));

            var r = QuestEvaluator.Evaluate(placed, ExampleRequirements(), false);
            AssertResult(r, QuestOutcome.Failure, 0, 3, 1);
        }

        [Test]
        public void Failure_RightTypeWrongTierOnly()
        {
            var placed = Items(Place(EnchantmentType.Efficiency, Small));

            var r = QuestEvaluator.Evaluate(placed, ExampleRequirements());

            AssertResult(r, QuestOutcome.Failure, 0, 3, 0);
        }

        // ---------------------------------------------------------------- max tier per type

        [Test]
        public void MaxTier_TwoAndFourCellEfficiency_SatisfiesTierThree()
        {
            var requirements = new List<EnchantmentRequirement>
            {
                new EnchantmentRequirement(EnchantmentType.Efficiency, 3)
            };
            var placed = Items(
                Place(EnchantmentType.Efficiency, Small),
                Place(EnchantmentType.Efficiency, Large));

            var r = QuestEvaluator.Evaluate(placed, requirements);

            AssertResult(r, QuestOutcome.Full, 1, 1, 0);
        }

        [Test]
        public void MaxTier_MaxIsIndependentOfPlacementOrder()
        {
            var requirements = new List<EnchantmentRequirement>
            {
                new EnchantmentRequirement(EnchantmentType.Efficiency, 3)
            };
            var placed = Items(
                Place(EnchantmentType.Efficiency, Large),
                Place(EnchantmentType.Efficiency, Small));

            var r = QuestEvaluator.Evaluate(placed, requirements);

            AssertResult(r, QuestOutcome.Full, 1, 1, 0);
        }

        [Test]
        public void MaxTier_TwoCellEfficiencyAlone_IsTierOneAndDoesNotSatisfy()
        {
            var requirements = new List<EnchantmentRequirement>
            {
                new EnchantmentRequirement(EnchantmentType.Efficiency, 3)
            };
            var placed = Items(Place(EnchantmentType.Efficiency, Small));

            var r = QuestEvaluator.Evaluate(placed, requirements);

            AssertResult(r, QuestOutcome.Failure, 0, 1, 0);
        }

        [Test]
        public void MaxTier_ManySmallPiecesDoNotAddUpToHigherTier()
        {
            var requirements = new List<EnchantmentRequirement>
            {
                new EnchantmentRequirement(EnchantmentType.Efficiency, 2)
            };
            var placed = Items(
                Place(EnchantmentType.Efficiency, Small),
                Place(EnchantmentType.Efficiency, Small),
                Place(EnchantmentType.Efficiency, "#"));

            var r = QuestEvaluator.Evaluate(placed, requirements);

            AssertResult(r, QuestOutcome.Failure, 0, 1, 0);
        }

        [Test]
        public void MaxTier_HigherTierPieceOverridesExactLowerRequirement()
        {
            // Required exactly tier I, but a 4-cell piece of the same type is also placed: max is III -> not satisfied.
            var requirements = new List<EnchantmentRequirement>
            {
                new EnchantmentRequirement(EnchantmentType.Haste, 1)
            };
            var placed = Items(
                Place(EnchantmentType.Haste, Small),
                Place(EnchantmentType.Haste, Large));

            var r = QuestEvaluator.Evaluate(placed, requirements);

            AssertResult(r, QuestOutcome.Failure, 0, 1, 0);
        }

        [Test]
        public void WrongCount_CountsTypesNotItems()
        {
            var placed = Items(
                Place(EnchantmentType.Luck, Small),
                Place(EnchantmentType.Luck, Large),
                Place(EnchantmentType.Luck, "#"),
                Place(EnchantmentType.Protection, Small));

            var r = QuestEvaluator.Evaluate(placed, ExampleRequirements());

            Assert.AreEqual(2, r.WrongCount);
            Assert.AreEqual(QuestOutcome.Failure, r.Outcome);
        }

        // ---------------------------------------------------------------- edge cases

        [Test]
        public void NullPlaced_WithRequirements_IsFailure()
        {
            var r = QuestEvaluator.Evaluate(null, ExampleRequirements());

            AssertResult(r, QuestOutcome.Failure, 0, 3, 0);
        }

        [Test]
        public void EmptyRequirements_NothingApplied_IsFull()
        {
            var r = QuestEvaluator.Evaluate(new List<PlacedItem>(), new List<EnchantmentRequirement>());

            AssertResult(r, QuestOutcome.Full, 0, 0, 0);
        }

        [Test]
        public void EmptyRequirements_OnlyPlainItems_IsFull()
        {
            var r = QuestEvaluator.Evaluate(Items(Plain("#"), Plain(Large)), new List<EnchantmentRequirement>());

            AssertResult(r, QuestOutcome.Full, 0, 0, 0);
        }

        [Test]
        public void EmptyRequirements_AnyEnchantmentApplied_IsFailure()
        {
            var r = QuestEvaluator.Evaluate(
                Items(Place(EnchantmentType.Luck, Small)),
                new List<EnchantmentRequirement>());

            AssertResult(r, QuestOutcome.Failure, 0, 0, 1);
        }

        [Test]
        public void NullRequirements_BehaveLikeEmpty()
        {
            var full = QuestEvaluator.Evaluate(null, null);
            AssertResult(full, QuestOutcome.Full, 0, 0, 0);

            var failure = QuestEvaluator.Evaluate(Items(Place(EnchantmentType.Luck, Small)), null);
            AssertResult(failure, QuestOutcome.Failure, 0, 0, 1);
        }

        [Test]
        public void DefaultPlacedItem_IsIgnored()
        {
            var placed = Items(default(PlacedItem), Place(EnchantmentType.Durability, Small));

            var r = QuestEvaluator.Evaluate(placed, ExampleRequirements());

            AssertResult(r, QuestOutcome.Partial, 1, 3, 0);
        }

        [Test]
        public void Evaluate_AcceptsArraysAsReadOnlyLists()
        {
            PlacedItem[] placed =
            {
                Place(EnchantmentType.Durability, Small),
                Place(EnchantmentType.Sharpness, Small),
                Place(EnchantmentType.Efficiency, Large)
            };
            EnchantmentRequirement[] requirements = ExampleRequirements().ToArray();

            var r = QuestEvaluator.Evaluate(placed, requirements);

            AssertResult(r, QuestOutcome.Full, 3, 3, 0);
        }

        [Test]
        public void Evaluate_DoesNotMutateInputs()
        {
            var placed = Items(Place(EnchantmentType.Durability, Small), Plain("#"));
            var requirements = ExampleRequirements();

            QuestEvaluator.Evaluate(placed, requirements);
            QuestEvaluator.Evaluate(placed, requirements);

            Assert.AreEqual(2, placed.Count);
            Assert.AreEqual(3, requirements.Count);
        }
    }
}
