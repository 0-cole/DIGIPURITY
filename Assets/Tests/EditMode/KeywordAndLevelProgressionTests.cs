#if UNITY_EDITOR
using NUnit.Framework;
using DigiPurity.Combat;
using DigiPurity.World;

namespace DigiPurity.Tests
{
    public class KeywordAndLevelProgressionTests
    {
        [Test]
        public void StutterBurst_RaisesGlitchThresholdAndTriggersGlitchedState()
        {
            var system = new StaggerAndGlitchSystem(maxHealth: 100);
            // Default HP 100, base Glitch threshold 50.
            system.TakeDamage(40); // HP is now 60, not yet Glitched
            Assert.IsFalse(system.isGlitched);

            // Stutter Burst adds +15 to threshold (50 -> 65)
            // Since current HP (60) <= raised threshold (65), unit enters Glitched state!
            bool triggered = system.TriggerStutterBurst(stutterPotency: 15);
            Assert.IsTrue(triggered);
            Assert.IsTrue(system.isGlitched);
            Assert.AreEqual(65, system.currentGlitchThreshold);
        }

        [Test]
        public void DeckManager_UnlocksSkill3AndSkill4UltimateWithKeywordAffinity()
        {
            var deck = new CombatDeckManager();
            deck.CurrentAffinity = KeywordType.Burn;
            deck.HasUnlockedSkill3 = true;
            deck.HasUnlockedSkill4 = true;
            deck.InitializeStandardDeck();

            var drawnSkills = new System.Collections.Generic.List<CombatSkill>();
            for (int i = 0; i < 50; i++)
            {
                drawnSkills.Add(deck.DrawSkill());
            }

            bool foundSkill3 = drawnSkills.Exists(s => s.tier == SkillTier.Skill3_KeywordSpecialist && s.skillName.Contains("Burn"));
            bool foundSkill4 = drawnSkills.Exists(s => s.tier == SkillTier.Skill4_UltimateMove && s.chargeCost > 0);

            Assert.IsTrue(foundSkill3, "Skill 3 Burn specialist should be in deck");
            Assert.IsTrue(foundSkill4, "Skill 4 Ultimate move should be in deck");
        }

        [Test]
        public void ZoneProgression_LevelCapIncreasesPerZone()
        {
            var zone = new ZoneProgressionData();
            Assert.AreEqual(1, zone.currentZone);
            Assert.AreEqual(25, zone.MaxLevelForCurrentZone);

            // Accumulate massive EXP in Zone 1 (3000 EXP = normally Lv 301)
            zone.totalExp = 3000;
            zone.RecordEncounterFinished(wasPurged: false, baseExp: 10);
            Assert.AreEqual(25, zone.currentLevel, "Zone 1 must strictly cap at Level 25");

            // Advance to Zone 2 (Cap 50)
            zone.AdvanceToNextZone();
            Assert.AreEqual(2, zone.currentZone);
            Assert.AreEqual(50, zone.MaxLevelForCurrentZone);
            zone.RecordEncounterFinished(wasPurged: false, baseExp: 10);
            Assert.AreEqual(50, zone.currentLevel, "Zone 2 must strictly cap at Level 50");

            // Advance to Zone 3 (Cap 75)
            zone.AdvanceToNextZone();
            Assert.AreEqual(3, zone.currentZone);
            Assert.AreEqual(75, zone.MaxLevelForCurrentZone);
            zone.RecordEncounterFinished(wasPurged: false, baseExp: 10);
            Assert.AreEqual(75, zone.currentLevel, "Zone 3 must strictly cap at Level 75");

            // Advance to Zone 4 (Cap 100)
            zone.AdvanceToNextZone();
            Assert.AreEqual(4, zone.currentZone);
            Assert.AreEqual(100, zone.MaxLevelForCurrentZone);
            zone.RecordEncounterFinished(wasPurged: false, baseExp: 10);
            Assert.AreEqual(100, zone.currentLevel, "Zone 4 must cap at Level 100");
        }
    }
}
#endif
