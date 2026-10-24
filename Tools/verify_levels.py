#!/usr/bin/env python3
"""
Level content source of truth + verifier for the Shape Inventory enchantment quests (10-level campaign, docs/LEVELS.md).

Usage (from the repo root):
    python3 Tools/verify_levels.py            # verify everything, also checks that SampleContentData.cs is in sync
    python3 Tools/verify_levels.py --emit     # (re)write Assets/Scripts/Editor/SampleContentData.cs from the data below

Story / reaction texts come from Tools/level_texts.py (TEXTS[object_id] = {story, success, partial, failure}); this script never
writes texts itself and fails with a clear message when a key is missing.

What is verified for every level (see docs/ENCHANTMENTS.md, "Full result => silhouette 100% filled"):
  a) the intended tiling (explicit multiset in the data) is an exact cover of the silhouette, uses ONLY required types with
     max tier per type == required tier (tier 0 = any), no plain pieces, and is a sub-multiset of the real starting inventory;
  b) the starting inventory itself contains a Full tiling, found with the SAME algorithm as the C# LevelValidator
     (depth-first, first empty cell, biggest piece first) well below its node limit, and the QuestEvaluator replica says Full with
     the silhouette 100% covered;
  c) from L2 on a single unrequired-type piece gives Failure (and fits); from L3 on a Partial arrangement is packable
     (L2 has ONE requirement, where Partial cannot exist by the evaluator rules; there a too-low-tier arrangement gives Failure);
  d) L1: only one enchantment type, no plain, no distractors, everything placed is Full;
  e) tutorial / content rules: L2 two types with one right, L3 three types with two right, plain filler only from L6, wrong types as
     in the doc table, distractor counts per level, item cap, shuffled order, distinct tilings, tier I/II/III distribution sanity.
Pieces are fixed-orientation polyominoes (no rotation). Tier comes from cell count: 1-2 = I, 3 = II, 4+ = III.
"""
import os
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
CS_PATH = os.path.join(ROOT, "Assets", "Scripts", "Editor", "SampleContentData.cs")
sys.dont_write_bytecode = True
sys.path.insert(0, HERE)

# Must match LevelValidator.NodeLimit in C#.
NODE_LIMIT = 400000
# The inventory search of every level must stay far below the C# limit.
NODE_BUDGET = NODE_LIMIT // 10
MAX_ITEMS_PER_LEVEL = 30

# Enum order is serialized: append-only (mirrors EnchantmentType).
ENCH = ["none", "durability", "haste", "sharpness", "efficiency", "luck", "protection"]

# ---------------------------------------------------------------------------------------------- catalogue
# (id, display name, hex colour, rows) - the 14 plain items that already exist as assets (unchanged).
PLAIN_ITEMS = [
    ("i4", "I Piece", "#00BCD4", ["####"]),
    ("i4v", "I Piece (upright)", "#0097A7", ["#", "#", "#", "#"]),
    ("o", "O Piece", "#FDD835", ["##", "##"]),
    ("t", "T Piece", "#AB47BC", ["###", ".#."]),
    ("s", "S Piece", "#66BB6A", [".##", "##."]),
    ("z", "Z Piece", "#EF5350", ["##", ".##"]),
    ("l", "L Piece", "#FFA726", ["#.", "#.", "##"]),
    ("j", "J Piece", "#42A5F5", [".#", ".#", "##"]),
    ("dot", "Dot", "#ECEFF1", ["#"]),
    ("domino", "Domino", "#8D6E63", ["##"]),
    ("dominov", "Domino (upright)", "#BCAAA4", ["#", "#"]),
    ("i3", "Bar 3", "#26A69A", ["###"]),
    ("i3v", "Bar 3 (upright)", "#5C6BC0", ["#", "#", "#"]),
    ("l3", "Corner", "#EC407A", ["##", "#."]),
]

# Extra orientations / mirrors needed because pieces cannot be rotated (id = base id + clockwise rotation in degrees).
# Only the ones that a level actually uses are appended to the generated PlainItems (see emit_csharp), in this order.
EXTRA_ITEMS = [
    ("t90", "T Piece (left)", "#8E24AA", [".#", "##", ".#"]),
    ("t180", "T Piece (up)", "#CE93D8", [".#.", "###"]),
    ("t270", "T Piece (right)", "#7B1FA2", ["#.", "##", "#."]),
    ("s90", "S Piece (upright)", "#43A047", ["#.", "##", ".#"]),
    ("z90", "Z Piece (upright)", "#E53935", [".#", "##", "#."]),
    ("l90", "L Piece (flat)", "#FB8C00", ["###", "#.."]),
    ("l180", "L Piece (hook)", "#FFB74D", ["##", ".#", ".#"]),
    ("l270", "L Piece (roof)", "#F57C00", ["..#", "###"]),
    ("j90", "J Piece (flat)", "#1E88E5", ["#..", "###"]),
    ("j180", "J Piece (hook)", "#64B5F6", ["##", "#.", "#."]),
    ("j270", "J Piece (roof)", "#1976D2", ["###", "..#"]),
    ("l3_90", "Corner (rot 90)", "#D81B60", ["##", ".#"]),
    ("l3_180", "Corner (rot 180)", "#F06292", [".#", "##"]),
    ("l3_270", "Corner (rot 270)", "#C2185B", ["#.", "##"]),
]
CATALOGUE = PLAIN_ITEMS + EXTRA_ITEMS
SHAPES = {pid: rows for (pid, _n, _h, rows) in CATALOGUE}
CATALOGUE_ORDER = [c[0] for c in CATALOGUE]

# ---------------------------------------------------------------------------------------------- objects
# (id, name, hex colour, rows). Hole in the millstone is intended. Cell counts are listed in the comments.
OBJECTS = [
    # Levels 1-3 are tutorial levels: the silhouette is only the part of the tool that is being enchanted.
    ("axe", "Axe", "#B0BEC5", [  # 4: the handle only
        "#",
        "#",
        "#",
        "#"]),
    ("pickaxe", "Pickaxe", "#90A4AE", [  # 6: head + handle
        "###",
        ".#.",
        ".#.",
        ".#."]),
    ("pitchfork", "Pitchfork", "#A1887F", [  # 8: two prongs + crossbar + handle
        "#.#",
        "###",
        ".#.",
        ".#.",
        ".#."]),
    ("scissors", "Scissors", "#CFD8DC", [  # 31
        "##...##",
        ".##.##.",
        "..###..",
        ".##.##.",
        "###.###",
        "#.#.#.#",
        "###.###"]),
    ("hammer", "Hammer", "#8D6E63", [  # 36
        "########",
        "########",
        "########",
        "...##...",
        "...##...",
        "...##...",
        "...##...",
        "...##...",
        "...##..."]),
    ("shovel", "Shovel", "#78909C", [  # 41
        "..###..",
        "..#.#..",
        "..###..",
        "...#...",
        "...#...",
        "...#...",
        "..###..",
        ".#####.",
        "#######",
        "#######",
        ".#####.",
        "..###.."]),
    ("plow", "Plow", "#6D4C41", [  # 46
        "..........##",
        ".........##.",
        "........##..",
        "#########..#",
        "...##....###",
        "...###..##..",
        "..#######...",
        ".########...",
        "#####......."]),
    ("anvil", "Anvil", "#546E7A", [  # 48
        "###########",
        ".##########",
        "...#######.",
        ".....####..",
        ".....####..",
        "....#####..",
        "...#######."]),
    ("scythe", "Scythe", "#BCAAA4", [  # 49
        "........###",
        ".....######",
        "....#######",
        "...########",
        "..#####...#",
        "..###.....#",
        ".####.....#",
        ".###......#",
        "........###",
        "..........#",
        "..........#",
        "..........#"]),
    ("millstone", "Millstone", "#9E9E9E", [  # 60
        "..#####..",
        ".#######.",
        "#########",
        "###...###",
        "###...###",
        "###...###",
        "#########",
        ".#######.",
        "..#####.."]),
]
OBJ = {o[0]: o[3] for o in OBJECTS}
# silhouette cell range per object from docs/LEVELS.md (target range)
CELL_RANGE = {"axe": (4, 4), "pickaxe": (6, 6), "pitchfork": (8, 8), "scissors": (28, 32), "hammer": (32, 36),
              "shovel": (36, 42), "plow": (40, 46), "anvil": (44, 50), "scythe": (48, 54), "millstone": (56, 64)}

# ---------------------------------------------------------------------------------------------- quest table of docs/LEVELS.md
# The requirement table of the doc, used to report deviations (none at the moment).
DOC_REQS = {
    "axe": [("durability", 0)],
    "pickaxe": [("efficiency", 3), ("durability", 1)],
    "pitchfork": [("durability", 2), ("sharpness", 2)],
    "scissors": [("sharpness", 3), ("durability", 0)],
    "hammer": [("durability", 2), ("efficiency", 0), ("haste", 3)],
    "shovel": [("efficiency", 3), ("durability", 1), ("luck", 2)],
    "plow": [("durability", 2), ("efficiency", 3), ("luck", 1), ("protection", 2)],
    "anvil": [("durability", 3), ("protection", 3), ("sharpness", 2), ("haste", 1)],
    "scythe": [("sharpness", 3), ("haste", 2), ("luck", 3), ("efficiency", 1)],
    "millstone": [("durability", 3), ("efficiency", 2), ("protection", 3), ("haste", 1), ("luck", 2)],
}
# unrequired ("wrong") enchantment types in each starting inventory, per the doc
DOC_WRONG = {"axe": [], "pickaxe": ["haste"], "pitchfork": ["haste"], "scissors": ["luck", "haste"],
             "hammer": ["luck", "protection"], "shovel": ["sharpness"], "plow": ["haste"], "anvil": ["luck"],
             "scythe": ["durability", "protection"], "millstone": ["sharpness"]}
# allowed number of distractors per level (1-based), from the doc: L1 none, L2-3 4-6, L4-5 6-8, L6-7 8-10, L8-9 10-12, L10 12-14
DISTRACTOR_RANGE = {1: (0, 0), 2: (1, 1), 3: (2, 2), 4: (6, 8), 5: (6, 8), 6: (8, 10), 7: (8, 10), 8: (10, 12),
                    9: (10, 12), 10: (12, 14)}

# ---------------------------------------------------------------------------------------------- levels
# Every level lists:
#   tiling:      the intended perfect tiling's pieces (the multiset the exact cover must reproduce)
#   distractors: wrong-type pieces, required-type pieces whose size overshoots the required tier, plain filler (from L6 on)
#   order:       the shuffled starting inventory shown to the player (exactly tiling + distractors)
# Entry "ench:shape" = enchanted piece, "shape" = plain piece. Texts are filled in from Tools/level_texts.py.
LEVELS = [
    {   # tutorial: one handle, one stick
        "asset": "Level_01_Axe", "object": "axe", "tutorial": True,
        "reqs": [("durability", 0)],
        "tiling": ["durability:i4v"],
        "distractors": [],
        "order": ["durability:i4v"],
    },
    {   # tutorial: head + handle; 2 right, 1 wrong (an enchantment type the quest never asked for)
        "asset": "Level_02_Pickaxe", "object": "pickaxe", "tutorial": True,
        "reqs": [("efficiency", 3), ("durability", 1)],
        "tiling": ["efficiency:t", "durability:dominov"],
        "distractors": ["haste:dominov"],
        "order": ["haste:dominov", "efficiency:t", "durability:dominov"],
    },
    {   # tutorial: 5 entries, 3 right, 2 wrong (one unrequested type, one right type at the wrong tier)
        "asset": "Level_03_Pitchfork", "object": "pitchfork", "tutorial": True,
        "reqs": [("durability", 2), ("sharpness", 2)],
        "tiling": ["durability:i3v", "sharpness:l3_270", "sharpness:dominov"],
        "distractors": ["haste:dominov", "sharpness:i4v"],
        "order": ["sharpness:dominov", "haste:dominov", "durability:i3v", "sharpness:i4v", "sharpness:l3_270"],
    },
    {
        "asset": "Level_04_Scissors", "object": "scissors",
        "reqs": [("sharpness", 3), ("durability", 0)],
        "tiling": ["durability:dot", "durability:i3", "durability:i4v", "durability:l3_90", "durability:s",
            "sharpness:domino", "sharpness:dot", "sharpness:i3v", "sharpness:l3_180", "sharpness:l3_270",
            "sharpness:z"],
        "distractors": ["luck:dominov", "haste:domino", "luck:l3", "haste:l270", "luck:z90", "haste:dominov",
            "luck:i3v"],
        "order": ["haste:dominov", "luck:l3", "sharpness:dot", "sharpness:l3_180", "durability:i4v", "haste:l270",
            "haste:domino", "luck:z90", "durability:i3", "sharpness:z", "sharpness:l3_270", "durability:s",
            "luck:i3v", "sharpness:domino", "sharpness:i3v", "luck:dominov", "durability:l3_90", "durability:dot"],
    },
    {
        "asset": "Level_05_Hammer", "object": "hammer",
        "reqs": [("durability", 2), ("efficiency", 0), ("haste", 3)],
        "tiling": ["durability:dominov", "durability:dot", "durability:l3_180", "durability:l3_270",
            "efficiency:domino", "efficiency:dot", "efficiency:l270", "efficiency:l90", "efficiency:t90",
            "haste:i4v", "haste:j", "haste:l180"],
        "distractors": ["luck:j90", "protection:l", "luck:o", "protection:t", "luck:l180", "durability:j90",
            "durability:t270", "durability:t"],
        "order": ["efficiency:l90", "durability:j90", "durability:t270", "haste:l180", "protection:t", "haste:j",
            "efficiency:dot", "durability:dot", "luck:o", "durability:t", "luck:j90", "durability:dominov",
            "haste:i4v", "efficiency:domino", "durability:l3_180", "efficiency:l270", "efficiency:t90", "luck:l180",
            "protection:l", "durability:l3_270"],
    },
    {
        "asset": "Level_06_Shovel", "object": "shovel",
        "reqs": [("efficiency", 3), ("durability", 1), ("luck", 2)],
        "tiling": ["durability:domino", "durability:dominov", "efficiency:dot", "efficiency:i4", "efficiency:l",
            "efficiency:l180", "efficiency:l270", "efficiency:s90", "efficiency:t180", "luck:i3", "luck:i3",
            "luck:i3v", "luck:l3_90"],
        "distractors": ["sharpness:domino", "sharpness:j", "sharpness:l3", "durability:i4", "durability:l3_90",
            "luck:s", "l3_180", "l3_270", "i4"],
        "order": ["sharpness:j", "sharpness:l3", "efficiency:l", "sharpness:domino", "durability:dominov",
            "efficiency:s90", "luck:l3_90", "durability:l3_90", "i4", "efficiency:l270", "l3_180", "luck:i3",
            "luck:s", "efficiency:l180", "durability:i4", "durability:domino", "efficiency:dot", "luck:i3v",
            "l3_270", "efficiency:i4", "efficiency:t180", "luck:i3"],
    },
    {
        "asset": "Level_07_Plow", "object": "plow",
        "reqs": [("durability", 2), ("efficiency", 3), ("luck", 1), ("protection", 2)],
        "tiling": ["durability:l3", "durability:l3_180", "durability:l3_180", "durability:l3_90", "efficiency:i4",
            "efficiency:i4", "efficiency:l", "efficiency:t", "efficiency:z90", "luck:domino", "luck:dot",
            "protection:dominov", "protection:i3", "protection:i3v", "protection:l3_180"],
        "distractors": ["haste:l90", "haste:t", "haste:dominov", "durability:j270", "luck:z90", "luck:l3",
            "protection:t180", "z", "i3v", "l3_90"],
        "order": ["protection:l3_180", "haste:l90", "durability:l3", "durability:j270", "efficiency:t",
            "efficiency:i4", "durability:l3_90", "luck:l3", "durability:l3_180", "i3v", "luck:dot", "protection:i3v",
            "haste:t", "efficiency:l", "protection:dominov", "luck:z90", "haste:dominov", "efficiency:z90",
            "efficiency:i4", "protection:t180", "z", "protection:i3", "luck:domino", "l3_90", "durability:l3_180"],
    },
    {
        "asset": "Level_08_Anvil", "object": "anvil",
        "reqs": [("durability", 3), ("protection", 3), ("sharpness", 2), ("haste", 1)],
        "tiling": ["durability:j270", "durability:j90", "durability:l3_90", "durability:s", "durability:t180",
            "haste:domino", "haste:dominov", "protection:dot", "protection:i4", "protection:l270", "protection:s",
            "protection:t", "sharpness:domino", "sharpness:i3", "sharpness:l3_270"],
        "distractors": ["luck:j180", "luck:dominov", "luck:domino", "sharpness:l", "sharpness:j180", "haste:l90",
            "haste:t270", "t180", "z90", "j180", "dominov"],
        "order": ["protection:t", "sharpness:j180", "protection:i4", "haste:t270", "protection:l270",
            "durability:t180", "j180", "z90", "durability:j90", "luck:j180", "luck:dominov", "haste:dominov",
            "dominov", "haste:domino", "durability:j270", "sharpness:domino", "protection:s", "sharpness:i3", "t180",
            "sharpness:l3_270", "luck:domino", "durability:s", "haste:l90", "sharpness:l", "durability:l3_90",
            "protection:dot"],
    },
    {
        "asset": "Level_09_Scythe", "object": "scythe",
        "reqs": [("sharpness", 3), ("haste", 2), ("luck", 3), ("efficiency", 1)],
        "tiling": ["efficiency:domino", "efficiency:dominov", "haste:i3", "haste:i3v", "haste:l3", "haste:l3_180",
            "luck:dot", "luck:i4v", "luck:l", "luck:l3_90", "luck:o", "sharpness:domino", "sharpness:i3",
            "sharpness:s", "sharpness:t180", "sharpness:t90"],
        "distractors": ["durability:dominov", "protection:l3", "durability:domino", "protection:l3_180",
            "durability:domino", "protection:i3", "haste:s", "haste:l180", "efficiency:t270", "efficiency:l3_270",
            "domino", "t"],
        "order": ["protection:l3_180", "luck:dot", "durability:domino", "efficiency:dominov", "haste:l3_180",
            "luck:l", "efficiency:l3_270", "haste:l3", "sharpness:t180", "sharpness:i3", "durability:domino", "t",
            "efficiency:domino", "protection:l3", "efficiency:t270", "haste:l180", "domino", "luck:l3_90",
            "protection:i3", "luck:i4v", "haste:i3v", "sharpness:t90", "durability:dominov", "sharpness:s",
            "sharpness:domino", "haste:s", "haste:i3", "luck:o"],
    },
    {
        "asset": "Level_10_Millstone", "object": "millstone",
        "reqs": [("durability", 3), ("efficiency", 2), ("protection", 3), ("haste", 1), ("luck", 2)],
        "tiling": ["durability:dot", "durability:j", "durability:o", "durability:s", "durability:t270",
            "durability:t90", "efficiency:i3", "efficiency:l3_180", "haste:domino", "haste:dot", "luck:i3",
            "luck:l3", "protection:i4v", "protection:i4v", "protection:j180", "protection:l90", "protection:t180",
            "protection:t90"],
        "distractors": ["sharpness:domino", "sharpness:l180", "sharpness:t90", "efficiency:l180", "efficiency:l",
            "luck:o", "luck:t180", "haste:l", "haste:z", "l", "s90", "i3"],
        "order": ["sharpness:l180", "protection:l90", "durability:t270", "i3", "s90", "haste:z", "luck:o",
            "durability:o", "efficiency:l180", "efficiency:i3", "haste:dot", "luck:i3", "durability:t90", "luck:l3",
            "sharpness:domino", "durability:j", "durability:dot", "sharpness:t90", "protection:t180", "efficiency:l",
            "haste:domino", "protection:i4v", "luck:t180", "protection:t90", "durability:s", "protection:i4v", "l",
            "protection:j180", "efficiency:l3_180", "haste:l"],
    },
]

# ---------------------------------------------------------------------------------------------- texts (owned by Tools/level_texts.py)
TEXT_KEYS = ("story", "success", "partial", "failure")


def load_texts():
    try:
        from level_texts import TEXTS
    except Exception as ex:  # ImportError, SyntaxError, ...
        sys.exit("ERROR: cannot import Tools/level_texts.py (%s: %s). It must define TEXTS = {object_id: {story, success, partial, "
                 "failure}}." % (type(ex).__name__, ex))
    missing = []
    for lvl in LEVELS:
        oid = lvl["object"]
        if oid not in TEXTS:
            missing.append("TEXTS[%r]" % oid)
            continue
        for k in TEXT_KEYS:
            if k not in TEXTS[oid] or not isinstance(TEXTS[oid][k], str):
                missing.append("TEXTS[%r][%r]" % (oid, k))
    if missing:
        sys.exit("ERROR: Tools/level_texts.py is missing text entries: " + ", ".join(missing))
    return TEXTS


# ---------------------------------------------------------------------------------------------- core rules

def cells_of(rows):
    return [(x, y) for y, r in enumerate(rows) for x, c in enumerate(r) if c == "#"]


def normalise(cs):
    mx = min(c[0] for c in cs)
    my = min(c[1] for c in cs)
    return sorted(((c[0] - mx, c[1] - my) for c in cs), key=lambda c: (c[1], c[0]))


SHAPE_CELLS = {k: normalise(cells_of(v)) for k, v in SHAPES.items()}


def tier_of(cell_count):
    """EnchantmentTierRules.GetTier"""
    if cell_count <= 2:
        return 1
    if cell_count == 3:
        return 2
    return 3


def parse_item(entry):
    """'haste:i4v' -> ('haste', 'i4v'); 'i4v' -> (None, 'i4v')"""
    if ":" in entry:
        e, s = entry.split(":")
        assert e in ENCH and e != "none", entry
        assert s in SHAPES, entry
        return e, s
    assert entry in SHAPES, entry
    return None, entry


def item_id(piece):
    e, s = piece
    return s if e is None else e + "_" + s


def evaluate(placed, reqs):
    """Replica of QuestEvaluator rules. placed: list of (ench or None, shape id). Returns (outcome, score, max_score, wrong)."""
    applied = {}
    for e, s in placed:
        if e is None:
            continue
        t = tier_of(len(SHAPE_CELLS[s]))
        if t > applied.get(e, 0):
            applied[e] = t
    score = 0
    for e, t in reqs:
        if e in applied and (t <= 0 or applied[e] == t):
            score += 1
    req_types = set(e for e, _ in reqs)
    wrong = sum(1 for e in applied if e not in req_types)
    max_score = len(reqs)
    if wrong > 0:
        outcome = "Failure"
    elif score == max_score:
        outcome = "Full"
    elif score >= 1:
        outcome = "Partial"
    else:
        outcome = "Failure"
    if outcome == "Failure":
        score = 0  # a failed quest never scores (QuestEvaluator)
    return outcome, score, max_score, wrong


# ---------------------------------------------------------------------------------------------- search
# Search mirrors Assets/Scripts/Editor/LevelValidator.cs: depth-first exact cover, first empty silhouette cell (row-major)
# gets one of the distinct available pieces (largest first, ties by first occurrence in the inventory), strict mode =
# required types only and no piece above the required tier.

class Search(object):
    def __init__(self, silhouette_rows, items, reqs, allow_plain=False, node_limit=NODE_LIMIT):
        self.sil = set(cells_of(silhouette_rows))
        self.order = sorted(self.sil, key=lambda c: (c[1], c[0]))
        self.reqs = reqs
        self.req_tier = {}
        for e, t in reqs:
            self.req_tier[e] = t
        self.allow_plain = allow_plain
        self.node_limit = node_limit
        self.nodes = 0
        self.aborted = False
        distinct = []
        counts = {}
        for it in items:
            if it not in counts:
                counts[it] = 0
                distinct.append(it)
            counts[it] += 1
        distinct = [d for d in distinct if self._usable(d)]
        distinct.sort(key=lambda d: -len(SHAPE_CELLS[d[1]]))  # python sort is stable
        self.distinct = distinct
        self.remaining = {d: counts[d] for d in distinct}
        self.occupied = set()
        self.placed = []  # list of ((ench, shape), origin)

    def _usable(self, piece):
        e, s = piece
        if e is None:
            return self.allow_plain
        if e not in self.req_tier:
            return False
        t = self.req_tier[e]
        if t > 0 and tier_of(len(SHAPE_CELLS[s])) > t:
            return False
        return True

    def run(self):
        return self._rec(len(self.sil))

    def _rec(self, empty_left):
        self.nodes += 1
        if self.nodes > self.node_limit:
            self.aborted = True
            return False
        if empty_left == 0:
            return evaluate([p for p, _ in self.placed], self.reqs)[0] == "Full"
        first = None
        for c in self.order:
            if c not in self.occupied:
                first = c
                break
        for d in self.distinct:
            if self.remaining[d] <= 0:
                continue
            cs = SHAPE_CELLS[d[1]]
            if len(cs) > empty_left:
                continue
            ox, oy = first[0] - cs[0][0], first[1] - cs[0][1]
            absolute = [(ox + x, oy + y) for x, y in cs]
            if any(p not in self.sil or p in self.occupied for p in absolute):
                continue
            self.remaining[d] -= 1
            self.occupied.update(absolute)
            self.placed.append((d, (ox, oy)))
            if self._rec(empty_left - len(cs)):
                return True
            self.placed.pop()
            self.occupied.difference_update(absolute)
            self.remaining[d] += 1
            if self.aborted:
                return False
        return False


def exact_cover(silhouette_rows, shape_ids, node_limit=200000):
    """Independent exact cover of the silhouette by the multiset shape_ids (most-constrained cell first).
    Returns (list of (shape, origin) or None, nodes, aborted)."""
    sil = set(cells_of(silhouette_rows))
    remaining = {}
    for s in shape_ids:
        remaining[s] = remaining.get(s, 0) + 1
    # every placement (shape, origin, absolute cells) that lies inside the silhouette
    placements = []
    for s in remaining:
        cs = SHAPE_CELLS[s]
        for (ax, ay) in sil:
            ox, oy = ax - cs[0][0], ay - cs[0][1]
            absolute = [(ox + x, oy + y) for x, y in cs]
            if all(p in sil for p in absolute):
                placements.append((s, (ox, oy), frozenset(absolute)))
    by_cell = {c: [] for c in sil}
    for pl in placements:
        for c in pl[2]:
            by_cell[c].append(pl)
    state = {"nodes": 0, "aborted": False}
    occ = set()
    chosen = []

    def rec():
        state["nodes"] += 1
        if state["nodes"] > node_limit:
            state["aborted"] = True
            return False
        if len(occ) == len(sil):
            return True
        best = None
        for c in sil:
            if c in occ:
                continue
            opts = [p for p in by_cell[c] if remaining[p[0]] > 0 and not (p[2] & occ)]
            if best is None or len(opts) < len(best):
                best = opts
                if len(opts) <= 1:
                    break
        for p in best:
            remaining[p[0]] -= 1
            occ.update(p[2])
            chosen.append((p[0], p[1]))
            if rec():
                return True
            chosen.pop()
            occ.difference_update(p[2])
            remaining[p[0]] += 1
            if state["aborted"]:
                return False
        return False

    ok = rec()
    return (list(chosen) if ok else None), state["nodes"], state["aborted"]


def can_pack(silhouette_rows, pieces):
    """True if all pieces can be placed without overlap inside the silhouette (any positions)."""
    sil = set(cells_of(silhouette_rows))
    order = sorted(sil, key=lambda c: (c[1], c[0]))
    occ = set()
    pieces = sorted(pieces, key=lambda p: -len(SHAPE_CELLS[p[1]]))

    def rec(i):
        if i == len(pieces):
            return True
        cs = SHAPE_CELLS[pieces[i][1]]
        for a in order:
            ox, oy = a[0] - cs[0][0], a[1] - cs[0][1]
            absolute = [(ox + x, oy + y) for x, y in cs]
            if all(p in sil and p not in occ for p in absolute):
                occ.update(absolute)
                if rec(i + 1):
                    return True
                occ.difference_update(absolute)
        return False

    return rec(0)


# ---------------------------------------------------------------------------------------------- checks

def multiset(entries):
    d = {}
    for e in entries:
        d[e] = d.get(e, 0) + 1
    return d


def is_connected(cells):
    cells = set(cells)
    if not cells:
        return False
    stack = [next(iter(cells))]
    seen = set(stack)
    while stack:
        x, y = stack.pop()
        for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if n in cells and n not in seen:
                seen.add(n)
                stack.append(n)
    return len(seen) == len(cells)


def tier_name(t):
    return "any" if t == 0 else "I" * t if t < 3 else "III"


def max_run(seq):
    best = run = 0
    prev = object()
    for x in seq:
        run = run + 1 if x == prev else 1
        prev = x
        best = max(best, run)
    return best


def verify():
    errors = []
    warnings = []

    def check(cond, msg):
        if not cond:
            errors.append(msg)
        return cond

    texts = load_texts()

    # ---- catalogue
    ids = [c[0] for c in CATALOGUE]
    check(len(set(ids)) == len(ids), "catalogue: duplicate shape ids")
    seen_shapes = {}
    for pid, name, hexc, rows in CATALOGUE:
        check(all(ch in "abcdefghijklmnopqrstuvwxyz0123456789_" for ch in pid) and pid, "catalogue: bad id " + pid)
        check(len(hexc) == 7 and hexc[0] == "#", "catalogue: bad colour of " + pid)
        check(is_connected(cells_of(rows)), "catalogue: shape not connected: " + pid)
        key = tuple(SHAPE_CELLS[pid])
        check(key not in seen_shapes, "catalogue: %s duplicates the shape of %s" % (pid, seen_shapes.get(key)))
        seen_shapes[key] = pid
    check(ids[:14] == ["i4", "i4v", "o", "t", "s", "z", "l", "j", "dot", "domino", "dominov", "i3", "i3v", "l3"],
          "catalogue: the 14 existing plain items must stay first and unchanged")

    # ---- objects
    obj_ids = [o[0] for o in OBJECTS]
    check(obj_ids == ["axe", "pickaxe", "pitchfork", "scissors", "hammer", "shovel", "plow", "anvil", "scythe", "millstone"],
          "objects: expected the ten objects of docs/LEVELS.md in order")
    for oid, name, hexc, rows in OBJECTS:
        n = len(cells_of(rows))
        lo, hi = CELL_RANGE[oid]
        check(lo <= n <= hi, "object %s: %d cells outside the doc range %d-%d" % (oid, n, lo, hi))
        check(len(rows) <= 12 and max(len(r) for r in rows) <= 12, "object %s: larger than 12x12" % oid)
        check(len(set(len(r) for r in rows)) == 1, "object %s: ragged rows" % oid)
        check(is_connected(cells_of(rows)), "object %s: silhouette is not one connected piece" % oid)
        check(all(set(r) <= set("#.") for r in rows), "object %s: bad characters" % oid)
    check(len(set(tuple(o[3]) for o in OBJECTS)) == len(OBJECTS), "objects: duplicate silhouettes")
    check([l["object"] for l in LEVELS] == obj_ids, "levels must follow the object order")
    check([l["asset"] for l in LEVELS] == ["Level_%02d_%s" % (i + 1, o.capitalize()) for i, o in enumerate(obj_ids)],
          "level asset names must be Level_01_Axe ... Level_10_Millstone")

    signatures = {}
    all_tier_use = set()
    t_start = time.time()
    for li, lvl in enumerate(LEVELS):
        num = li + 1
        name = lvl["asset"]
        oid = lvl["object"]
        sil = OBJ[oid]
        sil_cells = len(cells_of(sil))
        reqs = lvl["reqs"]
        tiling = [parse_item(e) for e in lvl["tiling"]]
        distractors = [parse_item(e) for e in lvl["distractors"]]
        items = [parse_item(e) for e in lvl["order"]]
        req_tier = dict(reqs)
        req_types = set(req_tier)
        print("--- %s (%s, %d cells) requirements: %s" % (
            name, oid, sil_cells, ", ".join("%s %s" % (e, tier_name(t)) for e, t in reqs)))

        # doc table deviations (informational)
        if reqs != DOC_REQS[oid]:
            print("  DEVIATION from docs/LEVELS.md requirement table: doc %s" % (DOC_REQS[oid],))

        # texts
        for k in TEXT_KEYS:
            lvl[k] = texts[oid][k]

        # ---- bookkeeping
        check(len(set(e for e, _ in reqs)) == len(reqs), name + ": duplicate required types")
        check(1 <= len(reqs) <= 5 and all(e in ENCH[1:] for e in req_types), name + ": bad requirement list")
        check(all(0 <= t <= 3 for _, t in reqs), name + ": bad tier")
        check(len(items) <= MAX_ITEMS_PER_LEVEL, name + ": too many items (%d)" % len(items))
        check(multiset(lvl["order"]) == multiset(lvl["tiling"] + lvl["distractors"]),
              name + ": order must be exactly tiling + distractors")
        lo, hi = DISTRACTOR_RANGE[num]
        check(lo <= len(distractors) <= hi, "%s: %d distractors, expected %d-%d" % (name, len(distractors), lo, hi))
        for e, s in tiling:
            check(e in req_types, name + ": tiling piece of unrequired type %s:%s" % (e, s))
        check(sum(len(SHAPE_CELLS[s]) for _, s in tiling) == sil_cells, name + ": tiling cell count != silhouette")
        check(all(e is not None for e, _ in tiling), name + ": plain piece in the tiling")

        # types in the inventory vs the doc table
        inv_types = set(e for e, _ in items if e is not None)
        wrong_types = inv_types - req_types
        check(req_types <= inv_types, name + ": a required type is missing from the inventory")
        check(wrong_types == set(DOC_WRONG[oid]), "%s: wrong types %s, doc says %s" % (name, sorted(wrong_types), DOC_WRONG[oid]))
        plain_count = sum(1 for e, _ in items if e is None)
        if num <= 5:
            check(plain_count == 0, name + ": no plain pieces allowed before L6")
        else:
            check(plain_count >= 1, name + ": plain filler expected from L6 on")
        # tutorial rules (levels 1-3: partial tool silhouettes, small inventories)
        tutorial = bool(lvl.get("tutorial"))
        if num == 1:
            check(len(items) == 1 and len(tiling) == 1 and not distractors and len(reqs) == 1 and plain_count == 0,
                  "L1: the inventory is exactly ONE correct piece (the stick)")
            check(inv_types == {"durability"} and reqs == [("durability", 0)], "L1: only durability, any tier")
        if num == 2:
            check(len(items) == 3 and len(tiling) == 2 and len(distractors) == 1 and len(reqs) == 2,
                  "L2: 3 inventory entries = 2 correct + 1 wrong, 2 requirements")
            check(len(wrong_types) == 1, "L2: the wrong piece is of an unrequested type")
        if num == 3:
            check(len(items) == 5 and len(tiling) == 3 and len(distractors) == 2 and len(reqs) == 2,
                  "L3: 5 inventory entries = 3 correct + 2 wrong")
            check(len(wrong_types) == 1 and any(d[0] in req_types for d in distractors),
                  "L3: one wrong piece of an unrequested type and one of a required type at the wrong tier")
        # overshoot decoys: a piece of a required type above the required tier (impossible for tier III / any)
        overshoot = [d for d in distractors if d[0] in req_tier and req_tier[d[0]] > 0
                     and tier_of(len(SHAPE_CELLS[d[1]])) > req_tier[d[0]]]
        possible = any(0 < t < 3 for _, t in reqs)
        other = [d for d in distractors if d[0] is not None and d[0] in req_tier and d not in overshoot]
        check(not other, name + ": required-type distractors must overshoot the required tier (else they could be usable)")
        if num >= 3 and possible:
            check(len(overshoot) >= 1, name + ": expected wrong-tier decoys")
        # shuffled order, not grouped by type
        kinds = [e or "plain" for e, _ in items]
        if len(set(kinds)) > 1:
            check(max_run(kinds) <= 2, name + ": order is grouped by type (run of %d)" % max_run(kinds))
        check(len(items) < 3 or lvl["order"] != sorted(lvl["order"]), name + ": order looks sorted")

        # ---- tier sanity of the intended tiling
        sizes_by_type = {}
        for e, s in tiling:
            sizes_by_type.setdefault(e, []).append(len(SHAPE_CELLS[s]))
        for e, t in reqs:
            sz = sizes_by_type.get(e, [])
            check(len(sz) >= 1, "%s: tiling has no %s piece" % (name, e))
            if not sz:
                continue
            if num >= 2 and len(tiling) >= 4 * len(reqs):
                check(len(sz) >= 2, "%s: tiling uses only one %s piece" % (name, e))
            if t == 1:
                check(max(sz) <= 2, "%s: %s tier I but piece with %d cells" % (name, e, max(sz)))
            elif t == 2:
                check(max(sz) == 3, "%s: %s tier II needs max piece 3 cells, got %d" % (name, e, max(sz)))
            elif t == 3:
                check(max(sz) >= 4, "%s: %s tier III needs a 4+ cell piece" % (name, e))
            all_tier_use.add(t)
        shapes_used = set(s for _, s in tiling)
        if not tutorial:
            check(len(shapes_used) >= max(4, len(tiling) // 3), "%s: tiling uses too few different shapes" % name)
        print("  tiling: %d pieces (cells x count: %s), %d different shapes" % (
            len(tiling), ", ".join("%dx%d" % (k, v) for k, v in sorted(multiset([len(SHAPE_CELLS[s]) for _, s in tiling]).items())),
            len(shapes_used)))

        # ---- a) the intended tiling is an exact cover, sub-multiset of the inventory
        inv_ms = multiset(lvl["order"])
        til_ms = multiset(lvl["tiling"])
        check(all(inv_ms.get(k, 0) >= v for k, v in til_ms.items()), name + ": (a) tiling is not a sub-multiset of the inventory")
        cover, nodes_a, aborted_a = exact_cover(sil, [s for _, s in tiling])
        check(cover is not None, name + ": (a) intended tiling does not tile the silhouette%s" % (" (search aborted)" if aborted_a else ""))
        print("  a) intended tiling is an exact cover: %s (%d nodes)" % (cover is not None, nodes_a))

        # ---- b) the real starting inventory, same algorithm as the C# validator
        sb = Search(sil, items, reqs)
        okb = sb.run()
        check(okb, name + ": (b) starting items contain no Full tiling")
        check(not sb.aborted, name + ": (b) search hit the node limit")
        check(sb.nodes <= NODE_BUDGET, "%s: (b) search needs %d nodes, budget %d (C# limit %d)" % (name, sb.nodes, NODE_BUDGET, NODE_LIMIT))
        print("  b) inventory (%d items = %d tiling + %d distractors) contains a Full tiling: %s (%d nodes, C# limit %d)" % (
            len(items), len(tiling), len(distractors), okb, sb.nodes, NODE_LIMIT))
        if okb:
            placed_items = [p for p, _ in sb.placed]
            outcome, score, max_score, wrong = evaluate(placed_items, reqs)
            check(outcome == "Full" and score == max_score and wrong == 0, name + ": (b) found tiling is not Full")
            check(len(sb.occupied) == len(sb.sil), name + ": (b) silhouette not 100% filled")
            check(all(p[0] is not None for p in placed_items), name + ": (b) plain piece in the strict tiling")
            used_ms = multiset([item_id(p) for p in placed_items])
            have_ms = multiset([item_id(p) for p in items])
            check(all(have_ms.get(k, 0) >= v for k, v in used_ms.items()), name + ": (b) tiling uses pieces the inventory lacks")
            print("     evaluator: %s %d/%d wrong %d; silhouette %d/%d filled with %d pieces" % (
                outcome, score, max_score, wrong, len(sb.occupied), len(sb.sil), len(placed_items)))
            sig = tuple(sorted((p[0], p[1], o) for p, o in sb.placed))
            check(sig not in signatures.values(), name + ": identical tiling to another level")
            signatures[name] = sig

        # ---- c) failure / partial arrangements
        if num >= 2:
            wrong_pieces = [d for d in distractors if d[0] is not None and d[0] not in req_types]
            check(len(wrong_pieces) >= 1, name + ": (c) no unrequired-type distractor")
            for d in wrong_pieces:
                check(can_pack(sil, [d]) and evaluate([d], reqs)[0] == "Failure", "%s: (c) %s does not give Failure" % (name, item_id(d)))
                check(evaluate([d, tiling[0]], reqs)[0] == "Failure", "%s: (c) %s + a right piece must be Failure" % (name, item_id(d)))
            print("  c) a single unrequired piece (%d kinds) gives Failure" % len(set(wrong_pieces)))
        if num >= 2:
            partial_single = [t for t in tiling if evaluate([t], reqs)[0] == "Partial" and can_pack(sil, [t])]
            check(partial_single, name + ": (c) no single right piece gives Partial")
            desc = ""
            for o in overshoot:
                for t in tiling:
                    if t[0] != o[0] and can_pack(sil, [o, t]) and evaluate([o, t], reqs)[0] == "Partial":
                        desc = "%s + %s" % (item_id(o), item_id(t))
                        break
                if desc:
                    break
            if overshoot and not tutorial:
                check(desc, name + ": (c) no Partial arrangement involving a wrong-tier decoy")
            # a type placed at the wrong tier gives no point and no failure
            print("  c) Partial packable: single %s%s" % (item_id(partial_single[0]) if partial_single else "-",
                                                         "; with decoy " + desc if desc else ""))

        # ---- d) level 1: the single stick is the whole inventory
        if num == 1:
            check(evaluate(list(items), reqs)[0] == "Full", "L1: placing the stick must be Full")
            check(evaluate([], reqs)[0] == "Failure", "L1: an empty object must be Failure")
            print("  d) L1: one durability stick; placing it is Full, placing nothing is Failure")

    # ---- campaign level checks
    check({1, 2, 3} <= all_tier_use, "campaign should use tier I, II and III requirements")
    check(len(signatures) == len(LEVELS), "not every level produced a tiling")
    print("\nverification took %.1f s" % (time.time() - t_start))
    return errors, warnings


# ---------------------------------------------------------------------------------------------- C# emission

def cs_str(s):
    out = ['"']
    for ch in s:
        if ch == "\\":
            out.append("\\\\")
        elif ch == '"':
            out.append('\\"')
        elif ch == "\n":
            out.append("\\n")
        elif ch == "\r":
            out.append("\\r")
        elif ch == "\t":
            out.append("\\t")
        elif ord(ch) < 0x20:
            out.append("\\u%04x" % ord(ch))
        else:
            out.append(ch)
    out.append('"')
    return "".join(out)


def cs_rows(rows):
    return "new[] { " + ", ".join(cs_str(r) for r in rows) + " }"


def cs_type(e):
    return "EnchantmentType." + e.capitalize()


def emit_csharp():
    used = set()
    used_shapes = set()
    for lvl in LEVELS:
        for entry in lvl["order"]:
            e, s = parse_item(entry)
            used_shapes.add(s)
            if e is not None:
                used.add((e, s))
    plain = list(PLAIN_ITEMS) + [x for x in EXTRA_ITEMS if x[0] in used_shapes]
    enchanted = sorted(used, key=lambda es: (ENCH.index(es[0]), CATALOGUE_ORDER.index(es[1])))

    out = []
    w = out.append
    w("// <auto-generated>")
    w("// Generated by Tools/verify_levels.py --emit. DO NOT EDIT BY HAND: edit the data in that script, verify, re-emit.")
    w("// The level piece lists below are the ones the Python exact-cover verifier proved solvable (Full outcome = silhouette 100% filled).")
    w("// </auto-generated>")
    w("using ShapeInventory.Core;")
    w("")
    w("namespace ShapeInventory.Editor")
    w("{")
    w("    internal static class SampleContentData")
    w("    {")
    w("        internal static readonly ItemDef[] PlainItems =")
    w("        {")
    for pid, name, hexc, rows in plain:
        w("            new ItemDef(%s, %s, %s, %s)," % (cs_str(pid), cs_str(name), cs_str(hexc), cs_rows(rows)))
    w("        };")
    w("")
    w("        // Item_<type>_<shape> assets; colour = EnchantmentStyle.GetColor(type), shape taken from the plain item with that id.")
    w("        internal static readonly EnchantedItemDef[] EnchantedItems =")
    w("        {")
    for e, s in enchanted:
        w("            new EnchantedItemDef(%s, %s)," % (cs_type(e), cs_str(s)))
    w("        };")
    w("")
    w("        internal static readonly ObjectDef[] Objects =")
    w("        {")
    for oid, name, hexc, rows in OBJECTS:
        w("            new ObjectDef(%s, %s, %s, %s)," % (cs_str(oid), cs_str(name), cs_str(hexc), cs_rows(rows)))
    w("        };")
    w("")
    w("        // Item ids: a plain item id (\"i4\") or an enchanted id (\"<type>_<shape>\", e.g. \"sharpness_i4\").")
    w("        internal static readonly LevelDef[] Levels =")
    w("        {")
    for lvl in LEVELS:
        w("            new LevelDef(")
        w("                %s, %s," % (cs_str(lvl["asset"]), cs_str(lvl["object"])))
        w("                %s," % cs_str(lvl["story"]))
        w("                new[]")
        w("                {")
        for e, t in lvl["reqs"]:
            w("                    new EnchantmentRequirement(%s, %d)," % (cs_type(e), t))
        w("                },")
        w("                new[]")
        w("                {")
        for entry in lvl["order"]:
            e, s = parse_item(entry)
            w("                    %s," % cs_str(item_id((e, s))))
        w("                },")
        w("                %s," % cs_str(lvl["success"]))
        w("                %s," % cs_str(lvl["partial"]))
        w("                %s)," % cs_str(lvl["failure"]))
    w("        };")
    w("    }")
    w("}")
    return "\n".join(out) + "\n"


def main():
    emit = "--emit" in sys.argv
    errors, _warnings = verify()
    text = emit_csharp()
    if emit:
        if errors:
            print("\nNOT emitting: verification failed")
        else:
            with open(CS_PATH, "w", newline="\n", encoding="utf-8") as f:
                f.write(text)
            print("\nwrote " + os.path.relpath(CS_PATH, ROOT))
    else:
        if os.path.exists(CS_PATH):
            with open(CS_PATH, newline="", encoding="utf-8") as f:
                current = f.read()
            if current != text:
                errors.append("SampleContentData.cs is out of sync with this script (run with --emit)")
            else:
                print("\nSampleContentData.cs is in sync with the verified data.")
        else:
            errors.append("SampleContentData.cs does not exist (run with --emit)")
    if errors:
        print("\nFAILED:")
        for e in errors:
            print("  - " + e)
        sys.exit(1)
    print("\nALL LEVELS OK")


if __name__ == "__main__":
    main()
