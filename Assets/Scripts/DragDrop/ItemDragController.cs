using System;
using System.Collections.Generic;
using ShapeInventory.Core;
using ShapeInventory.UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ShapeInventory.DragDrop
{
    /// <summary>
    /// Drag orchestration (see docs/ARCHITECTURE.md "Drag protocol"). Spawns a ghost that follows the pointer,
    /// finds the topmost <see cref="IDropTarget"/> under it and commits / cancels the drop.
    /// </summary>
    public class ItemDragController : MonoBehaviour, IDragService
    {
        private const float GhostAlpha = 0.85f;

        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Vector2 ghostCellSize = new Vector2(64f, 64f);

        private readonly List<RaycastResult> _results = new List<RaycastResult>(16);
        private PointerEventData _pointerData;
        private EventSystem _pointerDataOwner;

        private DragPayload _payload;
        private bool _dragging;
        private GameObject _ghost;
        private RectTransform _ghostRect;
        private RectTransform _canvasRect;
        private Vector2 _ghostCell;
        private IDropTarget _target;

        /// <summary>Creates a controller on a new GameObject under <paramref name="rootCanvas"/>.</summary>
        public static ItemDragController Create(Canvas rootCanvas, Vector2 ghostCellSize)
        {
            var go = new GameObject("ItemDragController", typeof(RectTransform));
            if (rootCanvas != null) go.transform.SetParent(rootCanvas.transform, false);
            var controller = go.AddComponent<ItemDragController>();
            controller.rootCanvas = rootCanvas;
            controller.ghostCellSize = ghostCellSize;
            return controller;
        }

        public Vector2 GhostCellSize
        {
            get { return ghostCellSize; }
            set { ghostCellSize = value; }
        }

        public bool IsDragging { get { return _dragging; } }

        private void OnEnable()
        {
            DragServiceLocator.Current = this;
        }

        private void OnDisable()
        {
            if (_dragging) CancelDrag();
            if (ReferenceEquals(DragServiceLocator.Current, this)) DragServiceLocator.Current = null;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // WebGL: the browser can lose focus mid-drag and never deliver the pointer-up.
            if (!hasFocus && _dragging) CancelDrag();
        }

        private void Update()
        {
            if (!_dragging) return;
            if (_ghost == null || SourceDestroyed(_payload))
                CancelDrag();
        }

        // ------------------------------------------------------------------ IDragService

        public void BeginDrag(DragPayload payload, Vector2 screenPos, Camera cam)
        {
            if (_dragging) return;
            if (payload == null || payload.Item == null) return;

            if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
            {
                Debug.LogError("ItemDragController: no root canvas assigned; cannot start drag.");
                return;
            }

            _canvasRect = rootCanvas.transform as RectTransform;
            if (_canvasRect == null) return;

            _payload = payload;
            _dragging = true;
            _target = null;

            try
            {
                if (payload.Source != null) payload.Source.OnDragLifted(payload);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                AbortAfterFailedLift();
                return;
            }

            CreateGhost(payload, screenPos, cam);
            if (_ghost == null)
            {
                AbortAfterFailedLift();
                return;
            }

            UpdateDrag(screenPos, cam);
        }

        public void UpdateDrag(Vector2 screenPos, Camera cam)
        {
            if (!_dragging) return;

            if (_ghost == null || SourceDestroyed(_payload))
            {
                CancelDrag();
                return;
            }

            MoveGhost(screenPos, cam);

            IDropTarget newTarget = FindTarget(screenPos);
            if (!ReferenceEquals(newTarget, _target))
            {
                ClearHoverSafe(_target);
                _target = newTarget;
            }

            if (_target != null)
            {
                try
                {
                    _target.UpdateHover(_payload, screenPos, cam);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public void EndDrag(Vector2 screenPos, Camera cam)
        {
            if (!_dragging) return;

            DragPayload payload = _payload;
            IDragSource source = payload.Source;
            bool sourceAlive = !SourceDestroyed(payload);

            IDropTarget finalTarget = sourceAlive ? FindTarget(screenPos) : null;
            if (!ReferenceEquals(finalTarget, _target))
            {
                ClearHoverSafe(_target);
                _target = finalTarget;
            }

            bool accepted = false;
            if (finalTarget != null)
            {
                try
                {
                    accepted = finalTarget.TryDrop(payload, screenPos, cam);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    accepted = false;
                }
            }

            ClearHoverSafe(finalTarget);
            ResetState();

            if (!sourceAlive || ReferenceEquals(source, null)) return;
            try
            {
                if (accepted) source.OnDragAccepted(payload, finalTarget);
                else source.OnDragCancelled(payload);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void CancelDrag()
        {
            if (!_dragging) return;

            DragPayload payload = _payload;
            bool sourceAlive = !SourceDestroyed(payload);

            ClearHoverSafe(_target);
            ResetState();

            NotifyCancelled(payload, sourceAlive);
        }

        // ------------------------------------------------------------------ internals

        private void AbortAfterFailedLift()
        {
            DragPayload payload = _payload;
            bool sourceAlive = !SourceDestroyed(payload);
            ResetState();
            NotifyCancelled(payload, sourceAlive);
        }

        private static void NotifyCancelled(DragPayload payload, bool sourceAlive)
        {
            if (payload == null || !sourceAlive || ReferenceEquals(payload.Source, null)) return;
            try
            {
                payload.Source.OnDragCancelled(payload);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void ResetState()
        {
            if (_ghost != null)
            {
                _ghost.SetActive(false);
                Destroy(_ghost);
            }

            _ghost = null;
            _ghostRect = null;
            _target = null;
            _payload = null;
            _dragging = false;
        }

        private void CreateGhost(DragPayload payload, Vector2 screenPos, Camera cam)
        {
            try
            {
                var go = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup));
                _ghost = go;
                go.transform.SetParent(rootCanvas.transform, false);
                go.transform.SetAsLastSibling();

                var group = go.GetComponent<CanvasGroup>();
                group.blocksRaycasts = false;
                group.interactable = false;
                group.alpha = GhostAlpha;

                var graphic = go.AddComponent<ItemShapeGraphic>();
                graphic.Bind(payload.Item, ghostCellSize, 2f, false);

                var rt = (RectTransform)go.transform;
                // Anchor at the canvas pivot so anchoredPosition lives in the same space that
                // ScreenPointToLocalPointInRectangle (relative to the canvas rect pivot) returns.
                Vector2 canvasPivot = _canvasRect.pivot;
                rt.anchorMin = canvasPivot;
                rt.anchorMax = canvasPivot;
                rt.pivot = new Vector2(0f, 1f);

                _ghostRect = rt;
                _ghostCell = ghostCellSize;
                MoveGhost(screenPos, cam);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (_ghost != null) Destroy(_ghost);
                _ghost = null;
                _ghostRect = null;
            }
        }

        private void MoveGhost(Vector2 screenPos, Camera cam)
        {
            if (_ghostRect == null || _canvasRect == null || _payload == null) return;

            Vector2 pointerLocal;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, cam, out pointerLocal))
                return;

            float gx = _payload.GrabCell.x + 0.5f;
            float gy = _payload.GrabCell.y + 0.5f;
            // Ghost pivot is top-left; shape y grows downward, canvas y grows upward.
            _ghostRect.anchoredPosition = new Vector2(
                pointerLocal.x - gx * _ghostCell.x,
                pointerLocal.y + gy * _ghostCell.y);
        }

        private IDropTarget FindTarget(Vector2 screenPos)
        {
            EventSystem es = EventSystem.current;
            if (es == null) return null;

            if (_pointerData == null || !ReferenceEquals(_pointerDataOwner, es))
            {
                _pointerData = new PointerEventData(es);
                _pointerDataOwner = es;
            }

            _pointerData.position = screenPos;
            _results.Clear();
            es.RaycastAll(_pointerData, _results);

            IDropTarget found = null;
            for (int i = 0; i < _results.Count; i++)
            {
                GameObject go = _results[i].gameObject;
                if (go == null) continue;
                IDropTarget candidate = go.GetComponentInParent<IDropTarget>();
                if (candidate != null && !IsDestroyed(candidate))
                {
                    found = candidate;
                    break;
                }
            }

            _results.Clear();
            return found;
        }

        private static void ClearHoverSafe(IDropTarget target)
        {
            if (target == null || IsDestroyed(target)) return;
            try
            {
                target.ClearHover();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>True only when the payload has a source that is a destroyed UnityEngine.Object.</summary>
        private static bool SourceDestroyed(DragPayload payload)
        {
            if (payload == null) return true;
            if (ReferenceEquals(payload.Source, null)) return false;
            return IsDestroyed(payload.Source);
        }

        /// <summary>True for null references and for destroyed UnityEngine.Objects hidden behind an interface.</summary>
        private static bool IsDestroyed(object obj)
        {
            if (ReferenceEquals(obj, null)) return true;
            var unityObj = obj as UnityEngine.Object;
            if (!ReferenceEquals(unityObj, null)) return unityObj == null;
            return false;
        }
    }
}
