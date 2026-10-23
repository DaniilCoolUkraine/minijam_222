using UnityEngine;
using UnityEngine.UI;

namespace ShapeInventory.Game
{
    /// <summary>References to the UI pieces created by <see cref="GameUiBuilder.BuildDefaultLayout"/>.</summary>
    public sealed class GameUiRefs
    {
        public RectTransform PlayerPanel;
        public RectTransform ObjectPanel;
        public Button DoneButton;
        public Text DoneLabel;
        public Text StatusText;
    }

    /// <summary>
    /// Static helpers that create the plain uGUI bits (panels, button, text) in code.
    /// Used by the editor scene builder AND by <see cref="GameFlowController"/> at runtime when references are missing.
    /// </summary>
    public static class GameUiBuilder
    {
        public static readonly Color PlayerPanelColor = new Color(0.16f, 0.22f, 0.30f, 1f);
        public static readonly Color ObjectPanelColor = new Color(0.24f, 0.19f, 0.30f, 1f);

        public static Font GetFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var img = go.GetComponent<Image>();
            img.color = color;
            return rt;
        }

        public static Text CreateText(Transform parent, string name, string content, int fontSize,
            TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = GetFont();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, out Text labelText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.20f, 0.62f, 0.35f, 1f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = img;

            labelText = CreateText(rt, "Label", label, 32, TextAnchor.MiddleCenter, Color.white);
            var lrt = labelText.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            return button;
        }

        /// <summary>
        /// Builds: status text (top), left panel (player inventory), right panel (object inventory),
        /// Done button (bottom centre) as children of the given canvas.
        /// </summary>
        public static GameUiRefs BuildDefaultLayout(Canvas canvas)
        {
            var root = (RectTransform)canvas.transform;
            var refs = new GameUiRefs();

            refs.PlayerPanel = CreatePanel(root, "PlayerPanel", PlayerPanelColor,
                new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(20f, 110f), new Vector2(-10f, -100f));
            refs.ObjectPanel = CreatePanel(root, "ObjectPanel", ObjectPanelColor,
                new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(10f, 110f), new Vector2(-20f, -100f));

            refs.StatusText = CreateText(root, "StatusText", string.Empty, 34, TextAnchor.MiddleCenter, Color.white);
            var srt = refs.StatusText.rectTransform;
            srt.anchorMin = new Vector2(0f, 1f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.offsetMin = new Vector2(20f, -95f);
            srt.offsetMax = new Vector2(-20f, -5f);

            refs.DoneButton = CreateButton(root, "DoneButton", "Done", out refs.DoneLabel);
            var brt = (RectTransform)refs.DoneButton.transform;
            brt.anchorMin = new Vector2(0.5f, 0f);
            brt.anchorMax = new Vector2(0.5f, 0f);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.sizeDelta = new Vector2(300f, 72f);
            brt.anchoredPosition = new Vector2(0f, 20f);

            return refs;
        }
    }
}
