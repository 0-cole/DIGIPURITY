using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DigiPurity.World.Editor
{
    [CustomEditor(typeof(RoomBounds))]
    [CanEditMultipleObjects]
    public class RoomBoundsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var room = (RoomBounds)target;
            if (room == null) return;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Room Connection & Diagnostics", EditorStyles.boldLabel);

            Bounds b = room.Bounds;
            EditorGUILayout.HelpBox(
                $"Room Bounds: Center ({b.center.x:F1}, {b.center.y:F1}) | Size ({b.size.x:F1} x {b.size.y:F1})\n" +
                "RoomBounds acts as the camera guide and room connector. Owen can walk freely between any overlapping rooms.",
                MessageType.None);

            // Check if this GameObject has attached BoxCollider2D components that might block doorways
            var attachedColliders = room.GetComponents<BoxCollider2D>();
            List<BoxCollider2D> solidColliders = new List<BoxCollider2D>();
            foreach (var col in attachedColliders)
            {
                if (col != null && !col.isTrigger) solidColliders.Add(col);
            }

            if (solidColliders.Count > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    $"Notice: This floor object has {solidColliders.Count} solid BoxCollider2D components attached directly to it.\n" +
                    "If these colliders surround the floor, they will block doorways into connected rooms/hallways.\n" +
                    "For multifaceted rooms and open hallways, use 'InvisibleWall' empties for perimeter walls instead of putting colliders directly on the floor.",
                    MessageType.Warning);

                if (GUILayout.Button("Remove Solid Colliders from Floor Object", GUILayout.Height(26)))
                {
                    Undo.RecordObject(room.gameObject, "Remove Floor Colliders");
                    for (int i = solidColliders.Count - 1; i >= 0; i--)
                    {
                        Undo.DestroyObjectImmediate(solidColliders[i]);
                    }
                    EditorUtility.SetDirty(room.gameObject);
                }
            }

            // Detect overlapping rooms in the scene
            EditorGUILayout.Space(4);
            var allRooms = RoomBounds.ActiveRooms;
            List<string> connectedRooms = new List<string>();
            for (int i = 0; i < allRooms.Count; i++)
            {
                var other = allRooms[i];
                if (other == null || other == room) continue;

                if (b.Intersects(other.Bounds))
                {
                    connectedRooms.Add(other.RoomName);
                }
            }

            if (connectedRooms.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"Connected to {connectedRooms.Count} overlapping room(s):\n• " + string.Join("\n• ", connectedRooms) +
                    "\n(Owen and Camera will transition smoothly between them!)",
                    MessageType.Info);
            }
        }
    }
}
