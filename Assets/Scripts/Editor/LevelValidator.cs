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
    /// Search (mirrored by Tools/verify_levels.py): depth-first, the first empty silhouette cell (row-major) receives one of
    /// the distinct available pieces, biggest first. Strict mode only uses pieces of REQUIRED enchantment types (no plain filler,
    /// no tier above the required one). If strict mode fails, a second pass with plain filler allowed is tried and reported
    /// as a warning. Bounded by NodeLimit so it cannot hang the editor.
    /// </summary>
    internal static class LevelValidator
    {
        internal const int NodeLimit = 400000;

        internal static bool ValidateAllLevels()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            var paths = new List<string>();
            foreach (string guid in guids) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Sort(System.StringComparer.Ordinal);

            if (paths.Count == 0)
            {
                Debug.LogWarning("ShapeInventory: Validate Levels found no LevelData assets (run ShapeInventory/Create Sample Content).");
                return false;
            }

            int ok = 0, fail = 0;
            foreach (string path in paths)
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level == null) continue;
                if (ValidateLevel(level, path)) ok++;
                else fail++;
            }

            string summary = "ShapeInventory: level validation finished - " + ok + " OK, " + fail + " FAILED (" + paths.Count + " levels).";
            if (fail == 0) Debug.Log(summary);
            else Debug.LogError(summary);
            return fail == 0;
        }

        private static bool ValidateLevel(LevelData level, string path)
        {
            string label = level.name + " (" + path + ")";

            if (level.TargetObject == null)
            {
                Debug.LogError("Level FAIL " + label + ": no target object.", level);
                return false;
            }

            if (level.StartingItems == null || level.StartingItems.Length == 0)
            {
                Debug.LogError("Level FAIL " + label + ": no starting items.", level);
                return false;
            }

            for (int i = 0; i < level.StartingItems.Length; i++)
            {
                if (level.StartingItems[i] == null)
                {
                    Debug.LogError("Level FAIL " + label + ": starting item #" + i + " is missing (null).", level);
                    return false;
                }
            }

            EnchantmentRequirement[] requirements = level.Requirements ?? new EnchantmentRequirement[0];
            if (requirements.Length == 0)
                Debug.LogWarning("Level " + label + " has no requirements.", level);

            var timer = Stopwatch.StartNew();

            var strict = new TilingSearch(level.TargetObject, level.StartingItems, requirements, false);
            bool found = strict.Run();
            if (found)
            {
                timer.Stop();
                Debug.Log("Level OK   " + label + ": Full tiling of " + level.TargetObject.CellCount + " cells with "
                          + strict.PlacedCount + " pieces (" + Describe(strict.Result) + "), " + strict.Nodes + " nodes, "
                          + timer.ElapsedMilliseconds + " ms. Requirements: " + Describe(requirements) + ".", level);
                return true;
            }

            if (strict.Aborted)
            {
                timer.Stop();
                Debug.LogError("Level FAIL " + label + ": search stopped at the node limit (" + NodeLimit
                               + ") without finding a Full tiling. Requirements: " + Describe(requirements) + ".", level);
                return false;
            }

            var loose = new TilingSearch(level.TargetObject, level.StartingItems, requirements, true);
            bool foundLoose = loose.Run();
            timer.Stop();
            if (foundLoose)
            {
                Debug.LogWarning("Level WARN " + label + ": Full only reachable WITH plain filler pieces (" + Describe(loose.Result)
                                 + "). Design rule: a Full tiling should use only the required enchantment types. Requirements: "
                                 + Describe(requirements) + ".", level);
                return true;
            }

            Debug.LogError("Level FAIL " + label + ": no tiling of the silhouette gives a Full outcome with its starting items"
                           + (loose.Aborted ? " (search hit the node limit)" : "") + ". Requirements: " + Describe(requirements)
                           + ". " + timer.ElapsedMilliseconds + " ms.", level);
            return false;
        }

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

        private sealed class Candidate
        {
            public ItemData Data;
            public int CellCount;
            public readonly List<ItemInstance> Instances = new List<ItemInstance>();
            public int Used;
        }

        private sealed class TilingSearch
        {
            private readonly ObjectInventoryModel _model;
            private readonly EnchantmentRequirement[] _requirements;
            private readonly List<Vector2Int> _order = new List<Vector2Int>();
            private readonly List<Candidate> _candidates = new List<Candidate>();
            private readonly int _silhouetteCells;
            private int _placedCount;

            public int Nodes { get; private set; }
            public bool Aborted { get; private set; }
            public QuestResult Result { get; private set; }
            public int PlacedCount { get { return _placedCount; } }

            public TilingSearch(ObjectData target, ItemData[] items, EnchantmentRequirement[] requirements, bool allowPlain)
            {
                _requirements = requirements;
                _model = new ObjectInventoryModel(target);

                ItemShape sil = target.Silhouette;
                for (int y = 0; y < sil.Height; y++)
                {
                    for (int x = 0; x < sil.Width; x++)
                    {
                        var cell = new Vector2Int(x, y);
                        if (sil.Contains(cell)) _order.Add(cell);
                    }
                }

                _silhouetteCells = _order.Count;

                // Distinct pieces in first-occurrence order (one ItemInstance per copy).
                var byData = new Dictionary<ItemData, Candidate>();
                var distinct = new List<Candidate>();
                for (int i = 0; i < items.Length; i++)
                {
                    ItemData data = items[i];
                    if (!IsUsable(data, requirements, allowPlain)) continue;

                    Candidate c;
                    if (!byData.TryGetValue(data, out c))
                    {
                        c = new Candidate { Data = data, CellCount = data.Shape.CellCount };
                        byData[data] = c;
                        distinct.Add(c);
                    }

                    c.Instances.Add(new ItemInstance(data));
                }

                // Stable insertion sort: biggest pieces first, ties keep their order.
                for (int i = 0; i < distinct.Count; i++)
                {
                    Candidate current = distinct[i];
                    int j = i - 1;
                    while (j >= 0 && distinct[j].CellCount < current.CellCount)
                    {
                        distinct[j + 1] = distinct[j];
                        j--;
                    }

                    distinct[j + 1] = current;
                }

                _candidates.AddRange(distinct);
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
                if (_silhouetteCells == 0) return false;
                return Recurse(_silhouetteCells);
            }

            private bool Recurse(int emptyLeft)
            {
                Nodes++;
                if (Nodes > NodeLimit)
                {
                    Aborted = true;
                    return false;
                }

                if (emptyLeft == 0)
                {
                    QuestResult result = QuestEvaluator.Evaluate(_model.PlacedItems, _requirements);
                    if (result.Outcome != QuestOutcome.Full) return false;
                    Result = result;
                    return true;
                }

                Vector2Int first = FindFirstEmpty();
                for (int i = 0; i < _candidates.Count; i++)
                {
                    Candidate c = _candidates[i];
                    if (c.Used >= c.Instances.Count || c.CellCount > emptyLeft) continue;

                    ItemInstance instance = c.Instances[c.Used];
                    Vector2Int origin = first - c.Data.Shape.Cells[0];
                    if (!_model.TryPlace(instance, origin)) continue;

                    c.Used++;
                    _placedCount++;
                    if (Recurse(emptyLeft - c.CellCount)) return true;

                    _placedCount--;
                    c.Used--;
                    _model.Remove(instance);
                    if (Aborted) return false;
                }

                return false;
            }

            private Vector2Int FindFirstEmpty()
            {
                for (int i = 0; i < _order.Count; i++)
                {
                    if (_model.GetItemAt(_order[i]) == null) return _order[i];
                }

                return _order[_order.Count - 1];
            }
        }
    }
}
