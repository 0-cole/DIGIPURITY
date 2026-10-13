using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigiPurity.Combat
{
    public class StaggerAndGlitchSystem
    {
        public int maxHp;
        public int currentHp;
        public int baseGlitchThreshold;      // Standard Stagger threshold (e.g. 50% HP)
        public int currentGlitchThreshold;   // Dynamically raised by Stutter/Tremor
        public bool isGlitched;              // True when Staggered (vulnerable, skips turn)

        public StaggerAndGlitchSystem(int maxHealth = 100)
        {
            maxHp = maxHealth;
            currentHp = maxHealth;
            baseGlitchThreshold = maxHealth / 2;
            currentGlitchThreshold = baseGlitchThreshold;
            isGlitched = false;
        }

        /// <summary>
        /// Stutter Burst raises the Glitch/Stagger threshold by the Stutter potency.
        /// If HP is at or below the raised threshold, the unit enters a Glitched state.
        /// </summary>
        public bool TriggerStutterBurst(int stutterPotency)
        {
            currentGlitchThreshold = Mathf.Min(maxHp, currentGlitchThreshold + stutterPotency);
            if (!isGlitched && currentHp <= currentGlitchThreshold)
            {
                isGlitched = true;
                return true;
            }
            return false;
        }

        public void TakeDamage(int dmg)
        {
            currentHp = Mathf.Max(0, currentHp - dmg);
            if (!isGlitched && currentHp <= currentGlitchThreshold)
            {
                isGlitched = true;
            }
        }

        public void RecoverFromGlitch()
        {
            isGlitched = false;
            currentGlitchThreshold = baseGlitchThreshold;
        }
    }
}
