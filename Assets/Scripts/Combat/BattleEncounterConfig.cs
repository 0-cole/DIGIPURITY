using System.Collections.Generic;
using UnityEngine;

namespace DigiPurity.Combat
{
    /// <summary>
    /// Attach BattleEncounterConfig to ANY OverworldEnemy (or trigger) to configure a fully modular
    /// 1..4 enemy battle right in the Unity Inspector!
    /// - Adjust 'numberOfEnemies' (1..4) and it automatically creates Enemy [1]..Enemy [N] slots!
    /// - Includes Golfer's attack patterns pre-loaded as baseline presets ready to tweak or replace with Custom Attack Prefabs.
    /// - Supports Combined Multi-Enemy Attacks, Boss/Basic enemy types, Conditional Dialogue, ACT/Purify builders, and Song Rules.
    /// </summary>
    public class BattleEncounterConfig : MonoBehaviour
    {
        [Header("1. Number of Enemies & Stage Formation")]
        [Tooltip("Set how many enemies are in this battle (1 to 4). Changing this automatically creates/resizes the Enemy Slots list below!")]
        [Range(1, 4)]
        public int numberOfEnemies = 1;

        [Tooltip("Automatically appends A, B, C (e.g. 'Golfer A', 'Golfer B') when two enemies share the exact same name.")]
        public bool autoLetterDuplicateNames = true;

        public EnemyFormationPreset formationPreset = EnemyFormationPreset.AutoDiagonalStagger;

        [Tooltip("Edit every enemy's type (Boss/Basic), name, HP/damage types, attack patterns, conditional dialogue, sprites, and ACT/Purify thresholds!")]
        public List<BattleEncounterSlot> enemies = new List<BattleEncounterSlot>();

        [Header("4. Combined Multi-Enemy Attacks (Team / Duo Attacks)")]
        [Tooltip("Special combined attacks that trigger while specific enemy pairs/trios are alive together!")]
        public List<CombinedAttackRule> combinedAttacks = new List<CombinedAttackRule>();

        [Header("Encounter Song & Conditional Song Rules")]
        [Tooltip("Optional song for this whole encounter (if empty, uses Enemy [1]'s baseProfile song or BattleStageController default).")]
        public AudioClip encounterSong;
        [Range(0f, 1f)] public float bgmVolume = 0.85f;
        [Range(0.2f, 2.0f)] public float bgmStartPitch = 1.0f;
        public bool enableAutoLowHpSlowdown = true;
        public List<BattleSongRule> conditionalSongRules = new List<BattleSongRule>();

        [Header("Custom Stage Dodge Box (Optional Encounter Override)")]
        public bool useCustomDodgeBox = false;
        public Vector2 customDodgeBoxOffset = new Vector2(0.85f, 1.35f);
        public Vector2 customDodgeBoxSize = new Vector2(3.6f, 2.7f);

        [Header("Intro Flavor Text & Tutorial Mode")]
        [Tooltip("Optional custom opening narration in the bottom bar on Turn 1 (leave empty for automatic intro).")]
        [TextArea(1, 2)]
        public string customOpeningFlavorText = "";
        public bool runTutorialDriverIfPresent = false;

        private void Reset()
        {
            SyncSlotCountWithNumberOfEnemies();
        }

        private void OnValidate()
        {
            SyncSlotCountWithNumberOfEnemies();
        }

        public void SyncSlotCountWithNumberOfEnemies()
        {
            numberOfEnemies = Mathf.Clamp(numberOfEnemies, 1, 4);
            if (enemies == null) enemies = new List<BattleEncounterSlot>();

            EnemyData defaultGolfer = null;
            var owEnemy = GetComponent<OverworldEnemy>();
            if (owEnemy != null && owEnemy.Data != null)
            {
                defaultGolfer = owEnemy.Data;
            }
#if UNITY_EDITOR
            if (defaultGolfer == null)
            {
                defaultGolfer = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/Enemies/Golfer_Data.asset");
            }
#endif

            while (enemies.Count < numberOfEnemies)
            {
                var presetSlot = BattleEncounterSlot.CreateGolferPresetSlot(defaultGolfer);
                enemies.Add(presetSlot);
            }

            while (enemies.Count > numberOfEnemies)
            {
                enemies.RemoveAt(enemies.Count - 1);
            }
        }

        [ContextMenu("Load Golfer Baseline Preset Into All Slots")]
        public void ApplyGolferBaselineToAllSlots()
        {
            SyncSlotCountWithNumberOfEnemies();
            EnemyData golferAsset = null;
#if UNITY_EDITOR
            golferAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/Enemies/Golfer_Data.asset");
#endif
            for (int i = 0; i < enemies.Count; i++)
            {
                enemies[i] = BattleEncounterSlot.CreateGolferPresetSlot(golferAsset);
            }
        }

        /// <summary>
        /// Calculates the world stage position for Enemy [slotIndex] out of [totalCount] enemies.
        /// </summary>
        public static Vector3 CalculateSlotStagePosition(
            Vector3 stageCenter,
            Vector2 primaryEnemyOffset,
            int slotIndex,
            int totalCount,
            EnemyFormationPreset formation,
            Vector2 perEnemyManualOffset,
            float groundLineOffset = 0f)
        {
            Vector2 basePos = new Vector2(stageCenter.x + primaryEnemyOffset.x, stageCenter.y + primaryEnemyOffset.y);

            if (totalCount <= 1 || formation == EnemyFormationPreset.ManualOffsetsOnly)
            {
                return new Vector3(
                    basePos.x + perEnemyManualOffset.x,
                    basePos.y + perEnemyManualOffset.y + groundLineOffset,
                    0f);
            }

            Vector2 formationShift = Vector2.zero;
            float centerIdx = (totalCount - 1) * 0.5f;
            float rel = slotIndex - centerIdx; // e.g. -0.5, +0.5 for 2 enemies; -1, 0, +1 for 3 enemies

            switch (formation)
            {
                case EnemyFormationPreset.AutoDiagonalStagger:
                    // Top-back to bottom-front Deltarune diagonal stagger so sprites never block each other
                    formationShift = new Vector2(rel * 0.85f, -rel * 1.55f);
                    break;

                case EnemyFormationPreset.VerticalColumn:
                    formationShift = new Vector2(0f, -rel * 1.65f);
                    break;

                case EnemyFormationPreset.BossCenterWithFlanks:
                    if (slotIndex == 0)
                    {
                        formationShift = new Vector2(0.35f, 0f); // Boss prominent center-right
                    }
                    else
                    {
                        float flankDir = (slotIndex % 2 == 1) ? 1f : -1f;
                        float tier = Mathf.Ceil(slotIndex * 0.5f);
                        formationShift = new Vector2(-0.95f, flankDir * 1.75f * tier);
                    }
                    break;
            }

            return new Vector3(
                basePos.x + formationShift.x + perEnemyManualOffset.x,
                basePos.y + formationShift.y + perEnemyManualOffset.y + groundLineOffset,
                0f);
        }
    }
}
