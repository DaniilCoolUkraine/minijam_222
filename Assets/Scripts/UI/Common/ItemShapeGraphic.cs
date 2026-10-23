using System.Collections.Generic;
using ShapeInventory.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ShapeInventory.UI.Common
{
    /// <summary>
    /// Renders an item's Tetris shape as a set of tinted cell Images inside this RectTransform
    /// (pivot top-left, y grows downward). Shared by the player inventory, object inventory and drag ghost.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ItemShapeGraphic : MonoBehaviour
    {
        private readonly List<Image> _cellImages = new List<Image>();
        private Image _iconImage;
        private RectTransform _rect;
        private CanvasGroup _group;

        public ItemInstance Item { get; private set; }
        public Vector2 CellSize { get; private set; }

        /// <summary>Whether cell images block raycasts (the drag ghost sets this to false).</summary>
        public bool RaycastTarget { get; private set; } = true;

        public RectTransform Rect => _rect != null ? _rect : (_rect = (RectTransform)transform);

        /// <summary>Builds the visual. cellSize is in canvas units; gap shrinks each cell image to leave a visible border.</summary>
        public void Bind(ItemInstance item, Vector2 cellSize, float gap = 2f, bool raycastTarget = true)
        {
            Clear();
            Item = item;
            CellSize = cellSize;
            RaycastTarget = raycastTarget;
            if (item == null) return;

            var shape = item.Shape;
            var rect = Rect;
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(shape.Width * cellSize.x, shape.Height * cellSize.y);

            foreach (var c in shape.Cells)
            {
                var go = new GameObject($"Cell_{c.x}_{c.y}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(rect, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(c.x * cellSize.x + gap * 0.5f, -c.y * cellSize.y - gap * 0.5f);
                rt.sizeDelta = new Vector2(Mathf.Max(1f, cellSize.x - gap), Mathf.Max(1f, cellSize.y - gap));

                var img = go.GetComponent<Image>();
                img.color = item.Data.Color;
                img.raycastTarget = raycastTarget;
                _cellImages.Add(img);
            }

            if (item.Data.Icon != null)
            {
                var go = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(rect, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(shape.Width * cellSize.x * 0.5f, -shape.Height * cellSize.y * 0.5f);
                float side = Mathf.Min(shape.Width * cellSize.x, shape.Height * cellSize.y) * 0.7f;
                rt.sizeDelta = new Vector2(side, side);

                _iconImage = go.GetComponent<Image>();
                _iconImage.sprite = item.Data.Icon;
                _iconImage.preserveAspect = true;
                _iconImage.raycastTarget = false;
            }
        }

        public void Clear()
        {
            for (int i = _cellImages.Count - 1; i >= 0; i--)
                if (_cellImages[i] != null)
                    Destroy(_cellImages[i].gameObject);
            _cellImages.Clear();

            if (_iconImage != null) Destroy(_iconImage.gameObject);
            _iconImage = null;
            Item = null;
        }

        public void SetAlpha(float alpha)
        {
            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
                if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            }

            _group.alpha = alpha;
        }

        /// <summary>
        /// Converts a screen point to the shape cell under it (shape coordinates).
        /// If the point is outside the shape the nearest shape cell is returned.
        /// </summary>
        public Vector2Int ScreenToShapeCell(Vector2 screenPos, Camera cam)
        {
            if (Item == null) return Vector2Int.zero;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect, screenPos, cam, out var local);
            // Pivot is top-left, so local.x >= 0 and local.y <= 0 inside the rect.
            int x = Mathf.FloorToInt(local.x / CellSize.x);
            int y = Mathf.FloorToInt(-local.y / CellSize.y);
            return Item.Shape.GetNearestCell(new Vector2Int(x, y));
        }
    }
}
