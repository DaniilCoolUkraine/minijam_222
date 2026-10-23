using System.Collections.Generic;
using ShapeInventory.Core;
using ShapeInventory.UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShapeInventory.UI.PlayerInventory
{
    /// <summary>
    /// View for the plain-grid player inventory. Every item occupies exactly one cell (its shape is only drawn
    /// miniaturised). Acts as drag source and drop target. Codes against <see cref="IPlayerInventory"/> only.
    /// </summary>
    public class PlayerInventoryView : MonoBehaviour, IDragSource, IDropTarget
    {
        private const float CellGap = 2f;
        private const float MiniShapeFill = 0.8f;
        private const float DimmedAlpha = 0.3f;

        private static readonly Color EmptyCellColor = new Color(0.16f, 0.17f, 0.20f, 0.90f);
        private static readonly Color ValidHoverColor = new Color(0.25f, 0.70f, 0.30f, 0.90f);
        private static readonly Color InvalidHoverColor = new Color(0.80f, 0.25f, 0.25f, 0.90f);

        private RectTransform _grid;
        private IPlayerInventory _model;
        private Vector2 _cellSize = new Vector2(64f, 64f);

        private readonly List<Image> _cellImages = new List<Image>();
        private readonly Dictionary<ItemInstance, PlayerInventoryItemVisual> _visuals =
            new Dictionary<ItemInstance, PlayerInventoryItemVisual>();
        private readonly HashSet<ItemInstance> _seen = new HashSet<ItemInstance>();
        private readonly List<ItemInstance> _stale = new List<ItemInstance>();

        private ItemInstance _draggedItem;
        private bool _dragActive;
        private int _hoverCell = -1;

        // ------------------------------------------------------------------ construction

        /// <summary>Creates the whole view hierarchy in code, stretched to fill <paramref name="parent"/>.</summary>
        public static PlayerInventoryView Build(RectTransform parent, string name = "PlayerInventory")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Fully transparent but still raycastable, so drops anywhere on the panel hit this IDropTarget.
            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;
            go.GetComponent<CanvasRenderer>().cullTransparentMesh = false;

            var view = go.AddComponent<PlayerInventoryView>();
            view.EnsureGrid();
            return view;
        }

        private void EnsureGrid()
        {
            if (_grid != null) return;

            var go = new GameObject("Grid", typeof(RectTransform));
            _grid = (RectTransform)go.transform;
            _grid.SetParent((RectTransform)transform, false);
            _grid.anchorMin = _grid.anchorMax = new Vector2(0.5f, 0.5f);
            _grid.pivot = new Vector2(0.5f, 0.5f);
            _grid.anchoredPosition = Vector2.zero;
            _grid.sizeDelta = Vector2.zero;
        }

        // ------------------------------------------------------------------ binding

        public void Bind(IPlayerInventory model, Vector2 cellSize)
        {
            Unbind();
            if (model == null) return;

            EnsureGrid();
            _model = model;
            _cellSize = new Vector2(Mathf.Max(1f, cellSize.x), Mathf.Max(1f, cellSize.y));
            _model.Changed += OnModelChanged;

            _grid.sizeDelta = new Vector2(_model.Columns * _cellSize.x, _model.Rows * _cellSize.y);
            BuildBackground();
            Refresh();
        }

        public void Unbind()
        {
            if (_model != null) _model.Changed -= OnModelChanged;
            _model = null;
            _draggedItem = null;
            _dragActive = false;
            _hoverCell = -1;

            DestroyAllVisuals();
            for (int i = 0; i < _cellImages.Count; i++)
                if (_cellImages[i] != null)
                    DestroyNow(_cellImages[i].gameObject);
            _cellImages.Clear();
        }

        private void OnDestroy()
        {
            if (_model != null) _model.Changed -= OnModelChanged;
            _model = null;
        }

        private void OnModelChanged()
        {
            Refresh();
        }

        private static void DestroyNow(GameObject go)
        {
            go.SetActive(false);
            Destroy(go);
        }

        // ------------------------------------------------------------------ visuals

        private void BuildBackground()
        {
            int count = _model.Columns * _model.Rows;
            for (int i = 0; i < count; i++)
            {
                int col = i % _model.Columns;
                int row = i / _model.Columns;

                var go = new GameObject("Cell_" + col + "_" + row, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(_grid, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(col * _cellSize.x + CellGap * 0.5f, -row * _cellSize.y - CellGap * 0.5f);
                rt.sizeDelta = new Vector2(Mathf.Max(1f, _cellSize.x - CellGap), Mathf.Max(1f, _cellSize.y - CellGap));

                var img = go.GetComponent<Image>();
                img.color = EmptyCellColor;
                img.raycastTarget = false; // hits fall through to the panel root (and item visuals sit on top)
                _cellImages.Add(img);
            }
        }

        /// <summary>
        /// Reconciles item visuals with the model: reuses visuals of items that are still stored (so a visual that is
        /// currently being dragged survives moves), creates missing ones, destroys stale ones.
        /// </summary>
        private void Refresh()
        {
            if (_model == null || _grid == null) return;

            _seen.Clear();
            int count = _model.CellCount;
            for (int i = 0; i < count; i++)
            {
                var item = _model.GetAt(i);
                if (item == null) continue;

                _seen.Add(item);
                PlayerInventoryItemVisual visual;
                if (!_visuals.TryGetValue(item, out visual) || visual == null)
                {
                    visual = CreateVisual(item);
                    _visuals[item] = visual;
                }

                PositionVisual(visual, i);
            }

            _stale.Clear();
            foreach (var kv in _visuals)
            {
                // An item removed from the model mid-drag keeps its (dimmed) visual until the drag ends,
                // so the EventSystem's pointerDrag target stays alive.
                if (!_seen.Contains(kv.Key) && kv.Key != _draggedItem)
                    _stale.Add(kv.Key);
            }

            for (int i = 0; i < _stale.Count; i++)
            {
                PlayerInventoryItemVisual v;
                if (_visuals.TryGetValue(_stale[i], out v) && v != null)
                    DestroyNow(v.gameObject);
                _visuals.Remove(_stale[i]);
            }

            _stale.Clear();
            _seen.Clear();

            foreach (var kv in _visuals)
            {
                var v = kv.Value;
                if (v == null || v.Shape == null) continue;
                v.Shape.SetAlpha(kv.Key == _draggedItem ? DimmedAlpha : 1f);
            }
        }

        private PlayerInventoryItemVisual CreateVisual(ItemInstance item)
        {
            var go = new GameObject("Item_" + item.InstanceId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_grid, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = _cellSize;

            // Transparent hit area covering the whole cell (receives drag events).
            var hit = go.GetComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            hit.raycastTarget = true;
            go.GetComponent<CanvasRenderer>().cullTransparentMesh = false;

            var shapeGo = new GameObject("Shape", typeof(RectTransform), typeof(ItemShapeGraphic));
            var shapeRt = (RectTransform)shapeGo.transform;
            shapeRt.SetParent(rt, false);
            shapeRt.anchorMin = shapeRt.anchorMax = new Vector2(0f, 1f);

            var shape = item.Shape;
            float maxDim = Mathf.Max(1, Mathf.Max(shape.Width, shape.Height));
            var miniCell = new Vector2(_cellSize.x / maxDim * MiniShapeFill, _cellSize.y / maxDim * MiniShapeFill);

            var graphic = shapeGo.GetComponent<ItemShapeGraphic>();
            graphic.Bind(item, miniCell, 1f, false);

            float shapeW = shape.Width * miniCell.x;
            float shapeH = shape.Height * miniCell.y;
            shapeRt.anchoredPosition = new Vector2((_cellSize.x - shapeW) * 0.5f, -(_cellSize.y - shapeH) * 0.5f);

            var visual = go.AddComponent<PlayerInventoryItemVisual>();
            visual.View = this;
            visual.Item = item;
            visual.Shape = graphic;
            visual.Rect = rt;
            return visual;
        }

        private void PositionVisual(PlayerInventoryItemVisual visual, int cellIndex)
        {
            int col = cellIndex % _model.Columns;
            int row = cellIndex / _model.Columns;
            visual.Rect.anchoredPosition = new Vector2(col * _cellSize.x, -row * _cellSize.y);
        }

        private void DestroyAllVisuals()
        {
            foreach (var kv in _visuals)
                if (kv.Value != null)
                    DestroyNow(kv.Value.gameObject);
            _visuals.Clear();
            _seen.Clear();
            _stale.Clear();
        }

        private PlayerInventoryItemVisual FindVisual(ItemInstance item)
        {
            if (item == null) return null;
            PlayerInventoryItemVisual v;
            return _visuals.TryGetValue(item, out v) && v != null ? v : null;
        }

        // ------------------------------------------------------------------ drag forwarding (from item visuals)

        internal void HandleBeginDrag(PlayerInventoryItemVisual visual, PointerEventData eventData)
        {
            _dragActive = false;
            if (_model == null || visual == null || visual.Item == null) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;

            var service = DragServiceLocator.Current;
            if (service == null || service.IsDragging) return;

            var payload = new DragPayload
            {
                Item = visual.Item,
                Source = this,
                GrabCell = visual.Item.Shape.GetCenterCell()
            };

            _dragActive = true;
            service.BeginDrag(payload, eventData.position, eventData.pressEventCamera);
        }

        internal void HandleDrag(PointerEventData eventData)
        {
            if (!_dragActive) return;
            var service = DragServiceLocator.Current;
            if (service != null) service.UpdateDrag(eventData.position, eventData.pressEventCamera);
        }

        internal void HandleEndDrag(PointerEventData eventData)
        {
            if (!_dragActive) return;
            _dragActive = false;
            var service = DragServiceLocator.Current;
            if (service != null) service.EndDrag(eventData.position, eventData.pressEventCamera);
        }

        // ------------------------------------------------------------------ IDragSource

        public void OnDragLifted(DragPayload payload)
        {
            if (payload == null) return;
            _draggedItem = payload.Item;
            var visual = FindVisual(_draggedItem);
            if (visual != null && visual.Shape != null) visual.Shape.SetAlpha(DimmedAlpha);
        }

        public void OnDragAccepted(DragPayload payload, IDropTarget target)
        {
            _draggedItem = null;
            ClearHover();

            if (_model != null && payload != null && !ReferenceEquals(target, this))
                _model.Remove(payload.Item); // raises Changed -> Refresh

            Refresh(); // restores alpha / drops orphaned visuals
        }

        public void OnDragCancelled(DragPayload payload)
        {
            _draggedItem = null;
            ClearHover();
            Refresh();
        }

        // ------------------------------------------------------------------ IDropTarget

        public bool UpdateHover(DragPayload payload, Vector2 screenPos, Camera cam)
        {
            if (_model == null || payload == null) return false;

            int cell = CellAt(screenPos, cam);
            bool own = ReferenceEquals(payload.Source, this);

            if (cell < 0)
            {
                SetHover(-1, false);
                if (own) return false;
                return _model.Items.Count < _model.CellCount; // goes to the first free cell unless full
            }

            var occupant = _model.GetAt(cell);
            bool valid = occupant == null || (own && occupant == payload.Item);
            SetHover(cell, valid);
            return own ? occupant == null : valid;
        }

        public bool TryDrop(DragPayload payload, Vector2 screenPos, Camera cam)
        {
            if (_model == null || payload == null || payload.Item == null) return false;

            int cell = CellAt(screenPos, cam);
            bool own = ReferenceEquals(payload.Source, this);

            if (own)
            {
                // Move inside the grid: only onto an EMPTY hovered cell, no swapping.
                if (cell < 0 || _model.GetAt(cell) != null) return false;
                return _model.TryAddAt(payload.Item, cell);
            }

            if (cell >= 0)
            {
                // Hovered cell is highlighted red when occupied, so refuse instead of silently redirecting.
                if (_model.GetAt(cell) != null) return false;
                return _model.TryAddAt(payload.Item, cell);
            }

            int placed;
            return _model.TryAddFirstFree(payload.Item, out placed);
        }

        public void ClearHover()
        {
            SetHover(-1, false);
        }

        // ------------------------------------------------------------------ helpers

        private int CellAt(Vector2 screenPos, Camera cam)
        {
            if (_model == null || _grid == null) return -1;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_grid, screenPos, cam, out local))
                return -1;

            Rect r = _grid.rect;
            int col = Mathf.FloorToInt((local.x - r.xMin) / _cellSize.x);
            int row = Mathf.FloorToInt((r.yMax - local.y) / _cellSize.y);
            if (col < 0 || col >= _model.Columns || row < 0 || row >= _model.Rows) return -1;
            return row * _model.Columns + col;
        }

        private void SetHover(int cell, bool valid)
        {
            if (_hoverCell >= 0 && _hoverCell < _cellImages.Count && _cellImages[_hoverCell] != null)
                _cellImages[_hoverCell].color = EmptyCellColor;

            _hoverCell = cell;

            if (cell >= 0 && cell < _cellImages.Count && _cellImages[cell] != null)
                _cellImages[cell].color = valid ? ValidHoverColor : InvalidHoverColor;
            else
                _hoverCell = -1;
        }
    }
}
