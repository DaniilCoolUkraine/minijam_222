using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShapeInventory.Core
{
    /// <summary>
    /// Immutable Tetris-like shape. Cells use grid coordinates: x grows to the RIGHT,
    /// y grows DOWNWARD (row 0 is the TOP row). Cells are normalised so that
    /// min x == 0 and min y == 0.
    /// </summary>
    public sealed class ItemShape
    {
        private readonly Vector2Int[] _cells;
        private readonly HashSet<Vector2Int> _set;

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<Vector2Int> Cells => _cells;
        public int CellCount => _cells.Length;

        public ItemShape(IEnumerable<Vector2Int> cells)
        {
            if (cells == null) throw new ArgumentNullException(nameof(cells));

            var unique = new HashSet<Vector2Int>(cells);
            if (unique.Count == 0) throw new ArgumentException("Shape must contain at least one cell.", nameof(cells));

            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var c in unique)
            {
                if (c.x < minX) minX = c.x;
                if (c.y < minY) minY = c.y;
                if (c.x > maxX) maxX = c.x;
                if (c.y > maxY) maxY = c.y;
            }

            _cells = new Vector2Int[unique.Count];
            _set = new HashSet<Vector2Int>();
            int i = 0;
            foreach (var c in unique)
            {
                var n = new Vector2Int(c.x - minX, c.y - minY);
                _cells[i++] = n;
                _set.Add(n);
            }

            Array.Sort(_cells, (a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            Width = maxX - minX + 1;
            Height = maxY - minY + 1;
        }

        public bool Contains(Vector2Int cell) => _set.Contains(cell);

        /// <summary>
        /// Builds a shape from text rows (top row first). '#', 'X', 'x' and '1' are filled; anything else is empty.
        /// Example: new[] { "##", "#." } is an L-tromino.
        /// </summary>
        public static ItemShape FromRows(IReadOnlyList<string> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));

            var cells = new List<Vector2Int>();
            for (int y = 0; y < rows.Count; y++)
            {
                string row = rows[y] ?? string.Empty;
                for (int x = 0; x < row.Length; x++)
                {
                    char ch = row[x];
                    if (ch == '#' || ch == 'X' || ch == 'x' || ch == '1')
                        cells.Add(new Vector2Int(x, y));
                }
            }

            return new ItemShape(cells);
        }

        /// <summary>The shape cell closest to the bounding-box centre. Used as the default drag grab cell.</summary>
        public Vector2Int GetCenterCell()
        {
            var centre = new Vector2((Width - 1) * 0.5f, (Height - 1) * 0.5f);
            var best = _cells[0];
            float bestDist = float.MaxValue;
            foreach (var c in _cells)
            {
                float d = (new Vector2(c.x, c.y) - centre).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c;
                }
            }

            return best;
        }

        /// <summary>The shape cell closest to an arbitrary (possibly out-of-shape) cell coordinate.</summary>
        public Vector2Int GetNearestCell(Vector2Int cell)
        {
            if (_set.Contains(cell)) return cell;

            var best = _cells[0];
            int bestDist = int.MaxValue;
            foreach (var c in _cells)
            {
                int dx = c.x - cell.x, dy = c.y - cell.y;
                int d = dx * dx + dy * dy;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c;
                }
            }

            return best;
        }
    }
}
