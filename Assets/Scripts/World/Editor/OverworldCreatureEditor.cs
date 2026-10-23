using UnityEditor;
using UnityEngine;

namespace DigiPurity.World.Editor
{
    [CustomEditor(typeof(OverworldCreature), true)]
    [CanEditMultipleObjects]
    public class OverworldCreatureEditor : UnityEditor.Editor
    {
        private SerializedProperty _creatureNameProp;
        private SerializedProperty _colliderModeProp;
        private SerializedProperty _footprintHeightRatioProp;
        private SerializedProperty _footprintWidthRatioProp;
        private SerializedProperty _footprintOffsetXProp;
        private SerializedProperty _footprintOffsetYProp;

        private SerializedProperty _dynamicDepthSortingProp;
        private SerializedProperty _baseSortingOrderProp;
        private SerializedProperty _sortingMultiplierProp;

        private SerializedProperty _encounterModeProp;
        private SerializedProperty _battleSceneOrIdProp;
        private SerializedProperty _encounterCooldownProp;
        private SerializedProperty _onPlayerTouchProp;
        private SerializedProperty _onPlayerInteractProp;

        private SerializedProperty _gizmoFillColorProp;
        private SerializedProperty _gizmoWireColorProp;
        private SerializedProperty _gizmoSelectedColorProp;
        private SerializedProperty _showLabelProp;
        private SerializedProperty _showInPlayModeProp;

        private void OnEnable()
        {
            _creatureNameProp = serializedObject.FindProperty("creatureName");
            _colliderModeProp = serializedObject.FindProperty("colliderMode");
            _footprintHeightRatioProp = serializedObject.FindProperty("footprintHeightRatio");
            _footprintWidthRatioProp = serializedObject.FindProperty("footprintWidthRatio");
            _footprintOffsetXProp = serializedObject.FindProperty("footprintOffsetX");
            _footprintOffsetYProp = serializedObject.FindProperty("footprintOffsetY");

            _dynamicDepthSortingProp = serializedObject.FindProperty("dynamicDepthSorting");
            _baseSortingOrderProp = serializedObject.FindProperty("baseSortingOrder");
            _sortingMultiplierProp = serializedObject.FindProperty("sortingMultiplier");

            _encounterModeProp = serializedObject.FindProperty("encounterMode");
            _battleSceneOrIdProp = serializedObject.FindProperty("battleSceneOrId");
            _encounterCooldownProp = serializedObject.FindProperty("encounterCooldown");
            _onPlayerTouchProp = serializedObject.FindProperty("onPlayerTouch");
            _onPlayerInteractProp = serializedObject.FindProperty("onPlayerInteract");

            _gizmoFillColorProp = serializedObject.FindProperty("gizmoFillColor");
            _gizmoWireColorProp = serializedObject.FindProperty("gizmoWireColor");
            _gizmoSelectedColorProp = serializedObject.FindProperty("gizmoSelectedColor");
            _showLabelProp = serializedObject.FindProperty("showLabel");
            _showInPlayModeProp = serializedObject.FindProperty("showInPlayMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var creature = (OverworldCreature)target;
            var col = creature.GetComponent<BoxCollider2D>();
            var sr = creature.GetComponentInChildren<SpriteRenderer>();

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Overworld NPC Creature (Solid Obstacle):\n" +
                "• Prevents Owen from phasing through characters.\n" +
                "• Footprint collision enables Owen to walk behind the creature's head/torso without snagging.\n" +
                "• Dynamic depth sorting ensures natural top-down 2D perspective.",
                MessageType.Info);

            EditorGUILayout.Space(4);

            // Quick Actions Header
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Fit Feet Footprint", GUILayout.Height(26)))
            {
                foreach (var obj in targets)
                {
                    if (obj is OverworldCreature c)
                    {
                        Undo.RecordObject(c, "Fit Feet Footprint");
                        var cCol = c.EnsureCollider();
                        Undo.RecordObject(cCol, "Fit Feet Collider");
                        c.Mode = OverworldCreature.ColliderFitMode.Footprint;
                        c.AutoFitCollider(false);
                        EditorUtility.SetDirty(c.gameObject);
                    }
                }
            }

            if (GUILayout.Button("Fit Full Body", GUILayout.Height(26)))
            {
                foreach (var obj in targets)
                {
                    if (obj is OverworldCreature c)
                    {
                        Undo.RecordObject(c, "Fit Full Body");
                        var cCol = c.EnsureCollider();
                        Undo.RecordObject(cCol, "Fit Full Body Collider");
                        c.Mode = OverworldCreature.ColliderFitMode.FullBody;
                        c.AutoFitCollider(true);
                        EditorUtility.SetDirty(c.gameObject);
                    }
                }
            }

            if (GUILayout.Button("Ensure Solid", GUILayout.Height(26)))
            {
                foreach (var obj in targets)
                {
                    if (obj is OverworldCreature c)
                    {
                        var cCol = c.EnsureCollider();
                        if (cCol.isTrigger)
                        {
                            Undo.RecordObject(cCol, "Set Non-Trigger");
                            cCol.isTrigger = false;
                            EditorUtility.SetDirty(cCol);
                        }
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_creatureNameProp);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Collision Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_colliderModeProp);

            if (_colliderModeProp.enumValueIndex == (int)OverworldCreature.ColliderFitMode.Footprint)
            {
                EditorGUILayout.PropertyField(_footprintHeightRatioProp);
                EditorGUILayout.PropertyField(_footprintWidthRatioProp);
                EditorGUILayout.PropertyField(_footprintOffsetXProp);
                EditorGUILayout.PropertyField(_footprintOffsetYProp);
            }
            else if (_colliderModeProp.enumValueIndex == (int)OverworldCreature.ColliderFitMode.FullBody)
            {
                EditorGUILayout.PropertyField(_footprintOffsetXProp, new GUIContent("Offset X"));
                EditorGUILayout.PropertyField(_footprintOffsetYProp, new GUIContent("Offset Y"));
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Depth & Perspective Sorting", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_dynamicDepthSortingProp);
            if (_dynamicDepthSortingProp.boolValue)
            {
                EditorGUILayout.PropertyField(_baseSortingOrderProp);
                EditorGUILayout.PropertyField(_sortingMultiplierProp);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Encounter & Interaction", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_encounterModeProp);
            if (_encounterModeProp.enumValueIndex != (int)OverworldCreature.EncounterMode.SolidObstacleOnly)
            {
                EditorGUILayout.PropertyField(_battleSceneOrIdProp);
                EditorGUILayout.PropertyField(_encounterCooldownProp);
                EditorGUILayout.PropertyField(_onPlayerTouchProp);
                EditorGUILayout.PropertyField(_onPlayerInteractProp);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Scene View Visuals", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_gizmoFillColorProp);
            EditorGUILayout.PropertyField(_gizmoWireColorProp);
            EditorGUILayout.PropertyField(_gizmoSelectedColorProp);
            EditorGUILayout.PropertyField(_showLabelProp);
            EditorGUILayout.PropertyField(_showInPlayModeProp);

            serializedObject.ApplyModifiedProperties();

            // Status & Diagnostics
            if (col != null && col.isTrigger)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox(
                    "Warning: BoxCollider2D has 'Is Trigger' enabled! Creatures must have 'Is Trigger = false' so Owen does not phase through them.",
                    MessageType.Warning);

                if (GUILayout.Button("Fix: Make Solid Non-Trigger"))
                {
                    Undo.RecordObject(col, "Uncheck Trigger");
                    col.isTrigger = false;
                    EditorUtility.SetDirty(col);
                }
            }

            if (sr == null || sr.sprite == null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox("Notice: No SpriteRenderer or Sprite assigned. Attach a sprite to enable automatic footprint calculation.", MessageType.Info);
            }
        }
    }
}
