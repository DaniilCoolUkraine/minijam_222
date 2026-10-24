# Shape Inventory

A small Unity (uGUI, C#, **WebGL only**) puzzle game built around Tetris-shaped items.

The screen is split in two:

- **Left - player inventory:** a plain grid. Every item sits in exactly one cell, whatever its shape.
- **Right - object inventory:** a grid shaped like the silhouette of the current object (axe, pickaxe, ... millstone). Items keep their Tetris shape here.

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
   (plain and enchanted pieces, 10 village-tool objects of 20-60 cells and the 10-level campaign described in "The 10-level campaign"; every level's pieces tile its object exactly).
2. `ShapeInventory -> Build Demo Scene` - creates `Assets/Scenes/Main.unity` (canvas, event system, two panels, Done button, status text, drag controller, `GameFlowController` wired to the sample levels) and adds it to the Build Settings.
3. Open `Assets/Scenes/Main.unity` if it is not already open and press **Play**.

You can also create your own content via `Assets > Create > ShapeInventory > Item / Object / Level`. A level is an object plus a list of starting items (items may repeat; each entry becomes one item instance).
Shapes and silhouettes are written as text rows, top row first, `#` = filled, `.` = empty.

### GameFlowController options

- `levels` - the level order.
- `playerColumns` / `playerRows` - player inventory size (default 5 x 4; rows grow automatically if a level has more items).
- Panels, the Done button, the status text, the quest bubble / result / score texts and the drag controller are created in code if the references are left empty.
- There is no auto-advance any more: the quest is judged only when **Done** is pressed (see "Enchantments and quests").

## Enchantments and quests

Every item has an `EnchantmentType` (Durability, Haste, Sharpness, Efficiency, Luck, Protection, or None = plain filler).
The **tier** comes from the item's size: 1-2 cells = I, 3 cells = II, 4+ cells = III (`EnchantmentTierRules`, change it there).
Pieces show a badge such as `SHP III`.

Every `LevelData` has an object sprite (drawn behind the silhouette grid), a story from the object's owner (`questDescription`,
a hint, the requirement list is never shown), the `requirements` (type + tier, tier 0 = any tier) and three reactions:
`successText`, `partialText`, `failureText`.

Pressing Done runs `QuestEvaluator.Evaluate(placedItems, level.Requirements)` (`Assets/Scripts/Core/Enchantments/`):

| Outcome | Rule | Shown as |
|---|---|---|
| Full | every requirement met (right type AND right tier) and no enchantment type applied that the quest did not ask for | green, `successText` |
| Partial | at least one requirement met, nothing unrequired applied, but not everything right (missing one, or right type at the wrong tier) | amber, `partialText` |
| Failure | nothing correct, or any unrequired enchantment applied | red, `failureText` |

When one type is placed several times, its highest tier counts. Plain pieces are ignored. **Every outcome advances** to the next level
(after the reaction is shown); the end screen sums the scores. A Full result is designed to fill the silhouette 100% - the filled
shape is the player's hint about which pieces to use.

## The 10-level campaign

Full spec: [`docs/LEVELS.md`](docs/LEVELS.md). Each level is a different object from a village, each with its own owner telling the story:

| # | Object | Requirements (type : tier, 0 = any) | Notes |
|---|---|---|---|
| 1 | Axe | sharpness | tutorial: only sharpness pieces exist, any arrangement is a Full result |
| 2 | Pickaxe | efficiency III | tutorial: one right type, one wrong type; tiers start here |
| 3 | Pitchfork | durability, sharpness II | tutorial: two right, one wrong |
| 4 | Scissors | sharpness III, durability | |
| 5 | Hammer | durability II, efficiency, haste III | |
| 6 | Shovel | efficiency III, durability I, luck II | plain filler pieces start here |
| 7 | Plow | durability II, efficiency III, luck I, protection II | |
| 8 | Anvil | durability III, protection III, sharpness II, haste I | |
| 9 | Scythe | sharpness III, haste II, luck III, efficiency I | |
| 10 | Millstone | durability III, efficiency II, protection III, haste I, luck II | finale, 60 cells with a hole |

Every level has a guaranteed Full tiling among its starting pieces (checked by an exact-cover solver). The tiling is the hint: the
filled silhouette shows which pieces to use, the story hints at the enchantments.

Where things live:
- `Tools/verify_levels.py` - single source of truth for items, silhouettes and levels. Run it to verify all ten levels; `--emit`
  regenerates `Assets/Scripts/Editor/SampleContentData.cs`. The plain run fails when that file is out of sync.
- `Tools/level_texts.py` - stories and the three reaction texts per object. Edit the texts here, then run `--emit`.

After pulling this branch, in Unity run once:
1. `ShapeInventory -> Create Sample Content` - updates assets in place (GUIDs kept), moves the five old levels onto the first ones,
   deletes the old rocket / tree / boat / house / cat objects, sprites and unused enchanted items, creates the new objects, items and levels,
   writes the ten levels into the `levels` array of `Assets/Scenes/Main.unity`, and generates placeholder sprites only when a PNG is missing
   (replace them with real art, they are never overwritten). It is idempotent.
2. `ShapeInventory -> Validate Levels` - checks that each level has a Full tiling, requirements and texts.
   (`ShapeInventory -> Sync Levels Into Main Scene` repeats only the scene part.)

The `.asset` files are not in git for the new content: only Unity can generate them, so the repository keeps the old assets until you run the menu.

## Main menu and cutscene slot

`ShapeInventory -> Build Main Menu Scene` creates `Assets/Scenes/MainMenu.unity` (title, Play, Quit - Quit is hidden on WebGL) and makes it
build index 0, with `Main` right after it. Re-running it asks before overwriting, because it replaces your cutscene setup.

The `CutsceneOverlay` (a full-screen panel in front of the menu) carries a **`CutscenePlayer`**. It plays no media itself: every time the
scene loads it waits `duration` seconds (default 5, real time), then fades out (`fadeOutSeconds`) and hides `rootToHide`.
The player can skip it by clicking / tapping anywhere, or Enter / Space (the invisible full-screen `SkipButton` is selected in the EventSystem,
so this works with the old and the new Input System). `skippable` can be turned off.

Plugging in your GIF player:
- put it under `CutsceneOverlay/CutsceneContent` (delete the placeholder label) and start it from `onStarted` (inspector) or the C# event
  `CutscenePlayer.Started`; stop it in `onFinished` / `Finished(bool skipped)`, which fire when the time runs out or on skip, BEFORE the fade-out;
- or tick off `playOnEnable`, start your GIF yourself and call `Play(float seconds)` with the GIF length (`SetDuration`, `Skip`, `IsPlaying`, `Elapsed` are public too).

Timing and skip state live in `Core/Cutscene/CutsceneTimer.cs` (unit-tested in `Assets/Tests/EditMode/CutsceneTimerTests.cs`).
Note: browsers only start audio after a user gesture, so a GIF-with-sound cutscene in WebGL should expect that.

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
The stub always returns `IsFilled = true`. Because of that, the game flow only calls it when **Done** is pressed - there is no auto-advance, so nothing is judged before then. The quest result itself comes from `QuestEvaluator`, not from this stub.
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
