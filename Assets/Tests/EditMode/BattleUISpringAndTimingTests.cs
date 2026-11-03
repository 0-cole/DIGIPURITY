#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using DigiPurity.Combat;

namespace DigiPurity.Tests
{
    public class BattleUISpringAndTimingTests
    {
        [Test]
        public void SideButtonSpring_ExpandsWithBounceAndSettlesAtOne()
        {
            float pos = 0f;
            float vel = 0f;
            float maxObserved = 0f;

            for (int step = 0; step < 60; step++)
            {
                BattleStageController.StepButtonBounceSpring(ref pos, ref vel, 1.0f, 1f / 60f);
                if (pos > maxObserved) maxObserved = pos;
            }

            Assert.Greater(maxObserved, 1.04f, "Highlighted button should bounce slightly past 1.0 before settling.");
            Assert.AreEqual(1.0f, pos, 0.01f, "Highlighted button should settle cleanly at expanded target 1.0.");
        }

        [Test]
        public void SideButtonSpring_ShrinksWithBounceAndSettlesAtNeutralZero()
        {
            float pos = 1.0f;
            float vel = 0f;
            float minObserved = 1.0f;

            for (int step = 0; step < 60; step++)
            {
                BattleStageController.StepButtonBounceSpring(ref pos, ref vel, 0.0f, 1f / 60f);
                if (pos < minObserved) minObserved = pos;
            }

            Assert.Less(minObserved, -0.04f, "Deselected button should softly bounce past neutral 0.0 before settling.");
            Assert.AreEqual(0.0f, pos, 0.01f, "Deselected button should settle cleanly at neutral 0.0.");
        }

        [Test]
        public void AttackTimingBar_DealsHigherDamageNearCenterSweetSpot()
        {
            float critMult = BattleStageController.CalculateAttackTimingMultiplier(0.50f, out string critLabel);
            float greatMult = BattleStageController.CalculateAttackTimingMultiplier(0.60f, out string greatLabel);
            float goodMult = BattleStageController.CalculateAttackTimingMultiplier(0.75f, out string goodLabel);
            float grazeMult = BattleStageController.CalculateAttackTimingMultiplier(0.95f, out string grazeLabel);
            float missMult = BattleStageController.CalculateAttackTimingMultiplier(1.06f, out string missLabel);

            Assert.AreEqual("CRITICAL!!", critLabel);
            Assert.AreEqual("GREAT!", greatLabel);
            Assert.AreEqual("GOOD", goodLabel);
            Assert.AreEqual("GRAZE", grazeLabel);
            Assert.AreEqual("MISS", missLabel);

            Assert.Greater(critMult, greatMult);
            Assert.Greater(greatMult, goodMult);
            Assert.Greater(goodMult, grazeMult);
            Assert.AreEqual(0f, missMult, 0.0001f);
        }

        [Test]
        public void EnemyData_SupportsCustomDodgeBoxPlacementAndDecoupledTutorialFlag()
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.enemyId = "triple_t";
            data.enemyName = "Triple T";
            data.useCustomDodgeBox = true;
            data.customDodgeBoxOffset = new Vector2(1.2f, 0.4f);
            data.customDodgeBoxSize = new Vector2(4.8f, 3.2f);
            data.speechBubbleOffset = new Vector2(-24f, 10f);
            data.useTutorialOnboarding = false;

            Assert.IsTrue(data.useCustomDodgeBox);
            Assert.AreEqual(new Vector2(1.2f, 0.4f), data.customDodgeBoxOffset);
            Assert.AreEqual(new Vector2(4.8f, 3.2f), data.customDodgeBoxSize);
            Assert.AreEqual(new Vector2(-24f, 10f), data.speechBubbleOffset);
            Assert.IsFalse(data.useTutorialOnboarding);

            Object.DestroyImmediate(data);
        }

        [Test]
        public void BattleSongRule_MatchesHpPurifyTurnAndSpecificActConditions()
        {
            var hpRule = new BattleSongRule
            {
                condition = SongTriggerCondition.EnemyHpAtOrBelowPercent,
                thresholdValue = 50f
            };
            Assert.IsFalse(hpRule.Matches(SongTriggerCondition.EnemyHpAtOrBelowPercent, 75f, 100f, 0f, 1));
            Assert.IsTrue(hpRule.Matches(SongTriggerCondition.EnemyHpAtOrBelowPercent, 50f, 100f, 0f, 1));

            var owenPinchRule = new BattleSongRule
            {
                condition = SongTriggerCondition.OwenHpAtOrBelowPercent,
                thresholdValue = 25f
            };
            Assert.IsFalse(owenPinchRule.Matches(SongTriggerCondition.OwenHpAtOrBelowPercent, 100f, 40f, 0f, 2));
            Assert.IsTrue(owenPinchRule.Matches(SongTriggerCondition.OwenHpAtOrBelowPercent, 100f, 20f, 0f, 2));

            var purifyRule = new BattleSongRule
            {
                condition = SongTriggerCondition.PurifyAtOrAbovePercent,
                thresholdValue = 100f
            };
            Assert.IsFalse(purifyRule.Matches(SongTriggerCondition.PurifyAtOrAbovePercent, 100f, 100f, 50f, 2));
            Assert.IsTrue(purifyRule.Matches(SongTriggerCondition.PurifyAtOrAbovePercent, 100f, 100f, 100f, 2));

            var turnRule = new BattleSongRule
            {
                condition = SongTriggerCondition.OnTurnNumberReached,
                thresholdValue = 3f
            };
            Assert.IsFalse(turnRule.Matches(SongTriggerCondition.OnTurnNumberReached, 100f, 100f, 0f, 2));
            Assert.IsTrue(turnRule.Matches(SongTriggerCondition.OnTurnNumberReached, 100f, 100f, 0f, 3));

            var actRule = new BattleSongRule
            {
                condition = SongTriggerCondition.OnSpecificActUsed,
                triggerActName = "Heckle"
            };
            Assert.IsFalse(actRule.Matches(SongTriggerCondition.OnSpecificActUsed, 100f, 100f, 0f, 1, "Check"));
            Assert.IsTrue(actRule.Matches(SongTriggerCondition.OnSpecificActUsed, 100f, 100f, 0f, 1, "heckle"));
        }

        [Test]
        public void ModularBattleSystem_ResizesMultiEnemySlotsWithGolferBaselinePresetsAndDisambiguatesNames()
        {
            var go = new GameObject("ModularEncounterTest");
            var config = go.AddComponent<BattleEncounterConfig>();
            config.numberOfEnemies = 3;
            config.autoLetterDuplicateNames = true;
            config.SyncSlotCountWithNumberOfEnemies();

            Assert.AreEqual(3, config.enemies.Count, "Changing numberOfEnemies to 3 must auto-populate 3 Enemy slots.");
            Assert.GreaterOrEqual(config.enemies[0].attackPatterns.Count, 3, "Each slot should start with Golfer's baseline attack presets.");
            Assert.AreEqual(AttackPresetType.Golfer_GolfBallRain, config.enemies[0].attackPatterns[0].presetType);
            Assert.AreEqual(AttackPresetType.Golfer_SandTrapSpread, config.enemies[0].attackPatterns[1].presetType);
            Assert.Greater(config.enemies[0].attackPatterns[1].coreDamage, config.enemies[0].attackPatterns[1].outerSplashDamage,
                "Golfer Sand Trap Spread preset should distinguish heavy core damage from lighter outer sand splash damage.");

            var stageGo = new GameObject("ModularStageTest");
            var stage = stageGo.AddComponent<BattleStageController>();
            stage.InitializeWithEncounter(config);

            Assert.AreEqual(3, stage.EnemySlots.Count);
            Assert.AreEqual("Golfer A", stage.EnemySlots[0].displayName);
            Assert.AreEqual("Golfer B", stage.EnemySlots[1].displayName);
            Assert.AreEqual("Golfer C", stage.EnemySlots[2].displayName);

            Object.DestroyImmediate(stageGo);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void AttackDamageZone_AndConditionalDialogue_RespectRulesAndPriorities()
        {
            // Movement-conditional Undertale Cyan & Orange hazard rules
            Assert.IsFalse(AttackDamageZone.ShouldTriggerForMovementRule(HazardMovementRule.CyanStopToAvoid, soulIsMoving: false));
            Assert.IsTrue(AttackDamageZone.ShouldTriggerForMovementRule(HazardMovementRule.CyanStopToAvoid, soulIsMoving: true));
            Assert.IsTrue(AttackDamageZone.ShouldTriggerForMovementRule(HazardMovementRule.OrangeMoveToAvoid, soulIsMoving: false));
            Assert.IsFalse(AttackDamageZone.ShouldTriggerForMovementRule(HazardMovementRule.OrangeMoveToAvoid, soulIsMoving: true));

            // Conditional dialogue matching active attack pattern index & HP threshold
            var atkRule = new ConditionalDialogueRule
            {
                triggerType = DialogueTriggerType.WhenUsingAttackIndex,
                attackPatternIndex = 2,
                dialogueLines = new System.Collections.Generic.List<string> { "RICOCHET DRIVE!" }
            };
            Assert.IsFalse(atkRule.Matches(DialogueTriggerType.WhenUsingAttackIndex, 1, 100f, 0f, chosenAttackIndex: 0));
            Assert.IsTrue(atkRule.Matches(DialogueTriggerType.WhenUsingAttackIndex, 1, 100f, 0f, chosenAttackIndex: 2));
            Assert.AreEqual("RICOCHET DRIVE!", atkRule.NextLine());

            // Damage type affinity multiplier
            var slot = BattleEncounterSlot.CreateGolferPresetSlot();
            slot.damageAffinities.Add(new DamageAffinityEntry { damageType = BattleDamageType.PhysicalScythe, multiplier = 1.5f });
            Assert.AreEqual(1.5f, slot.GetDamageMultiplier(BattleDamageType.PhysicalScythe), 0.001f);
            Assert.AreEqual(1.0f, slot.GetDamageMultiplier(BattleDamageType.ThermalBurn), 0.001f);
        }
    }
}
#endif
