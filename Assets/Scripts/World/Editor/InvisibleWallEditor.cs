using UnityEditor;
using UnityEngine;

namespace DigiPurity.World.Editor
{
    [CustomEditor(typeof(InvisibleWall))]
    [CanEditMultipleObjects]
    public class InvisibleWallEditor : UnityEditor.Editor
    {
        private SerializedProperty _fillColorProp;
        private SerializedProperty _wireColorProp;
        private SerializedProperty _selectedColorProp;
        private SerializedProperty _drawDiagonalCrossProp;
        private SerializedProperty _showLabelProp;
        private SerializedProperty _showInPlayModeProp;

        private void OnEnable()
        {
            _fillColorProp = serializedObject.FindProperty("fillColor");
            _wireColorProp = serializedObject.FindProperty("wireColor");
            _selectedColorProp = serializedObject.FindProperty("selectedColor");
            _drawDiagonalCrossProp = serializedObject.FindProperty("drawDiagonalCross");
            _showLabelProp = serializedObject.FindProperty("showLabel");
            _showInPlayModeProp = serializedObject.FindProperty("showInPlayMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var wall = (InvisibleWall)target;
            var colliders = wall.GetComponents<BoxCollider2D>();

            // Header Banner
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Invisible Solid Wall (Undertale obj_solid):\n" +
                "• Visible in Scene view during level design.\n" +
                "• Completely invisible to the player in Play mode.\n" +
                $"• Active Colliders: {colliders.Length} BoxCollider2D{(colliders.Length != 1 ? "s" : "")}.",
                MessageType.Info);

            EditorGUILayout.Space(4);

            // Quick Actions
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Snap to 2x2 Grid", GUILayout.Height(26)))
            {
                foreach (var obj in targets)
                {
                    if (obj is InvisibleWall w)
                    {
                        Undo.RecordObject(w.transform, "Snap Wall Transform");
                        var col = w.GetComponent<BoxCollider2D>();
                        if (col != null) Undo.RecordObject(col, "Snap Wall Collider");
                        w.SnapToGrid();
                        EditorUtility.SetDirty(w.gameObject);
                    }
                }
            }

            if (GUILayout.Button("Ensure Solid", GUILayout.Height(26)))
            {
                foreach (var obj in targets)
                {
                    if (obj is InvisibleWall w)
                    {
                        var cols = w.GetComponents<BoxCollider2D>();
                        foreach (var c in cols)
                        {
                            if (c.isTrigger)
                            {
                                Undo.RecordObject(c, "Set Non-Trigger");
                                c.isTrigger = false;
                                EditorUtility.SetDirty(c);
                            }
                        }
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Scene View Visuals", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_fillColorProp);
            EditorGUILayout.PropertyField(_wireColorProp);
            EditorGUILayout.PropertyField(_selectedColorProp);
            EditorGUILayout.PropertyField(_drawDiagonalCrossProp);
            EditorGUILayout.PropertyField(_showLabelProp);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Runtime Debugging", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_showInPlayModeProp);

            serializedObject.ApplyModifiedProperties();

            // Warning if any attached collider is accidentally set to trigger
            bool hasTrigger = false;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && colliders[i].isTrigger)
                {
                    hasTrigger = true;
                    break;
                }
            }

            if (hasTrigger)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox(
                    "Warning: One or more BoxCollider2D components have 'Is Trigger' checked! Solid walls must have 'Is Trigger = false' so Owen bumps into them.",
                    MessageType.Warning);

                if (GUILayout.Button("Fix: Make All Non-Trigger"))
                {
                    foreach (var c in colliders)
                    {
                        if (c != null && c.isTrigger)
                        {
                            Undo.RecordObject(c, "Uncheck Trigger");
                            c.isTrigger = false;
                            EditorUtility.SetDirty(c);
                        }
                    }
                }
            }
        }
    }
}
