using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigiPurity.Combat
{
    public enum EnemyClassification
    {
        BasicEnemy = 0, // Standard encounter; allows FLEE unless zone-locked
        Miniboss = 1,   // Disables FLEE; custom multi-wave behavior
        ZoneBoss = 2,   // Full boss encounter (e.g. Triple T); disables FLEE
        FinalBoss = 3   // Master AI (MCP/AM Hybrid); disables FLEE
    }

    public enum BattleDamageType
    {
        PhysicalScythe = 0, // Standard timed PURGE scythe swing
        DigitalGlitch = 1,  // Stutter / Glitch burst
        ThermalBurn = 2,    // Burn keyword damage
        CrimsonBleed = 3,   // Bleed keyword damage
        ChargeDrainage = 4  // Drainage / electrical shock
    }

    [Serializable]
    public class DamageAffinityEntry
    {
        public BattleDamageType damageType = BattleDamageType.PhysicalScythe;
        [Tooltip("0.0 = Immune, 0.5 = Resistant, 1.0 = Neutral, 1.5 - 2.0 = Weak!")]
        [Range(0f, 3f)] public float multiplier = 1.0f;
    }

    /// <summary>
    /// Built-in attack pattern presets (starting with Golfer's attack patterns as baseline presets)
    /// plus CustomAttackPrefab mode so you can craft bespoke attacks with AttackDamageZone hitboxes.
    /// </summary>
    public enum AttackPresetType
    {
        Golfer_GolfBallRain = 0,    // Baseline Preset 1: Falling golf balls from top with slight wind drift
        Golfer_SandTrapSpread = 1,  // Baseline Preset 2: Aimed club shot (heavy core ball + lighter outer sand spray!)
        Golfer_BouncingDrive = 2,   // Baseline Preset 3: High-velocity bouncing golf ball ricocheting off box walls
        Golfer_CrossFairway = 3,    // Baseline Preset 4: Alternating left/right fairway crossfire
        RadialSpiralRing = 4,       // Baseline Preset 5: Expanding 360-degree rotating spiral ring
        CustomAttackPrefab = 5      // Spawns your custom Attack Wave Prefab (with AttackDamageZone sub-hitboxes!)
    }

    /// <summary>
    /// Undertale/Deltarune movement-conditional & special projectile rules.
    /// </summary>
    public enum HazardMovementRule
    {
        StandardWhite = 0,      // Always deals damage on contact
        CyanStopToAvoid = 1,    // Deals 0 damage if Owen's soul is holding still!
        OrangeMoveToAvoid = 2,  // Deals 0 damage if Owen's soul is actively moving!
        GreenHealOrb = 3,       // Heals Owen by 'damage' amount on touch!
        GoldPurifyOrb = 4       // Adds 'damage' % to target enemy's Purify meter on touch!
    }

    public enum AttackOrderMode
    {
        SequentialLoop = 0,     // Attack 1 -> Attack 2 -> Attack 3 -> repeat
        RandomEachTurn = 1,     // Picks randomly from unlocked attacks
        HpPhasePriority = 2     // Prioritizes attacks unlocked at lower HP thresholds first
    }

    [Serializable]
    public class EnemyAttackSlot
    {
        [Tooltip("Name of this attack (helps match conditional dialogue rules & debugging).")]
        public string attackName = "Golfer Preset: Fairway Rain";

        [Tooltip("Choose one of Golfer's baseline presets to tweak, or CustomAttackPrefab to spawn your own prefab!")]
        public AttackPresetType presetType = AttackPresetType.Golfer_GolfBallRain;

        [Tooltip("Optional custom prefab spawned at Dodge Box center! Can be used standalone (CustomAttackPrefab) or layered on top of a Golfer preset.")]
        public GameObject customAttackPrefab;

        [Header("Timing & Projectile Settings")]
        [Range(1.0f, 15.0f)] public float duration = 4.8f;
        [Range(0.08f, 2.5f)] public float spawnInterval = 0.28f;
        [Range(0.5f, 14.0f)] public float projectileSpeed = 3.2f;
        [Range(1, 12)] public int projectilesPerStep = 3;

        [Header("Per-Part Damage Values (Core Sweet-Spot vs. Outer Splash)")]
        [Tooltip("Heavy/direct hit damage (e.g. direct golf ball / laser core).")]
        public int coreDamage = 10;
        [Tooltip("Lighter outer/glancing hit damage (e.g. sand spray sparks in SandTrapSpread).")]
        public int outerSplashDamage = 5;

        [Header("Visuals & Movement Rule")]
        public Sprite projectileSprite;
        public Color projectileColor = Color.white;
        [Range(0.1f, 3.0f)] public float projectileScale = 0.40f;
        public HazardMovementRule movementRule = HazardMovementRule.StandardWhite;

        [Header("Phase Unlock Condition")]
        [Tooltip("100 = unlocked from the start. Set to e.g. 50 so this attack only unlocks once enemy HP <= 50%!")]
        [Range(0f, 100f)] public float unlockAtOrBelowHpPercent = 100f;

        [Header("Per-Attack Custom Dodge Box Shape (Optional)")]
        [Tooltip("If checked, reshapes/moves the Dodge Box specifically during this attack!")]
        public bool overrideDodgeBoxForAttack = false;
        public Vector2 attackDodgeBoxOffset = new Vector2(0.85f, 1.35f);
        public Vector2 attackDodgeBoxSize = new Vector2(3.6f, 2.7f);

        [Header("Dodge Phase Screen Dimming")]
        [Tooltip("[Darken UI Buttons]\nDarkens/dims the left-side command buttons and bottom angled HUD while dodging.")]
        public bool darkenUIButtons = true;
        [Tooltip("[UI Dim Amount]\nHow much the UI darkens (0.0 = completely invisible black, 0.45 = default dim, 1.0 = full brightness).")]
        [Range(0f, 1f)] public float uiDimAlpha = 0.40f;
        [Tooltip("[Darken Owen Sprite]\nDarkens Owen's battle sprite while dodging to focus attention on the cyan soul inside the box.")]
        public bool darkenOwenSprite = false;
        [Tooltip("[Darken Enemy Sprites]\nDarkens enemy sprites while dodging (default false: enemy sprites stay fully lit).")]
        public bool darkenEnemySprites = false;
        [Tooltip("[Sprite Dim Amount]\nHow dark Owen or enemies get if their dimming is enabled (0.0 = black silhouette, 0.45 = dim, 1.0 = normal).")]
        [Range(0f, 1f)] public float spriteDimAmount = 0.45f;

        public static EnemyAttackSlot CreateGolferFairwayRainPreset()
        {
            return new EnemyAttackSlot
            {
                attackName = "Golfer Preset: Fairway Rain",
                presetType = AttackPresetType.Golfer_GolfBallRain,
                duration = 4.8f,
                spawnInterval = 0.28f,
                projectileSpeed = 3.2f,
                projectilesPerStep = 1,
                coreDamage = 8,
                outerSplashDamage = 5,
                projectileColor = Color.white,
                projectileScale = 0.40f
            };
        }

        public static EnemyAttackSlot CreateGolferSandTrapSpreadPreset()
        {
            return new EnemyAttackSlot
            {
                attackName = "Golfer Preset: Sand Trap Bunker Spread",
                presetType = AttackPresetType.Golfer_SandTrapSpread,
                duration = 4.8f,
                spawnInterval = 0.52f,
                projectileSpeed = 3.6f,
                projectilesPerStep = 3,
                coreDamage = 12,        // Center golf ball deals heavy damage
                outerSplashDamage = 5,  // Flanking sand-spray pellets deal lighter splash damage
                projectileColor = Color.white,
                projectileScale = 0.40f
            };
        }

        public static EnemyAttackSlot CreateGolferBouncingDrivePreset()
        {
            return new EnemyAttackSlot
            {
                attackName = "Golfer Preset: Ricochet Putter Drive",
                presetType = AttackPresetType.Golfer_BouncingDrive,
                duration = 5.0f,
                spawnInterval = 0.70f,
                projectileSpeed = 3.8f,
                projectilesPerStep = 1,
                coreDamage = 10,
                outerSplashDamage = 6,
                projectileColor = Color.white,
                projectileScale = 0.44f
            };
        }

        public static EnemyAttackSlot CreateGolferCrossFairwayPreset()
        {
            return new EnemyAttackSlot
            {
                attackName = "Golfer Preset: Cross-Fairway Winds",
                presetType = AttackPresetType.Golfer_CrossFairway,
                duration = 4.8f,
                spawnInterval = 0.30f,
                projectileSpeed = 3.4f,
                projectilesPerStep = 1,
                coreDamage = 8,
                outerSplashDamage = 5,
                projectileColor = Color.white,
                projectileScale = 0.38f
            };
        }

        public static List<EnemyAttackSlot> CreateGolferBaselinePresets()
        {
            return new List<EnemyAttackSlot>
            {
                CreateGolferFairwayRainPreset(),
                CreateGolferSandTrapSpreadPreset(),
                CreateGolferBouncingDrivePreset()
            };
        }

        public static AttackPresetType ConvertLegacyPattern(BulletPatternType legacy)
        {
            switch (legacy)
            {
                case BulletPatternType.RainFromTop: return AttackPresetType.Golfer_GolfBallRain;
                case BulletPatternType.AimedSpreadBurst: return AttackPresetType.Golfer_SandTrapSpread;
                case BulletPatternType.AlternatingSide: return AttackPresetType.Golfer_CrossFairway;
                case BulletPatternType.RadialSpiral: return AttackPresetType.RadialSpiralRing;
                default: return AttackPresetType.Golfer_GolfBallRain;
            }
        }
    }

    [Serializable]
    public class CombinedAttackRule
    {
        public string comboName = "Dual Fairway Crossfire";
        [Tooltip("0-based enemy slot indices that must ALL be alive (> 0 HP) and unpurified for this combo to trigger (e.g. 0 and 1 for Enemy 1 + Enemy 2).")]
        public List<int> requiredAliveSlotIndices = new List<int> { 0, 1 };
        [Tooltip("If > 0, triggers cleanly every N turns (e.g. 2 = every 2nd turn while partners are alive).")]
        public int triggerEveryNTurns = 2;
        [Range(0f, 1f)] public float randomTriggerChance = 0.5f;

        [Tooltip("Optional bark lines spoken by the participating enemies right before the combined attack starts!")]
        [TextArea(1, 2)]
        public List<string> participantTelegraphBarks = new List<string>
        {
            "WATCH MY BACKSWING!",
            "COVERING THE BUNKER!"
        };

        [Tooltip("The combined attack wave (can use any Golfer preset, simultaneous emitters, or a custom Attack Prefab!).")]
        public EnemyAttackSlot combinedAttack = EnemyAttackSlot.CreateGolferSandTrapSpreadPreset();
    }

    public enum DialogueTriggerType
    {
        TurnSequence = 0,               // Plays on exact Turn # (1, 2, 3...)
        HealthAtOrBelowPercent = 1,     // Fires when enemy HP <= thresholdValue %
        WhenUsingAttackIndex = 2,       // Fires right before enemy uses Attack Pattern [attackIndex] (0 = Attack 1, 1 = Attack 2...)
        PurifyAtOrAbovePercent = 3,     // Fires when enemy Purify >= thresholdValue %
        AfterSpecificAct = 4,           // Fires right after player uses triggerActName on this enemy
        OnPlayerCriticalStrike = 5,     // Fires when Owen hits CRITICAL!! on the timing bar
        OnPlayerMiss = 6,               // Fires when Owen MISSes the timing bar
        WhenAllyPurged = 7,             // Fires when another enemy in the encounter is lethally PURGED
        WhenAllyPurified = 8            // Fires when another enemy in the encounter is peacefully PURIFIED
    }

    [Serializable]
    public class ConditionalDialogueRule
    {
        public string ruleLabel = "Dialogue Rule";
        public DialogueTriggerType triggerType = DialogueTriggerType.TurnSequence;
        [Tooltip("Turn # (for TurnSequence), HP % (for HealthAtOrBelowPercent), or Purify % (for PurifyAtOrAbovePercent).")]
        public float thresholdValue = 1f;
        [Tooltip("0-based index into Attack Patterns list (0 = Attack 1, 1 = Attack 2, etc.) when triggerType is WhenUsingAttackIndex.")]
        public int attackPatternIndex = 0;
        [Tooltip("Exact ACT name match when triggerType is AfterSpecificAct.")]
        public string triggerActName = "";
        [Tooltip("Higher priority rules override lower priority rules when multiple conditions match on the same beat.")]
        public int priority = 50;
        public bool triggerOnce = false;

        [Tooltip("One or more lines spoken inside this enemy's Deltarune stepped-pixel speech bubble. Cycles in order if multiple lines are provided!")]
        [TextArea(2, 3)]
        public List<string> dialogueLines = new List<string> { "PAR FOR THE COURSE." };

        [NonSerialized] public int runtimeLineCursor = 0;

        public string NextLine()
        {
            if (dialogueLines == null || dialogueLines.Count == 0) return "";
            string line = dialogueLines[Mathf.Clamp(runtimeLineCursor, 0, dialogueLines.Count - 1)];
            runtimeLineCursor = (runtimeLineCursor + 1) % dialogueLines.Count;
            return line;
        }

        public bool Matches(
            DialogueTriggerType context,
            int turnNumber,
            float hpPercent,
            float purifyPercent,
            int chosenAttackIndex = -1,
            string actUsed = "")
        {
            if (triggerType != context) return false;

            switch (triggerType)
            {
                case DialogueTriggerType.TurnSequence:
                    return turnNumber == Mathf.RoundToInt(thresholdValue);
                case DialogueTriggerType.HealthAtOrBelowPercent:
                    return hpPercent <= thresholdValue + 0.01f;
                case DialogueTriggerType.WhenUsingAttackIndex:
                    return chosenAttackIndex == attackPatternIndex;
                case DialogueTriggerType.PurifyAtOrAbovePercent:
                    return purifyPercent >= thresholdValue - 0.01f;
                case DialogueTriggerType.AfterSpecificAct:
                    return !string.IsNullOrEmpty(triggerActName) &&
                           !string.IsNullOrEmpty(actUsed) &&
                           string.Equals(triggerActName.Trim(), actUsed.Trim(), StringComparison.OrdinalIgnoreCase);
                case DialogueTriggerType.OnPlayerCriticalStrike:
                case DialogueTriggerType.OnPlayerMiss:
                case DialogueTriggerType.WhenAllyPurged:
                case DialogueTriggerType.WhenAllyPurified:
                    return true;
                default:
                    return false;
            }
        }
    }

    [Serializable]
    public class EnemyVisualSetup
    {
        [Tooltip("1 sprite for static idle, or multiple frames for an animated pixel idle loop!")]
        public Sprite[] idleFrames;
        [Range(1f, 24f)] public float idleFps = 6f;
        public Sprite attackWindupSprite;
        public Sprite hurtSprite;
        [Tooltip("Optional calmer sprite once Purify reaches the enemy's Purify Threshold.")]
        public Sprite purifyReadySprite;
        [Tooltip("Optional slumped/exhausted sprite when reduced to 0 HP.")]
        public Sprite zeroHpExhaustedSprite;
        public Sprite overworldPurifiedSprite;

        [Header("Inspector Framing & Stage Calibration")]
        [Tooltip("Fine-tune X/Y offset for this specific enemy relative to their formation slot position.")]
        public Vector2 stagePositionOffset = Vector2.zero;
        [Tooltip("Uniform sprite scale on the battle stage (Golfer baseline = 4.35).")]
        public float scale = 4.35f;
        public bool flipX = false;
        [Tooltip("Vertical offset to align feet cleanly with the ground plane.")]
        public float groundLineOffset = 0f;
        public int sortingOrderOffset = 0;

        [Header("Procedural Sine Bob")]
        public bool enableProceduralBob = true;
        public float bobFrequency = 2.5f;
        public float bobAmplitude = 0.12f;
    }

    [Serializable]
    public class ModularActOption
    {
        public string actName = "Check";
        [Tooltip("Narration shown inside the bottom angled cyber bar when this ACT is chosen.")]
        [TextArea(2, 4)]
        public string bottomBarNarration = "GOLFER - ATK 4 DEF 2. Mindlessly repeating the 18th hole from 2004.";
        [Tooltip("Optional immediate line the enemy speaks back in their white speech bubble!")]
        [TextArea(1, 3)]
        public string enemySpeechReply = "";

        [Header("Purify & Combat Effects")]
        [Tooltip("[Purify Gain]\nHow much Purify progress to add (can be positive like +50, 0 for Check, or negative like -20 for an insult!).")]
        public int purifyGain = 0;
        [Tooltip("[Next Turn Damage Multiplier]\nMultiplies this enemy's bullet damage on their very next turn (e.g. 0.75 = weakens attack by 25%).")]
        [Range(0.1f, 2.0f)] public float nextTurnDamageMultiplier = 1.0f;
        [Tooltip("[Next Turn Speed Multiplier]\nMultiplies this enemy's bullet speed on their very next turn (e.g. 0.8 = slows bullets by 20%).")]
        [Range(0.2f, 2.0f)] public float nextTurnSpeedMultiplier = 1.0f;
        [Tooltip("[Affects All Enemies]\nIf checked, applies purifyGain and stat effects to ALL active enemies on stage!")]
        public bool affectsAllEnemies = false;
        [Tooltip("[Unlocked After Act Name]\nOptional: leave empty if unlocked from Turn 1, or type another ACT's name (e.g. 'Check') that must be used first to unlock this ACT!")]
        public string unlockedAfterActName = "";
    }

    public enum EnemyFormationPreset
    {
        AutoDiagonalStagger = 0,    // Clean Deltarune staggered right-side column
        VerticalColumn = 1,         // Straight vertical stack on the right
        BossCenterWithFlanks = 2,   // Slot 1 prominent center-right, additional slots flanking
        ManualOffsetsOnly = 3       // Uses each slot's exact stagePositionOffset relative to default enemyStageOffset
    }

    /// <summary>
    /// Full Inspector-editable configuration for Enemy [n] in a modular battle encounter.
    /// </summary>
    [Serializable]
    public class BattleEncounterSlot
    {
        [Header("1-3. Enemy Type & Identity")]
        public EnemyClassification enemyType = EnemyClassification.BasicEnemy;
        public string enemyName = "Golfer";
        [Tooltip("Optional EnemyData template. Inline settings below let you customize every enemy slot freely in the Inspector!")]
        public EnemyData baseProfile;

        [Header("5. Health, Defense & Damage Affinities")]
        [Tooltip("[Max HP]\nMaximum health of this enemy in combat.")]
        public int maxHp = 100;
        [Tooltip("[Starting HP]\n0 = starts at full maxHp. Set > 0 to start an enemy pre-damaged.")]
        public int startingHp = 0;
        [Tooltip("[Defense]\nDamage reduction subtracted from player attacks.")]
        public int defense = 0;
        [Tooltip("[Purge Marker HP]\nHP threshold at or below which lethal PURGE is unlocked (default 0 HP).")]
        public int purgeMarkerHp = 0;
        [Tooltip("[Damage Affinities]\nMultipliers for specific damage types (e.g. Scythe, Glitch, Burn, Bleed).")]
        public List<DamageAffinityEntry> damageAffinities = new List<DamageAffinityEntry>();

        [Header("7. Sprite Slots & Visual Framing")]
        public EnemyVisualSetup visuals = new EnemyVisualSetup();

        [Header("4. Attack Patterns (Golfer Presets + Custom Prefabs)")]
        public AttackOrderMode attackOrder = AttackOrderMode.SequentialLoop;
        public List<EnemyAttackSlot> attackPatterns = EnemyAttackSlot.CreateGolferBaselinePresets();

        [Header("6. Deltarune Speech Bubble & Conditional Dialogue")]
        [Tooltip("If checked, speech bubble will never appear and the enemy stays silent unless a specific dialogue rule triggers.")]
        public bool silentEnemyNoBarks = false;
        [Tooltip("Pixel offset (in 640x480 space) for this enemy's stepped-corner Deltarune speech bubble (placed to the LEFT of the enemy by default).")]
        public Vector2 speechBubbleOffset = Vector2.zero;
        [Tooltip("Width & min-height of this enemy's speech bubble in 640x480 virtual pixels.")]
        public Vector2 speechBubbleSize = new Vector2(188f, 68f);
        public AudioClip voiceBlipClip;
        [Tooltip("Fallback random barks when no higher-priority ConditionalDialogueRule triggers.")]
        [TextArea(1, 2)]
        public List<string> defaultBattleBarks = new List<string>
        {
            "PAR FOR THE COURSE.",
            "NICE SHOT, OWEN.",
            "OUT OF BOUNDS ERROR.",
            "FORE! SYSTEM CORRUPT."
        };
        public List<ConditionalDialogueRule> conditionalDialogue = new List<ConditionalDialogueRule>();

        [Header("8. ACT Options & Purify Threshold")]
        [Tooltip("[Purify Threshold]\nPurify value required to peacefully PURIFY this enemy (standard = 100; bosses/tough enemies can require 150 or 200!).")]
        public int purifyThreshold = 100;
        public List<ModularActOption> actOptions = new List<ModularActOption>();

        [Header("9. Ending Lines & Victory Rewards")]
        [Tooltip("[Hit Reaction Bark]\nOptional speech bark when this enemy is hit by Owen's scythe.")]
        [TextArea(1, 2)] public string hitReactionBark = "";
        [Tooltip("[Zero HP Exhausted Bark]\nOptional speech bark when enemy reaches 0 HP and enters the exhausted Purge Marker state.")]
        [TextArea(1, 2)] public string zeroHpExhaustedBark = "";
        [Tooltip("[Purify Farewell Bark]\nFinal peaceful words spoken before dissolving in radiant white light upon PURIFY.")]
        [TextArea(1, 2)] public string purifyFarewellBark = "The static in my head... it's finally quiet. Thank you, Owen.";
        [Tooltip("[Lethal Purge Monologue]\nSequential dialogue lines spoken when player selects PURGE, advanced with Z/Enter.")]
        [TextArea(1, 2)] public string[] lethalPurgeMonologue = Array.Empty<string>();
        [Tooltip("[Lethal Purge Cut Off Line]\nFinal cut-off sentence that Owen's scythe interrupts right as the enemy implodes.")]
        public string lethalPurgeCutOffLine = "";
        [Tooltip("[EXP On Purify]\nExperience points awarded for peaceful Purification.")]
        public int expOnPurify = 10;
        [Tooltip("[EXP On Purge]\nBonus experience points awarded for lethal Deletion (1.5x standard).")]
        public int expOnPurge = 25;
        [Tooltip("[Digi Dollars Reward]\nCurrency awarded upon winning the battle.")]
        public int digiDollarsReward = 5;

        public float GetDamageMultiplier(BattleDamageType dmgType)
        {
            if (damageAffinities != null)
            {
                for (int i = 0; i < damageAffinities.Count; i++)
                {
                    if (damageAffinities[i] != null && damageAffinities[i].damageType == dmgType)
                    {
                        return Mathf.Max(0f, damageAffinities[i].multiplier);
                    }
                }
            }
            return 1.0f;
        }

        public static BattleEncounterSlot CreateGolferPresetSlot(EnemyData golferAsset = null)
        {
            var slot = new BattleEncounterSlot
            {
                enemyType = EnemyClassification.BasicEnemy,
                enemyName = golferAsset != null && !string.IsNullOrEmpty(golferAsset.enemyName) ? golferAsset.enemyName : "Golfer",
                baseProfile = golferAsset,
                maxHp = golferAsset != null && golferAsset.maxHp > 0 ? golferAsset.maxHp : 100,
                startingHp = 0,
                defense = 0,
                purgeMarkerHp = 0,
                attackOrder = AttackOrderMode.SequentialLoop,
                attackPatterns = EnemyAttackSlot.CreateGolferBaselinePresets(),
                purifyThreshold = 100,
                speechBubbleOffset = golferAsset != null ? golferAsset.speechBubbleOffset : Vector2.zero,
                speechBubbleSize = new Vector2(188f, 68f)
            };

            if (golferAsset != null)
            {
                slot.visuals.idleFrames = golferAsset.battleSprite != null ? new Sprite[] { golferAsset.battleSprite } : Array.Empty<Sprite>();
                slot.visuals.hurtSprite = golferAsset.battleHurtSprite;
                slot.visuals.scale = golferAsset.battleSpriteScale > 0.05f && Mathf.Abs(golferAsset.battleSpriteScale - 1.0f) > 0.01f
                    ? golferAsset.battleSpriteScale
                    : 4.35f;
            }

            slot.actOptions = new List<ModularActOption>
            {
                new ModularActOption
                {
                    actName = "Check",
                    bottomBarNarration = "GOLFER - ATK 4 DEF 2. Mindlessly repeating the 18th hole from 2004.",
                    purifyGain = 0
                },
                new ModularActOption
                {
                    actName = "Analyze Swing",
                    bottomBarNarration = "You pointed out a hitch in his backswing. Golfer nods thoughtfully!",
                    enemySpeechReply = "Huh... keep my left arm straight? Not bad, kid.",
                    purifyGain = 50
                },
                new ModularActOption
                {
                    actName = "Heckle",
                    bottomBarNarration = "You yelled 'FORE!' during his putting routine. His next shot loses focus!",
                    enemySpeechReply = "HEY! Quiet on the green!",
                    purifyGain = 50,
                    nextTurnDamageMultiplier = 0.75f
                }
            };

            slot.conditionalDialogue = new List<ConditionalDialogueRule>
            {
                new ConditionalDialogueRule
                {
                    ruleLabel = "Attack 2 (Sand Trap Spread) Warning",
                    triggerType = DialogueTriggerType.WhenUsingAttackIndex,
                    attackPatternIndex = 1,
                    priority = 60,
                    dialogueLines = new List<string> { "WATCH THE SAND TRAP, KID!" }
                },
                new ConditionalDialogueRule
                {
                    ruleLabel = "Attack 3 (Ricochet Drive) Warning",
                    triggerType = DialogueTriggerType.WhenUsingAttackIndex,
                    attackPatternIndex = 2,
                    priority = 60,
                    dialogueLines = new List<string> { "BANKING THIS ONE OFF THE WALLS!" }
                },
                new ConditionalDialogueRule
                {
                    ruleLabel = "Low HP <= 50%",
                    triggerType = DialogueTriggerType.HealthAtOrBelowPercent,
                    thresholdValue = 50f,
                    priority = 75,
                    triggerOnce = true,
                    dialogueLines = new List<string> { "Huff... rough back nine today..." }
                }
            };

            return slot;
        }

        public static BattleEncounterSlot FromEnemyData(EnemyData data)
        {
            if (data == null) return CreateGolferPresetSlot(null);

            var slot = new BattleEncounterSlot
            {
                enemyType = data.enemyType,
                enemyName = !string.IsNullOrEmpty(data.enemyName) ? data.enemyName : "Enemy",
                baseProfile = data,
                maxHp = data.maxHp > 0 ? data.maxHp : 100,
                startingHp = 0,
                defense = data.defense,
                purgeMarkerHp = 0,
                damageAffinities = data.damageAffinities != null ? new List<DamageAffinityEntry>(data.damageAffinities) : new List<DamageAffinityEntry>(),
                attackOrder = data.attackOrder,
                purifyThreshold = data.purifyThreshold > 0 ? data.purifyThreshold : 100,
                speechBubbleOffset = data.speechBubbleOffset,
                speechBubbleSize = data.speechBubbleSize.x > 40f ? data.speechBubbleSize : new Vector2(188f, 68f),
                voiceBlipClip = data.voiceBlipClip
            };

            // Visuals
            if (data.idleFrames != null && data.idleFrames.Length > 0)
            {
                slot.visuals.idleFrames = data.idleFrames;
            }
            else if (data.battleSprite != null)
            {
                slot.visuals.idleFrames = new Sprite[] { data.battleSprite };
            }
            slot.visuals.idleFps = data.idleFps > 0f ? data.idleFps : 6f;
            slot.visuals.hurtSprite = data.battleHurtSprite;
            slot.visuals.purifyReadySprite = data.purifyReadySprite;
            slot.visuals.zeroHpExhaustedSprite = data.zeroHpExhaustedSprite;
            slot.visuals.scale = (data.battleSpriteScale > 0.05f && Mathf.Abs(data.battleSpriteScale - 1.0f) > 0.01f)
                ? data.battleSpriteScale
                : 4.35f;
            slot.visuals.stagePositionOffset = data.stagePositionOffset;
            slot.visuals.flipX = data.flipBattleSpriteX;
            slot.visuals.enableProceduralBob = data.enableProceduralBob;

            // Attack Patterns: use modularAttackPatterns if populated, otherwise wrap legacy primaryPattern + Golfer presets!
            if (data.modularAttackPatterns != null && data.modularAttackPatterns.Count > 0)
            {
                slot.attackPatterns = new List<EnemyAttackSlot>(data.modularAttackPatterns);
            }
            else
            {
                var legacySlot = new EnemyAttackSlot
                {
                    attackName = $"{slot.enemyName} Primary Attack",
                    presetType = EnemyAttackSlot.ConvertLegacyPattern(data.primaryPattern),
                    duration = data.dodgePhaseDuration > 0.5f ? data.dodgePhaseDuration : 4.8f,
                    spawnInterval = 0.28f,
                    projectileSpeed = data.bulletSpeed > 0.1f ? data.bulletSpeed : 3.2f,
                    coreDamage = data.bulletDamage > 0 ? data.bulletDamage : 8,
                    outerSplashDamage = Mathf.Max(1, (data.bulletDamage > 0 ? data.bulletDamage : 8) / 2),
                    projectileSprite = data.bulletSprite,
                    projectileColor = data.bulletColor.a > 0.01f ? data.bulletColor : Color.white
                };
                slot.attackPatterns = new List<EnemyAttackSlot> { legacySlot };
            }

            // Barks & Conditional Dialogue
            if (data.inBattleBarks != null && data.inBattleBarks.Length > 0)
            {
                slot.defaultBattleBarks = new List<string>(data.inBattleBarks);
            }
            if (data.conditionalDialogueRules != null && data.conditionalDialogueRules.Count > 0)
            {
                slot.conditionalDialogue = new List<ConditionalDialogueRule>(data.conditionalDialogueRules);
            }

            // ACT Options: use modularActOptions if populated, otherwise convert talkOptions seamlessly!
            if (data.modularActOptions != null && data.modularActOptions.Count > 0)
            {
                slot.actOptions = new List<ModularActOption>(data.modularActOptions);
            }
            else if (data.talkOptions != null && data.talkOptions.Count > 0)
            {
                slot.actOptions = new List<ModularActOption>();
                for (int i = 0; i < data.talkOptions.Count; i++)
                {
                    var t = data.talkOptions[i];
                    if (t == null) continue;
                    int pGain = t.purifyReward;
                    if (pGain <= 0 && !string.Equals(t.optionName, "Check", StringComparison.OrdinalIgnoreCase))
                    {
                        pGain = 50;
                    }
                    slot.actOptions.Add(new ModularActOption
                    {
                        actName = t.optionName,
                        bottomBarNarration = t.responseText,
                        purifyGain = string.Equals(t.optionName, "Check", StringComparison.OrdinalIgnoreCase) ? 0 : pGain,
                        nextTurnDamageMultiplier = t.chargeDelta < 0 ? 0.80f : 1.0f
                    });
                }
            }

            return slot;
        }
    }
}
