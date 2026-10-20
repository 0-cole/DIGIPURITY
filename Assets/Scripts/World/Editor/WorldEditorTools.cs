using UnityEditor;
using UnityEngine;

namespace DigiPurity.World.Editor
{
    public static class WorldEditorTools
    {
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
                rcc.SetOrthographicSize = 6.0f;
                rcc.CurrentRoom = rb;
                rcc.SnapToTarget();
            }

            Selection.activeGameObject = squareObj;
            Debug.Log("[DIGIPURITY] WalkableSquare has been completely rebuilt at 1:1 scale with solid blue wall colliders and RoomBounds!");
        }

        [MenuItem("GameObject/DIGIPURITY/Solid Wall (Undertale obj_solid)", false, 10)]
        public static void CreateSolidWall(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Solid_Wall");
            go.transform.position = Vector3.zero;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2f, 2f);

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
    }
}
