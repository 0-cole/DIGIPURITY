using UnityEngine;

namespace DigiPurity.Combat
{
    /// <summary>
    /// Decoupled tutorial encounter driver for the initial Weary Sportsman (Golfer) tutorial battle.
    /// Keeps BattleStageController 100% universal and enemy-agnostic for all non-tutorial battles,
    /// while exposing all tutorial dialogue lines as Inspector-editable fields.
    /// </summary>
    public class BattleTutorialEncounter : MonoBehaviour
    {
        public enum Phase
        {
            None = 0,
            Intro1,
            Intro2,
            PromptScroll,
            WaitingForScroll,
            PromptSelect,
            PromptPurge,
            PromptActAndMercy,
            PromptMercyBar,
            PromptDoAct,
            WaitingForAct,
            MercyFull,
            Completed
        }

        [Header("Tutorial Activation")]
        [Tooltip("If true, runs the interactive onboarding sequence at the start of this encounter.")]
        public bool enableTutorialSequence = true;

        [Header("Onboarding Step Dialogue (Inspector Editable)")]
        [TextArea(2, 3)]
        public string introLine1 = "Easy there, kid. Rest your scythe for a second. Let's see your posture.";

        [TextArea(2, 3)]
        public string introLine2 = "You're digitized now. Everything in this decaying network moves by strict turn rules.";

        [TextArea(2, 3)]
        public string promptScrollLine = "See those 4 buttons on the left? Use [W / S] or [Up / Down] to scroll through your options. Give it a try!";

        [TextArea(2, 3)]
        public string waitingForScrollBark = "Give it a try! Scroll through your commands.";

        [TextArea(2, 3)]
        public string promptSelectLine = "Good grip! [ENTER] or [Z] confirms your choice. [X] or [ESC] cancels and steps back.";

        [TextArea(2, 3)]
        public string promptPurgeLine = "Notice that first slot: [PURGE]. Attacking chips away at HP until 0 HP. Once down to 0, you can execute a full PURGE to delete a soul forever.";

        [TextArea(2, 3)]
        public string promptActAndPurifyLine = "A lot of bitter souls take that shot... but [ACT] and [PURIFY] let you stabilize corrupted routines instead.";

        [TextArea(2, 3)]
        public string promptPurifyBarLine = "When you select a target, you'll see a PURIFY bar. Connecting with someone through [ACT] fills that bar to 100%!";

        [TextArea(2, 3)]
        public string promptDoActLine = "Select [ACT] and give an old sportsman some advice on his swing. Let's see what you've got!";

        [TextArea(2, 3)]
        public string waitingForActBark = "Show me what you've got, kid.";

        [TextArea(2, 3)]
        public string wrongCommandReminderLine = "Hold on, kid! Try selecting [ACT] first to see how peaceful purification works.";

        [TextArea(2, 3)]
        public string partialActEncouragementLine = "Hah! Keep at it, kid!";

        [TextArea(2, 3)]
        public string purifyReadyLine = "There it is! 100% Purify! Now choose [PURIFY] to cleanse, or [PURGE] to attack.";

        [Header("Combat Reaction & Ending Dialogue (Inspector Editable)")]
        [TextArea(2, 3)]
        public string hitReactionLine = "Oof... steady swing, kid. I know you're hurting inside... I still believe in you.";

        [TextArea(2, 3)]
        public string zeroHpExhaustedLine = "Huff... puff... I... I can't keep my grip... my code is unravelling...";

        [TextArea(2, 3)]
        public string zeroHpCannotAttackLine = "I... I can't attack... my energy is drained...";

        [TextArea(2, 3)]
        public string forgivenessAfterDamageLine = "You... you're giving me advice even after that swing? Thanks, kid. I forgive you.";

        [TextArea(2, 3)]
        public string purifyFarewellLine = "The noise... the static in my head... it's finally quiet. Thank you, Owen.";

        [TextArea(1, 2)]
        public string[] lethalPurgeMonologue = new string[]
        {
            "...Do you really hate us that much?",
            "I.. have to warn you, Owen.",
            "There is another.",
            "He's out.. where this place leads.",
            "He will not be as forgiving if you continue this path.",
            "I hope you.. get revenge.",
            "I hope it satisfies you."
        };

        public string lethalPurgeCutOffLine = "I forgive you, O-";

        [Header("Runtime State")]
        public Phase currentPhase = Phase.None;

        public bool IsActiveTutorial => enableTutorialSequence && currentPhase != Phase.None && currentPhase != Phase.Completed;

        public void BeginEncounter(BattleStageController stage)
        {
            if (!enableTutorialSequence)
            {
                currentPhase = Phase.Completed;
                stage.StartNewPlayerTurn($"* {stage.activeEnemyName} blocks the way!");
                return;
            }

            currentPhase = Phase.Intro1;
            stage.SetMenuState(BattleStageController.BattleMenuState.TutorialNarrative);
            stage.SetEnemySpeech(introLine1, "* [Z / ENTER] Continue");
        }

        /// <summary>
        /// Advances tutorial narrative when player presses Confirm during TutorialNarrative state.
        /// </summary>
        public void AdvanceNarrative(BattleStageController stage)
        {
            switch (currentPhase)
            {
                case Phase.Intro1:
                    currentPhase = Phase.Intro2;
                    stage.SetEnemySpeech(introLine2, "* [Z / ENTER] Next");
                    break;

                case Phase.Intro2:
                    currentPhase = Phase.PromptScroll;
                    stage.SetEnemySpeech(promptScrollLine, "* [Z / ENTER] Practice");
                    break;

                case Phase.PromptScroll:
                    currentPhase = Phase.WaitingForScroll;
                    stage.SetMenuState(BattleStageController.BattleMenuState.CommandSelect);
                    stage.SetEnemySpeech(waitingForScrollBark, "* Use [W / S] or [Up / Down] to scroll through your commands.");
                    break;

                case Phase.PromptSelect:
                    currentPhase = Phase.PromptPurge;
                    stage.SetEnemySpeech(promptPurgeLine, "* [Z / ENTER] Next");
                    break;

                case Phase.PromptPurge:
                    currentPhase = Phase.PromptActAndMercy;
                    stage.SetEnemySpeech(promptActAndPurifyLine, "* [Z / ENTER] Next");
                    break;

                case Phase.PromptActAndMercy:
                    currentPhase = Phase.PromptMercyBar;
                    stage.SetEnemySpeech(promptPurifyBarLine, "* [Z / ENTER] Next");
                    break;

                case Phase.PromptMercyBar:
                    currentPhase = Phase.PromptDoAct;
                    stage.SetEnemySpeech(promptDoActLine, "* [Z / ENTER] Open ACT");
                    break;

                case Phase.PromptDoAct:
                    currentPhase = Phase.WaitingForAct;
                    stage.SelectCommandSlot(1); // Highlight ACT
                    stage.SetMenuState(BattleStageController.BattleMenuState.CommandSelect);
                    stage.SetEnemySpeech(waitingForActBark, "* Select [ACT] to interact.");
                    break;

                case Phase.MercyFull:
                    currentPhase = Phase.Completed;
                    stage.SelectCommandSlot(3); // Highlight PURIFY
                    stage.SetMenuState(BattleStageController.BattleMenuState.CommandSelect);
                    stage.HideEnemySpeech();
                    stage.SetDialogueText($"* {stage.activeEnemyName} is 100% stabilized! Choose [PURIFY] to cleanse, or [PURGE] to attack.");
                    break;

                default:
                    currentPhase = Phase.Completed;
                    stage.SetMenuState(BattleStageController.BattleMenuState.CommandSelect);
                    break;
            }
        }

        /// <summary>
        /// Returns true if scrolling triggered a tutorial transition and intercepted normal command input.
        /// </summary>
        public bool OnCommandScrolled(BattleStageController stage)
        {
            if (!IsActiveTutorial || currentPhase != Phase.WaitingForScroll) return false;

            currentPhase = Phase.PromptSelect;
            stage.SetMenuState(BattleStageController.BattleMenuState.TutorialNarrative);
            stage.SetInputDebounce(0.25f);
            stage.SetEnemySpeech(promptSelectLine, "* [Z / ENTER] Next");
            return true;
        }

        /// <summary>
        /// Validates whether the player is allowed to confirm the selected command during tutorial phases.
        /// </summary>
        public bool AllowCommandConfirm(BattleStageController stage, int selectedCommandIndex)
        {
            if (!IsActiveTutorial) return true;

            if (currentPhase == Phase.WaitingForScroll)
            {
                stage.SetDialogueText("* Use [W / S] or [Up / Down] to scroll through your commands.");
                return false;
            }

            if (currentPhase == Phase.WaitingForAct)
            {
                if (selectedCommandIndex == 1) // ACT
                {
                    return true;
                }
                stage.SetEnemySpeech(wrongCommandReminderLine, $"* Select [ACT] to interact with {stage.activeEnemyName}.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if the tutorial handled the ACT response transition.
        /// </summary>
        public bool TryHandleActOutcome(BattleStageController stage, string cleanResponse, int addPurify)
        {
            if (!IsActiveTutorial) return false;

            if (currentPhase == Phase.WaitingForAct || currentPhase == Phase.PromptDoAct)
            {
                if (stage.enemyCurrentMercy >= 100)
                {
                    currentPhase = Phase.MercyFull;
                    stage.SetMenuState(BattleStageController.BattleMenuState.TutorialNarrative);
                    stage.SetEnemySpeech(purifyReadyLine, $"* {cleanResponse}\n* [Z / ENTER] Finish Tutorial");
                }
                else
                {
                    stage.SetMenuState(BattleStageController.BattleMenuState.ActResponse);
                    stage.SetEnemySpeech(partialActEncouragementLine, $"* {cleanResponse} (Purify +{addPurify}%)\n* [Z / ENTER] Next");
                }
                return true;
            }

            return false;
        }
    }
}
