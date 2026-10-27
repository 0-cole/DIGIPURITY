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

2. **Fast-Path Code-First Execution (Anti-Overthinking Invariant)**:
   - **Direct C# Targeting**: For gameplay logic, enemy behavior, speed/acceleration tweaks, stats, or bug fixes, go directly to the target C# script in `Assets/Scripts/`. Consult the Quick-Reference table in `digipurity_context.md` Section 1.6 to find the exact file immediately.
   - **Strictly Banned Token-Wasting Checks**:
     - **NEVER** parse or grep serialized `.unity` scene YAML files unless explicitly asked to modify a scene asset.
     - **NEVER** run Python/PowerShell scripts to inspect raw image pixels, bounding boxes, or color palettes of `.png` sprites.
     - **NEVER** run diagnostic checks on line endings, file encodings, or irrelevant test suites for simple code tweaks.
   - **Minimal Tool Calls**: A simple tweak (e.g. speed, facing direction, collider size) should be completed in 1–2 edits directly in C#. Do not run dozens of diagnostic commands before making the edit.

3. **Continuous Context Maintenance & Update History**:
   - At the conclusion of any significant feature, modification, bug fix, or asset addition, update [`digipurity_context.md`](file:///c:/Users/cdbla/Documents/GitHub/DIGIPURITY/digipurity_context.md):
     - Update **Section 6 (Verified Codebase State)** with any new or modified scripts/assets.
     - Append an entry to **Section 7 (Update History & Decision Log)** summarizing changes made and key design decisions.
     - Refresh **Section 8 (Immediate Next Steps)** so the subsequent agent has clear, actionable instructions.

4. **Handoff Clarity**:
   - When handing off to a new conversation or agent, always provide the copyable absolute Windows path:
     `C:\Users\cdbla\Documents\GitHub\DIGIPURITY\digipurity_context.md`
     along with the clickable link [`digipurity_context.md`](file:///c:/Users/cdbla/Documents/GitHub/DIGIPURITY/digipurity_context.md).
