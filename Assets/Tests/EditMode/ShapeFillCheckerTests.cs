using System.Collections.Generic;
using NUnit.Framework;
using ShapeInventory.Core;
using UnityEngine;

namespace ShapeInventory.Tests
{
    /// <summary>
    /// ShapeFillChecker is a placeholder that currently always reports "filled".
    /// These tests pin that stub behaviour; replace them when the real logic is implemented.
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
    }
}
