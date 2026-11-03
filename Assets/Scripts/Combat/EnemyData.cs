using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigiPurity.Combat
{
    public enum BulletPatternType
    {
        RainFromTop,        // Vertical falling projectiles (e.g. golf balls, rain)
        AimedSpreadBurst,   // Aimed fan shot at Owen's soul
        AlternatingSide,    // Left-right crossfire
        RadialSpiral        // Expanding ring of bullets
    }

    public enum SongTriggerCondition
    {
        EnemyHpAtOrBelowPercent,    // e.g. Enemy HP <= 50%
        OwenHpAtOrBelowPercent,     // e.g. Owen HP <= 25% (Pinch / Danger!)
        PurifyAtOrAbovePercent,     // e.g. Purify >= 50% or 100%
        OnTurnNumberReached,        // e.g. Turn >= 3
        OnSpecificActUsed,          // Matches triggerActName (case-insensitive)
        OnZeroHpPurgeReady,         // Enemy reduced to 0 HP helpless state
        OnPurgeExecuted,            // Player confirms lethal PURGE
        OnPurifyExecuted            // Player confirms peaceful PURIFY
    }

    public enum SongActionType
    {
        SwitchToNewSong,            // Crossfade or instant-cut to targetSong
        ModifyCurrentSong,          // Smoothly change pitch/speed, volume, distortion, or low-pass muffle
        TapeStopToSilence,          // Analog pitch power-down deceleration to silence
        FadeOutAndStop,             // Smooth volume fade out to silence
        PauseSong,                  // Dramatic sudden pause
        ResumeSong                  // Resume paused song with target settings
    }

    [Serializable]
    public class BattleSongRule
    {
        [Tooltip("Optional label so you can read what this rule does at a glance in the Inspector.")]
        public string ruleName = "Phase 2 Song Rule";
        public SongTriggerCondition condition = SongTriggerCondition.EnemyHpAtOrBelowPercent;
        [Tooltip("Threshold % (for HP or Purify) or Turn Number (for OnTurnNumberReached).")]
        public float thresholdValue = 50f;
        [Tooltip("Exact ACT name that triggers this rule when condition is OnSpecificActUsed.")]
        public string triggerActName = "";

        [Header("What To Do To The Song")]
        public SongActionType action = SongActionType.SwitchToNewSong;
        [Tooltip("New AudioClip to play when action is SwitchToNewSong.")]
        public AudioClip targetSong;
        public bool loopTargetSong = true;
        [Tooltip("If true, starts the new song at the exact same timestamp as the previous song (great for layered calm/intense stems at the same BPM!).")]
        public bool syncPlaybackTimeOnSwitch = false;

        [Range(0f, 1f)] public float targetVolume = 0.85f;
        [Range(0.2f, 2.0f)] public float targetPitch = 1.0f;
        [Range(0f, 0.9f)] public float targetDistortion = 0.0f;
        [Range(500f, 22000f)] public float targetLowPassCutoff = 22000f;
        [Tooltip("Seconds to crossfade or glide pitch/effects (0 = instant cut).")]
        public float transitionDuration = 0.75f;
        public bool triggerOnce = true;

        public bool Matches(
            SongTriggerCondition eventContext,
            float enemyHpPercent,
            float owenHpPercent,
            float purifyPercent,
            int turnNumber,
            string actUsedName = "")
        {
            if (condition != eventContext) return false;

            switch (condition)
            {
                case SongTriggerCondition.EnemyHpAtOrBelowPercent:
                    return enemyHpPercent <= thresholdValue + 0.01f;
                case SongTriggerCondition.OwenHpAtOrBelowPercent:
                    return owenHpPercent <= thresholdValue + 0.01f;
                case SongTriggerCondition.PurifyAtOrAbovePercent:
                    return purifyPercent >= thresholdValue - 0.01f;
                case SongTriggerCondition.OnTurnNumberReached:
                    return turnNumber >= Mathf.RoundToInt(thresholdValue);
                case SongTriggerCondition.OnSpecificActUsed:
                    return !string.IsNullOrEmpty(triggerActName) &&
                           !string.IsNullOrEmpty(actUsedName) &&
                           string.Equals(triggerActName.Trim(), actUsedName.Trim(), StringComparison.OrdinalIgnoreCase);
                case SongTriggerCondition.OnZeroHpPurgeReady:
                case SongTriggerCondition.OnPurgeExecuted:
                case SongTriggerCondition.OnPurifyExecuted:
                    return true;
                default:
                    return false;
            }
        }
    }

    [Serializable]
    public class TalkOption
    {
        public string optionName = "Check";
        [TextArea(2, 4)]
        public string responseText = "GOLFER - ATK 4 DEF 2. An obsolete virtual athlete puppeteered by rogue subroutines.";
        public int chargeDelta = 0;
        public int mercyReward = 50;
        public int purifyReward { get => mercyReward; set => mercyReward = value; }
    }

    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "DIGIPURITY/Enemy Data", order = 10)]
    public class EnemyData : ScriptableObject
    {
        [Header("General Identity")]
        public EnemyClassification enemyType = EnemyClassification.BasicEnemy;
        public string enemyId = "golfer";
        public string enemyName = "Golfer";
        [TextArea(2, 3)]
        public string description = "A rogue sports simulation entity infected by primitive AI scripts.";

        [Header("Sprites & Visual Calibration")]
        public Sprite overworldSprite;
        public Sprite battleSprite;
        [Tooltip("Optional multi-frame idle animation loop (overrides static battleSprite if populated).")]
        public Sprite[] idleFrames;
        [Range(1f, 24f)] public float idleFps = 6f;
        public Sprite battleHurtSprite;
        public Sprite purifyReadySprite;
        public Sprite zeroHpExhaustedSprite;
        public Sprite bulletSprite;
        public Color bulletColor = Color.white;
        public float battleSpriteScale = 1.0f;
        public Vector2 stagePositionOffset = Vector2.zero;
        public bool flipBattleSpriteX = false;
        public bool enableProceduralBob = true;

        [Header("Encounter Music & Conditional Song Rules")]
        [Tooltip("Main battle song played when this encounter starts.")]
        public AudioClip battleBgm;
        [Range(0f, 1f)] public float bgmVolume = 0.85f;
        [Range(0.2f, 2.0f)] public float bgmStartPitch = 1.0f;
        [Tooltip("If enabled, automatically slows & distorts the song as enemy HP drops toward 0 HP (and restores tempo on positive ACT). Turn off if using custom Conditional Song Rules instead!")]
        public bool enableAutoLowHpSlowdown = true;
        [Tooltip("Add conditional rules to swap songs, crossfade phases, change speed/pitch, muffle, distort, or tape-stop at specific moments!")]
        public List<BattleSongRule> conditionalSongRules = new List<BattleSongRule>();

        public AudioClip attackSound;
        public AudioClip hurtSound;

        [Header("Stats, Defense & Damage Affinities")]
        public int maxHp = 100;
        public int defense = 0;
        public int purifyThreshold = 100;
        public List<DamageAffinityEntry> damageAffinities = new List<DamageAffinityEntry>();
        public int glitchThreshold = 50;
        public int basePower = 4;
        public int coinCount = 3;
        public int coinPower = 3;
        public string primarySkillName = "Putter Cleave";

        [Header("Deltarune-Style Dialogue, Conditional Rules & Speech Bubbles")]
        public AudioClip voiceBlipClip;
        [TextArea(1, 2)]
        public string[] inBattleBarks = new string[]
        {
            "PAR FOR THE COURSE.",
            "NICE SHOT, OWEN.",
            "OUT OF BOUNDS ERROR.",
            "FORE! SYSTEM CORRUPT."
        };
        [Tooltip("Ordered & conditional dialogue rules (Turn #, HP %, Active Attack Pattern #, Purify %, Specific ACT, Ally Purged/Purified).")]
        public List<ConditionalDialogueRule> conditionalDialogueRules = new List<ConditionalDialogueRule>();

        [Header("TALK / ACT Options (Classic & Modular)")]
        public List<TalkOption> talkOptions = new List<TalkOption>()
        {
            new TalkOption
            {
                optionName = "Check",
                responseText = "GOLFER - ATK 4 DEF 2. Mindlessly repeating the 18th hole from 2004."
            },
            new TalkOption
            {
                optionName = "Analyze Swing",
                responseText = "You pointed out a hitch in his backswing. Golfer looks perturbed!"
            },
            new TalkOption
            {
                optionName = "Heckle",
                responseText = "You yelled during his putting routine. His Charge dropped!",
                chargeDelta = -15
            }
        };
        [Tooltip("Optional richer ACT options with immediate enemy speech replies, stat debuffs, unlock chains, and group targeting.")]
        public List<ModularActOption> modularActOptions = new List<ModularActOption>();

        [Header("Modular Attack Patterns (Golfer Baseline Presets + Custom Prefabs)")]
        public AttackOrderMode attackOrder = AttackOrderMode.SequentialLoop;
        [Tooltip("Add multiple solo attacks using Golfer's baseline presets or your own Custom Attack Prefabs! If empty, falls back to primaryPattern below.")]
        public List<EnemyAttackSlot> modularAttackPatterns = new List<EnemyAttackSlot>();

        [Header("Legacy Single Bullet Pattern & Editable Dodge Box")]
        public BulletPatternType primaryPattern = BulletPatternType.RainFromTop;
        public float bulletSpeed = 3.2f;
        public int bulletDamage = 8;
        public float dodgePhaseDuration = 5.0f;

        [Tooltip("If enabled, this enemy overrides the default Dodge Box position and size on the battle stage.")]
        public bool useCustomDodgeBox = false;
        [Tooltip("Custom offset of the Dodge Box relative to Stage Center (1000, 1000).")]
        public Vector2 customDodgeBoxOffset = new Vector2(0.85f, 1.35f);
        [Tooltip("Custom width and height of the Dodge Box in world units.")]
        public Vector2 customDodgeBoxSize = new Vector2(3.6f, 2.7f);

        [Header("Speech Dialogue Box Placement & Encounter Mode")]
        [Tooltip("Pixel offset (in 640x480 space) of the enemy's speech bubble relative to the left of their sprite.")]
        public Vector2 speechBubbleOffset = Vector2.zero;
        [Tooltip("Width and minimum height of this enemy's stepped-corner Deltarune speech bubble in 640x480 space.")]
        public Vector2 speechBubbleSize = new Vector2(188f, 68f);
        [Tooltip("If true, runs BattleTutorialEncounter onboarding for this enemy (e.g. initial Golfer tutorial).")]
        public bool useTutorialOnboarding = false;

        [ContextMenu("Load Golfer Baseline Attack Presets")]
        public void LoadGolferBaselineAttackPresets()
        {
            modularAttackPatterns = EnemyAttackSlot.CreateGolferBaselinePresets();
        }
    }
}
