using System;
using System.Collections.Generic;
using NUnit.Framework;
using ShapeInventory.Core;
using UnityEngine;

namespace ShapeInventory.Tests
{
    public class PlayerInventoryModelTests
    {
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();
        private PlayerInventoryModel _inv;
        private int _changes;

        [SetUp]
        public void SetUp()
        {
            _inv = new PlayerInventoryModel(3, 2); // 6 cells
            _changes = 0;
            _inv.Changed += () => _changes++;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created)
            {
                if (o != null) UnityEngine.Object.DestroyImmediate(o);
            }

            _created.Clear();
        }

        private ItemInstance NewItem(string id = "dot", params string[] rows)
        {
            if (rows == null || rows.Length == 0) rows = new[] { "#" };
            var data = ItemData.Create(id, id, Color.white, rows);
            _created.Add(data);
            return new ItemInstance(data);
        }

        [Test]
        public void Constructor_SetsDimensions()
        {
            Assert.AreEqual(3, _inv.Columns);
            Assert.AreEqual(2, _inv.Rows);
            Assert.AreEqual(6, _inv.CellCount);
            Assert.AreEqual(0, _inv.Items.Count);
        }

        [Test]
        public void Constructor_RejectsNonPositiveSizes()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerInventoryModel(0, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerInventoryModel(2, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerInventoryModel(-1, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerInventoryModel(2, -1));
        }

        [Test]
        public void TryAddAt_EmptyCell_Succeeds()
        {
            var a = NewItem("a");

            Assert.IsTrue(_inv.TryAddAt(a, 4));

            Assert.AreSame(a, _inv.GetAt(4));
            Assert.AreEqual(4, _inv.IndexOf(a));
            Assert.AreEqual(1, _changes);
        }

        [Test]
        public void TryAddAt_OccupiedByOther_FailsWithoutChange()
        {
            var a = NewItem("a");
            var b = NewItem("b");
            _inv.TryAddAt(a, 1);
            _changes = 0;

            Assert.IsFalse(_inv.TryAddAt(b, 1));

            Assert.AreSame(a, _inv.GetAt(1));
            Assert.AreEqual(-1, _inv.IndexOf(b));
            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void TryAddAt_OutOfRange_Fails()
        {
            var a = NewItem("a");

            Assert.IsFalse(_inv.TryAddAt(a, -1));
            Assert.IsFalse(_inv.TryAddAt(a, 6));
            Assert.IsFalse(_inv.TryAddAt(a, 100));

            Assert.AreEqual(-1, _inv.IndexOf(a));
            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void TryAddAt_NullItem_Fails()
        {
            Assert.IsFalse(_inv.TryAddAt(null, 0));
            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void TryAddAt_AlreadyStored_MovesItem()
        {
            var a = NewItem("a");
            _inv.TryAddAt(a, 0);
            _changes = 0;

            Assert.IsTrue(_inv.TryAddAt(a, 5));

            Assert.IsNull(_inv.GetAt(0));
            Assert.AreSame(a, _inv.GetAt(5));
            Assert.AreEqual(5, _inv.IndexOf(a));
            Assert.AreEqual(1, _inv.Items.Count);
            Assert.AreEqual(1, _changes);
        }

        [Test]
        public void TryAddAt_MoveOntoOtherItem_FailsAndKeepsBoth()
        {
            var a = NewItem("a");
            var b = NewItem("b");
            _inv.TryAddAt(a, 0);
            _inv.TryAddAt(b, 1);
            _changes = 0;

            Assert.IsFalse(_inv.TryAddAt(a, 1));

            Assert.AreEqual(0, _inv.IndexOf(a));
            Assert.AreEqual(1, _inv.IndexOf(b));
            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void TryAddAt_OntoOwnCell_SucceedsWithoutChangeEvent()
        {
            var a = NewItem("a");
            _inv.TryAddAt(a, 2);
            _changes = 0;

            Assert.IsTrue(_inv.TryAddAt(a, 2));

            Assert.AreEqual(2, _inv.IndexOf(a));
            Assert.AreEqual(1, _inv.Items.Count);
            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void TryAddFirstFree_FillsCellsInOrder()
        {
            var a = NewItem("a");
            var b = NewItem("b");
            int ia, ib;

            Assert.IsTrue(_inv.TryAddFirstFree(a, out ia));
            Assert.IsTrue(_inv.TryAddFirstFree(b, out ib));

            Assert.AreEqual(0, ia);
            Assert.AreEqual(1, ib);
            Assert.AreEqual(2, _changes);
        }

        [Test]
        public void TryAddFirstFree_SkipsOccupiedCells()
        {
            var a = NewItem("a");
            var b = NewItem("b");
            _inv.TryAddAt(a, 0);
            _changes = 0;
            int index;

            Assert.IsTrue(_inv.TryAddFirstFree(b, out index));

            Assert.AreEqual(1, index);
            Assert.AreEqual(1, _changes);
        }

        [Test]
        public void TryAddFirstFree_ReusesHoleLeftByRemove()
        {
            var a = NewItem("a");
            var b = NewItem("b");
            var c = NewItem("c");
            int index;
            _inv.TryAddFirstFree(a, out index);
            _inv.TryAddFirstFree(b, out index);
            _inv.Remove(a);

            Assert.IsTrue(_inv.TryAddFirstFree(c, out index));

            Assert.AreEqual(0, index);
        }

        [Test]
        public void TryAddFirstFree_WhenFull_FailsWithoutChange()
        {
            int index;
            for (int i = 0; i < 6; i++)
                Assert.IsTrue(_inv.TryAddFirstFree(NewItem("i" + i), out index));
            _changes = 0;

            var extra = NewItem("extra");
            Assert.IsFalse(_inv.TryAddFirstFree(extra, out index));

            Assert.AreEqual(-1, index);
            Assert.AreEqual(-1, _inv.IndexOf(extra));
            Assert.AreEqual(6, _inv.Items.Count);
            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void TryAddFirstFree_NullItem_Fails()
        {
            int index;

            Assert.IsFalse(_inv.TryAddFirstFree(null, out index));

            Assert.AreEqual(-1, index);
            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void TryAddFirstFree_AlreadyStoredLater_MovesToFirstFreeCell()
        {
            var a = NewItem("a");
            _inv.TryAddAt(a, 4);
            _changes = 0;
            int index;

            Assert.IsTrue(_inv.TryAddFirstFree(a, out index));

            Assert.AreEqual(0, index);
            Assert.AreEqual(0, _inv.IndexOf(a));
            Assert.IsNull(_inv.GetAt(4));
            Assert.AreEqual(1, _inv.Items.Count);
            Assert.AreEqual(1, _changes);
        }

        [Test]
        public void TryAddFirstFree_AlreadyInFirstFreeCell_StaysWithoutChange()
        {
            var a = NewItem("a");
            _inv.TryAddAt(a, 0);
            _changes = 0;
            int index;

            Assert.IsTrue(_inv.TryAddFirstFree(a, out index));

            Assert.AreEqual(0, index);
            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void Remove_StoredItem_Succeeds()
        {
            var a = NewItem("a");
            _inv.TryAddAt(a, 3);
            _changes = 0;

            Assert.IsTrue(_inv.Remove(a));

            Assert.IsNull(_inv.GetAt(3));
            Assert.AreEqual(-1, _inv.IndexOf(a));
            Assert.AreEqual(0, _inv.Items.Count);
            Assert.AreEqual(1, _changes);
        }

        [Test]
        public void Remove_UnknownItem_FailsWithoutChange()
        {
            var a = NewItem("a");

            Assert.IsFalse(_inv.Remove(a));
            Assert.IsFalse(_inv.Remove(null));

            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void Remove_Twice_SecondFails()
        {
            var a = NewItem("a");
            _inv.TryAddAt(a, 0);

            Assert.IsTrue(_inv.Remove(a));
            Assert.IsFalse(_inv.Remove(a));
            Assert.AreEqual(2, _changes); // add + first remove only
        }

        [Test]
        public void GetAt_OutOfRange_ReturnsNull()
        {
            Assert.IsNull(_inv.GetAt(-1));
            Assert.IsNull(_inv.GetAt(6));
            Assert.IsNull(_inv.GetAt(int.MaxValue));
        }

        [Test]
        public void GetAt_EmptyCell_ReturnsNull()
        {
            Assert.IsNull(_inv.GetAt(0));
        }

        [Test]
        public void IndexOf_AbsentOrNull_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, _inv.IndexOf(NewItem("a")));
            Assert.AreEqual(-1, _inv.IndexOf(null));
        }

        [Test]
        public void SameDataDifferentInstances_AreDistinct()
        {
            var data = ItemData.Create("twin", "twin", Color.white, "#");
            _created.Add(data);
            var first = new ItemInstance(data);
            var second = new ItemInstance(data);

            Assert.IsTrue(_inv.TryAddAt(first, 0));
            Assert.IsTrue(_inv.TryAddAt(second, 1));

            Assert.AreEqual(0, _inv.IndexOf(first));
            Assert.AreEqual(1, _inv.IndexOf(second));
            Assert.AreEqual(2, _inv.Items.Count);
        }

        [Test]
        public void ItemShape_DoesNotAffectCellUsage()
        {
            var big = NewItem("big", "###", "###");

            Assert.IsTrue(_inv.TryAddAt(big, 0));
            Assert.IsTrue(_inv.TryAddAt(NewItem("small"), 1));
            Assert.AreEqual(2, _inv.Items.Count);
        }

        [Test]
        public void Items_ContainsExactlyStoredItems()
        {
            var a = NewItem("a");
            var b = NewItem("b");
            _inv.TryAddAt(a, 5);
            _inv.TryAddAt(b, 2);

            CollectionAssert.AreEquivalent(new[] { a, b }, _inv.Items);

            _inv.Remove(a);
            CollectionAssert.AreEquivalent(new[] { b }, _inv.Items);
        }

        [Test]
        public void Items_PreviouslyObtainedSnapshotIsNotMutated()
        {
            var a = NewItem("a");
            _inv.TryAddAt(a, 0);
            var snapshot = _inv.Items;
            Assert.AreEqual(1, snapshot.Count);

            _inv.TryAddAt(NewItem("b"), 1);

            Assert.AreEqual(1, snapshot.Count);
            Assert.AreEqual(2, _inv.Items.Count);
        }

        [Test]
        public void Items_IsStableBetweenChanges()
        {
            _inv.TryAddAt(NewItem("a"), 0);

            Assert.AreSame(_inv.Items, _inv.Items);
        }

        [Test]
        public void Items_CannotBeCastToMutableList()
        {
            _inv.TryAddAt(NewItem("a"), 0);

            Assert.IsNull(_inv.Items as List<ItemInstance>);
            Assert.IsNull(_inv.Items as ItemInstance[]);
        }

        [Test]
        public void Changed_FiresOncePerSuccessfulMutation()
        {
            var a = NewItem("a");
            int index;

            _inv.TryAddAt(a, 0);            // 1
            _inv.TryAddAt(a, 3);            // 2 (move)
            _inv.Remove(a);                 // 3
            _inv.TryAddFirstFree(a, out index); // 4

            Assert.AreEqual(4, _changes);
        }

        [Test]
        public void Changed_HandlerSeesUpdatedState()
        {
            var a = NewItem("a");
            int indexSeenInHandler = -2;
            _inv.Changed += () => indexSeenInHandler = _inv.IndexOf(a);

            _inv.TryAddAt(a, 4);

            Assert.AreEqual(4, indexSeenInHandler);
        }
    }
}
