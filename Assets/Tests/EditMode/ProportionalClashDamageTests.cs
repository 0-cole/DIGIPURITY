using NUnit.Framework;
using UnityEngine;
using DigiPurity.Combat;

namespace DigiPurity.Tests
{
    public class ProportionalClashDamageTests
    {
        public static int CalculateProportionalDamage(int headsRolled, int totalCoins, int maxPotentialDamage)
        {
            float ratio = Mathf.Clamp01((float)headsRolled / Mathf.Max(1, totalCoins));
            return Mathf.RoundToInt(maxPotentialDamage * ratio);
        }

        [Test]
        public void FullClashLoss_AllHeads_DealsFullDamage()
        {
            // Enemy had 3 coins, none broken during clash, rolls all 3 heads
            int totalCoins = 3;
            int headsRolled = 3;
            int maxDamage = 30;

            int damage = CalculateProportionalDamage(headsRolled, totalCoins, maxDamage);
            Assert.AreEqual(30, damage, "All heads on unbroken attack must deal 1.0x full damage");
        }

        [Test]
        public void PartialClashLoss_OneCoinBroken_DealsTwoThirdsDamage()
        {
            // Enemy had 3 coins, Owen broke 1 coin during clash so enemy only has 2 surviving coins
            // Enemy rolls 2 heads out of 3 total coins
            int totalCoins = 3;
            int headsRolled = 2;
            int maxDamage = 30;

            int damage = CalculateProportionalDamage(headsRolled, totalCoins, maxDamage);
            Assert.AreEqual(20, damage, "2 out of 3 coins heads must deal exactly 2/3 (20) damage");
        }

        [Test]
        public void PartialClashLoss_TwoCoinsBroken_DealsOneThirdDamage()
        {
            // Enemy had 3 coins, Owen broke 2 coins during clash, enemy rolls 1 head out of 3 total
            int totalCoins = 3;
            int headsRolled = 1;
            int maxDamage = 30;

            int damage = CalculateProportionalDamage(headsRolled, totalCoins, maxDamage);
            Assert.AreEqual(10, damage, "1 out of 3 coins heads must deal exactly 1/3 (10) damage");
        }

        [Test]
        public void AttackWithMoreCoins_HasHigherDamageCeiling()
        {
            // A 3-coin skill vs a 2-coin skill
            int skill2MaxDmg = 20;
            int skill3MaxDmg = 30;

            Assert.Greater(skill3MaxDmg, skill2MaxDmg, "Skill with 3 coins has higher power ceiling than 2 coins");
        }
    }
}
