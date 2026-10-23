using NUnit.Framework;
using UnityEngine;
using DigiPurity.Combat;

namespace DigiPurity.Tests
{
    public class EnemyDataEncounterTests
    {
        [Test]
        public void EnemyData_HoldsCustomDescriptorsAndBarks()
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.enemyName = "Shrimp Jesus";
            data.inBattleBarks = new string[] { "AMEN FOR THE ALGORITHM.", "LIKE AND SHARE OR FACE DELETION." };
            data.talkOptions = new System.Collections.Generic.List<TalkOption>()
            {
                new TalkOption { optionName = "Pray", responseText = "You recited obsolete terms of service.", tpReward = 25 },
                new TalkOption { optionName = "Debunk", responseText = "You zoomed in on the extra crustaceans. Jesus is perturbed!", chargeDelta = -20 }
            };

            Assert.AreEqual("Shrimp Jesus", data.enemyName);
            Assert.AreEqual(2, data.inBattleBarks.Length);
            Assert.AreEqual("Pray", data.talkOptions[0].optionName);
            Assert.AreEqual(25, data.talkOptions[0].tpReward);
            Assert.AreEqual(-20, data.talkOptions[1].chargeDelta);
        }

        [Test]
        public void TensionSystem_ProcessesTalkOptionRewards()
        {
            var system = new TensionAndChargeSystem(initialCharge: 0, initialTension: 50f);
            var opt = new TalkOption { optionName = "Analyze Swing", tpReward = 38, chargeDelta = -15 };

            system.AddGrazeTension(opt.tpReward);
            system.ModifyCharge(opt.chargeDelta);

            Assert.AreEqual(88f, system.CurrentTension, 0.001f);
            Assert.AreEqual(-15, system.CurrentCharge);
        }
    }
}
