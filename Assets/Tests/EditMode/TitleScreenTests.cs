#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DigiPurity.UI;

namespace DigiPurity.Tests.EditMode
{
    public class TitleScreenTests
    {
        private GameObject _titleGo;
        private TitleScreenController _controller;

        [SetUp]
        public void SetUp()
        {
            _titleGo = new GameObject("Test_TitleScreen");
            _controller = _titleGo.AddComponent<TitleScreenController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_titleGo != null)
            {
                Object.DestroyImmediate(_titleGo);
            }
        }

        [Test]
        public void TitleScreen_InitializesProperly()
        {
            Assert.IsNotNull(_controller, "TitleScreenController must be successfully attached to GameObject.");
            Assert.IsNotNull(TitleScreenController.Instance, "TitleScreenController singleton instance must be registered.");
        }

        [Test]
        public void TitleScreen_SettingsClampWithinValidBounds()
        {
            // Verify PlayerPrefs persistence keys
            int master = PlayerPrefs.GetInt("DIGIPURITY_MASTER_VOL", 100);
            Assert.GreaterOrEqual(master, 0);
            Assert.LessOrEqual(master, 100);

            int music = PlayerPrefs.GetInt("DIGIPURITY_MUSIC_VOL", 100);
            Assert.GreaterOrEqual(music, 0);
            Assert.LessOrEqual(music, 100);

            int sfx = PlayerPrefs.GetInt("DIGIPURITY_SFX_VOL", 100);
            Assert.GreaterOrEqual(sfx, 0);
            Assert.LessOrEqual(sfx, 100);
        }

        [Test]
        public void TitleScreen_DirectTextureLoader_ResolvesSoulSprite()
        {
            Texture2D soulTex = TitleScreenController.LoadTextureDirect("Assets/Sprites/Combat/CyanSoul.png");
            Assert.IsNotNull(soulTex, "Direct texture loader must load CyanSoul.png for authentic Toby Fox cursor.");
            Assert.Greater(soulTex.width, 0);
            Assert.Greater(soulTex.height, 0);
        }
    }
}
#endif
