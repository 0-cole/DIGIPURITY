#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using DigiPurity.Combat;

namespace DigiPurity.Tests
{
    public class CombatSystemTests
    {
        [Test]
        public void Limbus_HeadsProbability_CalculatesCorrectly()
        {
            // At 0 sanity, probability should be 50%
            Assert.AreEqual(0.50f, CoinClashResolver.CalculateHeadsProbability(0), 0.001f);

            // At +45 sanity, probability should be 95%
            Assert.AreEqual(0.95f, CoinClashResolver.CalculateHeadsProbability(45), 0.001f);

            // At -45 sanity, probability should be 5%
            Assert.AreEqual(0.05f, CoinClashResolver.CalculateHeadsProbability(-45), 0.001f);
        }

        [Test]
        public void Limbus_ClashResolution_LoserLosesCoin()
        {
            var resolver = new CoinClashResolver(seed: 42);
            var pSkill = new SkillData { skillName = "Heavy Purge", basePower = 10, coinPower = 5, coinCount = 2 };
            var eSkill = new SkillData { skillName = "Weak Glitch", basePower = 1, coinPower = 1, coinCount = 2 };

            int pCoins = 2;
            int eCoins = 2;

            // Player has overwhelmingly superior base power, should win round
            var result = resolver.ResolveClashRound(pSkill, 40, ref pCoins, eSkill, -40, ref eCoins);

            Assert.IsTrue(result.playerWonRound);
            Assert.AreEqual(2, pCoins, "Winner coins must remain unchanged");
            Assert.AreEqual(1, eCoins, "Loser must forfeit one coin");
        }

        [Test]
        public void Undertale_SoulMovement_CancelSlowsByHalf()
        {
            var logic = new SoulMovementLogic(speed: 4f);
            Vector2 origin = Vector2.zero;
            Vector2 dir = Vector2.right;

            Vector2 normalPos = logic.CalculateNextPosition(origin, dir, slowHeld: false, deltaTime: 1f);
            Vector2 slowedPos = logic.CalculateNextPosition(origin, dir, slowHeld: true, deltaTime: 1f);

            Assert.AreEqual(4f, normalPos.x, 0.001f);
            Assert.AreEqual(2f, slowedPos.x, 0.001f, "Holding cancel must halve movement speed exactly per Undertale bytecode");
        }

        [Test]
        public void Undertale_SoulMovement_DiagonalMovesAtOnePointFiveSpeed()
        {
            var logic = new SoulMovementLogic(speed: 4f, diagonalMult: 1.5f);
            Vector2 origin = Vector2.zero;
            Vector2 dir = new Vector2(1f, 1f);

            Vector2 diagPos = logic.CalculateNextPosition(origin, dir, slowHeld: false, deltaTime: 1f);

            float distance = Vector2.Distance(origin, diagPos);
            Assert.AreEqual(6f, distance, 0.001f, "Diagonal soul movement must be exactly 1.5x regular speed");
        }

        [Test]
        public void CombatStateMachine_SlotsGrowWithTurns()
        {
            var sm = new CombatStateMachine();
            Assert.AreEqual(1, sm.TurnNumber);
            Assert.AreEqual(1, sm.PlayerSlots);

            sm.AdvanceTurnSlots();
            Assert.AreEqual(2, sm.TurnNumber);
            Assert.AreEqual(2, sm.PlayerSlots);

            sm.AdvanceTurnSlots();
            Assert.AreEqual(3, sm.TurnNumber);
            Assert.AreEqual(3, sm.PlayerSlots);
        }
    }
}
#endif
