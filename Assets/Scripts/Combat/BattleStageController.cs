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
            PromptScroll,       // Golfer: "See those 4 buttons on the left? Use [W / S] or [Up / Down] to scroll through your options. Give it a try!"
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
            CommandSelect,      // PURGE, ACT, BAG, PURIFY
            FightTarget,        // Selecting target enemy (1..N enemies: Timed Strike vs PURGE)
            AttackTiming,       // Undertale / Deltarune accuracy-based attack timing bar
            FightAttackAnim,    // Owen swinging, enemy damage shake
            ActTarget,          // Selecting which enemy (1..N) to ACT upon when 2+ enemies are active
            ActSelect,          // Deltarune TALK/ACT options for chosen enemy
            ActResponse,        // Showing ACT flavor text
            ItemSelect,         // Bag item list (or "No items.")
            MercySelect,        // PURIFY submenu (PURIFY or FLEE)
            EnemySpeech,        // Speech bubble bark before dodging
            DodgeBoxPhase,      // Undertale bullet hell dodging (or helpless calm box)
            Victory,            // All enemies defeated / purified
            GameOver,           // Owen HP = 0
            ExitingToOverworld
        }

        [Serializable]
        public class RuntimeEnemySlot
        {
            public int slotIndex;
            public string displayName;
            public BattleEncounterSlot config;
            public int maxHp;
            public int currentHp;
            public int purgeMarkerHp;
            public int currentPurify;
            public int purifyThreshold;
            public bool isResolved;
            public bool wasPurified;
            public EnemyBattleAnimator animator;
            public int sequentialAttackCursor;
            public int chosenAttackIndex = -1;
            public EnemyAttackSlot chosenAttack;
            public float pendingDamageMultiplier = 1f;
            public float pendingSpeedMultiplier = 1f;
            public readonly HashSet<string> usedActs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<int> firedDialogueRules = new HashSet<int>();

            public bool IsAtPurgeMarker => currentHp <= purgeMarkerHp;
            public bool IsPurifyReady => currentPurify >= purifyThreshold;
            public float HpPercent => maxHp > 0 ? ((float)currentHp / maxHp) * 100f : 0f;
            public float PurifyPercent => purifyThreshold > 0 ? ((float)currentPurify / purifyThreshold) * 100f : 0f;
        }

        [Header("Editable Stage & Box Placement (Edit Freely in Inspector!)")]
        public static readonly Vector3 VOID_STAGE_POS = new Vector3(1000f, 1000f, 0f);

        [SerializeField] private Vector3 stageCenter = new Vector3(1000f, 1000f, 0f);
        [Tooltip("Offset of the Undertale Bullet-Hell Dodge Box relative to Stage Center (1000, 1000).")]
        [SerializeField] private Vector2 dodgeBoxOffset = new Vector2(0.85f, 1.35f);
        [Tooltip("Width and height of the Dodge Box arena in world units.")]
        [SerializeField] private Vector2 dodgeBoxSize = new Vector2(3.6f, 2.7f);
        [Tooltip("Offset of Owen on the battle stage relative to Stage Center.")]
        [SerializeField] private Vector2 owenStageOffset = new Vector2(-3.30f, -1.35f);
        [Tooltip("Offset of the primary enemy on the battle stage relative to Stage Center.")]
        [SerializeField] private Vector2 enemyStageOffset = new Vector2(5.10f, -1.35f);

        [Header("Editable UI Box Offsets (640x480 Virtual Pixel Space)")]
        [Tooltip("Pixel offset applied to the enemy's speech dialogue bubble.")]
        [SerializeField] private Vector2 speechBubblePixelOffset = Vector2.zero;
        [Tooltip("Pixel offset applied to the bottom status/options/attack bar.")]
        [SerializeField] private Vector2 bottomBarPixelOffset = Vector2.zero;

        [HideInInspector] [SerializeField] private Vector3 owenBattlePos = new Vector3(996.7f, 998.65f, 0f);
        [HideInInspector] [SerializeField] private Vector3 enemyBattlePos = new Vector3(1005.1f, 998.65f, 0f);
        [HideInInspector] [SerializeField] private Vector3 dodgeBoxPos = new Vector3(1000.85f, 1001.35f, 0f);

        [Header("Optional Tutorial Driver (Decoupled from Universal Battle Engine)")]
        [Tooltip("Optional tutorial script for onboarding encounters (e.g. Golfer). Leave null for normal battles!")]
        [SerializeField] private BattleTutorialEncounter activeTutorialDriver;

        [Header("Optional Stage Multi-Enemy Encounter Config")]
        [Tooltip("Optional multi-enemy BattleEncounterConfig directly on the stage for quick Play Mode testing.")]
        [SerializeField] private BattleEncounterConfig stageEncounterConfig;

        public Vector2 EffectiveDodgeBoxOffset
        {
            get
            {
                if (_activeEncounterConfig != null && _activeEncounterConfig.useCustomDodgeBox)
                    return _activeEncounterConfig.customDodgeBoxOffset;
                if (activeEnemyData != null && activeEnemyData.useCustomDodgeBox)
                    return activeEnemyData.customDodgeBoxOffset;
                return dodgeBoxOffset;
            }
        }

        public Vector2 EffectiveDodgeBoxSize
        {
            get
            {
                if (_activeEncounterConfig != null && _activeEncounterConfig.useCustomDodgeBox)
                    return _activeEncounterConfig.customDodgeBoxSize;
                if (activeEnemyData != null && activeEnemyData.useCustomDodgeBox)
                    return activeEnemyData.customDodgeBoxSize;
                return dodgeBoxSize;
            }
        }

        /// <summary>
        /// Calculates the pixel rectangle of the centered 4:3 viewport on the player's monitor.
        /// </summary>
        public static Rect Get43ScreenPixelRect()
        {
            float screenW = Screen.width;
            float screenH = Screen.height;
            if (screenW <= 0f || screenH <= 0f) return new Rect(0f, 0f, 640f, 480f);

            const float targetAspect = 4.0f / 3.0f;
            float windowAspect = screenW / screenH;

            if (windowAspect > targetAspect)
            {
                // Pillarbox: screen is wider than 4:3 (e.g. 16:9)
                float w = screenH * targetAspect;
                float x = (screenW - w) * 0.5f;
                return new Rect(x, 0f, w, screenH);
            }
            else
            {
                // Letterbox: screen is taller than 4:3
                float h = screenW / targetAspect;
                float y = (screenH - h) * 0.5f;
                return new Rect(0f, y, screenW, h);
            }
        }

        private void ApplyDodgePhaseDimming(List<ActiveAttackWaveContext> waves, bool active)
        {
            if (waves == null || waves.Count == 0) return;
            var slot = waves[0].slot;
            if (slot == null) return;

            // 1. Owen battle sprite dimming
            if (owenAnimator != null)
            {
                if (active && slot.darkenOwenSprite)
                {
                    owenAnimator.SetDimmed(true, slot.spriteDimAmount);
                }
                else
                {
                    owenAnimator.SetDimmed(false);
                }
            }

            // 2. Enemy sprites dimming (default false, so enemies remain brightly lit)
            for (int i = 0; i < _enemySlots.Count; i++)
            {
                if (_enemySlots[i].animator != null)
                {
                    if (active && slot.darkenEnemySprites)
                    {
                        _enemySlots[i].animator.SetDimmed(true, slot.spriteDimAmount);
                    }
                    else
                    {
                        _enemySlots[i].animator.SetDimmed(false);
                    }
                }
            }
        }

        public void RecalculateStagePositions()
        {
            stageCenter = VOID_STAGE_POS;
            transform.position = VOID_STAGE_POS;

            Vector2 effBoxOffset = EffectiveDodgeBoxOffset;
            Vector2 effBoxSize = EffectiveDodgeBoxSize;
            if (effBoxSize.x < 0.5f) effBoxSize.x = 3.6f;
            if (effBoxSize.y < 0.5f) effBoxSize.y = 2.7f;

            owenBattlePos = new Vector3(stageCenter.x + owenStageOffset.x, stageCenter.y + owenStageOffset.y, 0f);
            enemyBattlePos = new Vector3(stageCenter.x + enemyStageOffset.x, stageCenter.y + enemyStageOffset.y, 0f);
            dodgeBoxPos = new Vector3(stageCenter.x + effBoxOffset.x, stageCenter.y + effBoxOffset.y, 0f);

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
                dodgeBox.SetCenterAndSize(dodgeBoxPos, effBoxSize);
            }
        }

        private void OnValidate()
        {
            RecalculateStagePositions();
        }

        private void OnDrawGizmos()
        {
            Vector3 center = VOID_STAGE_POS;
            Vector2 effBoxOffset = EffectiveDodgeBoxOffset;
            Vector2 effBoxSize = EffectiveDodgeBoxSize;
            Vector3 boxCenter = new Vector3(center.x + effBoxOffset.x, center.y + effBoxOffset.y, 0f);

            Gizmos.color = new Color(0.15f, 1.0f, 0.45f, 0.9f);
            Gizmos.DrawWireCube(boxCenter, new Vector3(effBoxSize.x, effBoxSize.y, 0.05f));

            Gizmos.color = new Color(0.25f, 0.85f, 1.0f, 0.8f);
            Gizmos.DrawWireSphere(new Vector3(center.x + owenStageOffset.x, center.y + owenStageOffset.y, 0f), 0.35f);

            Gizmos.color = new Color(1.0f, 0.45f, 0.25f, 0.85f);
            var previewConfig = stageEncounterConfig != null ? stageEncounterConfig : GetComponent<BattleEncounterConfig>();
            if (previewConfig != null && previewConfig.enemies != null && previewConfig.enemies.Count > 0)
            {
                int total = Mathf.Clamp(previewConfig.enemies.Count, 1, 4);
                for (int i = 0; i < total; i++)
                {
                    var s = previewConfig.enemies[i];
                    Vector2 manualOff = (s != null && s.visuals != null) ? s.visuals.stagePositionOffset : Vector2.zero;
                    float groundOff = (s != null && s.visuals != null) ? s.visuals.groundLineOffset : 0f;
                    Vector3 slotPos = BattleEncounterConfig.CalculateSlotStagePosition(
                        center, enemyStageOffset, i, total, previewConfig.formationPreset, manualOff, groundOff);
                    Gizmos.DrawWireSphere(slotPos, 0.35f);
                }
            }
            else
            {
                Gizmos.DrawWireSphere(new Vector3(center.x + enemyStageOffset.x, center.y + enemyStageOffset.y, 0f), 0.35f);
            }
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

        // Custom 640x480 Angled Cyber Battle UI Textures (from Battle_HUD_UI.ase)
        private static Texture2D _uiBottomFrameTex;
        private static Texture2D _uiOwenHeadTex;
        private static Texture2D _uiOwenNameTex;
        private static Texture2D _uiHpFrameTex;
        private static Texture2D _uiHpFillTex;
        private static readonly Texture2D[] _uiBtnTextures = new Texture2D[4];
        private static Texture2D _uiSoulHeartTex;

        // Underdamped spring bounce state for the 4 left-side command buttons
        private readonly float[] _btnSpringPos = new float[4];
        private readonly float[] _btnSpringVel = new float[4];
        private static readonly float[] BaseButtonY = new float[] { 103f, 184f, 268f, 351f };
        private const float BaseButtonW = 115f;
        private const float BaseButtonH = 99f;

        // Undertale / Deltarune Accuracy Attack Timing Bar state
        private float _attackBarPos = 0f;
        private float _attackBarSpeed = 1.12f;
        private bool _attackBarLocked = false;
        private bool _attackDidMiss = false;
        private float _lastAttackAccuracy = 1.0f;
        private float _lastAttackMultiplier = 1.0f;
        private string _lastAttackRating = "";
        private const float BaseOwenAttackDamage = 34f;

        [Header("Controllers")]
        [SerializeField] private OwenBattleAnimator owenAnimator;
        [SerializeField] private EnemyBattleAnimator enemyAnimator;
        [SerializeField] private BattleDodgeBox dodgeBox;
        [SerializeField] private AudioSource bgmAudioSource;

        [Header("Encounter Song & Conditional Song Rules")]
        [Tooltip("Optional stage-level song override. If left empty, uses activeEnemyData.battleBgm.")]
        [SerializeField] private AudioClip overrideEncounterSong;
        [Tooltip("Optional stage-level conditional song rules (checked alongside activeEnemyData.conditionalSongRules).")]
        [SerializeField] private List<BattleSongRule> stageSongRules = new List<BattleSongRule>();

        [Header("Audio Distortion & Dynamic Slowdown")]
        [SerializeField] private AudioDistortionFilter bgmDistortion;
        [SerializeField] private AudioLowPassFilter bgmLowPass;
        private float _targetBgmVolume = 0.85f;
        private float _currentBgmPitch = 1.0f;
        private float _targetBgmPitch = 1.0f;
        private float _targetBgmDistortion = 0.0f;
        private float _targetBgmCutoff = 22000.0f;
        private float _flutterIntensity = 0.0f;
        private bool _isMusicStopping = false;
        private bool _isCustomSongTransitionActive = false;
        private Coroutine _songTransitionCoroutine;
        private int _currentTurnNumber = 0;
        private readonly HashSet<int> _firedStageSongRules = new HashSet<int>();
        private readonly HashSet<int> _firedEnemySongRules = new HashSet<int>();

        [Header("Combatant Stats")]
        public int owenMaxHp = 100;
        public int owenCurrentHp = 100;
        public int enemyMaxHp = 100;
        public int enemyCurrentHp = 100;
        public int purgeMarkerHp = 0; // Enemy can be purged once reduced to purgeMarkerHp (default 0 HP)
        public string activeEnemyName = "Golfer";

        public bool IsEnemyAtPurgeMarker => enemyCurrentHp <= purgeMarkerHp;

        [Header("Active Enemy Configuration")]
        [SerializeField] private EnemyData activeEnemyData;

        private BattleEncounterConfig _activeEncounterConfig;
        private readonly List<RuntimeEnemySlot> _enemySlots = new List<RuntimeEnemySlot>();
        private readonly List<EnemyBattleAnimator> _extraSlotAnimators = new List<EnemyBattleAnimator>();
        private int _selectedTargetSlotCursor = 0; // Index within GetActiveUnresolvedSlots()
        private int _speakingSlotIndex = 0;
        private CombinedAttackRule _pendingCombinedAttack = null;
        private readonly HashSet<int> _firedEncounterSongRules = new HashSet<int>();
        private int _accumulatedExpReward = 0;
        private int _accumulatedDollarReward = 0;
        private bool _anyEnemyWasPurged = false;

        public IReadOnlyList<RuntimeEnemySlot> EnemySlots => _enemySlots;

        private OverworldPlayerController _overworldPlayer;
        private OverworldEnemy _overworldEnemy;
        private BattleMenuState _menuState = BattleMenuState.CommandSelect;
        private List<ActiveAttackWaveContext> _activeWaveList;
        private float _currentUiAlpha = 1f;

        private int _selectedCommandIndex = 0; // 0: PURGE, 1: ACT, 2: BAG, 3: PURIFY
        public int enemyCurrentMercy = 0; // Current Purify progress (0 - 100%)
        public bool isPurifiedVictory = false;
        public bool isBattleActive = false;
        public BattleTutorialPhase tutorialPhase = BattleTutorialPhase.Intro1;
        public int purgeAttackCount = 0;

        private readonly string[] _commandNames = new string[] { "PURGE", "ACT", "BAG", "PURIFY" };

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

        /// <summary>
        /// Steps a 2nd-order underdamped spring for smooth button expansion & settle bounce.
        /// </summary>
        public static void StepButtonBounceSpring(ref float pos, ref float vel, float target, float dt, float omega = 26.0f, float zeta = 0.50f)
        {
            float clampedDt = Mathf.Clamp(dt, 0f, 0.033f);
            float accel = (omega * omega) * (target - pos) - (2.0f * zeta * omega * vel);
            vel += accel * clampedDt;
            pos += vel * clampedDt;

            if (Mathf.Abs(target - pos) < 0.001f && Mathf.Abs(vel) < 0.005f)
            {
                pos = target;
                vel = 0f;
            }
        }

        /// <summary>
        /// Calculates Undertale / Deltarune accuracy-based attack multiplier from normalized cursor position [0..1],
        /// where 0.5 is dead-center bullseye.
        /// </summary>
        public static float CalculateAttackTimingMultiplier(float normalizedCursorPos, out string ratingLabel)
        {
            if (normalizedCursorPos < 0f || normalizedCursorPos > 1.03f)
            {
                ratingLabel = "MISS";
                return 0f;
            }

            float distFromCenter = Mathf.Abs(normalizedCursorPos - 0.5f) / 0.5f; // 0 = center bullseye, 1 = far edge
            if (distFromCenter <= 0.09f)
            {
                ratingLabel = "CRITICAL!!";
                return 1.75f;
            }
            if (distFromCenter <= 0.28f)
            {
                ratingLabel = "GREAT!";
                float t = 1f - ((distFromCenter - 0.09f) / 0.19f);
                return Mathf.Lerp(1.25f, 1.55f, t);
            }
            if (distFromCenter <= 0.60f)
            {
                ratingLabel = "GOOD";
                float t = 1f - ((distFromCenter - 0.28f) / 0.32f);
                return Mathf.Lerp(0.85f, 1.25f, t);
            }

            ratingLabel = "GRAZE";
            float edgeT = 1f - Mathf.Clamp01((distFromCenter - 0.60f) / 0.40f);
            return Mathf.Lerp(0.35f, 0.85f, edgeT);
        }

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
            dodgeBox.SetCenterAndSize(dodgeBoxPos, EffectiveDodgeBoxSize);
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

        public static void SafeDrawTexture(Rect r, Texture2D tex, ScaleMode mode = ScaleMode.StretchToFill)
        {
            if (tex == null || tex.width <= 0 || tex.height <= 0 || r.width <= 0.001f || r.height <= 0.001f) return;
            UnityEngine.GUI.DrawTexture(r, tex, mode);
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
                    if (tex.LoadImage(bytes) && tex.width > 2 && tex.height > 2)
                    {
                        tex.filterMode = FilterMode.Point;
                        tex.wrapMode = TextureWrapMode.Clamp;
                        return tex;
                    }
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

            EnsureCustomUITextures();
        }

        private static void EnsureCustomUITextures()
        {
            if (_uiBottomFrameTex == null) _uiBottomFrameTex = LoadTextureDirect("Assets/Sprites/UI/BattleUI_BottomBar_Frame.png");
            if (_uiOwenHeadTex == null) _uiOwenHeadTex = LoadTextureDirect("Assets/Sprites/UI/BattleUI_Owen_Head.png");
            if (_uiOwenNameTex == null) _uiOwenNameTex = LoadTextureDirect("Assets/Sprites/UI/BattleUI_Owen_Name.png");
            if (_uiHpFrameTex == null) _uiHpFrameTex = LoadTextureDirect("Assets/Sprites/UI/BattleUI_HP_Frame.png");
            if (_uiHpFillTex == null) _uiHpFillTex = LoadTextureDirect("Assets/Sprites/UI/BattleUI_HP_Fill.png");

            if (_uiBtnTextures[0] == null) _uiBtnTextures[0] = LoadTextureDirect("Assets/Sprites/UI/BattleUI_Btn_Purge.png");
            if (_uiBtnTextures[1] == null) _uiBtnTextures[1] = LoadTextureDirect("Assets/Sprites/UI/BattleUI_Btn_Act.png");
            if (_uiBtnTextures[2] == null) _uiBtnTextures[2] = LoadTextureDirect("Assets/Sprites/UI/BattleUI_Btn_Bag.png");
            if (_uiBtnTextures[3] == null) _uiBtnTextures[3] = LoadTextureDirect("Assets/Sprites/UI/BattleUI_Btn_Purify.png");

            if (_uiSoulHeartTex == null) _uiSoulHeartTex = LoadTextureDirect("Assets/Sprites/Combat/CyanSoul.png");
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

            AudioClip clipToPlay = voiceBlipClip;
            if (_speakingSlotIndex >= 0 && _speakingSlotIndex < _enemySlots.Count)
            {
                var speaker = _enemySlots[_speakingSlotIndex];
                if (speaker != null && speaker.config != null && speaker.config.voiceBlipClip != null)
                {
                    clipToPlay = speaker.config.voiceBlipClip;
                }
            }

            if (voiceAudioSource != null && clipToPlay != null)
            {
                voiceAudioSource.pitch = UnityEngine.Random.Range(0.97f, 1.03f);
                voiceAudioSource.PlayOneShot(clipToPlay, 0.45f);
            }
        }

        public void SetMenuState(BattleMenuState newState) => _menuState = newState;
        public void SelectCommandSlot(int slotIndex) => _selectedCommandIndex = Mathf.Clamp(slotIndex, 0, 3);
        public void SetInputDebounce(float seconds) => _inputDebounceTimer = seconds;
        public void SetDialogueText(string text) => _activeDialogueText = text;
        public void SetEnemySpeech(string spokenText, string bottomPrompt = "") => SetEnemySpeechForSlot(_speakingSlotIndex, spokenText, bottomPrompt);
        public void HideEnemySpeech() => HideGolferSpeech();

        public void SetEnemySpeechForSlot(int slotIndex, string spokenText, string bottomPrompt = "")
        {
            _speakingSlotIndex = Mathf.Max(0, slotIndex);
            SetGolferSpeech(spokenText, bottomPrompt);
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
            if (stageEncounterConfig == null)
            {
                stageEncounterConfig = GetComponent<BattleEncounterConfig>();
            }
            if (stageEncounterConfig != null)
            {
                InitializeWithEncounter(stageEncounterConfig);
            }
            else if (activeEnemyData != null)
            {
                InitializeWithEnemy(activeEnemyData);
            }
            else
            {
                StartNewPlayerTurn("* Golfer winds up a wicked drive!");
            }
        }

        public void InitializeWithEncounter(BattleEncounterConfig encounter, OverworldPlayerController player = null, OverworldEnemy enemy = null)
        {
            if (encounter == null)
            {
                InitializeWithEnemy(activeEnemyData, player, enemy);
                return;
            }

            encounter.SyncSlotCountWithNumberOfEnemies();
            _activeEncounterConfig = encounter;
            EnemyData primaryProfile = (encounter.enemies != null && encounter.enemies.Count > 0 && encounter.enemies[0] != null)
                ? encounter.enemies[0].baseProfile
                : activeEnemyData;

            InitializeCoreBattleSession(
                slots: encounter.enemies,
                autoLetterDuplicates: encounter.autoLetterDuplicateNames,
                formation: encounter.formationPreset,
                primaryProfile: primaryProfile,
                customSong: encounter.encounterSong,
                songVol: encounter.bgmVolume,
                songPitch: encounter.bgmStartPitch,
                openingText: encounter.customOpeningFlavorText,
                allowTutorial: encounter.runTutorialDriverIfPresent,
                player: player,
                enemy: enemy);
        }

        public void InitializeWithEnemy(EnemyData data, OverworldPlayerController player = null, OverworldEnemy enemy = null)
        {
            if (enemy != null)
            {
                var attachedEncounter = enemy.GetComponent<BattleEncounterConfig>();
                if (attachedEncounter != null)
                {
                    InitializeWithEncounter(attachedEncounter, player, enemy);
                    return;
                }
            }

            _activeEncounterConfig = null;
            var singleSlotList = new List<BattleEncounterSlot> { BattleEncounterSlot.FromEnemyData(data) };
            bool allowTut = (data != null) && (data.useTutorialOnboarding || (!string.IsNullOrEmpty(data.enemyId) && data.enemyId.ToLowerInvariant() == "golfer"));

            InitializeCoreBattleSession(
                slots: singleSlotList,
                autoLetterDuplicates: false,
                formation: EnemyFormationPreset.ManualOffsetsOnly,
                primaryProfile: data,
                customSong: data != null ? data.battleBgm : null,
                songVol: data != null ? data.bgmVolume : 0.85f,
                songPitch: data != null ? data.bgmStartPitch : 1.0f,
                openingText: "",
                allowTutorial: allowTut,
                player: player,
                enemy: enemy);
        }

        private void InitializeCoreBattleSession(
            List<BattleEncounterSlot> slots,
            bool autoLetterDuplicates,
            EnemyFormationPreset formation,
            EnemyData primaryProfile,
            AudioClip customSong,
            float songVol,
            float songPitch,
            string openingText,
            bool allowTutorial,
            OverworldPlayerController player,
            OverworldEnemy enemy)
        {
            isBattleActive = true;
            if (player != null) _overworldPlayer = player;
            if (enemy != null) _overworldEnemy = enemy;

            activeEnemyData = primaryProfile;
            RecalculateStagePositions();
            EnsureCombatantsExist();

            for (int i = 0; i < 4; i++)
            {
                _btnSpringPos[i] = 0f;
                _btnSpringVel[i] = 0f;
            }

            _currentTurnNumber = 0;
            _selectedTargetSlotCursor = 0;
            _speakingSlotIndex = 0;
            _pendingCombinedAttack = null;
            _accumulatedExpReward = 0;
            _accumulatedDollarReward = 0;
            _anyEnemyWasPurged = false;
            _firedStageSongRules.Clear();
            _firedEnemySongRules.Clear();
            _firedEncounterSongRules.Clear();
            if (_songTransitionCoroutine != null)
            {
                StopCoroutine(_songTransitionCoroutine);
                _songTransitionCoroutine = null;
            }
            _isCustomSongTransitionActive = false;

            BuildRuntimeEnemySlots(slots, autoLetterDuplicates, formation);
            SyncPrimaryLegacyEnemyFields();

            if (bgmAudioSource != null)
            {
                AudioClip clip = overrideEncounterSong != null
                    ? overrideEncounterSong
                    : (customSong != null ? customSong : (primaryProfile != null ? primaryProfile.battleBgm : null));
                if (clip == null)
                {
#if UNITY_EDITOR
                    clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Battle_Beta.mp3");
#endif
                }
                if (clip != null)
                {
                    DigiPurity.World.OverworldBgmManager.Instance.StopMusic();

                    float startVol = Mathf.Clamp01(songVol);
                    float startPitch = Mathf.Clamp(songPitch, 0.2f, 2.0f);

                    bgmAudioSource.clip = clip;
                    bgmAudioSource.loop = true;
                    bgmAudioSource.volume = startVol;
                    bgmAudioSource.pitch = startPitch;
                    _targetBgmVolume = startVol;
                    _currentBgmPitch = startPitch;
                    _targetBgmPitch = startPitch;
                    _targetBgmDistortion = 0.0f;
                    _targetBgmCutoff = 22000.0f;
                    _flutterIntensity = 0.0f;
                    _isMusicStopping = false;
                    EnsureAudioFilters();
                    if (bgmDistortion != null) bgmDistortion.distortionLevel = 0f;
                    if (bgmLowPass != null) bgmLowPass.cutoffFrequency = 22000f;
                    bgmAudioSource.Play();
                }
            }

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var roomCam = mainCam.GetComponent<RoomCameraController>();
                if (roomCam != null) roomCam.enabled = false;
                mainCam.transform.position = new Vector3(stageCenter.x, stageCenter.y + 0.5f, -10f);
                mainCam.orthographic = true;
                mainCam.orthographicSize = 6.0f;
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = Color.black;
                mainCam.rect = RoomCameraController.Calculate4By3ViewportRect(Screen.width, Screen.height);
            }

            RecalculateStagePositions();

            purgeAttackCount = 0;
            _inputDebounceTimer = 0.25f;

            activeTutorialDriver = null;
            if (enemy != null)
            {
                activeTutorialDriver = enemy.GetComponent<BattleTutorialEncounter>();
            }
            if (activeTutorialDriver == null)
            {
                activeTutorialDriver = GetComponent<BattleTutorialEncounter>();
            }
            if (activeTutorialDriver == null && allowTutorial && _enemySlots.Count == 1)
            {
                activeTutorialDriver = gameObject.AddComponent<BattleTutorialEncounter>();
            }

            if (allowTutorial && activeTutorialDriver != null && activeTutorialDriver.enableTutorialSequence)
            {
                tutorialPhase = BattleTutorialPhase.Intro1;
                activeTutorialDriver.BeginEncounter(this);
            }
            else
            {
                tutorialPhase = BattleTutorialPhase.Completed;
                string introMsg = !string.IsNullOrEmpty(openingText)
                    ? openingText
                    : (_enemySlots.Count > 1
                        ? $"* {BuildActivePartyNamesSummary()} block the way!"
                        : $"* {activeEnemyName} blocks the way!");
                StartNewPlayerTurn(introMsg);
            }
        }

        private void BuildRuntimeEnemySlots(List<BattleEncounterSlot> rawSlots, bool autoLetterDuplicates, EnemyFormationPreset formation)
        {
            _enemySlots.Clear();
            if (rawSlots == null || rawSlots.Count == 0)
            {
                rawSlots = new List<BattleEncounterSlot> { BattleEncounterSlot.CreateGolferPresetSlot(activeEnemyData) };
            }

            // Hide any extra animators from prior multi-enemy fights first
            for (int e = 0; e < _extraSlotAnimators.Count; e++)
            {
                if (_extraSlotAnimators[e] != null) _extraSlotAnimators[e].gameObject.SetActive(false);
            }

            // Count duplicate names for A / B / C disambiguation
            var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rawSlots.Count; i++)
            {
                string baseName = (rawSlots[i] != null && !string.IsNullOrEmpty(rawSlots[i].enemyName)) ? rawSlots[i].enemyName.Trim() : "Enemy";
                nameCounts[baseName] = nameCounts.TryGetValue(baseName, out int c) ? c + 1 : 1;
            }
            var seenCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            int total = Mathf.Clamp(rawSlots.Count, 1, 4);
            for (int i = 0; i < total; i++)
            {
                var cfg = rawSlots[i] ?? BattleEncounterSlot.CreateGolferPresetSlot(activeEnemyData);
                string baseName = !string.IsNullOrEmpty(cfg.enemyName) ? cfg.enemyName.Trim() : $"Enemy {i + 1}";
                string finalName = baseName;

                if (autoLetterDuplicates && nameCounts.TryGetValue(baseName, out int dupTotal) && dupTotal > 1)
                {
                    int idx = seenCounts.TryGetValue(baseName, out int s) ? s : 0;
                    seenCounts[baseName] = idx + 1;
                    char suffix = (char)('A' + idx);
                    finalName = $"{baseName} {suffix}";
                }

                EnemyBattleAnimator slotAnim;
                if (i == 0)
                {
                    slotAnim = enemyAnimator;
                    if (slotAnim != null) slotAnim.gameObject.SetActive(true);
                }
                else
                {
                    int extraIdx = i - 1;
                    while (_extraSlotAnimators.Count <= extraIdx)
                    {
                        GameObject extraObj = new GameObject($"Enemy_Slot_{_extraSlotAnimators.Count + 2}");
                        extraObj.transform.SetParent(transform);
                        var anim = extraObj.AddComponent<GolferBattleAnimator>();
                        anim.EnsureRenderer();
                        anim.AutoLoadSprite();
                        _extraSlotAnimators.Add(anim);
                    }
                    slotAnim = _extraSlotAnimators[extraIdx];
                    slotAnim.gameObject.SetActive(true);
                }

                Vector3 slotPos = BattleEncounterConfig.CalculateSlotStagePosition(
                    stageCenter,
                    enemyStageOffset,
                    i,
                    total,
                    formation,
                    cfg.visuals != null ? cfg.visuals.stagePositionOffset : Vector2.zero,
                    cfg.visuals != null ? cfg.visuals.groundLineOffset : 0f);

                if (slotAnim != null)
                {
                    var sr = slotAnim.EnsureRenderer();
                    sr.enabled = true;
                    slotAnim.ConfigureFromVisualSetup(cfg.visuals, cfg.baseProfile != null ? cfg.baseProfile : activeEnemyData, i);
                    slotAnim.transform.position = slotPos;
                    slotAnim.SetBasePosition(slotPos);
                }

                if (cfg.conditionalDialogue != null)
                {
                    for (int d = 0; d < cfg.conditionalDialogue.Count; d++)
                    {
                        if (cfg.conditionalDialogue[d] != null) cfg.conditionalDialogue[d].runtimeLineCursor = 0;
                    }
                }

                int mHp = cfg.maxHp > 0 ? cfg.maxHp : 100;
                int sHp = (cfg.startingHp > 0 && cfg.startingHp <= mHp) ? cfg.startingHp : mHp;
                int pThresh = cfg.purifyThreshold > 0 ? cfg.purifyThreshold : 100;

                var rt = new RuntimeEnemySlot
                {
                    slotIndex = i,
                    displayName = finalName,
                    config = cfg,
                    maxHp = mHp,
                    currentHp = sHp,
                    purgeMarkerHp = cfg.purgeMarkerHp,
                    currentPurify = 0,
                    purifyThreshold = pThresh,
                    isResolved = false,
                    wasPurified = false,
                    animator = slotAnim,
                    sequentialAttackCursor = 0,
                    pendingDamageMultiplier = 1f,
                    pendingSpeedMultiplier = 1f
                };
                _enemySlots.Add(rt);
            }
        }

        public List<RuntimeEnemySlot> GetActiveUnresolvedSlots()
        {
            var list = new List<RuntimeEnemySlot>();
            for (int i = 0; i < _enemySlots.Count; i++)
            {
                if (_enemySlots[i] != null && !_enemySlots[i].isResolved)
                {
                    list.Add(_enemySlots[i]);
                }
            }
            return list;
        }

        public RuntimeEnemySlot GetTargetedEnemySlot()
        {
            var active = GetActiveUnresolvedSlots();
            if (active.Count == 0)
            {
                return _enemySlots.Count > 0 ? _enemySlots[0] : null;
            }
            _selectedTargetSlotCursor = Mathf.Clamp(_selectedTargetSlotCursor, 0, active.Count - 1);
            return active[_selectedTargetSlotCursor];
        }

        private void SyncPrimaryLegacyEnemyFields()
        {
            var target = GetTargetedEnemySlot();
            if (target == null) return;

            activeEnemyName = target.displayName;
            enemyMaxHp = target.maxHp;
            enemyCurrentHp = target.currentHp;
            purgeMarkerHp = target.purgeMarkerHp;
            enemyCurrentMercy = target.purifyThreshold > 0
                ? Mathf.Clamp(Mathf.RoundToInt(((float)target.currentPurify / target.purifyThreshold) * 100f), 0, 100)
                : target.currentPurify;
            if (target.animator != null)
            {
                enemyAnimator = target.animator;
            }
        }

        private string BuildActivePartyNamesSummary()
        {
            var active = GetActiveUnresolvedSlots();
            if (active.Count == 0) return activeEnemyName;
            if (active.Count == 1) return active[0].displayName;
            if (active.Count == 2) return $"{active[0].displayName} & {active[1].displayName}";
            return $"{active[0].displayName}, {active[1].displayName} & {active.Count - 2} more";
        }

        public string TryResolveConditionalDialogue(
            RuntimeEnemySlot slot,
            DialogueTriggerType context,
            int chosenAttackIdx = -1,
            string actUsed = "")
        {
            if (slot == null || slot.config == null || slot.config.conditionalDialogue == null) return "";

            ConditionalDialogueRule bestRule = null;
            int bestIndex = -1;

            for (int i = 0; i < slot.config.conditionalDialogue.Count; i++)
            {
                var r = slot.config.conditionalDialogue[i];
                if (r == null) continue;
                if (r.triggerOnce && slot.firedDialogueRules.Contains(i)) continue;

                if (r.Matches(context, _currentTurnNumber, slot.HpPercent, slot.PurifyPercent, chosenAttackIdx, actUsed))
                {
                    if (bestRule == null || r.priority > bestRule.priority)
                    {
                        bestRule = r;
                        bestIndex = i;
                    }
                }
            }

            if (bestRule != null)
            {
                if (bestRule.triggerOnce && bestIndex >= 0) slot.firedDialogueRules.Add(bestIndex);
                return bestRule.NextLine();
            }
            return "";
        }

        public void StartNewPlayerTurn(string flavorText = "")
        {
            _currentTurnNumber++;
            EvaluateSongRules(SongTriggerCondition.OnTurnNumberReached);

            HideGolferSpeech();
            _speechBarkTimer = 0f;
            _menuState = BattleMenuState.CommandSelect;
            _selectedCommandIndex = 0;
            _selectedActIndex = 0;
            _selectedMercyIndex = 0;
            _inputDebounceTimer = 0.15f;

            var active = GetActiveUnresolvedSlots();
            if (_selectedTargetSlotCursor >= active.Count) _selectedTargetSlotCursor = 0;
            SyncPrimaryLegacyEnemyFields();
            RecalculateStagePositions();

            if (!string.IsNullOrEmpty(flavorText))
            {
                _activeDialogueText = flavorText;
            }
            else if (active.Count > 1)
            {
                _activeDialogueText = $"* {BuildActivePartyNamesSummary()} await your move.";
            }
            else
            {
                _activeDialogueText = (enemyCurrentHp <= purgeMarkerHp)
                    ? $"* {activeEnemyName} is exhausted and completely vulnerable. (0 HP)"
                    : $"* {activeEnemyName} prepares for your move.";
            }

            if (owenAnimator != null)
            {
                owenAnimator.transform.position = owenBattlePos;
                owenAnimator.SetBasePosition(owenBattlePos);
                owenAnimator.PlayIdle();
            }

            EnemyFormationPreset form = _activeEncounterConfig != null
                ? _activeEncounterConfig.formationPreset
                : EnemyFormationPreset.ManualOffsetsOnly;

            for (int i = 0; i < _enemySlots.Count; i++)
            {
                var slot = _enemySlots[i];
                if (slot == null || slot.isResolved || slot.animator == null) continue;
                Vector3 pos = BattleEncounterConfig.CalculateSlotStagePosition(
                    stageCenter,
                    enemyStageOffset,
                    i,
                    _enemySlots.Count,
                    form,
                    slot.config.visuals != null ? slot.config.visuals.stagePositionOffset : Vector2.zero,
                    slot.config.visuals != null ? slot.config.visuals.groundLineOffset : 0f);
                slot.animator.transform.position = pos;
                slot.animator.SetBasePosition(pos);
                slot.animator.UpdateStatusPosture(slot.IsAtPurgeMarker, slot.IsPurifyReady);
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

        private void UpdateButtonSprings()
        {
            bool hasHighlightedButton =
                _menuState == BattleMenuState.CommandSelect ||
                _menuState == BattleMenuState.FightTarget ||
                _menuState == BattleMenuState.AttackTiming ||
                _menuState == BattleMenuState.ActTarget ||
                _menuState == BattleMenuState.ActSelect ||
                _menuState == BattleMenuState.ItemSelect ||
                _menuState == BattleMenuState.MercySelect;

            float dt = Time.deltaTime;
            for (int i = 0; i < 4; i++)
            {
                float target = (hasHighlightedButton && i == _selectedCommandIndex) ? 1.0f : 0.0f;
                StepButtonBounceSpring(ref _btnSpringPos[i], ref _btnSpringVel[i], target, dt);
            }
        }

        private void Update()
        {
            if (_inputDebounceTimer > 0f)
            {
                _inputDebounceTimer -= Time.deltaTime;
            }

            Camera cam = Camera.main;
            if (cam != null && cam.orthographic)
            {
                Rect targetRect = RoomCameraController.Calculate4By3ViewportRect(Screen.width, Screen.height);
                if (cam.rect != targetRect)
                {
                    cam.rect = targetRect;
                    RecalculateStagePositions();
                }
            }

            UpdateButtonSprings();
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
                case BattleMenuState.AttackTiming:
                    HandleAttackTiming();
                    break;
                case BattleMenuState.ActTarget:
                    HandleActTarget();
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
            if (!_isMusicStopping && !_isCustomSongTransitionActive && bgmAudioSource != null && bgmAudioSource.isPlaying)
            {
                if (Mathf.Abs(bgmAudioSource.volume - _targetBgmVolume) > 0.002f)
                {
                    bgmAudioSource.volume = Mathf.MoveTowards(bgmAudioSource.volume, _targetBgmVolume, Time.deltaTime * 0.85f);
                }

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

        /// <summary>
        /// Evaluates Inspector-configured conditional song rules on both BattleStageController and activeEnemyData.
        /// Returns true if at least one matching rule was triggered.
        /// </summary>
        public bool EvaluateSongRules(SongTriggerCondition eventContext, string actUsedName = "")
        {
            float enemyHpPct = enemyMaxHp > 0 ? ((float)enemyCurrentHp / enemyMaxHp) * 100f : 0f;
            float owenHpPct = owenMaxHp > 0 ? ((float)owenCurrentHp / owenMaxHp) * 100f : 0f;
            float purifyPct = enemyCurrentMercy;

            bool triggeredAny = false;

            if (stageSongRules != null)
            {
                for (int i = 0; i < stageSongRules.Count; i++)
                {
                    var rule = stageSongRules[i];
                    if (rule == null) continue;
                    if (rule.triggerOnce && _firedStageSongRules.Contains(i)) continue;

                    if (rule.Matches(eventContext, enemyHpPct, owenHpPct, purifyPct, _currentTurnNumber, actUsedName))
                    {
                        if (rule.triggerOnce) _firedStageSongRules.Add(i);
                        ApplySongRule(rule);
                        triggeredAny = true;
                    }
                }
            }

            if (_activeEncounterConfig != null && _activeEncounterConfig.conditionalSongRules != null)
            {
                var encRules = _activeEncounterConfig.conditionalSongRules;
                for (int i = 0; i < encRules.Count; i++)
                {
                    var rule = encRules[i];
                    if (rule == null) continue;
                    if (rule.triggerOnce && _firedEncounterSongRules.Contains(i)) continue;

                    if (rule.Matches(eventContext, enemyHpPct, owenHpPct, purifyPct, _currentTurnNumber, actUsedName))
                    {
                        if (rule.triggerOnce) _firedEncounterSongRules.Add(i);
                        ApplySongRule(rule);
                        triggeredAny = true;
                    }
                }
            }

            if (activeEnemyData != null && activeEnemyData.conditionalSongRules != null)
            {
                var enemyRules = activeEnemyData.conditionalSongRules;
                for (int i = 0; i < enemyRules.Count; i++)
                {
                    var rule = enemyRules[i];
                    if (rule == null) continue;
                    if (rule.triggerOnce && _firedEnemySongRules.Contains(i)) continue;

                    if (rule.Matches(eventContext, enemyHpPct, owenHpPct, purifyPct, _currentTurnNumber, actUsedName))
                    {
                        if (rule.triggerOnce) _firedEnemySongRules.Add(i);
                        ApplySongRule(rule);
                        triggeredAny = true;
                    }
                }
            }

            return triggeredAny;
        }

        public void ApplySongRule(BattleSongRule rule)
        {
            if (rule == null || bgmAudioSource == null) return;
            EnsureAudioFilters();

            if (_songTransitionCoroutine != null)
            {
                StopCoroutine(_songTransitionCoroutine);
                _songTransitionCoroutine = null;
            }
            _isCustomSongTransitionActive = false;

            switch (rule.action)
            {
                case SongActionType.PauseSong:
                    bgmAudioSource.Pause();
                    break;

                case SongActionType.TapeStopToSilence:
                    float stopDur = rule.transitionDuration > 0.01f ? rule.transitionDuration : 1.5f;
                    _songTransitionCoroutine = StartCoroutine(PurgeMusicSlowdownAndStopRoutine(stopDur));
                    break;

                case SongActionType.FadeOutAndStop:
                case SongActionType.SwitchToNewSong:
                case SongActionType.ModifyCurrentSong:
                case SongActionType.ResumeSong:
                    _songTransitionCoroutine = StartCoroutine(ExecuteSongRuleRoutine(rule));
                    break;
            }
        }

        private IEnumerator ExecuteSongRuleRoutine(BattleSongRule rule)
        {
            _isCustomSongTransitionActive = true;
            float dur = Mathf.Max(0f, rule.transitionDuration);

            if (rule.action == SongActionType.FadeOutAndStop)
            {
                float startVol = bgmAudioSource.volume;
                if (dur > 0.01f)
                {
                    float elapsed = 0f;
                    while (elapsed < dur)
                    {
                        elapsed += Time.deltaTime;
                        bgmAudioSource.volume = Mathf.Lerp(startVol, 0f, Mathf.Clamp01(elapsed / dur));
                        yield return null;
                    }
                }
                bgmAudioSource.volume = 0f;
                bgmAudioSource.Stop();
                _targetBgmVolume = 0f;
                _isCustomSongTransitionActive = false;
                _songTransitionCoroutine = null;
                yield break;
            }

            if (rule.action == SongActionType.SwitchToNewSong && rule.targetSong != null)
            {
                float prevTime = bgmAudioSource.isPlaying ? bgmAudioSource.time : 0f;
                float halfDur = dur * 0.5f;

                if (bgmAudioSource.isPlaying && halfDur > 0.01f)
                {
                    float startVol = bgmAudioSource.volume;
                    float elapsed = 0f;
                    while (elapsed < halfDur)
                    {
                        elapsed += Time.deltaTime;
                        bgmAudioSource.volume = Mathf.Lerp(startVol, 0f, Mathf.Clamp01(elapsed / halfDur));
                        yield return null;
                    }
                }

                bgmAudioSource.clip = rule.targetSong;
                bgmAudioSource.loop = rule.loopTargetSong;
                if (rule.syncPlaybackTimeOnSwitch && rule.targetSong.length > 0.05f)
                {
                    bgmAudioSource.time = prevTime % rule.targetSong.length;
                }
                else
                {
                    bgmAudioSource.time = 0f;
                }

                bgmAudioSource.pitch = rule.targetPitch;
                _currentBgmPitch = rule.targetPitch;
                _targetBgmPitch = rule.targetPitch;
                _targetBgmVolume = rule.targetVolume;
                _targetBgmDistortion = rule.targetDistortion;
                _targetBgmCutoff = rule.targetLowPassCutoff;

                if (bgmDistortion != null)
                {
                    bgmDistortion.enabled = rule.targetDistortion > 0.01f;
                    bgmDistortion.distortionLevel = rule.targetDistortion;
                }
                if (bgmLowPass != null)
                {
                    bgmLowPass.enabled = rule.targetLowPassCutoff < 21000f;
                    bgmLowPass.cutoffFrequency = rule.targetLowPassCutoff;
                }

                bgmAudioSource.volume = halfDur > 0.01f ? 0f : rule.targetVolume;
                bgmAudioSource.Play();

                if (halfDur > 0.01f)
                {
                    float elapsed = 0f;
                    while (elapsed < halfDur)
                    {
                        elapsed += Time.deltaTime;
                        bgmAudioSource.volume = Mathf.Lerp(0f, rule.targetVolume, Mathf.Clamp01(elapsed / halfDur));
                        yield return null;
                    }
                    bgmAudioSource.volume = rule.targetVolume;
                }

                _isCustomSongTransitionActive = false;
                _songTransitionCoroutine = null;
                yield break;
            }

            // ModifyCurrentSong or ResumeSong
            if (rule.action == SongActionType.ResumeSong && !bgmAudioSource.isPlaying)
            {
                bgmAudioSource.UnPause();
                if (!bgmAudioSource.isPlaying && bgmAudioSource.clip != null)
                {
                    bgmAudioSource.Play();
                }
            }

            float initVol = bgmAudioSource.volume;
            float initPitch = bgmAudioSource.pitch;
            float initDist = bgmDistortion != null ? bgmDistortion.distortionLevel : 0f;
            float initCut = bgmLowPass != null ? bgmLowPass.cutoffFrequency : 22000f;

            _targetBgmVolume = rule.targetVolume;
            _targetBgmPitch = rule.targetPitch;
            _targetBgmDistortion = rule.targetDistortion;
            _targetBgmCutoff = rule.targetLowPassCutoff;

            if (bgmDistortion != null && (initDist > 0.01f || rule.targetDistortion > 0.01f))
            {
                bgmDistortion.enabled = true;
            }
            if (bgmLowPass != null && (initCut < 21000f || rule.targetLowPassCutoff < 21000f))
            {
                bgmLowPass.enabled = true;
            }

            if (dur > 0.01f)
            {
                float elapsed = 0f;
                while (elapsed < dur)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / dur);
                    bgmAudioSource.volume = Mathf.Lerp(initVol, rule.targetVolume, t);
                    _currentBgmPitch = Mathf.Lerp(initPitch, rule.targetPitch, t);
                    bgmAudioSource.pitch = _currentBgmPitch;
                    if (bgmDistortion != null) bgmDistortion.distortionLevel = Mathf.Lerp(initDist, rule.targetDistortion, t);
                    if (bgmLowPass != null) bgmLowPass.cutoffFrequency = Mathf.Lerp(initCut, rule.targetLowPassCutoff, t);
                    yield return null;
                }
            }

            bgmAudioSource.volume = rule.targetVolume;
            _currentBgmPitch = rule.targetPitch;
            bgmAudioSource.pitch = rule.targetPitch;
            if (bgmDistortion != null)
            {
                bgmDistortion.distortionLevel = rule.targetDistortion;
                bgmDistortion.enabled = rule.targetDistortion > 0.01f;
            }
            if (bgmLowPass != null)
            {
                bgmLowPass.cutoffFrequency = rule.targetLowPassCutoff;
                bgmLowPass.enabled = rule.targetLowPassCutoff < 21000f;
            }

            _isCustomSongTransitionActive = false;
            _songTransitionCoroutine = null;
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

                _inputDebounceTimer = 0.22f;

                if (activeTutorialDriver != null && activeTutorialDriver.enableTutorialSequence)
                {
                    activeTutorialDriver.AdvanceNarrative(this);
                    if (!activeTutorialDriver.IsActiveTutorial)
                    {
                        tutorialPhase = BattleTutorialPhase.Completed;
                    }
                }
                else
                {
                    tutorialPhase = BattleTutorialPhase.Completed;
                    _menuState = BattleMenuState.CommandSelect;
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

            if (moved && activeTutorialDriver != null && activeTutorialDriver.OnCommandScrolled(this))
            {
                return;
            }

            if (activeTutorialDriver != null && activeTutorialDriver.IsActiveTutorial)
            {
                if (_inputDebounceTimer <= 0f && IsConfirmDown())
                {
                    if (activeTutorialDriver.AllowCommandConfirm(this, _selectedCommandIndex))
                    {
                        _inputDebounceTimer = 0.20f;
                        _selectedActIndex = 0;
                        _menuState = BattleMenuState.ActSelect;
                    }
                }
                return;
            }

            if (_inputDebounceTimer <= 0f && IsConfirmDown())
            {
                _inputDebounceTimer = 0.20f;
                var activeEnemies = GetActiveUnresolvedSlots();
                switch (_selectedCommandIndex)
                {
                    case 0: // PURGE
                        _selectedTargetSlotCursor = Mathf.Clamp(_selectedTargetSlotCursor, 0, Mathf.Max(0, activeEnemies.Count - 1));
                        SyncPrimaryLegacyEnemyFields();
                        _menuState = BattleMenuState.FightTarget;
                        break;
                    case 1: // ACT
                        _selectedActIndex = 0;
                        if (activeEnemies.Count > 1)
                        {
                            _selectedTargetSlotCursor = Mathf.Clamp(_selectedTargetSlotCursor, 0, activeEnemies.Count - 1);
                            SyncPrimaryLegacyEnemyFields();
                            _menuState = BattleMenuState.ActTarget;
                        }
                        else
                        {
                            _selectedTargetSlotCursor = 0;
                            SyncPrimaryLegacyEnemyFields();
                            _menuState = BattleMenuState.ActSelect;
                        }
                        break;
                    case 2: // BAG
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

            var active = GetActiveUnresolvedSlots();
            if (active.Count > 1)
            {
                if (IsUpDown() || IsLeftDown())
                {
                    _selectedTargetSlotCursor = (_selectedTargetSlotCursor + active.Count - 1) % active.Count;
                    SyncPrimaryLegacyEnemyFields();
                }
                else if (IsDownDown() || IsRightDown())
                {
                    _selectedTargetSlotCursor = (_selectedTargetSlotCursor + 1) % active.Count;
                    SyncPrimaryLegacyEnemyFields();
                }
            }

            if (_inputDebounceTimer <= 0f && IsConfirmDown())
            {
                SyncPrimaryLegacyEnemyFields();
                _attackBarPos = 0f;
                _attackBarLocked = false;
                _attackDidMiss = false;
                _lastAttackRating = "";
                _lastAttackMultiplier = 1.0f;
                _lastAttackAccuracy = 1.0f;
                _inputDebounceTimer = 0.08f;
                _menuState = BattleMenuState.AttackTiming;
            }
        }

        private void HandleActTarget()
        {
            if (IsCancelDown())
            {
                _inputDebounceTimer = 0.15f;
                _menuState = BattleMenuState.CommandSelect;
                return;
            }

            var active = GetActiveUnresolvedSlots();
            if (active.Count > 1)
            {
                if (IsUpDown() || IsLeftDown())
                {
                    _selectedTargetSlotCursor = (_selectedTargetSlotCursor + active.Count - 1) % active.Count;
                    SyncPrimaryLegacyEnemyFields();
                }
                else if (IsDownDown() || IsRightDown())
                {
                    _selectedTargetSlotCursor = (_selectedTargetSlotCursor + 1) % active.Count;
                    SyncPrimaryLegacyEnemyFields();
                }
            }

            if (_inputDebounceTimer <= 0f && IsConfirmDown())
            {
                _inputDebounceTimer = 0.18f;
                _selectedActIndex = 0;
                SyncPrimaryLegacyEnemyFields();
                _menuState = BattleMenuState.ActSelect;
            }
        }

        private void HandleAttackTiming()
        {
            if (_attackBarLocked) return;

            _attackBarPos += Time.deltaTime * _attackBarSpeed;

            if (_inputDebounceTimer <= 0f && IsConfirmDown())
            {
                _attackBarLocked = true;
                _lastAttackMultiplier = CalculateAttackTimingMultiplier(_attackBarPos, out _lastAttackRating);
                _attackDidMiss = _lastAttackMultiplier <= 0.001f;
                _lastAttackAccuracy = Mathf.Clamp01(1f - (Mathf.Abs(_attackBarPos - 0.5f) / 0.5f));
                StartCoroutine(ResolveAttackTimingAndStrikeRoutine());
            }
            else if (_attackBarPos > 1.03f)
            {
                _attackBarLocked = true;
                _attackDidMiss = true;
                _lastAttackAccuracy = 0f;
                _lastAttackMultiplier = 0f;
                _lastAttackRating = "MISS";
                StartCoroutine(ResolveAttackTimingAndStrikeRoutine());
            }
        }

        private IEnumerator ResolveAttackTimingAndStrikeRoutine()
        {
            yield return new WaitForSeconds(0.28f);
            var target = GetTargetedEnemySlot();
            bool isPurgeExecution = target != null ? target.IsAtPurgeMarker : IsEnemyAtPurgeMarker;
            yield return StartCoroutine(FightAttackRoutine(isPurgeExecution, _lastAttackMultiplier, _attackDidMiss, _lastAttackRating));
        }

        private IEnumerator FightAttackRoutine(bool isPurgeExecution, float damageMultiplier = 1.0f, bool didMiss = false, string ratingLabel = "")
        {
            _menuState = BattleMenuState.FightAttackAnim;
            var target = GetTargetedEnemySlot();
            SyncPrimaryLegacyEnemyFields();

            if (owenAnimator != null)
            {
                owenAnimator.PlayAttack();
            }

            yield return new WaitForSeconds(0.35f);

            if (didMiss)
            {
                _lastDamageDealt = 0;
                string missBark = TryResolveConditionalDialogue(target, DialogueTriggerType.OnPlayerMiss);
                if (!string.IsNullOrEmpty(missBark) && target != null)
                {
                    SetEnemySpeechForSlot(target.slotIndex, missBark, "* Owen swung wide... MISS!\n* [Z / ENTER] Next");
                }
                else
                {
                    _activeDialogueText = "* Owen swung wide... MISS!\n* [Z / ENTER] Next";
                }
                _menuState = BattleMenuState.ActResponse;
                _inputDebounceTimer = 0.25f;
                yield break;
            }

            purgeAttackCount++;

            if (isPurgeExecution)
            {
                string purgedName = target != null ? target.displayName : activeEnemyName;
                if (target != null) target.currentHp = 0;
                enemyCurrentHp = 0;
                _lastDamageDealt = 99;
                _anyEnemyWasPurged = true;

                if (target != null && target.config != null)
                {
                    _accumulatedExpReward += target.config.expOnPurge;
                    _accumulatedDollarReward += target.config.digiDollarsReward * 2;
                }
                else
                {
                    _accumulatedExpReward += 25;
                    _accumulatedDollarReward += 10;
                }

                var anim = (target != null && target.animator != null) ? target.animator : enemyAnimator;
                if (anim != null)
                {
                    anim.PlayHurt(0.40f);
                }

                bool isLastActiveEnemy = GetActiveUnresolvedSlots().Count <= 1;
                if (isLastActiveEnemy)
                {
                    if (!EvaluateSongRules(SongTriggerCondition.OnPurgeExecuted))
                    {
                        StartCoroutine(PurgeMusicSlowdownAndStopRoutine(1.8f));
                    }
                }
                else
                {
                    EvaluateSongRules(SongTriggerCondition.OnPurgeExecuted);
                }

                yield return new WaitForSeconds(1.15f); // 1~ second visual delay after strike before returning to menu
                _menuState = BattleMenuState.FightAttackAnim;

                string[] purgeLines = null;
                string cutOffLine = "";
                if (activeTutorialDriver != null && activeTutorialDriver.lethalPurgeMonologue != null && activeTutorialDriver.lethalPurgeMonologue.Length > 0)
                {
                    purgeLines = activeTutorialDriver.lethalPurgeMonologue;
                    cutOffLine = activeTutorialDriver.lethalPurgeCutOffLine;
                }
                else if (target != null && target.config != null && target.config.lethalPurgeMonologue != null && target.config.lethalPurgeMonologue.Length > 0)
                {
                    purgeLines = target.config.lethalPurgeMonologue;
                    cutOffLine = target.config.lethalPurgeCutOffLine;
                }

                int speakerIdx = target != null ? target.slotIndex : 0;
                if (purgeLines != null && purgeLines.Length > 0)
                {
                    for (int i = 0; i < purgeLines.Length; i++)
                    {
                        SetEnemySpeechForSlot(speakerIdx, purgeLines[i], "* Owen executes lethal PURGE!\n* [Z / ENTER] Next");
                        _inputDebounceTimer = 0.35f;

                        while (true)
                        {
                            if (_inputDebounceTimer <= 0f && IsConfirmDown())
                            {
                                if (!SkipSpeechTyping()) break;
                            }
                            yield return null;
                        }
                    }

                    if (!string.IsNullOrEmpty(cutOffLine))
                    {
                        SetEnemySpeechForSlot(speakerIdx, cutOffLine, "* Owen executes lethal PURGE!");
                        yield return new WaitForSeconds(0.75f);
                    }
                }

                HideGolferSpeech();
                _activeDialogueText = $"* {purgedName.ToUpper()} implodes into cyber sparks...";

                bool implosionDone = false;
                if (anim != null)
                {
                    anim.PlayPurgeImplosion(1.6f, () => implosionDone = true);
                }
                else
                {
                    implosionDone = true;
                }

                while (!implosionDone) yield return null;

                if (target != null)
                {
                    target.isResolved = true;
                    target.wasPurified = false;
                    if (target.animator != null) target.animator.gameObject.SetActive(false);
                }

                var remaining = GetActiveUnresolvedSlots();
                if (remaining.Count > 0)
                {
                    _selectedTargetSlotCursor = 0;
                    SyncPrimaryLegacyEnemyFields();
                    var survivor = remaining[0];
                    string allyPurgedBark = TryResolveConditionalDialogue(survivor, DialogueTriggerType.WhenAllyPurged);
                    if (!string.IsNullOrEmpty(allyPurgedBark))
                    {
                        SetEnemySpeechForSlot(survivor.slotIndex, allyPurgedBark, $"* {purgedName.ToUpper()} was PURGED! {remaining.Count} enemy remaining.");
                    }
                    else
                    {
                        _activeDialogueText = $"* {purgedName.ToUpper()} was PURGED! {remaining.Count} enemy remaining.";
                    }
                    _menuState = BattleMenuState.ActResponse;
                    _inputDebounceTimer = 0.25f;
                }
                else
                {
                    _isPurifiedVictory = false;
                    _menuState = BattleMenuState.Victory;
                    _inputDebounceTimer = 0.35f;
                    int finalExp = _accumulatedExpReward > 0 ? _accumulatedExpReward : 25;
                    int finalDol = _accumulatedDollarReward > 0 ? _accumulatedDollarReward : 10;
                    _activeDialogueText = $"* {purgedName.ToUpper()} was PURGED.\n* Digital code collapsed into sparks and faded.\n* Got {finalExp} EXP and {finalDol} D$. The network feels colder.";
                }
            }
            else
            {
                float affinityMult = (target != null && target.config != null)
                    ? target.config.GetDamageMultiplier(BattleDamageType.PhysicalScythe)
                    : 1.0f;
                int def = (target != null && target.config != null) ? Mathf.Max(0, target.config.defense) : 0;

                string affinityTag = "";
                if (affinityMult <= 0.001f)
                {
                    _lastDamageDealt = 0;
                    affinityTag = " [IMMUNE]";
                }
                else
                {
                    int raw = Mathf.RoundToInt(BaseOwenAttackDamage * damageMultiplier * affinityMult);
                    _lastDamageDealt = Mathf.Max(1, raw - def);
                    if (affinityMult >= 1.35f) affinityTag = " [WEAK!]";
                    else if (affinityMult <= 0.70f) affinityTag = " [RESIST]";
                }

                if (target != null)
                {
                    target.currentHp = Mathf.Max(0, target.currentHp - _lastDamageDealt);
                    target.animator?.UpdateStatusPosture(target.IsAtPurgeMarker, target.IsPurifyReady);
                }
                else
                {
                    enemyCurrentHp = Mathf.Max(0, enemyCurrentHp - _lastDamageDealt);
                }
                SyncPrimaryLegacyEnemyFields();

                var anim = (target != null && target.animator != null) ? target.animator : enemyAnimator;
                if (anim != null)
                {
                    anim.PlayHurt(0.35f);
                }

                bool autoSlow = _activeEncounterConfig != null
                    ? _activeEncounterConfig.enableAutoLowHpSlowdown
                    : (activeEnemyData == null || activeEnemyData.enableAutoLowHpSlowdown);

                if (autoSlow)
                {
                    float hpRatio = ComputeTotalEncounterHpRatio();
                    float basePitch = _activeEncounterConfig != null
                        ? _activeEncounterConfig.bgmStartPitch
                        : (activeEnemyData != null ? activeEnemyData.bgmStartPitch : 1.0f);
                    _targetBgmPitch = Mathf.Lerp(0.68f * basePitch, basePitch, hpRatio);
                    _targetBgmDistortion = Mathf.Lerp(0.38f, 0.0f, hpRatio);
                    _targetBgmCutoff = Mathf.Lerp(4200f, 22000f, hpRatio);
                    _flutterIntensity = Mathf.Lerp(0.022f, 0.0f, hpRatio);
                }

                EvaluateSongRules(SongTriggerCondition.EnemyHpAtOrBelowPercent);
                if (IsEnemyAtPurgeMarker)
                {
                    EvaluateSongRules(SongTriggerCondition.OnZeroHpPurgeReady);
                }

                yield return new WaitForSeconds(0.45f);

                string ratingPrefix = string.IsNullOrEmpty(ratingLabel) ? "Owen strikes!" : $"{ratingLabel}";
                int speakerIdx = target != null ? target.slotIndex : 0;

                if (IsEnemyAtPurgeMarker)
                {
                    if (anim != null) anim.IsBobbing = false;
                    string exhaustedBark = TryResolveConditionalDialogue(target, DialogueTriggerType.HealthAtOrBelowPercent);
                    if (string.IsNullOrEmpty(exhaustedBark))
                    {
                        exhaustedBark = activeTutorialDriver != null
                            ? activeTutorialDriver.zeroHpExhaustedLine
                            : (target != null && target.config != null ? target.config.zeroHpExhaustedBark : "");
                    }

                    string msg = $"* {ratingPrefix} Dealt {_lastDamageDealt} damage!{affinityTag}\n* {activeEnemyName} reached 0 HP! [PURGE] or [PURIFY] ready.";
                    if (!string.IsNullOrEmpty(exhaustedBark))
                        SetEnemySpeechForSlot(speakerIdx, exhaustedBark, msg);
                    else
                        _activeDialogueText = msg;
                }
                else
                {
                    string hitBark = "";
                    if (ratingLabel == "CRITICAL!!")
                    {
                        hitBark = TryResolveConditionalDialogue(target, DialogueTriggerType.OnPlayerCriticalStrike);
                    }
                    if (string.IsNullOrEmpty(hitBark))
                    {
                        hitBark = TryResolveConditionalDialogue(target, DialogueTriggerType.HealthAtOrBelowPercent);
                    }
                    if (string.IsNullOrEmpty(hitBark))
                    {
                        hitBark = activeTutorialDriver != null
                            ? activeTutorialDriver.hitReactionLine
                            : (target != null && target.config != null ? target.config.hitReactionBark : "");
                    }

                    string msg = $"* {ratingPrefix} Dealt {_lastDamageDealt} damage!{affinityTag}";
                    if (!string.IsNullOrEmpty(hitBark))
                        SetEnemySpeechForSlot(speakerIdx, hitBark, msg);
                    else
                        _activeDialogueText = msg;
                }
                _menuState = BattleMenuState.ActResponse;
                _inputDebounceTimer = 0.25f;
            }
        }

        private float ComputeTotalEncounterHpRatio()
        {
            if (_enemySlots.Count == 0)
            {
                return Mathf.Clamp01((float)enemyCurrentHp / Mathf.Max(1, enemyMaxHp));
            }
            int curSum = 0;
            int maxSum = 0;
            for (int i = 0; i < _enemySlots.Count; i++)
            {
                if (_enemySlots[i] == null || _enemySlots[i].isResolved) continue;
                curSum += Mathf.Max(0, _enemySlots[i].currentHp);
                maxSum += Mathf.Max(1, _enemySlots[i].maxHp);
            }
            return maxSum > 0 ? Mathf.Clamp01((float)curSum / maxSum) : 0f;
        }

        private List<ModularActOption> GetAvailableActsForSlot(RuntimeEnemySlot slot)
        {
            var result = new List<ModularActOption>();
            if (slot != null && slot.config != null && slot.config.actOptions != null && slot.config.actOptions.Count > 0)
            {
                for (int i = 0; i < slot.config.actOptions.Count; i++)
                {
                    var act = slot.config.actOptions[i];
                    if (act == null) continue;
                    if (!string.IsNullOrEmpty(act.unlockedAfterActName) && !slot.usedActs.Contains(act.unlockedAfterActName.Trim()))
                    {
                        continue; // Locked until prerequisite ACT is used!
                    }
                    result.Add(act);
                }
            }

            if (result.Count == 0 && activeEnemyData != null && activeEnemyData.talkOptions != null && activeEnemyData.talkOptions.Count > 0)
            {
                for (int i = 0; i < activeEnemyData.talkOptions.Count; i++)
                {
                    var t = activeEnemyData.talkOptions[i];
                    if (t == null) continue;
                    int p = t.purifyReward;
                    if (p <= 0 && !string.Equals(t.optionName, "Check", StringComparison.OrdinalIgnoreCase)) p = 50;
                    result.Add(new ModularActOption
                    {
                        actName = t.optionName,
                        bottomBarNarration = t.responseText,
                        purifyGain = string.Equals(t.optionName, "Check", StringComparison.OrdinalIgnoreCase) ? 0 : p
                    });
                }
            }

            if (result.Count == 0)
            {
                result.Add(new ModularActOption
                {
                    actName = "Check",
                    bottomBarNarration = $"{activeEnemyName.ToUpper()} - HP {enemyCurrentHp}/{enemyMaxHp}.",
                    purifyGain = 0
                });
            }
            return result;
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
                var active = GetActiveUnresolvedSlots();
                _menuState = active.Count > 1 ? BattleMenuState.ActTarget : BattleMenuState.CommandSelect;
                return;
            }

            var target = GetTargetedEnemySlot();
            var acts = GetAvailableActsForSlot(target);
            int count = acts.Count;
            _selectedActIndex = Mathf.Clamp(_selectedActIndex, 0, Mathf.Max(0, count - 1));

            if (IsUpDown() || IsLeftDown())
            {
                _selectedActIndex = (_selectedActIndex + count - 1) % count;
            }
            else if (IsDownDown() || IsRightDown())
            {
                _selectedActIndex = (_selectedActIndex + 1) % count;
            }

            if (_inputDebounceTimer <= 0f && IsConfirmDown())
            {
                _inputDebounceTimer = 0.25f;
                var opt = acts[_selectedActIndex];
                int addPurify = opt.purifyGain;

                if (target != null && !string.IsNullOrEmpty(opt.actName))
                {
                    target.usedActs.Add(opt.actName.Trim());
                }

                var affectedSlots = opt.affectsAllEnemies
                    ? GetActiveUnresolvedSlots()
                    : (target != null ? new List<RuntimeEnemySlot> { target } : new List<RuntimeEnemySlot>());

                for (int i = 0; i < affectedSlots.Count; i++)
                {
                    var s = affectedSlots[i];
                    s.currentPurify = Mathf.Clamp(s.currentPurify + addPurify, 0, s.purifyThreshold);
                    s.pendingDamageMultiplier *= Mathf.Clamp(opt.nextTurnDamageMultiplier, 0.1f, 2.0f);
                    s.pendingSpeedMultiplier *= Mathf.Clamp(opt.nextTurnSpeedMultiplier, 0.2f, 2.0f);
                    s.animator?.UpdateStatusPosture(s.IsAtPurgeMarker, s.IsPurifyReady);
                }
                if (affectedSlots.Count == 0)
                {
                    enemyCurrentMercy = Mathf.Clamp(enemyCurrentMercy + addPurify, 0, 100);
                }
                SyncPrimaryLegacyEnemyFields();

                float normalPitch = _activeEncounterConfig != null
                    ? _activeEncounterConfig.bgmStartPitch
                    : (activeEnemyData != null ? activeEnemyData.bgmStartPitch : 1.0f);
                bool autoSlow = _activeEncounterConfig != null
                    ? _activeEncounterConfig.enableAutoLowHpSlowdown
                    : (activeEnemyData == null || activeEnemyData.enableAutoLowHpSlowdown);
                bool wasDamaged = enemyCurrentHp < enemyMaxHp || _targetBgmPitch < normalPitch;

                if (addPurify > 0 && wasDamaged && autoSlow)
                {
                    _targetBgmPitch = normalPitch;
                    _targetBgmDistortion = 0.0f;
                    _targetBgmCutoff = 22000.0f;
                    _flutterIntensity = 0.0f;
                }

                EvaluateSongRules(SongTriggerCondition.OnSpecificActUsed, opt.actName ?? "");
                EvaluateSongRules(SongTriggerCondition.PurifyAtOrAbovePercent);

                string cleanOptResponse = opt.bottomBarNarration ?? "";
                cleanOptResponse = System.Text.RegularExpressions.Regex.Replace(cleanOptResponse, @"\s*\(\+?\d+%\s*(Mercy|Purify)\)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
                cleanOptResponse = cleanOptResponse.Replace("Mercy", "Purify").Replace("mercy", "purify");

                if (activeTutorialDriver != null && activeTutorialDriver.TryHandleActOutcome(this, cleanOptResponse, addPurify))
                {
                    return;
                }

                string tag = addPurify > 0
                    ? $" (Purify +{addPurify})"
                    : (addPurify < 0 ? $" (Purify {addPurify})" : "");

                int speakerIdx = target != null ? target.slotIndex : 0;
                string enemyReply = !string.IsNullOrEmpty(opt.enemySpeechReply)
                    ? opt.enemySpeechReply
                    : TryResolveConditionalDialogue(target, DialogueTriggerType.AfterSpecificAct, actUsed: opt.actName);
                if (string.IsNullOrEmpty(enemyReply))
                {
                    enemyReply = TryResolveConditionalDialogue(target, DialogueTriggerType.PurifyAtOrAbovePercent);
                }

                if (addPurify > 0 && wasDamaged && activeTutorialDriver != null && !string.IsNullOrEmpty(activeTutorialDriver.forgivenessAfterDamageLine))
                {
                    SetEnemySpeechForSlot(
                        speakerIdx,
                        activeTutorialDriver.forgivenessAfterDamageLine,
                        $"* {cleanOptResponse}{tag}\n* {activeEnemyName} forgives you! Music restored.");
                }
                else if (!string.IsNullOrEmpty(enemyReply))
                {
                    SetEnemySpeechForSlot(speakerIdx, enemyReply, $"* {cleanOptResponse}{tag}");
                }
                else
                {
                    _activeDialogueText = $"* {cleanOptResponse}{tag}";
                }
                _menuState = BattleMenuState.ActResponse;
            }
        }

        private List<RuntimeEnemySlot> GetPurifyReadySlots()
        {
            var ready = new List<RuntimeEnemySlot>();
            var active = GetActiveUnresolvedSlots();
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].IsPurifyReady) ready.Add(active[i]);
            }
            return ready;
        }

        private bool IsFleeBlockedByBoss()
        {
            var active = GetActiveUnresolvedSlots();
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].config != null && active[i].config.enemyType != EnemyClassification.BasicEnemy)
                {
                    return true;
                }
            }
            return false;
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
                    var readySlots = GetPurifyReadySlots();
                    if (readySlots.Count > 0 || (_enemySlots.Count == 0 && enemyCurrentMercy >= 100))
                    {
                        StartCoroutine(PurifySequenceRoutine(readySlots));
                    }
                    else
                    {
                        var target = GetTargetedEnemySlot();
                        int curP = target != null ? target.currentPurify : enemyCurrentMercy;
                        int reqP = target != null ? target.purifyThreshold : 100;
                        string tName = target != null ? target.displayName : activeEnemyName;
                        _activeDialogueText = $"* {tName}'s code is still unstable (Purify {curP}/{reqP}).\n* Use ACT to reach {reqP} first!";
                        _menuState = BattleMenuState.ActResponse;
                    }
                }
                else // FLEE
                {
                    if (IsFleeBlockedByBoss())
                    {
                        _activeDialogueText = "* Escape is blocked by an imposing boss presence!";
                    }
                    else
                    {
                        _activeDialogueText = "* Nowhere to flee in this digitized sector.";
                    }
                    _menuState = BattleMenuState.ActResponse;
                }
            }
        }

        private IEnumerator PurifySequenceRoutine(List<RuntimeEnemySlot> slotsToPurify = null)
        {
            _menuState = BattleMenuState.FightAttackAnim;
            EvaluateSongRules(SongTriggerCondition.OnPurifyExecuted);

            if (slotsToPurify == null || slotsToPurify.Count == 0)
            {
                var t = GetTargetedEnemySlot();
                if (t != null) slotsToPurify = new List<RuntimeEnemySlot> { t };
            }

            var primaryPurified = (slotsToPurify != null && slotsToPurify.Count > 0) ? slotsToPurify[0] : null;
            string purifiedNames = primaryPurified != null ? primaryPurified.displayName : activeEnemyName;
            if (slotsToPurify != null && slotsToPurify.Count > 1)
            {
                purifiedNames = $"{slotsToPurify[0].displayName} & allies";
            }

            string bark = (activeTutorialDriver != null && !string.IsNullOrEmpty(activeTutorialDriver.purifyFarewellLine))
                ? activeTutorialDriver.purifyFarewellLine
                : (primaryPurified != null && primaryPurified.config != null && !string.IsNullOrEmpty(primaryPurified.config.purifyFarewellBark)
                    ? primaryPurified.config.purifyFarewellBark
                    : "I can see clearly now... Thank you, Owen.");

            int speakerIdx = primaryPurified != null ? primaryPurified.slotIndex : 0;
            SetEnemySpeechForSlot(speakerIdx, bark, $"* {purifiedNames.ToUpper()}'s code is stabilizing...");
            _inputDebounceTimer = 0.35f;

            while (true)
            {
                if (_inputDebounceTimer <= 0f && IsConfirmDown())
                {
                    if (!SkipSpeechTyping()) break;
                }
                yield return null;
            }

            HideGolferSpeech();
            _activeDialogueText = $"* {purifiedNames.ToUpper()}'s corrupted routines dissolve into white light...";

            if (slotsToPurify != null && slotsToPurify.Count > 0)
            {
                int remainingExplosions = 0;
                for (int i = 0; i < slotsToPurify.Count; i++)
                {
                    var s = slotsToPurify[i];
                    if (s.config != null)
                    {
                        _accumulatedExpReward += s.config.expOnPurify;
                        _accumulatedDollarReward += s.config.digiDollarsReward;
                    }
                    else
                    {
                        _accumulatedExpReward += 10;
                        _accumulatedDollarReward += 5;
                    }

                    if (s.animator != null)
                    {
                        remainingExplosions++;
                        s.animator.PlayPurifyWhiteLightExplosion(1.8f, () => remainingExplosions--);
                    }
                }
                while (remainingExplosions > 0) yield return null;

                for (int i = 0; i < slotsToPurify.Count; i++)
                {
                    slotsToPurify[i].isResolved = true;
                    slotsToPurify[i].wasPurified = true;
                    if (slotsToPurify[i].animator != null) slotsToPurify[i].animator.gameObject.SetActive(false);
                }
            }
            else
            {
                bool explosionDone = false;
                if (enemyAnimator != null)
                    enemyAnimator.PlayPurifyWhiteLightExplosion(1.8f, () => explosionDone = true);
                else
                    explosionDone = true;
                while (!explosionDone) yield return null;
            }

            var stillFighting = GetActiveUnresolvedSlots();
            if (stillFighting.Count > 0)
            {
                _selectedTargetSlotCursor = 0;
                SyncPrimaryLegacyEnemyFields();
                var survivor = stillFighting[0];
                string allyPurifiedBark = TryResolveConditionalDialogue(survivor, DialogueTriggerType.WhenAllyPurified);
                if (!string.IsNullOrEmpty(allyPurifiedBark))
                {
                    SetEnemySpeechForSlot(survivor.slotIndex, allyPurifiedBark, $"* {purifiedNames.ToUpper()} was PURIFIED! {stillFighting.Count} enemy remaining.");
                }
                else
                {
                    _activeDialogueText = $"* {purifiedNames.ToUpper()} was PURIFIED! {stillFighting.Count} enemy remaining.";
                }
                _menuState = BattleMenuState.ActResponse;
                _inputDebounceTimer = 0.25f;
            }
            else
            {
                _isPurifiedVictory = !_anyEnemyWasPurged;
                _menuState = BattleMenuState.Victory;
                _inputDebounceTimer = 0.35f;
                int finalExp = _accumulatedExpReward > 0 ? _accumulatedExpReward : 10;
                int finalDol = _accumulatedDollarReward > 0 ? _accumulatedDollarReward : 5;
                _activeDialogueText = $"* YOU WON! {purifiedNames.ToUpper()} was PURIFIED!\n* Corrupted routines dissolved peacefully.\n* Got {finalExp} EXP and {finalDol} D$.";
            }
        }

        private CombinedAttackRule TrySelectCombinedAttackForTurn()
        {
            if (_activeEncounterConfig == null || _activeEncounterConfig.combinedAttacks == null) return null;

            for (int i = 0; i < _activeEncounterConfig.combinedAttacks.Count; i++)
            {
                var rule = _activeEncounterConfig.combinedAttacks[i];
                if (rule == null || rule.combinedAttack == null) continue;
                if (rule.requiredAliveSlotIndices == null || rule.requiredAliveSlotIndices.Count < 2) continue;

                bool allPartnersAlive = true;
                for (int r = 0; r < rule.requiredAliveSlotIndices.Count; r++)
                {
                    int idx = rule.requiredAliveSlotIndices[r];
                    if (idx < 0 || idx >= _enemySlots.Count || _enemySlots[idx].isResolved || _enemySlots[idx].currentHp <= 0)
                    {
                        allPartnersAlive = false;
                        break;
                    }
                }
                if (!allPartnersAlive) continue;

                bool scheduleMatch = (rule.triggerEveryNTurns > 0 && (_currentTurnNumber % rule.triggerEveryNTurns) == 0);
                bool chanceMatch = (rule.triggerEveryNTurns <= 0 && UnityEngine.Random.value <= rule.randomTriggerChance);

                if (scheduleMatch || chanceMatch)
                {
                    return rule;
                }
            }
            return null;
        }

        private void SelectNextAttackForSlot(RuntimeEnemySlot slot)
        {
            slot.chosenAttack = null;
            slot.chosenAttackIndex = -1;
            if (slot.config == null || slot.config.attackPatterns == null || slot.config.attackPatterns.Count == 0)
            {
                slot.chosenAttack = EnemyAttackSlot.CreateGolferFairwayRainPreset();
                slot.chosenAttackIndex = 0;
                return;
            }

            float hpPct = slot.HpPercent;
            var unlockedIndices = new List<int>();
            for (int i = 0; i < slot.config.attackPatterns.Count; i++)
            {
                var atk = slot.config.attackPatterns[i];
                if (atk == null) continue;
                if (hpPct <= atk.unlockAtOrBelowHpPercent + 0.01f)
                {
                    unlockedIndices.Add(i);
                }
            }
            if (unlockedIndices.Count == 0) unlockedIndices.Add(0);

            int pickedIdx = unlockedIndices[0];
            switch (slot.config.attackOrder)
            {
                case AttackOrderMode.SequentialLoop:
                    pickedIdx = unlockedIndices[slot.sequentialAttackCursor % unlockedIndices.Count];
                    slot.sequentialAttackCursor++;
                    break;

                case AttackOrderMode.RandomEachTurn:
                    pickedIdx = unlockedIndices[UnityEngine.Random.Range(0, unlockedIndices.Count)];
                    break;

                case AttackOrderMode.HpPhasePriority:
                    float lowestThreshold = 999f;
                    for (int u = 0; u < unlockedIndices.Count; u++)
                    {
                        int idx = unlockedIndices[u];
                        float thresh = slot.config.attackPatterns[idx].unlockAtOrBelowHpPercent;
                        if (thresh < lowestThreshold)
                        {
                            lowestThreshold = thresh;
                            pickedIdx = idx;
                        }
                    }
                    break;
            }

            slot.chosenAttackIndex = pickedIdx;
            slot.chosenAttack = slot.config.attackPatterns[pickedIdx];
        }

        private void StartEnemyAttackTelegraph()
        {
            _menuState = BattleMenuState.EnemySpeech;
            _enemySpeechWaitTimer = 2.0f;
            _inputDebounceTimer = 0.20f;
            _pendingCombinedAttack = null;

            var active = GetActiveUnresolvedSlots();
            var capableAttackers = new List<RuntimeEnemySlot>();
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].currentHp > active[i].purgeMarkerHp) capableAttackers.Add(active[i]);
            }

            if (capableAttackers.Count == 0)
            {
                string helplessBark = (activeTutorialDriver != null && !string.IsNullOrEmpty(activeTutorialDriver.zeroHpCannotAttackLine))
                    ? activeTutorialDriver.zeroHpCannotAttackLine
                    : "...";
                int spk = active.Count > 0 ? active[0].slotIndex : 0;
                SetEnemySpeechForSlot(spk, helplessBark, $"* {BuildActivePartyNamesSummary()} too weak to attack!");
                return;
            }

            // 1. Check for Combined Multi-Enemy Synergy Attack first!
            _pendingCombinedAttack = TrySelectCombinedAttackForTurn();
            if (_pendingCombinedAttack != null)
            {
                int leadSlot = _pendingCombinedAttack.requiredAliveSlotIndices[0];
                string comboBark = (_pendingCombinedAttack.participantTelegraphBarks != null && _pendingCombinedAttack.participantTelegraphBarks.Count > 0)
                    ? _pendingCombinedAttack.participantTelegraphBarks[UnityEngine.Random.Range(0, _pendingCombinedAttack.participantTelegraphBarks.Count)]
                    : $"{_pendingCombinedAttack.comboName.ToUpper()}!";
                SetEnemySpeechForSlot(leadSlot, comboBark, $"* COMBINED ATTACK: {_pendingCombinedAttack.comboName}!");
                return;
            }

            // 2. Otherwise pick each living enemy's solo attack & resolve highest-priority Conditional Dialogue!
            for (int i = 0; i < capableAttackers.Count; i++)
            {
                SelectNextAttackForSlot(capableAttackers[i]);
            }

            var speaker = capableAttackers[UnityEngine.Random.Range(0, capableAttackers.Count)];
            string chosenBark = TryResolveConditionalDialogue(speaker, DialogueTriggerType.WhenUsingAttackIndex, chosenAttackIdx: speaker.chosenAttackIndex);
            if (string.IsNullOrEmpty(chosenBark))
                chosenBark = TryResolveConditionalDialogue(speaker, DialogueTriggerType.TurnSequence);
            if (string.IsNullOrEmpty(chosenBark))
                chosenBark = TryResolveConditionalDialogue(speaker, DialogueTriggerType.HealthAtOrBelowPercent);
            if (string.IsNullOrEmpty(chosenBark))
                chosenBark = TryResolveConditionalDialogue(speaker, DialogueTriggerType.PurifyAtOrAbovePercent);

            if (string.IsNullOrEmpty(chosenBark))
            {
                if (speaker.config != null && speaker.config.silentEnemyNoBarks)
                {
                    chosenBark = "";
                }
                else if (speaker.config != null && speaker.config.defaultBattleBarks != null && speaker.config.defaultBattleBarks.Count > 0)
                {
                    chosenBark = speaker.config.defaultBattleBarks[UnityEngine.Random.Range(0, speaker.config.defaultBattleBarks.Count)];
                }
                else if (activeEnemyData != null && activeEnemyData.inBattleBarks != null && activeEnemyData.inBattleBarks.Length > 0)
                {
                    chosenBark = activeEnemyData.inBattleBarks[UnityEngine.Random.Range(0, activeEnemyData.inBattleBarks.Length)];
                }
            }

            string prompt = capableAttackers.Count > 1
                ? $"* {BuildActivePartyNamesSummary()} attack!"
                : $"* {speaker.displayName} attacks!";

            if (!string.IsNullOrEmpty(chosenBark))
            {
                _speechBarkText = chosenBark;
                SetEnemySpeechForSlot(speaker.slotIndex, chosenBark, prompt);
            }
            else
            {
                _activeDialogueText = prompt;
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

            var active = GetActiveUnresolvedSlots();
            var capableAttackers = new List<RuntimeEnemySlot>();
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].currentHp > active[i].purgeMarkerHp) capableAttackers.Add(active[i]);
            }

            Vector2 turnBoxOffset = EffectiveDodgeBoxOffset;
            Vector2 turnBoxSize = EffectiveDodgeBoxSize;

            var waves = new List<ActiveAttackWaveContext>();
            if (_pendingCombinedAttack != null && _pendingCombinedAttack.combinedAttack != null)
            {
                var cAtk = _pendingCombinedAttack.combinedAttack;
                if (cAtk.overrideDodgeBoxForAttack)
                {
                    turnBoxOffset = cAtk.attackDodgeBoxOffset;
                    turnBoxSize = cAtk.attackDodgeBoxSize;
                }
                waves.Add(new ActiveAttackWaveContext
                {
                    slot = cAtk,
                    runtimeDamageMultiplier = 1f,
                    runtimeSpeedMultiplier = 1f
                });
            }
            else
            {
                for (int i = 0; i < capableAttackers.Count; i++)
                {
                    var attacker = capableAttackers[i];
                    if (attacker.chosenAttack == null) SelectNextAttackForSlot(attacker);
                    if (attacker.chosenAttack.overrideDodgeBoxForAttack)
                    {
                        turnBoxOffset = attacker.chosenAttack.attackDodgeBoxOffset;
                        turnBoxSize = attacker.chosenAttack.attackDodgeBoxSize;
                    }
                    waves.Add(new ActiveAttackWaveContext
                    {
                        slot = attacker.chosenAttack,
                        runtimeDamageMultiplier = attacker.pendingDamageMultiplier,
                        runtimeSpeedMultiplier = attacker.pendingSpeedMultiplier
                    });
                    attacker.pendingDamageMultiplier = 1f;
                    attacker.pendingSpeedMultiplier = 1f;
                }
            }

            Vector3 activeBoxWorldCenter = new Vector3(stageCenter.x + turnBoxOffset.x, stageCenter.y + turnBoxOffset.y, 0f);
            if (dodgeBox != null)
            {
                dodgeBox.transform.position = activeBoxWorldCenter;
                dodgeBox.SetCenterAndSize(activeBoxWorldCenter, turnBoxSize);
            }

            if (capableAttackers.Count == 0 && (_enemySlots.Count > 0 || enemyCurrentHp <= purgeMarkerHp))
            {
                _activeDialogueText = $"* But {BuildActivePartyNamesSummary()} cannot muster an attack...";
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
                _activeDialogueText = "* INCOMING ATTACK! (WASD / Arrows to Move)";
                if (dodgeBox != null)
                {
                    if (waves.Count == 0)
                    {
                        waves.Add(new ActiveAttackWaveContext
                        {
                            slot = EnemyAttackSlot.CreateGolferFairwayRainPreset(),
                            runtimeDamageMultiplier = 1f,
                            runtimeSpeedMultiplier = 1f
                        });
                    }

                    _activeWaveList = waves;
                    ApplyDodgePhaseDimming(waves, true);
                    dodgeBox.StartModularDodgePhase(
                        waves,
                        onRuleContact: (val, rule) =>
                        {
                            if (rule == HazardMovementRule.GreenHealOrb)
                            {
                                owenCurrentHp = Mathf.Min(owenMaxHp, owenCurrentHp + Mathf.Max(1, val));
                            }
                            else if (rule == HazardMovementRule.GoldPurifyOrb)
                            {
                                var t = GetTargetedEnemySlot();
                                if (t != null)
                                {
                                    t.currentPurify = Mathf.Clamp(t.currentPurify + Mathf.Max(1, val), 0, t.purifyThreshold);
                                    t.animator?.UpdateStatusPosture(t.IsAtPurgeMarker, t.IsPurifyReady);
                                    SyncPrimaryLegacyEnemyFields();
                                }
                            }
                            else
                            {
                                owenCurrentHp = Mathf.Max(0, owenCurrentHp - Mathf.Max(0, val));
                                owenAnimator?.PlayHurt(0.3f);
                                EvaluateSongRules(SongTriggerCondition.OwenHpAtOrBelowPercent);
                            }
                        },
                        onPhaseEnd: () => dodgeDone = true);
                }
                else
                {
                    yield return new WaitForSeconds(3.0f);
                    dodgeDone = true;
                }
            }

            while (!dodgeDone) yield return null;

            // Restore default stage Dodge Box framing after attack ends
            ApplyDodgePhaseDimming(waves, false);
            _activeWaveList = null;
            RecalculateStagePositions();

            if (owenCurrentHp <= 0)
            {
                ExecuteGameOver();
            }
            else
            {
                bool allExhausted = true;
                var rem = GetActiveUnresolvedSlots();
                for (int i = 0; i < rem.Count; i++)
                {
                    if (!rem[i].IsAtPurgeMarker) { allExhausted = false; break; }
                }
                if (rem.Count == 0) allExhausted = IsEnemyAtPurgeMarker;

                StartNewPlayerTurn(allExhausted
                    ? $"* {BuildActivePartyNamesSummary()} helpless. Choose fate: [PURGE] or [PURIFY]."
                    : $"* {BuildActivePartyNamesSummary()} waiting for your move.");
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
            isBattleActive = false;
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

        #region Custom 640x480 Angled Cyber Battle HUD
        private void OnGUI()
        {
            if (!isBattleActive) return;
            if (_menuState == BattleMenuState.ExitingToOverworld || _menuState == BattleMenuState.GameOver) return;
            if (DigiPurity.World.ScreenFader.Instance != null && DigiPurity.World.ScreenFader.Instance.CurrentAlpha > 0.05f) return;
            GUI.depth = 100;

            EnsureCustomUITextures();

            Font font = DigiPurity.Core.GameFonts.MainFont;
            if (font != null) GUI.skin.font = font;

            Rect rect43 = Get43ScreenPixelRect();

            // Draw solid black letterbox / pillarbox bars outside the 4:3 active frame
            if (rect43.x > 0.5f)
            {
                GUI.color = Color.black;
                SafeDrawTexture(new Rect(0f, 0f, rect43.x, Screen.height), Texture2D.whiteTexture);
                SafeDrawTexture(new Rect(rect43.x + rect43.width, 0f, rect43.x + 4f, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            if (rect43.y > 0.5f)
            {
                GUI.color = Color.black;
                SafeDrawTexture(new Rect(0f, 0f, Screen.width, rect43.y), Texture2D.whiteTexture);
                SafeDrawTexture(new Rect(0f, rect43.y + rect43.height, Screen.width, rect43.y + 4f), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            // Single-point 640x480 virtual matrix transform matching LibreSprite pixel coordinates 1:1
            Matrix4x4 prevMatrix = GUI.matrix;
            float scaleX = rect43.width / 640f;
            float scaleY = rect43.height / 480f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(rect43.x, rect43.y, 0f), Quaternion.identity, new Vector3(scaleX, scaleY, 1f));

            GUI.BeginGroup(new Rect(0f, 0f, 640f, 480f));
            DrawCustomCyberHUD(font);

            // Overhead speech bubble attached directly to enemy sprite edge vertex at an offsetted distance
            if (_isSpeechBubbleVisible && !string.IsNullOrEmpty(_speechVisibleText))
            {
                DrawEnemySpeechBubbleInVirtualSpace(font);
            }
            GUI.EndGroup();

            GUI.matrix = prevMatrix;
            GUI.color = Color.white;
        }

        /// <summary>
        /// Draws the authentic Deltarune stepped-corner white speech bubble to the LEFT of the speaking enemy
        /// (matching Deltarune_Enemy_Speech_Reference.png: pure white stepped pixel corners + right-pointing pixel tail).
        /// </summary>
        private void DrawEnemySpeechBubbleInVirtualSpace(Font hudFont)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            RuntimeEnemySlot speakerSlot = (_speakingSlotIndex >= 0 && _speakingSlotIndex < _enemySlots.Count)
                ? _enemySlots[_speakingSlotIndex]
                : GetTargetedEnemySlot();
            EnemyBattleAnimator speakerAnim = (speakerSlot != null && speakerSlot.animator != null)
                ? speakerSlot.animator
                : enemyAnimator;

            // Robust anchor calculation immune to transparent canvas padding:
            // Transform position is the enemy's center origin on the battle stage.
            float rootX = (speakerAnim != null) ? speakerAnim.transform.position.x : (stageCenter.x + enemyStageOffset.x);
            float rootY = (speakerAnim != null) ? speakerAnim.transform.position.y : (stageCenter.y + enemyStageOffset.y);

            Vector2 customOffset = (speakerSlot != null && speakerSlot.config != null)
                ? speakerSlot.config.speechBubbleOffset
                : Vector2.zero;

            float anchorWorldX;
            float anchorWorldY;

            if (customOffset.sqrMagnitude > 0.001f)
            {
                anchorWorldX = rootX + customOffset.x;
                anchorWorldY = rootY + customOffset.y;
            }
            else
            {
                // Left edge of enemy's upper body / head
                // For scale ~4.35 characters, face height is rootY + ~3.4f, half-width is ~1.76f.
                float leftEdgeOffset = 1.76f;
                float faceHeight = 3.40f;

                if (speakerAnim != null)
                {
                    var sr = speakerAnim.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null && sr.sprite != null && sr.bounds.size.x > 0.05f)
                    {
                        // If sr.bounds.min.x is reasonable (between 0.8 and 1.9 units from root), use it.
                        // If sr.bounds is bloated by transparent canvas padding (e.g. 256x256 texture),
                        // clamping to 1.85f keeps the bubble anchored directly on his cap/face!
                        float rawOffset = rootX - sr.bounds.min.x;
                        leftEdgeOffset = Mathf.Clamp(rawOffset, 0.8f, 1.85f);

                        float rawHeight = sr.bounds.max.y - rootY - 0.70f;
                        faceHeight = Mathf.Clamp(rawHeight, 2.2f, 3.8f);
                    }
                }
                anchorWorldX = rootX - leftEdgeOffset;
                anchorWorldY = rootY + faceHeight;
            }

            Vector3 spriteVertex = new Vector3(anchorWorldX, anchorWorldY, 0f);

            // Convert world vertex to 640x480 virtual canvas coordinates
            Vector3 vp = cam.WorldToViewportPoint(spriteVertex);
            float anchorX = vp.x * 640f;
            float anchorY = (1f - vp.y) * 480f;

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                wordWrap = true
            };
            if (hudFont != null) style.font = hudFont;
            style.normal.textColor = Color.black;

            Vector2 cfgSize = (speakerSlot != null && speakerSlot.config != null && speakerSlot.config.speechBubbleSize.x > 40f)
                ? speakerSlot.config.speechBubbleSize
                : new Vector2(175f, 62f);

            float bubbleW = cfgSize.x;
            float pad = 10f;
            float calcH = style.CalcHeight(new GUIContent(_speechFullText), bubbleW - (pad * 2f));
            float bubbleH = Mathf.Clamp(calcH + (pad * 1.8f), cfgSize.y, 140f);

            // Attach bubble at an offsetted distance (8px to the left of the sprite edge vertex)
            float bubbleX = Mathf.Clamp(anchorX - bubbleW - 8f, 15f, 640f - bubbleW - 15f);
            float bubbleY = Mathf.Clamp(anchorY - (bubbleH * 0.45f), 12f, 260f);

            Rect bubble = new Rect(bubbleX, bubbleY, bubbleW, bubbleH);

            // Draw white stepped box
            DrawSteppedWhiteSpeechBox(bubble, 3f);

            // Tail connects directly from right edge of bubble to the sprite vertex!
            float tailY = Mathf.Clamp(anchorY, bubble.y + 12f, bubble.yMax - 12f);
            DrawRightPointingSteppedTail(bubble.xMax, tailY, 3f);

            // Text
            Rect textRect = new Rect(bubbleX + pad, bubbleY + pad - 2f, bubbleW - (pad * 2f), bubbleH - (pad * 2f) + 4f);
            GUI.color = Color.black;
            GUI.Label(textRect, _speechVisibleText, style);
            GUI.color = Color.white;
        }

        private static void DrawSteppedWhiteSpeechBox(Rect r, float stepPx)
        {
            float s1 = stepPx;
            float s2 = stepPx * 2f;
            GUI.color = Color.white;

            // Center full-width body
            SafeDrawTexture(new Rect(r.x, r.y + s2, r.width, Mathf.Max(2f, r.height - (s2 * 2f))), Texture2D.whiteTexture);
            // Mid top & bottom pixel steps
            SafeDrawTexture(new Rect(r.x + s1, r.y + s1, Mathf.Max(2f, r.width - (s1 * 2f)), s1), Texture2D.whiteTexture);
            SafeDrawTexture(new Rect(r.x + s1, r.yMax - s2, Mathf.Max(2f, r.width - (s1 * 2f)), s1), Texture2D.whiteTexture);
            // Outer top & bottom pixel caps
            SafeDrawTexture(new Rect(r.x + s2, r.y, Mathf.Max(2f, r.width - (s2 * 2f)), s1), Texture2D.whiteTexture);
            SafeDrawTexture(new Rect(r.x + s2, r.yMax - s1, Mathf.Max(2f, r.width - (s2 * 2f)), s1), Texture2D.whiteTexture);
        }

        private static void DrawRightPointingSteppedTail(float rightEdgeX, float centerY, float stepPx)
        {
            GUI.color = Color.white;
            SafeDrawTexture(new Rect(rightEdgeX - 1f, centerY - (stepPx * 2f), stepPx + 1f, stepPx * 4f), Texture2D.whiteTexture);
            SafeDrawTexture(new Rect(rightEdgeX + stepPx, centerY - (stepPx * 1.2f), stepPx, stepPx * 2.4f), Texture2D.whiteTexture);
            SafeDrawTexture(new Rect(rightEdgeX + (stepPx * 2f), centerY - (stepPx * 0.5f), stepPx, stepPx), Texture2D.whiteTexture);
        }

        private void DrawPulsingSoulHeart(float centerX, float centerY, float baseSize = 18f)
        {
            float pulseScale = 1.0f + (0.12f * Mathf.Sin(Time.time * 6.0f));
            float size = baseSize * pulseScale;
            Rect r = new Rect(centerX - (size * 0.5f), centerY - (size * 0.5f), size, size);

            GUI.color = Color.white;
            if (_uiSoulHeartTex != null)
            {
                SafeDrawTexture(r, _uiSoulHeartTex, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.color = new Color(0.15f, 0.9f, 1.0f);
                SafeDrawTexture(r, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }

        private void DrawCustomCyberHUD(Font hudFont)
        {
            bool isDodge = (_menuState == BattleMenuState.DodgeBoxPhase);
            float targetUiAlpha = 1f;
            if (isDodge && _activeWaveList != null && _activeWaveList.Count > 0 && _activeWaveList[0].slot != null && _activeWaveList[0].slot.darkenUIButtons)
            {
                targetUiAlpha = Mathf.Clamp01(_activeWaveList[0].slot.uiDimAlpha);
            }
            // Smooth stylistic alpha interpolation
            _currentUiAlpha = Mathf.MoveTowards(_currentUiAlpha, targetUiAlpha, Time.unscaledDeltaTime * 4.5f);
            float uiAlpha = _currentUiAlpha;
            Color prevGuiColor = GUI.color;

            // 1. LEFT-SIDE ANGLED COMMAND BUTTONS (PURGE, ACT, BAG, PURIFY)
            // Draw unselected buttons first, then draw the highlighted button on top so expansion never clips behind neighbors
            if (isDodge) GUI.color = new Color(uiAlpha, uiAlpha, uiAlpha, 1f);
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < 4; i++)
                {
                    bool isPrimary = (i == _selectedCommandIndex);
                    if ((pass == 0 && isPrimary) || (pass == 1 && !isPrimary)) continue;

                    DrawSingleSideButton(i, hudFont);
                }
            }

            // 2. BOTTOM ANGLED CYBER BAR SHELL + OWEN PORTRAIT (1:1 LibreSprite coordinates + Inspector-editable bottomBarPixelOffset)
            GUI.BeginGroup(new Rect(bottomBarPixelOffset.x, bottomBarPixelOffset.y, 640f, 480f));
            GUI.color = isDodge ? new Color(uiAlpha, uiAlpha, uiAlpha, 1f) : Color.white;
            if (_uiBottomFrameTex != null)
            {
                SafeDrawTexture(new Rect(193f, 303f, 447f, 177f), _uiBottomFrameTex);
            }
            else
            {
                DrawBorderedBox(new Rect(200f, 338f, 435f, 120f), new Color(0.12f, 0.12f, 0.20f), Color.white, 3f);
            }

            if (_uiOwenHeadTex != null)
            {
                SafeDrawTexture(new Rect(541f, 366f, 66f, 56f), _uiOwenHeadTex);
            }
            else if (_owenPortraitTex != null)
            {
                SafeDrawTexture(new Rect(546f, 368f, 54f, 54f), _owenPortraitTex, ScaleMode.ScaleToFit);
            }

            // 3. DYNAMIC BOTTOM BAR CONTENT:
            // Default: Owen's angled HP bar + stylized "OWEN" nametag
            // Selected Option: Replaces interior with Submenu Options or Undertale/Deltarune Accuracy Attack Bar!
            switch (_menuState)
            {
                case BattleMenuState.CommandSelect:
                case BattleMenuState.TutorialNarrative:
                case BattleMenuState.EnemySpeech:
                case BattleMenuState.DodgeBoxPhase:
                    DrawOwenStatusBarInterior(hudFont);
                    break;

                case BattleMenuState.FightTarget:
                    DrawFightTargetInterior(hudFont);
                    break;

                case BattleMenuState.AttackTiming:
                    DrawAttackTimingBarInterior(hudFont);
                    break;

                case BattleMenuState.ActTarget:
                    DrawActTargetInterior(hudFont);
                    break;

                case BattleMenuState.ActSelect:
                    DrawActSelectInterior(hudFont);
                    break;

                case BattleMenuState.ItemSelect:
                    DrawBagSelectInterior(hudFont);
                    break;

                case BattleMenuState.MercySelect:
                    DrawPurifySelectInterior(hudFont);
                    break;

                case BattleMenuState.FightAttackAnim:
                case BattleMenuState.ActResponse:
                case BattleMenuState.Victory:
                    DrawNarrativeMessageInterior(hudFont);
                    break;
            }
            GUI.EndGroup();

            GUI.color = Color.white;
        }

        private void DrawSingleSideButton(int index, Font hudFont)
        {
            // Neighbor vertical shift driven by all 4 buttons' active bounce springs
            float shiftY = 0f;
            for (int k = 0; k < 4; k++)
            {
                if (k == index) continue;
                float push = 14.0f * _btnSpringPos[k];
                shiftY += (index < k) ? -push : push;
            }

            // Button's own bounce scale (neutral = 1.0, expanded = ~1.25 with slight bounce overshoot, settle bounce on shrink)
            float scale = Mathf.Clamp(1.0f + (0.25f * _btnSpringPos[index]), 0.92f, 1.38f);
            float w = BaseButtonW * scale;
            float h = BaseButtonH * scale;
            float y = BaseButtonY[index] - ((h - BaseButtonH) * 0.5f) + shiftY;

            Rect btnRect = new Rect(0f, y, w, h);

            Texture2D tex = _uiBtnTextures[index];
            if (tex != null)
            {
                // Glow around the sprite itself when selected per user mandate (conforms to slanted pixel boundary)
                if (_menuState == BattleMenuState.CommandSelect && index == _selectedCommandIndex)
                {
                    float pulse = Mathf.PingPong(Time.time * 2.5f, 0.25f) + 0.35f; // soft 0.35..0.60 glow
                    GUI.color = new Color(0.9f, 0.96f, 1f, pulse);
                    // 4 single-pixel offsets produce an exact glowing contour around the sprite silhouette
                    SafeDrawTexture(new Rect(btnRect.x - 2f, btnRect.y, btnRect.width, btnRect.height), tex);
                    SafeDrawTexture(new Rect(btnRect.x + 2f, btnRect.y, btnRect.width, btnRect.height), tex);
                    SafeDrawTexture(new Rect(btnRect.x, btnRect.y - 2f, btnRect.width, btnRect.height), tex);
                    SafeDrawTexture(new Rect(btnRect.x, btnRect.y + 2f, btnRect.width, btnRect.height), tex);
                }

                GUI.color = Color.white;
                SafeDrawTexture(btnRect, tex);
            }
            else
            {
                DrawBorderedBox(new Rect(0f, y + 15f, w, h - 30f), new Color(0.12f, 0.12f, 0.2f), Color.white, 2f);
                var fallbackStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Bold };
                if (hudFont != null) fallbackStyle.font = hudFont;
                GUI.Label(btnRect, _commandNames[index], fallbackStyle);
            }
        }

        private void DrawOwenStatusBarInterior(Font hudFont)
        {
            GUI.color = Color.white;

            // Angled HP Bar Outline
            if (_uiHpFrameTex != null)
            {
                SafeDrawTexture(new Rect(207f, 352f, 194f, 95f), _uiHpFrameTex);
            }

            // Angled Green Gradient HP Fill clipped horizontally by Owen's current HP ratio
            float hpRatio = Mathf.Clamp01((float)owenCurrentHp / Mathf.Max(1, owenMaxHp));
            if (_uiHpFillTex != null && hpRatio > 0.001f)
            {
                GUI.BeginGroup(new Rect(211f, 355f, 187f * hpRatio, 88f));
                SafeDrawTexture(new Rect(0f, 0f, 187f, 88f), _uiHpFillTex);
                GUI.EndGroup();
            }

            // Owen HP bar digits omitted per user design

            // Stylized Pixel "OWEN" Nametag
            GUI.color = Color.white;
            if (_uiOwenNameTex != null)
            {
                SafeDrawTexture(new Rect(408f, 360f, 120f, 79f), _uiOwenNameTex);
            }

            // Subtle non-intrusive advance hint above bar when Golfer's speech bubble is waiting for input
            if (_menuState == BattleMenuState.TutorialNarrative || _menuState == BattleMenuState.EnemySpeech)
            {
                float pulse = Mathf.PingPong(Time.time * 2.2f, 0.35f) + 0.65f;
                var hintStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleRight,
                    fontSize = 11,
                    fontStyle = FontStyle.Bold
                };
                if (hudFont != null) hintStyle.font = hudFont;
                GUI.color = new Color(1f, 0.95f, 0.25f, pulse);
                string hint = (_menuState == BattleMenuState.EnemySpeech && enemyCurrentHp > 0)
                    ? "[Z / ENTER] Dodge!"
                    : "[Z / ENTER] Next";
                GUI.Label(new Rect(350f, 314f, 265f, 20f), hint, hintStyle);
                GUI.color = Color.white;
            }
        }

        private void DrawFightTargetInterior(Font hudFont)
        {
            var active = GetActiveUnresolvedSlots();
            if (active.Count > 1)
            {
                DrawMultiEnemyTargetListInterior(hudFont, active, isFightMenu: true);
                return;
            }

            Rect tray = new Rect(242f, 350f, 282f, 96f);
            float pulse = Mathf.PingPong(Time.time * 2.2f, 0.35f) + 0.65f;

            var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
            var smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold };
            if (hudFont != null) { titleStyle.font = hudFont; smallStyle.font = hudFont; }

            var single = active.Count == 1 ? active[0] : null;
            bool isSparable = single != null ? single.IsPurifyReady : (enemyCurrentMercy >= 100);
            bool canPurge = single != null ? single.IsAtPurgeMarker : IsEnemyAtPurgeMarker;
            string dispName = single != null ? single.displayName : activeEnemyName;
            int curP = single != null ? Mathf.RoundToInt(single.PurifyPercent) : enemyCurrentMercy;

            DrawPulsingSoulHeart(tray.x + 10f, tray.y + 13f, 16f);
            GUI.color = isSparable ? new Color(1f, 0.92f, 0.15f) : (canPurge ? new Color(1f, 0.35f, 0.15f) : Color.white);
            GUI.Label(new Rect(tray.x + 24f, tray.y + 3f, 160f, 22f), dispName.ToUpper(), titleStyle);

            float enemyHpRatio = single != null
                ? Mathf.Clamp01((float)single.currentHp / Mathf.Max(1, single.maxHp))
                : Mathf.Clamp01((float)enemyCurrentHp / Mathf.Max(1, enemyMaxHp));
            GUI.color = new Color(0.85f, 0.85f, 0.85f);
            GUI.Label(new Rect(tray.x + 24f, tray.y + 28f, 36f, 18f), "HP", smallStyle);
            Rect eHpBg = new Rect(tray.x + 58f, tray.y + 31f, 72f, 11f);
            DrawBorderedBox(eHpBg, new Color(0.3f, 0.08f, 0.08f), Color.white, 1f);
            GUI.color = canPurge ? new Color(1f, 0.25f, 0.1f) : new Color(0.15f, 0.95f, 0.4f);
            SafeDrawTexture(new Rect(eHpBg.x + 1f, eHpBg.y + 1f, (eHpBg.width - 2f) * enemyHpRatio, eHpBg.height - 2f), Texture2D.whiteTexture);

            GUI.color = canPurge ? new Color(1f, 0.2f, 0.1f, pulse) : new Color(1f, 0.65f, 0.15f, 0.9f);
            SafeDrawTexture(new Rect(eHpBg.x - 1f, eHpBg.y - 3f, 2f, eHpBg.height + 6f), Texture2D.whiteTexture);

            float purifyRatio = Mathf.Clamp01(curP / 100f);
            GUI.color = isSparable ? new Color(1f, 0.92f, 0.15f) : new Color(0.95f, 0.7f, 0.15f);
            GUI.Label(new Rect(tray.x + 138f, tray.y + 28f, 66f, 18f), "PURIFY", smallStyle);
            Rect purBg = new Rect(tray.x + 206f, tray.y + 31f, 50f, 11f);
            DrawBorderedBox(purBg, new Color(0.22f, 0.16f, 0.05f), Color.white, 1f);
            GUI.color = isSparable ? new Color(1f, 0.95f, 0.15f) : new Color(1f, 0.65f, 0.1f);
            SafeDrawTexture(new Rect(purBg.x + 1f, purBg.y + 1f, (purBg.width - 2f) * purifyRatio, purBg.height - 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(tray.x + 258f, tray.y + 28f, 40f, 18f), $"{curP}%", smallStyle);

            if (canPurge)
            {
                GUI.color = new Color(1f, 0.35f, 0.15f);
                GUI.Label(new Rect(tray.x + 24f, tray.y + 52f, tray.width - 28f, 20f), "PURGE READY [0 HP DELETION]", smallStyle);
                GUI.color = new Color(1f, 0.95f, 0.2f, pulse);
                GUI.Label(new Rect(tray.x + 24f, tray.y + 73f, tray.width - 28f, 18f), "[Z] Execute PURGE   [X] Back", smallStyle);
            }
            else
            {
                GUI.color = new Color(0.85f, 0.92f, 1f);
                GUI.Label(new Rect(tray.x + 24f, tray.y + 52f, tray.width - 28f, 20f), "Timed Scythe Strike (Accuracy DMG)", smallStyle);
                GUI.color = new Color(0.78f, 0.78f, 0.82f);
                GUI.Label(new Rect(tray.x + 24f, tray.y + 73f, tray.width - 28f, 18f), "[Z] Aim Strike      [X] Back", smallStyle);
            }
            GUI.color = Color.white;
        }

        private void DrawActTargetInterior(Font hudFont)
        {
            var active = GetActiveUnresolvedSlots();
            DrawMultiEnemyTargetListInterior(hudFont, active, isFightMenu: false);
        }

        private void DrawMultiEnemyTargetListInterior(Font hudFont, List<RuntimeEnemySlot> active, bool isFightMenu)
        {
            Rect tray = new Rect(240f, 349f, 286f, 97f);
            var rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold };
            var hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Normal };
            if (hudFont != null) { rowStyle.font = hudFont; hintStyle.font = hudFont; }

            int count = Mathf.Min(4, active.Count);
            float rowStep = count >= 4 ? 18f : 20f;

            for (int i = 0; i < count; i++)
            {
                var slot = active[i];
                bool isSel = (i == _selectedTargetSlotCursor);
                float rowY = tray.y + (i * rowStep);

                if (isSel)
                {
                    DrawPulsingSoulHeart(tray.x + 9f, rowY + 9f, 14f);
                }

                // Dynamic Name Color: Yellow = Purify Ready, Red-Orange = 0 HP Purge Ready, White = Active
                if (slot.IsPurifyReady)
                    GUI.color = new Color(1f, 0.94f, 0.18f);
                else if (slot.IsAtPurgeMarker)
                    GUI.color = new Color(1f, 0.32f, 0.14f);
                else
                    GUI.color = isSel ? Color.white : new Color(0.86f, 0.88f, 0.92f);

                GUI.Label(new Rect(tray.x + 21f, rowY, 118f, 18f), slot.displayName.ToUpper(), rowStyle);

                // Mini HP Bar
                float hpRatio = Mathf.Clamp01((float)slot.currentHp / Mathf.Max(1, slot.maxHp));
                Rect hpBg = new Rect(tray.x + 142f, rowY + 4f, 58f, 9f);
                DrawBorderedBox(hpBg, new Color(0.28f, 0.08f, 0.08f), Color.white, 1f);
                GUI.color = slot.IsAtPurgeMarker ? new Color(1f, 0.25f, 0.1f) : new Color(0.15f, 0.95f, 0.4f);
                SafeDrawTexture(new Rect(hpBg.x + 1f, hpBg.y + 1f, (hpBg.width - 2f) * hpRatio, hpBg.height - 2f), Texture2D.whiteTexture);

                // Mini Purify % Bar & Readout
                float pRatio = Mathf.Clamp01(slot.PurifyPercent / 100f);
                Rect pBg = new Rect(tray.x + 206f, rowY + 4f, 44f, 9f);
                DrawBorderedBox(pBg, new Color(0.22f, 0.16f, 0.05f), Color.white, 1f);
                GUI.color = slot.IsPurifyReady ? new Color(1f, 0.95f, 0.15f) : new Color(1f, 0.65f, 0.12f);
                SafeDrawTexture(new Rect(pBg.x + 1f, pBg.y + 1f, (pBg.width - 2f) * pRatio, pBg.height - 2f), Texture2D.whiteTexture);

                GUI.color = Color.white;
                GUI.Label(new Rect(tray.x + 253f, rowY, 36f, 18f), $"{Mathf.RoundToInt(slot.PurifyPercent)}%", hintStyle);
            }

            GUI.color = new Color(0.76f, 0.78f, 0.84f);
            string actionHint = isFightMenu ? "[Z] Aim / Purge   [X] Back" : "[Z] Choose Enemy  [X] Back";
            GUI.Label(new Rect(tray.x + 21f, tray.y + 78f, 260f, 16f), actionHint, hintStyle);
            GUI.color = Color.white;
        }

        private void DrawAttackTimingBarInterior(Font hudFont)
        {
            // Undertale / Deltarune Accuracy Target Meter inside the bottom bar
            Rect meter = new Rect(244f, 356f, 274f, 60f);
            DrawBorderedBox(meter, new Color(0.04f, 0.04f, 0.08f), Color.white, 2f);

            float cx = meter.center.x;
            float innerY = meter.y + 4f;
            float innerH = meter.height - 8f;

            // Symmetrical accuracy zones radiating from dead-center sweet spot
            // Outer Graze band
            GUI.color = new Color(0.42f, 0.10f, 0.14f, 0.85f);
            SafeDrawTexture(new Rect(meter.x + 4f, innerY + 10f, meter.width - 8f, innerH - 20f), Texture2D.whiteTexture);

            // Mid Good band
            float goodHalfW = (meter.width * 0.5f) * 0.60f;
            GUI.color = new Color(0.82f, 0.44f, 0.10f, 0.90f);
            SafeDrawTexture(new Rect(cx - goodHalfW, innerY + 6f, goodHalfW * 2f, innerH - 12f), Texture2D.whiteTexture);

            // Inner Great band
            float greatHalfW = (meter.width * 0.5f) * 0.28f;
            GUI.color = new Color(0.15f, 0.82f, 0.38f, 0.95f);
            SafeDrawTexture(new Rect(cx - greatHalfW, innerY + 3f, greatHalfW * 2f, innerH - 6f), Texture2D.whiteTexture);

            // Center Critical Sweet Spot
            float critHalfW = (meter.width * 0.5f) * 0.09f;
            GUI.color = new Color(0.25f, 0.95f, 1.0f, 1.0f);
            SafeDrawTexture(new Rect(cx - critHalfW, innerY, critHalfW * 2f, innerH), Texture2D.whiteTexture);

            // Horizontal crosshair hairline & center bullseye target line
            GUI.color = new Color(1f, 1f, 1f, 0.35f);
            SafeDrawTexture(new Rect(meter.x + 4f, meter.center.y - 0.5f, meter.width - 8f, 1f), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.95f, 0.25f, 1f);
            SafeDrawTexture(new Rect(cx - 1.5f, meter.y + 1f, 3f, meter.height - 2f), Texture2D.whiteTexture);

            // Moving Strike Cursor Bar
            float clampedPos = Mathf.Clamp01(_attackBarPos);
            float cursorX = Mathf.Lerp(meter.x + 5f, meter.xMax - 5f, clampedPos);
            Rect cursorOuter = new Rect(cursorX - 4f, meter.y - 3f, 8f, meter.height + 6f);
            Rect cursorInner = new Rect(cursorX - 2.5f, meter.y - 1.5f, 5f, meter.height + 3f);

            GUI.color = Color.black;
            SafeDrawTexture(cursorOuter, Texture2D.whiteTexture);

            if (_attackBarLocked)
            {
                bool flashGold = ((int)(Time.time * 24f) % 2) == 0;
                GUI.color = _attackDidMiss
                    ? new Color(0.9f, 0.2f, 0.2f)
                    : (flashGold ? new Color(1f, 0.95f, 0.2f) : Color.white);
            }
            else
            {
                GUI.color = Color.white;
            }
            SafeDrawTexture(cursorInner, Texture2D.whiteTexture);

            // Prompt / Accuracy Rating Readout underneath meter
            var footerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            if (hudFont != null) footerStyle.font = hudFont;

            if (_attackBarLocked)
            {
                GUI.color = _attackDidMiss
                    ? new Color(1f, 0.3f, 0.3f)
                    : (_lastAttackMultiplier >= 1.6f ? new Color(1f, 0.95f, 0.2f) : new Color(0.3f, 0.95f, 1f));
                string msg = _attackDidMiss ? "MISS!" : $"{_lastAttackRating}  ({_lastAttackMultiplier:0.00}x DMG)";
                GUI.Label(new Rect(meter.x, 421f, meter.width, 22f), msg, footerStyle);
            }
            else
            {
                GUI.color = Color.white;
                GUI.Label(new Rect(meter.x, 421f, meter.width, 22f), "PRESS [Z / ENTER] AT CENTER!", footerStyle);
            }

            GUI.color = Color.white;
        }

        private void DrawActSelectInterior(Font hudFont)
        {
            Rect tray = new Rect(242f, 344f, 282f, 102f);
            var optStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
            var hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Normal };
            if (hudFont != null) { optStyle.font = hudFont; hintStyle.font = hudFont; }

            var target = GetTargetedEnemySlot();
            var acts = GetAvailableActsForSlot(target);
            int count = Mathf.Min(4, acts.Count);
            float rowStep = count >= 4 ? 18f : 21f;

            for (int i = 0; i < count; i++)
            {
                bool isSel = (i == _selectedActIndex);
                float rowY = tray.y + 1f + (i * rowStep);
                if (isSel)
                {
                    DrawPulsingSoulHeart(tray.x + 10f, rowY + 10f, 15f);
                }
                GUI.color = isSel ? new Color(1f, 0.92f, 0.25f) : Color.white;
                string suffix = acts[i].affectsAllEnemies ? " (ALL)" : "";
                GUI.Label(new Rect(tray.x + 24f, rowY, 256f, 20f), acts[i].actName + suffix, optStyle);
            }

            GUI.color = new Color(0.75f, 0.75f, 0.80f);
            GUI.Label(new Rect(tray.x + 24f, tray.y + 74f, 256f, 18f), "[Z] Confirm      [X] Back", hintStyle);
            GUI.color = Color.white;
        }

        private void DrawBagSelectInterior(Font hudFont)
        {
            Rect tray = new Rect(242f, 352f, 282f, 94f);
            var optStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            var hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            if (hudFont != null) { optStyle.font = hudFont; hintStyle.font = hudFont; }

            GUI.color = new Color(0.85f, 0.85f, 0.90f);
            GUI.Label(new Rect(tray.x + 12f, tray.y + 14f, 265f, 24f), "* Your BAG is empty.", optStyle);
            GUI.color = new Color(0.72f, 0.72f, 0.78f);
            GUI.Label(new Rect(tray.x + 12f, tray.y + 72f, 265f, 18f), "[X / ESC] Back", hintStyle);
            GUI.color = Color.white;
        }

        private void DrawPurifySelectInterior(Font hudFont)
        {
            Rect tray = new Rect(242f, 350f, 282f, 96f);
            var optStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            var hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            if (hudFont != null) { optStyle.font = hudFont; hintStyle.font = hudFont; }

            var readySlots = GetPurifyReadySlots();
            var target = GetTargetedEnemySlot();
            bool canPurify = readySlots.Count > 0 || (_enemySlots.Count == 0 && enemyCurrentMercy >= 100);
            int curP = target != null ? target.currentPurify : enemyCurrentMercy;
            int reqP = target != null ? target.purifyThreshold : 100;

            string purifyLabel = canPurify
                ? (readySlots.Count > 1 ? $"PURIFY [{readySlots.Count} READY!]" : "PURIFY [READY!]")
                : $"PURIFY [{curP} / {reqP}]";

            // Option 0: PURIFY
            if (_selectedMercyIndex == 0) DrawPulsingSoulHeart(tray.x + 10f, tray.y + 16f, 15f);
            GUI.color = (_selectedMercyIndex == 0)
                ? (canPurify ? new Color(1f, 0.95f, 0.15f) : new Color(1f, 0.88f, 0.25f))
                : (canPurify ? new Color(1f, 0.95f, 0.45f) : Color.white);
            GUI.Label(new Rect(tray.x + 24f, tray.y + 6f, 256f, 20f), purifyLabel, optStyle);

            // Option 1: FLEE (dimmed when Miniboss/ZoneBoss/FinalBoss blocks escape)
            bool bossBlocksFlee = IsFleeBlockedByBoss();
            if (_selectedMercyIndex == 1) DrawPulsingSoulHeart(tray.x + 10f, tray.y + 42f, 15f);
            GUI.color = bossBlocksFlee
                ? new Color(0.55f, 0.55f, 0.60f)
                : ((_selectedMercyIndex == 1) ? new Color(1f, 0.88f, 0.25f) : Color.white);
            GUI.Label(new Rect(tray.x + 24f, tray.y + 32f, 256f, 20f), bossBlocksFlee ? "FLEE [BOSS LOCKED]" : "FLEE", optStyle);

            GUI.color = new Color(0.75f, 0.75f, 0.80f);
            GUI.Label(new Rect(tray.x + 24f, tray.y + 74f, 256f, 18f), "[Z] Confirm      [X] Back", hintStyle);
            GUI.color = Color.white;
        }

        private void DrawNarrativeMessageInterior(Font hudFont)
        {
            Rect tray = new Rect(240f, 348f, 284f, 100f);
            float pulse = Mathf.PingPong(Time.time * 2.2f, 0.35f) + 0.65f;

            var bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Normal,
                wordWrap = true
            };
            var promptStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.LowerRight,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            if (hudFont != null) { bodyStyle.font = hudFont; promptStyle.font = hudFont; }

            GUI.color = (_menuState == BattleMenuState.Victory)
                ? new Color(1f, 0.93f, 0.30f)
                : Color.white;

            string displayMsg = string.IsNullOrEmpty(_activeDialogueText)
                ? $"* Owen strikes! Dealt {_lastDamageDealt} damage!"
                : _activeDialogueText;

            GUI.Label(new Rect(tray.x + 4f, tray.y + 4f, tray.width - 8f, 76f), displayMsg, bodyStyle);

            if (_menuState == BattleMenuState.Victory)
            {
                GUI.color = new Color(1f, 0.95f, 0.2f, pulse);
                GUI.Label(new Rect(tray.x, tray.y + 78f, tray.width - 6f, 18f), "[Z / ENTER] Return", promptStyle);
            }
            else if (_menuState == BattleMenuState.ActResponse || _isSpeechBubbleVisible)
            {
                GUI.color = new Color(1f, 0.95f, 0.2f, pulse);
                GUI.Label(new Rect(tray.x, tray.y + 78f, tray.width - 6f, 18f), "[Z / ENTER] Next", promptStyle);
            }

            GUI.color = Color.white;
        }

        private void DrawBorderedBox(Rect r, Color fillColor, Color borderColor, float borderWidth)
        {
            // Fill
            GUI.color = fillColor;
            SafeDrawTexture(r, Texture2D.whiteTexture);

            // Border
            if (borderWidth > 0.01f && borderColor.a > 0.01f)
            {
                GUI.color = borderColor;
                SafeDrawTexture(new Rect(r.x, r.y, r.width, borderWidth), Texture2D.whiteTexture); // Top
                SafeDrawTexture(new Rect(r.x, r.y + r.height - borderWidth, r.width, borderWidth), Texture2D.whiteTexture); // Bottom
                SafeDrawTexture(new Rect(r.x, r.y, borderWidth, r.height), Texture2D.whiteTexture); // Left
                SafeDrawTexture(new Rect(r.x + r.width - borderWidth, r.y, borderWidth, r.height), Texture2D.whiteTexture); // Right
            }
            GUI.color = Color.white;
        }
        #endregion
    }
}
