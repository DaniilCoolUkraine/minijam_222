using System;
using System.Collections.Generic;
using System.Diagnostics;
using ShapeInventory.Core;
using ShapeInventory.Game;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ShapeInventory.Editor
{
    /// <summary>
    /// "ShapeInventory/Validate Levels": for every LevelData in the project, searches for an exact cover of the silhouette
    /// by pieces from the level's startingItems that QuestEvaluator rates as Full (design rule: Full => silhouette 100% filled).
    ///
    /// Search: depth-first exact cover on a flat cell grid. At every node the EMPTY silhouette cell with the fewest legal
    /// placements is chosen (most constrained first; a cell with zero placements is a dead end), and each placement that covers
    /// it is tried, biggest pieces first. Identical pieces are interchangeable (counted, not permuted). Extra pruning: not enough
    /// unused cells left, or a requirement that can no longer be met with the unused pieces.
    /// Strict mode only uses pieces of REQUIRED enchantment types (no plain filler, no tier above the required one). If strict
    /// mode fails, a second pass with plain filler allowed is tried and reported as a warning.
    /// Bounded by NodeLimit (default 2,000,000 per pass) and a time budget per level (default 20 s) so it cannot hang the editor;
    /// a level whose search is aborted is reported as WARN (inconclusive). Both limits are stored in EditorPrefs
    /// (keys below) and can be changed from code or the registry/prefs file.
    /// Also checks cheap level-shape rules (item count, number of enchantment types of tutorial levels 1-3).
    /// </summary>
    internal static class LevelValidator
    {
        internal const string NodeLimitPref = "ShapeInventory.Validator.NodeLimit";
        internal const string TimeBudgetPref = "ShapeInventory.Validator.TimeBudgetSeconds";
        internal const int DefaultNodeLimit = 2000000;
        internal const float DefaultTimeBudgetSeconds = 20f;
        internal const int MaxItemsPerLevel = 30;

        private enum Status
        {
            Ok,
            Warn,
            Fail
        }

        /// <summary>Maximum search nodes per pass.</summary>
        internal static int NodeLimit
        {
            get { return Math.Max(1000, EditorPrefs.GetInt(NodeLimitPref, DefaultNodeLimit)); }
            set { EditorPrefs.SetInt(NodeLimitPref, value); }
        }

        /// <summary>Wall-clock budget per level (both passes together), in seconds.</summary>
        internal static float TimeBudgetSeconds
        {
            get { return Math.Max(0.5f, EditorPrefs.GetFloat(TimeBudgetPref, DefaultTimeBudgetSeconds)); }
            set { EditorPrefs.SetFloat(TimeBudgetPref, value); }
        }

        internal static bool ValidateAllLevels()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            var paths = new List<string>();
            foreach (string guid in guids) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Sort(StringComparer.Ordinal);

            if (paths.Count == 0)
            {
                Debug.LogWarning("ShapeInventory: Validate Levels found no LevelData assets (run ShapeInventory/Create Sample Content).");
                return false;
            }

            int nodeLimit = NodeLimit;
            float budget = TimeBudgetSeconds;
            int ok = 0, warn = 0, fail = 0;
            var total = Stopwatch.StartNew();
            for (int i = 0; i < paths.Count; i++)
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(paths[i]);
                if (level == null) continue;

                if (EditorUtility.DisplayCancelableProgressBar("Validate Levels", level.name, (i + 1f) / paths.Count))
                {
                    Debug.LogWarning("ShapeInventory: level validation cancelled.");
                    break;
                }

                Status status;
                try
                {
                    status = ValidateLevel(level, paths[i], i + 1, nodeLimit, budget);
                }
                catch (Exception e)
                {
                    Debug.LogError("Level FAIL " + level.name + ": exception during validation: " + e, level);
                    status = Status.Fail;
                }

                if (status == Status.Ok) ok++;
                else if (status == Status.Warn) warn++;
                else fail++;
            }

            EditorUtility.ClearProgressBar();

            string summary = "ShapeInventory: level validation finished - " + ok + " OK, " + warn + " WARN, " + fail + " FAIL ("
                             + paths.Count + " levels, " + total.ElapsedMilliseconds + " ms; node limit " + nodeLimit
                             + ", time budget " + budget.ToString("0.#") + " s per level).";
            if (fail > 0) Debug.LogError(summary);
            else if (warn > 0) Debug.LogWarning(summary);
            else Debug.Log(summary);
            return fail == 0;
        }

        private static Status ValidateLevel(LevelData level, string path, int fallbackNumber, int nodeLimit, float budgetSeconds)
        {
            string label = level.name + " (" + path + ")";

            if (level.TargetObject == null)
            {
                Debug.LogError("Level FAIL " + label + ": no target object.", level);
                return Status.Fail;
            }

            if (level.StartingItems == null || level.StartingItems.Length == 0)
            {
                Debug.LogError("Level FAIL " + label + ": no starting items.", level);
                return Status.Fail;
            }

            for (int i = 0; i < level.StartingItems.Length; i++)
            {
                if (level.StartingItems[i] == null)
                {
                    Debug.LogError("Level FAIL " + label + ": starting item #" + i + " is missing (null).", level);
                    return Status.Fail;
                }
            }

            EnchantmentRequirement[] requirements = level.Requirements ?? new EnchantmentRequirement[0];
            if (requirements.Length == 0)
                Debug.LogWarning("Level WARN " + label + ": no requirements.", level);

            Status status = Status.Ok;

            // ---- cheap shape rules (WARN only)
            List<string> ruleWarnings = CheckShapeRules(level, requirements, ParseLevelNumber(level.name, fallbackNumber));
            foreach (string w in ruleWarnings)
            {
                Debug.LogWarning("Level WARN " + label + ": " + w, level);
                status = Status.Warn;
            }

            // ---- exact cover search
            var timer = Stopwatch.StartNew();

            var strict = new TilingSearch(level.TargetObject, level.StartingItems, requirements, false, nodeLimit, budgetSeconds, timer);
            bool found = strict.Run();
            if (found)
            {
                Debug.Log("Level OK   " + label + ": Full tiling of " + level.TargetObject.CellCount + " cells with "
                          + strict.PlacedCount + " pieces (" + Describe(strict.Result) + "), " + strict.Nodes + " nodes, "
                          + timer.ElapsedMilliseconds + " ms. Requirements: " + Describe(requirements) + ".", level);
                return status;
            }

            if (strict.Aborted)
            {
                Debug.LogWarning("Level WARN " + label + ": search aborted (" + strict.AbortReason + " after " + strict.Nodes + " nodes, "
                                 + timer.ElapsedMilliseconds + " ms) - solvability is UNDECIDED. Requirements: "
                                 + Describe(requirements) + ".", level);
                return Status.Warn;
            }

            var loose = new TilingSearch(level.TargetObject, level.StartingItems, requirements, true, nodeLimit, budgetSeconds, timer);
            bool foundLoose = loose.Run();
            if (foundLoose)
            {
                Debug.LogWarning("Level WARN " + label + ": Full only reachable WITH plain filler pieces (" + Describe(loose.Result)
                                 + "). Design rule: a Full tiling should use only the required enchantment types. Requirements: "
                                 + Describe(requirements) + ".", level);
                return Status.Warn;
            }

            if (loose.Aborted)
            {
                Debug.LogWarning("Level WARN " + label + ": strict search found no Full tiling and the lenient pass was aborted ("
                                 + loose.AbortReason + " after " + loose.Nodes + " nodes, " + timer.ElapsedMilliseconds
                                 + " ms) - UNDECIDED. Requirements: " + Describe(requirements) + ".", level);
                return Status.Warn;
            }

            Debug.LogError("Level FAIL " + label + ": no tiling of the silhouette gives a Full outcome with its starting items. "
                           + "Requirements: " + Describe(requirements) + ". " + (strict.Nodes + loose.Nodes) + " nodes, "
                           + timer.ElapsedMilliseconds + " ms.", level);
            return Status.Fail;
        }

        // ------------------------------------------------------------------ shape rules

        /// <summary>"Level_03_Pitchfork" -> 3; the fallback is used for names without the Level_NN_ prefix.</summary>
        private static int ParseLevelNumber(string levelName, int fallback)
        {
            const string prefix = "Level_";
            if (levelName == null || !levelName.StartsWith(prefix, StringComparison.Ordinal)) return fallback;

            int i = prefix.Length;
            int n = 0;
            int digits = 0;
            while (i < levelName.Length && levelName[i] >= '0' && levelName[i] <= '9' && digits < 4)
            {
                n = n * 10 + (levelName[i] - '0');
                i++;
                digits++;
            }

            return digits > 0 ? n : fallback;
        }

        private static List<string> CheckShapeRules(LevelData level, EnchantmentRequirement[] requirements, int levelNumber)
        {
            var warnings = new List<string>();
            ItemData[] items = level.StartingItems;
            if (items.Length > MaxItemsPerLevel)
                warnings.Add("has " + items.Length + " starting items (limit " + MaxItemsPerLevel + ").");

            var itemTypes = new HashSet<EnchantmentType>();
            foreach (ItemData item in items)
            {
                if (item != null && item.HasEnchantment) itemTypes.Add(item.Enchantment);
            }

            var requiredTypes = new HashSet<EnchantmentType>();
            foreach (EnchantmentRequirement r in requirements) requiredTypes.Add(r.Type);

            foreach (EnchantmentType t in requiredTypes)
            {
                if (!itemTypes.Contains(t))
                    warnings.Add("requires " + t + " but no starting item carries it.");
            }

            if (levelNumber == 1)
            {
                if (itemTypes.Count != 1)
                    warnings.Add("tutorial level 1 should contain exactly ONE enchantment type among its items (found " + itemTypes.Count + ").");
            }
            else if (levelNumber == 2)
            {
                if (itemTypes.Count != 2)
                    warnings.Add("level 2 should contain exactly TWO enchantment types among its items (found " + itemTypes.Count + ").");
                if (requiredTypes.Count != 1)
                    warnings.Add("level 2 should require exactly ONE enchantment type (found " + requiredTypes.Count + ").");
            }
            else if (levelNumber == 3)
            {
                if (itemTypes.Count != 3)
                    warnings.Add("level 3 should contain exactly THREE enchantment types among its items (found " + itemTypes.Count + ").");
                if (requiredTypes.Count != 2)
                    warnings.Add("level 3 should require exactly TWO enchantment types (found " + requiredTypes.Count + ").");
            }

            return warnings;
        }

        // ------------------------------------------------------------------ descriptions

        private static string Describe(QuestResult result)
        {
            if (result == null) return "no result";
            return result.Outcome + " " + result.Score + "/" + result.MaxScore + ", wrong " + result.WrongCount;
        }

        private static string Describe(EnchantmentRequirement[] requirements)
        {
            if (requirements.Length == 0) return "none";
            var parts = new string[requirements.Length];
            for (int i = 0; i < requirements.Length; i++) parts[i] = requirements[i].ToString();
            return string.Join(", ", parts);
        }

        // ------------------------------------------------------------------ search

        private sealed class Piece
        {
            public ItemData Data;
            public int CellCount;
            public int Count;
            public int Used;
            public int TypeSlot;
            public int Tier;
        }

        private sealed class Placement
        {
            public int Piece;
            public int[] Cells;
            public Vector2Int Origin;
        }

        private sealed class TilingSearch
        {
            private const int TierSlots = EnchantmentTierRules.MaxTier + 1;

            private readonly EnchantmentRequirement[] _requirements;
            private readonly int _nodeLimit;
            private readonly double _budgetSeconds;
            private readonly Stopwatch _clock;

            private readonly int _cellCount;
            private readonly bool[] _filled;
            private readonly List<Piece> _pieces = new List<Piece>();
            private readonly List<Placement> _placements = new List<Placement>();
            private int[][] _byCell;
            private readonly int _typeSlots;
            private readonly int[] _placedByTypeTier;
            private readonly int[] _unusedByTypeTier;
            private int _unusedCells;
            private readonly List<int> _chosen = new List<int>();

            public int Nodes { get; private set; }
            public bool Aborted { get; private set; }
            public string AbortReason { get; private set; }
            public QuestResult Result { get; private set; }
            public int PlacedCount { get { return _chosen.Count; } }

            public TilingSearch(ObjectData target, ItemData[] items, EnchantmentRequirement[] requirements, bool allowPlain,
                int nodeLimit, float budgetSeconds, Stopwatch levelClock)
            {
                _requirements = requirements;
                _nodeLimit = nodeLimit;
                _budgetSeconds = budgetSeconds;
                _clock = levelClock;

                int maxType = 0;
                foreach (EnchantmentType t in Enum.GetValues(typeof(EnchantmentType)))
                    maxType = Math.Max(maxType, (int)t);
                _typeSlots = maxType + 1;
                _placedByTypeTier = new int[_typeSlots * TierSlots];
                _unusedByTypeTier = new int[_typeSlots * TierSlots];

                // ---- silhouette -> flat cell indices
                ItemShape sil = target.Silhouette;
                int w = sil.Width, h = sil.Height;
                var grid = new int[w * h];
                for (int i = 0; i < grid.Length; i++) grid[i] = -1;
                int n = 0;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (sil.Contains(new Vector2Int(x, y))) grid[y * w + x] = n++;
                    }
                }

                _cellCount = n;
                _filled = new bool[n];

                // ---- distinct usable pieces (one Piece per ItemData, with a count), biggest first, stable
                var byData = new Dictionary<ItemData, Piece>();
                var distinct = new List<Piece>();
                for (int i = 0; i < items.Length; i++)
                {
                    ItemData data = items[i];
                    if (data == null || !IsUsable(data, requirements, allowPlain)) continue;

                    Piece p;
                    if (!byData.TryGetValue(data, out p))
                    {
                        p = new Piece
                        {
                            Data = data,
                            CellCount = data.Shape.CellCount,
                            TypeSlot = (int)data.Enchantment,
                            Tier = data.HasEnchantment ? data.Tier : 0
                        };
                        byData[data] = p;
                        distinct.Add(p);
                    }

                    p.Count++;
                }

                for (int i = 1; i < distinct.Count; i++)
                {
                    Piece current = distinct[i];
                    int j = i - 1;
                    while (j >= 0 && distinct[j].CellCount < current.CellCount)
                    {
                        distinct[j + 1] = distinct[j];
                        j--;
                    }

                    distinct[j + 1] = current;
                }

                _pieces.AddRange(distinct);

                // ---- every legal translation of every distinct piece
                var perCell = new List<int>[n];
                for (int i = 0; i < n; i++) perCell[i] = new List<int>();

                for (int pi = 0; pi < _pieces.Count; pi++)
                {
                    Piece piece = _pieces[pi];
                    _unusedCells += piece.CellCount * piece.Count;
                    if (piece.TypeSlot > 0) _unusedByTypeTier[piece.TypeSlot * TierSlots + piece.Tier] += piece.Count;

                    IReadOnlyList<Vector2Int> cells = piece.Data.Shape.Cells;
                    for (int oy = -h; oy < h; oy++)
                    {
                        for (int ox = -w; ox < w; ox++)
                        {
                            var idx = new int[cells.Count];
                            bool fits = true;
                            for (int c = 0; c < cells.Count; c++)
                            {
                                int x = ox + cells[c].x;
                                int y = oy + cells[c].y;
                                if (x < 0 || y < 0 || x >= w || y >= h || grid[y * w + x] < 0)
                                {
                                    fits = false;
                                    break;
                                }

                                idx[c] = grid[y * w + x];
                            }

                            if (!fits) continue;

                            int id = _placements.Count;
                            _placements.Add(new Placement { Piece = pi, Cells = idx, Origin = new Vector2Int(ox, oy) });
                            for (int c = 0; c < idx.Length; c++) perCell[idx[c]].Add(id);
                        }
                    }
                }

                _byCell = new int[n][];
                for (int i = 0; i < n; i++) _byCell[i] = perCell[i].ToArray(); // already ordered biggest piece first
            }

            private static bool IsUsable(ItemData data, EnchantmentRequirement[] requirements, bool allowPlain)
            {
                if (!data.HasEnchantment) return allowPlain;

                bool required = false;
                for (int i = 0; i < requirements.Length; i++)
                {
                    if (requirements[i].Type != data.Enchantment) continue;
                    required = true;
                    // A piece above the required tier would overshoot the max tier of that type.
                    if (!requirements[i].AnyTier && data.Tier > requirements[i].Tier) return false;
                }

                return required;
            }

            public bool Run()
            {
                if (_cellCount == 0) return false;
                return Recurse(_cellCount);
            }

            private bool Recurse(int emptyLeft)
            {
                Nodes++;
                if (Nodes > _nodeLimit)
                {
                    Aborted = true;
                    AbortReason = "node limit " + _nodeLimit;
                    return false;
                }

                if ((Nodes & 255) == 0 && _clock.Elapsed.TotalSeconds > _budgetSeconds)
                {
                    Aborted = true;
                    AbortReason = "time budget " + _budgetSeconds.ToString("0.#") + " s";
                    return false;
                }

                if (emptyLeft == 0) return AcceptIfFull();
                if (_unusedCells < emptyLeft) return false;
                if (!RequirementsStillReachable()) return false;

                // Most constrained empty cell.
                int bestCell = -1;
                int bestCount = int.MaxValue;
                for (int c = 0; c < _cellCount; c++)
                {
                    if (_filled[c]) continue;
                    int count = CountPlacements(c, bestCount);
                    if (count < bestCount)
                    {
                        bestCount = count;
                        bestCell = c;
                        if (count <= 1) break; // dead end (0) or forced move (1)
                    }
                }

                if (bestCount == 0 || bestCell < 0) return false;

                int[] options = _byCell[bestCell];
                for (int k = 0; k < options.Length; k++)
                {
                    int id = options[k];
                    Placement pl = _placements[id];
                    Piece piece = _pieces[pl.Piece];
                    if (piece.Used >= piece.Count || !AllFree(pl)) continue;

                    Apply(pl, piece, true);
                    _chosen.Add(id);
                    if (Recurse(emptyLeft - piece.CellCount)) return true;

                    _chosen.RemoveAt(_chosen.Count - 1);
                    Apply(pl, piece, false);
                    if (Aborted) return false;
                }

                return false;
            }

            private int CountPlacements(int cell, int stopAt)
            {
                int[] options = _byCell[cell];
                int count = 0;
                for (int k = 0; k < options.Length; k++)
                {
                    Placement pl = _placements[options[k]];
                    Piece piece = _pieces[pl.Piece];
                    if (piece.Used >= piece.Count || !AllFree(pl)) continue;
                    count++;
                    if (count >= stopAt) return count;
                }

                return count;
            }

            private bool AllFree(Placement pl)
            {
                int[] cells = pl.Cells;
                for (int i = 0; i < cells.Length; i++)
                {
                    if (_filled[cells[i]]) return false;
                }

                return true;
            }

            private void Apply(Placement pl, Piece piece, bool place)
            {
                int[] cells = pl.Cells;
                for (int i = 0; i < cells.Length; i++) _filled[cells[i]] = place;

                int delta = place ? 1 : -1;
                piece.Used += delta;
                _unusedCells -= delta * piece.CellCount;
                if (piece.TypeSlot > 0)
                {
                    int slot = piece.TypeSlot * TierSlots + piece.Tier;
                    _placedByTypeTier[slot] += delta;
                    _unusedByTypeTier[slot] -= delta;
                }
            }

            /// <summary>Prune: every requirement needs a placed piece of its type (and tier) now or an unused one later.</summary>
            private bool RequirementsStillReachable()
            {
                for (int r = 0; r < _requirements.Length; r++)
                {
                    EnchantmentRequirement req = _requirements[r];
                    int slot = (int)req.Type;
                    if (slot <= 0 || slot >= _typeSlots) continue;

                    if (req.AnyTier)
                    {
                        bool any = false;
                        for (int t = 1; t < TierSlots && !any; t++)
                            any = _placedByTypeTier[slot * TierSlots + t] > 0 || _unusedByTypeTier[slot * TierSlots + t] > 0;
                        if (!any) return false;
                    }
                    else
                    {
                        int s = slot * TierSlots + req.Tier;
                        if (req.Tier < TierSlots && _placedByTypeTier[s] == 0 && _unusedByTypeTier[s] == 0) return false;
                    }
                }

                return true;
            }

            /// <summary>Complete cover: confirm with the real QuestEvaluator.</summary>
            private bool AcceptIfFull()
            {
                var placed = new List<PlacedItem>(_chosen.Count);
                for (int i = 0; i < _chosen.Count; i++)
                {
                    Placement pl = _placements[_chosen[i]];
                    placed.Add(new PlacedItem(new ItemInstance(_pieces[pl.Piece].Data), pl.Origin));
                }

                QuestResult result = QuestEvaluator.Evaluate(placed, _requirements);
                if (result.Outcome != QuestOutcome.Full) return false;
                Result = result;
                return true;
            }
        }
    }
}
