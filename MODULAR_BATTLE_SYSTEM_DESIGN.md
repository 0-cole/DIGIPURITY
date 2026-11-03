# DIGIPURITY — Modular Inspector-Driven Battle System Specification

**Author / Lead Designer**: Cole Blair  
**Status**: **IMPLEMENTED & ACTIVE IN PRODUCTION** (`2026-11-02`)  
**Related Production Files**:
- [`ModularBattleData.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/ModularBattleData.cs) *(Multi-enemy `BattleEncounterSlot`, Golfer Baseline Attack Presets, `CombinedAttackRule`, `ConditionalDialogueRule`, `EnemyVisualSetup`, `ModularActOption`, Damage Affinities)*
- [`BattleEncounterConfig.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/BattleEncounterConfig.cs) & [`BattleEncounterConfigEditor.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/Editor/BattleEncounterConfigEditor.cs) *(Dynamic `1..4` Enemy Slot Inspector Builder + 1-click Golfer Preset & Sample Custom Attack Prefab Generator)*
- [`AttackDamageZone.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/AttackDamageZone.cs) *(Per-hitbox damage zones for Custom Attack Prefabs: Core Heavy DMG vs. Splash Light DMG vs. 0-DMG Warning Telegraph + Cyan/Orange/Heal/Purify rules & Scene Gizmos)*
- [`BattleStageController.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/BattleStageController.cs) *(Universal Multi-Enemy Battle Engine + Stepped-Corner Deltarune Speech Bubble Renderer)*
- [`BattleDodgeBox.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/BattleDodgeBox.cs) & [`BattleBullet.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/BattleBullet.cs) *(Simultaneous multi-enemy emitters, Golfer baseline presets including Sand Trap Core/Splash & Ricochet Drive, and Custom Attack Prefab spawner)*
- [`BattleTutorialEncounter.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/BattleTutorialEncounter.cs) *(Decoupled Initial Onboarding Driver — Inspector-editable fields)*
- [`EnemyData.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/EnemyData.cs) *(Per-Enemy ScriptableObject Profile)*
- [`Deltarune_Enemy_Speech_Reference.png`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Docs/References/Deltarune_Enemy_Speech_Reference.png) *(Canonical Enemy Dialogue Speech Bubble Visual Reference)*

---

## 0. Core Architectural Philosophy

1. **Strict Separation of Universal Engine vs. Encounter Data**:
   - `BattleStageController` is the **universal runtime battle engine** used for *every* encounter in DIGIPURITY. It must **never** contain hardcoded enemy dialogue, tutorial scripts, or boss-specific hacks.
   - Special scripted encounters (such as the initial Golfer tutorial via `BattleTutorialEncounter`, mid-battle boss cutscenes, or scripted phase transitions) attach as **modular optional Encounter Drivers** or **serialized Inspector fields** on the encounter asset/prefab.
2. **100% Author-Driven Inspector Control**:
   - Every stat, enemy slot, sprite framing tweak, attack pattern, hitbox damage multiplier, conditional dialogue line, speech bubble coordinate, Dodge Box coordinate/size, and `ACT` / `PURIFY` threshold is exposed as **instantiated, expandable `[Serializable]` fields in the Unity Inspector** so Cole can craft encounters, jokes, tragedies, and bullet-hell patterns directly without touching boilerplate code.

---

## 1. Dynamic Multi-Enemy Encounter Slots (`1..N` Enemies)

An encounter component (`BattleEncounterConfig` on any `OverworldEnemy` / trigger, or a `BattleEncounterAsset` ScriptableObject) exposes an expandable enemy list:

- **Inspector Workflow**:
  - Setting **`Enemy Count`** (`1` to `4`, or clicking `+ Add Enemy Slot`) dynamically creates `N` numbered Inspector foldouts: **`Enemy [1]`**, **`Enemy [2]`**, **`Enemy [3]`**, etc.
  - Each slot can either reference a shared `EnemyData` template **with per-instance Inspector overrides** (e.g., tweaking Enemy 2's starting HP, dialogue, or stage offset right in the scene) OR define an inline bespoke enemy directly.
- **Stage Formation & Target Selection**:
  - **Auto-Formation Preset** (`Single`, `VerticalColumn`, `DiagonalStagger`, `BossCenterWithMinions`) + **Manual Per-Slot Stage Offset `(X, Y)`** with live Scene-View Gizmo handles.
  - When choosing `[PURGE]` or `[ACT]`, the bottom angled cyber bar lists all active living/unpurified enemies (`Enemy 1..N`) with each enemy's mini **HP Bar** (with `0 HP` Purge Marker) and **PURIFY % Bar**, navigated via `[W/S]` / `[Up/Down]` with Owen's pulsing cyan heart cursor.
  - If Enemy 1 is Purified or Purged mid-fight while Enemy 2 is still active, Enemy 1 exits cleanly (radiant white light or cyber spark implosion) while combat seamlessly continues against the remaining enemies.

```csharp
[Serializable]
public class BattleEncounterSlot
{
    [Header("1-3. Slot Identity & Classification")]
    public EnemyClassification enemyType = EnemyClassification.BasicEnemy; // BasicEnemy, Miniboss, ZoneBoss, FinalBoss
    public string enemyName = "Corrupted Entity";
    [Tooltip("Optional base profile; any inline overrides below take priority.")]
    public EnemyData baseProfile;

    [Header("5. Health, Defense & Damage Affinities")]
    public int maxHp = 100;
    public int defense = 2;
    public int purgeMarkerHp = 0; // Default 0 HP required before lethal PURGE is unlocked
    public List<DamageAffinityEntry> damageTypeMultipliers;

    [Header("7. Sprite Slots & Visual Framing")]
    public EnemyVisualSetup visuals;

    [Header("4. Solo & Synergy Attack Patterns")]
    public List<EnemyAttackSlot> attackPatterns;

    [Header("6. Ordered & Conditional Speech Bubble Dialogue")]
    public Vector2 speechBubbleOffset = new Vector2(-150f, -20f);
    public Vector2 speechBubbleSize = new Vector2(220f, 84f);
    public List<ConditionalDialogueRule> dialogueRules;

    [Header("8. Custom ACT Actions & Purify Thresholds")]
    public int purifyThreshold = 100; // Standard = 100; tough enemies/bosses can use 150, 200, or phase-locked thresholds
    public List<ModularActDefinition> actOptions;
}
```

---

## 2. Enemy Type / Classification (`Enemy [n]` Tiering)

Each enemy slot `n` specifies `EnemyClassification`:

| Tier | Gameplay & UI Behavior |
| :--- | :--- |
| **`BasicEnemy`** | Standard overworld encounter; counts toward the Zone's `25` OFF-style encounter cap; standard `100` Purify threshold; allows `FLEE` (unless zone-locked). |
| **`Miniboss`** | Disables `FLEE`; supports multi-wave conditional attack pools, custom intro bark before Turn 1, and unique item/equipment drops. |
| **`ZoneBoss`** | *(e.g. Triple T)* Full boss presentation: custom BGM + dynamic DSP pitch/distortion phases, optional wide boss HP header, multi-phase transitions at HP/Purify milestones, immune to standard one-shot bypasses. |
| **`FinalBoss`** | *(Master AI: MCP / AM Hybrid)* Supports full-stage environmental takeovers, custom Dodge Box geometry morphing mid-attack, and ending branch triggers (`Purify` Peaceful Rest vs. `Purge` Infrastructure Collapse). |

---

## 3. Enemy Naming & Duplicate Disambiguation

- **Inspector Field**: `enemyName` per slot `n`.
- **Automatic Suffixing Toggle** (`autoLetterDuplicates = true`): If two `"Slop Bot"` enemies are in the same battle, automatically labels them `"Slop Bot A"` and `"Slop Bot B"` (or custom author suffixes like `"Left"` / `"Right"`).
- **Dynamic Name Color States in Target List**:
  - **Pure White**: Standard active state.
  - **Glowing Yellow (`#FFF226`)**: `currentPurify >= purifyThreshold` (Ready to `[PURIFY]`!).
  - **Warning Red-Orange (`#FF401A`)**: `currentHp <= purgeMarkerHp` (`0 HP` helpless state — Lethal `[PURGE]` ready!).

---

## 4. Author-Crafted Custom Attack Patterns, Combined Attacks & Hitbox Damage Zones

Designed specifically so Cole can **hand-craft unique bullet-hell attacks, visual effects, and multi-enemy combos** in Unity without rigid enum limitations:

### 4.1 Prefab / Timeline-Based Custom Attack Builder (`BattleAttackPattern` Prefab / Asset)
Instead of being restricted to hardcoded C# enums, each attack slot takes a **`BattleAttackPattern`** asset or **Attack Wave Prefab** that Cole can build visually in the Scene/Prefab Editor:
- **Drop-in Spawners & Choreography**: Place emitter nodes (`LineEmitter`, `AimedBurstEmitter`, `SweepingLaserHazard`, `BouncingPropHazard`, `AnimatedSpriteHazard`, `ScreenShakeTrigger`, `VfxFlashTrigger`) inside an Attack Prefab timed along delays or Unity `AnimationClip` / `Timeline` curves!
- **Editable Dodge Box Per Attack**:
  - Every attack pattern has:
    - `bool overrideDodgeBoxTransform`
    - `Vector2 targetDodgeBoxOffset` *(where the Dodge Box moves on screen for this specific attack!)*
    - `Vector2 targetDodgeBoxSize` *(e.g. narrow vertical corridor `1.8 x 4.2`, tiny pressure box `2.2 x 2.2`, wide floor arena `5.5 x 2.4`)*
    - `float boxMorphSpeed` *(smoothly slides/resizes the Dodge Box before or *during* the bullet wave!)*

### 4.2 Per-Part Hitbox Damage Zones (`AttackDamageZone` Component)
Attach `AttackDamageZone` to **any sub-part** of an attack prefab so different parts of the *same* attack deal different damage and effects:
- **Sweet-Spot / Core Hitbox** *(e.g. direct club head, laser core, giant fist center)*: `damage = 22`, `knockbackScreenShake = 0.25f`, `ignoreExtraIFrames = false`.
- **Outer / Splash / Graze Sparks** *(e.g. shockwave dust, trailing embers, glancing edge)*: `damage = 6`, `appliesStatus = Burn/Stutter`.
- **Telegraph Warning Zone** *(e.g. flashing red exclamation box 0.6s before beam fires)*: `damage = 0`, `grantsGrazeTp = false`.
- **Movement-Conditional Hazards** *(Undertale Cyan / Orange rules)*:
  - `StandardWhite` (always hurts), `CyanStopToAvoid` (0 damage if Owen's soul is stationary!), `OrangeMoveToAvoid` (0 damage if Owen's soul is moving!), `GreenHeal/PurifyOrb` (heals HP or adds Purify % on touch!).

### 4.3 Combined Multi-Enemy Synergy Attacks (`CombinedAttackRule`)
When `2+` enemies share the stage, the Encounter Inspector includes a **`Combined Enemy Attacks`** list:
- **Rule Definition**:
  - `requiredAliveSlots` (e.g. `Enemy [1]` + `Enemy [2]`, or `EnemyType A` + `EnemyType B`).
  - `triggerChanceOrTurnSchedule` (e.g. every 3rd turn, or 40% chance while both are alive).
  - `combinedAttackPrefab` — A special **Dual/Team Attack Prefab** (for example: Enemy 1 fires horizontal sweeping bars that force Owen's soul up/down *while* Enemy 2 lobs aimed AoE explosions, OR a bespoke fused combo attack unique to that duo!).
  - **Fallback Rule**: The instant one partner is Purified or Purged, the combo rule automatically deactivates and the survivor falls back to their solo `attackPatterns` (or unlocks an enraged/mournful `"Solo Desperation Attack"`!).

### 4.4 Attack Selection Order per Enemy
- `AttackOrderMode`: `SequentialLoop` (`Attack 1 -> Attack 2 -> Attack 3 -> repeat`), `WeightedRandom`, `HPPhaseGated` (unlocks `Attack 3` only when `HP <= 50%`), or `ScriptedTurnIndex`.

---

## 5. Health, Defense, Damage Types & Status Resistances

Each enemy slot `n` exposes full combat math fields in the Inspector:
- **`maxHp`** & **`startingHp`** (allows pre-damaged/corrupted enemies).
- **`defense`** (flat or percentage mitigation) & **`purgeMarkerHp`** (default `0 HP`, where enemy stops attacking and becomes vulnerable to lethal `[PURGE]`).
- **`DamageType` Multipliers** (editable list of affinities):
  - `PhysicalScythe` *(Owen's standard timed `[PURGE]` swing — scaled by the Undertale/Deltarune Accuracy Timing Bar!)*
  - `DigitalGlitch / Stutter`
  - `ThermalBurn`
  - `CrimsonBleed`
  - `ChargeDrainage`
  - Per-type multiplier (`0.0x` Immune, `0.5x` Resistant, `1.0x` Neutral, `1.5x–2.0x` Weak — triggers `"WEAK!"` / `"RESIST"` floating pixel popup!).

---

## 6. Ordered & Conditional Enemy Dialogue Engine

Every enemy slot has an Inspector-ordered list of **`ConditionalDialogueRule`** entries so Cole can write reactive, hilarious, or gut-wrenching lines without code:

### Supported Inspector Trigger Conditions (Combinable!)
1. **`TurnSequence`**: Plays in exact custom order (`Turn 1`, `Turn 2`, `Turn 3...`, with toggle `playOnce = true` or `loopSequence`).
2. **`HealthThreshold`**: Fires immediately or on next turn when `HP <= X%` (e.g. `HP <= 50%`, `HP == 0%` exhausted state).
3. **`ActiveAttackPattern`**: Tied directly to *which attack pattern is about to fire*! (e.g., if Enemy selects `Attack #3: Ultimate Brainrot Beam`, they ALWAYS say the specific warning/trash-talk bark paired with `Attack #3` right before the Dodge Box opens!).
4. **`PurifyThresholdReached`**: Fires when `Purify >= 50%` or `Purify >= 100%` (e.g. enemy's hostility cracks and their dialogue softens).
5. **`SpecificActPerformed`**: Triggered directly after Owen uses a specific `ACT` (e.g. `"Heckle"` vs `"Compliment"`).
6. **`PlayerAttackAccuracyReaction`**: Optional snarky bark if Owen lands a `CRITICAL!!` bullseye vs. completely `MISS`ing the timing bar!
7. **`PartyStateCondition`**: Fires when an ally in Slot `m` was just **Purged** (*"Did you just delete him?!"*) vs. **Purified** (*"Wait... his static cleared up..."*).
8. **`Priority` Integer**: Higher priority rules (e.g. `0 HP Exhausted = 100`, `Attack #3 Telegraph = 80`, `HP < 50% = 70`) cleanly override default sequential chatter (`Priority = 10`).

---

## 7. Sprite Slots & Inspector Visual Calibration Tools

Each enemy slot `n` has an **`EnemyVisualSetup`** drawer so any hand-drawn sprite from LibreSprite drops in and looks right immediately:

- **Sprite / Animation Slots**:
  - `idleFrames[]` + `idleFps` *(works seamlessly whether you drop in 1 static PNG or a 4–8 frame idle loop!)*
  - `attackWindupFrames[]` *(plays during speech telegraph / attack cast)*
  - `hurtSprite` *(shown during damage shake)*
  - `purifyReadySprite` *(optional calmer posture once Purify hits 100%)*
  - `zeroHpExhaustedSprite` *(slumped/glitching posture at 0 HP when bobbing stops)*
  - `overworldPurifiedNpcSprite` *(sprite used if enemy converts into a friendly room NPC after Purification)*
- **Inspector Framing & Alignment Controls**:
  - `stagePositionOffset (X, Y)` & `uniformScale` (with `pixelSnap` toggle)
  - `flipX` & `nativeFacingDirection` (`FacesLeft` / `FacesRight`)
  - `groundLineOffset` *(aligns feet shadow/ground plane regardless of transparent padding in PNG)*
  - `sortingOrderOffset` *(controls overlap in multi-enemy formations)*
  - `proceduralBob` toggle (`bobFrequency`, `bobAmplitude` — can be disabled for enemies that have full frame-by-frame pixel idle animations!).

---

## 8. Inspector-Driven `ACT` Builder, Purify Thresholds & Deltarune Speech Bubble Spec

### 8.1 Modular `ACT` Builder (Per Enemy)
Cole can add any number of custom `ACT` choices per enemy in the Inspector:
- **`actName`**: Button label in the bottom bar submenu (e.g. `"Check"`, `"Analyze Swing"`, `"Unplug Feed"`, `"Offer Empathy"`).
- **`unlockCondition`**: Available from Turn 1, OR unlocked only after another ACT / HP threshold (e.g. `"Stabilize Core"` unlocks after `"Diagnose Glitch"`!).
- **`purifyDelta`**: How much Purify progress this adds (`+35`, `+50`, `+100`, or `-20` if you pick an insulting ACT!).
- **`tpReward`** & **`enemyStatEffect`**: Optional gameplay buff/debuff (e.g., `nextEnemyAttackDamageMultiplier = 0.75f`, `nextEnemyBulletSpeedMultiplier = 0.8f`, or `tired = true`).
- **`bottomBarNarrationText`** (`[TextArea]`): Flavor text displayed inside the bottom angled cyber bar (supports multi-page strings!).
- **`immediateEnemySpeechResponse`** (`[TextArea]`): Exact line the enemy immediately speaks back in their speech bubble!
- **`TargetScope`**: `SingleEnemy` or `AllEnemies` (group ACTs!).

### 8.2 Authentic Deltarune Enemy Speech Bubble Visual Spec
Per [`Deltarune_Enemy_Speech_Reference.png`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Docs/References/Deltarune_Enemy_Speech_Reference.png):

```
      ┌──────────────────────────┐
    ┌─┘                          └─┐
    │  It was                      │
    │  glowing.                    ├──►   [ Enemy Sprite ]
    │  The voice                   │
    │  was glowing!                │
    └─┐                          ┌─┘
      └──────────────────────────┘
```

- **Placement & Editable Offset**:
  - Positioned to the **LEFT** of the speaking enemy (`speechBubbleOffset` & `speechBubbleSize` editable per-enemy in the Inspector so tall/wide bosses never get covered!).
  - In multi-enemy battles, multiple enemies can display staggered speech bubbles simultaneously (top enemy bubble upper-left, bottom enemy bubble lower-left) without overlapping.
- **Exact Pixel Geometry**:
  - **Pure White (`#FFFFFF`) interior** with **stepped pixel-notched corners** (`4px`/`6px` staircase corner cutouts in `640x480` pixel space — **no** heavy black outer border around the white rectangle, popping cleanly against the dark battle void/grid!).
  - **Right-Pointing Pixel Triangle Pointer Tail** protruding from the right edge of the bubble directly toward the speaking enemy's mouth/head height.
  - **Typography & Voice**: Pure black (`#000000`) crisp pixel font (`VCR OSD Mono` / custom pixel font), left-aligned with generous margin padding, typewriter reveal synced to per-enemy `voiceBlipClip` and `voicePitchRange`.

---

## 9. Additional Essential Conditions & Inspector Hooks

To ensure the modular system handles every creative scenario across all 5 zones of DIGIPURITY:

1. **Editable Battle Boxes (Live in Inspector + Per-Attack Morphing)**:
   - **Dodge Box**: Global default `dodgeBoxOffset` & `dodgeBoxSize` on `BattleStageController` + per-enemy override on `EnemyData` + per-attack dynamic morphing on `BattleAttackPattern`, all visualized with live neon-green wireframe Gizmos in Unity's Scene View.
   - **UI & Speech Boxes**: Inspector-editable `speechBubblePixelOffset` and `bottomBarPixelOffset` in `640x480` virtual canvas coordinates.
2. **Custom Battle Background Grid & Environment Layer**:
   - Per-encounter toggle for subtle Deltarune-style scrolling/pulsing dark cyber background grid (color-tinted per Zone: e.g. purple/indigo for `abandon.war3`, neon nursery pastels for `Playpen`, museum crimson/gold for `Warped Museum`) plus custom background props.
3. **Encounter Song & Conditional Song Manipulation Rules (Live Now in `EnemyData` & `BattleStageController` + Multi-Enemy Ready)**:
   - Inspector-configurable base song (`battleBgm` / `overrideEncounterSong`), starting `bgmVolume` and `bgmStartPitch`, optional `enableAutoLowHpSlowdown` toggle (for automatic pitch/distortion slowdown toward `0 HP` and forgiveness tempo recovery on positive `ACT`), plus **`List<BattleSongRule> conditionalSongRules`** (`stageSongRules` on stage):
     - **Conditions (`SongTriggerCondition`)**: `EnemyHpAtOrBelowPercent`, `OwenHpAtOrBelowPercent`, `PurifyAtOrAbovePercent`, `OnTurnNumberReached`, `OnSpecificActUsed`, `OnZeroHpPurgeReady`, `OnPurgeExecuted`, `OnPurifyExecuted`.
     - **Song Actions (`SongActionType`)**: `SwitchToNewSong` (with crossfade duration and optional `syncPlaybackTimeOnSwitch` for beat-matched layered stems), `ModifyCurrentSong` (smoothly glide pitch/speed, volume, cyber distortion, and low-pass muffle), `TapeStopToSilence`, `FadeOutAndStop`, `PauseSong`, and `ResumeSong`.
4. **Mid-Battle Phase Transitions & Scripted Events**:
   - `List<BattlePhaseTrigger>`: At `HP <= X%`, `Purify >= Y%`, or `Turn == N`, can swap enemy sprites, change BGM track, spawn reinforcement enemy into Slot 2, or play a short in-battle dialogue exchange.
5. **Victory, Rewards & Moral World-State Persistence**:
   - Per-enemy `baseExp`, `purgeBonusExpMultiplier` (`1.5x` default per DIGIPURITY moral invariant), `digiDollarsReward`, `worldStateFlagKey` (saved to `SaveSystem` so the overworld remembers whether this specific entity/boss was **Purified** or **Purged**, adjusting power-grid stability and spawning their `PurifiedNPC` counterpart in the room!).
