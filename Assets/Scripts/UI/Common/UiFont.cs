using UnityEngine;

namespace ShapeInventory.UI.Common
{
    /// <summary>Cached access to the built-in UI font (Unity 6: LegacyRuntime.ttf).</summary>
    public static class UiFont
    {
        private static Font _font;

        public static Font Get()
        {
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _font;
        }
    }
}
