# Shape Inventory

A small Unity (uGUI, C#, **WebGL only**) puzzle game built around Tetris-shaped items.

The screen is split in two:

- **Left - player inventory:** a plain grid. Every item sits in exactly one cell, whatever its shape.
- **Right - object inventory:** a grid shaped like the silhouette of the current object (rocket, tree, boat, house, cat, ...). Items keep their Tetris shape here.

Drag pieces from the left onto the object. Pieces can be dragged back or moved around inside the object. Invalid drops snap back. There is no rotation.
Press **Done** when you think the object is filled; if the fill check passes the next object is loaded, otherwise the status text says "Not filled yet". After the last object the game shows "All objects complete!" and a **Restart** button.

## Opening the project

Pick one:

1. **Open the repo root as a Unity project** (Unity Hub -> Add -> select this folder). Unity creates `ProjectSettings/`, `Library/` and the `.meta` files on first import.
2. **New project:** create an empty Unity project (any recent version with uGUI, Unity 2021 LTS or newer; Unity 6 is fine), then copy `Assets/` and `Packages/manifest.json` from this repo into it.

The project does not use TextMeshPro or UI Toolkit. Text uses the legacy `UnityEngine.UI.Text` with the built-in `LegacyRuntime.ttf` font.
Pointer input goes through the EventSystem only, so both the old Input Manager and the new Input System package work.

## Quick start

1. `ShapeInventory -> Create Sample Content` - creates item, object and level assets under `Assets/Content/{Items,Objects,Levels}`
   (14 item types: tetrominoes I/O/T/S/Z/L/J, dot, dominoes, 3-bars, a 3-cell corner; 5 objects of 24-35 cells; 5 levels whose pieces tile their object exactly).
2. `ShapeInventory -> Build Demo Scene` - creates `Assets/Scenes/Main.unity` (canvas, event system, two panels, Done button, status text, drag controller, `GameFlowController` wired to the sample levels) and adds it to the Build Settings.
3. Open `Assets/Scenes/Main.unity` if it is not already open and press **Play**.

You can also create your own content via `Assets > Create > ShapeInventory > Item / Object / Level`. A level is an object plus a list of starting items (items may repeat; each entry becomes one item instance).
Shapes and silhouettes are written as text rows, top row first, `#` = filled, `.` = empty.

### GameFlowController options

- `levels` - the level order.
- `playerColumns` / `playerRows` - player inventory size (default 5 x 4; rows grow automatically if a level has more items).
- `autoAdvanceOnChange` - run the fill check after every change of the object inventory and advance automatically. **Off by default** (see the next section for why).
- Panels, the Done button, the status text and the drag controller are created in code if the references are left empty.

## WebGL

- `ShapeInventory -> Apply WebGL Settings` - switches the active build target to WebGL and sets Gzip compression, decompression fallback on, exception support off, run in background on.
- `ShapeInventory -> Build WebGL` - builds the scenes from the Build Settings (creating the demo scene if it does not exist) into `Build/WebGL`.
- Serve `Build/WebGL` over HTTP (for example `python3 -m http.server` inside that folder). With Gzip compression the server must send `Content-Encoding: gzip` for `.gz` files, otherwise the decompression fallback is used (slower start-up, but works on simple static hosts).
- The UI uses a Canvas Scaler (Scale With Screen Size, 1920x1080, match 0.5), so it adapts to the browser window; cell sizes are recomputed when the panels change size.

## ShapeFillChecker - where the "is it filled?" rule lives

`Assets/Scripts/Core/Rules/ShapeFillChecker.cs` is a **placeholder for you to implement**:

```csharp
public static ShapeFillResult IsShapeFilled(IReadOnlyList<PlacedItem> items)
```

It receives the items currently placed in the object inventory (each `PlacedItem` has the `ItemInstance` and the grid `Origin`; `PlacedItem.OccupiedCells()` gives absolute cells) and returns a `ShapeFillResult` with `IsFilled`.
The stub always returns `IsFilled = true`. Because of that, the game flow only calls it when **Done** is pressed - with `autoAdvanceOnChange` on, the stub would complete a level after the first placement.
To implement the real rule you will normally compare the union of all occupied cells with the silhouette cells of the object (`ObjectData.Silhouette` / `IObjectInventory.IsSilhouetteCell`); you may need to add the object to the method's inputs - the signature lives in the frozen `Core` contracts, so change it deliberately and update `GameFlowController.IsObjectFilled`.

## Architecture

Full contracts and conventions are in [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md). In short, assemblies under `Assets/Scripts`:

```
Core  <-  UI.Common  <-  UI.PlayerInventory
                     <-  UI.ObjectInventory
                     <-  DragDrop
                     <-  Game (references all of the above)  <-  Editor
```

- `Core` - data (`ItemData`, `ObjectData`), shapes, models (`PlayerInventoryModel`, `ObjectInventoryModel`), drag contracts, `ShapeFillChecker`. No UI.
- `UI.*` - the two inventory views; they never reference each other and talk to the drag layer only through the interfaces in `Core/DragDrop`.
- `DragDrop` - `ItemDragController`: ghost, hover/drop dispatch.
- `Game` - `LevelData`, `GameFlowController` (level flow, view binding, cell-size computation), `GameUiBuilder` (code-built panels/button/text).
- `Editor` - the `ShapeInventory/` menu.

## Known limitation

The code in this repository was written **without a compiler pass** (no Unity or .NET SDK was available while it was produced). Expect to fix a few compile errors or small API mismatches on first import; the sources deliberately stick to well-known APIs and C# 9.
