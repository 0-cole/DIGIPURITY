using UnityEngine;

namespace DigiPurity.World
{
    /// <summary>
    /// Visual and functional component for invisible collision walls (Undertale obj_solid workflow).
    /// Attach this to empty GameObjects to instantly see and edit collision barriers in the Scene view.
    /// In Edit Mode: Displays a clear semi-transparent fill, solid outline, and Toby Fox obj_solid 'X' cross.
    /// In Play Mode: Completely invisible to the player (unless showInPlayMode is enabled for debugging).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("DIGIPURITY/Invisible Wall (Solid Wall)")]
    public class InvisibleWall : MonoBehaviour
    {
        [Header("Appearance & Scene Gizmos")]
        [Tooltip("Semi-transparent fill color in Scene view (default: Toby Fox solid blue).")]
        [SerializeField] private Color fillColor = new Color(0.12f, 0.55f, 1.0f, 0.32f);

        [Tooltip("Solid wireframe outline color in Scene view.")]
        [SerializeField] private Color wireColor = new Color(0.20f, 0.85f, 1.0f, 0.90f);

        [Tooltip("Outline color when this wall is selected in Hierarchy/Scene.")]
        [SerializeField] private Color selectedColor = new Color(1.0f, 0.88f, 0.15f, 0.95f);

        [Tooltip("Draws the classic Toby Fox GameMaker obj_solid diagonal 'X' across the face.")]
        [SerializeField] private bool drawDiagonalCross = true;

        [Tooltip("Shows a subtle 'WALL' label in the Scene view.")]
        [SerializeField] private bool showLabel = true;

#pragma warning disable CS0414
        [Header("Runtime Debug")]
        [Tooltip("If true, keeps gizmo visible during play mode in the Scene view for collision debugging.")]
        [SerializeField] private bool showInPlayMode = false;
#pragma warning restore CS0414

        public Color FillColor { get => fillColor; set => fillColor = value; }
        public Color WireColor { get => wireColor; set => wireColor = value; }
        public bool DrawDiagonalCross { get => drawDiagonalCross; set => drawDiagonalCross = value; }
        public bool ShowLabel { get => showLabel; set => showLabel = value; }

        private void Reset()
        {
            EnsureCollider();
        }

        private void Awake()
        {
            EnsureCollider();
        }

        /// <summary>
        /// Ensures at least one BoxCollider2D is present and configured as a solid (non-trigger) wall.
        /// </summary>
        public BoxCollider2D EnsureCollider()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider2D>();
                col.size = new Vector2(2f, 2f);
                col.offset = Vector2.zero;
            }

            // Solid walls must never be triggers
            if (col.isTrigger)
            {
                col.isTrigger = false;
            }

            return col;
        }

        /// <summary>
        /// Snaps position and collider size to the standard 2x2 tile grid (16 PPU).
        /// </summary>
        [ContextMenu("Snap to 2x2 Unit Grid")]
        public void SnapToGrid()
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Round(pos.x / 2f) * 2f;
            pos.y = Mathf.Round(pos.y / 2f) * 2f;
            transform.position = pos;

            var col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                Vector2 s = col.size;
                s.x = Mathf.Max(0.5f, Mathf.Round(s.x / 2f) * 2f);
                s.y = Mathf.Max(0.5f, Mathf.Round(s.y / 2f) * 2f);
                col.size = s;
                col.offset = Vector2.zero;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Application.isPlaying && !showInPlayMode) return;
            DrawWallGizmo(false);
        }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying && !showInPlayMode) return;
            DrawWallGizmo(true);
        }

        private void DrawWallGizmo(bool isSelected)
        {
            Matrix4x4 prevMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            var colliders = GetComponents<BoxCollider2D>();
            if (colliders != null && colliders.Length > 0)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    var col = colliders[i];
                    if (col == null || !col.enabled) continue;
                    DrawBoxShape(col.offset, col.size, isSelected);
                }
            }
            else
            {
                // Fallback for brand-new empty GameObjects before Awake/Reset completes
                DrawBoxShape(Vector2.zero, new Vector2(2f, 2f), isSelected);
            }

            Gizmos.matrix = prevMatrix;
        }

        private void DrawBoxShape(Vector2 offset, Vector2 size, bool isSelected)
        {
            Vector3 center = (Vector3)offset;
            Vector3 cubeSize = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), 0.05f);

            // 1. Semi-transparent fill
            Color fill = isSelected
                ? new Color(selectedColor.r, selectedColor.g, selectedColor.b, Mathf.Clamp01(fillColor.a * 1.4f))
                : fillColor;
            Gizmos.color = fill;
            Gizmos.DrawCube(center, cubeSize);

            // 2. Solid wireframe boundary
            Color wire = isSelected ? selectedColor : wireColor;
            Gizmos.color = wire;
            Gizmos.DrawWireCube(center, cubeSize);

            // 3. Diagonal cross lines (classic GameMaker / Undertale obj_solid pattern)
            if (drawDiagonalCross)
            {
                float hx = Mathf.Abs(size.x) * 0.5f;
                float hy = Mathf.Abs(size.y) * 0.5f;

                Vector3 pTL = center + new Vector3(-hx, hy, 0f);
                Vector3 pTR = center + new Vector3(hx, hy, 0f);
                Vector3 pBL = center + new Vector3(-hx, -hy, 0f);
                Vector3 pBR = center + new Vector3(hx, -hy, 0f);

                Gizmos.color = new Color(wire.r, wire.g, wire.b, wire.a * 0.75f);
                Gizmos.DrawLine(pTL, pBR);
                Gizmos.DrawLine(pBL, pTR);
            }

            // 4. Scene View Label
            if (showLabel && !Application.isPlaying)
            {
                UnityEditor.Handles.matrix = transform.localToWorldMatrix;
                var style = new GUIStyle(UnityEditor.EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = isSelected ? selectedColor : wireColor }
                };
                UnityEditor.Handles.Label(center, "WALL", style);
                UnityEditor.Handles.matrix = Matrix4x4.identity;
            }
        }
#endif
    }
}
