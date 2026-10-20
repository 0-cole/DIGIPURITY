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
    }
}
