using System;
using UnityEngine;

namespace DigiPurity.Combat
{
    public enum CombatTurnPhase
    {
        InitiativeAndSlots,
        SkillPlanning,
        CoinClashing,
        BulletHellDefense,
        PurifyOrPurgeDecision,
        Victory,
        Defeat
    }

    public class CombatStateMachine
    {
        public CombatTurnPhase CurrentPhase { get; private set; }
        public int TurnNumber { get; private set; }
        public int PlayerSlots { get; private set; }
        public int EnemySlots { get; private set; }

        public CombatStateMachine()
        {
            ResetCombat();
        }

        public void ResetCombat()
        {
            TurnNumber = 1;
            PlayerSlots = 1;
            EnemySlots = 1;
            CurrentPhase = CombatTurnPhase.InitiativeAndSlots;
        }

        public void AdvanceTurnSlots()
        {
            TurnNumber++;
            PlayerSlots = Mathf.Min(6, 1 + (TurnNumber - 1));
            EnemySlots = Mathf.Min(6, 1 + (TurnNumber - 1));
        }

        public void TransitionTo(CombatTurnPhase newPhase)
        {
            CurrentPhase = newPhase;
        }
    }
}
