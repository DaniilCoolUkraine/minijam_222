using System.Collections.Generic;
using NUnit.Framework;
using ShapeInventory.Core;
using UnityEngine;

namespace ShapeInventory.Tests
{
    /// <summary>
    /// The IObjectInventory overload is the real check (every silhouette cell covered); the IReadOnlyList overload is
    /// the legacy placeholder that always reports "filled".
    /// </summary>
    public class ShapeFillCheckerTests
    {
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created)
            {
                if (o != null) UnityEngine.Object.DestroyImmediate(o);
            }

            _created.Clear();
        }

        [Test]
        public void Stub_EmptyList_IsFilled()
        {
            var result = ShapeFillChecker.IsShapeFilled(new List<PlacedItem>());

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsFilled);
        }

        [Test]
        public void Stub_NonEmptyList_IsFilled()
        {
            var data = ItemData.Create("dot", "dot", Color.white, "#");
            _created.Add(data);
            var items = new List<PlacedItem>
            {
                new PlacedItem(new ItemInstance(data), new Vector2Int(0, 0))
            };

            var result = ShapeFillChecker.IsShapeFilled(items);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsFilled);
        }

        [Test]
        public void Stub_WorksWithModelPlacedItems()
        {
            var objectData = ObjectData.Create("obj", "obj", Color.gray, "##");
            var itemData = ItemData.Create("dot", "dot", Color.white, "#");
            _created.Add(objectData);
            _created.Add(itemData);
            var model = new ObjectInventoryModel(objectData);
            model.TryPlace(new ItemInstance(itemData), new Vector2Int(0, 0));

            var result = ShapeFillChecker.IsShapeFilled(model.PlacedItems);

            Assert.IsTrue(result.IsFilled);
        }

        [Test]
        public void Real_EmptyObject_IsNotFilled()
        {
            var objectData = ObjectData.Create("obj", "obj", Color.gray, "##", "#.");
            _created.Add(objectData);
            var model = new ObjectInventoryModel(objectData);

            Assert.IsFalse(ShapeFillChecker.IsShapeFilled(model).IsFilled);
        }

        [Test]
        public void Real_PartlyCovered_IsNotFilled()
        {
            var objectData = ObjectData.Create("obj", "obj", Color.gray, "##", "#.");
            var domino = ItemData.Create("domino", "domino", Color.white, "##");
            _created.Add(objectData);
            _created.Add(domino);
            var model = new ObjectInventoryModel(objectData);
            Assert.IsTrue(model.TryPlace(new ItemInstance(domino), new Vector2Int(0, 0)));

            Assert.IsFalse(ShapeFillChecker.IsShapeFilled(model).IsFilled);
        }

        [Test]
        public void Real_FullyCoveredWithTwoPieces_IsFilled()
        {
            var objectData = ObjectData.Create("obj", "obj", Color.gray, "##", "#.");
            var domino = ItemData.Create("domino", "domino", Color.white, "##");
            var dot = ItemData.Create("dot", "dot", Color.white, "#");
            _created.Add(objectData);
            _created.Add(domino);
            _created.Add(dot);
            var model = new ObjectInventoryModel(objectData);
            Assert.IsTrue(model.TryPlace(new ItemInstance(domino), new Vector2Int(0, 0)));
            Assert.IsTrue(model.TryPlace(new ItemInstance(dot), new Vector2Int(0, 1)));

            Assert.IsTrue(ShapeFillChecker.IsShapeFilled(model).IsFilled);
        }

        [Test]
        public void Real_NullInventory_IsNotFilled()
        {
            Assert.IsFalse(ShapeFillChecker.IsShapeFilled((IObjectInventory)null).IsFilled);
        }
    }
}
