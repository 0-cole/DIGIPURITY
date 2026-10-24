using DigiPurity.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DigiPurity.World;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DigiPurity.Combat
{
    public class BattleStageController : MonoBehaviour
    {
        public static BattleStageController Instance { get; private set; }

        public enum BattleTutorialPhase
        {
            None = 0,
            Intro1,             // Golfer: "Easy there, kid. Rest your scythe for a second. Let's see your posture."
            Intro2,             // Golfer: "You're digitized now. Everything in this decaying network moves by strict turn rules."
            PromptScroll,       // Golfer: "See those 4 buttons below? Use [W / S] or [A / D] to scroll through your options. Give it a try!"
            WaitingForScroll,   // Player scrolls through buttons
            PromptSelect,       // Golfer: "Good grip! [ENTER] or [Z] confirms your choice. [X] or [ESC] cancels and steps back."
            PromptPurge,        // Golfer: "Notice that first slot: [PURGE]. Attacking chips away at HP until 0 HP. Once down to 0, you can execute a full PURGE to delete a soul forever."
            PromptActAndMercy,  // Golfer: "A lot of bitter souls take that shot... but [ACT] and [PURIFY] let you stabilize corrupted routines instead."
            PromptMercyBar,     // Golfer: "Beside my name is a PURIFY bar. When you connect with someone through [ACT], that bar fills to 100%!"
            PromptDoAct,        // Golfer: "Select [ACT] and give an old sportsman some advice on his swing. Let's see what you've got!"
            WaitingForAct,      // Player chooses [ACT] and performs an action!
            ActResponse,        // Golfer responds to the advice (+50% Purify!)
            MercyFull,          // Golfer: "There it is! 100% Purify! My name's glowing yellow—now choose [PURIFY] to stabilize my code, or [PURGE] to end it with force."
            Completed
        }

        public enum BattleMenuState
        {
            TutorialNarrative,  // Golfer Flowey-style tips locking inputs temporarily
            CommandSelect,      // PURGE, ACT, ITEM, PURIFY
            FightTarget,        // Selecting target enemy (Basic Attack vs PURGE)
            FightAttackAnim,    // Owen swinging, enemy damage shake
            ActSelect,          // Deltarune TALK/ACT options
            ActResponse,        // Showing ACT flavor text
            ItemSelect,         // Item list (or "No items.")
            MercySelect,        // PURIFY submenu (PURIFY or FLEE)
            EnemySpeech,        // Speech bubble bark before dodging
            DodgeBoxPhase,      // Undertale bullet hell dodging (or helpless calm box)
            Victory,            // Enemy defeated / spared
            GameOver,           // Owen HP = 0
            ExitingToOverworld
        }

        [Header("Stage Positioning in Deep Void (No Overworld Bleed)")]
        public static readonly Vector3 VOID_STAGE_POS = new Vector3(1000f, 1000f, 0f);

        [SerializeField] private Vector3 stageCenter = new Vector3(1000f, 1000f, 0f);
        [SerializeField] private Vector3 owenBattlePos = new Vector3(991.3f, 998.5f, 0f);
        [SerializeField] private Vector3 enemyBattlePos = new Vector3(1007.5f, 996.1f, 0f);
        [SerializeField] private Vector3 dodgeBoxPos = new Vector3(1000f, 1001.25f, 0f);

        private float _lastCamAspect = 0f;

        public void RecalculateStagePositions()
        {
            stageCenter = VOID_STAGE_POS;
            transform.position = VOID_STAGE_POS;

            Camera cam = Camera.main;
            float halfW = (cam != null && cam.orthographic) ? cam.orthographicSize * cam.aspect : 6.0f * (16f / 9f);

            // Dynamically anchor Owen to the left screen edge and Golfer to the right screen edge
            float owenX = Mathf.Min(stageCenter.x - halfW + 1.8f, stageCenter.x - 4.5f);
            float enemyX = Mathf.Max(stageCenter.x + halfW - 3.2f, stageCenter.x + 4.8f);

            owenBattlePos = new Vector3(owenX, 998.5f, 0f);
            enemyBattlePos = new Vector3(enemyX, 996.1f, 0f);
            dodgeBoxPos = new Vector3(stageCenter.x, stageCenter.y + 1.25f, 0f);

            if (owenAnimator != null)
            {
                owenAnimator.transform.position = owenBattlePos;
                owenAnimator.SetBasePosition(owenBattlePos);
            }
            if (enemyAnimator != null)
            {
                enemyAnimator.transform.position = enemyBattlePos;
                enemyAnimator.SetBasePosition(enemyBattlePos);
            }
            if (dodgeBox != null)
            {
                dodgeBox.transform.position = dodgeBoxPos;
                dodgeBox.SetCenterAndSize(dodgeBoxPos, new Vector2(3.6f, 2.7f));
            }
        }

        private void OnValidate()
        {
            RecalculateStagePositions();
        }

        [Header("Sprites & UI Icons")]
        [SerializeField] private Sprite owenPortrait;
        [SerializeField] private Sprite iconFight;
        [SerializeField] private Sprite iconAct;
        [SerializeField] private Sprite iconItem;
        [SerializeField] private Sprite iconMercy;

        private static Texture2D _owenPortraitTex;
        private static Texture2D _iconFightTex;
        private static Texture2D _iconActTex;
        private static Texture2D _iconItemTex;
        private static Texture2D _iconMercyTex;

        [Header("Controllers")]
        [SerializeField] private OwenBattleAnimator owenAnimator;
        [SerializeField] private EnemyBattleAnimator enemyAnimator;
        [SerializeField] private BattleDodgeBox dodgeBox;
        [SerializeField] private AudioSource bgmAudioSource;

        [Header("Audio Distortion & Dynamic Slowdown")]
        [SerializeField] private AudioDistortionFilter bgmDistortion;
        [SerializeField] private AudioLowPassFilter bgmLowPass;
        private float _currentBgmPitch = 1.0f;
        private float _targetBgmPitch = 1.0f;
        private float _targetBgmDistortion = 0.0f;
        private float _targetBgmCutoff = 22000.0f;
        private float _flutterIntensity = 0.0f;
        private bool _isMusicStopping = false;

        [Header("Combatant Stats")]
        public int owenMaxHp = 100;
        public int owenCurrentHp = 100;
        public int enemyMaxHp = 100;
        public int enemyCurrentHp = 100;
        public int purgeMarkerHp = 0; // Enemy can be purged once reduced to 0 HP
        public string activeEnemyName = "Golfer";

        public bool IsEnemyAtPurgeMarker => enemyCurrentHp <= 0;

        [Header("Active Enemy Configuration")]
        [SerializeField] private EnemyData activeEnemyData;

        private OverworldPlayerController _overworldPlayer;
        private OverworldEnemy _overworldEnemy;
        private BattleMenuState _menuState = BattleMenuState.CommandSelect;

        private int _selectedCommandIndex = 0; // 0: PURGE, 1: ACT, 2: ITEM, 3: PURIFY
        public int enemyCurrentMercy = 0; // Current Purify progress (0 - 100%)
        public bool isPurifiedVictory = false;
        public BattleTutorialPhase tutorialPhase = BattleTutorialPhase.Intro1;
        public int purgeAttackCount = 0;

        private readonly string[] _commandNames = new string[] { "PURGE", "ACT", "ITEM", "PURIFY" };

        private int _selectedActIndex = 0;
        private int _selectedMercyIndex = 0;
        private string _activeDialogueText = "* Golfer blocks the fairway!";
        private string _speechBarkText = "";
        private float _speechBarkTimer = 0f;
        private int _lastDamageDealt = 0;
        private bool _isPurifiedVictory = true;
        private float _enemySpeechWaitTimer = 0f;
        private float _inputDebounceTimer = 0f;

        [Header("Overhead Speech & Voice Chatter")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioClip voiceBlipClip;

        private string _speechFullText = "";
        private string _speechVisibleText = "";
        private int _speechTypedChars = 0;
        private float _speechCharTimer = 0f;
        private bool _speechIsTyping = false;
        private bool _isSpeechBubbleVisible = false;
        private int _letterSoundStep = 0;

        public BattleMenuState CurrentState => _menuState;

        public static BattleStageController EnsureBattleStageExists()
        {
            var stage = FindAnyObjectByType<BattleStageController>(FindObjectsInactive.Include);
            if (stage == null)
            {
                GameObject stageObj = new GameObject("BattleStage");
                stageObj.transform.position = new Vector3(1000f, 1000f, 0f);
                stage = stageObj.AddComponent<BattleStageController>();
            }

            stage.transform.SetParent(null); // Ensure stage is always a root GameObject!
            stage.transform.position = new Vector3(1000f, 1000f, 0f);
            stage.EnsureCombatantsExist();
            stage.AutoLoadIcons();
            stage.EnsureAudioFilters();

            return stage;
        }

        public void EnsureCombatantsExist()
        {
            // Force deep void coordinates regardless of any stale inspector serialized values
            RecalculateStagePositions();

            // Remove duplicate Light2D to prevent URP 2D conflict warnings (combatants use unlit shaders)
            var lightComp = GetComponent<UnityEngine.Rendering.Universal.Light2D>();
            if (lightComp != null) DestroyImmediate(lightComp);

            // 2. Ensure Owen Battle Entity (Left side)
            if (owenAnimator == null)
            {
                Transform owenChild = transform.Find("Owen_Battle");
                if (owenChild != null) owenAnimator = owenChild.GetComponent<OwenBattleAnimator>();
            }
            if (owenAnimator == null)
            {
                GameObject owenObj = new GameObject("Owen_Battle");
                owenObj.transform.SetParent(transform);
                owenAnimator = owenObj.AddComponent<OwenBattleAnimator>();
            }
            owenAnimator.EnsureRenderer();
            owenAnimator.transform.localScale = new Vector3(0.85f, 0.85f, 1f); // Prominent, clear ~3.0 unit height
            owenAnimator.transform.position = owenBattlePos;
            owenAnimator.SetBasePosition(owenBattlePos);
            owenAnimator.AutoLoadSprites();
            owenAnimator.PlayIdle();

            // 3. Ensure Golfer / Enemy Battle Entity (Right side)
            if (enemyAnimator == null)
            {
                Transform enemyChild = transform.Find("Golfer_Battle");
                if (enemyChild != null) enemyAnimator = enemyChild.GetComponent<EnemyBattleAnimator>();
            }
            if (enemyAnimator == null)
            {
                GameObject enemyObj = new GameObject("Golfer_Battle");
                enemyObj.transform.SetParent(transform);
                var golferAnim = enemyObj.AddComponent<GolferBattleAnimator>();
                enemyAnimator = golferAnim;
            }
            enemyAnimator.EnsureRenderer();
            if (enemyAnimator is GolferBattleAnimator gAnim)
            {
                gAnim.AutoLoadSprite();
            }
            enemyAnimator.transform.localScale = new Vector3(4.35f, 4.35f, 1f); // Imposing tripled size per user direction!
            enemyAnimator.transform.position = enemyBattlePos;
            enemyAnimator.SetBasePosition(enemyBattlePos);
            enemyAnimator.IsBobbing = true;

            // Clean up old overhead coin trays if present
            var oldOwenUI = owenAnimator.GetComponent<BattleCombatantUI>();
            if (oldOwenUI != null) DestroyImmediate(oldOwenUI);
            var oldEnemyUI = enemyAnimator.GetComponent<BattleCombatantUI>();
            if (oldEnemyUI != null) DestroyImmediate(oldEnemyUI);

            // 4. Ensure BattleDodgeBox
            if (dodgeBox == null)
            {
                dodgeBox = GetComponentInChildren<BattleDodgeBox>(true);
            }
            if (dodgeBox == null)
            {
                GameObject dodgeObj = new GameObject("BattleDodgeBox");
                dodgeObj.transform.SetParent(transform);
                dodgeBox = dodgeObj.AddComponent<BattleDodgeBox>();
            }
            dodgeBox.transform.position = dodgeBoxPos;
            dodgeBox.SetCenterAndSize(dodgeBoxPos, new Vector2(3.6f, 2.7f));
            dodgeBox.AutoLoadSprites();

            // 5. Ensure AudioSource & Filters
            if (bgmAudioSource == null) bgmAudioSource = GetComponent<AudioSource>();
            if (bgmAudioSource == null) bgmAudioSource = gameObject.AddComponent<AudioSource>();
            EnsureAudioFilters();
        }

        public void EnsureAudioFilters()
        {
            if (bgmDistortion == null) bgmDistortion = GetComponent<AudioDistortionFilter>();
            if (bgmDistortion == null) bgmDistortion = gameObject.AddComponent<AudioDistortionFilter>();

            if (bgmLowPass == null) bgmLowPass = GetComponent<AudioLowPassFilter>();
            if (bgmLowPass == null) bgmLowPass = gameObject.AddComponent<AudioLowPassFilter>();

            bgmDistortion.distortionLevel = 0f;
            bgmLowPass.cutoffFrequency = 22000f;
            bgmDistortion.enabled = false;
            bgmLowPass.enabled = false;
        }

        public void StartNewTurn(string flavorText = "")
        {
            StartNewPlayerTurn(flavorText);
        }

        private void Awake()
        {
            Instance = this;
            RecalculateStagePositions();
            EnsureCombatantsExist();
            AutoLoadIcons();
            EnsureVoiceComponents();
            EnsureAudioFilters();
        }

        public static Texture2D LoadTextureDirect(string relativePath)
        {
            try
            {
                string fullPath = System.IO.Path.Combine(Application.dataPath, "..", relativePath);
                if (System.IO.File.Exists(fullPath))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.LoadImage(bytes);
                    tex.filterMode = FilterMode.Point;
                    return tex;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BattleStageController] Direct texture load failed for {relativePath}: {ex.Message}");
            }
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);
            if (asset != null) return asset;
#endif
            return null;
        }

        public void AutoLoadIcons()
        {
            _owenPortraitTex = LoadTextureDirect("Assets/Sprites/Combat/Owen_Battle_Portrait.png");
            _iconFightTex = LoadTextureDirect("Assets/Sprites/Combat/Icon_Purge.png");
            if (_iconFightTex == null) _iconFightTex = LoadTextureDirect("Assets/Sprites/Combat/Icon_Fight.png");
            _iconActTex = LoadTextureDirect("Assets/Sprites/Combat/Icon_Act.png");
            _iconItemTex = LoadTextureDirect("Assets/Sprites/Combat/Icon_Item.png");
            _iconMercyTex = LoadTextureDirect("Assets/Sprites/Combat/Icon_Mercy.png");

            if (_owenPortraitTex != null)
                owenPortrait = Sprite.Create(_owenPortraitTex, new Rect(0, 0, _owenPortraitTex.width, _owenPortraitTex.height), new Vector2(0.5f, 0.5f), 100f);
            if (_iconFightTex != null)
                iconFight = Sprite.Create(_iconFightTex, new Rect(0, 0, _iconFightTex.width, _iconFightTex.height), new Vector2(0.5f, 0.5f), 100f);
            if (_iconActTex != null)
                iconAct = Sprite.Create(_iconActTex, new Rect(0, 0, _iconActTex.width, _iconActTex.height), new Vector2(0.5f, 0.5f), 100f);
            if (_iconItemTex != null)
                iconItem = Sprite.Create(_iconItemTex, new Rect(0, 0, _iconItemTex.width, _iconItemTex.height), new Vector2(0.5f, 0.5f), 100f);
            if (_iconMercyTex != null)
                iconMercy = Sprite.Create(_iconMercyTex, new Rect(0, 0, _iconMercyTex.width, _iconMercyTex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private void EnsureVoiceComponents()
        {
            if (voiceAudioSource == null)
            {
                Transform child = transform.Find("VoiceAudioSource");
                if (child != null)
                {
                    voiceAudioSource = child.GetComponent<AudioSource>();
                }
                if (voiceAudioSource == null)
                {
                    GameObject vObj = new GameObject("VoiceAudioSource");
                    vObj.transform.SetParent(transform);
                    voiceAudioSource = vObj.AddComponent<AudioSource>();
                }
                voiceAudioSource.playOnAwake = false;
                voiceAudioSource.loop = false;
                voiceAudioSource.spatialBlend = 0f;
            }
            EnsureVoiceClip();
        }

        private void EnsureVoiceClip()
        {
            if (voiceBlipClip != null) return;
#if UNITY_EDITOR
            voiceBlipClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/snd_txt_golfer.wav");
            if (voiceBlipClip == null)
            {
                voiceBlipClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/snd_txt_default.wav");
            }
#endif
            if (voiceBlipClip == null)
            {
                int rate = 44100;
                int length = (int)(rate * 0.038f);
                float[] samples = new float[length];
                float baseFreq = 185f;
                for (int i = 0; i < length; i++)
                {
                    float t = (float)i / rate;
                    float prog = (float)i / length;
                    float freq = baseFreq * (1f - 0.25f * prog);
                    float phase = (freq * t) % 1f;
                    float raw = phase < 0.35f ? 1f : -1f;
                    float env = Mathf.Exp(-prog * 6.5f) * (1f - Mathf.Exp(-i / (rate * 0.002f)));
                    samples[i] = raw * env * 0.45f;
                }
                voiceBlipClip = AudioClip.Create("snd_txt_golfer_procedural", length, 1, rate, false);
                voiceBlipClip.SetData(samples, 0);
            }
        }

        private void PlayVoiceBlip()
        {
            if (voiceAudioSource == null) EnsureVoiceComponents();
            if (voiceBlipClip == null) EnsureVoiceClip();

            if (voiceAudioSource != null && voiceBlipClip != null)
            {
                voiceAudioSource.pitch = UnityEngine.Random.Range(0.97f, 1.03f);
                voiceAudioSource.PlayOneShot(voiceBlipClip, 0.45f);
            }
        }

        public void SetGolferSpeech(string spokenText, string bottomPrompt = "")
        {
            _speechFullText = spokenText;
            _speechVisibleText = "";
            _speechTypedChars = 0;
            _speechCharTimer = 0f;
            _speechIsTyping = true;
            _isSpeechBubbleVisible = !string.IsNullOrEmpty(spokenText);
            _letterSoundStep = 0;
            if (!string.IsNullOrEmpty(bottomPrompt))
            {
                _activeDialogueText = bottomPrompt;
            }
        }

        public void HideGolferSpeech()
        {
            _speechFullText = "";
            _speechVisibleText = "";
            _speechTypedChars = 0;
            _speechIsTyping = false;
            _isSpeechBubbleVisible = false;
        }

        public bool SkipSpeechTyping()
        {
            if (_speechIsTyping && !string.IsNullOrEmpty(_speechFullText))
            {
                _speechTypedChars = _speechFullText.Length;
                _speechVisibleText = _speechFullText;
                _speechIsTyping = false;
                return true;
            }
            return false;
        }

        private void UpdateSpeechTypewriter()
        {
            if (!_speechIsTyping || string.IsNullOrEmpty(_speechFullText)) return;

            _speechCharTimer += Time.deltaTime;
            float charInterval = 0.030f;

            while (_speechCharTimer >= charInterval && _speechTypedChars < _speechFullText.Length)
            {
                _speechCharTimer -= charInterval;
                char c = _speechFullText[_speechTypedChars];
                _speechTypedChars++;
                _speechVisibleText = _speechFullText.Substring(0, _speechTypedChars);

                if (!char.IsWhiteSpace(c) && !char.IsPunctuation(c) && c != '*' && c != '"' && c != '\'')
                {
                    _letterSoundStep++;
                    if (_letterSoundStep % 2 == 1)
                    {
                        PlayVoiceBlip();
                    }
                }
            }

            if (_speechTypedChars >= _speechFullText.Length)
            {
                _speechIsTyping = false;
            }
        }

        private void Start()
        {
            RecalculateStagePositions();
            EnsureCombatantsExist();
            if (activeEnemyData != null)
            {
                InitializeWithEnemy(activeEnemyData);
            }
            else
            {
                StartNewPlayerTurn("* Golfer winds up a wicked drive!");
            }
        }

        public void InitializeWithEnemy(EnemyData data, OverworldPlayerController player = null, OverworldEnemy enemy = null)
        {
            if (player != null) _overworldPlayer = player;
            if (enemy != null) _overworldEnemy = enemy;

            activeEnemyData = data;
            RecalculateStagePositions();
            EnsureCombatantsExist();

            if (data != null)
            {
                activeEnemyName = data.enemyName;
                enemyMaxHp = data.maxHp > 0 ? data.maxHp : 100;
                enemyCurrentHp = enemyMaxHp;
                purgeMarkerHp = 0; // Enemy can only be purged at 0 HP

                if (enemyAnimator != null)
                {
                    enemyAnimator.SetEnemyVisuals(data.battleSprite, data.battleHurtSprite, 4.35f);
                    enemyAnimator.transform.position = enemyBattlePos;
                    enemyAnimator.SetBasePosition(enemyBattlePos);
                    enemyAnimator.IsBobbing = true;
                }

                if (bgmAudioSource != null)
                {
                    AudioClip clip = (data != null && data.battleBgm != null) ? data.battleBgm : null;
                    if (clip == null)
                    {
#if UNITY_EDITOR
                        clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Battle_Beta.mp3");
#endif
                    }
                    if (clip != null)
                    {
                        // Stop any overworld music so it never clashes or double-plays
                        DigiPurity.World.OverworldBgmManager.Instance.StopMusic();

                        bgmAudioSource.clip = clip;
                        bgmAudioSource.loop = true;
                        bgmAudioSource.volume = 0.85f;
                        bgmAudioSource.pitch = 1.0f;
                        _currentBgmPitch = 1.0f;
                        _targetBgmPitch = 1.0f;
                        _targetBgmDistortion = 0.0f;
                        _targetBgmCutoff = 22000.0f;
                        _flutterIntensity = 0.0f;
                        _isMusicStopping = false;
                        EnsureAudioFilters();
                        bgmAudioSource.Play();
                    }
                }
            }

            // Center Main Camera on the battle stage in deep void
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var roomCam = mainCam.GetComponent<RoomCameraController>();
                if (roomCam != null) roomCam.enabled = false;
                mainCam.transform.position = new Vector3(stageCenter.x, stageCenter.y + 0.5f, -10f);
                mainCam.orthographicSize = 6.0f;
            }

            // Start Golfer flowey-style Weary Sportsman tutorial
            enemyCurrentMercy = 0;
            purgeAttackCount = 0;
            tutorialPhase = BattleTutorialPhase.Intro1;
            _menuState = BattleMenuState.TutorialNarrative;
            _inputDebounceTimer = 0.25f;
            SetGolferSpeech(
                "Easy there, kid. Rest your scythe for a second. Let's see your posture.",
                "* [Z / ENTER] Continue"
            );
        }

        public void StartNewPlayerTurn(string flavorText = "")
        {
            HideGolferSpeech();
            _speechBarkTimer = 0f;
            _menuState = BattleMenuState.CommandSelect;
            _selectedCommandIndex = 0;
            _selectedActIndex = 0;
            _selectedMercyIndex = 0;
            _inputDebounceTimer = 0.15f;

            RecalculateStagePositions();

            if (!string.IsNullOrEmpty(flavorText))
            {
                _activeDialogueText = flavorText;
            }
            else
            {
                _activeDialogueText = (enemyCurrentHp <= 0)
                    ? $"* {activeEnemyName} is exhausted and completely vulnerable. (0 HP)"
                    : $"* {activeEnemyName} prepares for your move.";
            }

            if (owenAnimator != null)
            {
                owenAnimator.transform.position = owenBattlePos;
                owenAnimator.SetBasePosition(owenBattlePos);
                owenAnimator.PlayIdle();
            }

            if (enemyAnimator != null)
            {
                enemyAnimator.transform.position = enemyBattlePos;
                enemyAnimator.SetBasePosition(enemyBattlePos);
                enemyAnimator.IsBobbing = (enemyCurrentHp > 0);
            }
        }

        #region Input System Helpers
        private bool IsLeftDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)) return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.dpad.left.wasPressedThisFrame || pad.leftStick.left.wasPressedThisFrame)) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) return true;
#endif
            return false;
        }

        private bool IsRightDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)) return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.dpad.right.wasPressedThisFrame || pad.leftStick.right.wasPressedThisFrame)) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) return true;
#endif
            return false;
        }

        private bool IsUpDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)) return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.dpad.up.wasPressedThisFrame || pad.leftStick.up.wasPressedThisFrame)) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) return true;
#endif
            return false;
        }

        private bool IsDownDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)) return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.dpad.down.wasPressedThisFrame || pad.leftStick.down.wasPressedThisFrame)) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) return true;
#endif
            return false;
        }

        private bool IsConfirmDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.zKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame)) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) return true;
#endif
            return false;
        }

        private bool IsCancelDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.xKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.buttonEast.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame)) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Escape)) return true;
#endif
            return false;
        }
        #endregion

        private void Update()
        {
            if (_inputDebounceTimer > 0f)
            {
                _inputDebounceTimer -= Time.deltaTime;
            }

            Camera cam = Camera.main;
            if (cam != null && cam.orthographic && !Mathf.Approximately(cam.aspect, _lastCamAspect))
            {
                _lastCamAspect = cam.aspect;
                RecalculateStagePositions();
            }

            UpdateSpeechTypewriter();
            UpdateDynamicAudio();

            if (_speechBarkTimer > 0f)
            {
                _speechBarkTimer -= Time.deltaTime;
                if (_speechBarkTimer <= 0f && tutorialPhase == BattleTutorialPhase.Completed && _menuState != BattleMenuState.TutorialNarrative && _menuState != BattleMenuState.EnemySpeech)
                {
                    HideGolferSpeech();
                }
            }

            switch (_menuState)
            {
                case BattleMenuState.TutorialNarrative:
                    HandleTutorialNarrative();
                    break;
                case BattleMenuState.CommandSelect:
                    HandleCommandSelect();
                    break;
                case BattleMenuState.FightTarget:
                    HandleFightTarget();
                    break;
                case BattleMenuState.ActSelect:
                    HandleActSelect();
                    break;
                case BattleMenuState.ActResponse:
                    if (_inputDebounceTimer <= 0f && (IsConfirmDown() || IsCancelDown()))
                    {
                        if (!SkipSpeechTyping())
                        {
                            HideGolferSpeech();
                            StartEnemyAttackTelegraph();
                        }
                    }
                    break;
                case BattleMenuState.ItemSelect:
                    if (_inputDebounceTimer <= 0f && (IsCancelDown() || IsConfirmDown()))
                    {
                        _menuState = BattleMenuState.CommandSelect;
                        _inputDebounceTimer = 0.15f;
                    }
                    break;
                case BattleMenuState.MercySelect:
                    HandleMercySelect();
                    break;
                case BattleMenuState.EnemySpeech:
                    HandleEnemySpeechTelegraph();
                    break;
                case BattleMenuState.Victory:
                    if (_inputDebounceTimer <= 0f && IsConfirmDown())
                    {
                        ExecuteReturnToOverworld(isPurified: _isPurifiedVictory);
                    }
                    break;
            }
        }

        private void UpdateDynamicAudio()
        {
            if (!_isMusicStopping && bgmAudioSource != null && bgmAudioSource.isPlaying)
            {
                _currentBgmPitch = Mathf.MoveTowards(_currentBgmPitch, _targetBgmPitch, Time.deltaTime * 0.45f);
                if (Mathf.Abs(bgmAudioSource.pitch - _currentBgmPitch) > 0.002f)
                {
                    bgmAudioSource.pitch = _currentBgmPitch;
                }

                if (bgmDistortion != null)
                {
                    bool needDist = _targetBgmDistortion > 0.01f || bgmDistortion.distortionLevel > 0.01f;
                    bgmDistortion.enabled = needDist;
                    if (needDist)
                    {
                        bgmDistortion.distortionLevel = Mathf.MoveTowards(bgmDistortion.distortionLevel, _targetBgmDistortion, Time.deltaTime * 0.40f);
                    }
                }
                if (bgmLowPass != null)
                {
                    bool needLowPass = _targetBgmCutoff < 21000f || bgmLowPass.cutoffFrequency < 21000f;
                    bgmLowPass.enabled = needLowPass;
                    if (needLowPass)
                    {
                        bgmLowPass.cutoffFrequency = Mathf.MoveTowards(bgmLowPass.cutoffFrequency, _targetBgmCutoff, Time.deltaTime * 16000f);
                    }
                }
            }
        }

        private void HandleTutorialNarrative()
        {
            if (_inputDebounceTimer > 0f) return;

            if (IsConfirmDown())
            {
                if (SkipSpeechTyping())
                {
                    _inputDebounceTimer = 0.12f;
                    return;
                }

                _inputDebounceTimer = 0.22f; // Debounce so spamming Z doesn't bleed into command select!

                switch (tutorialPhase)
                {
                    case BattleTutorialPhase.Intro1:
                        tutorialPhase = BattleTutorialPhase.Intro2;
                        SetGolferSpeech(
                            "You're digitized now. Everything in this decaying network moves by strict turn rules.",
                            "* [Z / ENTER] Next"
                        );
                        break;

                    case BattleTutorialPhase.Intro2:
                        tutorialPhase = BattleTutorialPhase.PromptScroll;
                        SetGolferSpeech(
                            "See those 4 buttons below? Use [W / S] or [A / D] to scroll through your options. Give it a try!",
                            "* [Z / ENTER] Practice"
                        );
                        break;

                    case BattleTutorialPhase.PromptScroll:
                        tutorialPhase = BattleTutorialPhase.WaitingForScroll;
                        _menuState = BattleMenuState.CommandSelect;
                        SetGolferSpeech(
                            "Give it a try! Scroll through your commands.",
                            "* Use [W / S] or [A / D] to scroll through your commands."
                        );
                        break;

                    case BattleTutorialPhase.PromptSelect:
                        tutorialPhase = BattleTutorialPhase.PromptPurge;
                        SetGolferSpeech(
                            "Notice that first slot: [PURGE]. Attacking chips away at HP until 0 HP. Once down to 0, you can execute a full PURGE to delete a soul forever.",
                            "* [Z / ENTER] Next"
                        );
                        break;

                    case BattleTutorialPhase.PromptPurge:
                        tutorialPhase = BattleTutorialPhase.PromptActAndMercy;
                        SetGolferSpeech(
                            "A lot of bitter souls take that shot... but [ACT] and [PURIFY] let you stabilize corrupted routines instead.",
                            "* [Z / ENTER] Next"
                        );
                        break;

                    case BattleTutorialPhase.PromptActAndMercy:
                        tutorialPhase = BattleTutorialPhase.PromptMercyBar;
                        SetGolferSpeech(
                            "Beside my name is a PURIFY bar. When you connect with someone through [ACT], that bar fills to 100%!",
                            "* [Z / ENTER] Next"
                        );
                        break;

                    case BattleTutorialPhase.PromptMercyBar:
                        tutorialPhase = BattleTutorialPhase.PromptDoAct;
                        SetGolferSpeech(
                            "Select [ACT] and give an old sportsman some advice on his swing. Let's see what you've got!",
                            "* [Z / ENTER] Open ACT"
                        );
                        break;

                    case BattleTutorialPhase.PromptDoAct:
                        tutorialPhase = BattleTutorialPhase.WaitingForAct;
                        _menuState = BattleMenuState.CommandSelect;
                        _selectedCommandIndex = 1; // Default to ACT
                        SetGolferSpeech(
                            "Show me what you've got, kid.",
                            "* Select [ACT] to interact with Golfer."
                        );
                        break;

                    case BattleTutorialPhase.MercyFull:
                        tutorialPhase = BattleTutorialPhase.Completed;
                        _menuState = BattleMenuState.CommandSelect;
                        _selectedCommandIndex = 3; // Hover PURIFY
                        HideGolferSpeech();
                        _activeDialogueText = "* Golfer is 100% stabilized! Choose [PURIFY] to cleanse, or [PURGE] to attack.";
                        break;
                }
            }
        }

        private void HandleCommandSelect()
        {
            bool moved = false;
            if (IsLeftDown() || IsUpDown())
            {
                _selectedCommandIndex = (_selectedCommandIndex + 3) % 4;
                moved = true;
            }
            else if (IsRightDown() || IsDownDown())
            {
                _selectedCommandIndex = (_selectedCommandIndex + 1) % 4;
                moved = true;
            }

            if (moved && tutorialPhase == BattleTutorialPhase.WaitingForScroll)
            {
                tutorialPhase = BattleTutorialPhase.PromptSelect;
                _menuState = BattleMenuState.TutorialNarrative;
                _inputDebounceTimer = 0.25f;
                SetGolferSpeech(
                    "Good grip! [ENTER] or [Z] confirms your choice. [X] or [ESC] cancels and steps back.",
                    "* [Z / ENTER] Next"
                );
                return;
            }

            // Lock out spam during tutorial to prevent accidental early PURGE
            if (tutorialPhase == BattleTutorialPhase.WaitingForScroll)
            {
                if (IsConfirmDown())
                {
                    _activeDialogueText = "* Use [W / S] or [A / D] to scroll through your commands.";
                }
                return;
            }

            if (tutorialPhase == BattleTutorialPhase.WaitingForAct)
            {
                if (IsConfirmDown())
                {
                    if (_selectedCommandIndex == 1) // ACT
                    {
                        _inputDebounceTimer = 0.20f;
                        _selectedActIndex = 0;
                        _menuState = BattleMenuState.ActSelect;
                    }
                    else
                    {
                        SetGolferSpeech(
                            "Hold on, kid! Try selecting [ACT] first to see how peaceful purification works.",
                            "* Select [ACT] to interact with Golfer."
                        );
                    }
                }
                return;
            }

            if (_inputDebounceTimer <= 0f && IsConfirmDown())
            {
                _inputDebounceTimer = 0.20f;
                switch (_selectedCommandIndex)
                {
                    case 0: // PURGE
                        _menuState = BattleMenuState.FightTarget;
                        break;
                    case 1: // ACT
                        _selectedActIndex = 0;
                        _menuState = BattleMenuState.ActSelect;
                        break;
                    case 2: // ITEM
                        _menuState = BattleMenuState.ItemSelect;
                        break;
                    case 3: // PURIFY
                        _selectedMercyIndex = 0;
                        _menuState = BattleMenuState.MercySelect;
                        break;
                }
            }
        }

        private void HandleFightTarget()
        {
            if (IsCancelDown())
            {
                _inputDebounceTimer = 0.15f;
                _menuState = BattleMenuState.CommandSelect;
                return;
            }

            if (_inputDebounceTimer <= 0f && IsConfirmDown())
            {
                _inputDebounceTimer = 0.25f;
                bool isPurgeExecution = IsEnemyAtPurgeMarker;
                StartCoroutine(FightAttackRoutine(isPurgeExecution));
            }
        }

        private IEnumerator FightAttackRoutine(bool isPurgeExecution)
        {
            _menuState = BattleMenuState.FightAttackAnim;
            purgeAttackCount++;

            // 1. Owen scythe swing animation
            if (owenAnimator != null)
            {
                owenAnimator.PlayAttack();
            }

            yield return new WaitForSeconds(0.35f);

            if (isPurgeExecution)
            {
                // Lethal PURGE execution: deletes the 0 HP helpless enemy forever
                enemyCurrentHp = 0;
                _lastDamageDealt = 99;

                if (enemyAnimator != null)
                {
                    enemyAnimator.PlayHurt(0.40f);
                }

                // Smoothly slow down battle music to a dead stop
                StartCoroutine(PurgeMusicSlowdownAndStopRoutine(1.8f));

                yield return new WaitForSeconds(0.45f);

                _menuState = BattleMenuState.FightAttackAnim;

                string[] purgeLines = new string[]
                {
                    "...Do you really hate us that much?",
                    "I.. have to warn you, Owen.",
                    "There is another.",
                    "He's out.. where this place leads.",
                    "He will not be as forgiving if you continue this path.",
                    "I hope you.. get revenge.",
                    "I hope it satisfies you."
                };

                for (int i = 0; i < purgeLines.Length; i++)
                {
                    SetGolferSpeech(
                        purgeLines[i],
                        "* Owen executes lethal PURGE!\n* [Z / ENTER] Next"
                    );
                    _inputDebounceTimer = 0.35f;

                    while (true)
                    {
                        if (_inputDebounceTimer <= 0f && IsConfirmDown())
                        {
                            if (!SkipSpeechTyping())
                            {
                                break;
                            }
                        }
                        yield return null;
                    }
                }

                // 8th Line: "I forgive you, O-" -> Cut off mid-sentence after 0.75 seconds!
                SetGolferSpeech("I forgive you, O-", "* Owen executes lethal PURGE!");
                yield return new WaitForSeconds(0.75f);

                HideGolferSpeech();
                _activeDialogueText = "* GOLFER implodes into cyber sparks...";

                bool implosionDone = false;
                if (enemyAnimator != null)
                {
                    enemyAnimator.PlayPurgeImplosion(1.6f, () => implosionDone = true);
                }
                else
                {
                    implosionDone = true;
                }

                while (!implosionDone) yield return null;

                _isPurifiedVictory = false;
                _menuState = BattleMenuState.Victory;
                _inputDebounceTimer = 0.35f;
                _activeDialogueText = "* GOLFER was PURGED.\n* Digital code collapsed into sparks and faded into nothingness.\n* Got 25 EXP and 10 D$. The network feels colder.";
            }
            else
            {
                // Basic Attack: deal damage towards 0 HP
                _lastDamageDealt = UnityEngine.Random.Range(32, 38);
                enemyCurrentHp = Mathf.Max(0, enemyCurrentHp - _lastDamageDealt);

                if (enemyAnimator != null)
                {
                    enemyAnimator.PlayHurt(0.35f);
                }

                // Update audio slowdown & distortion cleanly as damage is sustained
                float hpRatio = Mathf.Clamp01((float)enemyCurrentHp / enemyMaxHp);
                _targetBgmPitch = Mathf.Lerp(0.68f, 1.0f, hpRatio);
                _targetBgmDistortion = Mathf.Lerp(0.38f, 0.0f, hpRatio);
                _targetBgmCutoff = Mathf.Lerp(4200f, 22000f, hpRatio);
                _flutterIntensity = Mathf.Lerp(0.022f, 0.0f, hpRatio);

                yield return new WaitForSeconds(0.45f);

                if (IsEnemyAtPurgeMarker)
                {
                    // Code reached 0 HP: enemy is exhausted and can no longer attack!
                    if (enemyAnimator != null) enemyAnimator.IsBobbing = false;
                    SetGolferSpeech(
                        "Huff... puff... I... I can't keep my grip... my code is unravelling...",
                        $"* Owen strikes! Dealt {_lastDamageDealt} damage!\n* Golfer has 0 HP and can no longer attack!\n* [PURGE READY!] Select [PURGE] to delete, or [PURIFY] to spare.\n* [Z / ENTER] Next"
                    );
                }
                else
                {
                    SetGolferSpeech(
                        "Oof... steady swing, kid. I know you're hurting inside... I still believe in you.",
                        $"* Owen strikes! Dealt {_lastDamageDealt} damage!\n* [Z / ENTER] Next"
                    );
                }
                _menuState = BattleMenuState.ActResponse;
                _inputDebounceTimer = 0.25f;
            }
        }

        private IEnumerator PurgeMusicSlowdownAndStopRoutine(float duration)
        {
            _isMusicStopping = true;
            float startPitch = bgmAudioSource != null ? bgmAudioSource.pitch : 0.70f;
            float startDist = (bgmDistortion != null) ? bgmDistortion.distortionLevel : 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float prog = Mathf.Clamp01(elapsed / duration);

                if (bgmAudioSource != null)
                {
                    bgmAudioSource.pitch = Mathf.Max(0f, Mathf.Lerp(startPitch, 0.0f, prog));
                }
                if (bgmDistortion != null)
                {
                    bgmDistortion.distortionLevel = Mathf.Lerp(startDist, 0.55f, prog);
                }
                if (bgmLowPass != null)
                {
                    bgmLowPass.cutoffFrequency = Mathf.Lerp(4000f, 750f, prog);
                }

                yield return null;
            }

            if (bgmAudioSource != null)
            {
                bgmAudioSource.Stop();
                bgmAudioSource.pitch = 1.0f;
            }
            if (bgmDistortion != null) bgmDistortion.distortionLevel = 0f;
            if (bgmLowPass != null) bgmLowPass.cutoffFrequency = 22000f;
            _isMusicStopping = false;
        }

        private void HandleActSelect()
        {
            if (IsCancelDown())
            {
                _inputDebounceTimer = 0.15f;
                _menuState = BattleMenuState.CommandSelect;
                return;
            }

            int count = (activeEnemyData != null && activeEnemyData.talkOptions != null) ? activeEnemyData.talkOptions.Count : 1;
            if (IsUpDown())
            {
                _selectedActIndex = (_selectedActIndex + count - 1) % count;
            }
            else if (IsDownDown())
            {
                _selectedActIndex = (_selectedActIndex + 1) % count;
            }

            if (_inputDebounceTimer <= 0f && IsConfirmDown())
            {
                _inputDebounceTimer = 0.25f;
                var opt = (activeEnemyData != null && activeEnemyData.talkOptions != null && _selectedActIndex < activeEnemyData.talkOptions.Count)
                    ? activeEnemyData.talkOptions[_selectedActIndex]
                    : null;

                int addPurify = opt != null ? opt.purifyReward : 50;
                if (addPurify <= 0 && opt != null && opt.optionName != "Check") addPurify = 50;

                enemyCurrentMercy = Mathf.Clamp(enemyCurrentMercy + addPurify, 0, 100);

                // ACTing positively after taking damage raises the music back up to regular and makes Golfer forgive you!
                bool wasDamaged = enemyCurrentHp < enemyMaxHp || _targetBgmPitch < 1.0f;
                if (addPurify > 0 && wasDamaged)
                {
                    _targetBgmPitch = 1.0f;
                    _targetBgmDistortion = 0.0f;
                    _targetBgmCutoff = 22000.0f;
                    _flutterIntensity = 0.0f;
                }

                string cleanOptResponse = opt != null ? opt.responseText : "";
                cleanOptResponse = System.Text.RegularExpressions.Regex.Replace(cleanOptResponse, @"\s*\(\+?\d+%\s*(Mercy|Purify)\)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
                cleanOptResponse = cleanOptResponse.Replace("Mercy", "Purify").Replace("mercy", "purify");

                if (tutorialPhase == BattleTutorialPhase.WaitingForAct || tutorialPhase == BattleTutorialPhase.PromptDoAct)
                {
                    if (enemyCurrentMercy >= 100)
                    {
                        tutorialPhase = BattleTutorialPhase.MercyFull;
                        _menuState = BattleMenuState.TutorialNarrative;
                        SetGolferSpeech(
                            "There it is! 100% Purify! Now choose [PURIFY] to cleanse, or [PURGE] to attack.",
                            $"* {cleanOptResponse}\n* [Z / ENTER] Finish Tutorial"
                        );
                    }
                    else
                    {
                        _menuState = BattleMenuState.ActResponse;
                        SetGolferSpeech(
                            "Hah! Keep at it, kid!",
                            $"* {cleanOptResponse} (Purify +{addPurify}%)\n* [Z / ENTER] Next"
                        );
                    }
                }
                else
                {
                    string tag = addPurify > 0 ? $" (Purify +{addPurify}%)" : "";
                    if (addPurify > 0 && wasDamaged)
                    {
                        SetGolferSpeech(
                            "You... you're giving me advice even after that swing? Thanks, kid. I forgive you.",
                            $"* {cleanOptResponse}{tag}\n* Golfer forgives you! Music restored to regular tempo.\n* [Z / ENTER] Next"
                        );
                    }
                    else
                    {
                        _activeDialogueText = $"* {cleanOptResponse}{tag}\n* [Z / ENTER] Next";
                    }
                    _menuState = BattleMenuState.ActResponse;
                }
            }
        }

        private void HandleMercySelect()
        {
            if (IsCancelDown())
            {
                _inputDebounceTimer = 0.15f;
                _menuState = BattleMenuState.CommandSelect;
                return;
            }

            if (IsUpDown() || IsDownDown() || IsLeftDown() || IsRightDown())
            {
                _selectedMercyIndex = 1 - _selectedMercyIndex;
            }

            if (_inputDebounceTimer <= 0f && IsConfirmDown())
            {
                _inputDebounceTimer = 0.30f;
                if (_selectedMercyIndex == 0) // PURIFY
                {
                    if (enemyCurrentMercy >= 100)
                    {
                        StartCoroutine(PurifySequenceRoutine());
                    }
                    else
                    {
                        _activeDialogueText = $"* You reached out with purifying energy, but {activeEnemyName}'s code is still too unstable (Purify at {enemyCurrentMercy}%).\n* Use ACT to stabilize routines first!";
                        _menuState = BattleMenuState.ActResponse;
                    }
                }
                else // FLEE
                {
                    _activeDialogueText = "* There is nowhere to flee in this digitized sector.";
                    _menuState = BattleMenuState.ActResponse;
                }
            }
        }

        private IEnumerator PurifySequenceRoutine()
        {
            _menuState = BattleMenuState.FightAttackAnim;

            // 1. Golfer's peaceful farewell bark
            string bark = activeEnemyName.ToLower().Contains("golfer")
                ? "The noise... the static in my head... it's finally quiet. Thank you, Owen."
                : "I can see clearly now... Thank you, Owen.";

            SetGolferSpeech(bark, $"* {activeEnemyName.ToUpper()}'s code is stabilizing... [Z / ENTER] Next");
            _inputDebounceTimer = 0.35f;

            while (true)
            {
                if (_inputDebounceTimer <= 0f && IsConfirmDown())
                {
                    if (!SkipSpeechTyping())
                    {
                        break;
                    }
                }
                yield return null;
            }

            HideGolferSpeech();
            _activeDialogueText = $"* {activeEnemyName.ToUpper()}'s corrupted routines dissolve into radiant white light...";

            // 2. Play white light explosion animation (flare -> expand/dissolve -> 48 ascending light particles)
            bool explosionDone = false;
            if (enemyAnimator != null)
            {
                enemyAnimator.PlayPurifyWhiteLightExplosion(1.8f, () => explosionDone = true);
            }
            else
            {
                explosionDone = true;
            }

            while (!explosionDone) yield return null;

            // 3. Enter Victory state with clean 4-line readout
            _isPurifiedVictory = true;
            _menuState = BattleMenuState.Victory;
            _inputDebounceTimer = 0.35f;
            _activeDialogueText = $"* You extended purifying digital resonance!\n* {activeEnemyName.ToUpper()}'s corrupted routines dissolved peacefully into white light!\n* YOU WON! {activeEnemyName.ToUpper()} was PURIFIED.\n* Got 10 EXP and 5 D$.";
        }

        private void StartEnemyAttackTelegraph()
        {
            _menuState = BattleMenuState.EnemySpeech;
            _enemySpeechWaitTimer = 2.0f;
            _inputDebounceTimer = 0.20f;

            if (enemyCurrentHp <= 0)
            {
                // Helpless state: Golfer has 0 HP and cannot attack
                SetGolferSpeech("I... I can't attack... my energy is drained...", $"* {activeEnemyName} is too weak to attack! [Z / ENTER] Continue");
            }
            else if (activeEnemyData != null && activeEnemyData.inBattleBarks != null && activeEnemyData.inBattleBarks.Length > 0)
            {
                int r = UnityEngine.Random.Range(0, activeEnemyData.inBattleBarks.Length);
                _speechBarkText = activeEnemyData.inBattleBarks[r];
                SetGolferSpeech(_speechBarkText, $"* {activeEnemyName} attacks! [Z / ENTER] Dodge!");
            }
            else
            {
                _activeDialogueText = $"* {activeEnemyName} attacks! [Z / ENTER] Dodge!";
            }
        }

        private void HandleEnemySpeechTelegraph()
        {
            _enemySpeechWaitTimer -= Time.deltaTime;

            if (_inputDebounceTimer <= 0f && (IsConfirmDown() || _enemySpeechWaitTimer <= 0f))
            {
                if (_speechIsTyping)
                {
                    SkipSpeechTyping();
                    _inputDebounceTimer = 0.15f;
                    return;
                }

                HideGolferSpeech();
                StartCoroutine(StartEnemyAttackTurn());
            }
        }

        private IEnumerator StartEnemyAttackTurn()
        {
            _menuState = BattleMenuState.DodgeBoxPhase;
            bool dodgeDone = false;

            if (dodgeBox != null)
            {
                dodgeBox.transform.position = dodgeBoxPos;
                dodgeBox.SetCenterAndSize(dodgeBoxPos, new Vector2(3.6f, 2.7f));
            }

            if (enemyCurrentHp <= 0)
            {
                // Enemy has 0 HP and can no longer attack: heart goes to the empty box for 1.5s then exits
                _activeDialogueText = $"* But {activeEnemyName} is too weak to attack...";
                if (dodgeBox != null)
                {
                    dodgeBox.StartHelplessDodgePhase(1.5f, onPhaseEnd: () => dodgeDone = true);
                }
                else
                {
                    yield return new WaitForSeconds(1.5f);
                    dodgeDone = true;
                }
            }
            else
            {
                _activeDialogueText = $"* INCOMING ATTACK! (WASD / Arrows to Move)";
                if (dodgeBox != null)
                {
                    BulletPatternType p = activeEnemyData != null ? activeEnemyData.primaryPattern : BulletPatternType.RainFromTop;
                    float spd = activeEnemyData != null ? activeEnemyData.bulletSpeed : 3.0f;

                    dodgeBox.StartDodgePhase(
                        duration: 4.8f,
                        pattern: p,
                        speed: spd,
                        onPlayerHit: (dmg) =>
                        {
                            owenCurrentHp = Mathf.Max(0, owenCurrentHp - dmg);
                            owenAnimator?.PlayHurt(0.3f);
                        },
                        onPhaseEnd: () => dodgeDone = true
                    );
                }
                else
                {
                    yield return new WaitForSeconds(3.0f);
                    dodgeDone = true;
                }
            }

            while (!dodgeDone) yield return null;

            if (owenCurrentHp <= 0)
            {
                ExecuteGameOver();
            }
            else
            {
                StartNewPlayerTurn(enemyCurrentHp <= 0 
                    ? $"* {activeEnemyName} is helpless on the fairway. Choose fate: [PURGE] or [PURIFY]." 
                    : $"* {activeEnemyName} is waiting for your move.");
            }
        }

        private void ExecuteGameOver()
        {
            _menuState = BattleMenuState.GameOver;
            owenAnimator?.PlayDowned();
            StartCoroutine(GameOverRoutine());
        }

        private IEnumerator GameOverRoutine()
        {
            yield return new WaitForSeconds(1.5f);
            ScreenFader.Instance.Transition(
                onBlack: () =>
                {
                    owenCurrentHp = owenMaxHp;
                    gameObject.SetActive(false);

                    var player = _overworldPlayer != null ? _overworldPlayer : FindAnyObjectByType<OverworldPlayerController>(FindObjectsInactive.Include);
                    if (player != null)
                    {
                        player.gameObject.SetActive(true);
                        player.InputLocked = false;

                        Camera mainCam = Camera.main;
                        if (mainCam != null)
                        {
                            var roomCam = mainCam.GetComponent<RoomCameraController>();
                            if (roomCam != null)
                            {
                                roomCam.enabled = true;
                                roomCam.Target = player.transform;
                                roomCam.SnapToTarget();
                            }
                        }
                    }

                    if (_overworldEnemy != null)
                    {
                        _overworldEnemy.gameObject.SetActive(true);
                    }
                },
                duration: 0.45f,
                onComplete: () =>
                {
                    ScreenFader.Instance.SetAlpha(0f);
                }
            );
        }

        public void ExecuteReturnToOverworld(bool isPurified)
        {
            _menuState = BattleMenuState.ExitingToOverworld;

            if (bgmAudioSource != null && bgmAudioSource.isPlaying)
            {
                bgmAudioSource.Stop();
                bgmAudioSource.pitch = 1.0f;
            }
            if (bgmDistortion != null) bgmDistortion.distortionLevel = 0f;
            if (bgmLowPass != null) bgmLowPass.cutoffFrequency = 22000f;

            if (isPurified)
            {
                DigiPurity.World.OverworldBgmManager.Instance.PlayAbandonware(targetVolume: 0.28f, fadeInDuration: 3.0f);
            }
            else
            {
                DigiPurity.World.OverworldBgmManager.Instance.StopMusic();
            }

            ScreenFader.Instance.Transition(
                onBlack: () =>
                {
                    gameObject.SetActive(false);

                    if (_overworldEnemy != null)
                    {
                        if (isPurified)
                        {
                            _overworldEnemy.gameObject.SetActive(true);
                            _overworldEnemy.enabled = false;
                            var npc = _overworldEnemy.GetComponent<DigiPurity.World.PurifiedGolferNPC>();
                            if (npc == null) npc = _overworldEnemy.gameObject.AddComponent<DigiPurity.World.PurifiedGolferNPC>();
                            npc.enabled = true;
                        }
                        else
                        {
                            _overworldEnemy.gameObject.SetActive(false);
                        }
                    }

                    var player = _overworldPlayer != null ? _overworldPlayer : FindAnyObjectByType<OverworldPlayerController>(FindObjectsInactive.Include);
                    if (player != null)
                    {
                        player.gameObject.SetActive(true);
                        player.InputLocked = false;

                        Camera mainCam = Camera.main;
                        if (mainCam != null)
                        {
                            var roomCam = mainCam.GetComponent<RoomCameraController>();
                            if (roomCam != null)
                            {
                                roomCam.enabled = true;
                                roomCam.Target = player.transform;
                                roomCam.SnapToTarget();
                            }
                        }
                    }
                },
                duration: 0.35f,
                onComplete: () =>
                {
                    ScreenFader.Instance.SetAlpha(0f);
                }
            );
        }

        #region Deltarune UI OnGUI
        private void OnGUI()
        {
            if (_menuState == BattleMenuState.ExitingToOverworld || _menuState == BattleMenuState.GameOver) return;

            Font font = DigiPurity.Core.GameFonts.MainFont;
            if (font != null) GUI.skin.font = font;

            // Overhead speech bubble with typewriter text and comic tail over Golfer's head
            if (_isSpeechBubbleVisible && !string.IsNullOrEmpty(_speechVisibleText))
            {
                DrawEnemySpeechBubble();
            }

            // Authentic Deltarune Chapter 2 Battle HUD
            DrawDeltaruneHUD();
        }

        private void DrawEnemySpeechBubble()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            float headWorldX = 0f;
            float headWorldY = 0f;
            if (enemyAnimator != null)
            {
                headWorldX = enemyAnimator.transform.position.x;
                var sr = enemyAnimator.GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    headWorldY = sr.bounds.max.y;
                }
                else
                {
                    headWorldY = enemyAnimator.transform.position.y + 5.5f;
                }
            }

            Vector3 screenPos = cam.WorldToScreenPoint(new Vector3(headWorldX, headWorldY, 0f));
            float screenHeadX = screenPos.x;
            float screenHeadY = Screen.height - screenPos.y;

            Font font = DigiPurity.Core.GameFonts.MainFont;
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 15,
                fontStyle = FontStyle.Normal,
                wordWrap = true
            };
            if (font != null) style.font = font;

            float bubbleW = 340f;
            float calcH = style.CalcHeight(new GUIContent(_speechFullText), bubbleW - 32f);
            float bubbleH = Mathf.Clamp(calcH + 28f, 96f, 160f);

            float bubbleX = screenHeadX - (bubbleW * 0.5f);
            bubbleX = Mathf.Clamp(bubbleX, 24f, Screen.width - bubbleW - 24f);

            float bubbleY = screenHeadY - bubbleH - 18f;
            bubbleY = Mathf.Clamp(bubbleY, 20f, Screen.height - 230f);

            Rect bubble = new Rect(bubbleX, bubbleY, bubbleW, bubbleH);

            // Comic pointer tail pointing down toward Golfer's head
            float tailStartX = Mathf.Clamp(screenHeadX, bubbleX + 35f, bubbleX + bubbleW - 35f);
            DrawSpeechBubbleTail(tailStartX, bubbleY + bubbleH, screenHeadX, screenHeadY);

            // Bordered box: pure white background with solid black outline
            DrawBorderedBox(bubble, Color.white, Color.black, 3f);

            // Text inside bubble: black, word wrap, crisp font
            GUI.color = Color.black;
            Rect textRect = new Rect(bubbleX + 16f, bubbleY + 12f, bubbleW - 32f, bubbleH - 24f);
            GUI.Label(textRect, _speechVisibleText, style);
            GUI.color = Color.white;
        }

        private void DrawSpeechBubbleTail(float startX, float startY, float targetX, float targetY)
        {
            float dirX = Mathf.Sign(targetX - startX);
            if (Mathf.Abs(targetX - startX) < 6f) dirX = 0f;

            Rect[] steps = new Rect[]
            {
                new Rect(startX - 9f, startY - 1f, 18f, 5f),
                new Rect(startX - 7f + (dirX * 3f), startY + 4f, 14f, 5f),
                new Rect(startX - 5f + (dirX * 6f), startY + 9f, 10f, 5f),
                new Rect(startX - 3f + (dirX * 9f), startY + 14f, 6f, 5f)
            };

            GUI.color = Color.black;
            for (int i = 0; i < steps.Length; i++)
            {
                Rect border = new Rect(steps[i].x - 1.5f, steps[i].y - 1.5f, steps[i].width + 3f, steps[i].height + 3f);
                GUI.DrawTexture(border, Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
            for (int i = 0; i < steps.Length; i++)
            {
                GUI.DrawTexture(steps[i], Texture2D.whiteTexture);
            }

            GUI.DrawTexture(new Rect(startX - 8f, startY - 3f, 16f, 5f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawDeltaruneHUD()
        {
            Font hudFont = DigiPurity.Core.GameFonts.MainFont;
            float margin = 24f;
            float totalW = Screen.width - (margin * 2f);

            // Row 1: Character Info + Action Buttons
            // Upgraded row 2 height to 122px so text is prominent and never clips!
            float row1H = 58f;
            float row2H = 122f;
            float row2Y = Screen.height - row2H - 12f;
            float row1Y = row2Y - row1H - 8f;

            // Character Box Width: 340px
            float charBoxW = Mathf.Clamp(340f, 260f, totalW * 0.40f);

            // 1. TOP-LEFT: OWEN CHARACTER CARD
            Rect charBox = new Rect(margin, row1Y, charBoxW, row1H);
            Color charBoxBorder = new Color(0.78f, 0.78f, 0.82f, 1f); // Light grey outline
            Color charBoxFill = new Color(0.13f, 0.13f, 0.15f, 0.98f);  // Clean grey fill
            DrawBorderedBox(charBox, charBoxFill, charBoxBorder, 3f);

            // Mini Face Portrait (36x36) - Owen's front-facing idle head/face
            Rect faceRect = new Rect(charBox.x + 8, charBox.y + 11, 36, 36);
            Texture portTex = _owenPortraitTex != null ? _owenPortraitTex : (owenPortrait != null ? owenPortrait.texture : null);
            if (portTex == null)
            {
                _owenPortraitTex = LoadTextureDirect("Assets/Sprites/Combat/Owen_Battle_Portrait.png");
                portTex = _owenPortraitTex;
            }
            if (portTex != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(faceRect, portTex, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.color = new Color(0.70f, 0.52f, 0.40f);
                GUI.DrawTexture(faceRect, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            // Name: "OWEN"
            GUI.color = Color.white;
            var nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            if (hudFont != null) nameStyle.font = hudFont;
            GUI.Label(new Rect(charBox.x + 48, charBox.y + 8, 65, 22), "OWEN", nameStyle);

            // HP Label & Bar
            var hpLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold }; if (hudFont != null) hpLabelStyle.font = hudFont;
            GUI.Label(new Rect(charBox.x + 118, charBox.y + 10, 25, 18), "HP", hpLabelStyle);

            float hpBarW = Mathf.Max(60f, charBoxW - 225f);
            Rect hpBg = new Rect(charBox.x + 144, charBox.y + 12, hpBarW, 14);
            GUI.color = new Color(0.4f, 0.08f, 0.08f);
            GUI.DrawTexture(hpBg, Texture2D.whiteTexture);

            float hpRatio = (float)owenCurrentHp / owenMaxHp;
            Rect hpFill = new Rect(charBox.x + 144, charBox.y + 12, hpBarW * hpRatio, 14);
            GUI.color = hpRatio > 0.25f ? new Color(0.1f, 0.95f, 0.4f) : new Color(0.95f, 0.2f, 0.2f);
            GUI.DrawTexture(hpFill, Texture2D.whiteTexture);

            // HP Digits: 100 / 100
            GUI.color = Color.white;
            var digitStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 }; if (hudFont != null) digitStyle.font = hudFont;
            GUI.Label(new Rect(charBox.x + 48, charBox.y + 30, 100, 20), $"{owenCurrentHp} / {owenMaxHp}", digitStyle);

            // 2. TOP-RIGHT: 4 ACTION BUTTONS [PURGE] [ACT] [ITEM] [PURIFY]
            float btnsStartX = margin + charBoxW + 14f;
            float btnsTotalW = totalW - charBoxW - 14f;
            float btnW = Mathf.Max(70f, (btnsTotalW - 24f) / 4f);
            float btnH = row1H;

            for (int i = 0; i < 4; i++)
            {
                Rect bRect = new Rect(btnsStartX + (i * (btnW + 8f)), row1Y, btnW, btnH);
                bool isSel = (_menuState == BattleMenuState.CommandSelect) && (i == _selectedCommandIndex);

                Color borderCol = isSel 
                    ? (i == 0 ? (IsEnemyAtPurgeMarker ? new Color(1f, 0.2f, 0.1f) : new Color(1f, 0.45f, 0.2f)) : (i == 3 && enemyCurrentMercy >= 100 ? new Color(1f, 0.95f, 0.15f) : new Color(1f, 0.65f, 0.05f))) 
                    : (i == 3 && enemyCurrentMercy >= 100 ? new Color(0.85f, 0.8f, 0.15f) : (i == 0 && IsEnemyAtPurgeMarker ? new Color(0.85f, 0.25f, 0.15f) : new Color(0.12f, 0.40f, 0.55f)));
                Color fillCol = isSel 
                    ? (i == 0 ? new Color(0.25f, 0.06f, 0.04f, 0.98f) : new Color(0.20f, 0.12f, 0.03f, 0.98f)) 
                    : new Color(0.02f, 0.05f, 0.12f, 0.98f);
                DrawBorderedBox(bRect, fillCol, borderCol, isSel ? 3.0f : 1.5f);

                // Button Icon
                Texture iconTex = null;
                if (i == 0) iconTex = _iconFightTex != null ? _iconFightTex : (iconFight != null ? iconFight.texture : null);
                else if (i == 1) iconTex = _iconActTex != null ? _iconActTex : (iconAct != null ? iconAct.texture : null);
                else if (i == 2) iconTex = _iconItemTex != null ? _iconItemTex : (iconItem != null ? iconItem.texture : null);
                else if (i == 3) iconTex = _iconMercyTex != null ? _iconMercyTex : (iconMercy != null ? iconMercy.texture : null);

                if (iconTex != null)
                {
                    GUI.color = isSel ? new Color(1f, 0.88f, 0.2f) : Color.white;
                    GUI.DrawTexture(new Rect(bRect.x + (btnW * 0.5f) - 9, bRect.y + 6, 18, 18), iconTex);
                }

                GUI.color = isSel ? new Color(1f, 0.88f, 0.2f) : new Color(0.85f, 0.85f, 0.85f);
                var btnStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    fontStyle = isSel ? FontStyle.Bold : FontStyle.Normal
                };
                if (hudFont != null) btnStyle.font = hudFont;
                GUI.Label(new Rect(bRect.x, bRect.y + 28, btnW, 20), _commandNames[i], btnStyle);
            }

            // 3. BOTTOM: FULL-WIDTH DELTARUNE DIALOGUE & SUBMENU BOX
            // Increased text size to 18 for high visibility and visual weight!
            Rect dialogBox = new Rect(margin, row2Y, totalW, row2H);
            DrawBorderedBox(dialogBox, Color.black, Color.white, 3.5f);

            GUI.color = Color.white;
            var textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Normal,
                wordWrap = true
            };
            if (hudFont != null) textStyle.font = hudFont;

            var promptStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            if (hudFont != null) promptStyle.font = hudFont;

            float textPadX = dialogBox.x + 28f;
            float textPadY = dialogBox.y + 14f;
            float textW = dialogBox.width - 56f;

            float pulse = Mathf.PingPong(Time.time * 2.2f, 0.35f) + 0.65f;

            switch (_menuState)
            {
                case BattleMenuState.TutorialNarrative:
                    GUI.Label(new Rect(textPadX, textPadY, textW, 85), _activeDialogueText, textStyle);
                    break;

                case BattleMenuState.CommandSelect:
                    GUI.Label(new Rect(textPadX, textPadY, textW, 85), _activeDialogueText, textStyle);
                    break;

                case BattleMenuState.FightTarget:
                    bool isSparable = enemyCurrentMercy >= 100;
                    bool canPurge = IsEnemyAtPurgeMarker;

                    GUI.color = isSparable ? new Color(1f, 0.92f, 0.15f) : (canPurge ? new Color(1f, 0.35f, 0.15f) : Color.white);
                    GUI.Label(new Rect(textPadX, textPadY, 140, 26), $"> {activeEnemyName.ToUpper()}", textStyle);

                    // HP Bar with 0 HP Purge Marker
                    GUI.color = new Color(0.85f, 0.85f, 0.85f);
                    GUI.Label(new Rect(textPadX + 150, textPadY, 30, 26), "HP", textStyle);
                    float enemyHpRatio = Mathf.Clamp01((float)enemyCurrentHp / enemyMaxHp);
                    Rect eHpBg = new Rect(textPadX + 185, textPadY + 6, 85, 14);
                    DrawBorderedBox(eHpBg, new Color(0.3f, 0.08f, 0.08f), Color.clear, 0f);
                    DrawBorderedBox(new Rect(eHpBg.x, eHpBg.y, eHpBg.width * enemyHpRatio, 14), canPurge ? new Color(1f, 0.25f, 0.1f) : new Color(0.1f, 0.95f, 0.4f), Color.clear, 0f);

                    // Purge Marker tick at 0 HP (left edge)
                    Color markerCol = canPurge ? new Color(1f, 0.2f, 0.1f, pulse) : new Color(1f, 0.65f, 0.15f, 0.85f);
                    GUI.color = markerCol;
                    GUI.DrawTexture(new Rect(eHpBg.x - 1f, eHpBg.y - 3f, 3f, 20f), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(eHpBg.x - 3f, eHpBg.y - 6f, 7f, 4f), Texture2D.whiteTexture);

                    // Deltarune PURIFY Bar
                    float purifyRatio = Mathf.Clamp01((float)enemyCurrentMercy / 100f);
                    Color purifyBarCol = isSparable ? new Color(1f, 0.95f, 0.15f) : new Color(1f, 0.65f, 0.1f);
                    GUI.color = isSparable ? new Color(1f, 0.92f, 0.15f) : new Color(0.9f, 0.65f, 0.1f);
                    GUI.Label(new Rect(textPadX + 285, textPadY, 75, 26), "PURIFY", textStyle);
                    DrawBorderedBox(new Rect(textPadX + 365, textPadY + 6, 85, 14), new Color(0.2f, 0.15f, 0.05f), Color.clear, 0f);
                    DrawBorderedBox(new Rect(textPadX + 365, textPadY + 6, 85 * purifyRatio, 14), purifyBarCol, Color.clear, 0f);

                    GUI.color = isSparable ? new Color(1f, 0.92f, 0.15f) : new Color(0.85f, 0.85f, 0.85f);
                    GUI.Label(new Rect(textPadX + 460, textPadY, 180, 26), $"{enemyCurrentMercy}%{(isSparable ? " (READY!)" : "")}", textStyle);

                    // Row 2: Basic Attack vs PURGE option
                    if (canPurge)
                    {
                        GUI.color = new Color(1f, 0.35f, 0.15f);
                        GUI.Label(new Rect(textPadX, textPadY + 38, textW, 26), "> PURGE  [LETHAL DELETION READY!]", textStyle);
                        GUI.color = new Color(1f, 0.95f, 0.2f, pulse);
                        GUI.Label(new Rect(textPadX, textPadY + 68, textW, 26), "[Z / ENTER] Execute PURGE!      [X / ESC] Back", promptStyle);
                    }
                    else
                    {
                        GUI.color = Color.white;
                        GUI.Label(new Rect(textPadX, textPadY + 38, textW, 26), "> Basic Attack  (Chip HP toward 0 HP)", textStyle);
                        GUI.color = new Color(0.85f, 0.85f, 0.85f);
                        GUI.Label(new Rect(textPadX, textPadY + 68, textW, 26), "[Z / ENTER] Strike      [X / ESC] Back", promptStyle);
                    }
                    break;

                case BattleMenuState.FightAttackAnim:
                    if (!string.IsNullOrEmpty(_activeDialogueText))
                    {
                        GUI.color = Color.white;
                        GUI.Label(new Rect(textPadX, textPadY, textW, 64), _activeDialogueText, textStyle);
                        if (_isSpeechBubbleVisible)
                        {
                            GUI.color = new Color(1f, 0.95f, 0.2f, pulse);
                            GUI.Label(new Rect(textPadX, textPadY + 68, textW, 26), "[Z / ENTER] Next", promptStyle);
                        }
                    }
                    else
                    {
                        GUI.color = new Color(1f, 0.35f, 0.35f);
                        GUI.Label(new Rect(textPadX, textPadY, textW, 50), $"* Owen strikes! Dealt {_lastDamageDealt} damage!", textStyle);
                    }
                    break;

                case BattleMenuState.ActSelect:
                    if (activeEnemyData != null && activeEnemyData.talkOptions != null)
                    {
                        for (int i = 0; i < activeEnemyData.talkOptions.Count; i++)
                        {
                            bool isSel = i == _selectedActIndex;
                            GUI.color = isSel ? new Color(1f, 0.88f, 0.2f) : Color.white;
                            string optName = activeEnemyData.talkOptions[i].optionName;
                            float colX = textPadX + ((i % 2) * 310f);
                            float rowY = textPadY + ((i / 2) * 32f);
                            GUI.Label(new Rect(colX, rowY, 290f, 28), $"{(isSel ? "> " : "* ")}{optName}", textStyle);
                        }
                    }
                    break;

                case BattleMenuState.ActResponse:
                    GUI.Label(new Rect(textPadX, textPadY, textW, 64), _activeDialogueText, textStyle);
                    GUI.color = new Color(1f, 0.95f, 0.2f, pulse);
                    GUI.Label(new Rect(textPadX, textPadY + 68, textW, 26), "[Z / ENTER] Continue", promptStyle);
                    break;

                case BattleMenuState.ItemSelect:
                    GUI.Label(new Rect(textPadX, textPadY, textW, 50), "* No items in inventory.", textStyle);
                    GUI.color = new Color(0.85f, 0.85f, 0.85f);
                    GUI.Label(new Rect(textPadX, textPadY + 55, textW, 26), "[X / ESC] Back", promptStyle);
                    break;

                case BattleMenuState.MercySelect:
                    bool canPurify = enemyCurrentMercy >= 100;
                    GUI.color = (_selectedMercyIndex == 0) ? (canPurify ? new Color(1f, 0.95f, 0.15f) : new Color(1f, 0.88f, 0.2f)) : (canPurify ? new Color(1f, 0.95f, 0.4f) : Color.white);
                    string purifyLabel = canPurify ? "PURIFY [READY!]" : $"PURIFY [{enemyCurrentMercy}% - Need 100%]";
                    GUI.Label(new Rect(textPadX, textPadY, 340, 28), $"{(_selectedMercyIndex == 0 ? "> " : "* ")}{purifyLabel}", textStyle);

                    GUI.color = _selectedMercyIndex == 1 ? new Color(1f, 0.88f, 0.2f) : Color.white;
                    GUI.Label(new Rect(textPadX + 360, textPadY, textW, 28), $"{(_selectedMercyIndex == 1 ? "> " : "* ")}FLEE", textStyle);

                    GUI.color = new Color(0.85f, 0.85f, 0.85f);
                    GUI.Label(new Rect(textPadX, textPadY + 58, textW, 26), "[Z / ENTER] Confirm      [X / ESC] Back", promptStyle);
                    break;

                case BattleMenuState.EnemySpeech:
                    GUI.color = new Color(1f, 0.45f, 0.35f);
                    GUI.Label(new Rect(textPadX, textPadY, textW, 52), _activeDialogueText, textStyle);
                    GUI.color = new Color(1f, 0.95f, 0.2f, pulse);
                    GUI.Label(new Rect(textPadX, textPadY + 58, textW, 26), (enemyCurrentHp <= 0) ? "[Z / ENTER] Continue" : "[Z / ENTER] Dodge Attack!", promptStyle);
                    break;

                case BattleMenuState.DodgeBoxPhase:
                    if (enemyCurrentHp <= 0)
                    {
                        GUI.color = new Color(0.7f, 0.7f, 0.7f);
                        GUI.Label(new Rect(textPadX, textPadY, textW, 50), "* But Golfer is too weak to attack...", textStyle);
                    }
                    else
                    {
                        GUI.color = new Color(0.2f, 1f, 0.4f);
                        GUI.Label(new Rect(textPadX, textPadY, textW, 50), "* DODGE THE BULLETS! (WASD / Arrows to Move)", textStyle);
                    }
                    break;

                case BattleMenuState.Victory:
                    GUI.color = new Color(1f, 0.92f, 0.25f);
                    // Ample 82px label height guarantees all 4 lines are 100% visible with zero clipping!
                    GUI.Label(new Rect(textPadX, textPadY, textW, 82), _activeDialogueText, textStyle);
                    GUI.color = new Color(1f, 0.95f, 0.2f, pulse);
                    GUI.Label(new Rect(textPadX, textPadY + 86, textW, 26), "[Z / ENTER] Return to Overworld", promptStyle);
                    break;
            }

            GUI.color = Color.white;
        }

        private void DrawBorderedBox(Rect r, Color fillColor, Color borderColor, float borderWidth)
        {
            // Fill
            GUI.color = fillColor;
            GUI.DrawTexture(r, Texture2D.whiteTexture);

            // Border
            GUI.color = borderColor;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, borderWidth), Texture2D.whiteTexture); // Top
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - borderWidth, r.width, borderWidth), Texture2D.whiteTexture); // Bottom
            GUI.DrawTexture(new Rect(r.x, r.y, borderWidth, r.height), Texture2D.whiteTexture); // Left
            GUI.DrawTexture(new Rect(r.x + r.width - borderWidth, r.y, borderWidth, r.height), Texture2D.whiteTexture); // Right
            GUI.color = Color.white;
        }
        #endregion
    }
}
