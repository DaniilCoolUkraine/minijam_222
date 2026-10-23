using ShapeInventory.Core;
using ShapeInventory.UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ShapeInventory.UI.PlayerInventory
{
    /// <summary>
    /// One item inside the plain player grid. Holds a transparent hit-area Image (on this GameObject)
    /// and a miniaturised <see cref="ItemShapeGraphic"/> child. Forwards drag events to the owning view.
    /// </summary>
    internal sealed class PlayerInventoryItemVisual : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        internal PlayerInventoryView View;
        internal ItemInstance Item;
        internal ItemShapeGraphic Shape;
        internal RectTransform Rect;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (View != null) View.HandleBeginDrag(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (View != null) View.HandleDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (View != null) View.HandleEndDrag(eventData);
        }
    }
}
