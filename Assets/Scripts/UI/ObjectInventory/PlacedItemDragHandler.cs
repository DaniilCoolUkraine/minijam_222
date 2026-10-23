using ShapeInventory.Core;
using ShapeInventory.UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ShapeInventory.UI.ObjectInventory
{
    /// <summary>
    /// Lives on every placed-item visual of an <see cref="ObjectInventoryView"/>. Starts a drag through the
    /// <see cref="DragServiceLocator"/> and forwards drag updates / end to it. Pointer events bubble up from the
    /// raycast-enabled cell images to this component (which sits on the visual's root).
    /// </summary>
    internal sealed class PlacedItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private ObjectInventoryView _view;
        private ItemShapeGraphic _graphic;
        private bool _dragStarted;

        public void Init(ObjectInventoryView view, ItemShapeGraphic graphic)
        {
            _view = view;
            _graphic = graphic;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragStarted = false;

            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (_view == null || _graphic == null || _graphic.Item == null) return;

            var service = DragServiceLocator.Current;
            if (service == null) return;

            Camera cam = eventData.pressEventCamera;
            var payload = new DragPayload
            {
                Item = _graphic.Item,
                Source = _view,
                GrabCell = _graphic.ScreenToShapeCell(eventData.position, cam)
            };

            _dragStarted = true;
            service.BeginDrag(payload, eventData.position, cam);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragStarted) return;

            var service = DragServiceLocator.Current;
            if (service != null)
                service.UpdateDrag(eventData.position, eventData.pressEventCamera);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragStarted) return;
            _dragStarted = false;

            var service = DragServiceLocator.Current;
            if (service != null)
                service.EndDrag(eventData.position, eventData.pressEventCamera);
        }
    }
}
