# DIGIPURITY — Master Project Context & Agent Development Guide

**Last Updated**: 2026-10-26  
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

### 1.5 The Human-Crafted Creative Invariant (Placeholders vs. Final Assets)
- **Authentic Human Art & Music**: Because **DIGIPURITY** is fundamentally a critique of generative AI replacing human expression, **all final creative assets (music, sprites, artwork) must be genuinely human-made** by the developer and human collaborators.
- **Role of AI-Generated Non-Code Assets**: Any non-code asset generated during development (such as the BeepBox JSON battle theme or extrapolated sprite frames) is strictly a **temporary placeholder / greybox prototype** or a technical study reference, and will be replaced with human-crafted assets. Always build audio/visual systems so custom human-made `.ogg`/`.wav` tracks and `.ase`/`.png` sprites can be swapped in effortlessly.

---

### 1.6 Core Mechanics & Entities Quick-Reference Table (Fast Lookup for Agents)
When requested to inspect, tweak, or bugfix any game system, jump directly to the target C# script listed below. **Do NOT parse .unity scene YAML files or inspect raw image pixels.**

| Concept / Entity | Primary Script Path | Purpose & Key Variables |
| :--- | :--- | :--- |
| **Golfer / Overworld Enemies** | [Assets/Scripts/Combat/OverworldEnemy.cs](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/OverworldEnemy.cs) | Overworld agro, sight radius, pursuit speed (rushSpeed, rushAcceleration, maxRushSpeed), sprite facing flip (CalculateFacingFlip), encounter transition. |
| **Enemy Data & Stats** | [Assets/Scripts/Combat/EnemyData.cs](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/EnemyData.cs) | ScriptableObject defining enemy HP, Stagger, coins, dialogue barks, TALK options, and attack patterns (Golfer_Data.asset). |
| **Owen Movement & Overworld** | [Assets/Scripts/Overworld/OverworldPlayerController.cs](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Overworld/OverworldPlayerController.cs) | Owen's 8-way overworld movement, sprint multiplier (1.5x), collision raycasting, directional walk animations. |
| **Battle Stage & Flow** | [Assets/Scripts/Combat/BattleStageController.cs](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/BattleStageController.cs) | 3rd-person side-by-side battle manager: menu states, command selection (FIGHT/PURGE, TALK/ACT, ITEM, PURIFY), clash sequence, music pitch/distortion DSP. |
| **Coin Clash Math** | [Assets/Scripts/Combat/CoinClashResolver.cs](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/CoinClashResolver.cs) | Limbus coin spinning, heads probability from Charge, clash resolution, coin breakage, proportional damage. |
| **Dodge Box & Soul (Bullet Hell)** | [Assets/Scripts/Combat/BattleDodgeBox.cs](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/BattleDodgeBox.cs)<br>[Assets/Scripts/Combat/SoulController.cs](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/SoulController.cs) | Undertale central dodge arena, soul movement, bullet collision, graze detection, TP generation. |
| **Doors & Transitions** | [Assets/Scripts/World/AreaDoor.cs](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/AreaDoor.cs) | 2-way paired door transitions, spawn offsets, and 1-in-50 (2%) secret room diversion. |
| **Camera & Viewport** | [Assets/Scripts/World/RoomCameraController.cs](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/RoomCameraController.cs) | 4:3 fixed orthographic camera (size = 9.0f), torso centering, room boundary clamping, letterbox/pillarbox bars. |
| **Invisible Solid Walls** | [Assets/Scripts/World/InvisibleWall.cs](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/InvisibleWall.cs) | Toby Fox static solid wall collider (obj_solid) using BoxCollider2D. |
| **Spared Golfer NPC** | [Assets/Scripts/World/PurifiedGolferNPC.cs](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/PurifiedGolferNPC.cs) | Overworld friendly NPC dialogue typewriter, text advance, character voice blips (snd_txt_golfer.wav). |
| **Screen Transitions & Fades** | [Assets/Scripts/World/ScreenFader.cs](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/ScreenFader.cs) | Full-screen fade in/out for room doors and battle transitions. |

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
- **Fixed Size ($9.0\text{f}$)**: Orthographic size is calibrated at `9.0f` ($18.0\text{ units}$ tall $\times$ $24.0\text{ units}$ wide at 4:3, corresponding to $12 \times 9$ grid tiles at $16\text{ PPU}$). Provides natural player framing so Owen does not dominate the screen while preserving Owen's full sprite scale without artificial shrinkage.
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
- **Outline / Boots / Eye Shadow**: `#000000` `(0, 0, 0)` (Pure Black — updated from purplish black `#080013`)
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
| [`Assets/Scripts/World/RoomCameraController.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/RoomCameraController.cs) | Fixed 4:3 pillarbox/letterbox viewport, calibrated `setOrthographicSize = 9.0f` ($24 \times 18$ unit view / $12 \times 9$ tiles), torso centering, Undertale room clamp. |
| [`Assets/Scripts/World/RoomBounds.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/RoomBounds.cs) | Room perimeter tracking supporting Colliders, Renderers, and composite child walls. |
| [`Assets/Scripts/World/AreaDoor.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/AreaDoor.cs) | Two-way door transitions, arrival offset/facing, cooldowns, and Deltarune 1-in-50 Secret Egg Room divert/return. |
| [`Assets/Scripts/World/ScreenFader.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/ScreenFader.cs) | Zero-setup full-screen black transition fader with smooth alpha interpolation. |
| [`Assets/Scripts/World/InvisibleWall.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/InvisibleWall.cs) | Toby Fox `obj_solid` invisible wall component. Displays clear semi-transparent blue fill, solid outline, and 'X' cross in Scene view; auto-provisions non-trigger BoxCollider2D; invisible in Play mode. |
| [`Assets/Scripts/World/OverworldCreature.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/OverworldCreature.cs) | Overworld NPC Creature component. Auto-provisions and enforces solid non-trigger `BoxCollider2D` preventing Owen from phasing through characters; supports 2D footprint sizing (bottom 35% at feet) and dynamic depth sorting. |
| [`Assets/Scripts/World/OverworldNPC.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/OverworldNPC.cs) | Dedicated alias/subclass for `OverworldCreature` appearing as "Overworld NPC" in Unity's Add Component menu. |
| [`Assets/Scripts/World/Editor/InvisibleWallEditor.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/Editor/InvisibleWallEditor.cs) | Custom Inspector with grid snap (2x2 units), solid non-trigger validation, and color customization. |
| [`Assets/Scripts/World/Editor/OverworldCreatureEditor.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/Editor/OverworldCreatureEditor.cs) | Custom Inspector for NPC creatures featuring 1-click buttons: "Fit Feet Footprint", "Fit Full Body", and "Ensure Solid Non-Trigger". |
| [`Assets/Scripts/World/Editor/RoomBoundsEditor.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/Editor/RoomBoundsEditor.cs) | Room connection inspector displaying connected overlapping rooms and a 1-click button to strip floor-level colliders for open doorways. |
| [`Assets/Scripts/World/Editor/WorldEditorTools.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/Editor/WorldEditorTools.cs) | 1-click Unity menu items: `Rebuild Clean WalkableSquare`, `Room Floor (Sprite + RoomBounds)`, `Solid Wall (Undertale obj_solid)`, `NPC Creature (Solid)`, `Spawn Golfer Battle Encounter (Overworld + Stage)`, `Room Trigger`, `Area Door`, `Egg Door`. |
| [`Assets/Scripts/Combat/EnemyData.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/EnemyData.cs) | Modular ScriptableObject profile for ANY enemy encounter: name, overworld/battle/hurt/bullet sprites, stats, in-battle speech barks, custom Deltarune TALK/ACT options (with TP/Charge rewards), and bullet patterns (`RainFromTop`, `AimedSpreadBurst`, `AlternatingSide`, `RadialSpiral`). |
| [`Assets/Scripts/Combat/OverworldEnemy.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/OverworldEnemy.cs) | Universal overworld enemy component. Drag-and-drop onto ANY enemy GameObject; features agro on sight (`sightRadius`), continuous speed acceleration (`rushSpeed` = 4.0 u/s, `rushAcceleration` = 7.5 u/s², `maxRushSpeed` = 13.5 u/s outrunning Owen's 10.8 u/s max diagonal sprint), corrected sprite facing for left-facing native sprites (`spriteFacesLeftByDefault`, `CalculateFacingFlip`), rushes toward Owen to initiate combat, executes 3-blink screen & chest cyan soul alert, and passes `EnemyData` into battle. |
| [`Assets/Scripts/Combat/EnemyBattleAnimator.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/EnemyBattleAnimator.cs) | Universal 3rd-person enemy battle animator: unlit URP 2D shader (`Universal Render Pipeline/2D/Sprite-Unlit-Default`), procedural sine bob, hurt vibration, white light explosion on Purify (`PlayPurifyWhiteLightExplosion`: radiant ascent, expansion dissolve, 48 ascending white/gold/cyan particles), and cyber spark implosion on Purge (`PlayPurgeImplosion`: torso-anchored, horizontal slice jitter, singularity collapse, 38 multi-color sparks). |
| [`Assets/Scripts/Combat/BattleStageController.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/BattleStageController.cs) | Pure Deltarune 3rd-person turn-based battle engine isolated in deep void `(1000, 1000, 0)`: command selection (`PURGE`, `ACT`, `ITEM`, `PURIFY`), dynamic audio slowdown & distortion with positive ACT forgiveness recovery, 0 HP Purge Marker execution, helpless 1.5s calm dodge box when enemy is at 0 HP, tutorial anti-spam debounce, authentic Deltarune OnGUI HUD with prominent 18px text and zero clipping, dynamic ACT submenu, combatant scaling (Owen 0.85, Golfer tripled to 4.35 on shared ground line $Y = 998.8\text{f}$), direct disk asset loaders, and resilient auto-provisioning via `EnsureBattleStageExists()`. |
| [`Assets/Scripts/Combat/OverworldBattleTrigger.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/OverworldBattleTrigger.cs) | Overworld trigger on Golfer sprite: locks player input, screen blackout, 3-blink cyan soul alert over Owen's chest, smooth fade-in to battle. |
| [`Assets/Scripts/Combat/OwenBattleAnimator.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/OwenBattleAnimator.cs) | Owen 3rd-person battle sprite controller: bulletproof direct disk sprite loader (`LoadSpriteDirect`), unlit URP 2D shader, idle loop, scythe attack with held clash frame (0.25s), defend posture, hurt shake, downed pose. |
| [`Assets/Scripts/Combat/GolferBattleAnimator.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/GolferBattleAnimator.cs) | Golfer battle animator subclassing `EnemyBattleAnimator` with tripled scale ($4.35\text{f}$) and direct disk sprite auto-load fallback. |
| [`Assets/Scripts/Combat/BattleCombatantUI.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/BattleCombatantUI.cs) | Overhead skill slots and digital coin tray above combatants' heads: skill badge, uppercase name, spinning coins, Heads/Tails resolution, shattered coin display. |
| [`Assets/Scripts/Combat/BattleDodgeBox.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/BattleDodgeBox.cs) | Central bullet hell arena (`3.8x3.0` units) with neon green border, cyan soul inside, 0.85s reaction grace buffer before bullet waves, bullet spawning patterns, graze TP detection, and smooth expand/shrink animations. |
| [`Assets/Scripts/Combat/BattleBullet.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/BattleBullet.cs) | Pure white circle bullet projectile: velocity movement, graze radius awarding Deltarune TP, hit radius dealing damage with i-frames. |
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

### 6.2 Combat Sprites (`Assets/Sprites/Combat/`)
| File | Dimensions | Role |
| :--- | :--- | :--- |
| [`CyanSoul.png`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Sprites/Combat/CyanSoul.png) | 200x200 px | Owen's cyan SOUL used during overworld encounter alert (3-blink chest flash) and central dodge box bullet hell. |
| [`WhiteBullet_16.png`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Sprites/Combat/WhiteBullet_16.png) | 16x16 px | Pure white pixelated circle bullet for high-visibility bullet dodging. |
| [`Icon_Purge.png`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Sprites/Combat/Icon_Purge.png) | 32x32 px | Deltarune PURGE scythe icon with orange blade and cyber glow. |
| [`Icon_Fight.png`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Sprites/Combat/Icon_Fight.png) | 32x32 px | Deltarune classic FIGHT sword icon. |
| [`Icon_Act.png`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Sprites/Combat/Icon_Act.png) | 32x32 px | Deltarune ACT speech balloon icon. |
| [`Icon_Item.png`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Sprites/Combat/Icon_Item.png) | 32x32 px | Deltarune ITEM pouch bag icon. |
| [`Icon_Mercy.png`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Sprites/Combat/Icon_Mercy.png) | 32x32 px | Deltarune MERCY hand / cross icon. |
| [`Owen_Battle_Portrait.png`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Sprites/Combat/Owen_Battle_Portrait.png) | 34x34 px | Owen mini battle portrait for the bottom-left Deltarune character box. |
| [`Icon_Sword.png`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Sprites/Combat/Icon_Sword.png) | 16x16 px | Skill slot badge icon. |
| [`Coin_Heads.png`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Sprites/Combat/Coin_Heads.png) | 32x32 px | Digital cyber Limbus coin (glowing cyan/gold circuit core). |
| [`Coin_Tails.png`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Sprites/Combat/Coin_Tails.png) | 32x32 px | Digital cyber Limbus coin (dim steel core). |
| [`Coin_Broken.png`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Sprites/Combat/Coin_Broken.png) | 32x32 px | Shattered red-glitched coin when a coin is lost in clash. |
| [`Coin_Spin_0..3.png`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Sprites/Combat/Coin_Spin_0.png) | 32x32 px | 4-frame horizontal coin spin rotation animation. |

### 6.1 World Traversal, Camera & Dialogue Scripts (`Assets/Scripts/World/`)
- [`OverworldPlayerController.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/OverworldPlayerController.cs) (Input System package WASD/gamepad movement, $1.5\times$ diagonal speed, multi-room footprint clamping, foot-depth sorting)
- [`RoomCameraController.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/RoomCameraController.cs) (Undertale 4:3 camera, locked orthographic size 9.0f, smooth room follow)
- [`RoomBounds.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/RoomBounds.cs) (Multi-room compound geometry and doorway detection)
- [`AreaDoor.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/AreaDoor.cs) (2-way room transitions, 1-in-50 Secret Egg Room chance)
- [`ScreenFader.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/ScreenFader.cs) (Persistent singleton screen fade overlay)
- [`OverworldCreature.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/OverworldCreature.cs) & [`OverworldNPC.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/OverworldNPC.cs) (Solid creature physics, foot collision, depth sorting)
- [`LucasSign.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/LucasSign.cs) (Doorway lore sign at `(10.5, -0.8)`, 4-frame animated glitching white sign scaled 2.0x1.0 units [double width of Owen's legs, height of legs], faint prompt, solid foot collider, Deltarune text window reading *"purify the world, owen. or burn it all to the ground. - lucas."*, and digital fizzle dissolution)
- [`PurifiedGolferNPC.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/PurifiedGolferNPC.cs) (Overworld peaceful NPC following battle purification, radiant aura, Deltarune dialogue thanking Owen and closing demo)
- [`OverworldBgmManager.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/OverworldBgmManager.cs) (Smooth audio fade-in controller for overworld exploration music)

### 6.2 Combat Scripts (`Assets/Scripts/Combat/`)
- [`BattleStageController.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/BattleStageController.cs) (Deltarune $\times$ Limbus turn loop, Flowey-style Weary Sportsman tutorial, PURGE/ACT/ITEM/MERCY buttons, Deltarune Chapter 2 MERCY % bar, implosion vs purify branching)
- [`EnemyBattleAnimator.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/EnemyBattleAnimator.cs) (Procedural bob, damage shake, radiant Purify shimmer, and violent glitch implosion)
- [`BattleDodgeBox.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/BattleDodgeBox.cs) & [`BattleBullet.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/BattleBullet.cs) (Undertale bullet hell box, 0.22 scale agile cyan soul, bullet grazing)
- [`SoulMovementBehaviour.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/SoulMovementBehaviour.cs) & [`SoulMovementLogic.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/SoulMovementLogic.cs) (Precision bullet hell soul movement)
- [`EnemyData.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/EnemyData.cs) (Data-driven profiles with talk options and `mercyReward` progression)

### 6.3 Owen Sprites (`Assets/Sprites/Owen/` and `LibreSprite_Files/`)
- All 47 production PNG sprite frames & sheets (overworld idle/walk in 4 directions, battle stances: Idle, Attack, Defend, Hurt, Downed, and composite sheets), 3 animated preview GIFs, and 15 source `.ase` LibreSprite files standardized with **Pure Black** (`#000000` / `rgba(0,0,0,255)`) outlines and boot/eye-shadow silhouettes.
- All battle stance frames (`Assets/Sprites/Owen/Battle/`) feature dark charcoal grey leg fills (`#2e2e34` / `rgba(46, 46, 52, 255)`) so Owen's legs and boots remain cleanly visible against the pitch black void background during combat.

### 6.4 Audio & Soundtrack Assets (`Assets/Audio/`)
| Path | Description |
| :--- | :--- |
| `Assets/Audio/Battle_Beta.mp3` | User-provided energetic battle music loop loaded into `Golfer_Data.asset` and played during combat. |
| `Assets/Audio/Abandonware.mp3` | Ambient overworld theme that smoothly fades in quietly when Golfer is Purified, marking the start of Owen's journey. |
| `Assets/Audio/Daycare.mp3` | Zone 1 Playpen environment track composed by Corben. |
| `Assets/Audio/Factory1.mp3` | Zone 1 Content Factory manufacturing floor environment track composed by Corben. |
| `Assets/Audio/snd_txt_golfer.wav` | Authentic Undertale/Deltarune weary character voice blip (185 Hz pulse downward chirp) played on dialogue typing. |
| `Assets/Audio/snd_txt_default.wav` | Default Undertale/Deltarune dialogue character voice blip (240 Hz pulse). |

### 6.5 Typography & Fonts (`Assets/Fonts/`)
| Path | Description |
| :--- | :--- |
| `Assets/Fonts/VCR_OSD_MONO_1.001.ttf` | Canonical main game font loaded via `GameFonts.MainFont` across battle HUD, command buttons, dialogue windows, NPC dialogue, and overhead speech bubbles. |
| `Assets/Fonts/HachicroUndertaleBattleFontRegular-L3zlg.ttf` | Undertale battle font strictly reserved for the Lucas Sign at spawn per user mandate. |

### 6.6 EditMode Test Suites (`Assets/Tests/EditMode/`)
- [`OverworldEnemyAgroTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/OverworldEnemyAgroTests.cs) (Verifies enemy sprite facing direction calculation preventing Golfer from turning backwards when charging, acceleration speed ramping up to maxRushSpeed, and top speed exceeding Owen's maximum possible diagonal running speed)
- [`EnemyDataEncounterTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/EnemyDataEncounterTests.cs) (Validates `EnemyData` ScriptableObject profile, in-battle speech barks, TALK options, and TP/Charge reward processing)
- [`ProportionalClashDamageTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/ProportionalClashDamageTests.cs) (Proportional damage ratio $\frac{\text{Heads Rolled}}{\text{Total Coins}}$, partial clash mitigation, full clash damage)
- [`InvisibleWallTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/InvisibleWallTests.cs)
- [`OverworldCreatureTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/OverworldCreatureTests.cs)
- [`AreaDoorTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/AreaDoorTests.cs)
- [`RoomCameraTests.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/RoomCameraTests.cs)
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
| **2026-10-20** | **Golfer Sprite Imported**: Added first enemy visual asset (`Assets/Sprites/Golfer/Golfer_256.png`). |
| **2026-10-20** | **4:3 Undertale Camera Built**: Implemented [`RoomCameraController.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/RoomCameraController.cs) with locked orthographic size, pillarbox/letterbox viewport bars, torso centering, and room boundary clamping. |
| **2026-10-20** | **Procedural System Discarded $\to$ Toby Fox Workflow Adopted**: Replaced with clean Toby Fox static map approach: manual floor sprites + invisible `BoxCollider2D` wall blocks (`obj_solid`). |
| **2026-10-20** | **Deltarune Doors & Secret Egg Room Implemented**: Created [`AreaDoor.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/AreaDoor.cs) and [`ScreenFader.cs`](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/World/ScreenFader.cs) with 2-way door IDs, screen fading, and 1-in-50 (2%) Secret Egg Room diversion. |
| **2026-10-21** | **Learned Workflow & Master Context Consolidation**: Merged isolated context notes into `digipurity_context.md`. Added narrative premise (2030, brother's fatal accident, consciousness transfer), 5-zone breakdown, Triple T boss, and MCP/AM final boss. |
| **2026-10-23** | **Deltarune $\times$ Limbus Battle System Fully Implemented**: <br>1. **Encounter Trigger (`OverworldBattleTrigger.cs` / `OverworldEnemy.cs`)**: Touching Golfer in overworld locks input, flashes screen black 3 times, blinks cyan soul over Owen's chest 3 times with alert sound, and transitions to battle. <br>2. **Universal Modular Enemy Architecture (`EnemyData.cs` & `OverworldEnemy.cs`)**: Data-driven system allowing any overworld enemy to rush at Owen on sight (`agroOnSight`, `sightRadius`), trigger the 3-blink encounter alert, and dynamically inject custom battle sprites, stats, in-battle speech bubbles (`* "FORE! SYSTEM CORRUPT."`), Deltarune TALK/ACT submenus (e.g. Check, Analyze Swing, Heckle), and bullet hell attack patterns (`RainFromTop`, `AimedSpreadBurst`, `AlternatingSide`, `RadialSpiral`). <br>3. **3rd-Person Side-by-Side Stage (`BattleStageController.cs`)**: Owen on Left with Charge Meter ($-45\dots+45$) and vertical TP Bar ($0\text{--}100\%$); Enemy on Right with Charge Meter and sine bob; Deltarune command HUD (`SKILLS`, `TALK`, `ITEM`, `RUN`) + HP bar with Stagger line (50%). <br>4. **Clash Coroutine**: Combatants rush forward, play attack animations, **hold impact frame for 0.25 seconds** (impact freeze), spin digital coins (`Coin_Spin_0..3`), land on Heads/Tails based on Charge, shatter 1 coin per round loser, and push back. <br>5. **Proportional Damage Scaling**: If Owen breaks 1 out of 3 enemy coins, enemy rolls 2 surviving coins; damage dealt is scaled by $\frac{\text{Heads Rolled}}{\text{Total Coins}}$ (e.g. 2/3 damage) rather than full 1.0x damage. <br>6. **Central Bullet Hell (`BattleDodgeBox.cs`)**: When Owen loses clash, overhead slots fade out, central green dodge box expands (`3.5x3.5`), cyan soul dodges bullets, and grazing awards Deltarune TP! |

---
| **2026-10-23** | **Dynamic Music Slowdown & Distortion, Purge Execution Marker, Spark Implosion, PURIFY Renaming & Bullet Hell Telegraph Buffer**: <br>1. **Dynamic Audio Degradation & Tape-Stop Slowdown**: Integrated `AudioDistortionFilter` and `AudioLowPassFilter` on `BattleStageController`. As Golfer takes damage, the battle BGM pitch smoothly slides down (from `1.0` down to `0.68` at the Purge Marker), warm cyber distortion increases (`0.0` -> `0.38`), cutoff frequency drops (`22000Hz` -> `4200Hz`), and subtle vintage tape flutter modulates the pitch. On lethal PURGE execution, `PurgeMusicSlowdownAndStopRoutine` cleanly decelerates playback down to `0.0` over 1.8 seconds with rising distortion, producing a clean analog power-down / tape-stop effect before stopping audio. <br>2. **Purge Marker & Dynamic 'PURGE' Command Option**: Added a visual Purge Marker notch and indicator line on the Golfer's HP bar at 35% HP (`purgeMarkerHp = 35`). When Golfer is above the marker, selecting slot 0 displays `> Basic Attack (Chip HP toward Purge Marker)` dealing ~34 damage; once Golfer's HP drops to or below 35%, Golfer's code enters a destabilized state (`[PURGE READY!]`), and slot 0 dynamically converts to `> PURGE [LETHAL DELETION READY!]`. Confirming with `[Z]` or `[ENTER]` executes the lethal PURGE deletion sequence. <br>3. **Spark Implosion (No Ground Sinking)**: Completely overhauled `EnemyBattleAnimator.PlayPurgeImplosion`: rather than scaling down into his feet on the ground, Golfer's sprite anchors strictly to his torso center of mass (`sr.bounds.center`). Shakes in a violent horizontal cyber-slice spasm for 0.4s, collapses smoothly inward into a blazing white core at chest height (0.35s), and erupts into a burst of 38 multi-color cyber spark particles (white, fiery orange, electric yellow, cyan, pink) that radiate outward at 3.2–7.8 units/s, decelerate with air drag, strobe-flicker near expiration, and fizzle out into the void. <br>4. **Complete PURIFY Renaming Across GUI & Dialogue**: Replaced all occurrences of "MERCY" with "PURIFY" across the 4 command buttons (`PURGE`, `ACT`, `ITEM`, `PURIFY`), HUD target bar (`PURIFY [====  ] {percent}%`), tutorial narratives, and Golfer's in-battle dialogue. In `MercySelect` state, options now display `PURIFY [READY!]` and `FLEE`. Added `purifyReward` property in `EnemyData.TalkOption` for terminology compliance while maintaining backward compatibility with serialized assets. <br>5. **Bullet Hell Telegraph & 0.85s Reaction Grace Buffer**: To ensure the player can react when an attack begins, `StartEnemyAttackTelegraph()` prompts the player with Golfer's battle bark and `* Golfer attacks! [Z / ENTER] Dodge!` before transitioning. Upon entering the arena, `BattleDodgeBox.DodgePhaseRoutine` grants an initial **0.85-second grace period** where the box is open and the cyan soul is fully responsive and movable, allowing the player to position their soul and get their fingers ready before any bullets begin spawning. |

---
| **2026-10-23** | **Victory Text Clipping Resolved, 0 HP Purge Marker & Helpless Turn, Tutorial Anti-Spam Lockout, ACT Forgiveness & Peaceful Purify**: <br>1. **Victory Text Clipping & HUD Readability**: Expanded dialogue box height to 122px (`row2H = 122f`) and increased font size to 18px (`fontSize = 18`), eliminating the text clipping bug where EXP and D$ rewards were truncated at the bottom of the box. Added pulsating high-visibility prompt styling (`[Z / ENTER] Return to Overworld`) so prompts are prominent and unmissable. <br>2. **Tutorial Anti-Spam Confirm Lockout**: Added input debounce (`_inputDebounceTimer`) across tutorial state transitions and locked command selection during `WaitingForScroll` and `WaitingForAct`. Prevents player spamming Z/Enter from accidentally triggering PURGE early in the tutorial. <br>3. **0 HP Purge Marker & Helpless Dodge Turn**: Calibrated the Purge Marker to 0 HP (`purgeMarkerHp = 0`). When enemy drops to 0 HP, they do not immediately die; instead, they become exhausted and helpless (`[PURGE READY!]`). On the enemy's turn, the soul enters an empty dodge box for 1.5s with zero bullets (`StartHelplessDodgePhase`), representing their weakness. On Owen's subsequent turn, slot 0 converts to `> PURGE [LETHAL DELETION READY!]` to execute the fatal purge. <br>4. **ACT Forgiveness & Music Recovery**: When Owen ACTs positively after dealing damage (`addPurify > 0`), Golfer forgives Owen (*"You're giving me advice even after that swing? Thanks, kid. I forgive you."*), and the battle music pitch, distortion, and filter cutoff smoothly glide back to normal tempo (`1.0x` pitch, `0.0` distortion, `22000Hz` cutoff). <br>5. **Peaceful Purify Cleansing (No Dissolution/Explosion)**: When purified, Golfer does not dissolve into white light or explode; instead, his routines are cleansed peacefully, a gentle radiance shimmers, and the battle cleanly concludes with Golfer remaining intact as an overworld NPC. |

---

| **2026-10-23** | **White Light Purify Explosion, Golfer Dying Monologue Foreshadowing, Dynamic Speech Autosizing & Cinematic State Management**: <br>1. **White Light Purify Explosion (`PlayPurifyWhiteLightExplosion`)**: When Golfer is purified at 100% resonance, Owen initiates `PurifySequenceRoutine()`. Golfer delivers a peaceful farewell bark (*"The noise... the static in my head... it's finally quiet. Thank you, Owen."*). Upon confirmation, Golfer gently ascends (+0.45 units) while flaring to blinding white, expands outward to 1.25x scale, dissolves into the void (`sr.enabled = false`), and erupts into a radiant 48-particle shower of ascending white, golden dawn, and cyber-cyan light particles over 1.8s before smoothly transitioning to the 4-line Victory screen. <br>2. **Golfer's Tragic Purge Dying Monologue (Undertale-style Foreshadowing)**: When the player executes lethal PURGE against the exhausted 0 HP Golfer, Golfer delivers an emotionally crushing 2-page dying monologue: <br>&nbsp;&nbsp;&nbsp;&nbsp;• Page 1: *"..do you really hate us that much..? there is another.. waiting.. you will encounter him."* <br>&nbsp;&nbsp;&nbsp;&nbsp;• Page 2: *"I can't say he will be as forgiving if you continue down this path. I... I don't blame you. I forgive you, Owen."* <br>Directly foreshadows the next zone's guardian while maintaining Owen's tragic emotional weight. <br>3. **Dynamic Overhead Speech Bubble Autosizing**: Overhauled `DrawEnemySpeechBubble()` in `BattleStageController.cs` to dynamically evaluate full speech text height using `style.CalcHeight(new GUIContent(_speechFullText), bubbleW - 32f)` and clamp height between 96px and 160px. Eliminates all text truncation or clipping regardless of speech length. <br>4. **Cinematic Menu State Locking**: Re-routed both Purge and Purify cinematic sequences through `BattleMenuState.FightAttackAnim`, completely preventing `Update()`'s `ActResponse` handler from prematurely triggering `StartEnemyAttackTelegraph()` or racing with cinematic coroutines. `OnGUI` now cleanly displays the bottom narrative prompt (`[Z / ENTER] Next`) with pulsing highlights during all cinematic speech phases. |

---

| **2026-10-23** | **Zero-Mercy Strict PURIFY, Elevated Dodge Box, Spared NPC Typewriter & Voice, 8-Step Purge Cut-Off Monologue & BGM Break Fix**: <br>1. **Strict PURIFY Terminology (Zero Mercy)**: Removed residual `(+50% Mercy)` text from `Golfer_Data.asset`. In `BattleStageController.cs`, added runtime regex filtering to strip any residual "Mercy" tags and output strictly `(Purify +{addPurify}%)`. All in-game dialogue and GUI buttons now reference PURIFY exclusively. <br>2. **Elevated Dodge Box (Zero GUI Overlap)**: Shifted `dodgeBoxPos` from Y `999.3f` up to Y `1001.25f` (`boxSize = (3.6f, 2.7f)`). The bottom edge is now anchored safely at Y `999.9f`, comfortably clear of the bottom Deltarune action buttons (`row1Y`) across all aspect ratios and window resolutions. <br>3. **Spared Golfer NPC Typewriter Scroll & Voice Blips**: Updated `PurifiedGolferNPC.cs` to feature character-by-character typewriter scrolling (`charSpeed = 0.03f`), integrated audio blip playback (`snd_txt_golfer.wav`) every 2 characters, interactive skip-to-end on press, and 4:3-constrained text box rendering. <br>4. **8-Step Purge Dying Monologue with 0.75s Mid-Sentence Cut-Off**: When executing lethal PURGE, Golfer's monologue is separated into 8 distinct dramatic steps: <br>&nbsp;&nbsp;&nbsp;&nbsp;1. *"...Do you really hate us that much?"* <br>&nbsp;&nbsp;&nbsp;&nbsp;2. *"I.. have to warn you, Owen."* <br>&nbsp;&nbsp;&nbsp;&nbsp;3. *"There is another."* <br>&nbsp;&nbsp;&nbsp;&nbsp;4. *"He's out.. where this place leads."* <br>&nbsp;&nbsp;&nbsp;&nbsp;5. *"He will not be as forgiving if you continue this path."* <br>&nbsp;&nbsp;&nbsp;&nbsp;6. *"I hope you.. get revenge."* <br>&nbsp;&nbsp;&nbsp;&nbsp;7. *"I hope it satisfies you."* <br>&nbsp;&nbsp;&nbsp;&nbsp;8. *"I forgive you, O-"* $\to$ Immediately pauses for 0.75 seconds before abruptly exploding into cyber sparks, cutting off mid-word. <br>5. **Audio Break Resolution**: Configured `Battle_Beta.mp3.meta` with `DecompressOnLoad` and `PCM` to eliminate MP3 stream re-buffering gaps at the loop boundary. In `BattleStageController.cs`, stopped overworld music on battle start, isolated voice typing audio to a dedicated child `VoiceAudioSource` (preventing DSP filter interruptions), and removed raw sine pitch flutter. |

---

| **2026-10-23** | **Standalone Player Build Fix (Resolved Unity CIL Linker NUnit AssemblyResolutionException) & Compiler Warning Cleanup**: <br>1. **Resolved Fatal UnityLinker `nunit.framework` Error**: When building a standalone Windows player (`WinPlayerBuildProgram`), `UnityLinker.exe` failed with `Mono.Cecil.AssemblyResolutionException: Failed to resolve assembly: 'nunit.framework, Version=3.5.0.0'` because test suites in `Assets/Tests/EditMode/*.cs` were compiling directly into the player runtime `Assembly-CSharp.dll`. Wrapped all 12 test files in `Assets/Tests/EditMode/` with `#if UNITY_EDITOR ... #endif`, ensuring zero references to `NUnit` are compiled into standalone player builds while preserving full test execution inside the Unity Editor. <br>2. **Cleaned CS0414 Assigned-Never-Used Warnings**: Added `#pragma warning disable CS0414` around editor-only gizmo configuration fields (`showInPlayMode`, `showLabel`) in `InvisibleWall.cs` and `OverworldCreature.cs` so builds produce clean zero-warning output. |

---

| **2026-10-26** | **Golfer Overworld Agro Acceleration & Forward Facing Fix**: <br>1. **Sprite Facing Correction (`CalculateFacingFlip`)**: Fixed inverted horizontal sprite flipping in `OverworldEnemy.cs` where `spriteRenderer.flipX = toPlayer.x < 0f;` assumed a native right-facing sprite. Because `Golfer.png` natively faces LEFT, when Owen approached from the left (`toPlayer.x < 0`), Golfer set `flipX = true` and literally turned his back to Owen, moonwalking backwards into the player. Added `spriteFacesLeftByDefault` and static `CalculateFacingFlip()`, ensuring Golfer charges forward looking directly at Owen without turning around. <br>2. **Continuous Pursuit Acceleration**: Replaced static 3.8 u/s pursuit with continuous acceleration (`rushSpeed` = 4.0 u/s, `rushAcceleration` = 7.5 u/s², `maxRushSpeed` = 13.5 u/s). While Owen's max sprint speed is 7.2 u/s (10.8 u/s diagonal), Golfer accelerates past Owen within ~1 second and caps at 13.5 u/s, preventing the player from trivially outrunning him in the overworld. Added frame-tunneling guard `dist <= step || dist <= stopDistance` for reliable encounter triggers at high speed. <br>3. **Unit Test Suite**: Added `OverworldEnemyAgroTests.cs` verifying facing flip states for left/right native sprites, acceleration ramping up to max speed cap, and assertion that `maxRushSpeed` is strictly greater than Owen's maximum possible diagonal running speed. |

---

| **2026-10-27** | **Public README Modernization (Purged Limbus Mechanics) & Lucas Sign Build Resilience**: <br>1. **README Removal**: Removed README.md completely per user request so the repository has no public bio/readme. <br>2. **Lucas Sign Build Asset Resilience (LucasSign.cs)**: Unified font loading to use GameFonts.SignFont (avoiding missing font errors in standalone players), added editor AssetDatabase sprite resolution, and expanded standalone file search paths with procedural fallback so the threshold lore sign operates reliably across editor and compiled builds. |

---

## 8. Immediate Next Steps for Next Agent

1. **Test Golfer Overworld Pursuit in Unity Play Mode**:
   - Press **Play** in Unity.
   - Walk Owen into Room 2 where Golfer stands.
   - Observe Golfer when entering sight radius:
     - Verify Golfer **does NOT turn around** or face backwards when he spots Owen—he charges forward looking straight at Owen.
     - Try running away from Golfer holding Shift and moving diagonally: observe Golfer smoothly accelerating up to 13.5 u/s and catching Owen so you cannot outrun him.
     - When Golfer catches Owen, verify the cyan soul 3-blink alert triggers cleanly and transitions to battle.
2. **Build Standalone Player (File -> Build Settings -> Build)**:
   - Select Windows x64 Standalone.
   - Click **Build** to verify that `UnityLinker.exe` completes cleanly with **Build result: Succeeded** and zero missing assembly errors.
   - **Verify Strict PURIFY (No Mercy)**:
     - Select `[ACT]` -> `Golf Tips` or `Compliment`. Verify the response text says only `(Purify +50%)` with zero mention of "Mercy".
   - **Verify Elevated Bullet Hell Box**:
     - Enter enemy attack / dodge phase. Verify the green rectangular dodge box is positioned safely above the bottom action buttons (`PURGE`, `ACT`, `ITEM`), with zero GUI clipping.
   - **Test 8-Step Purge Dying Monologue & Mid-Sentence Explosion**:
     - Attack Golfer down to 0 HP. Confirm `[PURGE]`.
     - Step through lines 1-7 using `[Z]` / `[ENTER]`:
       - `"...Do you really hate us that much?"`
       - `"I.. have to warn you, Owen."`
       - `"There is another."`
       - `"He's out.. where this place leads."`
       - `"He will not be as forgiving if you continue this path."`
       - `"I hope you.. get revenge."`
       - `"I hope it satisfies you."`
     - Watch line 8 appear: `"I forgive you, O-"`. Verify it displays for 0.75 seconds, then Golfer immediately explodes into cyber sparks mid-sentence!
   - **Test Spared Golfer NPC Typewriter & Voice Blips**:
     - Spared Golfer appears in overworld. Walk up and press `[E]` to talk.
     - Verify dialogue text types out character-by-character with authentic Golfer voice blips (`snd_txt_golfer.wav`).
     - Verify pressing `[E]` skips typing to the end of the line, and next press advances lines cleanly.
