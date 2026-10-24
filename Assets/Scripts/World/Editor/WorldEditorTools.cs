using UnityEditor;
using UnityEngine;

namespace DigiPurity.World.Editor
{
    public static class WorldEditorTools
    {
        [MenuItem("File/Reload Active Scene From Disk %&#r", false, 150)]
        [MenuItem("DIGIPURITY/Reload Current Scene from Disk", false, 0)]
        public static void ReloadActiveScene()
        {
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(activeScene.path))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(activeScene.path, UnityEditor.SceneManagement.OpenSceneMode.Single);
                Debug.Log($"[DIGIPURITY] Reloaded scene '{activeScene.path}' from disk.");
            }
            else
            {
                string fallbackPath = System.IO.File.Exists("Assets/Scenes/TestScene.unity") ? "Assets/Scenes/TestScene.unity" : "Assets/Scenes/SampleScene.unity";
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(fallbackPath, UnityEditor.SceneManagement.OpenSceneMode.Single);
                Debug.Log($"[DIGIPURITY] Opened {fallbackPath} from disk.");
            }
        }

        [MenuItem("GameObject/DIGIPURITY/Rebuild Clean WalkableSquare (1:1 Scale + Walls)", false, 1)]
        public static void RebuildCleanSquare()
        {
            GameObject squareObj = GameObject.Find("WalkableSquare");
            if (squareObj == null) squareObj = GameObject.Find("Square");
            if (squareObj == null)
            {
                squareObj = new GameObject("WalkableSquare");
            }

            Undo.RegisterFullObjectHierarchyUndo(squareObj, "Rebuild WalkableSquare");

            squareObj.name = "WalkableSquare";
            squareObj.transform.position = Vector3.zero;
            squareObj.transform.localScale = Vector3.one;

            var sr = squareObj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = squareObj.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/WalkableSquare.png");
            sr.drawMode = SpriteDrawMode.Simple;
            sr.sortingOrder = -10;

            // Remove any old/broken colliders
            var existingCols = squareObj.GetComponents<BoxCollider2D>();
            for (int i = existingCols.Length - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(existingCols[i]);
            }

            // Clean 4 solid walls aligned directly with the outer blue borders (16x16 units)
            var topCol = squareObj.AddComponent<BoxCollider2D>();
            topCol.offset = new Vector2(0f, 7.8f);
            topCol.size = new Vector2(16f, 0.4f);

            var botCol = squareObj.AddComponent<BoxCollider2D>();
            botCol.offset = new Vector2(0f, -7.8f);
            botCol.size = new Vector2(16f, 0.4f);

            var leftCol = squareObj.AddComponent<BoxCollider2D>();
            leftCol.offset = new Vector2(-7.8f, 0f);
            leftCol.size = new Vector2(0.4f, 16f);

            var rightCol = squareObj.AddComponent<BoxCollider2D>();
            rightCol.offset = new Vector2(7.8f, 0f);
            rightCol.size = new Vector2(0.4f, 16f);

            var rb = squareObj.GetComponent<RoomBounds>();
            if (rb == null) rb = squareObj.AddComponent<RoomBounds>();
            rb.Source = RoomBounds.BoundsSource.Auto;

            // Ensure Main Camera has RoomCameraController
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var rcc = mainCam.GetComponent<RoomCameraController>();
                if (rcc == null) rcc = mainCam.gameObject.AddComponent<RoomCameraController>();
                rcc.SetOrthographicSize = 9.0f;
                rcc.CurrentRoom = rb;
                rcc.SnapToTarget();
            }

            Selection.activeGameObject = squareObj;
            Debug.Log("[DIGIPURITY] WalkableSquare has been completely rebuilt at 1:1 scale with solid blue wall colliders and RoomBounds!");
        }

        [MenuItem("GameObject/DIGIPURITY/Room Floor (Sprite + RoomBounds)", false, 2)]
        public static void CreateRoomFloor(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Room_Floor");
            go.transform.position = Vector3.zero;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -10;
            var rb = go.AddComponent<RoomBounds>();
            rb.Source = RoomBounds.BoundsSource.Auto;

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Room Floor");
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/DIGIPURITY/Solid Wall (Undertale obj_solid)", false, 10)]
        public static void CreateSolidWall(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Solid_Wall");
            go.transform.position = Vector3.zero;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2f, 2f);
            col.isTrigger = false;
            go.AddComponent<InvisibleWall>();

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Solid Wall");
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/DIGIPURITY/Room Camera Trigger (Bounds)", false, 11)]
        public static void CreateRoomBounds(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Room_Trigger");
            go.transform.position = Vector3.zero;
            var rb = go.AddComponent<RoomBounds>();
            rb.Source = RoomBounds.BoundsSource.Auto;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(16f, 12f);

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Room Trigger");
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/DIGIPURITY/Area Transition Door", false, 12)]
        public static void CreateAreaDoor(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Area_Door");
            go.transform.position = Vector3.zero;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2f, 1f);

            var door = go.AddComponent<AreaDoor>();
            door.DoorId = "Door_A";
            door.TargetDoorId = "Door_B";

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Area Door");
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/DIGIPURITY/Deltarune Secret Egg Door", false, 13)]
        public static void CreateEggDoor(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Secret_Egg_Door");
            go.transform.position = Vector3.zero;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2f, 1f);

            var door = go.AddComponent<AreaDoor>();
            door.DoorId = "Main_Door";
            door.TargetDoorId = "Next_Area_Door";
            door.HasSecretChance = true;
            door.SecretChancePercent = 2.0f; // 1 in 50
            door.SecretTargetDoorId = "EggRoom_Entrance";

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Secret Egg Door");
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/DIGIPURITY/NPC Creature (Solid)", false, 14)]
        public static void CreateOverworldCreature(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("NPC_Creature");
            go.transform.position = Vector3.zero;
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<OverworldCreature>();

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create NPC Creature");
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/DIGIPURITY/Spawn Golfer Battle Encounter (Overworld + Stage)", false, 15)]
        [MenuItem("DIGIPURITY/Setup Golfer Battle Encounter in Scene", false, 1)]
        public static void CreateGolferBattleEncounter()
        {
            CreateGolferBattleEncounter(null);
        }

        public static void CreateGolferBattleEncounter(MenuCommand menuCommand)
        {
            // 1. Overworld Golfer NPC (find existing Golfer or Overworld_Golfer)
            GameObject overworldGolfer = GameObject.Find("Golfer");
            if (overworldGolfer == null) overworldGolfer = GameObject.Find("Overworld_Golfer");
            if (overworldGolfer == null)
            {
                overworldGolfer = new GameObject("Golfer");
                overworldGolfer.transform.position = new Vector3(2.5f, 1.0f, 0f);
            }
            var sr = overworldGolfer.GetComponent<SpriteRenderer>();
            if (sr == null) sr = overworldGolfer.AddComponent<SpriteRenderer>();
            if (sr.sprite == null)
            {
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Golfer/Golfer_256.png");
                if (sr.sprite == null) sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Golfer/Golfer.png");
            }
            if (sr.sortingOrder <= 0) sr.sortingOrder = 10;

            var col = overworldGolfer.GetComponent<BoxCollider2D>();
            if (col == null)
            {
                col = overworldGolfer.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1.2f, 1.8f);
            }

            var enemyComp = overworldGolfer.GetComponent<DigiPurity.Combat.OverworldEnemy>();
            if (enemyComp == null) enemyComp = overworldGolfer.AddComponent<DigiPurity.Combat.OverworldEnemy>();
            enemyComp.AutoLoadSoul();

            // Load or create Golfer EnemyData
            if (!System.IO.Directory.Exists("Assets/Data/Enemies"))
            {
                System.IO.Directory.CreateDirectory("Assets/Data/Enemies");
            }
            var golferData = AssetDatabase.LoadAssetAtPath<DigiPurity.Combat.EnemyData>("Assets/Data/Enemies/Golfer_Data.asset");
            if (golferData == null)
            {
                golferData = ScriptableObject.CreateInstance<DigiPurity.Combat.EnemyData>();
                golferData.enemyId = "golfer";
                golferData.enemyName = "Golfer";
                golferData.description = "A rogue sports simulation entity infected by primitive AI scripts.";
                golferData.battleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Golfer/Golfer_256.png");
                golferData.bulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Combat/WhiteBullet_16.png");
                golferData.maxHp = 100;
                golferData.glitchThreshold = 50;
                golferData.basePower = 4;
                golferData.coinCount = 3;
                golferData.coinPower = 3;
                golferData.primarySkillName = "Putter Cleave";
                golferData.inBattleBarks = new string[]
                {
                    "PAR FOR THE COURSE.",
                    "NICE SHOT, OWEN.",
                    "OUT OF BOUNDS ERROR.",
                    "FORE! SYSTEM CORRUPT."
                };
                golferData.talkOptions = new System.Collections.Generic.List<DigiPurity.Combat.TalkOption>()
                {
                    new DigiPurity.Combat.TalkOption
                    {
                        optionName = "Check",
                        responseText = "GOLFER - ATK 4 DEF 2. Mindlessly repeating the 18th hole from 2004."
                    },
                    new DigiPurity.Combat.TalkOption
                    {
                        optionName = "Analyze Swing",
                        responseText = "You pointed out a hitch in his backswing. Golfer looks perturbed (+15 TP)!",
                        tpReward = 38
                    },
                    new DigiPurity.Combat.TalkOption
                    {
                        optionName = "Heckle",
                        responseText = "You yelled during his putting routine. His Charge dropped!",
                        chargeDelta = -15
                    }
                };
                AssetDatabase.CreateAsset(golferData, "Assets/Data/Enemies/Golfer_Data.asset");
                AssetDatabase.SaveAssets();
            }
            enemyComp.SetEnemyData(golferData);

            // 2. Battle Stage Master Container in Deep Void (1000, 1000)
            GameObject battleStage = GameObject.Find("BattleStage");
            if (battleStage == null)
            {
                battleStage = new GameObject("BattleStage");
            }
            battleStage.transform.SetParent(null); // Keep at scene root
            battleStage.transform.position = new Vector3(1000f, 1000f, 0f);

            var bsc = battleStage.GetComponent<DigiPurity.Combat.BattleStageController>();
            if (bsc == null) bsc = battleStage.AddComponent<DigiPurity.Combat.BattleStageController>();
            bsc.EnsureCombatantsExist();
            bsc.AutoLoadIcons();

            battleStage.SetActive(false); // Inactive in overworld until Owen touches Golfer!

            Undo.RegisterCreatedObjectUndo(overworldGolfer, "Spawn Overworld Golfer");
            Undo.RegisterCreatedObjectUndo(battleStage, "Spawn Battle Stage");

            Selection.activeGameObject = overworldGolfer;
            Debug.Log("[DIGIPURITY] Clean Deltarune Battle Stage in deep void (1000, 1000) configured!");
        }

        [MenuItem("DIGIPURITY/Setup Glitching Lucas Sign at Doorway", false, 60)]
        public static void SetupLucasSign()
        {
            var signObj = GameObject.Find("Lucas_Sign");
            if (signObj == null)
            {
                signObj = new GameObject("Lucas_Sign");
            }
            signObj.transform.position = LucasSign.DEFAULT_SIGN_POS;
            var signComp = signObj.GetComponent<LucasSign>();
            if (signComp == null) signComp = signObj.AddComponent<LucasSign>();

            Undo.RegisterCreatedObjectUndo(signObj, "Spawn Lucas Sign");
            Selection.activeGameObject = signObj;
            Debug.Log($"[DIGIPURITY] Glitching Lucas Sign placed at doorway {LucasSign.DEFAULT_SIGN_POS}!");
        }
    }
}
