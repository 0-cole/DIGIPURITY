using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace DigiPurity.World
{
    [CustomEditor(typeof(ModularRoom))]
    public class ModularRoomEditor : Editor
    {
        private BoxBoundsHandle _boundsHandle = new BoxBoundsHandle();

        public override void OnInspectorGUI()
        {
            ModularRoom room = (ModularRoom)target;

            serializedObject.Update();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("📐 Room Dimensions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("roomName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("roomSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("wallThickness"));

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("🚪 Doorways & Openings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("openLeft"), new GUIContent("Open Left Doorway"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("openRight"), new GUIContent("Open Right Doorway"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("openTop"), new GUIContent("Open Top Doorway"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("openBottom"), new GUIContent("Open Bottom Doorway"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("doorwayWidth"), new GUIContent("Doorway Width"));

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("🎨 Visuals & Rendering", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("floorSprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tileFloor"), new GUIContent("Tile Floor Graphic"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sortingOrder"));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(10);

            // Spacious Full-Width Action Buttons (Never Truncated!)
            GUI.backgroundColor = new Color(0.2f, 0.7f, 1f);
            if (GUILayout.Button("🔄 Rebuild Room & Colliders", GUILayout.Height(30)))
            {
                Undo.RecordObject(room, "Rebuild Room");
                room.RebuildRoom();
                EditorUtility.SetDirty(room);
            }

            EditorGUILayout.Space(3);
            GUI.backgroundColor = new Color(1f, 0.75f, 0.2f);
            if (GUILayout.Button("🎯 Fit Room to Current Visual Sprite", GUILayout.Height(28)))
            {
                Undo.RecordObject(room, "Fit Room to Sprite");
                room.FitToSpriteBounds();
                EditorUtility.SetDirty(room);
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("➕ Instant Connected Hallway Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Click to carve a doorway and attach a seamless connected hallway.", MessageType.Info);

            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.45f);
            if (GUILayout.Button("➡️ Attach Hallway to Right", GUILayout.Height(26)))
            {
                room.AttachRight();
            }
            if (GUILayout.Button("⬅️ Attach Hallway to Left", GUILayout.Height(26)))
            {
                room.AttachLeft();
            }
            if (GUILayout.Button("⬆️ Attach Hallway to Top", GUILayout.Height(26)))
            {
                room.AttachTop();
            }
            if (GUILayout.Button("⬇️ Attach Hallway to Bottom", GUILayout.Height(26)))
            {
                room.AttachBottom();
            }

            GUI.backgroundColor = Color.white;
        }

        private void OnSceneGUI()
        {
            ModularRoom room = (ModularRoom)target;
            if (room == null) return;

            // When Unity's native Rect Tool ('T' key) is active, let it handle dragging without interference
            if (Tools.current == Tool.Rect) return;

            // Otherwise, provide visual bounding handles when in View/Move mode
            Matrix4x4 originalMatrix = Handles.matrix;
            Handles.matrix = room.transform.localToWorldMatrix;

            _boundsHandle.center = Vector3.zero;
            _boundsHandle.size = new Vector3(room.RoomSize.x, room.RoomSize.y, 0f);
            _boundsHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
            _boundsHandle.wireframeColor = new Color(0.3f, 0.9f, 1f, 0.6f);
            _boundsHandle.handleColor = new Color(1f, 0.85f, 0.2f, 0.9f);

            EditorGUI.BeginChangeCheck();
            _boundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(room, "Resize Modular Room");
                Vector2 newSize = new Vector2(
                    Mathf.Max(1f, _boundsHandle.size.x),
                    Mathf.Max(1f, _boundsHandle.size.y)
                );
                room.RoomSize = newSize;
                room.RebuildRoom();
                EditorUtility.SetDirty(room);
            }

            Handles.matrix = originalMatrix;
        }

        [MenuItem("GameObject/DIGIPURITY/Create Hallway (Horizontal)", false, 10)]
        public static void CreateHorizontalHallway(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Hallway_Horizontal");
            go.transform.position = Vector3.zero;
            var mr = go.AddComponent<ModularRoom>();
            mr.RoomSize = new Vector2(24f, 4.5f);
            mr.OpenLeft = true;
            mr.OpenRight = false;
            mr.TileFloor = true;
            mr.RebuildRoom();

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Horizontal Hallway");
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/DIGIPURITY/Create Hallway (Vertical)", false, 11)]
        public static void CreateVerticalHallway(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Hallway_Vertical");
            go.transform.position = Vector3.zero;
            var mr = go.AddComponent<ModularRoom>();
            mr.RoomSize = new Vector2(4.5f, 24f);
            mr.OpenBottom = true;
            mr.OpenTop = false;
            mr.TileFloor = true;
            mr.RebuildRoom();

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Vertical Hallway");
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/DIGIPURITY/Create Modular Room", false, 12)]
        public static void CreateModularRoom(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("ModularRoom");
            go.transform.position = Vector3.zero;
            var mr = go.AddComponent<ModularRoom>();
            mr.RoomSize = new Vector2(18f, 14f);
            mr.OpenRight = true;
            mr.TileFloor = true;
            mr.RebuildRoom();

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Modular Room");
            Selection.activeObject = go;
        }
    }
}
