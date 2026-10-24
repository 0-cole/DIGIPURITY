#if UNITY_EDITOR
using NUnit.Framework;
using DigiPurity.Combat;

namespace DigiPurity.Tests
{
    public class DefenseSkillSwapTests
    {
        [Test]
        public void SkillSlot_ToggleDefense_SwapsBetweenAttackAndDefense()
        {
            var attackSkill = CombatSkill.CreateSkill1();
            var slot = new CombatSkillSlot(0, attackSkill);

            // Initially default attack skill is active
            Assert.IsFalse(slot.isDefenseActive);
            Assert.AreEqual(SkillTier.Skill1_PureDamage, slot.ActiveSkill.tier);

            // Tap slot / portrait -> swaps to Defense Skill (Limbus invariant)
            slot.ToggleDefense();
            Assert.IsTrue(slot.isDefenseActive);
            Assert.AreEqual(SkillTier.DefenseSkill, slot.ActiveSkill.tier);

            // Tap slot / portrait again -> reverts back to primary attack skill
            slot.ToggleDefense();
            Assert.IsFalse(slot.isDefenseActive);
            Assert.AreEqual(SkillTier.Skill1_PureDamage, slot.ActiveSkill.tier);
        }

        [Test]
        public void SkillSlotManager_ToggleSlotDefense_AffectsTargetSlotOnly()
        {
            var deck = new CombatDeckManager(seed: 123);
            var manager = new SkillSlotManager();
            manager.SetupSlots(3, deck);

            Assert.AreEqual(3, manager.Slots.Count);
            Assert.IsFalse(manager.Slots[1].isDefenseActive);

            manager.ToggleSlotDefense(1);
            Assert.IsTrue(manager.Slots[1].isDefenseActive);
            Assert.IsFalse(manager.Slots[0].isDefenseActive, "Other slots must remain unaffected");
            Assert.IsFalse(manager.Slots[2].isDefenseActive, "Other slots must remain unaffected");
        }
    }
}
#endif
