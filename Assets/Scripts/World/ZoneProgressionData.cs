using System;
using UnityEngine;

namespace DigiPurity.World
{
    [Serializable]
    public class ZoneProgressionData
    {
        public int currentZone = 1; // 1 to 4
        public int encountersInZone = 0;
        public int maxEncountersPerZone = 25; // OFF-style encounter ceiling per zone
        public int totalExp = 0;
        public int currentLevel = 1;

        /// <summary>
        /// Zone 1 Cap: 25
        /// Zone 2 Cap: 50
        /// Zone 3 Cap: 75
        /// Zone 4 Cap: 100
        /// </summary>
        public int MaxLevelForCurrentZone
        {
            get
            {
                switch (currentZone)
                {
                    case 1: return 25;
                    case 2: return 50;
                    case 3: return 75;
                    case 4: return 100;
                    default: return 100;
                }
            }
        }

        public bool CanTriggerEncounter()
        {
            return encountersInZone < maxEncountersPerZone;
        }

        public void RecordEncounterFinished(bool wasPurged, int baseExp)
        {
            encountersInZone++;
            // Purging yields extra EXP per user rules
            int awardedExp = wasPurged ? Mathf.RoundToInt(baseExp * 1.5f) : baseExp;
            totalExp += awardedExp;

            CheckLevelUp();
        }

        private void CheckLevelUp()
        {
            // Level formula: 10 EXP per level, capped at the active zone's level ceiling
            int calculatedLevel = 1 + (totalExp / 10);
            currentLevel = Mathf.Min(calculatedLevel, MaxLevelForCurrentZone);
        }

        public void AdvanceToNextZone()
        {
            if (currentZone < 4)
            {
                currentZone++;
                encountersInZone = 0; // Reset encounter counter for the new zone
            }
        }
    }
}
