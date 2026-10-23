using System;
using System.Collections.Generic;
using NUnit.Framework;
using ShapeInventory.Core;
using UnityEngine;

namespace ShapeInventory.Tests
{
    public class ItemShapeTests
    {
        [Test]
        public void FromRows_BuildsLTromino()
        {
            var shape = ItemShape.FromRows(new[] { "##", "#." });

            Assert.AreEqual(3, shape.CellCount);
            Assert.AreEqual(2, shape.Width);
            Assert.AreEqual(2, shape.Height);
            Assert.IsTrue(shape.Contains(new Vector2Int(0, 0)));
            Assert.IsTrue(shape.Contains(new Vector2Int(1, 0)));
            Assert.IsTrue(shape.Contains(new Vector2Int(0, 1)));
            Assert.IsFalse(shape.Contains(new Vector2Int(1, 1)));
        }

        [Test]
        public void FromRows_AcceptsAllFilledCharacters()
        {
            var shape = ItemShape.FromRows(new[] { "#Xx1." });

            Assert.AreEqual(4, shape.CellCount);
            Assert.AreEqual(4, shape.Width);
            Assert.AreEqual(1, shape.Height);
        }

        [Test]
        public void FromRows_TreatsUnknownCharactersAsEmpty()
        {
            var shape = ItemShape.FromRows(new[] { "#?#", "-#-" });

            Assert.AreEqual(3, shape.CellCount);
            Assert.IsFalse(shape.Contains(new Vector2Int(1, 0)));
        }

        [Test]
        public void FromRows_TrimsLeadingEmptyRowsAndColumns()
        {
            var shape = ItemShape.FromRows(new[] { "...", ".##", ".#." });

            Assert.AreEqual(3, shape.CellCount);
            Assert.AreEqual(2, shape.Width);
            Assert.AreEqual(2, shape.Height);
            Assert.IsTrue(shape.Contains(new Vector2Int(0, 0)));
            Assert.IsTrue(shape.Contains(new Vector2Int(1, 0)));
            Assert.IsTrue(shape.Contains(new Vector2Int(0, 1)));
        }

        [Test]
        public void FromRows_NullRowIsEmptyRow()
        {
            var shape = ItemShape.FromRows(new string[] { null, "#" });

            Assert.AreEqual(1, shape.CellCount);
            Assert.AreEqual(new Vector2Int(0, 0), shape.Cells[0]);
        }

        [Test]
        public void FromRows_NoFilledCellsThrows()
        {
            Assert.Throws<ArgumentException>(() => ItemShape.FromRows(new[] { "...", "..." }));
        }

        [Test]
        public void FromRows_NullThrows()
        {
            Assert.Throws<ArgumentNullException>(() => ItemShape.FromRows(null));
        }

        [Test]
        public void Constructor_NormalisesOffsetCells()
        {
            var shape = new ItemShape(new[]
            {
                new Vector2Int(5, 7), new Vector2Int(6, 7), new Vector2Int(5, 8)
            });

            Assert.AreEqual(2, shape.Width);
            Assert.AreEqual(2, shape.Height);
            CollectionAssert.AreEquivalent(
                new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) },
                shape.Cells);
        }

        [Test]
        public void Constructor_NormalisesNegativeCells()
        {
            var shape = new ItemShape(new[] { new Vector2Int(-3, -2), new Vector2Int(-2, -2) });

            Assert.AreEqual(2, shape.Width);
            Assert.AreEqual(1, shape.Height);
            CollectionAssert.AreEquivalent(new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) }, shape.Cells);
        }

        [Test]
        public void Constructor_RemovesDuplicateCells()
        {
            var shape = new ItemShape(new[] { new Vector2Int(2, 2), new Vector2Int(2, 2) });

            Assert.AreEqual(1, shape.CellCount);
            Assert.AreEqual(1, shape.Width);
            Assert.AreEqual(1, shape.Height);
        }

        [Test]
        public void Constructor_EmptyThrows()
        {
            Assert.Throws<ArgumentException>(() => new ItemShape(new List<Vector2Int>()));
        }

        [Test]
        public void Constructor_NullThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new ItemShape(null));
        }

        [Test]
        public void Cells_AreSortedRowByRow()
        {
            var shape = ItemShape.FromRows(new[] { ".#", "##" });

            CollectionAssert.AreEqual(
                new[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
                shape.Cells);
        }

        [Test]
        public void WidthAndHeight_ForStraightPieces()
        {
            var horizontal = ItemShape.FromRows(new[] { "####" });
            var vertical = ItemShape.FromRows(new[] { "#", "#", "#" });

            Assert.AreEqual(4, horizontal.Width);
            Assert.AreEqual(1, horizontal.Height);
            Assert.AreEqual(1, vertical.Width);
            Assert.AreEqual(3, vertical.Height);
        }

        [Test]
        public void GetCenterCell_SingleCell()
        {
            var shape = ItemShape.FromRows(new[] { "#" });

            Assert.AreEqual(new Vector2Int(0, 0), shape.GetCenterCell());
        }

        [Test]
        public void GetCenterCell_OddLine_IsMiddleCell()
        {
            var shape = ItemShape.FromRows(new[] { "###" });

            Assert.AreEqual(new Vector2Int(1, 0), shape.GetCenterCell());
        }

        [Test]
        public void GetCenterCell_Plus_IsCentre()
        {
            var shape = ItemShape.FromRows(new[] { ".#.", "###", ".#." });

            Assert.AreEqual(new Vector2Int(1, 1), shape.GetCenterCell());
        }

        [Test]
        public void GetCenterCell_AlwaysReturnsCellInsideShape()
        {
            var shapes = new[]
            {
                ItemShape.FromRows(new[] { "##", "#." }),
                ItemShape.FromRows(new[] { "#..", "#..", "###" }),
                ItemShape.FromRows(new[] { "####" }),
                ItemShape.FromRows(new[] { "#.#", "###" }),
                ItemShape.FromRows(new[] { "#", "#", "#", "#" })
            };

            foreach (var shape in shapes)
                Assert.IsTrue(shape.Contains(shape.GetCenterCell()));
        }

        [Test]
        public void GetNearestCell_CellInsideShape_ReturnsItself()
        {
            var shape = ItemShape.FromRows(new[] { "##", "#." });

            Assert.AreEqual(new Vector2Int(1, 0), shape.GetNearestCell(new Vector2Int(1, 0)));
        }

        [Test]
        public void GetNearestCell_FarRight_ReturnsRightmostCell()
        {
            var shape = ItemShape.FromRows(new[] { "###" });

            Assert.AreEqual(new Vector2Int(2, 0), shape.GetNearestCell(new Vector2Int(9, 0)));
        }

        [Test]
        public void GetNearestCell_FarLeft_ReturnsLeftmostCell()
        {
            var shape = ItemShape.FromRows(new[] { "###" });

            Assert.AreEqual(new Vector2Int(0, 0), shape.GetNearestCell(new Vector2Int(-4, 0)));
        }

        [Test]
        public void GetNearestCell_EmptyBoundingBoxCorner_ReturnsAdjacentCell()
        {
            // L shape: (1,1) is empty. Nearest cells at distance 1 are (1,0) and (0,1); either is acceptable.
            var shape = ItemShape.FromRows(new[] { "##", "#." });

            var nearest = shape.GetNearestCell(new Vector2Int(1, 1));

            Assert.IsTrue(shape.Contains(nearest));
            Assert.IsTrue(nearest == new Vector2Int(1, 0) || nearest == new Vector2Int(0, 1));
        }

        [Test]
        public void GetNearestCell_BelowTallShape_ReturnsBottomCell()
        {
            var shape = ItemShape.FromRows(new[] { "#", "#", "#" });

            Assert.AreEqual(new Vector2Int(0, 2), shape.GetNearestCell(new Vector2Int(0, 8)));
        }
    }
}
