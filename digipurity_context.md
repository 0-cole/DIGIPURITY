# DIGIPURITY — Master Project Context & Agent Development Guide

**Last Updated**: 2026-10-21  
**Project Root**: `C:\Users\cdbla\Documents\GitHub\DIGIPURITY`  
**Git Remote**: `git@github.com:0-cole/DIGIPURITY.git` (`main` branch)  
**Engine**: Unity `6000.6.0f1` (Universal 2D / URP)  
**Author / Lead Developer**: Cole Blair  
**Design Reference**: `C:\Users\cdbla\Downloads\Capstone Game Idea (1).pdf`  

---

## 1. Executive Summary & Core Game Concept

- **Project**: **DIGIPURITY** is a 2D futuristic cyber-RPG following **Owen**, a pre-digital human pulled into a corrupted, decaying digital dimension.
- **Traversal & Exploration**: 4 distinct cybernetic zones filled with traversal puzzles, interconnected hallways, hidden secrets, and hostile **"AI Slop"** entities.
- **The Moral Invariant (Purify vs. Purge)**:
  - **[PURIFY]**: Restores power grids, repairs infrastructure, produces positive NPC dialogue, yields standard EXP ($1.0\times$), leading to the **Peaceful Rest Ending**.
  - **[PURGE]**: Deletes entities permanently, awards **$+50\%$ bonus EXP ($1.5\times$)** and accelerated stat growth, but causes progressive digital infrastructure collapse, leading to the **Dying Digital World Ending**.
- **Combat Philosophy (4-Way Mechanical Synthesis)**:
  1. **Limbus Company**: Expanding skill slots ($1 \to 6$), Coin flipping, Clashing, Sanity-adapted Charge pool, and click-to-swap Defense skills (`Guard` / `Evade` / `Counter`).
  2. **Undertale**: Real-time dodge box dodging during unopposed enemy attacks, $50\%$ precision slow-mode (`Shift` / `X`), and `invc` invulnerability frames.
  3. **Deltarune**: Proximity bullet **Graze** system, **Tension Points (TP)** pool ($0\text{–}250$ internal, $0\text{–}100\%$ display), $+40\text{ TP}$ ($16\%$) on Defending, spending TP to restore Charge.
  4. **OFF (Mortis Ghost)**: 4 zones with hard encounter caps (`25` per zone), zone level caps ($25 / 50 / 75 / 100$), and modular item loadouts acting as player competence kits.

---

## 2. World & Traversal Architecture

### 2.1 The Toby Fox Level Design Workflow (Strict Project Invariant)
- **NO Procedural Room Generation**: Do NOT use procedural room generators, runtime doorway cutters, or dynamic wall math scripts.
- **Static Visuals**: Floor sprites (e.g. [`WalkableSquare.png`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Sprites/Environment/WalkableSquare.png) or seamless repeating [`Floor_Tile.png`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Sprites/Environment/Floor_Tile.png)) placed directly in the scene. Both use $16\text{ PPU}$ ($2.0 \times 2.0\text{ units}$ per grid tile).
- **Invisible Wall Colliders (`obj_solid`)**:
  - Walls are standard static `BoxCollider2D` components placed over borders and obstacles (like Toby Fox's `obj_solid` in GameMaker).
  - Openings and doorways are created simply by leaving a gap between solid colliders.
  - Spawn shortcut: `GameObject` $\to$ `DIGIPURITY` $\to$ `Solid Wall (Undertale obj_solid)`.

### 2.2 Camera System ([`RoomCameraController.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/RoomCameraController.cs))
- **Strict 4:3 Aspect Ratio**: Enforced with solid black pillarbox/letterbox bars.
- **Fixed Size ($6.0\text{f}$)**: Orthographic size is locked at `6.0f` ($12.0\text{ units}$ tall $\times$ $16.0\text{ units}$ wide at 4:3). A $16 \times 16$ room fills the horizontal screen edge-to-edge with **zero black void**.
- **Torso-Centered Tracking**: `GetTargetCenter()` tracks `SpriteRenderer.bounds.center` (~chest height) rather than feet, keeping Owen vertically centered even when walking upward.
- **Undertale Room Clamping Rules**:
  - Small Room ($\le$ viewport): Camera remains stationary at room center with borders.
  - Large Room / Hallway: Camera follows Owen smoothly, strictly clamped to [`RoomBounds.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/RoomBounds.cs) so out-of-bounds void is never visible.

### 2.3 Area Door & Screen Fade Transitions ([`AreaDoor.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/AreaDoor.cs) & [`ScreenFader.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/ScreenFader.cs))
- **Two-Way Door ID Pairing**: Doors pair via `doorId` and `targetDoorId` with custom `arrivalSpawnOffset` and `arrivalFacing`.
- **Full-Screen Black Fader**: Solid black GUI overlay with zero UI prefab dependencies; freezes player movement (`player.InputLocked = true`), snaps camera, and unfreezes upon fade-in completion.
- **Deltarune "Egg Room" Divert Mechanic**:
  - Any door can toggle `hasSecretChance` with `secretChancePercent = 2.0f` (1 in 50 chance, identical to Deltarune Chapter 1 & 2).
  - When triggered, saves original target (`DivertedTargetDoorId`) and warps player to the secret room (`room_man` / Egg Room).
  - The exit door (`isSecretRoomExit = true`) automatically returns the player back to their original intended destination.

---

## 3. Combat System & Math Specifications

### 3.1 Skill Deck & Slot Expansion (Limbus Adaptation)
- **Deck Structure**:
  - `Skill1_PureDamage`: $3\times$ copies, baseline reliable attack, 2 coins.
  - `Skill2_HighPowerRare`: $1\times$ copy, higher power ceiling, 3 coins.
  - `Skill3_KeywordSpecialist`: Dynamic draw based on player's dominant keyword kit.
  - `Skill4_UltimateMove`: Rare draw, costs Charge (e.g. $20\text{ Charge}$), 4 coins.
  - `DefenseSkill`: Click-to-swap toggle on any active slot (`Guard`, `Evade`, or `Counter`).
- **Slot Progression**: Starts at $1$ slot on Turn 1, expanding by $+1$ slot each turn up to $6$ slots max.
- *Naming Invariant*: Do not hardcode final skill names—the author will name all skills.

### 3.2 Charge & Tension Math
- **Charge Pool**: Ranges from **$-45$ (Drained)** to **$+45$ (Overcharged)**.
- **Coin Heads Probability Formula**:
  $$P(\text{Heads}) = \text{Clamp}\left(0.50 + \frac{\text{Clamp}(\text{Charge}, -45, 45)}{100},\; 0.05,\; 0.95\right)$$
- **Clash Resolution**: Both combatants flip all active coins. Winner deals unopposed attack with remaining coins; loser loses $1$ coin permanently for that clash round.
- **Tension Points (TP)**: Internal range $0\text{–}250$ (`MaxTension = 250f`), displayed as $0\text{–}100\%$.
  - Defending adds flat $+40\text{ TP}$ ($16\%$).
  - Grazing bullets in dodge box adds proximity TP.
  - Spent via `SpendTensionToBoostCharge()` to raise Charge.

### 3.3 Status Keywords & 4 Rare Resonance Hybrid Fusions
- **Burn**: Deals `Potency` damage at turn end; `Count` drops by $1$.
- **Bleed**: Deals `Potency` damage whenever an attack coin is flipped; `Count` drops by $1$.
- **Poise**: `Potency`% chance for critical strike; `Count` drops by $1$ on crit.
- **Drainage**: Directly drains target's `Charge` pool.
- **Stutter & Glitch Threshold** (Tremor/Stagger adaptation):
  - **Stutter Burst** raises `currentGlitchThreshold` by `potency`.
  - When `currentHp <= currentGlitchThreshold`, unit enters **Glitched State** (`isGlitched = true`), losing their turn.
- **Hybrid Keyword Fusions** (Requires equipping Resonance Core items for both parent keywords):
  1. **Burn + Stutter $\to$ `StutterScorch`**: Turn-end Burn damage accelerates Glitch threshold rise.
  2. **Poise + Bleed $\to$ `CrimsonLacerate`**: Critical coin flips deal $2\times$ Bleed damage.
  3. **Drainage + Stutter $\to$ `StaticSeizure`**: Draining Charge directly raises enemy Glitch threshold by drained amount.
  4. **Burn + Poise $\to$ `CobaltCombustion` (Blue Burn)**: Consumes $1$ Poise Count at turn end to deal **$1.5\times$ Critical Burn damage** (blue flame visual).

---

## 4. Visual Art, Palette & Sprite Standards

### 4.1 Owen's Exact Color Palette
- **Outline / Boots / Eye Shadow**: `#080013` `(8, 0, 19)`
- **Hair**: `#2f201d` `(47, 32, 29)` & `#372825` `(55, 40, 37)`
- **Skin (Warm Golden Tan)**: `#b28666` `(178, 134, 102)` & shadow `#8c664a` `(140, 102, 74)`
- **Hoodie**: `#3f3f74` `(63, 63, 116)`, rib pattern/trim `#0f0f26` `(15, 15, 38)`, cuffs `#767676` `(118, 118, 118)`

### 4.2 Sprite Anatomy & Import Rules
- **Import Settings**: `filterMode: 0` (Point / no filter), `spritePixelsPerUnit: 100`, pivot bottom-center `(0.5, 0)`.
- **Walk/Idle Frame Alignment**: `Owen_Walk_Right_0` and `_2` ($158 \times 369$) are shifted vertically by $+8\text{px}$ relative to `Owen_Idle_Side_0` ($158 \times 354$). Always offset by $dy = +8$ so neck ($y = 120..151$) and collar stay connected.
- **Arms & Hands**: Always use Owen's actual user-drawn arm (extracted from `Owen_Idle_Side_0.png` at $x = 40..81, y = 175..268$) rotated around shoulder pivot with `Image.NEAREST`—never procedural stick arms.
- **Battle Sprites** in [`Assets/Sprites/Owen/Battle/`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Sprites/Owen/Battle): Separated sheets for Idle, Attack (Scythe), Defend, Hurt, and Downed.

---

## 5. Verified Codebase State & Active Files

### 5.1 Active Production Scripts (`Assets/Scripts/`)
| Path | Responsibility |
| :--- | :--- |
| [`Assets/Scripts/World/OverworldPlayerController.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/OverworldPlayerController.cs) | 4-direction walk/idle animation, $1.5\times$ diagonal speed, wall slide, New Input System, `InputLocked`, `SetFacingDirection`. |
| [`Assets/Scripts/World/RoomCameraController.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/RoomCameraController.cs) | Fixed 4:3 pillarbox/letterbox viewport, `setOrthographicSize = 6.0f`, torso centering, Undertale room clamp. |
| [`Assets/Scripts/World/RoomBounds.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/RoomBounds.cs) | Room perimeter tracking supporting Colliders, Renderers, and composite child walls. |
| [`Assets/Scripts/World/AreaDoor.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/AreaDoor.cs) | Two-way door transitions, arrival offset/facing, cooldowns, and Deltarune 1-in-50 Secret Egg Room divert/return. |
| [`Assets/Scripts/World/ScreenFader.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/ScreenFader.cs) | Zero-setup full-screen black transition fader with smooth alpha interpolation. |
| [`Assets/Scripts/World/Editor/WorldEditorTools.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/Editor/WorldEditorTools.cs) | 1-click Unity menu items: `Rebuild Clean WalkableSquare`, `Solid Wall`, `Room Trigger`, `Area Door`, `Egg Door`. |
| [`Assets/Scripts/Combat/CoinClashResolver.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/CoinClashResolver.cs) | Limbus coin flip resolution, heads probability from Charge, clash loops. |
| [`Assets/Scripts/Combat/CombatDeckManager.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/CombatDeckManager.cs) | Deck drawing, Skill 1/2/3/4 generation, Defense swap. |
| [`Assets/Scripts/Combat/CombatSkillSlot.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/CombatSkillSlot.cs) | Slot state, click-to-swap defense behavior. |
| [`Assets/Scripts/Combat/CombatStateMachine.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/CombatStateMachine.cs) | Combat phase management, turn-based slot expansion ($1 \to 6$). |
| [`Assets/Scripts/Combat/EquipmentLoadoutManager.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/EquipmentLoadoutManager.cs) | 5 equipment slots, hybrid keyword activation. |
| [`Assets/Scripts/Combat/KeywordProcessor.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/KeywordProcessor.cs) | Burn, Bleed, Poise, Drainage, and 4 Hybrid status calculations. |
| [`Assets/Scripts/Combat/StaggerAndGlitchSystem.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/StaggerAndGlitchSystem.cs) | HP, Stutter Bursts, Glitch Threshold elevation, Glitched turn skip. |
| [`Assets/Scripts/Combat/TensionAndChargeSystem.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/TensionAndChargeSystem.cs) | Deltarune 250 TP pool, Graze detection, $[-45, +45]$ Charge boost. |
| [`Assets/Scripts/Combat/SoulMovementLogic.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/SoulMovementLogic.cs) | Undertale dodge soul logic, slow-mode, border clamping, `invc` i-frames. |
| [`Assets/Scripts/World/ZoneProgressionData.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/ZoneProgressionData.cs) | 4 zones, 25 encounter caps, zone level caps ($25/50/75/100$), Purge $1.5\times$ EXP. |
| [`Assets/Scripts/Core/SaveSystem.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Core/SaveSystem.cs) | JSON save file management (`digipurity_save.json`). |
| [`Assets/Scripts/Core/TutorialManager.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Core/TutorialManager.cs) | 5-phase introductory onboarding state machine. |

### 5.2 EditMode Test Suites (`Assets/Tests/EditMode/`)
- [`AreaDoorTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/AreaDoorTests.cs) (Door matching, spawn offsets, Egg Room diversion/return, input locking)
- [`RoomCameraTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/RoomCameraTests.cs) (4:3 pillarbox math, stationary small rooms, hallway tracking, torso center)
- [`CombatSystemTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/CombatSystemTests.cs)
- [`OverworldMovementTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/OverworldMovementTests.cs)
- [`HybridCombatTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/HybridCombatTests.cs)
- [`DefenseSkillSwapTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/DefenseSkillSwapTests.cs)
- [`KeywordAndLevelProgressionTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/KeywordAndLevelProgressionTests.cs)
- [`HybridKeywordCombinationTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/HybridKeywordCombinationTests.cs)

---

## 6. Update History & Decision Log

| Date | Summary of Changes & Key Architectural Decisions |
| :--- | :--- |
| **2026-10-18** | **Core Architecture Established**: Designed Limbus $\times$ Deltarune $\times$ Undertale $\times$ OFF combat synthesis. Implemented `CoinClashResolver`, `KeywordProcessor`, `TensionAndChargeSystem`, and `SaveSystem`. |
| **2026-10-19** | **Owen Art & Animation Foundation**: Exported initial directional sprites from `.ase` files into `Assets/Sprites/Owen/`. Standardized Warm Golden Tan palette (`#b28666`). Configured `OverworldPlayerController` with $1.5\times$ diagonal speed and wall collision. |
| **2026-10-19** | **Scythe Battle Sprites Generated**: Extracted Owen's hand-drawn sleeves and head to generate pixel-perfect battle frames (Idle, Attack, Defend, Hurt, Downed) in `Assets/Sprites/Owen/Battle/`. |
| **2026-10-20** | **Golfer Sprite Imported**: Added first enemy visual asset (`Assets/Sprites/Golfer/Golfer_256.png`). Bound to wait on battle implementation until overworld rooms are complete. |
| **2026-10-20** | **4:3 Undertale Camera Built**: Implemented [`RoomCameraController.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/RoomCameraController.cs) with locked orthographic size, pillarbox/letterbox viewport bars, torso centering, and room boundary clamping. |
| **2026-10-20** | **Procedural System Discarded $\to$ Toby Fox Workflow Adopted**: Evaluated and removed overengineered `ModularRoom` procedural script due to mutual recursion stack overflows and Rect Tool dragging locks. Replaced with clean Toby Fox static map approach: manual floor sprites + invisible `BoxCollider2D` wall blocks (`obj_solid`). |
| **2026-10-20** | **Deltarune Doors & Secret Egg Room Implemented**: Researched Deltarune bytecode (`room_man`). Created [`AreaDoor.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/AreaDoor.cs) and [`ScreenFader.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/ScreenFader.cs) with 2-way door IDs, screen fading, player input locking, and 1-in-50 (2%) Secret Egg Room diversion and return. |
| **2026-10-20** | **WalkableSquare & Camera Calibration**: Reset `WalkableSquare` to 1:1 scale ($16 \times 16\text{ units}$), attached 4 solid border colliders, and calibrated camera `orthographicSize = 6.0f` so the room fills the 4:3 screen horizontally with zero black void. Fixed Unity 6 deprecation warnings. |
| **2026-10-21** | **Learned Workflow & Master Context Consolidation**: Merged all isolated context notes into `digipurity_context.md`. Cleaned redundant files. Configured project context rule for cross-conversation agent handoff. |

---

## 7. Immediate Next Steps for Next Agent

1. **Test Overworld WalkableSquare & Area Doors in Unity**:
   - In Unity, verify Owen bumps solidly into all 4 blue borders in `SampleScene.unity`.
   - Place a second room or hallway in `SampleScene` and test an `AreaDoor` transition with the black fade.
2. **Hook Battle Scene & Golfer Encounter**:
   - Wire up a trigger or encounter check that transitions Owen into combat with `Golfer_256.png`.
   - Connect the Combat UI Dashboard (Skill Slots, Coin Clashing, TP Bar, and Undertale Dodge Box) to [`CombatStateMachine.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/CombatStateMachine.cs).
