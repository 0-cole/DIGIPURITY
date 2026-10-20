# DIGIPURITY — Full Development Context & Handoff

**Last Updated**: 2026-10-19  
**Project Root**: `C:\Users\cdbla\Documents\GitHub\DIGIPURITY`  
**Capstone Design Reference**: `C:\Users\cdbla\Downloads\Capstone Game Idea (1).pdf`  
**Author / Developer**: Cole Blair  

---

## 1. Executive Summary & High-Level Objective

- **Project**: **DIGIPURITY** — A 2D futuristic RPG built in **Unity 6000.6.0f1** (Universal 2D / URP template).
- **Protagonist**: **Owen** (pre-digital human pulled into a corrupted, decaying digital world).
- **Core Loop**: Traverse 4 distinct digital zones, solve traversal puzzles, battle distorted **"AI Slop"** entities, build modular status/keyword kits via equipment items, and choose at the end of every fight whether to **Purify** or **Purge** the enemy.
- **The Moral Invariant (Purify vs. Purge)**:
  - **[PURIFY]**: Restores sector power and world infrastructure, alters NPC dialogue positively, awards standard EXP ($1.0\times$), and leads to the **Peaceful Rest Ending**.
  - **[PURGE]**: Deletes the entity, awards **$+50\%$ bonus EXP ($1.5\times$)** and faster stat growth, but triggers progressive infrastructure collapse leading to the **Dying Digital World Ending**.
- **Combat Philosophy**: A 4-way mechanical synthesis combining:
  1. **Limbus Company**: Skill slots expanding per turn ($1 \to 6$), Coin flips, Clashing, Sanity-to-Charge adaptation, and tap-to-swap Defense Skills (`Guard` / `Evade` / `Counter`).
  2. **Undertale**: Real-time red/blue soul bullet-hell dodging inside a bounding box when enemy attacks go uncontested or win a clash, including $50\%$ speed reduction when holding `Shift` / `X` and `invc` invulnerability frames.
  3. **Deltarune**: Clean UI layout, **Tension Points (TP)** pool ($0\text{–}250$ internal, displayed as $0\text{–}100\%$) earned by Defending ($+40\text{ TP} / 16\%$) and proximity bullet **Grazing**, spent to raise **Charge**.
  4. **OFF (Mortis Ghost)**: 4 main zones with per-zone random encounter caps, level caps per zone ($25 / 50 / 75 / 100$), and equipment items acting like competence/kit modifiers.

---

## 2. Critical Architecture & Gameplay Invariants

### 2.1 Skill Deck & Slot System (Limbus Adaptation)
- **Skill 1 (`Skill1_PureDamage`)**: Common draw ($3\times$ in base deck), reliable pure damage, base power + 2 coins.
- **Skill 2 (`Skill2_HighPowerRare`)**: Rarer draw ($1\times$ in base deck), higher ceiling, 3 coins.
- **Skill 3 (`Skill3_KeywordSpecialist`)**: Dynamically unlocked once the game detects the player's dominant keyword affinity. Inflicts high Potency and Count for that keyword.
- **Skill 4 (`Skill4_UltimateMove`)**: Very rare draw, costs **Charge** (e.g., $20\text{ Charge}$) to activate, rolls 4 coins with massive keyword payloads.
- **Defense Skill (`DefenseSkill`)**: Freely toggled on any active skill slot by clicking the slot or character portrait (`CombatSkillSlot.ToggleDefense()`). Clicking again cancels back to the primary attack skill.
- *Important Note*: Do not hardcode final skill names yet—the user will name all skills themselves.

### 2.2 Charge & Tension Economy (Limbus + Deltarune Math)
- **Charge Pool**: Ranges from **$-45$ (Drained)** to **$+45$ (Overcharged)** (replaces Limbus Sanity; distinct from Limbus's Charge keyword, though it can use the icon).
- **Coin Heads Probability**:
  $$P(\text{Heads}) = \text{Clamp}\left(0.50 + \frac{\text{Clamp}(\text{Charge}, -45, 45)}{100},\; 0.05,\; 0.95\right)$$
- **Clash Resolution**: Both sides flip all active coins. Higher total power ($\text{BasePower} + \sum \text{Heads} \times \text{CoinPower}$) wins the clash round; the loser permanently loses $1$ coin for that clash. Repeats until one side reaches $0$ coins, then the winner executes an unopposed attack with remaining coins.
- **Tension Points (TP)**: Internal range $0\text{–}250$ (`MaxTension = 250f`), displayed as $0\text{–}100\%$.
  - Defending adds flat $+40\text{ TP}$ ($16\%$).
  - Grazing bullets in the dodge box adds proximity TP.
  - TP is spent via `SpendTensionToBoostCharge(tensionCost, chargeBoost)` to raise Charge.

### 2.3 Status Keywords, Stutter & Glitch System
- **Burn**: Deals fixed damage equal to `Potency` at turn end; `Count` decreases by $1$.
- **Bleed**: Deals fixed damage equal to `Potency` whenever an attack coin is flipped; `Count` decreases by $1$.
- **Poise**: Grants `Potency`% chance to deal critical damage on hit; `Count` decreases by $1$ on crit (or turn end).
- **Drainage**: Directly lowers the target's `Charge` pool.
- **Stutter & Glitch Threshold** (Machine adaptation of Tremor & Stagger):
  - **Stutter** replaces Tremor; **Glitch Threshold** replaces Stagger Threshold.
  - Triggering a **Stutter Burst** (`TriggerStutterBurst(potency)`) raises `currentGlitchThreshold` by `potency`.
  - When `currentHp <= currentGlitchThreshold`, the unit enters the **Glitched State** (`isGlitched = true`), freezing up and skipping turns.

### 2.4 Equipment Loadout & 4 Rare Hybrid Keyword Fusions
- **Max Equipped Items**: $5$ slots (`MaxEquippedItems = 5`).
- **Total Item Pool (21 Items)**:
  - $4$ items per keyword ($5\text{ keywords} \times 4 = 20$? Specifically: $2$ standard keyword items + $2$ **Resonance Core** items per keyword, plus $5$ keywordless stat items).
- **Hybrid Keyword Activation**: A rare hybrid keyword activates **only** when the player equips Resonance Core items (`isResonanceCore = true`) for both required keywords simultaneously:
  1. **Burn + Stutter $\to$ `StutterScorch`**: Turn-end Burn damage accelerates Glitch threshold elevation.
  2. **Poise + Bleed $\to$ `CrimsonLacerate`**: Critical coin flips deal $2\times$ Bleed damage.
  3. **Drainage + Stutter $\to$ `StaticSeizure`**: Draining enemy Charge directly raises their Glitch threshold by the drained amount.
  4. **Burn + Poise $\to$ `CobaltCombustion` (Blue Burn)**: Consumes $1$ Poise Count at turn end to deal **$1.5\times$ Critical Burn damage** (uses a blue flame icon).

### 2.5 Zones, Encounter Caps & Level Caps (OFF Adaptation)
- **4 Main Zones**: Unlimited random encounters up to `maxEncountersPerZone = 25` per zone.
- **Zone Level Caps**:
  - **Zone 1**: Level Cap **25**
  - **Zone 2**: Level Cap **50**
  - **Zone 3**: Level Cap **75**
  - **Zone 4**: Level Cap **100**
- **EXP Disparity**: Purging grants `Mathf.RoundToInt(baseExp * 1.5f)`; Purifying grants `baseExp`.

---

## 3. Recent Modifications & Verified Codebase State

### 3.1 Verified C# Scripts (`Assets/Scripts/`)
- **Combat (`Assets/Scripts/Combat/`)**:
  - [`CoinClashResolver.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/CoinClashResolver.cs) — Seedable RNG coin flipping, Charge-based heads probability, clash round loop.
  - [`CombatDeckManager.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/CombatDeckManager.cs) — Skill 1, Skill 2, Defense, dynamic Skill 3 (Keyword Specialist), and Skill 4 (Charge-cost Ultimate).
  - [`CombatSkillSlot.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/CombatSkillSlot.cs) — Slot wrapper and `SkillSlotManager` with click-to-toggle Defense swap (`ToggleDefense()`).
  - [`CombatStateMachine.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/CombatStateMachine.cs) — Turn phase transitions and per-turn slot expansion (`1` to `6`).
  - [`EquipmentLoadoutManager.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/EquipmentLoadoutManager.cs) — 5-slot equipment manager and `EvaluateActiveHybridKeywords()` resonance detector.
  - [`KeywordProcessor.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/KeywordProcessor.cs) — Status dictionary and execution math for Burn, Bleed, Poise, Drainage, and the 4 Hybrid Keywords.
  - [`StaggerAndGlitchSystem.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/StaggerAndGlitchSystem.cs) — HP, Glitch threshold elevation via Stutter Burst, and Glitched state.
  - [`TensionAndChargeSystem.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/TensionAndChargeSystem.cs) — Deltarune $250\text{ Max TP}$ math, $+40\text{ TP}$ Defend, Graze increments, and $[-45, +45]$ Charge conversion.
  - [`SoulMovementLogic.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/SoulMovementLogic.cs) & [`SoulMovementBehaviour.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/SoulMovementBehaviour.cs) — Undertale red/blue soul movement, $50\%$ slow on `Shift`/`X`, $1.5\times$ diagonal speed multiplier, box border clamping, and `invc` i-frames.
- **World & Core (`Assets/Scripts/World/` & `Assets/Scripts/Core/`)**:
  - [`OverworldPlayerController.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/OverworldPlayerController.cs) — Overworld movement with $1.5\times$ diagonal speed multiplier (`diagonalSpeedMultiplier = 1.5f`), animated step rate scaling, New & Legacy Input support, `WalkableSquare` perimeter clamping with wall-sliding, and 4-directional sprite playback.
  - [`ZoneProgressionData.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/ZoneProgressionData.cs) — Zone tracking ($1\text{–}4$), encounter cap ($25$), Purge $1.5\times$ EXP, and Zone level caps ($25, 50, 75, 100$).
  - [`SaveSystem.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Core/SaveSystem.cs) — JSON persistence (`digipurity_save.json`).
  - [`TutorialManager.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Core/TutorialManager.cs) — 5-stage progressive onboarding state tracker.

### 3.2 EditMode Unit Tests (`Assets/Tests/EditMode/`)
- [`CombatSystemTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/CombatSystemTests.cs)
- [`OverworldMovementTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/OverworldMovementTests.cs)
- [`HybridCombatTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/HybridCombatTests.cs)
- [`DefenseSkillSwapTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/DefenseSkillSwapTests.cs)
- [`KeywordAndLevelProgressionTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/KeywordAndLevelProgressionTests.cs)
- [`HybridKeywordCombinationTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/HybridKeywordCombinationTests.cs)

### 3.3 Sprite & Art Assets (`C:\Users\cdbla\Documents\GitHub\DIGIPURITY`)
- **Already Exported & Imported (`Assets/Sprites/Owen/`)**:
  - `Owen_Idle_Down_0.png` & `Owen_Walk_Down_0..3.png` (+ `_Sheet.png`)
  - `Owen_Idle_Up_0.png` & `Owen_Walk_Up_0..3.png` (+ `_Sheet.png`)
  - Configured with `filterMode: 0` (Point / no filter) and bottom-center pivot `(0.5, 0)`.
- **New Aseprite Files Created by User in Root (Pending Export to Unity)**:
  - `ForwardIdleSprite.ase` (replaced `IdleSprite.ase`)
  - `LeftSideWalkingSprite.ase`
  - `RightSideWalkingSprite.ase`
  - `SideIdleSprite.ase`
  - Modified: `Walking Forward.ase`, `BackSprite Idle.ase`, `BackSprite Walk.ase`

---

## 4. Immediate Next Steps

1. **Import New Side & Updated Aseprite Files into Unity**:
   - Extract frames and spritesheets from `LeftSideWalkingSprite.ase`, `RightSideWalkingSprite.ase`, `SideIdleSprite.ase`, and updated `ForwardIdleSprite.ase` / `BackSprite*.ase` into `Assets/Sprites/Owen/` with pixel-perfect `.meta` files.
   - Update [`OverworldPlayerController.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/OverworldPlayerController.cs) to support dedicated `walkLeftFrames` and `walkRightFrames` (since `RightSideWalkingSprite.ase` now exists alongside `LeftSideWalkingSprite.ase`).
2. **Build the First Playable Overworld & Combat Scene (World 1 / Zone 1)**:
   - Set up Owen's GameObject in the starting scene with `OverworldPlayerController`.
   - Build the Combat UI Canvas (Deltarune + Limbus dashboard hybrid: Skill Slots, Coin Flip display, TP bar $0\text{–}100\%$, Charge gauge $-45\text{ to }+45$, and the Undertale Dodge Box).
   - Add a `BulletEmitter` prefab for enemy attacks during `CombatTurnPhase.BulletHellDefense`.
3. **Git Commit & Remote Push (When Off School Wi-Fi)**:
   - Stage and commit the new `.ase` files and exported sprites.
   - When connected outside the school network (which blocks GitHub), authenticate `gh auth login -h github.com`, add the remote, and push:
     ```pwsh
     git remote add origin https://github.com/0-cole/DIGIPURITY.git
     git push -u origin main
     ```
