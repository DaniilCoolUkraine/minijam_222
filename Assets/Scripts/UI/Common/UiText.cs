using UnityEngine;
using UnityEngine.UI;

namespace ShapeInventory.UI.Common
{
    /// <summary>Factory for small outlined, non-raycastable labels (enchantment badges).</summary>
    public static class UiText
    {
        private static readonly Color OutlineColor = new Color(0f, 0f, 0f, 0.9f);

        /// <summary>
        /// Creates a bold white legacy Text with a dark outline under <paramref name="parent"/>.
        /// Overflow is enabled in both directions so the text never clips; best-fit is off; raycasts are off.
        /// The caller positions the returned RectTransform (via text.rectTransform).
        /// </summary>
        public static Text CreateOutlinedLabel(RectTransform parent, string name, string content, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);

            var text = go.GetComponent<Text>();
            text.font = UiFont.Get();
            text.fontStyle = FontStyle.Bold;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = false;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.color = Color.white;
            text.text = content;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = OutlineColor;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            return text;
        }
    }
}
