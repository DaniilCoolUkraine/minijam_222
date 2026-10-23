using System;
using System.Collections.Generic;
using System.IO;
using ShapeInventory.Core;
using ShapeInventory.DragDrop;
using ShapeInventory.Game;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShapeInventory.Editor
{
    /// <summary>Editor menu: sample content, demo scene and WebGL helpers. Menu root: "ShapeInventory/".</summary>
    public static class ShapeInventoryMenu
    {
        private const string ContentRoot = "Assets/Content";
        private const string ItemsDir = "Assets/Content/Items";
        private const string ObjectsDir = "Assets/Content/Objects";
        private const string LevelsDir = "Assets/Content/Levels";
        private const string ScenesDir = "Assets/Scenes";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string WebGlBuildPath = "Build/WebGL";

        // ------------------------------------------------------------------ sample data definitions

        private sealed class ItemDef
        {
            public string Id;
            public string Name;
            public string Hex;
            public string[] Rows;
        }

        private sealed class ObjectDef
        {
            public string Id;
            public string Name;
            public string Hex;
            public string[] Rows;
        }

        private sealed class LevelDef
        {
            public string AssetName;
            public string ObjectId;
            public string[] ItemIds;
        }

        private static ItemDef Item(string id, string name, string hex, params string[] rows)
        {
            return new ItemDef { Id = id, Name = name, Hex = hex, Rows = rows };
        }

        private static ObjectDef Obj(string id, string name, string hex, params string[] rows)
        {
            return new ObjectDef { Id = id, Name = name, Hex = hex, Rows = rows };
        }

        private static LevelDef Level(string assetName, string objectId, params string[] itemIds)
        {
            return new LevelDef { AssetName = assetName, ObjectId = objectId, ItemIds = itemIds };
        }

        private static readonly ItemDef[] ItemDefs =
        {
            Item("i4", "I Piece", "#00BCD4", "####"),
            Item("i4v", "I Piece (upright)", "#0097A7", "#", "#", "#", "#"),
            Item("o", "O Piece", "#FDD835", "##", "##"),
            Item("t", "T Piece", "#AB47BC", "###", ".#."),
            Item("s", "S Piece", "#66BB6A", ".##", "##."),
            Item("z", "Z Piece", "#EF5350", "##", ".##"),
            Item("l", "L Piece", "#FFA726", "#.", "#.", "##"),
            Item("j", "J Piece", "#42A5F5", ".#", ".#", "##"),
            Item("dot", "Dot", "#ECEFF1", "#"),
            Item("domino", "Domino", "#8D6E63", "##"),
            Item("dominov", "Domino (upright)", "#BCAAA4", "#", "#"),
            Item("i3", "Bar 3", "#26A69A", "###"),
            Item("i3v", "Bar 3 (upright)", "#5C6BC0", "#", "#", "#"),
            Item("l3", "Corner", "#EC407A", "##", "#."),
        };

        // Silhouettes are hand-written; the sample levels below give pieces that tile them exactly.
        private static readonly ObjectDef[] ObjectDefs =
        {
            Obj("rocket", "Rocket", "#B0BEC5",
                "..#..",
                ".###.",
                ".###.",
                ".###.",
                ".###.",
                ".###.",
                "#####",
                "#.#.#"),
            Obj("tree", "Tree", "#81C784",
                "..###..",
                ".#####.",
                "#######",
                ".#####.",
                "..###..",
                "...#...",
                "...#...",
                "...#..."),
            Obj("boat", "Boat", "#90CAF9",
                "...#.....",
                "...##....",
                "...###...",
                "...####..",
                "#########",
                ".#######.",
                "..#####.."),
            Obj("house", "House", "#FFCC80",
                "....#....",
                "...###...",
                "..#####..",
                ".#######.",
                ".#######.",
                ".###.###.",
                ".###.###."),
            Obj("cat", "Cat", "#CE93D8",
                "#.....#",
                "##...##",
                "#######",
                "#######",
                "#######",
                ".#####.",
                "..###.."),
        };

        private static readonly LevelDef[] LevelDefs =
        {
            // 24 cells
            Level("Level_01_Rocket", "rocket",
                "i4v", "l", "dominov", "o", "i3v", "dominov", "l3", "dominov"),
            // 26 cells
            Level("Level_02_Tree", "tree",
                "t", "z", "domino", "s", "l3", "t", "i3v", "dominov"),
            // 31 cells
            Level("Level_03_Boat", "boat",
                "i4v", "t", "l", "domino", "i4", "i4v", "l", "dominov", "i3v"),
            // 35 cells
            Level("Level_04_House", "house",
                "i4v", "i3v", "l", "o", "i4v", "domino", "i3v", "dominov", "i4v", "domino", "i3v"),
            // 35 cells
            Level("Level_05_Cat", "cat",
                "t", "i4", "j", "z", "domino", "l", "i4v", "t", "dominov", "i3v"),
        };

        // ------------------------------------------------------------------ (a) sample content

        [MenuItem("ShapeInventory/Create Sample Content")]
        public static void CreateSampleContent()
        {
            EnsureFolder(ContentRoot);
            EnsureFolder(ItemsDir);
            EnsureFolder(ObjectsDir);
            EnsureFolder(LevelsDir);

            var items = new Dictionary<string, ItemData>();
            foreach (var def in ItemDefs)
            {
                string path = ItemsDir + "/Item_" + def.Id + ".asset";
                Color color = ParseColor(def.Hex);
                ItemData asset = LoadOrCreate<ItemData>(path, () => ItemData.Create(def.Id, def.Name, color, def.Rows));

                var so = new SerializedObject(asset);
                so.FindProperty("id").stringValue = def.Id;
                so.FindProperty("displayName").stringValue = def.Name;
                so.FindProperty("color").colorValue = color;
                SetStringArray(so.FindProperty("shapeRows"), def.Rows);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                items[def.Id] = asset;
            }

            var objects = new Dictionary<string, ObjectData>();
            foreach (var def in ObjectDefs)
            {
                string path = ObjectsDir + "/Object_" + def.Id + ".asset";
                Color color = ParseColor(def.Hex);
                ObjectData asset = LoadOrCreate<ObjectData>(path, () => ObjectData.Create(def.Id, def.Name, color, def.Rows));

                var so = new SerializedObject(asset);
                so.FindProperty("id").stringValue = def.Id;
                so.FindProperty("displayName").stringValue = def.Name;
                so.FindProperty("silhouetteColor").colorValue = color;
                SetStringArray(so.FindProperty("silhouetteRows"), def.Rows);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                objects[def.Id] = asset;
            }

            foreach (var def in LevelDefs)
            {
                string path = LevelsDir + "/" + def.AssetName + ".asset";
                LevelData asset = LoadOrCreate<LevelData>(path, () => LevelData.Create(def.AssetName, null, null));

                var so = new SerializedObject(asset);
                so.FindProperty("targetObject").objectReferenceValue = objects[def.ObjectId];
                var arr = so.FindProperty("startingItems");
                arr.arraySize = def.ItemIds.Length;
                for (int i = 0; i < def.ItemIds.Length; i++)
                    arr.GetArrayElementAtIndex(i).objectReferenceValue = items[def.ItemIds[i]];
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("ShapeInventory: sample content created under " + ContentRoot + " ("
                      + ItemDefs.Length + " items, " + ObjectDefs.Length + " objects, " + LevelDefs.Length + " levels).");
        }

        // ------------------------------------------------------------------ (b) demo scene

        [MenuItem("ShapeInventory/Build Demo Scene")]
        public static void BuildDemoScene()
        {
            var levels = LoadLevels();
            if (levels.Count == 0)
            {
                CreateSampleContent();
                levels = LoadLevels();
            }

            EnsureFolder(ScenesDir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera (solid colour background; the canvas itself is ScreenSpaceOverlay).
            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);

            // Canvas
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // EventSystem + input module
            var esGo = new GameObject("EventSystem", typeof(EventSystem));
            AddInputModule(esGo);

            // Panels, button, status text
            GameUiRefs ui = GameUiBuilder.BuildDefaultLayout(canvas);

            // Drag controller
            ItemDragController dragController = ItemDragController.Create(canvas, new Vector2(80f, 80f));

            // Game flow
            var flowGo = new GameObject("GameFlow");
            var flow = flowGo.AddComponent<GameFlowController>();
            var so = new SerializedObject(flow);
            var levelsProp = so.FindProperty("levels");
            levelsProp.arraySize = levels.Count;
            for (int i = 0; i < levels.Count; i++)
                levelsProp.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            so.FindProperty("playerColumns").intValue = 5;
            so.FindProperty("playerRows").intValue = 4;
            so.FindProperty("rootCanvas").objectReferenceValue = canvas;
            so.FindProperty("playerPanel").objectReferenceValue = ui.PlayerPanel;
            so.FindProperty("objectPanel").objectReferenceValue = ui.ObjectPanel;
            so.FindProperty("doneButton").objectReferenceValue = ui.DoneButton;
            so.FindProperty("statusText").objectReferenceValue = ui.StatusText;
            so.FindProperty("dragController").objectReferenceValue = dragController;
            so.FindProperty("autoAdvanceOnChange").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("ShapeInventory: demo scene saved to " + ScenePath + ". Press Play.");
        }

        private static void AddInputModule(GameObject eventSystemGo)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            Type moduleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (moduleType != null)
            {
                eventSystemGo.AddComponent(moduleType);
                return;
            }

            Debug.LogWarning("ShapeInventory: InputSystemUIInputModule not found (is the Input System package installed?). "
                             + "Falling back to StandaloneInputModule.");
#endif
            eventSystemGo.AddComponent<StandaloneInputModule>();
        }

        private static List<LevelData> LoadLevels()
        {
            var result = new List<LevelData>();
            if (!AssetDatabase.IsValidFolder(LevelsDir)) return result;

            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { LevelsDir });
            var paths = new List<string>();
            foreach (string guid in guids) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Sort(StringComparer.Ordinal);

            foreach (string path in paths)
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level != null) result.Add(level);
            }

            return result;
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == path)
                {
                    scenes[i] = new EditorBuildSettingsScene(path, true);
                    found = true;
                    break;
                }
            }

            if (!found) scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ------------------------------------------------------------------ (c) WebGL settings

        [MenuItem("ShapeInventory/Apply WebGL Settings")]
        public static void ApplyWebGLSettings()
        {
            ApplyWebGLPlayerSettings();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                // May trigger a domain reload; settings above are already applied.
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            }

            Debug.Log("ShapeInventory: WebGL settings applied (Gzip, decompression fallback, no exception support, run in background).");
        }

        private static void ApplyWebGLPlayerSettings()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.runInBackground = true;
        }

        // ------------------------------------------------------------------ (d) WebGL build

        [MenuItem("ShapeInventory/Build WebGL")]
        public static void BuildWebGL()
        {
            ApplyWebGLPlayerSettings();

            if (!File.Exists(ScenePath))
                BuildDemoScene();

            var scenePaths = new List<string>();
            foreach (var s in EditorBuildSettings.scenes)
                if (s.enabled) scenePaths.Add(s.path);

            if (scenePaths.Count == 0)
            {
                AddSceneToBuildSettings(ScenePath);
                scenePaths.Add(ScenePath);
            }

            Directory.CreateDirectory(WebGlBuildPath);

            var options = new BuildPlayerOptions
            {
                scenes = scenePaths.ToArray(),
                locationPathName = WebGlBuildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log("ShapeInventory: WebGL build succeeded -> " + WebGlBuildPath + " (" + report.summary.totalSize + " bytes).");
            else
                Debug.LogError("ShapeInventory: WebGL build finished with result " + report.summary.result + ".");
        }

        // ------------------------------------------------------------------ helpers

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            string leaf = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static T LoadOrCreate<T>(string path, Func<T> factory) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            T created = factory();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void SetStringArray(SerializedProperty prop, string[] values)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        private static Color ParseColor(string hex)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.white;
        }
    }
}
