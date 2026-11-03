using UnityEngine;

namespace DigiPurity.Core
{
    public static class GameFonts
    {
        private static Font _mainFont;
        private static Font _signFont;

        /// <summary>
        /// The canonical main game CRT font (VCR OSD Mono) used across
        /// battle HUD, menus, NPC dialogues, and overhead speech bubbles.
        /// Bundled in Resources/Fonts for 100% cross-device standalone player compatibility.
        /// </summary>
        public static Font MainFont
        {
            get
            {
                if (_mainFont != null) return _mainFont;

                // 1. Primary: Runtime Resources load (works across all devices and standalone builds)
                _mainFont = Resources.Load<Font>("Fonts/VCR_OSD_MONO_1.001");
                if (_mainFont == null)
                {
                    _mainFont = Resources.Load<Font>("Fonts/HachicroUndertaleBattleFontRegular-L3zlg");
                }
                if (_mainFont == null)
                {
                    _mainFont = Resources.Load<Font>("Fonts/Pixelta");
                }

#if UNITY_EDITOR
                // 2. Editor fallback in case Resources cache has not refreshed yet
                if (_mainFont == null)
                {
                    _mainFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/VCR_OSD_MONO_1.001.ttf");
                }
                if (_mainFont == null)
                {
                    _mainFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/VCR_OSD_MONO_1.001.ttf");
                }
                if (_mainFont == null)
                {
                    _mainFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/HachicroUndertaleBattleFontRegular-L3zlg.ttf");
                }
                if (_mainFont == null)
                {
                    _mainFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/HachicroUndertaleBattleFontRegular-L3zlg.ttf");
                }
                if (_mainFont == null)
                {
                    _mainFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/Pixelta.ttf");
                }
                if (_mainFont == null)
                {
                    _mainFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Pixelta.ttf");
                }
#endif

                // 3. Last-resort fallback to Unity built-in runtime font
                if (_mainFont == null)
                {
                    _mainFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return _mainFont;
            }
        }

        /// <summary>
        /// Undertale battle font strictly reserved for the Lucas Sign at spawn.
        /// Bundled in Resources/Fonts for 100% cross-device standalone player compatibility.
        /// </summary>
        public static Font SignFont
        {
            get
            {
                if (_signFont != null) return _signFont;

                // 1. Primary: Runtime Resources load (works across all devices and standalone builds)
                _signFont = Resources.Load<Font>("Fonts/HachicroUndertaleBattleFontRegular-L3zlg");

#if UNITY_EDITOR
                // 2. Editor fallback in case Resources cache has not refreshed yet
                if (_signFont == null)
                {
                    _signFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/HachicroUndertaleBattleFontRegular-L3zlg.ttf");
                }
                if (_signFont == null)
                {
                    _signFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/HachicroUndertaleBattleFontRegular-L3zlg.ttf");
                }
#endif

                if (_signFont == null)
                {
                    _signFont = MainFont;
                }
                return _signFont;
            }
        }
    }
}
