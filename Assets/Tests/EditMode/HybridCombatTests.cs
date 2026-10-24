#if UNITY_EDITOR
using NUnit.Framework;
using DigiPurity.Combat;
using DigiPurity.World;

namespace DigiPurity.Tests
{
    public class HybridCombatTests
    {
        [Test]
        public void Tension_DefendAwardsFortyPoints_DeltaruneInvariant()
        {
            var system = new TensionAndChargeSystem(initialCharge: 0, initialTension: 0f);
            system.AddDefendTension();

            // 40 points = exactly 16% of 250 max tension
            Assert.AreEqual(40f, system.CurrentTension, 0.001f);
            Assert.AreEqual(16f, system.TensionPercentage, 0.001f);
        }

        [Test]
        public void Tension_SpendBoostsCharge_ClampedAtBoundaries()
        {
            var system = new TensionAndChargeSystem(initialCharge: 40, initialTension: 100f);
            bool success = system.SpendTensionToBoostCharge(tensionCost: 50f, chargeBoost: 10);

            Assert.IsTrue(success);
            Assert.AreEqual(50f, system.CurrentTension, 0.001f);
            Assert.AreEqual(45, system.CurrentCharge, "Charge must clamp at max +45");
        }

        [Test]
        public void Keywords_BurnAndBleedProcessCorrectly()
        {
            var processor = new KeywordProcessor();
            processor.ApplyKeyword(KeywordType.Burn, potency: 5, count: 2);
            processor.ApplyKeyword(KeywordType.Bleed, potency: 3, count: 1);

            int burnDmg1 = processor.ProcessTurnEndBurn();
            Assert.AreEqual(5, burnDmg1);

            int bleedDmg1 = processor.ProcessCoinFlipBleed();
            Assert.AreEqual(3, bleedDmg1);

            // Bleed count was 1, so second coin flip should deal 0 bleed damage
            int bleedDmg2 = processor.ProcessCoinFlipBleed();
            Assert.AreEqual(0, bleedDmg2);
        }

        [Test]
        public void ZoneProgression_PurgingAwardsExtraExp()
        {
            var zone = new ZoneProgressionData();
            zone.RecordEncounterFinished(wasPurged: true, baseExp: 20);

            // 20 * 1.5 = 30 EXP -> unlocks Level 4 with formula 1 + (30 / 10) = 4, capped at Zone 1 (25)
            Assert.AreEqual(30, zone.totalExp);
            Assert.AreEqual(4, zone.currentLevel);
        }
    }
}
#endif
