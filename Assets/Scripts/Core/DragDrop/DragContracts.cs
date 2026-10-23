using UnityEngine;

namespace ShapeInventory.Core
{
    /// <summary>Data describing the item currently being dragged.</summary>
    public sealed class DragPayload
    {
        public ItemInstance Item;
        public IDragSource Source;

        /// <summary>
        /// Shape cell (in the item's shape coordinates) that is "held" by the pointer.
        /// A drop target computes the placement origin as: hoveredGridCell - GrabCell.
        /// </summary>
        public Vector2Int GrabCell;
    }

    /// <summary>Implemented by anything items can be dragged FROM (player inventory view, object inventory view).</summary>
    public interface IDragSource
    {
        /// <summary>Drag started: hide/dim the source visual. The model is NOT changed yet.</summary>
        void OnDragLifted(DragPayload payload);

        /// <summary>
        /// Drop was accepted by <paramref name="target"/> (which already updated ITS model).
        /// The source must remove the item from its own model - EXCEPT when target is the source's own
        /// drop target (a move inside the same inventory: TryDrop already moved the item, do not remove it).
        /// Always restore/refresh the visual.
        /// </summary>
        void OnDragAccepted(DragPayload payload, IDropTarget target);

        /// <summary>Drop rejected / no target / cancelled: snap back (restore the visual, model untouched).</summary>
        void OnDragCancelled(DragPayload payload);
    }

    /// <summary>Implemented by anything items can be dropped ONTO.</summary>
    public interface IDropTarget
    {
        /// <summary>
        /// Called every frame while the pointer is over this target during a drag. Show a placement preview.
        /// Returns true if dropping right now would succeed.
        /// </summary>
        bool UpdateHover(DragPayload payload, Vector2 screenPos, Camera cam);

        /// <summary>Attempt to commit the drop into this target's model. True = accepted.</summary>
        bool TryDrop(DragPayload payload, Vector2 screenPos, Camera cam);

        /// <summary>Remove any hover/preview visuals (pointer left, drop finished or cancelled).</summary>
        void ClearHover();
    }

    /// <summary>Drag orchestration. Implemented by the DragDrop assembly's ItemDragController.</summary>
    public interface IDragService
    {
        bool IsDragging { get; }
        void BeginDrag(DragPayload payload, Vector2 screenPos, Camera cam);
        void UpdateDrag(Vector2 screenPos, Camera cam);
        void EndDrag(Vector2 screenPos, Camera cam);
        void CancelDrag();
    }

    /// <summary>Static access point so views do not need a scene reference to the controller.</summary>
    public static class DragServiceLocator
    {
        /// <summary>Set by the controller in OnEnable, cleared in OnDisable.</summary>
        public static IDragService Current;
    }
}
