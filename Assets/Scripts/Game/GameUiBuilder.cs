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

        // Quest UI (children of ObjectPanel).
        public RectTransform QuestBubble;
        public Text QuestText;
        public Text ResultText;
        public Text ScoreText;
    }

    /// <summary>References to the quest UI created by <see cref="GameUiBuilder.BuildQuestUi"/>.</summary>
    public sealed class QuestUiRefs
    {
        public RectTransform QuestBubble;
        public Text QuestText;
        public Text ResultText;
        public Text ScoreText;
    }

    /// <summary>
    /// Static helpers that create the plain uGUI bits (panels, button, text) in code.
    /// Used by the editor scene builder AND by <see cref="GameFlowController"/> at runtime when references are missing.
    /// </summary>
    public static class GameUiBuilder
    {
        public static readonly Color PlayerPanelColor = new Color(0.16f, 0.22f, 0.30f, 1f);
        public static readonly Color ObjectPanelColor = new Color(0.24f, 0.19f, 0.30f, 1f);
        public static readonly Color BubbleColor = new Color(0.97f, 0.93f, 0.82f, 1f);
        public static readonly Color BubbleTextColor = new Color(0.16f, 0.11f, 0.10f, 1f);

        /// <summary>Fraction of the object panel height (top) taken by the owner's speech bubble.</summary>
        public const float QuestHeightFraction = 0.16f;
        /// <summary>Fraction of the object panel height (bottom) taken by the result + score lines.</summary>
        public const float ResultHeightFraction = 0.09f;
        /// <summary>Total fraction of the object panel height that is NOT available for the object grid.</summary>
        public const float ReservedHeightFraction = QuestHeightFraction + ResultHeightFraction;

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
        /// Creates the owner's speech bubble (top) and the result/score lines (bottom) as children of the object panel.
        /// The space in between (see <see cref="QuestHeightFraction"/> / <see cref="ResultHeightFraction"/>) is left for the object grid.
        /// </summary>
        public static QuestUiRefs BuildQuestUi(RectTransform objectPanel)
        {
            var refs = new QuestUiRefs();

            // Speech bubble (cream panel, dark wrapped text).
            refs.QuestBubble = CreatePanel(objectPanel, "QuestBubble", BubbleColor,
                new Vector2(0f, 1f - QuestHeightFraction), new Vector2(1f, 1f),
                new Vector2(16f, 6f), new Vector2(-16f, -12f));
            var bubbleImage = refs.QuestBubble.GetComponent<Image>();
            if (bubbleImage != null) bubbleImage.raycastTarget = false;

            refs.QuestText = CreateText(refs.QuestBubble, "QuestText", string.Empty, 26,
                TextAnchor.MiddleLeft, BubbleTextColor);
            Stretch(refs.QuestText.rectTransform, new Vector2(18f, 8f), new Vector2(-18f, -8f));
            ConfigureBestFit(refs.QuestText, 16, 28);

            // Result + score strip.
            var strip = CreateRect(objectPanel, "ResultStrip");
            strip.anchorMin = new Vector2(0f, 0f);
            strip.anchorMax = new Vector2(1f, ResultHeightFraction);
            strip.offsetMin = new Vector2(16f, 8f);
            strip.offsetMax = new Vector2(-16f, 0f);

            refs.ResultText = CreateText(strip, "ResultText", string.Empty, 28, TextAnchor.MiddleCenter, Color.white);
            var rrt = refs.ResultText.rectTransform;
            rrt.anchorMin = new Vector2(0f, 0.36f);
            rrt.anchorMax = new Vector2(1f, 1f);
            rrt.offsetMin = Vector2.zero;
            rrt.offsetMax = Vector2.zero;
            ConfigureBestFit(refs.ResultText, 16, 30);

            refs.ScoreText = CreateText(strip, "ScoreText", string.Empty, 22, TextAnchor.MiddleCenter,
                new Color(0.85f, 0.85f, 0.90f, 1f));
            var srt = refs.ScoreText.rectTransform;
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 0.36f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;
            ConfigureBestFit(refs.ScoreText, 14, 24);

            return refs;
        }

        private static void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static void ConfigureBestFit(Text text, int minSize, int maxSize)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = maxSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        /// <summary>
        /// Builds: status text (top), left panel (player inventory), right panel (object inventory),
        /// Done button (bottom centre), quest bubble + result lines (inside the object panel) as children of the given canvas.
        /// </summary>
        public static GameUiRefs BuildDefaultLayout(Canvas canvas)
        {
            var root = (RectTransform)canvas.transform;
            var refs = new GameUiRefs();

            refs.PlayerPanel = CreatePanel(root, "PlayerPanel", PlayerPanelColor,
                new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(20f, 110f), new Vector2(-10f, -100f));
            refs.ObjectPanel = CreatePanel(root, "ObjectPanel", ObjectPanelColor,
                new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(10f, 110f), new Vector2(-20f, -100f));

            var quest = BuildQuestUi(refs.ObjectPanel);
            refs.QuestBubble = quest.QuestBubble;
            refs.QuestText = quest.QuestText;
            refs.ResultText = quest.ResultText;
            refs.ScoreText = quest.ScoreText;

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
