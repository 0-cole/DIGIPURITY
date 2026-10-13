using System;
using UnityEngine;

namespace DigiPurity.Combat
{
    public class TensionAndChargeSystem
    {
        public const float MaxTension = 250f; // Deltarune internal maxtension
        public const int MinCharge = -45;     // Limbus Sanity lower bound
        public const int MaxCharge = 45;      // Limbus Sanity upper bound

        public float CurrentTension { get; private set; } // 0 to 250
        public int CurrentCharge { get; private set; }    // -45 to +45

        public TensionAndChargeSystem(int initialCharge = 0, float initialTension = 0f)
        {
            CurrentCharge = Mathf.Clamp(initialCharge, MinCharge, MaxCharge);
            CurrentTension = Mathf.Clamp(initialTension, 0f, MaxTension);
        }

        public float TensionPercentage => (CurrentTension / MaxTension) * 100f;

        public void AddGrazeTension(float amount = 10f)
        {
            CurrentTension = Mathf.Clamp(CurrentTension + amount, 0f, MaxTension);
        }

        public void AddDefendTension()
        {
            // Deltarune DEFEND awards 16% (40 points)
            CurrentTension = Mathf.Clamp(CurrentTension + 40f, 0f, MaxTension);
        }

        public bool SpendTensionToBoostCharge(float tensionCost, int chargeBoost)
        {
            if (CurrentTension >= tensionCost)
            {
                CurrentTension -= tensionCost;
                CurrentCharge = Mathf.Clamp(CurrentCharge + chargeBoost, MinCharge, MaxCharge);
                return true;
            }
            return false;
        }

        public void ApplyDrainage(int drainageAmount)
        {
            CurrentCharge = Mathf.Clamp(CurrentCharge - drainageAmount, MinCharge, MaxCharge);
        }

        public void ModifyCharge(int delta)
        {
            CurrentCharge = Mathf.Clamp(CurrentCharge + delta, MinCharge, MaxCharge);
        }
    }
}
