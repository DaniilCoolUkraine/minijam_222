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
        private const string LevelsDir = SampleContentBuilder.LevelsDir;
        private const string ScenesDir = "Assets/Scenes";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string WebGlBuildPath = "Build/WebGL";

        // ------------------------------------------------------------------ (a) sample content

        /// <summary>
        /// Creates or UPDATES (in place, GUIDs kept) all sample items, objects, object sprites and levels. Idempotent.
        /// Data lives in SampleContentData.cs (generated from Tools/verify_levels.py); logic in SampleContentBuilder.
        /// </summary>
        [MenuItem("ShapeInventory/Create Sample Content")]
        public static void CreateSampleContent()
        {
            SampleContentBuilder.Run();
        }

        /// <summary>Alias for Create Sample Content: brings assets of an older project version up to date.</summary>
        [MenuItem("ShapeInventory/Upgrade Existing Content")]
        public static void UpgradeExistingContent()
        {
            SampleContentBuilder.Run();
        }

        [MenuItem("ShapeInventory/Validate Levels")]
        public static void ValidateLevels()
        {
            LevelValidator.ValidateAllLevels();
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
            SetInt(so, "playerColumns", 5);
            SetInt(so, "playerRows", 4);
            SetRef(so, "rootCanvas", canvas);
            SetRef(so, "playerPanel", ui.PlayerPanel);
            SetRef(so, "objectPanel", ui.ObjectPanel);
            SetRef(so, "doneButton", ui.DoneButton);
            SetRef(so, "statusText", ui.StatusText);
            SetRef(so, "dragController", dragController);
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
            ContentUtil.EnsureFolder(path);
        }

        // Null-safe SerializedObject setters: GameFlowController is edited by another agent, a renamed/removed optional
        // field must not break scene building (the controller builds missing UI at runtime).
        private static void SetRef(SerializedObject so, string field, UnityEngine.Object value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p != null) p.objectReferenceValue = value;
            else Debug.LogWarning("ShapeInventory: GameFlowController has no serialized field '" + field + "'.");
        }

        private static void SetInt(SerializedObject so, string field, int value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p != null) p.intValue = value;
            else Debug.LogWarning("ShapeInventory: GameFlowController has no serialized field '" + field + "'.");
        }

        private static void SetBool(SerializedObject so, string field, bool value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p != null) p.boolValue = value;
            else Debug.LogWarning("ShapeInventory: GameFlowController has no serialized field '" + field + "'.");
        }
    }
}
