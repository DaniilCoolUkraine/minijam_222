using System.Collections.Generic;
using System.IO;
using ShapeInventory.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShapeInventory.Editor
{
    /// <summary>
    /// "ShapeInventory/Build Main Menu Scene": creates Assets/Scenes/MainMenu.unity with a title, Play / Quit buttons and a
    /// full-screen cutscene overlay (CutscenePlayer) in front of the menu, and makes the menu the FIRST scene in the Build Settings.
    /// </summary>
    public static class MainMenuSceneBuilder
    {
        public const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string GameScenePath = "Assets/Scenes/Main.unity";
        private const string GameSceneName = "Main";

        private static readonly Color BackgroundColor = new Color(0.10f, 0.12f, 0.18f, 1f);

        [MenuItem("ShapeInventory/Build Main Menu Scene")]
        public static void Build()
        {
            if (File.Exists(MenuScenePath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Rebuild the main menu scene?",
                    MenuScenePath + " already exists. Rebuilding replaces it, including your cutscene setup under CutsceneContent.",
                    "Overwrite", "Cancel");
                if (!overwrite) return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera (solid colour; the canvas is ScreenSpaceOverlay).
            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BackgroundColor;

            // Canvas
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // EventSystem + the input module that matches the project's input backend.
            var esGo = new GameObject("EventSystem", typeof(EventSystem));
            ShapeInventoryMenu.AddInputModule(esGo);

            // ---- menu (behind the cutscene)
            GameUiBuilder.CreatePanel(canvas.transform, "Background", BackgroundColor,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Text title = GameUiBuilder.CreateText(canvas.transform, "Title", "SHAPE INVENTORY", 96,
                TextAnchor.MiddleCenter, Color.white);
            Place(title.rectTransform, new Vector2(0.5f, 0.66f), new Vector2(1400f, 180f));

            Text playLabel;
            Button playButton = GameUiBuilder.CreateButton(canvas.transform, "PlayButton", "Play", out playLabel);
            Place((RectTransform)playButton.transform, new Vector2(0.5f, 0.42f), new Vector2(440f, 120f));

            Text quitLabel;
            Button quitButton = GameUiBuilder.CreateButton(canvas.transform, "QuitButton", "Quit", out quitLabel);
            Place((RectTransform)quitButton.transform, new Vector2(0.5f, 0.27f), new Vector2(440f, 120f));
            var quitImage = quitButton.GetComponent<Image>();
            if (quitImage != null) quitImage.color = new Color(0.55f, 0.25f, 0.25f, 1f);

            // ---- cutscene overlay (last sibling = drawn in front of everything)
            RectTransform overlay = GameUiBuilder.CreatePanel(canvas.transform, "CutsceneOverlay", Color.black,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Slot for the GIF player. Put your playback object under this RectTransform.
            RectTransform content = GameUiBuilder.CreateRect(overlay, "CutsceneContent");
            Stretch(content);
            Text placeholder = GameUiBuilder.CreateText(content, "PlaceholderLabel",
                "CUTSCENE\n(put your GIF player under CutsceneContent)", 40, TextAnchor.MiddleCenter,
                new Color(1f, 1f, 1f, 0.35f));
            Stretch(placeholder.rectTransform);

            // Full-screen invisible button on top of the content: click / tap skips, Enter / Space too (it is selected at start).
            RectTransform skipRect = GameUiBuilder.CreatePanel(overlay, "SkipButton", new Color(0f, 0f, 0f, 0f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var skipImage = skipRect.GetComponent<Image>();
            skipImage.raycastTarget = true;
            skipImage.canvasRenderer.cullTransparentMesh = false; // alpha-0 graphics must still receive clicks
            var skipButton = skipRect.gameObject.AddComponent<Button>();
            skipButton.targetGraphic = skipImage;
            skipButton.transition = Selectable.Transition.None;

            Text hint = GameUiBuilder.CreateText(skipRect, "SkipHint", "Click or press Enter to skip", 30,
                TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.6f));
            Stretch(hint.rectTransform, new Vector2(40f, 30f), new Vector2(-40f, -30f));

            // ---- components
            var player = overlay.gameObject.AddComponent<CutscenePlayer>();
            var playerSo = new SerializedObject(player);
            SetFloat(playerSo, "duration", 5f);
            SetBool(playerSo, "playOnEnable", true);
            SetBool(playerSo, "useUnscaledTime", true);
            SetFloat(playerSo, "fadeOutSeconds", 0.25f);
            SetBool(playerSo, "skippable", true);
            SetRef(playerSo, "skipButton", skipButton);
            SetRef(playerSo, "rootToHide", overlay.gameObject);
            SetRef(playerSo, "contentRoot", content);
            playerSo.ApplyModifiedPropertiesWithoutUndo();

            var menuGo = new GameObject("MainMenu");
            var menu = menuGo.AddComponent<MainMenuController>();
            var menuSo = new SerializedObject(menu);
            SetString(menuSo, "gameSceneName", GameSceneName);
            SetRef(menuSo, "playButton", playButton);
            SetRef(menuSo, "quitButton", quitButton);
            SetRef(menuSo, "cutscene", player);
            menuSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, MenuScenePath);
            EnsureBuildSettingsOrder();
            AssetDatabase.SaveAssets();
            Debug.Log("ShapeInventory: main menu scene saved to " + MenuScenePath
                      + ". Open it and press Play; the cutscene overlay plays for 5 s (or until skipped), then reveals the menu.");
        }

        /// <summary>MainMenu = build index 0, the game scene right after it (added if it exists and is missing).</summary>
        private static void EnsureBuildSettingsOrder()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == MenuScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(MenuScenePath, true));

            if (File.Exists(GameScenePath))
            {
                if (!scenes.Exists(s => s.path == GameScenePath))
                    scenes.Insert(1, new EditorBuildSettingsScene(GameScenePath, true));
            }
            else
            {
                Debug.LogWarning("ShapeInventory: " + GameScenePath + " does not exist yet - run 'ShapeInventory/Build Demo Scene' "
                                 + "so the Play button has a scene to load.");
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ------------------------------------------------------------------ helpers

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }

        private static void Stretch(RectTransform rt)
        {
            Stretch(rt, Vector2.zero, Vector2.zero);
        }

        private static void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static SerializedProperty Find(SerializedObject so, string field)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
                Debug.LogWarning("ShapeInventory: field '" + field + "' not found on " + so.targetObject.GetType().Name + " (renamed?).");
            return p;
        }

        private static void SetFloat(SerializedObject so, string field, float value)
        {
            SerializedProperty p = Find(so, field);
            if (p != null) p.floatValue = value;
        }

        private static void SetBool(SerializedObject so, string field, bool value)
        {
            SerializedProperty p = Find(so, field);
            if (p != null) p.boolValue = value;
        }

        private static void SetString(SerializedObject so, string field, string value)
        {
            SerializedProperty p = Find(so, field);
            if (p != null) p.stringValue = value;
        }

        private static void SetRef(SerializedObject so, string field, Object value)
        {
            SerializedProperty p = Find(so, field);
            if (p != null) p.objectReferenceValue = value;
        }
    }
}
