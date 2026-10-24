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

        /// <summary>Cell-level enchantment strip + label container (null for plain items). Has its own CanvasGroup.</summary>
        internal CanvasGroup BadgeGroup;

        /// <summary>Applies the (dimmed) alpha to the miniature shape and the cell-level badge.</summary>
        internal void SetAlpha(float alpha)
        {
            if (Shape != null) Shape.SetAlpha(alpha);
            if (BadgeGroup != null) BadgeGroup.alpha = alpha;
        }

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
