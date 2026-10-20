using UnityEditor;
using UnityEngine;

namespace DigiPurity.World.Editor
{
    public static class WorldEditorTools
    {
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
