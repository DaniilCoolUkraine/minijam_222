using System;
using System.Collections.Generic;
using NUnit.Framework;
using ShapeInventory.Core;
using UnityEngine;

namespace ShapeInventory.Tests
{
    public class ObjectInventoryModelTests
    {
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();
        private int _changes;

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created)
            {
                if (o != null) UnityEngine.Object.DestroyImmediate(o);
            }

            _created.Clear();
        }

        private ObjectInventoryModel NewModel(params string[] silhouetteRows)
        {
            var data = ObjectData.Create("obj", "obj", Color.gray, silhouetteRows);
            _created.Add(data);
            var model = new ObjectInventoryModel(data);
            _changes = 0;
            model.Changed += () => _changes++;
            return model;
        }

        /// <summary>
        /// L-shaped silhouette, 2 wide x 3 high:
        /// <code>
        /// #.
        /// #.
        /// ##
        /// </code>
        /// </summary>
        private ObjectInventoryModel NewLModel()
        {
            return NewModel("#.", "#.", "##");
        }

        private ItemInstance NewItem(string id, params string[] rows)
        {
            var data = ItemData.Create(id, id, Color.white, rows);
            _created.Add(data);
            return new ItemInstance(data);
        }

        private static Vector2Int V(int x, int y)
        {
            return new Vector2Int(x, y);
        }

        // ---------------------------------------------------------------- construction / silhouette

        [Test]
        public void Constructor_NullDataThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new ObjectInventoryModel(null));
        }

        [Test]
        public void Dimensions_MatchSilhouetteBoundingBox()
        {
            var model = NewLModel();

            Assert.AreEqual(2, model.Width);
            Assert.AreEqual(3, model.Height);
            Assert.AreEqual(0, model.PlacedItems.Count);
            Assert.IsNotNull(model.Data);
        }

        [Test]
        public void IsSilhouetteCell_MatchesLShape()
        {
            var model = NewLModel();

            Assert.IsTrue(model.IsSilhouetteCell(V(0, 0)));
            Assert.IsTrue(model.IsSilhouetteCell(V(0, 1)));
            Assert.IsTrue(model.IsSilhouetteCell(V(0, 2)));
            Assert.IsTrue(model.IsSilhouetteCell(V(1, 2)));
            Assert.IsFalse(model.IsSilhouetteCell(V(1, 0)));
            Assert.IsFalse(model.IsSilhouetteCell(V(1, 1)));
        }

        [Test]
        public void IsSilhouetteCell_OutOfBounds_IsFalse()
        {
            var model = NewLModel();

            Assert.IsFalse(model.IsSilhouetteCell(V(-1, 0)));
            Assert.IsFalse(model.IsSilhouetteCell(V(0, -1)));
            Assert.IsFalse(model.IsSilhouetteCell(V(2, 2)));
            Assert.IsFalse(model.IsSilhouetteCell(V(0, 3)));
            Assert.IsFalse(model.IsSilhouetteCell(V(100, 100)));
        }

        // ---------------------------------------------------------------- CanPlace

        [Test]
        public void CanPlace_NullItem_IsFalse()
        {
            var model = NewLModel();

            Assert.IsFalse(model.CanPlace(null, V(0, 0)));
            Assert.IsFalse(model.TryPlace(null, V(0, 0)));
            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void CanPlace_SingleCellOnEverySilhouetteCell_IsTrue()
        {
            var model = NewLModel();
            var dot = NewItem("dot", "#");

            Assert.IsTrue(model.CanPlace(dot, V(0, 0)));
            Assert.IsTrue(model.CanPlace(dot, V(0, 1)));
            Assert.IsTrue(model.CanPlace(dot, V(0, 2)));
            Assert.IsTrue(model.CanPlace(dot, V(1, 2)));
        }

        [Test]
        public void CanPlace_CellOutsideSilhouetteHole_IsFalse()
        {
            var model = NewLModel();
            var dot = NewItem("dot", "#");

            Assert.IsFalse(model.CanPlace(dot, V(1, 0)));
            Assert.IsFalse(model.CanPlace(dot, V(1, 1)));
        }

        [Test]
        public void CanPlace_DoesNotMutate()
        {
            var model = NewLModel();
            var dot = NewItem("dot", "#");

            model.CanPlace(dot, V(0, 0));

            Assert.AreEqual(0, model.PlacedItems.Count);
            Assert.IsNull(model.GetItemAt(V(0, 0)));
            Assert.AreEqual(0, _changes);
        }

        // ---------------------------------------------------------------- TryPlace: silhouette

        [Test]
        public void TryPlace_FittingDomino_Succeeds()
        {
            var model = NewLModel();
            var domino = NewItem("domino", "##");

            Assert.IsTrue(model.TryPlace(domino, V(0, 2)));

            Assert.AreSame(domino, model.GetItemAt(V(0, 2)));
            Assert.AreSame(domino, model.GetItemAt(V(1, 2)));
            Assert.AreEqual(1, model.PlacedItems.Count);
            Assert.AreEqual(1, _changes);
        }

        [Test]
        public void TryPlace_PartlyOutsideSilhouette_IsRejected()
        {
            var model = NewLModel();
            var domino = NewItem("domino", "##");

            Assert.IsFalse(model.TryPlace(domino, V(0, 0))); // (1,0) is a hole
            Assert.IsFalse(model.TryPlace(domino, V(1, 2))); // (2,2) is out of bounds
            Assert.IsFalse(model.TryPlace(domino, V(0, 1))); // (1,1) is a hole

            Assert.AreEqual(0, model.PlacedItems.Count);
            Assert.IsNull(model.GetItemAt(V(0, 0)));
            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void TryPlace_NegativeOrigin_IsRejected()
        {
            var model = NewLModel();
            var dot = NewItem("dot", "#");

            Assert.IsFalse(model.TryPlace(dot, V(-1, 0)));
            Assert.IsFalse(model.TryPlace(dot, V(0, -1)));
            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void TryPlace_ItemLargerThanSilhouette_IsRejected()
        {
            var model = NewLModel();
            var big = NewItem("big", "####", "####");

            Assert.IsFalse(model.TryPlace(big, V(0, 0)));
            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void TryPlace_LShapedItemIntoLSilhouette_FitsExactly()
        {
            var model = NewLModel();
            var column = NewItem("column", "#", "#");
            var foot = NewItem("foot", "##");

            // silhouette: column occupies (0,0),(0,1); foot occupies (0,2),(1,2)
            Assert.IsTrue(model.TryPlace(column, V(0, 0)));
            Assert.IsTrue(model.TryPlace(foot, V(0, 2)));
            Assert.AreEqual(2, model.PlacedItems.Count);
            Assert.AreEqual(2, _changes);
        }

        [Test]
        public void TryPlace_ItemShapeWithOffsetCells_UsesNormalisedOrigin()
        {
            // Rows with leading empty column/row are normalised, so origin is the top-left filled cell.
            var model = NewModel("###", "###");
            var item = NewItem("offset", "...", ".##");

            Assert.IsTrue(model.TryPlace(item, V(0, 0)));

            Assert.AreSame(item, model.GetItemAt(V(0, 0)));
            Assert.AreSame(item, model.GetItemAt(V(1, 0)));
            Assert.IsNull(model.GetItemAt(V(2, 0)));
        }

        // ---------------------------------------------------------------- TryPlace: overlap

        [Test]
        public void TryPlace_OverlappingOtherItem_IsRejected()
        {
            var model = NewLModel();
            var dot = NewItem("dot", "#");
            var column = NewItem("column3", "#", "#", "#");
            model.TryPlace(dot, V(0, 2));
            _changes = 0;

            Assert.IsFalse(model.CanPlace(column, V(0, 0)));
            Assert.IsFalse(model.TryPlace(column, V(0, 0)));

            Assert.AreEqual(1, model.PlacedItems.Count);
            Assert.AreSame(dot, model.GetItemAt(V(0, 2)));
            Assert.IsNull(model.GetItemAt(V(0, 0)));
            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void TryPlace_AdjacentItems_AreAllowed()
        {
            var model = NewModel("####");
            var a = NewItem("a", "##");
            var b = NewItem("b", "##");

            Assert.IsTrue(model.TryPlace(a, V(0, 0)));
            Assert.IsTrue(model.TryPlace(b, V(2, 0)));
            Assert.AreSame(a, model.GetItemAt(V(1, 0)));
            Assert.AreSame(b, model.GetItemAt(V(2, 0)));
        }

        [Test]
        public void TryPlace_SameDataDifferentInstances_CannotOverlap()
        {
            var model = NewModel("##");
            var data = ItemData.Create("twin", "twin", Color.white, "#");
            _created.Add(data);
            var first = new ItemInstance(data);
            var second = new ItemInstance(data);

            Assert.IsTrue(model.TryPlace(first, V(0, 0)));
            Assert.IsFalse(model.TryPlace(second, V(0, 0)));
            Assert.IsTrue(model.TryPlace(second, V(1, 0)));
        }

        // ---------------------------------------------------------------- move / nudge

        [Test]
        public void TryPlace_AlreadyPlaced_MovesInsteadOfDuplicating()
        {
            var model = NewModel("####", "####");
            var domino = NewItem("domino", "##");
            model.TryPlace(domino, V(0, 0));
            _changes = 0;

            Assert.IsTrue(model.TryPlace(domino, V(0, 1)));

            Assert.AreEqual(1, model.PlacedItems.Count);
            Assert.IsNull(model.GetItemAt(V(0, 0)));
            Assert.IsNull(model.GetItemAt(V(1, 0)));
            Assert.AreSame(domino, model.GetItemAt(V(0, 1)));
            Assert.AreSame(domino, model.GetItemAt(V(1, 1)));
            Assert.AreEqual(1, _changes);
        }

        [Test]
        public void TryPlace_NudgeOverlappingOwnCells_IsAllowed()
        {
            var model = NewModel("####", "####");
            var domino = NewItem("domino", "##");
            model.TryPlace(domino, V(0, 0));
            _changes = 0;

            Assert.IsTrue(model.CanPlace(domino, V(1, 0)));
            Assert.IsTrue(model.TryPlace(domino, V(1, 0)));

            Assert.IsNull(model.GetItemAt(V(0, 0)));
            Assert.AreSame(domino, model.GetItemAt(V(1, 0)));
            Assert.AreSame(domino, model.GetItemAt(V(2, 0)));
            Assert.AreEqual(1, model.PlacedItems.Count);
            Assert.AreEqual(1, _changes);
        }

        [Test]
        public void TryPlace_MoveToSameOrigin_SucceedsWithoutChangeEvent()
        {
            var model = NewModel("####");
            var domino = NewItem("domino", "##");
            model.TryPlace(domino, V(1, 0));
            _changes = 0;

            Assert.IsTrue(model.CanPlace(domino, V(1, 0)));
            Assert.IsTrue(model.TryPlace(domino, V(1, 0)));

            Assert.AreEqual(1, model.PlacedItems.Count);
            Assert.AreSame(domino, model.GetItemAt(V(1, 0)));
            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void TryPlace_MoveOntoOtherItem_FailsAndKeepsOldPlacement()
        {
            var model = NewModel("####");
            var a = NewItem("a", "#");
            var b = NewItem("b", "#");
            model.TryPlace(a, V(0, 0));
            model.TryPlace(b, V(1, 0));
            _changes = 0;

            Assert.IsFalse(model.TryPlace(a, V(1, 0)));

            PlacedItem placed;
            Assert.IsTrue(model.TryGetPlaced(a, out placed));
            Assert.AreEqual(V(0, 0), placed.Origin);
            Assert.AreSame(a, model.GetItemAt(V(0, 0)));
            Assert.AreSame(b, model.GetItemAt(V(1, 0)));
            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void TryPlace_MoveOutsideSilhouette_FailsAndKeepsOldPlacement()
        {
            var model = NewLModel();
            var domino = NewItem("domino", "##");
            model.TryPlace(domino, V(0, 2));
            _changes = 0;

            Assert.IsFalse(model.TryPlace(domino, V(0, 0)));

            PlacedItem placed;
            Assert.IsTrue(model.TryGetPlaced(domino, out placed));
            Assert.AreEqual(V(0, 2), placed.Origin);
            Assert.AreSame(domino, model.GetItemAt(V(1, 2)));
            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void TryPlace_MoveFreesOldCellsForOtherItems()
        {
            var model = NewModel("###");
            var a = NewItem("a", "#");
            var b = NewItem("b", "#");
            model.TryPlace(a, V(0, 0));
            model.TryPlace(a, V(2, 0));

            Assert.IsTrue(model.TryPlace(b, V(0, 0)));
            Assert.AreSame(b, model.GetItemAt(V(0, 0)));
            Assert.AreSame(a, model.GetItemAt(V(2, 0)));
        }

        // ---------------------------------------------------------------- remove

        [Test]
        public void Remove_PlacedItem_FreesCells()
        {
            var model = NewLModel();
            var domino = NewItem("domino", "##");
            model.TryPlace(domino, V(0, 2));
            _changes = 0;

            Assert.IsTrue(model.Remove(domino));

            Assert.AreEqual(0, model.PlacedItems.Count);
            Assert.IsNull(model.GetItemAt(V(0, 2)));
            Assert.IsNull(model.GetItemAt(V(1, 2)));
            Assert.AreEqual(1, _changes);
        }

        [Test]
        public void Remove_NotPlaced_FailsWithoutChange()
        {
            var model = NewLModel();

            Assert.IsFalse(model.Remove(NewItem("dot", "#")));
            Assert.IsFalse(model.Remove(null));
            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void Remove_Twice_SecondFails()
        {
            var model = NewLModel();
            var dot = NewItem("dot", "#");
            model.TryPlace(dot, V(0, 0));

            Assert.IsTrue(model.Remove(dot));
            Assert.IsFalse(model.Remove(dot));
            Assert.AreEqual(2, _changes);
        }

        [Test]
        public void RemoveThenPlace_SamePositionWorksAgain()
        {
            var model = NewLModel();
            var dot = NewItem("dot", "#");
            var other = NewItem("other", "#");
            model.TryPlace(dot, V(0, 0));
            Assert.IsFalse(model.TryPlace(other, V(0, 0)));

            model.Remove(dot);

            Assert.IsTrue(model.TryPlace(other, V(0, 0)));
            Assert.AreSame(other, model.GetItemAt(V(0, 0)));
            Assert.AreEqual(1, model.PlacedItems.Count);
        }

        [Test]
        public void RemoveThenPlace_RemovedItemCanBePlacedElsewhere()
        {
            var model = NewLModel();
            var dot = NewItem("dot", "#");
            model.TryPlace(dot, V(0, 0));
            model.Remove(dot);

            Assert.IsTrue(model.TryPlace(dot, V(1, 2)));

            PlacedItem placed;
            Assert.IsTrue(model.TryGetPlaced(dot, out placed));
            Assert.AreEqual(V(1, 2), placed.Origin);
            Assert.IsNull(model.GetItemAt(V(0, 0)));
        }

        // ---------------------------------------------------------------- queries

        [Test]
        public void TryGetPlaced_ReturnsItemAndOrigin()
        {
            var model = NewLModel();
            var domino = NewItem("domino", "##");
            model.TryPlace(domino, V(0, 2));

            PlacedItem placed;
            Assert.IsTrue(model.TryGetPlaced(domino, out placed));

            Assert.AreSame(domino, placed.Item);
            Assert.AreEqual(V(0, 2), placed.Origin);
        }

        [Test]
        public void TryGetPlaced_UnknownOrNull_ReturnsFalse()
        {
            var model = NewLModel();

            PlacedItem placed;
            Assert.IsFalse(model.TryGetPlaced(NewItem("dot", "#"), out placed));
            Assert.IsFalse(model.TryGetPlaced(null, out placed));
        }

        [Test]
        public void GetItemAt_ReturnsItemForEveryCoveredCell()
        {
            var model = NewModel("###", "###");
            var l = NewItem("l", "##", "#.");
            model.TryPlace(l, V(1, 0));

            Assert.AreSame(l, model.GetItemAt(V(1, 0)));
            Assert.AreSame(l, model.GetItemAt(V(2, 0)));
            Assert.AreSame(l, model.GetItemAt(V(1, 1)));
            Assert.IsNull(model.GetItemAt(V(2, 1)));
            Assert.IsNull(model.GetItemAt(V(0, 0)));
        }

        [Test]
        public void GetItemAt_OutOfBoundsOrEmpty_ReturnsNull()
        {
            var model = NewLModel();

            Assert.IsNull(model.GetItemAt(V(0, 0)));
            Assert.IsNull(model.GetItemAt(V(-5, -5)));
            Assert.IsNull(model.GetItemAt(V(50, 50)));
        }

        // ---------------------------------------------------------------- PlacedItems / Changed

        [Test]
        public void PlacedItems_ReflectsCurrentState()
        {
            var model = NewModel("####", "####");
            var a = NewItem("a", "#");
            var b = NewItem("b", "##");

            model.TryPlace(a, V(0, 0));
            model.TryPlace(b, V(1, 1));
            Assert.AreEqual(2, model.PlacedItems.Count);

            model.TryPlace(a, V(3, 0)); // move
            Assert.AreEqual(2, model.PlacedItems.Count);
            var found = false;
            foreach (var p in model.PlacedItems)
            {
                if (p.Item == a)
                {
                    found = true;
                    Assert.AreEqual(V(3, 0), p.Origin);
                }
            }

            Assert.IsTrue(found);

            model.Remove(b);
            Assert.AreEqual(1, model.PlacedItems.Count);
            Assert.AreSame(a, model.PlacedItems[0].Item);
        }

        [Test]
        public void PlacedItems_OccupiedCellsAreAbsolute()
        {
            var model = NewModel("###", "###");
            var l = NewItem("l", "##", "#.");
            model.TryPlace(l, V(1, 0));

            var cells = new List<Vector2Int>(model.PlacedItems[0].OccupiedCells());

            CollectionAssert.AreEquivalent(new[] { V(1, 0), V(2, 0), V(1, 1) }, cells);
        }

        [Test]
        public void PlacedItems_PreviouslyObtainedSnapshotIsNotMutated()
        {
            var model = NewModel("###");
            model.TryPlace(NewItem("a", "#"), V(0, 0));
            var snapshot = model.PlacedItems;

            model.TryPlace(NewItem("b", "#"), V(1, 0));

            Assert.AreEqual(1, snapshot.Count);
            Assert.AreEqual(2, model.PlacedItems.Count);
        }

        [Test]
        public void Changed_FiresOncePerSuccessfulMutationOnly()
        {
            var model = NewModel("####");
            var a = NewItem("a", "##");
            var blocker = NewItem("blocker", "#");

            model.TryPlace(a, V(0, 0));        // 1 place
            model.TryPlace(blocker, V(0, 0));  // rejected
            model.TryPlace(a, V(1, 0));        // 2 nudge/move
            model.TryPlace(a, V(3, 0));        // rejected (out of range)
            model.TryPlace(blocker, V(0, 0));  // 3 place
            model.Remove(a);                   // 4 remove
            model.Remove(a);                   // rejected

            Assert.AreEqual(4, _changes);
        }

        [Test]
        public void Changed_HandlerSeesUpdatedState()
        {
            var data = ObjectData.Create("obj", "obj", Color.gray, "##");
            _created.Add(data);
            var model = new ObjectInventoryModel(data);
            var dot = NewItem("dot", "#");
            ItemInstance seen = null;
            model.Changed += () => seen = model.GetItemAt(V(1, 0));

            model.TryPlace(dot, V(1, 0));

            Assert.AreSame(dot, seen);
        }
    }
}
