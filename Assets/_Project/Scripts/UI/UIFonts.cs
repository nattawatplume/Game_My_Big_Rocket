using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Font used by the whole runtime UI (legacy uGUI Text).
    /// 1) Resources/UI/Fonts/Main (drop a Thai-capable .ttf there to override)
    /// 2) OS dynamic font: Leelawadee UI / Leelawadee / Tahoma (Thai on Windows) + symbol fallbacks
    /// 3) Unity built-in LegacyRuntime.ttf
    /// </summary>
    public static class UIFonts
    {
        static Font main;

        public static Font Main
        {
            get
            {
                if (main == null) main = Load();
                return main;
            }
        }

        static Font Load()
        {
            Font f = null;
            try { f = Resources.Load<Font>("UI/Fonts/Main"); }
            catch { f = null; }
            if (f != null) return f;

            try
            {
                // First found is used, the rest act as glyph fallbacks (arrows, ◀ ▶ ✕ ...).
                f = Font.CreateDynamicFontFromOSFont(
                    new[] { "Leelawadee UI", "Leelawadee", "Tahoma", "Segoe UI", "Segoe UI Symbol", "Arial" }, 32);
            }
            catch { f = null; }
            if (f != null) return f;

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
