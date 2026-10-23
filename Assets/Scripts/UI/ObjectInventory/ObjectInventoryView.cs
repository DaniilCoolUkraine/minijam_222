using System.Collections.Generic;
using ShapeInventory.Core;
using ShapeInventory.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace ShapeInventory.UI.ObjectInventory
{
    /// <summary>
    /// View of an <see cref="IObjectInventory"/>: draws the silhouette grid, the placed Tetris pieces,
    /// acts as a drop target (with a green/red placement preview) and as a drag source for placed pieces.
    /// Built entirely in code via <see cref="Build"/>.
    /// </summary>
    public class ObjectInventoryView : MonoBehaviour, IDragSource, IDropTarget
    {
        private const float CellGap = 2f;
        private const float DraggedAlpha = 0.3f;
        private const int PreviewMargin = 1; // preview cells farther than this outside the grid are not drawn

        private static readonly Color PreviewValidColor = new Color(0.2f, 0.85f, 0.3f, 0.5f);
        private static readonly Color PreviewInvalidColor = new Color(0.95f, 0.2f, 0.2f, 0.5f);

        private RectTransform _grid;
        private RectTransform _silhouetteRoot;
        private RectTransform _itemsRoot;
        private RectTransform _previewRoot;

        private IObjectInventory _model;
        private Vector2 _cellSize = new Vector2(64f, 64f);
        private System.Action _changedHandler;

        private readonly Dictionary<ItemInstance, ItemShapeGraphic> _visuals = new Dictionary<ItemInstance, ItemShapeGraphic>();
        private readonly HashSet<ItemInstance> _placedScratch = new HashSet<ItemInstance>();
        private readonly List<ItemInstance> _removeScratch = new List<ItemInstance>();
        private ItemInstance _draggedItem;

        private readonly List<Image> _previewPool = new List<Image>();
        private int _previewActive;
        private bool _previewValid;
        private bool _previewShown;
        private ItemInstance _previewItem;
        private Vector2Int _previewOrigin;

        // ------------------------------------------------------------------ construction

        /// <summary>Creates the view hierarchy under <paramref name="parent"/>, stretched to fill it.</summary>
        public static ObjectInventoryView Build(RectTransform parent, string name = "ObjectInventory")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            // Fully transparent but still raycastable, so drops anywhere in the panel reach this IDropTarget.
            var bg = go.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0f);
            bg.raycastTarget = true;
            bg.canvasRenderer.cullTransparentMesh = false;

            var view = go.AddComponent<ObjectInventoryView>();
            view.EnsureHierarchy();
            return view;
        }

        private void EnsureHierarchy()
        {
            if (_grid != null) return;

            _grid = CreateRect("Grid", (RectTransform)transform);
            _grid.anchorMin = _grid.anchorMax = new Vector2(0.5f, 0.5f);
            _grid.pivot = new Vector2(0f, 1f); // top-left, y grows downward

            _silhouetteRoot = CreateFillRect("Silhouette", _grid);
            _itemsRoot = CreateFillRect("Items", _grid);
            _previewRoot = CreateFillRect("Preview", _grid);
        }

        private static RectTransform CreateRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            return rt;
        }

        private static RectTransform CreateFillRect(string name, RectTransform parent)
        {
            var rt = CreateRect(name, parent);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        // ------------------------------------------------------------------ binding

        /// <summary>Shows <paramref name="model"/>. Safe to call repeatedly (re-binds).</summary>
        public void Bind(IObjectInventory model, Vector2 cellSize)
        {
            EnsureHierarchy();
            Unbind();

            if (model == null) return;

            _model = model;
            _cellSize = new Vector2(Mathf.Max(1f, cellSize.x), Mathf.Max(1f, cellSize.y));

            _grid.sizeDelta = new Vector2(_model.Width * _cellSize.x, _model.Height * _cellSize.y);
            // Centre the top-left-pivoted container inside the panel.
            _grid.anchoredPosition = new Vector2(-_grid.sizeDelta.x * 0.5f, _grid.sizeDelta.y * 0.5f);

            if (_changedHandler == null) _changedHandler = OnModelChanged;
            _model.Changed += _changedHandler;

            BuildSilhouette();
            RefreshItems();
        }

        /// <summary>Detaches from the model and clears all visuals. Safe to call when not bound.</summary>
        public void Unbind()
        {
            if (_model != null && _changedHandler != null)
                _model.Changed -= _changedHandler;
            _model = null;
            _draggedItem = null;

            ClearHover();

            foreach (var kv in _visuals)
                if (kv.Value != null)
                    Destroy(kv.Value.gameObject);
            _visuals.Clear();

            if (_silhouetteRoot != null)
                for (int i = _silhouetteRoot.childCount - 1; i >= 0; i--)
                    Destroy(_silhouetteRoot.GetChild(i).gameObject);
        }

        private void OnDestroy()
        {
            if (_model != null && _changedHandler != null)
                _model.Changed -= _changedHandler;
            _model = null;
        }

        private void OnModelChanged()
        {
            if (_model == null) return;
            RefreshItems();
        }

        // ------------------------------------------------------------------ silhouette

        private void BuildSilhouette()
        {
            Color fill = _model.Data != null ? _model.Data.SilhouetteColor : new Color(0.25f, 0.25f, 0.3f, 1f);
            Color edge = Color.Lerp(fill, Color.black, 0.35f);
            edge.a = fill.a;

            for (int y = 0; y < _model.Height; y++)
            {
                for (int x = 0; x < _model.Width; x++)
                {
                    if (!_model.IsSilhouetteCell(new Vector2Int(x, y))) continue;

                    // Darker full-size cell + inset in the silhouette colour: the grid lines read as a darker border.
                    var outer = CreateCellImage("Cell_" + x + "_" + y, _silhouetteRoot, x, y, 0f, edge);
                    var inner = CreateCellImage("Inset", outer.rectTransform, 0, 0, CellGap, fill);
                    inner.rectTransform.anchoredPosition = new Vector2(CellGap * 0.5f, -CellGap * 0.5f);
                }
            }
        }

        private Image CreateCellImage(string name, RectTransform parent, int cx, int cy, float gap, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(cx * _cellSize.x + gap * 0.5f, -cy * _cellSize.y - gap * 0.5f);
            rt.sizeDelta = new Vector2(Mathf.Max(1f, _cellSize.x - gap), Mathf.Max(1f, _cellSize.y - gap));

            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        // ------------------------------------------------------------------ placed items

        private void RefreshItems()
        {
            if (_model == null) return;

            var placed = _model.PlacedItems;
            _placedScratch.Clear();

            for (int i = 0; i < placed.Count; i++)
            {
                PlacedItem p = placed[i];
                if (p.Item == null) continue;
                _placedScratch.Add(p.Item);

                ItemShapeGraphic g;
                if (!_visuals.TryGetValue(p.Item, out g) || g == null)
                {
                    g = CreateItemVisual(p.Item);
                    _visuals[p.Item] = g;
                }

                var rt = g.Rect;
                rt.anchoredPosition = new Vector2(p.Origin.x * _cellSize.x, -p.Origin.y * _cellSize.y);
            }

            // Drop visuals of items that are no longer placed. The visual that is currently being dragged is kept
            // alive (its drag handler still has to receive OnEndDrag); it is cleaned up when the drag finishes.
            _removeScratch.Clear();
            foreach (var kv in _visuals)
            {
                if (_placedScratch.Contains(kv.Key)) continue;
                if (kv.Key == _draggedItem && kv.Value != null) continue;
                _removeScratch.Add(kv.Key);
            }

            for (int i = 0; i < _removeScratch.Count; i++)
            {
                var key = _removeScratch[i];
                ItemShapeGraphic g;
                if (_visuals.TryGetValue(key, out g) && g != null)
                    Destroy(g.gameObject);
                _visuals.Remove(key);
            }

            _removeScratch.Clear();
            _placedScratch.Clear();

            // Re-apply the drag dimming (looked up by item, never by a stale reference).
            if (_draggedItem != null)
            {
                ItemShapeGraphic dragged;
                if (_visuals.TryGetValue(_draggedItem, out dragged) && dragged != null)
                    dragged.SetAlpha(DraggedAlpha);
            }
        }

        private ItemShapeGraphic CreateItemVisual(ItemInstance item)
        {
            var go = new GameObject("Item_" + item, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_itemsRoot, false);
            rt.localScale = Vector3.one;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            var graphic = go.AddComponent<ItemShapeGraphic>();
            graphic.Bind(item, _cellSize, CellGap, true);

            var handler = go.AddComponent<PlacedItemDragHandler>();
            handler.Init(this, graphic);
            return graphic;
        }

        private void RestoreVisual(ItemInstance item)
        {
            if (item == null) return;
            ItemShapeGraphic g;
            if (_visuals.TryGetValue(item, out g) && g != null)
                g.SetAlpha(1f);
        }

        // ------------------------------------------------------------------ IDragSource

        public void OnDragLifted(DragPayload payload)
        {
            if (payload == null || payload.Item == null) return;
            _draggedItem = payload.Item;

            ItemShapeGraphic g;
            if (_visuals.TryGetValue(payload.Item, out g) && g != null)
                g.SetAlpha(DraggedAlpha);
        }

        public void OnDragAccepted(DragPayload payload, IDropTarget target)
        {
            ItemInstance item = payload != null ? payload.Item : null;
            RestoreVisual(item);
            _draggedItem = null;

            // A drop onto this very view was a move inside the model: nothing to remove.
            if (item != null && _model != null && !ReferenceEquals(target, this))
                _model.Remove(item);

            if (_model != null) RefreshItems();
        }

        public void OnDragCancelled(DragPayload payload)
        {
            RestoreVisual(payload != null ? payload.Item : null);
            _draggedItem = null;
            if (_model != null) RefreshItems();
        }

        // ------------------------------------------------------------------ IDropTarget

        public bool UpdateHover(DragPayload payload, Vector2 screenPos, Camera cam)
        {
            if (_model == null || payload == null || payload.Item == null || _grid == null)
            {
                ClearHover();
                return false;
            }

            bool inside;
            Vector2Int origin;
            if (!TryGetOrigin(payload, screenPos, cam, out origin, out inside))
            {
                ClearHover();
                return false;
            }

            bool valid = inside && _model.CanPlace(payload.Item, origin);
            ShowPreview(payload.Item, origin, valid);
            return valid;
        }

        public bool TryDrop(DragPayload payload, Vector2 screenPos, Camera cam)
        {
            if (_model == null || payload == null || payload.Item == null || _grid == null) return false;

            bool inside;
            Vector2Int origin;
            if (!TryGetOrigin(payload, screenPos, cam, out origin, out inside)) return false;
            if (!inside) return false;

            return _model.TryPlace(payload.Item, origin);
        }

        public void ClearHover()
        {
            _previewShown = false;
            _previewItem = null;
            if (_previewActive == 0) return;

            for (int i = 0; i < _previewActive; i++)
                if (_previewPool[i] != null)
                    _previewPool[i].gameObject.SetActive(false);
            _previewActive = 0;
        }

        /// <summary>
        /// Converts a screen point into the placement origin (hovered grid cell minus grab cell).
        /// <paramref name="inside"/> tells whether the pointer is within the grid container.
        /// </summary>
        private bool TryGetOrigin(DragPayload payload, Vector2 screenPos, Camera cam, out Vector2Int origin, out bool inside)
        {
            origin = Vector2Int.zero;
            inside = false;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_grid, screenPos, cam, out local))
                return false;

            // Pivot is top-left: local.x grows right, local.y is <= 0 inside the container.
            int cx = Mathf.FloorToInt(local.x / _cellSize.x);
            int cy = Mathf.FloorToInt(-local.y / _cellSize.y);
            inside = cx >= 0 && cy >= 0 && cx < _model.Width && cy < _model.Height;

            origin = new Vector2Int(cx - payload.GrabCell.x, cy - payload.GrabCell.y);
            return true;
        }

        // ------------------------------------------------------------------ preview

        private void ShowPreview(ItemInstance item, Vector2Int origin, bool valid)
        {
            if (_previewShown && _previewItem == item && _previewOrigin == origin && _previewValid == valid)
                return; // unchanged since last frame

            _previewShown = true;
            _previewItem = item;
            _previewOrigin = origin;
            _previewValid = valid;

            Color color = valid ? PreviewValidColor : PreviewInvalidColor;
            var cells = item.Shape.Cells;
            int used = 0;

            for (int i = 0; i < cells.Count; i++)
            {
                int gx = origin.x + cells[i].x;
                int gy = origin.y + cells[i].y;

                // Clip cells that are far outside the grid.
                if (gx < -PreviewMargin || gy < -PreviewMargin ||
                    gx >= _model.Width + PreviewMargin || gy >= _model.Height + PreviewMargin)
                    continue;

                Image img = GetPreviewImage(used);
                used++;

                var rt = img.rectTransform;
                rt.anchoredPosition = new Vector2(gx * _cellSize.x + CellGap * 0.5f, -gy * _cellSize.y - CellGap * 0.5f);
                rt.sizeDelta = new Vector2(Mathf.Max(1f, _cellSize.x - CellGap), Mathf.Max(1f, _cellSize.y - CellGap));
                img.color = color;
                if (!img.gameObject.activeSelf) img.gameObject.SetActive(true);
            }

            for (int i = used; i < _previewActive; i++)
                if (_previewPool[i] != null)
                    _previewPool[i].gameObject.SetActive(false);

            _previewActive = used;
        }

        private Image GetPreviewImage(int index)
        {
            if (index < _previewPool.Count && _previewPool[index] != null)
                return _previewPool[index];

            var go = new GameObject("Preview_" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_previewRoot, false);
            rt.localScale = Vector3.one;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;

            if (index < _previewPool.Count) _previewPool[index] = img;
            else _previewPool.Add(img);
            return img;
        }
    }
}
