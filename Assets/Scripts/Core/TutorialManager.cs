using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigiPurity.Core
{
    public enum TutorialStep
    {
        None = 0,
        SkillClashingBasics,      // Step 1: Matching slots & coin toss
        DefenseSlotSwap,          // Step 2: Clicking slot to Guard / Evade
        BulletHellDodging,        // Step 3: Moving soul & Shift slow mode
        ChargeAndTensionEconomy,  // Step 4: Deltarune TP grazing & boosting Charge
        StutterAndGlitchSystem,   // Step 5: Tremor burst & Staggering machine
        EquipmentResonance,       // Step 6: Equipping 5 items & fusing hybrids
        PurifyVsPurgeChoice       // Step 7: Moral decision & EXP disparity
    }

    [Serializable]
    public class TutorialMessage
    {
        public TutorialStep step;
        public string title;
        [TextArea(3, 6)]
        public string instructionText;
    }

    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        private readonly HashSet<TutorialStep> _completedSteps = new HashSet<TutorialStep>();

        public event Action<TutorialStep> OnTutorialTriggered;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public bool HasCompleted(TutorialStep step) => _completedSteps.Contains(step);

        public void TriggerTutorial(TutorialStep step)
        {
            if (!_completedSteps.Contains(step))
            {
                _completedSteps.Add(step);
                OnTutorialTriggered?.Invoke(step);
            }
        }
    }
}
