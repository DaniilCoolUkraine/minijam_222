# DragDrop (ShapeInventory.DragDrop)
- `ItemDragController` implements `IDragService`; registers itself in `DragServiceLocator.Current` (OnEnable), clears it in OnDisable.
- Code: `ItemDragController.Create(rootCanvas, ghostCellSize)` once at startup (Game assembly).
- Scene: add the component to any GameObject, assign `rootCanvas` (and `ghostCellSize`, canvas units).
- Ghost cell size should equal the object-inventory cell size so the ghost matches placed pieces.
- Views forward IBeginDragHandler/IDragHandler/IEndDragHandler to BeginDrag/UpdateDrag/EndDrag with `eventData.position` and `eventData.pressEventCamera`.
- Drop targets (IDropTarget) are found via EventSystem.RaycastAll + GetComponentInParent; they need a raycast-able Graphic.
- Needs an EventSystem and a GraphicRaycaster on every canvas that holds drop targets.
- The ghost blocks no raycasts. Focus loss, a destroyed source/ghost or controller disable cancels the drag (OnDragCancelled).
- No UnityEngine.Input / InputSystem usage.
