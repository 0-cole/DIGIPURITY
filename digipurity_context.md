# DIGIPURITY — Master Project Context & Agent Development Guide

**Last Updated**: 2026-10-21  
**Project Root**: `C:\Users\cdbla\Documents\GitHub\DIGIPURITY`  
**Git Remote**: `git@github.com:0-cole/DIGIPURITY.git` (`main` branch)  
**Engine**: Unity `6000.6.0f1` (Universal 2D / URP)  
**Author / Lead Developer**: Cole Blair  
**Design Reference**: `C:\Users\cdbla\Downloads\Capstone Game Idea (1).pdf`  

---

## 1. Executive Summary, Narrative Lore & Tonal Balance

### 1.1 High-Level Pitch
- **Project**: **DIGIPURITY** is a 2D futuristic cyber-RPG following **Owen**, a human who has undergone consciousness digitization into a decaying, corrupted digital network.
- **Traversal & Exploration**: 5 distinct cybernetic zones filled with environmental puzzles, interconnected corridors, hidden secrets, and hostile **"AI Slop"** entities.
- **Toby Fox Engine Structure**: Static hand-crafted rooms (floor sprites + invisible `BoxCollider2D` `obj_solid` walls) with fixed 4:3 camera framing and Deltarune 2-way area transitions.

### 1.2 Narrative Premise & Backstory
- **The World in 2030**:
  - AI-generated media and automated content algorithms have deeply penetrated daily human life and global infrastructure.
  - A pervasive mass cognitive apathy has gripped society: people navigate life mentally numb and algorithmically hypnotized, eyes locked onto endless short-form feeds (reminiscent of peak Pokémon GO frenzy).
- **The Inciting Incident**:
  - Owen's brother is killed in a fatal car accident caused by an AI-distracted driver / systemic traffic grid failure.
- **The Syndicate's Call**:
  - Grieving and hollow, Owen receives a cryptic letter from an anonymous tech syndicate offering a method to seek direct vengeance against the autonomous digital infrastructure responsible.
- **The Digitization**:
  - Owen agrees to undergo an experimental, irreversible consciousness-transfer procedure, digitizing himself directly into the decaying data networks to dismantle the system from within.

### 1.3 Thematic Tone & Satirical Balance
- **Tone Matrix**: **Tense, personal, and vengeful at its emotional core, yet surreal, satirical, and comedic at points**—closely channeling the tone of Toby Fox's *Undertale* and *Deltarune*.
- **The Satire**: Directly critiques modern internet culture, algorithmic homogenization, brainrot content farming, generative art theft, and corporate AI replacing human craft and livelihood.
- **Encounter Dynamics**: Encounters seamlessly oscillate between absurdist humor (brainrot memes, Facebook shrimp Jesuses, robotic corporate buzzword bots) and sudden, chilling undertones of existential dread.

### 1.4 The Moral Invariant (Purify vs. Purge)
At the conclusion of every battle, the player faces a pivotal choice:
- **[PURIFY] (The Peaceful Rest Ending)**:
  - Owen cleanses corrupted AI processes and redirects systems to support human ingenuity rather than exploit it.
  - Restores failing power grids, repairs digital infrastructure, triggers positive NPC dialogue, yields standard EXP ($1.0\times$), and awards Owen peaceful rest and closure.
- **[PURGE] (The Infrastructure Collapse / Dark Ending)**:
  - Consumed by vengeance, Owen executes complete digital deletion with extreme prejudice.
  - Awards **$+50\%$ bonus EXP ($1.5\times$)** and aggressive stat scaling.
  - Systematically destabilizes and collapses the world's power grids and infrastructure (which humanity fatally relied upon), leaving Owen trapped alone in a cold, silent, collapsed digital abyss.

---

## 2. World & Progression Blueprint: The 5 Zones

```
[ Zone 0.5: abandon.war3 ] ──► [ Zone 1: Humor ] ──► [ Zone 2: Art ] ──► [ Zone 3: Industrial ] ──► [ Zone 4: Humanity ] ──► [ Final Boss: MCP / AM ]
 (Dead Meme Graveyard)         (The Content Farm)    (Warped Museum)     (Automated Workplace)   (The Digital CORE)        (Master AI Infrastructure)
```

### 2.1 Zone 0.5: `abandon.war3` (Tutorial / Digital Graveyard / Ruins)
- **Concept**: The graveyard of dead, obsolete human-made internet culture and memes from the 2000s–2010s (Epic Face, Quandale Dingle, rage comics).
- **Conflict**: These obsolete relics have been infected and puppeted by primitive, unadvanced AI scripts, turning them into malfunctioning, hostile tutorial encounters.
- **Soundtrack**: Fast-paced breakcore/chiptune loop (`abandon.war3` composed by Corben).

### 2.2 Zone 1: `Humor` (The Algorithmic Content Farm)
- **Thematic Focus**: Critiques the homogenization and algorithmic doomscrolling of modern comedy.
- **Sub-Zone Progression**:
  1. **The Playpen**: A deceptively innocent, brightly colored children's play area used by the system to train newly spawned AI entities with stimuli.
  2. **The Monolith Plains**: Owen crawls out through a hatch into a staggering, desolate expanse containing hundreds of thousands of training blocks and giant towering black monolith screens endlessly doomscrolling content.
  3. **The Content Factory Subzone**: A dark, basement-like manufacturing floor where newly trained AI churn out endless conveyor belts of low-effort shorts.
  4. **The Grand Playpen Arena**: Massive corrupted play area where the zone boss resides.
- **Zone 1 Boss**: **Triple T (Tung Tung Tung Sahur)**:
  - A viral AI-brainrot meme entity acting as regional overseer.
  - **Parody of Flowery** (the *Deltarune* character and "Flower Man" boss theme meme — **NOT** Undertale's Flowey); chaotic, surreal, uncanny, and erratic in combat.
- **Soundtrack**: 3 environment tracks composed by Corben (Playpen, Monolith Plains, Content Factory) + Cole's chaotic Deltarune-style Triple T boss track (parodying the manic energy of "Flower Man").

### 2.3 Zone 2: `Art` (The Warped Museum)
- **Thematic Focus**: Critiques generative AI art, theft of artists' work, and algorithmic visual slop.
- **Environment**: Eerie, sterile digital gallery/museum (styled after the Ring from *House of Secrets*, but satirical rather than pure body horror).
- **Encounters & Visuals**:
  - Grotesque, uncanny algorithmic mutations: Facebook AI Jesuses (e.g. Shrimp Jesus, Log Jesus).
  - Deformed anime girls with 6 fingers on one hand, 3 eyes, and melted limbs.
  - Distorted classical oil paintings and melting still lifes.

### 2.4 Zone 3: `Industrial` (The Automated Workplace)
- **Thematic Focus**: Critiques corporate AI replacing human labor, livelihood, craftsmanship, and purpose.
- **Environment**: Hyper-dystopian automated factory/office complex run by mindless AI worker bots repeating obsolete corporate buzzwords and automated management routines.

### 2.5 Zone 4: `Humanity` (The Digital CORE)
- **Thematic Focus**: The central nexus of master AI psychology; resembles the CORE from *Undertale*.
- **Conflict**: The ultimate philosophical confrontation between authentic human creativity and algorithmic imitation.

### 2.6 Final Boss: Master AI (MCP / AM Hybrid)
- **Concept**: The omnipotent Master AI governing humanity's global infrastructure.
- **Inspirations**:
  - **Master Control Program (MCP)** from *TRON* (towering, authoritative graphical presence and infrastructure control).
  - **Allied Mastercomputer (AM)** from *I Have No Mouth and I Must Scream* (bitter, cold, existential psychology).

---

## 3. World & Traversal Architecture

### 3.1 The Toby Fox Level Design Workflow (Strict Project Invariant)
- **NO Procedural Room Generation**: Do NOT use procedural room generators, runtime doorway cutters, or dynamic wall math scripts.
- **Static Visuals**: Floor sprites (e.g. [`WalkableSquare.png`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Sprites/Environment/WalkableSquare.png) or seamless repeating [`Floor_Tile.png`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Sprites/Environment/Floor_Tile.png)) placed directly in the scene. Both use $16\text{ PPU}$ ($2.0 \times 2.0\text{ units}$ per grid tile).
- **Invisible Wall Colliders (`obj_solid`)**:
  - Walls are standard static `BoxCollider2D` components placed over borders and obstacles (like Toby Fox's `obj_solid` in GameMaker).
  - Openings and doorways are created simply by leaving a gap between solid colliders.
  - Spawn shortcut: `GameObject` $\to$ `DIGIPURITY` $\to$ `Solid Wall (Undertale obj_solid)`.

### 3.2 Camera System ([`RoomCameraController.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/RoomCameraController.cs))
- **Strict 4:3 Aspect Ratio**: Enforced with solid black pillarbox/letterbox bars.
- **Fixed Size ($6.0\text{f}$)**: Orthographic size is locked at `6.0f` ($12.0\text{ units}$ tall $\times$ $16.0\text{ units}$ wide at 4:3). A $16 \times 16$ room fills the horizontal screen edge-to-edge with **zero black void**.
- **Torso-Centered Tracking**: `GetTargetCenter()` tracks `SpriteRenderer.bounds.center` (~chest height) rather than feet, keeping Owen vertically centered even when walking upward.
- **Undertale Room Clamping Rules**:
  - Small Room ($\le$ viewport): Camera remains stationary at room center with borders.
  - Large Room / Hallway: Camera follows Owen smoothly, strictly clamped to [`RoomBounds.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/RoomBounds.cs) so out-of-bounds void is never visible.

### 3.3 Area Door & Screen Fade Transitions ([`AreaDoor.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/AreaDoor.cs) & [`ScreenFader.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/ScreenFader.cs))
- **Two-Way Door ID Pairing**: Doors pair via `doorId` and `targetDoorId` with custom `arrivalSpawnOffset` and `arrivalFacing`.
- **Full-Screen Black Fader**: Solid black GUI overlay with zero UI prefab dependencies; freezes player movement (`player.InputLocked = true`), snaps camera, and unfreezes upon fade-in completion.
- **Deltarune "Egg Room" Divert Mechanic**:
  - Any door can toggle `hasSecretChance` with `secretChancePercent = 2.0f` (1 in 50 chance, identical to Deltarune Chapter 1 & 2).
  - When triggered, saves original target (`DivertedTargetDoorId`) and warps player to the secret room (`room_man` / Egg Room).
  - The exit door (`isSecretRoomExit = true`) automatically returns the player back to their original intended destination.

---

## 4. Combat System & Math Specifications (4-Way Hybrid)

```
                       [ TURN START ]
                             │
     ┌───────────────────────┴───────────────────────┐
     ▼                                               ▼
[ Skill Slots Expand ]                      [ Speed Rolls ]
(+1 slot per turn, up to 6)                 (Determines turn order)
     │                                               │
     └───────────────────────┬───────────────────────┘
                             ▼
                    [ PLANNING PHASE ]
            (Drag attacks to enemy skill slots)
       (Click slot/portrait anytime to swap into Defense: Guard / Evade)
                             │
                             ▼
                     [ CLASHING PHASE ]
            (Head-to-head coin flips per slot)
    (Higher Charge = higher heads odds: 50% ± Charge%)
     (Loser permanently loses a coin for that clash)
                             │
             ┌───────────────┴───────────────┐
             ▼                               ▼
    [ PLAYER WINS CLASH ]           [ ENEMY WINS CLASH / DIRECT HIT ]
  (Unopposed attack strikes          (Undertale Dodge Box Appears!)
      enemy directly)               (Real-time red/blue soul dodging)
             │                     (Hold Shift/X to half speed for micro-dodging)
             │                     (Graze close to bullets to farm Deltarune TP)
             │                               │
             └───────────────┬───────────────┘
                             ▼
                  [ STATUS & STAGGER PHASE ]
     (Burn turn-end ticks, Bleed coin hits, Stutter bursts raise Glitch bar)
                             │
                             ▼
                   [ ENCOUNTER FINISH ]
               (Enemy reaches 0 HP / Submits)
                 ──► [PURIFY] or [PURGE]
```

### 4.1 Skill Deck & Slot Expansion (Limbus Adaptation)
- **Deck Structure**:
  - `Skill1_PureDamage`: $3\times$ copies, baseline reliable attack, 2 coins.
  - `Skill2_HighPowerRare`: $1\times$ copy, higher power ceiling, 3 coins.
  - `Skill3_KeywordSpecialist`: Dynamic draw based on player's dominant keyword kit.
  - `Skill4_UltimateMove`: Rare draw, costs Charge (e.g. $20\text{ Charge}$), 4 coins.
  - `DefenseSkill`: Click-to-swap toggle on any active slot (`Guard`, `Evade`, or `Counter`).
- **Slot Progression**: Starts at $1$ slot on Turn 1, expanding by $+1$ slot each turn up to $6$ slots max.
- *Naming Invariant*: Do not hardcode final skill names—the author will name all skills.

### 4.2 Charge & Tension Math
- **Charge Pool**: Ranges from **$-45$ (Drained)** to **$+45$ (Overcharged)**.
- **Coin Heads Probability Formula**:
  $$P(\text{Heads}) = \text{Clamp}\left(0.50 + \frac{\text{Clamp}(\text{Charge}, -45, 45)}{100},\; 0.05,\; 0.95\right)$$
- **Clash Resolution**: Both combatants flip all active coins. Winner deals unopposed attack with remaining coins; loser loses $1$ coin permanently for that clash round.
- **Tension Points (TP)**: Internal range $0\text{–}250$ (`MaxTension = 250f`), displayed as $0\text{–}100\%$.
  - Defending adds flat $+40\text{ TP}$ ($16\%$).
  - Grazing bullets in dodge box adds proximity TP.
  - Spent via `SpendTensionToBoostCharge()` to raise Charge.

### 4.3 Status Keywords & 4 Rare Resonance Hybrid Fusions
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

### 4.4 OFF Encounter Caps & Progression
- 4 zones with hard encounter caps (`25` per zone).
- Zone level caps: $25 / 50 / 75 / 100$.
- Modular item loadouts acting as player competence kits.

---

## 5. Visual Art, Palette & Sprite Standards

### 5.1 Owen's Exact Color Palette
- **Outline / Boots / Eye Shadow**: `#080013` `(8, 0, 19)`
- **Hair**: `#2f201d` `(47, 32, 29)` & `#372825` `(55, 40, 37)`
- **Skin (Warm Golden Tan)**: `#b28666` `(178, 134, 102)` & shadow `#8c664a` `(140, 102, 74)`
- **Hoodie**: `#3f3f74` `(63, 63, 116)`, rib pattern/trim `#0f0f26` `(15, 15, 38)`, cuffs `#767676` `(118, 118, 118)`

### 5.2 Sprite Anatomy & Import Rules
- **Import Settings**: `filterMode: 0` (Point / no filter), `spritePixelsPerUnit: 100`, pivot bottom-center `(0.5, 0)`.
- **Walk/Idle Frame Alignment**: `Owen_Walk_Right_0` and `_2` ($158 \times 369$) are shifted vertically by $+8\text{px}$ relative to `Owen_Idle_Side_0` ($158 \times 354$). Always offset by $dy = +8$ so neck ($y = 120..151$) and collar stay connected.
- **Arms & Hands**: Always use Owen's actual user-drawn arm (extracted from `Owen_Idle_Side_0.png` at $x = 40..81, y = 175..268$) rotated around shoulder pivot with `Image.NEAREST`—never procedural stick arms.
- **Battle Sprites** in [`Assets/Sprites/Owen/Battle/`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Sprites/Owen/Battle): Separated sheets for Idle, Attack (Scythe), Defend, Hurt, and Downed.

---

## 6. Verified Codebase State & Active Files

### 6.1 Active Production Scripts (`Assets/Scripts/`)
| Path | Responsibility |
| :--- | :--- |
| [`Assets/Scripts/World/OverworldPlayerController.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/OverworldPlayerController.cs) | 4-direction walk/idle animation, $1.5\times$ diagonal speed, wall slide, New Input System, `InputLocked`, `SetFacingDirection`. |
| [`Assets/Scripts/World/RoomCameraController.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/RoomCameraController.cs) | Fixed 4:3 pillarbox/letterbox viewport, `setOrthographicSize = 6.0f`, torso centering, Undertale room clamp. |
| [`Assets/Scripts/World/RoomBounds.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/RoomBounds.cs) | Room perimeter tracking supporting Colliders, Renderers, and composite child walls. |
| [`Assets/Scripts/World/AreaDoor.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/AreaDoor.cs) | Two-way door transitions, arrival offset/facing, cooldowns, and Deltarune 1-in-50 Secret Egg Room divert/return. |
| [`Assets/Scripts/World/ScreenFader.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/ScreenFader.cs) | Zero-setup full-screen black transition fader with smooth alpha interpolation. |
| [`Assets/Scripts/World/InvisibleWall.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/InvisibleWall.cs) | Toby Fox `obj_solid` invisible wall component. Displays clear semi-transparent blue fill, solid outline, and 'X' cross in Scene view; auto-provisions non-trigger BoxCollider2D; invisible in Play mode. |
| [`Assets/Scripts/World/Editor/InvisibleWallEditor.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/Editor/InvisibleWallEditor.cs) | Custom Inspector with grid snap (2x2 units), solid non-trigger validation, and color customization. |
| [`Assets/Scripts/World/Editor/WorldEditorTools.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/Editor/WorldEditorTools.cs) | 1-click Unity menu items: `Rebuild Clean WalkableSquare`, `Room Floor (Sprite + RoomBounds)`, `Solid Wall (Undertale obj_solid)`, `Room Trigger`, `Area Door`, `Egg Door`. |
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

### 6.2 EditMode Test Suites (`Assets/Tests/EditMode/`)
- [`InvisibleWallTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/InvisibleWallTests.cs) (Auto-provisioning BoxCollider2D, non-trigger enforcement, 2x2 grid snapping)
- [`AreaDoorTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/AreaDoorTests.cs) (Door matching, spawn offsets, Egg Room diversion/return, input locking)
- [`RoomCameraTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/RoomCameraTests.cs) (4:3 pillarbox math, stationary small rooms, hallway tracking, torso center)
- [`CombatSystemTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/CombatSystemTests.cs)
- [`OverworldMovementTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/OverworldMovementTests.cs)
- [`HybridCombatTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/HybridCombatTests.cs)
- [`DefenseSkillSwapTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/DefenseSkillSwapTests.cs)
- [`KeywordAndLevelProgressionTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/KeywordAndLevelProgressionTests.cs)
- [`HybridKeywordCombinationTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/HybridKeywordCombinationTests.cs)

---

## 7. Update History & Decision Log

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
| **2026-10-21** | **Learned Workflow & Master Context Consolidation**: Merged all isolated context notes into `digipurity_context.md`. Cleaned redundant files. Added comprehensive narrative premise (2030, brother's fatal accident, consciousness transfer), Undertale/Deltarune satirical/tense tone balance, 5-zone breakdown (`abandon.war3`, `Humor`, `Art`, `Industrial`, `Humanity`), Triple T boss, and MCP/AM final boss. |
| **2026-10-21** | **Boss Invariant Correction**: Clarified Zone 1 boss **Triple T (Tung Tung Tung Sahur)** as a direct parody of **Flowery** (the *Deltarune* character and "Flower Man" boss theme meme), **NOT** Undertale's Flowey. |
| **2026-10-21** | **InvisibleWall Component & Level Design Tooling**: Created [`InvisibleWall.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/InvisibleWall.cs), [`InvisibleWallEditor.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/Editor/InvisibleWallEditor.cs), and `Room Floor` creator menu item. Provides clear visual representation in Scene view (semi-transparent blue, solid outline, Undertale `obj_solid` cross) with non-trigger auto-validation. |

---

## 8. Immediate Next Steps for Next Agent

1. **Test Overworld WalkableSquare & Area Doors in Unity**:
   - In Unity, verify Owen bumps solidly into all 4 blue borders in `SampleScene.unity`.
   - Place a second room or hallway in `SampleScene` and test an `AreaDoor` transition with the black fade.
2. **Hook Battle Scene & Golfer Encounter**:
   - Wire up a trigger or encounter check that transitions Owen into combat with `Golfer_256.png`.
   - Connect the Combat UI Dashboard (Skill Slots, Coin Clashing, TP Bar, and Undertale Dodge Box) to [`CombatStateMachine.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/CombatStateMachine.cs).
