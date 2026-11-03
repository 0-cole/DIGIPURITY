# DIGIPURITY Project Invariants & Cross-Conversation Context Rule

When working on the **DIGIPURITY** repository:

1. **Mandatory Master Context Consultation**:
   - Always read and consult [`digipurity_context.md`](file:///c:/Users/cdbla/Documents/GitHub/DIGIPURITY/digipurity_context.md) at the beginning of any conversation or task before proposing or modifying systems.
   - Respect established architectural invariants documented in the context file:
     - **Toby Fox Level Design**: Use static floor sprites + invisible `BoxCollider2D` solid wall colliders (`obj_solid`). Never reintroduce procedural room generators or runtime doorway cutters.
     - **4:3 Undertale Camera**: Maintain locked orthographic size `9.0f` (24x18 units / 12x9 tiles), torso-centering, room clamping, and black letterbox/pillarbox bars.
     - **Deltarune Doors & Secret Egg Room**: Use `AreaDoor.cs` for 2-way paired door transitions and 1-in-50 (2%) secret room diversions.
     - **Combat Synthesis**: Maintain the Deltarune turn-based HUD/TP/Graze, Undertale central dodge box, and OFF moral polarity/encounter cap math.
     - **Narrative & Tonal Authenticity**: Maintain the tense, vengeful emotional spine blended with satirical, surreal, comedic Undertale/Deltarune tone critiquing modern AI content farming.

2. **Universal Fast-Path & Highest-Leverage Execution (Anti-Overthinking Invariant Across ALL Tasks)**:
   - **Applies to ALL Domains**: This rule governs gameplay logic, physics, enemy AI, movement, combat math, cameras, audio, dialogues, transitions, shaders, input, and UI.
   - **Direct C# Targeting**: Jump directly to the primary target script under `Assets/Scripts/`. Consult the Quick-Reference table in `digipurity_context.md` Section 1.6 to find the exact file immediately.
   - **Highest-Leverage Single-Point Architecture**:
     - When modifying systems, mechanics, aspect ratios, audio, or layouts, **NEVER** spam shell grep/find commands or attempt to rewrite dozens of individual calls one-by-one.
     - Always use the quickest, single-point architectural solution:
       - **UI & Layouts**: Use single group wrappers (`GUI.BeginGroup`) or matrix transforms (`GUI.matrix`).
       - **Camera & Aspect**: Use 1-line camera viewport rect calculation (`camera.rect = Calculate4By3ViewportRect(...)`).
       - **Combat & Data**: Use ScriptableObjects (`EnemyData`) or central directors (`CombatDirector`, `BattleStageController`).
       - **Movement & Physics**: Adjust variables on the primary controller (`OverworldPlayerController`, `OverworldEnemy`).
   - **Strictly Banned Token-Wasting Checks (All Domains)**:
     - **NEVER** parse or grep serialized `.unity` scene YAML files unless explicitly asked to modify a scene asset.
     - **NEVER** run Python/PowerShell scripts to inspect raw image pixels, bounding boxes, or color palettes of `.png` sprites.
     - **NEVER** run diagnostic checks on line endings, file encodings, or irrelevant test suites for simple tweaks.
     - **NEVER** run redundant `grep` / `find` terminal commands when reading the target file directly answers the question.
   - **Minimal Tool Calls (Budget: 1–3 Total)**: A simple tweak (speed, facing direction, collider size, damage math, text blip, 4:3 framing) must be completed in 1–2 edits directly in C#. Do not run exploratory diagnostic checks before making the edit.

3. **Continuous Context Maintenance & Update History**:
   - At the conclusion of any significant feature, modification, bug fix, or asset addition, update [`digipurity_context.md`](file:///c:/Users/cdbla/Documents/GitHub/DIGIPURITY/digipurity_context.md):
     - Update **Section 6 (Verified Codebase State)** with any new or modified scripts/assets.
     - Append an entry to **Section 7 (Update History & Decision Log)** summarizing changes made and key design decisions.
     - Refresh **Section 8 (Immediate Next Steps)** so the subsequent agent has clear, actionable instructions.

4. **Handoff Clarity**:
   - When handing off to a new conversation or agent, always provide the copyable absolute Windows path:
     `C:\Users\cdbla\Documents\GitHub\DIGIPURITY\digipurity_context.md`
     along with the clickable link [`digipurity_context.md`](file:///c:/Users/cdbla/Documents/GitHub/DIGIPURITY/digipurity_context.md).
