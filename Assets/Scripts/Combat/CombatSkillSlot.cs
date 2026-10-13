using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigiPurity.Combat
{
    public enum DefenseType
    {
        Guard,   // Generates Shield HP equal to final power
        Evade,   // Flips coins against enemy coins; ignores damage if power >= enemy
        Counter  // Retaliates after taking damage
    }

    [Serializable]
    public class CombatSkillSlot
    {
        public int slotIndex;
        public CombatSkill primarySkill;
        public CombatSkill defenseSkill;
        public bool isDefenseActive;

        public CombatSkill ActiveSkill => isDefenseActive ? defenseSkill : primarySkill;

        public CombatSkillSlot(int index, CombatSkill initialSkill)
        {
            slotIndex = index;
            primarySkill = initialSkill;
            defenseSkill = CombatSkill.CreateDefense();
            isDefenseActive = false;
        }

        /// <summary>
        /// Toggles between the assigned attack skill and the unit's defense skill,
        /// exactly mirroring Limbus Company's portrait/slot tap swap mechanic.
        /// </summary>
        public void ToggleDefense()
        {
            isDefenseActive = !isDefenseActive;
        }

        public void SetSkill(CombatSkill newSkill)
        {
            primarySkill = newSkill;
            isDefenseActive = false;
        }
    }

    public class SkillSlotManager
    {
        public List<CombatSkillSlot> Slots { get; private set; } = new List<CombatSkillSlot>();

        public void SetupSlots(int slotCount, CombatDeckManager deck)
        {
            Slots.Clear();
            for (int i = 0; i < slotCount; i++)
            {
                CombatSkill drawn = deck.DrawSkill();
                Slots.Add(new CombatSkillSlot(i, drawn));
            }
        }

        public void ToggleSlotDefense(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < Slots.Count)
            {
                Slots[slotIndex].ToggleDefense();
            }
        }
    }
}
