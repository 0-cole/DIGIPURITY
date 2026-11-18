using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using DigiPurity.Core;
using DigiPurity.World;

namespace DigiPurity.UI
{
    /// <summary>
    /// Authentic Undertale / Deltarune Title Screen with Vintage CRT TV & VHS Sequence.
    /// Matched 1:1 with authentic retro TV animation:
    /// 1. TV Turn-On: Dot in center -> snaps horizontally into beam -> snaps open vertically
    ///    with live snowy television static (from michael-white-old-tv-turn-off & static-on-older-televisions).
    /// 2. Pre-Protocol Pause: TV is ON, snowy static softens into black CRT void for ~1.6s.
    /// 3. Protocol Flicker: "CONSCIOUSNESS DIGITIZATION PROTOCOL" flickers on, holds steady for ~2.8s, flickers out.
    /// 4. DIGIPURITY Typing: Word strikes in letter-by-letter with heavy sub-bass impacts (zero camera shake).
    /// 5. Word Rise: Completed title glides smoothly to header position (Y = 40).
    /// 6. Deltarune Box Menu: Clean 3px white-bordered box with START GAME, CONTROLS, SETTINGS, QUIT, and Owen's Cyan SOUL.
    /// 7. Asriel Ghost Trail Sway: After 3.5s of menu stillness, title sways with 4 fading cyan ghost afterimages.
    /// </summary>
    public class TitleScreenController : MonoBehaviour
    {
        private static TitleScreenController _instance;
        public static TitleScreenController Instance => _instance;

        public enum TitlePhase
        {
            TvPowerOn,
            PreProtocolPause,
            ProtocolFlicker,
            PreTypingPause,
            IntroTyping,
            IntroHover,
            IntroRising,
            MenuActive
        }

        public enum MenuState
        {
            Main,
            Controls,
            Settings
        }

        [Header("Cinematic Intro Sequence")]
        [Tooltip("Enable VHS TV turn-on and typing sequence on startup.")]
        [SerializeField] private bool enableIntroSequence = true;
        [SerializeField] private float tvPowerDuration = 1.85f;
        [SerializeField] private float preProtocolDuration = 1.6f;
        [SerializeField] private float protocolDuration = 2.8f;
        [SerializeField] private float preTypingDuration = 0.4f;
        [SerializeField] private float letterTypeInterval = 0.32f;
        [SerializeField] private float hoverPauseDuration = 0.75f;
        [SerializeField] private float riseDuration = 0.90f;

        [Header("Scene Transition")]
        [Tooltip("Target scene to load when pressing Play (e.g. 'SampleScene' or 'TestScene').")]
        [SerializeField] private string firstSceneName = "SampleScene";

        [Tooltip("Duration of the fade to black when starting game.")]
        [SerializeField] private float startFadeDuration = 0.6f;

        [Header("Audio")]
        [Tooltip("Title background theme (falls back to Abandonware.mp3 or Daycare.mp3 if unassigned).")]
        [SerializeField] private AudioClip titleBgm;
        [Range(0f, 1f)]
        [SerializeField] private float bgmVolume = 0.5f;

        [Header("Runtime Auto-Bootstrap")]
        [Tooltip("If true, automatically appears when hitting Play in the first scene if not already instantiated.")]
        [SerializeField] private bool autoBootstrapEnabled = true;
        public bool AutoBootstrapEnabled => autoBootstrapEnabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapOnPlay()
        {
            // Only auto-spawn if none exists and user hasn't disabled it
            if (FindAnyObjectByType<TitleScreenController>() == null)
            {
                if (PlayerPrefs.GetInt("DIGIPURITY_DISABLE_TITLE_AUTOBOOT", 0) == 0)
                {
                    var go = new GameObject("TitleScreenController (Auto)");
                    var controller = go.AddComponent<TitleScreenController>();
                    if (!controller.autoBootstrapEnabled)
                    {
                        Destroy(go);
                    }
                }
            }
        }

        // Title sequence state
        private const string FullTitle = "DIGIPURITY";
        private const string ProtocolTitle = "CONSCIOUSNESS DIGITIZATION PROTOCOL";
        private TitlePhase _currentPhase = TitlePhase.TvPowerOn;
        private float _phaseTimer = 0f;
        private int _typedLettersCount = 0;
        private float _typeTimer = 0f;
        private float _hoverTimer = 0f;
        private float _riseTimer = 0f;
        private float _titleY = 205f; // Starts at center (205), rises to header (36)
        private float _compressT = 0f; // 0 = wide typed letters, 1 = compressed dual-font lockup
        private float _menuActiveTimer = 0f; // Tracks time after menu buttons appear for Asriel sway delay

        // VHS / CRT TV visual state
        private float _tvBeamProgress = 0f;
        private float _protocolAlpha = 0f;
        private float _vhsTrackingY = 0f;
        private int _startupFramesRemaining = 4;
        private bool _crtAudioTriggered = false;

        // Menu navigation state
        private MenuState _currentState = MenuState.Main;
        private int _mainMenuIndex = 0;
        private int _settingsIndex = 0;
        private readonly string[] _mainMenuItems = { "START GAME", "CONTROLS", "SETTINGS", "QUIT" };

        // Settings variables (persisted via PlayerPrefs)
        private int _masterVol = 100;
        private int _musicVol = 100;
        private int _sfxVol = 100;
        private bool _isFullscreen = true;

        // Transition & animation state
        private bool _isStartingGame = false;
        private float _soulBlinkTimer = 0f;
        private bool _soulBlinkVisible = true;
        private float _inputCooldown = 0f;

        // Audio components
        private AudioSource _bgmSource;
        private AudioSource _sfxSource;
        private AudioClip _sndCrtTurnOn;
        private AudioClip _sndImpact;
        private AudioClip _sndFinalImpact;
        private AudioClip _sndMove;
        private AudioClip _sndSelect;
        private AudioClip _sndBack;

        // Textures
        private Texture2D _cyanSoulTex;
        private Texture2D _pixelTex;
        private Texture2D _crtBezelTex;
        private Texture2D[] _tvStaticFrames;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            LoadSettings();
            EnsureAudioSources();
            EnsureTextures();
            GenerateAudioClips();
        }

        private void Start()
        {
            // Apply initial audio and screen settings
            ApplyAudioSettings();
            Screen.fullScreen = _isFullscreen;

            // Lock any player controller present in current scene until game begins
            var player = FindAnyObjectByType<OverworldPlayerController>();
            if (player != null)
            {
                player.InputLocked = true;
            }

            if (!enableIntroSequence)
            {
                SkipIntroImmediately();
            }
            else
            {
                _currentPhase = TitlePhase.TvPowerOn;
                _phaseTimer = 0f;
                _tvBeamProgress = 0f;
                _protocolAlpha = 0f;
                _titleY = 205f;
                _compressT = 0f;
                _startupFramesRemaining = 4;
                _crtAudioTriggered = false;
            }
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);

            // Update VHS tracking tape scroll
            _vhsTrackingY = (_vhsTrackingY + (dt * 45f)) % 480f;

            if (_isStartingGame)
            {
                // Soul blink animation on confirm
                _soulBlinkTimer += dt;
                if (_soulBlinkTimer >= 0.08f)
                {
                    _soulBlinkTimer = 0f;
                    _soulBlinkVisible = !_soulBlinkVisible;
                }
                return;
            }

            // Wait out initial Unity Editor Play-Mode / Domain Reload hitch before starting CRT animation
            if (_startupFramesRemaining > 0)
            {
                _startupFramesRemaining--;
                return;
            }

            if (!_crtAudioTriggered && _currentPhase == TitlePhase.TvPowerOn)
            {
                _crtAudioTriggered = true;
                PlaySfx(_sndCrtTurnOn);
            }

            // Handle Intro Sequence State Machine
            if (_currentPhase != TitlePhase.MenuActive)
            {
                UpdateIntroSequence(dt);
                return;
            }

            // Once in MenuActive phase, accumulate timer for Asriel title sway delay
            _menuActiveTimer += dt;

            if (_inputCooldown > 0f)
            {
                _inputCooldown -= dt;
            }

            HandleInput();
        }

        private void UpdateIntroSequence(float dt)
        {
            // Allow player to skip intro sequence with any confirm or cancel key using New Input System
            if (GetAnySkipPressed())
            {
                SkipIntroImmediately();
                return;
            }

            switch (_currentPhase)
            {
                case TitlePhase.TvPowerOn:
                    _phaseTimer += dt;
                    _tvBeamProgress = Mathf.Clamp01(_phaseTimer / tvPowerDuration);

                    if (_phaseTimer >= tvPowerDuration)
                    {
                        _currentPhase = TitlePhase.PreProtocolPause;
                        _phaseTimer = 0f;
                    }
                    break;

                case TitlePhase.PreProtocolPause:
                    _phaseTimer += dt;
                    if (_phaseTimer >= preProtocolDuration)
                    {
                        _currentPhase = TitlePhase.ProtocolFlicker;
                        _phaseTimer = 0f;
                    }
                    break;

                case TitlePhase.ProtocolFlicker:
                    _phaseTimer += dt;

                    // Protocol text flicker timing:
                    // 0.0s - 0.35s: rapid random flicker ON
                    // 0.35s - 2.45s: steady cathode tube hum illumination
                    // 2.45s - 2.80s: rapid random flicker OUT to black
                    if (_phaseTimer < 0.35f)
                    {
                        _protocolAlpha = (UnityEngine.Random.value > 0.3f) ? UnityEngine.Random.Range(0.6f, 1f) : 0f;
                    }
                    else if (_phaseTimer < (protocolDuration - 0.35f))
                    {
                        _protocolAlpha = 0.92f + (Mathf.Sin(Time.unscaledTime * 50f) * 0.08f);
                    }
                    else if (_phaseTimer < protocolDuration)
                    {
                        _protocolAlpha = (UnityEngine.Random.value > 0.5f) ? UnityEngine.Random.Range(0.2f, 0.7f) : 0f;
                    }
                    else
                    {
                        _protocolAlpha = 0f;
                        _currentPhase = TitlePhase.PreTypingPause;
                        _phaseTimer = 0f;
                    }
                    break;

                case TitlePhase.PreTypingPause:
                    _phaseTimer += dt;
                    if (_phaseTimer >= preTypingDuration)
                    {
                        _currentPhase = TitlePhase.IntroTyping;
                        _typedLettersCount = 0;
                        _typeTimer = 0.15f;
                        _compressT = 0f;
                    }
                    break;

                case TitlePhase.IntroTyping:
                    _typeTimer -= dt;
                    if (_typeTimer <= 0f)
                    {
                        if (_typedLettersCount < FullTitle.Length)
                        {
                            _typedLettersCount++;
                            bool isLastLetter = (_typedLettersCount == FullTitle.Length);
                            TriggerImpact(isLastLetter);
                            _typeTimer = letterTypeInterval;
                        }
                        else
                        {
                            _currentPhase = TitlePhase.IntroHover;
                            _hoverTimer = hoverPauseDuration;
                        }
                    }
                    break;

                case TitlePhase.IntroHover:
                    _hoverTimer -= dt;
                    // Begin smoothly compressing letters right after typing completes
                    float hoverProgress = Mathf.Clamp01(1f - (_hoverTimer / Mathf.Max(0.01f, hoverPauseDuration)));
                    _compressT = Mathf.SmoothStep(0f, 0.45f, hoverProgress);
                    if (_hoverTimer <= 0f)
                    {
                        _currentPhase = TitlePhase.IntroRising;
                        _riseTimer = 0f;
                    }
                    break;

                case TitlePhase.IntroRising:
                    _riseTimer += dt;
                    float t = Mathf.Clamp01(_riseTimer / riseDuration);
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);
                    _titleY = Mathf.Lerp(205f, 36f, smoothT);
                    _compressT = Mathf.SmoothStep(0.45f, 1f, Mathf.Clamp01(t / 0.85f));

                    if (t >= 1f)
                    {
                        _titleY = 36f;
                        _compressT = 1f;
                        _currentPhase = TitlePhase.MenuActive;
                        PlayTitleMusic();
                    }
                    break;
            }
        }

        private void TriggerImpact(bool isFinal)
        {
            // Zero screen shake on typing per user mandate (impact audio remains crisp & grounded)
            PlaySfx(isFinal ? _sndFinalImpact : _sndImpact);
        }

        private void SkipIntroImmediately()
        {
            _tvBeamProgress = 1f;
            _protocolAlpha = 0f;
            _typedLettersCount = FullTitle.Length;
            _titleY = 36f;
            _compressT = 1f;
            _startupFramesRemaining = 0;
            _crtAudioTriggered = true;
            _currentPhase = TitlePhase.MenuActive;
            _menuActiveTimer = 0f;
            PlayTitleMusic();
        }

        private void HandleInput()
        {
            if (_inputCooldown > 0f) return;

            // Input mapping via New Input System (Keyboard + Gamepad):
            bool up = GetUpPressed();
            bool down = GetDownPressed();
            bool left = GetLeftPressed();
            bool right = GetRightPressed();
            bool confirm = GetConfirmPressed();
            bool cancel = GetCancelPressed();

            switch (_currentState)
            {
                case MenuState.Main:
                    if (up)
                    {
                        _mainMenuIndex = (_mainMenuIndex - 1 + _mainMenuItems.Length) % _mainMenuItems.Length;
                        PlaySfx(_sndMove);
                        _inputCooldown = 0.12f;
                    }
                    else if (down)
                    {
                        _mainMenuIndex = (_mainMenuIndex + 1) % _mainMenuItems.Length;
                        PlaySfx(_sndMove);
                        _inputCooldown = 0.12f;
                    }
                    else if (confirm)
                    {
                        ExecuteMainMenuChoice(_mainMenuIndex);
                    }
                    break;

                case MenuState.Controls:
                    if (confirm || cancel)
                    {
                        _currentState = MenuState.Main;
                        PlaySfx(_sndBack);
                        _inputCooldown = 0.15f;
                    }
                    break;

                case MenuState.Settings:
                    if (up)
                    {
                        _settingsIndex = (_settingsIndex - 1 + 5) % 5;
                        PlaySfx(_sndMove);
                        _inputCooldown = 0.12f;
                    }
                    else if (down)
                    {
                        _settingsIndex = (_settingsIndex + 1) % 5;
                        PlaySfx(_sndMove);
                        _inputCooldown = 0.12f;
                    }
                    else if (left)
                    {
                        AdjustSetting(_settingsIndex, -1);
                        PlaySfx(_sndMove);
                        _inputCooldown = 0.12f;
                    }
                    else if (right)
                    {
                        AdjustSetting(_settingsIndex, 1);
                        PlaySfx(_sndMove);
                        _inputCooldown = 0.12f;
                    }
                    else if (confirm)
                    {
                        if (_settingsIndex == 3) // Display mode toggle
                        {
                            AdjustSetting(3, 1);
                            PlaySfx(_sndSelect);
                        }
                        else if (_settingsIndex == 4) // Back
                        {
                            SaveSettings();
                            _currentState = MenuState.Main;
                            PlaySfx(_sndBack);
                            _inputCooldown = 0.15f;
                        }
                    }
                    else if (cancel)
                    {
                        SaveSettings();
                        _currentState = MenuState.Main;
                        PlaySfx(_sndBack);
                        _inputCooldown = 0.15f;
                    }
                    break;
            }
        }

        #region New Input System Helpers

        private bool GetUpPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)) return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.dpad.up.wasPressedThisFrame || pad.leftStick.up.wasPressedThisFrame)) return true;
            return false;
        }

        private bool GetDownPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)) return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.dpad.down.wasPressedThisFrame || pad.leftStick.down.wasPressedThisFrame)) return true;
            return false;
        }

        private bool GetLeftPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)) return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.dpad.left.wasPressedThisFrame || pad.leftStick.left.wasPressedThisFrame)) return true;
            return false;
        }

        private bool GetRightPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)) return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.dpad.right.wasPressedThisFrame || pad.leftStick.right.wasPressedThisFrame)) return true;
            return false;
        }

        private bool GetConfirmPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.zKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame)) return true;
            return false;
        }

        private bool GetCancelPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.xKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.buttonEast.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame)) return true;
            return false;
        }

        private bool GetAnySkipPressed()
        {
            return GetConfirmPressed() || GetCancelPressed();
        }

        #endregion

        private void ExecuteMainMenuChoice(int index)
        {
            switch (index)
            {
                case 0: // START GAME / PLAY
                    PlaySfx(_sndSelect);
                    StartCoroutine(StartGameRoutine());
                    break;

                case 1: // CONTROLS
                    PlaySfx(_sndSelect);
                    _currentState = MenuState.Controls;
                    _inputCooldown = 0.15f;
                    break;

                case 2: // SETTINGS
                    PlaySfx(_sndSelect);
                    _currentState = MenuState.Settings;
                    _settingsIndex = 0;
                    _inputCooldown = 0.15f;
                    break;

                case 3: // QUIT
                    PlaySfx(_sndBack);
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
        }

        private void AdjustSetting(int index, int delta)
        {
            switch (index)
            {
                case 0: // Master Volume
                    _masterVol = Mathf.Clamp(_masterVol + (delta * 10), 0, 100);
                    ApplyAudioSettings();
                    break;
                case 1: // Music Volume
                    _musicVol = Mathf.Clamp(_musicVol + (delta * 10), 0, 100);
                    ApplyAudioSettings();
                    break;
                case 2: // SFX Volume
                    _sfxVol = Mathf.Clamp(_sfxVol + (delta * 10), 0, 100);
                    ApplyAudioSettings();
                    break;
                case 3: // Fullscreen
                    _isFullscreen = !_isFullscreen;
                    Screen.fullScreen = _isFullscreen;
                    PlayerPrefs.SetInt("DIGIPURITY_FULLSCREEN", _isFullscreen ? 1 : 0);
                    break;
            }
            SaveSettings();
        }

        private IEnumerator StartGameRoutine()
        {
            _isStartingGame = true;

            // Toby Fox 3-blink soul flash on confirmation
            _soulBlinkVisible = false;
            yield return new WaitForSecondsRealtime(0.1f);
            _soulBlinkVisible = true;
            yield return new WaitForSecondsRealtime(0.1f);
            _soulBlinkVisible = false;
            yield return new WaitForSecondsRealtime(0.1f);
            _soulBlinkVisible = true;
            yield return new WaitForSecondsRealtime(0.25f);

            // Fade title BGM out smoothly
            StartCoroutine(FadeOutTitleBgm(startFadeDuration));

            // Fade screen to black
            if (ScreenFader.Instance != null)
            {
                ScreenFader.Instance.Transition(() =>
                {
                    LoadTargetScene();
                }, startFadeDuration, () =>
                {
                    // Transition completed
                    var player = FindAnyObjectByType<OverworldPlayerController>();
                    if (player != null)
                    {
                        player.InputLocked = false;
                    }
                });
            }
            else
            {
                LoadTargetScene();
            }
        }

        private void LoadTargetScene()
        {
            string currentScene = SceneManager.GetActiveScene().name;

            // If a different target scene is specified, load it
            if (!string.IsNullOrEmpty(firstSceneName) && firstSceneName != currentScene)
            {
                // Check if target scene is in build settings
                if (Application.CanStreamedLevelBeLoaded(firstSceneName))
                {
                    SceneManager.LoadScene(firstSceneName);
                    return;
                }
            }

            // Fallback: If already in target scene (e.g. running in SampleScene), deactivate TitleScreen and unlock gameplay
            var player = FindAnyObjectByType<OverworldPlayerController>();
            if (player != null)
            {
                player.InputLocked = false;
            }
            gameObject.SetActive(false);
        }

        private IEnumerator FadeOutTitleBgm(float duration)
        {
            if (_bgmSource == null || !_bgmSource.isPlaying) yield break;

            float startVol = _bgmSource.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }
            _bgmSource.Stop();
        }

        #region OnGUI Rendering (Undertale 4:3 640x480 Matrix + Vintage TV & VHS)

        private void OnGUI()
        {
            GUI.depth = -100;

            Font font = GameFonts.MainFont;
            if (font != null) GUI.skin.font = font;

            Rect rect43 = Get43ScreenPixelRect();

            // Draw solid black letterbox / pillarbox bars outside 4:3 frame
            GUI.color = Color.black;
            if (rect43.x > 0.5f)
            {
                GUI.DrawTexture(new Rect(0f, 0f, rect43.x, Screen.height), _pixelTex);
                GUI.DrawTexture(new Rect(rect43.x + rect43.width, 0f, rect43.x + 4f, Screen.height), _pixelTex);
            }
            if (rect43.y > 0.5f)
            {
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, rect43.y), _pixelTex);
                GUI.DrawTexture(new Rect(0f, rect43.y + rect43.height, Screen.width, rect43.y + 4f), _pixelTex);
            }

            // Single-point 640x480 virtual matrix transform
            Matrix4x4 prevMatrix = GUI.matrix;
            float scaleX = rect43.width / 640f;
            float scaleY = rect43.height / 480f;
            Vector3 origin = new Vector3(rect43.x, rect43.y, 0f);
            GUI.matrix = Matrix4x4.TRS(origin, Quaternion.identity, new Vector3(scaleX, scaleY, 1f));

            GUI.BeginGroup(new Rect(0f, 0f, 640f, 480f));

            // Background black fill
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, 640f, 480f), _pixelTex);
            GUI.color = Color.white;

            // Handle Vintage TV Opening Animation vs Normal Playback
            if (_currentPhase == TitlePhase.TvPowerOn)
            {
                DrawVintageTvOpeningAnimation();
            }
            else
            {
                // Draw Active Sequence Visuals
                if (_currentPhase == TitlePhase.ProtocolFlicker)
                {
                    DrawProtocolSplash();
                }
                else if (_currentPhase >= TitlePhase.IntroTyping)
                {
                    // Render DIGIPURITY Title
                    DrawTitleHeader();

                    // Only render menus once title has locked into position at the top
                    if (_currentPhase == TitlePhase.MenuActive)
                    {
                        switch (_currentState)
                        {
                            case MenuState.Main:
                                DrawMainMenuBox();
                                break;
                            case MenuState.Controls:
                                DrawControlsBox();
                                break;
                            case MenuState.Settings:
                                DrawSettingsBox();
                                break;
                        }
                    }
                }

                // Render Retro VHS & Curved CRT Bezel Overlay
                DrawVhsOverlay();
            }

            GUI.EndGroup();
            GUI.matrix = prevMatrix;
            GUI.color = Color.white;
        }

        private void DrawVintageTvOpeningAnimation()
        {
            // Matched 1:1 with michael-white-old-tv-turn-off.gif (reversed) & static-on-older-televisions:
            // 0.00 - 0.16: Dark charcoal CRT glass tube sitting powered off inside curved fisheye bezel
            // 0.16 - 0.30: Crisp bright white center phosphor dot snaps alive
            // 0.30 - 0.44: Snaps into white horizontal scan beam with signature central square bulge
            // 0.44 - 0.58: Snaps vertically open into solid blazing pure-white CRT flash
            // 0.58 - 0.90: Bright snowy white/grey television static with rolling CRT sync hum bar
            // 0.90 - 1.00: Static locks/dims down into dark VHS channel
            float p = _tvBeamProgress;
            float cx = 320f;
            float cy = 240f;

            // 1. Base CRT Glass Surface (dark slate/charcoal so the curved CRT tube silhouette is clearly visible)
            if (p < 0.58f)
            {
                GUI.color = new Color(0.095f, 0.102f, 0.118f, 1f);
                GUI.DrawTexture(new Rect(0f, 0f, 640f, 480f), _pixelTex);

                // Subtle CRT convex glass highlight glint on dark tube
                GUI.color = new Color(1f, 1f, 1f, 0.025f);
                GUI.DrawTexture(new Rect(40f, 34f, 220f, 16f), _pixelTex);
                GUI.DrawTexture(new Rect(40f, 34f, 18f, 140f), _pixelTex);
            }

            if (p < 0.16f)
            {
                // Tube warming up (powered-off dark cathode glass)
            }
            else if (p < 0.30f)
            {
                // Stage 2: Crisp bright white pixel dot in exact center (Frame 5 of turn-off GIF)
                float dotSize = 12f;
                GUI.color = new Color(1f, 1f, 1f, 0.25f);
                GUI.DrawTexture(new Rect(cx - 10f, cy - 10f, 20f, 20f), _pixelTex);

                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(cx - (dotSize * 0.5f), cy - (dotSize * 0.5f), dotSize, dotSize), _pixelTex);
            }
            else if (p < 0.44f)
            {
                // Stage 3: Pure white horizontal beam across screen + signature center square bulge (Frame 4 of GIF)
                float beamH = 8f;
                float bulgeW = 48f;
                float bulgeH = 34f;

                // Soft outer phosphor bleed
                GUI.color = new Color(0.85f, 0.95f, 1f, 0.28f);
                GUI.DrawTexture(new Rect(0f, cy - (beamH * 0.5f) - 3f, 640f, beamH + 6f), _pixelTex);
                GUI.DrawTexture(new Rect(cx - (bulgeW * 0.5f) - 4f, cy - (bulgeH * 0.5f) - 4f, bulgeW + 8f, bulgeH + 8f), _pixelTex);

                // Crisp solid pure-white horizontal scan line + central bulge
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(0f, cy - (beamH * 0.5f), 640f, beamH), _pixelTex);
                GUI.DrawTexture(new Rect(cx - (bulgeW * 0.5f), cy - (bulgeH * 0.5f), bulgeW, bulgeH), _pixelTex);
            }
            else if (p < 0.58f)
            {
                // Stage 4: Rapid vertical snap into full blazing pure-white CRT screen flash (Frame 3 of GIF)
                float expandP = Mathf.Clamp01((p - 0.44f) / 0.05f);
                float h = Mathf.Lerp(34f, 480f, expandP);
                float y = (480f - h) * 0.5f;

                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(0f, y, 640f, h), _pixelTex);
            }
            else if (p < 0.90f)
            {
                // Stage 5: High-contrast snowy grey/white TV static + rolling CRT hum bar (Frames 0-2 + static GIF)
                DrawAnimatedTvStatic(new Rect(0f, 0f, 640f, 480f), 0.96f);

                // Rolling CRT vertical sync hum bar (diagonal/horizontal dark & cyan interference band)
                float humProgress = ((p - 0.58f) / 0.32f) * 1.6f;
                float humY = (humProgress * 480f) % 520f - 40f;
                GUI.color = new Color(0.05f, 0.08f, 0.12f, 0.48f);
                GUI.DrawTexture(new Rect(0f, humY, 640f, 28f), _pixelTex);
                GUI.color = new Color(0.35f, 0.75f, 0.95f, 0.25f);
                GUI.DrawTexture(new Rect(0f, humY + 28f, 640f, 6f), _pixelTex);

                // Scanlines over the static
                GUI.color = new Color(0f, 0f, 0f, 0.22f);
                for (int sy = 0; sy < 480; sy += 4)
                {
                    GUI.DrawTexture(new Rect(0f, sy, 640f, 2f), _pixelTex);
                }
            }
            else
            {
                // Stage 6: Static clicks/dims down into dark VHS channel with brief tracking roll
                float dropP = (p - 0.90f) / 0.10f;
                float staticAlpha = Mathf.Lerp(0.90f, 0.12f, dropP);
                DrawAnimatedTvStatic(new Rect(0f, 0f, 640f, 480f), staticAlpha);

                // Brief VHS horizontal line tear as channel locks in
                float tearY = Mathf.Lerp(240f, 480f, dropP);
                GUI.color = new Color(1f, 1f, 1f, (1f - dropP) * 0.4f);
                GUI.DrawTexture(new Rect(0f, tearY, 640f, 4f), _pixelTex);
            }

            // Always apply curved fisheye CRT glass bezel & corner mask on top of the TV opening animation
            DrawCrtBezelOverlay();
        }

        private void DrawAnimatedTvStatic(Rect rect, float alpha)
        {
            if (_tvStaticFrames == null || _tvStaticFrames.Length == 0) return;

            // Cycle through random noise frames at 25 FPS
            int frameIdx = (int)(Time.unscaledTime * 25f) % _tvStaticFrames.Length;
            Texture2D frame = _tvStaticFrames[frameIdx];

            if (frame != null)
            {
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTexture(rect, frame, ScaleMode.StretchToFill);
                GUI.color = Color.white;
            }
        }

        private void DrawProtocolSplash()
        {
            if (_protocolAlpha <= 0.01f) return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.3f, 0.95f, 0.9f, _protocolAlpha) } // Glowing cyan CRT terminal
            };

            // Terminal cyber brackets around protocol title
            string text = $"[ {ProtocolTitle} ]";

            // Subtle glow drop shadow
            GUI.color = new Color(0.1f, 0.4f, 0.45f, _protocolAlpha * 0.6f);
            GUI.Label(new Rect(2f, 222f, 640f, 40f), text, style);
            GUI.color = Color.white;
            GUI.Label(new Rect(0f, 220f, 640f, 40f), text, style);

            // Sub-indicator
            var subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.5f, 0.65f, 0.7f, _protocolAlpha * 0.75f) }
            };
            GUI.Label(new Rect(0f, 255f, 640f, 20f), "SYS_INIT // CH_03 // TAPE_SYNC", subStyle);
        }

        private void DrawVhsOverlay()
        {
            // 1. Subtle snowy TV static (quieter during active menus)
            float staticAlpha = (_currentPhase == TitlePhase.PreProtocolPause || _currentPhase == TitlePhase.PreTypingPause) ? 0.14f : 0.045f;
            DrawAnimatedTvStatic(new Rect(0f, 0f, 640f, 480f), staticAlpha);

            // 2. Alternating CRT scanlines across 480 lines
            GUI.color = new Color(0f, 0f, 0f, 0.16f);
            for (int y = 0; y < 480; y += 4)
            {
                GUI.DrawTexture(new Rect(0f, y, 640f, 2f), _pixelTex);
            }

            // 3. Moving VHS tape tracking noise distortion bar
            float trackY = _vhsTrackingY;
            GUI.color = new Color(1f, 1f, 1f, 0.045f);
            GUI.DrawTexture(new Rect(0f, trackY, 640f, 12f), _pixelTex);
            GUI.color = new Color(0f, 0f, 0f, 0.07f);
            GUI.DrawTexture(new Rect(0f, trackY + 12f, 640f, 4f), _pixelTex);

            // 4. Curved CRT Glass Bezel (fisheye rounded corners & cathode vignette)
            DrawCrtBezelOverlay();

            GUI.color = Color.white;
        }

        private void DrawCrtBezelOverlay()
        {
            if (_crtBezelTex != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(0f, 0f, 640f, 480f), _crtBezelTex);
            }
            GUI.color = Color.white;
        }

        private void DrawTitleHeader()
        {
            int visibleCount = (_currentPhase == TitlePhase.IntroTyping)
                ? Mathf.Clamp(_typedLettersCount, 0, FullTitle.Length)
                : FullTitle.Length;

            if (visibleCount <= 0) return;

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 42,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.95f, 1f) }
            };

            var shadowStyle = new GUIStyle(titleStyle)
            {
                normal = { textColor = new Color(0.12f, 0.35f, 0.45f, 0.7f) }
            };

            // Interpolate letter spacing from wide typed spread (52px) down to compressed header (27px)
            float letterPitch = Mathf.Lerp(52f, 27f, _compressT);

            // Calculate Asriel-style horizontal & vertical sway that builds over time up to a smooth cap
            float swayX = 0f;
            float swayY = 0f;
            const float swayStartDelay = 1.2f;

            if (_currentPhase == TitlePhase.MenuActive && _menuActiveTimer >= swayStartDelay)
            {
                float swayTime = _menuActiveTimer - swayStartDelay;
                const float swayFreq = 1.85f;

                float currentRamp = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(swayTime / 14f));
                float currentEntry = Mathf.Clamp01(swayTime / 1.0f);
                float currentAmpX = Mathf.Lerp(12f, 48f, currentRamp) * currentEntry;
                float currentAmpY = Mathf.Lerp(2.5f, 8f, currentRamp) * currentEntry;

                swayX = Mathf.Sin(swayTime * swayFreq) * currentAmpX;
                swayY = Mathf.Sin(swayTime * swayFreq * 2f) * currentAmpY;

                // Afterimage count ramps smoothly from 3 up to 10 (capped) as sway intensifies
                float targetGhosts = Mathf.Lerp(3f, 10f, currentRamp);
                int ghostCount = Mathf.CeilToInt(targetGhosts);

                for (int i = ghostCount; i >= 1; i--)
                {
                    float pastTime = swayTime - (i * 0.055f);
                    if (pastTime <= 0f) continue;

                    float pastRamp = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(pastTime / 14f));
                    float pastEntry = Mathf.Clamp01(pastTime / 1.0f);
                    float pastAmpX = Mathf.Lerp(12f, 48f, pastRamp) * pastEntry;
                    float pastAmpY = Mathf.Lerp(2.5f, 8f, pastRamp) * pastEntry;

                    float trailX = Mathf.Sin(pastTime * swayFreq) * pastAmpX;
                    float trailY = Mathf.Sin(pastTime * swayFreq * 2f) * pastAmpY;

                    float distNorm = (float)i / Mathf.Max(1f, targetGhosts);
                    float newGhostFade = Mathf.Clamp01(targetGhosts - (i - 1));
                    float ghostAlpha = Mathf.Pow(1f - distNorm * 0.85f, 1.4f) * Mathf.Lerp(0.32f, 0.44f, currentRamp) * currentEntry * newGhostFade;
                    if (ghostAlpha <= 0.01f) continue;

                    Color ghostColor = Color.Lerp(
                        new Color(0.28f, 0.92f, 1f, ghostAlpha),
                        new Color(0.14f, 0.52f, 1f, ghostAlpha),
                        distNorm
                    );

                    var ghostStyle = new GUIStyle(titleStyle)
                    {
                        normal = { textColor = ghostColor }
                    };
                    DrawSpacedTitleRow(visibleCount, letterPitch, trailX, _titleY + trailY, ghostStyle);
                }
            }

            // Subtle drop-shadow for authentic pixel punch
            GUI.color = Color.white;
            DrawSpacedTitleRow(visibleCount, letterPitch, swayX + 2f, _titleY + swayY + 2f, shadowStyle);
            DrawSpacedTitleRow(visibleCount, letterPitch, swayX, _titleY + swayY, titleStyle);
        }

        private void DrawSpacedTitleRow(int visibleCount, float letterPitch, float offsetX, float baseY, GUIStyle style)
        {
            float halfSpan = (visibleCount - 1) * 0.5f;
            for (int c = 0; c < visibleCount; c++)
            {
                float charCenterX = 320f + ((c - halfSpan) * letterPitch) + offsetX;
                string ch = FullTitle[c].ToString();
                GUI.Label(new Rect(charCenterX - 24f, baseY, 48f, 60f), ch, style);
            }
        }

        private void DrawMainMenuBox()
        {
            // Deltarune-style clean black box with 3px solid white border
            Rect boxRect = new Rect(190f, 165f, 260f, 210f);
            DrawDeltaruneBox(boxRect, 3);

            var itemStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            float startY = 185f;
            float stepY = 44f;
            float heartBobX = Mathf.Sin(Time.unscaledTime * 6.5f) * 2.5f;

            for (int i = 0; i < _mainMenuItems.Length; i++)
            {
                float y = startY + (i * stepY);
                bool isSelected = (i == _mainMenuIndex);

                if (isSelected)
                {
                    itemStyle.normal.textColor = new Color(1f, 0.92f, 0.2f); // Undertale yellow highlight

                    // Draw Cyan SOUL Cursor
                    if (_soulBlinkVisible)
                    {
                        Rect soulRect = new Rect(212f + heartBobX, y + 2f, 18f, 18f);
                        DrawSoulCursor(soulRect);
                    }
                }
                else
                {
                    itemStyle.normal.textColor = new Color(0.65f, 0.65f, 0.65f); // Unselected grey
                }

                GUI.Label(new Rect(244f, y, 180f, 24f), _mainMenuItems[i], itemStyle);
            }

            // Bottom Navigation Prompt
            var promptStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.55f, 0.55f, 0.6f) }
            };
            GUI.Label(new Rect(0f, 415f, 640f, 24f), "[ Z / ENTER ] Confirm      [ ARROWS ] Select", promptStyle);
        }

        private void DrawControlsBox()
        {
            // Deltarune-style clean box
            Rect boxRect = new Rect(46f, 150f, 548f, 250f);
            DrawDeltaruneBox(boxRect, 3);

            var headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                normal = { textColor = new Color(0.3f, 0.95f, 0.9f) } // Cyan header
            };
            GUI.Label(new Rect(boxRect.x, boxRect.y + 12f, boxRect.width, 24f), "====== CONTROLS ======", headerStyle);

            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            var valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = false,
                normal = { textColor = new Color(1f, 0.92f, 0.2f) }
            };

            float y = boxRect.y + 44f;
            float lineSpacing = 28f;

            DrawControlRow("MOVE OWEN / SOUL", "[ARROW KEYS] or [W A S D]", y, labelStyle, valueStyle);
            y += lineSpacing;
            DrawControlRow("CONFIRM / ADVANCE", "[Z] or [ENTER] or [SPACE]", y, labelStyle, valueStyle);
            y += lineSpacing;
            DrawControlRow("CANCEL / SPRINT (1.5x)", "[X] or [SHIFT] or [ESC]", y, labelStyle, valueStyle);
            y += lineSpacing;
            DrawControlRow("COMBAT SLOW-DODGE", "[HOLD X / SHIFT] in Dodge Box", y, labelStyle, valueStyle);
            y += lineSpacing;
            DrawControlRow("GRAZE BULLETS", "Skim close to bullets for TP", y, labelStyle, valueStyle);

            // Back prompt inside box
            float backY = boxRect.y + boxRect.height - 34f;
            float heartBobX = Mathf.Sin(Time.unscaledTime * 6.5f) * 2.5f;
            DrawSoulCursor(new Rect(boxRect.x + 195f + heartBobX, backY + 2f, 16f, 16f));

            var backStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(boxRect.x + 220f, backY, 220f, 22f), "BACK (Press X or Z)", backStyle);
        }

        private void DrawControlRow(string action, string keys, float y, GUIStyle labelStyle, GUIStyle valueStyle)
        {
            GUI.Label(new Rect(66f, y, 212f, 24f), action, labelStyle);
            GUI.Label(new Rect(280f, y, 300f, 24f), ":  " + keys, valueStyle);
        }

        private void DrawSettingsBox()
        {
            Rect boxRect = new Rect(80f, 150f, 480f, 250f);
            DrawDeltaruneBox(boxRect, 3);

            var headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.3f, 0.95f, 0.9f) }
            };
            GUI.Label(new Rect(boxRect.x, boxRect.y + 12f, boxRect.width, 24f), "====== SETTINGS ======", headerStyle);

            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };

            float startY = boxRect.y + 44f;
            float stepY = 36f;
            float heartBobX = Mathf.Sin(Time.unscaledTime * 6.5f) * 2.5f;

            string[] settingLabels = {
                "Master Volume",
                "Music Volume",
                "SFX Volume",
                "Display Mode",
                "BACK"
            };

            string[] settingValues = {
                $"<  {_masterVol}%  >",
                $"<  {_musicVol}%  >",
                $"<  {_sfxVol}%  >",
                _isFullscreen ? "[ FULLSCREEN ]" : "[ WINDOWED ]",
                ""
            };

            for (int i = 0; i < settingLabels.Length; i++)
            {
                float y = startY + (i * stepY);
                bool isSelected = (i == _settingsIndex);

                if (isSelected)
                {
                    labelStyle.normal.textColor = new Color(1f, 0.92f, 0.2f);
                    DrawSoulCursor(new Rect(100f + heartBobX, y + 2f, 16f, 16f));
                }
                else
                {
                    labelStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                }

                GUI.Label(new Rect(126f, y, 160f, 24f), settingLabels[i], labelStyle);

                if (!string.IsNullOrEmpty(settingValues[i]))
                {
                    var valStyle = new GUIStyle(labelStyle)
                    {
                        alignment = TextAnchor.MiddleRight,
                        normal = { textColor = isSelected ? Color.white : new Color(0.6f, 0.6f, 0.6f) }
                    };
                    GUI.Label(new Rect(280f, y, 240f, 24f), settingValues[i], valStyle);
                }
            }

            // Bottom Prompt
            var promptStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.5f, 0.5f, 0.55f) }
            };
            GUI.Label(new Rect(0f, 415f, 640f, 24f), "[ ARROWS ] Adjust Values      [ Z / X ] Return", promptStyle);
        }

        private void DrawDeltaruneBox(Rect rect, int borderWidth)
        {
            // Pure black interior
            GUI.color = Color.black;
            GUI.DrawTexture(rect, _pixelTex);

            // Crisp solid white borders
            GUI.color = Color.white;
            // Top
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, borderWidth), _pixelTex);
            // Bottom
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - borderWidth, rect.width, borderWidth), _pixelTex);
            // Left
            GUI.DrawTexture(new Rect(rect.x, rect.y, borderWidth, rect.height), _pixelTex);
            // Right
            GUI.DrawTexture(new Rect(rect.x + rect.width - borderWidth, rect.y, borderWidth, rect.height), _pixelTex);

            GUI.color = Color.white;
        }

        private void DrawSoulCursor(Rect rect)
        {
            if (_cyanSoulTex != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(rect, _cyanSoulTex, ScaleMode.ScaleToFit);
            }
            else
            {
                // Fallback Undertale heart drawn with pixel texture
                GUI.color = new Color(0.2f, 0.95f, 0.95f);
                GUI.DrawTexture(rect, _pixelTex);
                GUI.color = Color.white;
            }
        }

        private Rect Get43ScreenPixelRect()
        {
            float screenW = Screen.width;
            float screenH = Screen.height;
            float targetAspect = 4f / 3f;
            float currentAspect = screenW / screenH;

            if (currentAspect > targetAspect)
            {
                // Screen is wider -> Pillarbox (bars on left/right)
                float width = screenH * targetAspect;
                float x = (screenW - width) * 0.5f;
                return new Rect(x, 0f, width, screenH);
            }
            else
            {
                // Screen is taller -> Letterbox (bars on top/bottom)
                float height = screenW / targetAspect;
                float y = (screenH - height) * 0.5f;
                return new Rect(0f, y, screenW, height);
            }
        }

        #endregion

        #region Audio & Persistence Helpers

        private void EnsureAudioSources()
        {
            if (_bgmSource == null)
            {
                var bgmObj = new GameObject("Title_BGM");
                bgmObj.transform.SetParent(transform);
                _bgmSource = bgmObj.AddComponent<AudioSource>();
                _bgmSource.loop = true;
                _bgmSource.playOnAwake = false;
            }

            if (_sfxSource == null)
            {
                var sfxObj = new GameObject("Title_SFX");
                sfxObj.transform.SetParent(transform);
                _sfxSource = sfxObj.AddComponent<AudioSource>();
                _sfxSource.loop = false;
                _sfxSource.playOnAwake = false;
            }
        }

        private void PlayTitleMusic()
        {
            if (_bgmSource == null) return;

            if (titleBgm == null)
            {
                // Try loading ambient music from Assets/Audio
                titleBgm = LoadAudioClipDirect("Assets/Audio/Abandonware.mp3", AudioType.MPEG);
                if (titleBgm == null)
                {
                    titleBgm = LoadAudioClipDirect("Assets/Audio/Daycare.mp3", AudioType.MPEG);
                }
            }

            if (titleBgm != null && !_bgmSource.isPlaying)
            {
                _bgmSource.clip = titleBgm;
                _bgmSource.volume = (bgmVolume * (_musicVol / 100f)) * (_masterVol / 100f);
                _bgmSource.Play();
            }
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip != null && _sfxSource != null)
            {
                float vol = (_sfxVol / 100f) * (_masterVol / 100f);
                _sfxSource.PlayOneShot(clip, vol);
            }
        }

        private void LoadSettings()
        {
            _masterVol = PlayerPrefs.GetInt("DIGIPURITY_MASTER_VOL", 100);
            _musicVol = PlayerPrefs.GetInt("DIGIPURITY_MUSIC_VOL", 100);
            _sfxVol = PlayerPrefs.GetInt("DIGIPURITY_SFX_VOL", 100);
            _isFullscreen = PlayerPrefs.GetInt("DIGIPURITY_FULLSCREEN", Screen.fullScreen ? 1 : 0) == 1;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt("DIGIPURITY_MASTER_VOL", _masterVol);
            PlayerPrefs.SetInt("DIGIPURITY_MUSIC_VOL", _musicVol);
            PlayerPrefs.SetInt("DIGIPURITY_SFX_VOL", _sfxVol);
            PlayerPrefs.SetInt("DIGIPURITY_FULLSCREEN", _isFullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ApplyAudioSettings()
        {
            AudioListener.volume = _masterVol / 100f;
            if (_bgmSource != null)
            {
                _bgmSource.volume = (bgmVolume * (_musicVol / 100f)) * (_masterVol / 100f);
            }
        }

        private void EnsureTextures()
        {
            if (_pixelTex == null)
            {
                _pixelTex = new Texture2D(1, 1);
                _pixelTex.SetPixel(0, 0, Color.white);
                _pixelTex.Apply();
            }

            if (_cyanSoulTex == null)
            {
                _cyanSoulTex = LoadTextureDirect("Assets/Sprites/Combat/CyanSoul.png");
            }

            if (_crtBezelTex == null)
            {
                _crtBezelTex = GenerateCrtBezelTexture();
            }

            if (_tvStaticFrames == null || _tvStaticFrames.Length == 0)
            {
                _tvStaticFrames = GenerateTvStaticFrames();
            }
        }

        private Texture2D[] GenerateTvStaticFrames()
        {
            int width = 214;
            int height = 160;
            int frameCount = 4;
            Texture2D[] frames = new Texture2D[frameCount];

            for (int f = 0; f < frameCount; f++)
            {
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Repeat;

                Color32[] pixels = new Color32[width * height];
                for (int i = 0; i < pixels.Length; i++)
                {
                    // High-contrast vintage CRT snowy static grain
                    float r = UnityEngine.Random.value;
                    byte v = (byte)(r > 0.5f ? UnityEngine.Random.Range(165, 252) : UnityEngine.Random.Range(25, 115));
                    pixels[i] = new Color32(v, v, v, 255);
                }
                tex.SetPixels32(pixels);
                tex.Apply();
                frames[f] = tex;
            }
            return frames;
        }

        private Texture2D GenerateCrtBezelTexture()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            float rx = 0.96f;
            float ry = 0.95f;
            float cornerRadius = 0.12f;

            for (int y = 0; y < size; y++)
            {
                float ny = (y / (float)size) * 2f - 1f;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x / (float)size) * 2f - 1f;

                    // Fisheye / barrel curvature warp on CRT tube boundary
                    float bx = nx * (1f + 0.055f * ny * ny);
                    float by = ny * (1f + 0.055f * nx * nx);

                    float dx = Mathf.Max(Mathf.Abs(bx) - (rx - cornerRadius), 0f);
                    float dy = Mathf.Max(Mathf.Abs(by) - (ry - cornerRadius), 0f);
                    float innerDist = Mathf.Max(Mathf.Abs(bx) - (rx - cornerRadius), Mathf.Abs(by) - (ry - cornerRadius));
                    float sdf = Mathf.Sqrt(dx * dx + dy * dy) + Mathf.Min(innerDist, 0f) - cornerRadius;

                    if (sdf >= 0f)
                    {
                        // Solid pure black outer TV housing bezel around curved CRT corners
                        tex.SetPixel(x, y, Color.black);
                    }
                    else if (sdf > -0.015f)
                    {
                        // Crisp dark glass bevel rim along the curved CRT inner frame
                        float rimFactor = Mathf.InverseLerp(-0.015f, 0f, sdf);
                        float alpha = Mathf.Lerp(0.65f, 1f, rimFactor);
                        tex.SetPixel(x, y, new Color(0.01f, 0.01f, 0.015f, alpha));
                    }
                    else
                    {
                        // Subtle cathode glass vignette inside the curved screen edges
                        float vig = Mathf.Clamp01((-sdf) / 0.16f);
                        float alpha = (1f - vig) * 0.55f;
                        tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private void GenerateAudioClips()
        {
            // TV CRT Power-on charge & static pop tone
            _sndCrtTurnOn = CreateCrtPowerTone();

            // Undertale cinematic heavy impact tones
            _sndImpact = CreateHeavyImpactTone(0.35f, 130f, 38f);
            _sndFinalImpact = CreateHeavyImpactTone(0.65f, 150f, 28f);

            // Clean Undertale procedural navigation blips
            _sndMove = CreatePulseTone(440f, 0.045f, 0.25f);
            _sndSelect = CreateTwoToneChirp(587f, 880f, 0.12f, 0.35f);
            _sndBack = CreateTwoToneChirp(330f, 220f, 0.08f, 0.3f);
        }

        private AudioClip CreateCrtPowerTone()
        {
            int sampleRate = 44100;
            float duration = tvPowerDuration;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                float sample = 0f;

                // 1. Mechanical TV power switch clack at start (0.00 - 0.04)
                if (progress < 0.04f)
                {
                    float clackEnv = 1f - (progress / 0.04f);
                    sample += (UnityEngine.Random.value * 2f - 1f) * 0.55f * clackEnv;
                    sample += Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.45f * clackEnv;
                }

                // 2. CRT high-voltage tube warmup whine (0.16 - 0.58)
                if (progress >= 0.16f && progress < 0.58f)
                {
                    float p = (progress - 0.16f) / 0.42f;
                    float whineFreq = Mathf.Lerp(260f, 1850f, Mathf.Pow(p, 0.65f));
                    sample += Mathf.Sin(2f * Mathf.PI * whineFreq * t) * 0.18f * p;
                    sample += Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.25f;
                }

                // 3. Snowy TV static hiss burst (0.58 - 0.91)
                if (progress >= 0.58f && progress < 0.91f)
                {
                    sample += (UnityEngine.Random.value * 2f - 1f) * 0.24f;
                    sample += Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.18f;
                }

                // 4. Clean channel-lock click at 0.91
                if (progress >= 0.90f && progress < 0.93f)
                {
                    float lockEnv = 1f - ((progress - 0.90f) / 0.03f);
                    sample += (UnityEngine.Random.value * 2f - 1f) * 0.35f * lockEnv;
                }

                data[i] = Mathf.Clamp(sample, -1f, 1f);
            }
            AudioClip clip = AudioClip.Create("snd_crt_turnon", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateHeavyImpactTone(float duration, float startFreq, float endFreq)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // Deep sub-bass frequency slide down
                float freq = Mathf.Lerp(startFreq, endFreq, Mathf.Pow(progress, 0.35f));
                float decay = Mathf.Exp(-6f * progress);

                // Blend punchy square + sub sine
                float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
                float sqr = Mathf.Sign(sine) * 0.3f;
                float wave = (sine * 0.7f) + sqr;

                // Initial click punch transient
                if (progress < 0.04f)
                {
                    float punch = (UnityEngine.Random.value * 2f - 1f) * 0.45f * (1f - progress / 0.04f);
                    wave += punch;
                }

                data[i] = Mathf.Clamp(wave * 0.75f * decay, -1f, 1f);
            }
            AudioClip clip = AudioClip.Create($"snd_impact_{startFreq}", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreatePulseTone(float frequency, float duration, float amplitude)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = 1f - (t / duration);
                float wave = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * t));
                data[i] = wave * amplitude * decay;
            }
            AudioClip clip = AudioClip.Create("snd_pulse_" + frequency, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateTwoToneChirp(float freq1, float freq2, float duration, float amplitude)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            int half = samples / 2;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = (i < half) ? freq1 : freq2;
                float decay = 1f - (t / duration);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                data[i] = wave * amplitude * decay;
            }
            AudioClip clip = AudioClip.Create("snd_chirp", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static Texture2D LoadTextureDirect(string relativePath)
        {
            return RuntimeAssets.Load<Texture2D>(relativePath);
        }

        public static AudioClip LoadAudioClipDirect(string relativePath, AudioType type)
        {
            return RuntimeAssets.Load<AudioClip>(relativePath);
        }

        #endregion
    }
}
