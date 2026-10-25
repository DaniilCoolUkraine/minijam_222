# 10-level campaign - design spec (shared by all agents)

Levels 1-3 are tutorials with partial-tool silhouettes (only the part being enchanted); levels 4-10 use the full tools.
Replaces the five old sample levels (rocket, tree, boat, house, cat). Rules of the game are in `docs/ENCHANTMENTS.md`
(tier from item size: 1-2 cells = I, 3 = II, 4+ = III; max tier per type counts; Full / Partial / Failure; every outcome advances).
Pieces cannot be rotated: every orientation a level needs must exist as its own piece shape.

## The ten objects (village tools, smallest first, millstone last)
| # | object id | name | colour hint | silhouette cells | what it is |
|---|---|---|---|---|---|
| 1 | axe | Axe | #B0BEC5 | 4 | tutorial: the HANDLE only (1x4 stick) |
| 2 | pickaxe | Pickaxe | #90A4AE | 6 | tutorial: T-shaped head + handle |
| 3 | pitchfork | Pitchfork | #A1887F | 8 | tutorial: two prongs, crossbar, handle |
| 4 | scissors | Scissors | #CFD8DC | 28-32 | two blades and two loops |
| 5 | hammer | Hammer | #8D6E63 | 32-36 | heavy head on a handle |
| 6 | shovel | Shovel | #78909C | 36-42 | spade blade, long shaft, D-grip |
| 7 | plow | Plow | #6D4C41 | 40-46 | beam, share (blade) and two handles |
| 8 | anvil | Anvil | #546E7A | 44-50 | horn, face, waist, base |
| 9 | scythe | Scythe | #BCAAA4 | 48-54 | long curved blade on a long snath |
| 10 | millstone | Millstone | #9E9E9E | 56-64 | round disc with a hole in the middle |

Silhouettes are drawn as ASCII rows (`#` cell, `.` empty), top row first, at most 12 columns x 12 rows, one connected piece
(a hole inside is allowed), recognisable at a glance. Cell counts are a target range, not exact.

## The ten quests (type : tier, tier 0 = any tier) - default table, the levels agent may adjust a tier if a tiling is impossible and must report it
Types: durability, haste, sharpness, efficiency, luck, protection.

| # | object | requirements | types present in the starting inventory | extras in the inventory |
|---|---|---|---|---|
| 1 | axe | durability:0 | 1 inventory entry: durability i4v (the stick) | tutorial. Nothing else. Placing it = Full, placing nothing = Failure |
| 2 | pickaxe | efficiency:3, durability:1 | 3 entries: efficiency T (head, 4 cells) + durability domino (handle, 2 cells), plus 1 wrong: haste domino | tutorial. Tiers start here; Partial = only one of the two right pieces |
| 3 | pitchfork | durability:2, sharpness:2, efficiency:1 | 5 entries: durability bar-3 (handle), sharpness corner (left prong, 3 cells), efficiency domino (right prong), plus 2 wrong: haste domino and sharpness 4-bar | tutorial. Each part has its own requirement, so Full needs all three pieces (= filled shape). The 4-bar is the right type at the wrong tier (III instead of II) |
| 4 | scissors | sharpness:3, durability:0 | sharpness + durability (right), luck + haste (wrong) | 1-2 wrong-tier decoys; no plain pieces |
| 5 | hammer | durability:2, efficiency:0, haste:3 | the three right ones, luck + protection (wrong) | wrong-tier decoys; no plain pieces |
| 6 | shovel | efficiency:3, durability:1, luck:2 | the three right ones + sharpness (wrong) | wrong-tier decoys, plain filler pieces start here |
| 7 | plow | durability:2, efficiency:3, luck:1, protection:2 | four right + haste (wrong) | wrong-tier decoys, plain filler |
| 8 | anvil | durability:3, protection:3, sharpness:2, haste:1 | four right + luck (wrong) | more decoys, plain filler |
| 9 | scythe | sharpness:3, haste:2, luck:3, efficiency:1 | four right + durability + protection (wrong) | many decoys, plain filler |
| 10 | millstone | durability:3, efficiency:2, protection:3, haste:1, luck:2 | five right + sharpness (wrong) | the most decoys, plain filler; biggest silhouette |

"Wrong-tier decoy" = a piece of a REQUIRED type whose size would overshoot the required tier (e.g. a 4-cell haste piece when haste:1 or
haste:2 is required) or, when the required tier is III, simply a type that is missing its big piece unless the player finds it.
Remember: tier I of a type means every placed piece of that type has at most 2 cells; the player must be able to see which pieces are safe.

## Content rules (all levels)
- The Full tiling must exist among the starting items: pieces of REQUIRED types only, no plain pieces, max tier per type == required tier
  (tier 0: any), together covering the silhouette EXACTLY (100% filled). The Full result is the player's hint.
- Inventory = the tiling pieces + distractors. Total <= 30 items per level (the controller adds rows as needed). Distractors scale with the level:
  L1 none, L2 exactly 1, L3 exactly 2 (tutorial, see the table), L4-5 about 6-8, L6-7 about 8-10, L8-9 about 10-12, L10 about 12-14.
- A single unrequired-type piece placed anywhere must produce Failure; a partial result (>=1 requirement right, nothing wrong) must be possible
  in every level from L2 on.
- Distinct tilings per level; shuffle the shown order ("order"), do not list pieces grouped by type.
- Plain piece ids that already exist as assets keep their ids (i4, i4v, o, t, s, z, l, j, dot, domino, dominov, i3, i3v, l3). NEW plain shapes
  (extra orientations / mirrors of the tetrominoes, trominoes, etc.) may be appended to the catalogue with new lowercase ids [a-z0-9_]+.

## Ownership (parallel agents)
| Area | Owner |
|---|---|
| `Tools/verify_levels.py`, `Assets/Scripts/Editor/SampleContentData.cs` (generated by `python3 Tools/verify_levels.py --emit`) | Agent S (levels data + solver) |
| `Tools/level_texts.py` | Agent T (stories + reaction texts) |
| `Assets/Scripts/Editor/**` EXCEPT `SampleContentData.cs` | Agent U (editor tooling) |
| everything else | frozen |

Interface S <-> T: `Tools/level_texts.py` defines `TEXTS = {object_id: {"story": str, "success": str, "partial": str, "failure": str}}` for all ten object ids;
`verify_levels.py` imports it (`from level_texts import TEXTS`) and fails with a clear message when a key is missing.
Interface S <-> U: the generated `SampleContentData.cs` keeps exactly the current public shape (same classes from `SampleContentDefs.cs`, same
member names); Agent S must not require changes in Agent U's files. Level asset names: `Level_01_Axe` ... `Level_10_Millstone`;
object assets `Object_<id>`; sprites `Object_<id>.png`.
