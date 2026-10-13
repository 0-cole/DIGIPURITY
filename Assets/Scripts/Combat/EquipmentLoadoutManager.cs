using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigiPurity.Combat
{
    public enum HybridKeywordType
    {
        None,
        StutterScorch,  // Burn + Stutter: Burning damage accelerates Glitch threshold collapse
        CrimsonLacerate, // Poise + Bleed: Critical strikes trigger multiple Bleed ticks instantly
        StaticSeizure,   // Drainage + Stutter: Draining enemy Charge directly inflicts massive Stutter Potency
        CobaltCombustion // Burn + Poise: "Blue Burn" - Consumes Poise count to deal guaranteed Critical Burn damage (x1.5)
    }

    [Serializable]
    public class EquipmentItem
    {
        public string itemId;
        public string itemName;
        public string description;
        public KeywordType primaryKeyword = KeywordType.None;
        public bool isResonanceCore = false; // The 2 special items per keyword engineered to trigger fusions
        public int bonusHp = 0;
        public int bonusCoinPower = 0;

        public EquipmentItem(string id, string name, KeywordType kw, bool resonance = false, int hp = 0, int coinPwr = 0)
        {
            itemId = id;
            itemName = name;
            primaryKeyword = kw;
            isResonanceCore = resonance;
            bonusHp = hp;
            bonusCoinPower = coinPwr;
        }
    }

    public class EquipmentLoadoutManager
    {
        public const int MaxEquippedItems = 5;
        public List<EquipmentItem> EquippedItems { get; private set; } = new List<EquipmentItem>();

        public bool EquipItem(EquipmentItem item)
        {
            if (EquippedItems.Count >= MaxEquippedItems) return false;
            EquippedItems.Add(item);
            return true;
        }

        public void UnequipItem(string itemId)
        {
            EquippedItems.RemoveAll(i => i.itemId == itemId);
        }

        /// <summary>
        /// Detects active hybrid fusions based on equipped resonance items.
        /// Activates only if you have resonance items for both required keywords.
        /// </summary>
        public HashSet<HybridKeywordType> EvaluateActiveHybridKeywords()
        {
            var activeHybrids = new HashSet<HybridKeywordType>();

            bool hasBurnResonance = EquippedItems.Exists(i => i.primaryKeyword == KeywordType.Burn && i.isResonanceCore);
            bool hasStutterResonance = EquippedItems.Exists(i => i.primaryKeyword == KeywordType.Stutter && i.isResonanceCore);
            bool hasPoiseResonance = EquippedItems.Exists(i => i.primaryKeyword == KeywordType.Poise && i.isResonanceCore);
            bool hasBleedResonance = EquippedItems.Exists(i => i.primaryKeyword == KeywordType.Bleed && i.isResonanceCore);
            bool hasDrainageResonance = EquippedItems.Exists(i => i.primaryKeyword == KeywordType.Drainage && i.isResonanceCore);

            // 1. Burn & Stutter -> Stutter Scorch
            if (hasBurnResonance && hasStutterResonance)
                activeHybrids.Add(HybridKeywordType.StutterScorch);

            // 2. Poise & Bleed -> Crimson Lacerate
            if (hasPoiseResonance && hasBleedResonance)
                activeHybrids.Add(HybridKeywordType.CrimsonLacerate);

            // 3. Drainage & Stutter -> Static Seizure
            if (hasDrainageResonance && hasStutterResonance)
                activeHybrids.Add(HybridKeywordType.StaticSeizure);

            // 4. Burn & Poise -> Cobalt Combustion (Blue Burn)
            if (hasBurnResonance && hasPoiseResonance)
                activeHybrids.Add(HybridKeywordType.CobaltCombustion);

            return activeHybrids;
        }
    }
}
