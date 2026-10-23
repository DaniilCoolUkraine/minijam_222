# Shape Inventory - Architecture & Contracts

Unity (uGUI), C#, **WebGL only**, mouse pointer input via EventSystem pointer handlers
(`IBeginDragHandler` etc. work with both the Input System UI module and the Standalone Input Module - do NOT call `UnityEngine.Input` or `UnityEngine.InputSystem` directly).

## Game in one paragraph
Screen is split in two. **Player inventory**: a plain grid, every item sits in its own cell (shape ignored).
**Object inventory**: a grid shaped like the current object's silhouette; items keep their Tetris shape here.
The player drags pieces from the player inventory onto the object. Pieces can be dragged back / moved inside the object.
Invalid drops snap back. When the object is complete (`ShapeFillChecker.IsShapeFilled`, currently a stub that always returns true) the game advances to the next object.
No rotation.

## Conventions
- Grid coordinates: x right, **y down**, (0,0) = top-left. Shape/silhouette rows are strings, top row first.
- Player inventory cell index = `row * Columns + column`.
- uGUI only. Overlay or Screen Space - Camera canvases both must work: always pass the `Camera` (null for overlay) into `RectTransformUtility`.
- No `UnityEngine.Input`, no `FindObjectOfType` in hot paths, no per-frame allocations in drag update.
- No UI Toolkit, no TextMeshPro (keeps Unity-version agnostic). Use `UnityEngine.UI.Text` with the built-in font only if text is really needed (`Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`).
- Namespaces match asmdef names (e.g. `ShapeInventory.UI.PlayerInventory`).
- Every new folder with scripts needs the correct asmdef already provided - do NOT add or edit asmdefs outside your own folder.
- C# language level: C# 9 max, no records/init-only needed. No `new()` target typing tricks that need newer Unity.

## Assemblies (dependency direction ->)
```
Core  <-  UI.Common  <-  UI.PlayerInventory
                     <-  UI.ObjectInventory
                     <-  DragDrop
                     <-  Game (references all of the above)  <-  Editor
Tests.EditMode -> Core
```
UI.PlayerInventory, UI.ObjectInventory and DragDrop must NOT reference each other. They talk only through the interfaces in `Core/DragDrop/DragContracts.cs`
(`IDragSource`, `IDropTarget`, `IDragService`, `DragServiceLocator`).

## Drag protocol
1. A view (source) receives `OnBeginDrag` from the EventSystem, builds a `DragPayload { Item, Source, GrabCell }` and calls
   `DragServiceLocator.Current.BeginDrag(payload, eventData.position, eventData.pressEventCamera)`.
   Views must also forward `OnDrag` -> `UpdateDrag` and `OnEndDrag` -> `EndDrag`.
2. The controller calls `source.OnDragLifted(payload)`, spawns a ghost (`ItemShapeGraphic`, raycast off) that follows the pointer.
3. Each frame it raycasts (`EventSystem.RaycastAll`), finds the topmost `IDropTarget` via `GetComponentInParent<IDropTarget>()`, calls `UpdateHover`; when the target changes it calls `ClearHover` on the old one.
4. On `EndDrag`: if a target exists and `TryDrop` returns true -> `source.OnDragAccepted(payload, target)`; otherwise `source.OnDragCancelled(payload)` (snap back). Always `ClearHover` and destroy the ghost.
5. Moves inside one inventory: the target's `TryDrop` moves the item in the model (models treat re-adding an already-stored item as a MOVE). The source recognises "target is my own drop target" and does not remove the item.

Placement origin for the object inventory: `hoveredGridCell - payload.GrabCell`.
From the player inventory the grab cell is `item.Shape.GetCenterCell()`; from the object inventory it is the shape cell under the pointer (`ItemShapeGraphic.ScreenToShapeCell`).

## Ownership (to keep merges conflict-free)
| Folder | Owner |
|---|---|
| `Core/**` except the two model files | Scaffold (frozen - do not edit; ask the orchestrator through your final report if a contract must change) |
| `Core/Models/PlayerInventoryModel.cs`, `Core/Models/ObjectInventoryModel.cs`, `Assets/Tests/**` | Agent A |
| `UI/PlayerInventory/**` | Agent B |
| `UI/ObjectInventory/**` | Agent C |
| `DragDrop/**` | Agent D |
| `Game/**`, `Editor/**`, `ProjectSettings/**`, `README.md` | Agent E |
| `UI/Common/**` | Scaffold (frozen) |

Each agent works on its own git branch and commits only inside its owned folders.
