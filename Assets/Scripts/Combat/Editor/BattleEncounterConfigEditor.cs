#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DigiPurity.Combat.Editor
{
    [CustomEditor(typeof(BattleEncounterConfig))]
    public class BattleEncounterConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var config = (BattleEncounterConfig)target;

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "MODULAR MULTI-ENEMY BATTLE BUILDER\n" +
                "• Adjust 'Number Of Enemies' (1–4) to automatically create editable Enemy [1..N] slots.\n" +
                "• Each slot comes pre-loaded with Golfer's attack patterns as baseline presets—tweak speeds/damage or drop in your own Custom Attack Prefabs (with AttackDamageZone child hitboxes)!",
                MessageType.Info);

            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                wordWrap = true,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(6, 6, 6, 6)
            };

            if (GUILayout.Button("Load Golfer Baseline Preset (All Slots)", buttonStyle, GUILayout.MinHeight(30)))
            {
                Undo.RecordObject(config, "Load Golfer Baseline Presets");
                config.ApplyGolferBaselineToAllSlots();
                EditorUtility.SetDirty(config);
            }

            EditorGUILayout.Space(2);

            if (GUILayout.Button("Create Sample Custom Attack Prefab", buttonStyle, GUILayout.MinHeight(30)))
            {
                CreateSampleCustomAttackPrefab();
            }

            EditorGUILayout.Space(6);
            DrawDefaultInspector();
        }

        private static void CreateSampleCustomAttackPrefab()
        {
            const string folderPath = "Assets/Prefabs/Combat/Attacks";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/CustomAttack_GolferClubSweep.prefab");
            Sprite bulletSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Combat/WhiteBullet_16.png");

            GameObject root = new GameObject("CustomAttack_GolferClubSweep");

            // 1. Telegraph Warning Zone (0 DMG for 0.6s)
            GameObject warningObj = new GameObject("1_TelegraphWarningZone_0DMG");
            warningObj.transform.SetParent(root.transform);
            warningObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            var warnBox = warningObj.AddComponent<BoxCollider2D>();
            warnBox.isTrigger = true;
            warnBox.size = new Vector2(3.2f, 0.45f);
            var warnZone = warningObj.AddComponent<AttackDamageZone>();
            warnZone.zoneName = "Telegraph Warning Line (0 DMG)";
            warnZone.damage = 0;
            warnZone.activeDurationSeconds = 0.60f;

            // 2. Heavy Core Sweet Spot (20 DMG after 0.60s delay!)
            GameObject coreObj = new GameObject("2_ClubHead_HeavyCore_20DMG");
            coreObj.transform.SetParent(root.transform);
            coreObj.transform.localPosition = new Vector3(-1.4f, 0.35f, 0f);
            coreObj.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            var coreSr = coreObj.AddComponent<SpriteRenderer>();
            coreSr.sprite = bulletSpr;
            coreSr.color = new Color(1f, 0.3f, 0.25f, 1f);
            coreSr.sortingOrder = 360;
            var coreCol = coreObj.AddComponent<CircleCollider2D>();
            coreCol.isTrigger = true;
            coreCol.radius = 0.32f;
            var coreZone = coreObj.AddComponent<AttackDamageZone>();
            coreZone.zoneName = "Club Head Sweet Spot (Heavy 20 DMG)";
            coreZone.damage = 20;
            coreZone.linearVelocity = new Vector2(3.4f, 0f);
            coreZone.bounceInsideDodgeBox = true;
            coreZone.activationDelaySeconds = 0.60f;

            // 3. Outer Sand Splash Sparks (6 DMG light splash)
            GameObject splashObj = new GameObject("3_SandSplash_Light_6DMG");
            splashObj.transform.SetParent(root.transform);
            splashObj.transform.localPosition = new Vector3(-1.4f, -0.35f, 0f);
            splashObj.transform.localScale = new Vector3(0.38f, 0.38f, 1f);
            var splashSr = splashObj.AddComponent<SpriteRenderer>();
            splashSr.sprite = bulletSpr;
            splashSr.color = new Color(1f, 0.88f, 0.45f, 1f);
            splashSr.sortingOrder = 355;
            var splashZone = splashObj.AddComponent<AttackDamageZone>();
            splashZone.zoneName = "Outer Sand Spray (Light 6 DMG)";
            splashZone.damage = 6;
            splashZone.fallbackHitRadius = 0.18f;
            splashZone.linearVelocity = new Vector2(2.6f, 0.65f);
            splashZone.bounceInsideDodgeBox = true;
            splashZone.activationDelaySeconds = 0.60f;

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log($"[DIGIPURITY] Created sample Custom Attack Prefab with Heavy Core (20 DMG), Light Splash (6 DMG), and Telegraph Warning (0 DMG) at: {prefabPath}");
        }
    }
}
#endif
