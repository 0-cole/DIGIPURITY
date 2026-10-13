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
        public int purityLevel = 1;

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
            // Undertale LV curve: Lv 2 @ 10, Lv 3 @ 30, Lv 4 @ 70
            if (totalExp >= 70) purityLevel = 4;
            else if (totalExp >= 30) purityLevel = 3;
            else if (totalExp >= 10) purityLevel = 2;
        }
    }
}
