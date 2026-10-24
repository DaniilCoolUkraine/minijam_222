#!/usr/bin/env python3
"""
Level content source of truth + verifier for the Shape Inventory enchantment quests.

Usage (from the repo root):
    python3 Tools/verify_levels.py            # verify everything, also checks that SampleContentData.cs is in sync
    python3 Tools/verify_levels.py --emit     # (re)write Assets/Scripts/Editor/SampleContentData.cs from the data below

What is verified for every level (see docs/ENCHANTMENTS.md, "Full result => silhouette 100% filled"):
  a) a tiling of the silhouette exists that uses ONLY pieces of the required enchantment types, with the max tier per
     type equal to the required tier (tier 0 = any), pieces drawn from the whole shape catalogue;
  b) the level's starting items contain such a tiling (exact cover found with the SAME algorithm as the C# LevelValidator),
     and the intended tiling listed in the data is itself an exact cover;
  c) the evaluator rules (replica of QuestEvaluator) return Full for that arrangement;
  d) a distractor-only failure and a partial result that involves a distractor are possible;
  plus bookkeeping: item counts, distinct tilings across levels, node counts of the search under the C# limit.
Pieces are fixed-orientation polyominoes (no rotation). Tier comes from cell count: 1-2 = I, 3 = II, 4+ = III.
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
CS_PATH = os.path.join(ROOT, "Assets", "Scripts", "Editor", "SampleContentData.cs")

# Must match LevelValidator.NodeLimit in C#.
NODE_LIMIT = 400000
MAX_ITEMS_PER_LEVEL = 24

# Enum order is serialized: append-only (mirrors EnchantmentType).
ENCH = ["none", "durability", "haste", "sharpness", "efficiency", "luck", "protection"]

# ---------------------------------------------------------------------------------------------- catalogue
# (id, display name, hex colour, rows) - the 14 plain items that already exist as assets.
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
SHAPES = {pid: rows for (pid, _n, _h, rows) in PLAIN_ITEMS}

OBJECTS = [
    ("rocket", "Rocket", "#B0BEC5", [
        "..#..", ".###.", ".###.", ".###.", ".###.", ".###.", "#####", "#.#.#"]),
    ("tree", "Tree", "#81C784", [
        "..###..", ".#####.", "#######", ".#####.", "..###..", "...#...", "...#...", "...#..."]),
    ("boat", "Boat", "#90CAF9", [
        "...#.....", "...##....", "...###...", "...####..", "#########", ".#######.", "..#####.."]),
    ("house", "House", "#FFCC80", [
        "....#....", "...###...", "..#####..", ".#######.", ".#######.", ".###.###.", ".###.###."]),
    ("cat", "Cat", "#CE93D8", [
        "#.....#", "##...##", "#######", "#######", "#######", ".#####.", "..###.."]),
]
OBJ = {o[0]: o[3] for o in OBJECTS}

# ---------------------------------------------------------------------------------------------- levels
# items: explicit starting inventory (order as shown to the player). Entry "ench:shape" = enchanted, "shape" = plain.
# tiling: the intended perfect tiling's pieces (a sub-multiset of items); everything else is a distractor.
LEVELS = [
    {
        "asset": "Level_01_Rocket", "object": "rocket",
        "reqs": [("haste", 3), ("durability", 1), ("efficiency", 0)],
        "story": "Captain Pip here! My rocket rattles like a tin can on every launch, so a little more sturdiness would be nice. "
                 "But I want it SO fast that I overtake the moon! Oh, and she guzzles fuel, so please do something about that too.",
        "success": "Whoooosh! I overtook the moon and I still have fuel left!",
        "partial": "Okay, it flies. Not what I imagined, but fine.",
        "failure": "It exploded on the launch pad! My eyebrows are gone!",
        "tiling": ["durability:dominov", "durability:dominov", "efficiency:domino", "efficiency:dot", "efficiency:i4v",
                   "efficiency:o", "haste:dominov", "haste:i3v", "haste:j"],
        "distractors": ["durability:i3v", "luck:dominov", "sharpness:o", "protection:t", "t", "l3"],
        "order": [
            "efficiency:o", "t", "haste:dominov", "durability:i3v", "efficiency:domino", "haste:j", "luck:dominov",
            "durability:dominov", "efficiency:i4v", "l3", "haste:i3v", "protection:t", "efficiency:dot",
            "durability:dominov", "sharpness:o"],
    },
    {
        "asset": "Level_02_Tree", "object": "tree",
        "reqs": [("luck", 3), ("protection", 2), ("durability", 0)],
        "story": "I am the old apple tree by the mill, and the whole village shakes my branches. Lumberjacks keep eyeing my trunk, "
                 "so I'd like a decent shield against them, nothing crazy. I also dream of golden apples, lots and lots of them! "
                 "And no more snapping in the wind, please.",
        "success": "Golden apples everywhere! And the lumberjacks just walked right past me.",
        "partial": "Hm, I feel a bit different. It will do, I guess.",
        "failure": "Now my branches are all droopy and the lumberjacks are laughing!",
        "tiling": ["durability:i3", "durability:s", "durability:t", "luck:domino", "luck:i3v", "luck:z",
                   "protection:domino", "protection:dot", "protection:i3v"],
        "distractors": ["protection:i4", "haste:domino", "sharpness:t", "efficiency:l", "o", "dominov"],
        "order": [
            "luck:z", "o", "durability:s", "protection:i3v", "efficiency:l", "luck:i3v", "protection:dot", "dominov",
            "durability:t", "haste:domino", "luck:domino", "protection:i4", "durability:i3", "sharpness:t",
            "protection:domino"],
    },
    {
        "asset": "Level_03_Boat", "object": "boat",
        "reqs": [("protection", 3), ("haste", 0), ("sharpness", 1)],
        "story": "This is Soggy Susan, my little fishing boat. She leaks in every storm, so I want her hull as tough as a fortress "
                 "against the waves! Make her quicker too, I'm not picky how much. And she should nick through the harbor reeds, "
                 "just barely, nothing scary.",
        "success": "Not a drop of water inside, and look at her fly! Thank you!",
        "partial": "She floats. Good enough for the lake.",
        "failure": "She's sinking and now the fish are laughing at me!",
        "tiling": ["haste:i3", "haste:i3v", "haste:i4v", "protection:dot", "protection:l", "protection:z",
                   "protection:z", "protection:i4v", "sharpness:domino", "sharpness:domino"],
        "distractors": ["sharpness:l3", "sharpness:i4", "durability:o", "luck:dominov", "t", "s"],
        "order": [
            "protection:z", "t", "sharpness:domino", "haste:i3", "sharpness:i4", "protection:l", "durability:o",
            "haste:i4v", "s", "protection:dot", "sharpness:l3", "haste:i3v", "protection:z", "luck:dominov",
            "sharpness:domino", "protection:i4v"],
    },
    {
        "asset": "Level_04_House", "object": "house",
        "reqs": [("efficiency", 3), ("durability", 2), ("protection", 0)],
        "story": "My house eats a fortune in firewood every winter, I want it super-duper efficient! "
                 "The walls creak quite a bit too, so make them reasonably sturdy. And keep the wolves out somehow, I don't care how.",
        "success": "Warm, solid and wolf-proof! I'm throwing a party!",
        "partial": "Well, the draft is gone. Mostly. Okay.",
        "failure": "Now the roof leaks and the firewood bill doubled. Terrific.",
        "tiling": ["durability:dominov", "durability:dot", "durability:i3", "efficiency:i3v", "efficiency:i3v",
                   "efficiency:i3v", "efficiency:o", "protection:domino", "protection:i3v", "protection:i3v",
                   "protection:l", "protection:l"],
        "distractors": ["durability:o", "haste:i3", "sharpness:dominov", "luck:t", "j", "l3"],
        "order": [
            "protection:l", "efficiency:i3v", "j", "durability:i3", "protection:i3v", "haste:i3", "efficiency:o",
            "protection:domino", "luck:t", "efficiency:i3v", "durability:dominov", "l3", "protection:i3v",
            "durability:o", "efficiency:i3v", "sharpness:dominov", "durability:dot", "protection:l"],
    },
    {
        "asset": "Level_05_Cat", "object": "cat",
        "reqs": [("sharpness", 3), ("haste", 2), ("luck", 0)],
        "story": "My cat Mittens is a lousy hunter. Her claws are like butter knives, I want them razor sharp! "
                 "She should also be quite a bit quicker, and, well, a little luck never hurts when chasing mice.",
        "success": "Mittens caught three mice before breakfast. Purrfect!",
        "partial": "She caught one mouse. A small one. Fine.",
        "failure": "She shredded the sofa and then fell off it. Out!",
        "tiling": ["haste:domino", "haste:dominov", "haste:l3", "luck:dot", "luck:i3v", "luck:l3", "luck:o",
                   "luck:z", "sharpness:dominov", "sharpness:i3v", "sharpness:o", "sharpness:s"],
        "distractors": ["haste:i4v", "haste:j", "durability:i3v", "efficiency:domino", "protection:l", "i4"],
        "order": [
            "luck:o", "i4", "sharpness:s", "haste:l3", "protection:l", "luck:dot", "sharpness:o", "haste:j",
            "luck:l3", "haste:domino", "durability:i3v", "sharpness:dominov", "luck:z", "efficiency:domino",
            "haste:dominov", "sharpness:i3v", "haste:i4v", "luck:i3v"],
    },
]

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


def evaluate(placed, reqs):
    """Replica of QuestEvaluator rules. placed: list of (ench or None, shape id)."""
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
    if score == max_score and wrong == 0:
        outcome = "Full"
    elif score >= 1 and wrong == 0:
        outcome = "Partial"
    else:
        outcome = "Failure"
    return outcome, score, max_score, wrong


# ---------------------------------------------------------------------------------------------- search
# Mirrors Assets/Scripts/Editor/LevelValidator.cs: depth-first exact cover, first empty silhouette cell (row-major)
# gets one of the distinct available pieces (largest first, ties by list order), strict mode = required types only.

class Search(object):
    def __init__(self, silhouette_rows, items, reqs, allow_plain=False, unlimited=False, node_limit=NODE_LIMIT):
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
        # distinct pieces in first-occurrence order, then stable-sorted by cell count descending
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
        self.remaining = {d: (10 ** 6 if unlimited else counts[d]) for d in distinct}
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
        found = self._rec(len(self.sil))
        return found

    def _rec(self, empty_left):
        self.nodes += 1
        if self.nodes > self.node_limit:
            self.aborted = True
            return False
        if empty_left == 0:
            outcome = evaluate([p for p, _ in self.placed], self.reqs)[0]
            return outcome == "Full"
        first = None
        for c in self.order:
            if c not in self.occupied:
                first = c
                break
        for d in self.distinct:
            if self.remaining[d] <= 0:
                continue
            cs = SHAPE_CELLS[d[1]]
            ox, oy = first[0] - cs[0][0], first[1] - cs[0][1]
            absolute = [(ox + x, oy + y) for x, y in cs]
            ok = True
            for p in absolute:
                if p not in self.sil or p in self.occupied:
                    ok = False
                    break
            if not ok:
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


def verify():
    errors = []

    def check(cond, msg):
        if not cond:
            errors.append(msg)
        return cond

    signatures = {}
    all_item_ids = set()
    for lvl in LEVELS:
        name = lvl["asset"]
        sil = OBJ[lvl["object"]]
        reqs = lvl["reqs"]
        tiling = [parse_item(e) for e in lvl["tiling"]]
        distractors = [parse_item(e) for e in lvl["distractors"]]
        items = [parse_item(e) for e in lvl["order"]]
        req_types = set(e for e, _ in reqs)
        print("--- %s (%s, %d cells) requirements: %s" % (
            name, lvl["object"], len(cells_of(sil)),
            ", ".join("%s %s" % (e, "any" if t == 0 else "I" * t if t < 3 else "III") for e, t in reqs)))

        # bookkeeping
        check(2 <= len(reqs) <= 3, name + ": 2-3 requirements expected")
        check(len(set(e for e, _ in reqs)) == len(reqs), name + ": duplicate required types")
        check(4 <= len(distractors) <= 8, name + ": 4-8 distractors expected, got %d" % len(distractors))
        check(len(items) <= MAX_ITEMS_PER_LEVEL, name + ": too many items (%d)" % len(items))
        check(multiset(lvl["order"]) == multiset(lvl["tiling"] + lvl["distractors"]),
              name + ": order must be exactly tiling + distractors")
        check(any(t == 0 for _, t in reqs) and any(t > 0 for _, t in reqs), name + ": mix tier-0 and tier-specific")
        for e, s in tiling:
            check(e in req_types, name + ": tiling piece of unrequired type %s:%s" % (e, s))
        check(sum(len(SHAPE_CELLS[s]) for _, s in tiling) == len(cells_of(sil)), name + ": tiling cell count != silhouette")
        for e, s in items:
            all_item_ids.add(s if e is None else e + "_" + s)

        # a) exists with unlimited supply of every shape and every required type
        catalogue = [(e, sh) for (e, _t) in reqs for (sh, _n, _h, _r) in PLAIN_ITEMS]
        sa = Search(sil, catalogue, reqs, unlimited=True, node_limit=2000000)
        found_a = sa.run()
        check(found_a or sa.aborted, name + ": (a) no required-only tiling exists even with unlimited pieces")
        print("  a) unlimited-supply tiling exists: %s (nodes %d%s; the intended tiling in b) proves it anyway)" % (
            found_a, sa.nodes, ", search aborted" if sa.aborted else ""))

        # b1) the intended tiling is an exact cover using exactly those pieces
        s1 = Search(sil, tiling, reqs)
        s1.remaining = dict(multiset(tiling))
        s1.distinct = [d for d in s1.distinct if d in s1.remaining]
        ok1 = s1.run()
        check(ok1, name + ": (b) intended tiling does not tile the silhouette / is not Full")
        check(ok1 and len(s1.placed) == len(tiling), name + ": intended tiling should use every listed piece")

        # b) the actual starting inventory (tiling + distractors), same algorithm as C#
        sb = Search(sil, items, reqs)
        okb = sb.run()
        check(okb, name + ": (b) starting items contain no Full tiling")
        check(not sb.aborted, name + ": (b) search hit node limit")
        check(sb.nodes < NODE_LIMIT // 2, name + ": (b) search needs %d nodes, too close to the C# limit" % sb.nodes)
        print("  b) starting items contain a Full tiling: %s (nodes %d, limit %d)" % (okb, sb.nodes, NODE_LIMIT))

        if okb:
            placed_items = [p for p, _ in sb.placed]
            # c) the evaluator says Full and the silhouette is completely covered
            outcome, score, max_score, wrong = evaluate(placed_items, reqs)
            check(outcome == "Full" and score == max_score and wrong == 0, name + ": (c) found tiling is not Full")
            check(len(sb.occupied) == len(sb.sil), name + ": (c) silhouette not 100% filled")
            check(all(p[0] is not None for p in placed_items), name + ": (c) plain piece in strict tiling")
            print("  c) evaluator: %s score %d/%d wrong %d, filled %d/%d cells, %d pieces" % (
                outcome, score, max_score, wrong, len(sb.occupied), len(sb.sil), len(placed_items)))
            sig = tuple(sorted((p[0], p[1], o) for p, o in sb.placed))
            check(sig not in signatures.values(), name + ": identical tiling to another level")
            signatures[name] = sig

        # d) a distractor-only failure, a partial arrangement involving a distractor
        wrong_types = [d for d in distractors if d[0] is not None and d[0] not in req_types]
        failure_ok = False
        for d in wrong_types:
            if can_pack(sil, [d]) and evaluate([d], reqs)[0] == "Failure":
                failure_ok = True
                break
        check(failure_ok, name + ": (d) no distractor-only failure arrangement")
        overshoot = [d for d in distractors if d[0] in req_types and evaluate([d], reqs)[1] == 0]
        check(len(overshoot) >= 1, name + ": (d) no required-type-at-wrong-tier distractor")
        partial_ok = False
        partial_desc = ""
        for o in overshoot:
            for t in tiling:
                if t[0] != o[0] and can_pack(sil, [o, t]):
                    r = evaluate([o, t], reqs)
                    if r[0] == "Partial":
                        partial_ok = True
                        partial_desc = "%s:%s + %s:%s" % (o[0], o[1], t[0], t[1])
                        break
            if partial_ok:
                break
        check(partial_ok, name + ": (d) no partial arrangement with a distractor")
        # a distractor-free partial too (one right piece alone)
        right_alone = any(evaluate([t], reqs)[0] == "Partial" for t in tiling)
        check(right_alone, name + ": (d) single right piece should give Partial")
        print("  d) failure via unrequired piece: %s; partial via %s; wrong-tier distractors: %s" % (
            failure_ok, partial_desc or "-", ", ".join("%s:%s" % o for o in overshoot)))
        print("     items %d = tiling %d + distractors %d" % (len(items), len(tiling), len(distractors)))

    return errors, all_item_ids


# ---------------------------------------------------------------------------------------------- C# emission

def cs_str(s):
    return '"' + s.replace("\\", "\\\\").replace('"', '\\"') + '"'


def cs_rows(rows):
    return "new[] { " + ", ".join(cs_str(r) for r in rows) + " }"


def cs_type(e):
    return "EnchantmentType." + e.capitalize()


def emit_csharp():
    used = set()
    for lvl in LEVELS:
        for entry in lvl["order"]:
            e, s = parse_item(entry)
            if e is not None:
                used.add((e, s))
    shape_order = [p[0] for p in PLAIN_ITEMS]
    enchanted = sorted(used, key=lambda es: (ENCH.index(es[0]), shape_order.index(es[1])))

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
    for pid, name, hexc, rows in PLAIN_ITEMS:
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
            w("                    %s," % cs_str(s if e is None else e + "_" + s))
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
    errors, _ids = verify()
    text = emit_csharp()
    if emit:
        if errors:
            print("\nNOT emitting: verification failed")
        else:
            with open(CS_PATH, "w", newline="\n") as f:
                f.write(text)
            print("\nwrote " + os.path.relpath(CS_PATH, ROOT))
    else:
        if os.path.exists(CS_PATH):
            with open(CS_PATH, newline="") as f:
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
