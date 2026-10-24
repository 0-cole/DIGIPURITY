#if UNITY_EDITOR
using NUnit.Framework;
using DigiPurity.Combat;

namespace DigiPurity.Tests
{
    public class HybridKeywordCombinationTests
    {
        [Test]
        public void LoadoutManager_DetectsCobaltCombustionWhenResonanceItemsEquipped()
        {
            var loadout = new EquipmentLoadoutManager();
            var burnCore = new EquipmentItem("item_burn_core", "Thermo Overclock", KeywordType.Burn, resonance: true);
            var poiseCore = new EquipmentItem("item_poise_core", "Gyroscopic Focus", KeywordType.Poise, resonance: true);

            loadout.EquipItem(burnCore);
            loadout.EquipItem(poiseCore);

            var activeHybrids = loadout.EvaluateActiveHybridKeywords();
            Assert.IsTrue(activeHybrids.Contains(HybridKeywordType.CobaltCombustion), "Burn + Poise resonance must activate Cobalt Combustion");
        }

        [Test]
        public void CobaltCombustion_ConsumesPoiseForCriticalBurnDamage()
        {
            var processor = new KeywordProcessor();
            processor.ApplyKeyword(KeywordType.Burn, potency: 10, count: 2);
            processor.ApplyKeyword(KeywordType.Poise, potency: 20, count: 1);

            // With Cobalt Combustion active, 10 Burn deals 1.5x (15) and consumes 1 Poise count
            int burnDamage = processor.ProcessTurnEndBurn(hasCobaltCombustion: true);
            Assert.AreEqual(15, burnDamage);

            // Poise should have been consumed
            Assert.IsNull(processor.GetStatus(KeywordType.Poise), "Poise count must be exhausted after triggering Blue Burn");
        }

        [Test]
        public void CrimsonLacerate_DoublesBleedOnCriticalCoinFlips()
        {
            var processor = new KeywordProcessor();
            processor.ApplyKeyword(KeywordType.Bleed, potency: 6, count: 2);

            // Regular bleed deals 6
            // Crimson Lacerate crit deals 12
            int critBleed = processor.ProcessCoinFlipBleed(hasCrimsonLacerate: true, isCrit: true);
            Assert.AreEqual(12, critBleed);
        }

        [Test]
        public void StaticSeizure_DrainsChargeAndRaisesGlitchThreshold()
        {
            var processor = new KeywordProcessor();
            var glitchSystem = new StaggerAndGlitchSystem(maxHealth: 100);

            Assert.AreEqual(50, glitchSystem.currentGlitchThreshold);

            // Draining 10 charge with Static Seizure adds +10 directly to Glitch threshold
            processor.ProcessDrainageAndStutter(drainedCharge: 10, hasStaticSeizure: true, enemyGlitchSystem: glitchSystem);
            Assert.AreEqual(60, glitchSystem.currentGlitchThreshold);
        }
    }
}
#endif
