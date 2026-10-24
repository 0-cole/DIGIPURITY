using UnityEngine;

namespace DigiPurity.Core
{
    public static class GameFonts
    {
        private static Font _mainFont;
        private static Font _signFont;

        /// <summary>
        /// The canonical main game font (VCR OSD Mono from user's downloads) used across
        /// battle HUD, menus, NPC dialogues, and overhead speech bubbles.
        /// </summary>
        public static Font MainFont
        {
            get
            {
                if (_mainFont != null) return _mainFont;

#if UNITY_EDITOR
                _mainFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/VCR_OSD_MONO_1.001.ttf");
#endif
                if (_mainFont == null)
                {
                    _mainFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return _mainFont;
            }
        }

        /// <summary>
        /// Undertale battle font strictly reserved for the Lucas Sign at spawn.
        /// </summary>
        public static Font SignFont
        {
            get
            {
                if (_signFont != null) return _signFont;

#if UNITY_EDITOR
                _signFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/HachicroUndertaleBattleFontRegular-L3zlg.ttf");
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
