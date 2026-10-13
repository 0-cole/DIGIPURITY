# DIGIPURITY Development Context & Handoff

## 1. Executive Summary & High-Level Objective
- **Project**: DIGIPURITY (Unity 2D Capstone Game by Cole Blair).
- **Core Concept**: Futuristic RPG where the player battles "AI Slop" entities. At the end of combat, the player chooses whether to **Purify** or **Purge** them, directly impacting story flags, dialogue, and world stability.
- **Combat Style**: A hybrid of **Limbus Company** (Skill Slots expanding each turn, Coin Flips, Clashing, Sanity/Purity influencing head rates) and **Undertale** (Turn-based flow, red-soul bullet-hell dodging when taking direct enemy skill hits, border clamping, cancel-button halving speed).
- **Location**: `C:\Users\cdbla\Documents\GitHub\DIGIPURITY`

## 2. Critical Architecture & Invariants
- **Engine**: Unity 6000.6.0f1 (Universal 2D / URP).
- **Coin Clash Mechanics**:
  - Heads Probability: $0.50 + (\text{Sanity} / 100)$, clamped between $0.05$ and $0.95$.
  - Loser of a clash round permanently forfeits 1 active coin for that exchange.
- **Undertale Movement Invariants**:
  - Base soul speed is reduced by exactly $50\%$ when holding Cancel (`Shift` / `X`).
  - Position strictly clamped to combat border box minus margin.
  - Invulnerability timer decremented frame-by-frame.
- **Skill Slots**:
  - Dynamically scale with turn count: `Mathf.Min(6, 1 + (TurnNumber - 1))`.

## 3. Verified Codebase State
- [CoinClashResolver.cs](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/CoinClashResolver.cs): Pure C# clash calculation, probability, and coin power logic.
- [SoulMovementLogic.cs](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/SoulMovementLogic.cs): Pure C# soul movement and border clamping math.
- [CombatStateMachine.cs](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/CombatStateMachine.cs): Phase management bridging Clash, Planning, and Bullet Hell phases.
- [SoulMovementBehaviour.cs](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Combat/SoulMovementBehaviour.cs): Unity MonoBehaviour handling user input and sprite positioning.
- [SaveSystem.cs](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Scripts/Core/SaveSystem.cs): JSON save/load system tracking Purified/Purged counts and stats.
- [CombatSystemTests.cs](file:///C:/Users/cdbla/Documents/GitHub/DIGIPURITY/Assets/Tests/EditMode/CombatSystemTests.cs): EditMode unit tests validating movement and clash mechanics.

## 4. Immediate Next Steps
1. Wire UI overlays for the planning phase (displaying skill slots and coins).
2. Create bullet generator components (`BulletEmitter`) for the bullet-hell dodging phase.
3. Once off school Wi-Fi, push commits to GitHub:
   ```pwsh
   git remote add origin https://github.com/0-cole/DIGIPURITY.git
   git push -u origin main
   ```
