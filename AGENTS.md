# DIGIPURITY Project Invariants & Cross-Conversation Context Rule

When working on the **DIGIPURITY** repository:

1. **Mandatory Master Context Consultation**:
   - Always read and consult [`digipurity_context.md`](file:///c:/Users/cdbla/Documents/GitHub/DIGIPURITY/digipurity_context.md) at the beginning of any conversation or task before proposing or modifying systems.
   - Respect established architectural invariants documented in the context file:
     - **Toby Fox Level Design**: Use static floor sprites + invisible `BoxCollider2D` solid wall colliders (`obj_solid`). Never reintroduce procedural room generators or runtime doorway cutters.
     - **4:3 Undertale Camera**: Maintain locked orthographic size `9.0f` (24x18 units / 12x9 tiles), torso-centering, room clamping, and black letterbox/pillarbox bars.
     - **Deltarune Doors & Secret Egg Room**: Use `AreaDoor.cs` for 2-way paired door transitions and 1-in-50 (2%) secret room diversions.
     - **4-Way Combat Synthesis**: Maintain the Limbus deck/coin, Deltarune TP/Graze, Undertale dodge box, and OFF encounter/level cap math.
     - **Narrative & Tonal Authenticity**: Maintain the tense, vengeful emotional spine blended with satirical, surreal, comedic Undertale/Deltarune tone critiquing modern AI content farming.

2. **Continuous Context Maintenance & Update History**:
   - At the conclusion of any significant feature, modification, bug fix, or asset addition, update [`digipurity_context.md`](file:///c:/Users/cdbla/Documents/GitHub/DIGIPURITY/digipurity_context.md):
     - Update **Section 6 (Verified Codebase State)** with any new or modified scripts/assets.
     - Append an entry to **Section 7 (Update History & Decision Log)** summarizing changes made and key design decisions.
     - Refresh **Section 8 (Immediate Next Steps)** so the subsequent agent has clear, actionable instructions.

3. **Handoff Clarity**:
   - When handing off to a new conversation or agent, always provide the copyable absolute Windows path:
     `C:\Users\cdbla\Documents\GitHub\DIGIPURITY\digipurity_context.md`
     along with the clickable link [`digipurity_context.md`](file:///c:/Users/cdbla/Documents/GitHub/DIGIPURITY/digipurity_context.md).
