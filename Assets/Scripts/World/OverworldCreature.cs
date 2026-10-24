using UnityEngine;
using UnityEngine.Events;

namespace DigiPurity.World
{
    /// <summary>
    /// Overworld NPC Creature component for characters, monsters, and interactive entities.
    /// Ensures creatures act as solid physical obstacles that Owen cannot phase through.
    /// Provides automatic top-down footprint sizing (so Owen can walk behind heads/shoulders),
    /// dynamic Y-depth sorting, and optional collision/interaction events (e.g. initiating combat).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    [AddComponentMenu("DIGIPURITY/Overworld Creature (Solid NPC)")]
    public class OverworldCreature : MonoBehaviour
    {
        public enum ColliderFitMode
        {
            [Tooltip("Top-down RPG standard: collider only covers the base/feet so Owen can walk behind the creature's head/torso.")]
            Footprint,

            [Tooltip("Collider covers the entire sprite.")]
            FullBody,

            [Tooltip("Custom collider size and offset; will not be overwritten by auto-fit.")]
            Custom
        }

        public enum EncounterMode
        {
            [Tooltip("Acts strictly as a solid physical obstacle.")]
            SolidObstacleOnly,

            [Tooltip("Touching Owen triggers a combat encounter or event.")]
            TriggerCombatOnTouch,

            [Tooltip("Owen can interact by facing the creature and pressing Confirm (Z/Enter/Space).")]
            TriggerCombatOnInteract,

            [Tooltip("Custom event trigger on touch without starting standard combat.")]
            CustomEventOnly
        }

        [Header("Identity")]
        [SerializeField] private string creatureName = "NPC Creature";

        [Header("Solid Collision Settings")]
        [Tooltip("How the solid BoxCollider2D should fit the sprite.")]
        [SerializeField] private ColliderFitMode colliderMode = ColliderFitMode.Footprint;

        [Range(0.1f, 1.0f)]
        [Tooltip("Height ratio of the collider relative to the sprite (default: bottom 35% for top-down perspective).")]
        [SerializeField] private float footprintHeightRatio = 0.35f;

        [Range(0.1f, 1.0f)]
        [Tooltip("Width ratio of the collider relative to the sprite (default: 80% to allow smooth corner sliding).")]
        [SerializeField] private float footprintWidthRatio = 0.80f;

        [Tooltip("Additional horizontal offset adjustment in local units.")]
        [SerializeField] private float footprintOffsetX = 0.0f;

        [Tooltip("Additional vertical offset adjustment in local units.")]
        [SerializeField] private float footprintOffsetY = 0.0f;

        [Header("Depth & Perspective Sorting")]
        [Tooltip("Dynamically adjusts SpriteRenderer sortingOrder by Y position so Owen properly renders in front/behind.")]
        [SerializeField] private bool dynamicDepthSorting = true;

        [Tooltip("Base sorting order for characters (above floor tiles at -10).")]
        [SerializeField] private int baseSortingOrder = 1000;

        [Tooltip("Sorting order precision per world unit.")]
        [SerializeField] private float sortingMultiplier = 32f;

        [Header("Encounter & Interaction")]
        [SerializeField] private EncounterMode encounterMode = EncounterMode.SolidObstacleOnly;
        [SerializeField] private string battleSceneOrId = "Zone0.5_Battle";
        [SerializeField] private float encounterCooldown = 1.0f;
        [SerializeField] private UnityEvent onPlayerTouch;
        [SerializeField] private UnityEvent onPlayerInteract;

        [Header("Scene View Visuals")]
        [SerializeField] private Color gizmoFillColor = new Color(0.2f, 0.9f, 0.4f, 0.35f);
        [SerializeField] private Color gizmoWireColor = new Color(0.2f, 1.0f, 0.5f, 0.95f);
        [SerializeField] private Color gizmoSelectedColor = new Color(1.0f, 0.88f, 0.2f, 1.0f);
#pragma warning disable CS0414
        [SerializeField] private bool showLabel = true;
        [SerializeField] private bool showInPlayMode = false;
#pragma warning restore CS0414

        private BoxCollider2D _boxCollider;
        private SpriteRenderer _spriteRenderer;
        private float _lastEncounterTime = -999f;

        public string CreatureName { get => creatureName; set => creatureName = value; }
        public ColliderFitMode Mode { get => colliderMode; set => colliderMode = value; }
        public EncounterMode CurrentEncounterMode { get => encounterMode; set => encounterMode = value; }
        public float FootprintHeightRatio { get => footprintHeightRatio; set => footprintHeightRatio = value; }
        public float FootprintWidthRatio { get => footprintWidthRatio; set => footprintWidthRatio = value; }
        public float FootprintOffsetX { get => footprintOffsetX; set => footprintOffsetX = value; }
        public float FootprintOffsetY { get => footprintOffsetY; set => footprintOffsetY = value; }
        public bool DynamicDepthSorting { get => dynamicDepthSorting; set => dynamicDepthSorting = value; }
        public int BaseSortingOrder { get => baseSortingOrder; set => baseSortingOrder = value; }

        public BoxCollider2D BoxCollider => _boxCollider != null ? _boxCollider : (_boxCollider = GetComponent<BoxCollider2D>());
        public SpriteRenderer SpriteRenderer => _spriteRenderer != null ? _spriteRenderer : (_spriteRenderer = GetComponentInChildren<SpriteRenderer>());

        private void Reset()
        {
            if (string.IsNullOrEmpty(creatureName) || creatureName == "NPC Creature")
            {
                creatureName = gameObject.name;
            }
            EnsureCollider();
            AutoFitCollider(false);
            ConfigureSpriteRenderer();
        }

        private void Awake()
        {
            _boxCollider = GetComponent<BoxCollider2D>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            EnsureCollider();
            ConfigureSpriteRenderer();
        }

        private void OnValidate()
        {
            if (_boxCollider == null) _boxCollider = GetComponent<BoxCollider2D>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            EnsureCollider();

            if (colliderMode != ColliderFitMode.Custom)
            {
                AutoFitCollider(false);
            }

            ConfigureSpriteRenderer();
        }

        private void LateUpdate()
        {
            if (dynamicDepthSorting)
            {
                UpdateDepthSorting();
            }
        }

        /// <summary>
        /// Ensures the BoxCollider2D is present and configured as a solid non-trigger obstacle.
        /// </summary>
        public BoxCollider2D EnsureCollider()
        {
            if (_boxCollider == null)
            {
                _boxCollider = GetComponent<BoxCollider2D>();
                if (_boxCollider == null)
                {
                    _boxCollider = gameObject.AddComponent<BoxCollider2D>();
                }
            }

            // NPC creatures MUST be solid obstacles to prevent phasing
            if (_boxCollider.isTrigger)
            {
                _boxCollider.isTrigger = false;
            }

            return _boxCollider;
        }

        private void ConfigureSpriteRenderer()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;
            }
        }

        /// <summary>
        /// Computes and applies collider size and offset to match the sprite footprint or full body.
        /// </summary>
        public void AutoFitCollider(bool forceFullBody = false)
        {
            EnsureCollider();

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_spriteRenderer == null || _spriteRenderer.sprite == null)
            {
                // Fallback default footprint
                _boxCollider.size = new Vector2(1.5f, 1.0f);
                _boxCollider.offset = new Vector2(0f, -0.5f);
                return;
            }

            Bounds spriteBounds = _spriteRenderer.sprite.bounds;

            if (forceFullBody || colliderMode == ColliderFitMode.FullBody)
            {
                _boxCollider.size = spriteBounds.size;
                _boxCollider.offset = (Vector2)spriteBounds.center + new Vector2(footprintOffsetX, footprintOffsetY);
            }
            else if (colliderMode == ColliderFitMode.Footprint)
            {
                float w = spriteBounds.size.x * Mathf.Clamp(footprintWidthRatio, 0.1f, 1f);
                float h = spriteBounds.size.y * Mathf.Clamp(footprintHeightRatio, 0.1f, 1f);

                // Align to the bottom edge of the sprite bounds (the feet) with custom offsets
                float centerY = spriteBounds.min.y + (h * 0.5f) + footprintOffsetY;
                float centerX = spriteBounds.center.x + footprintOffsetX;

                _boxCollider.size = new Vector2(w, h);
                _boxCollider.offset = new Vector2(centerX, centerY);
            }
        }

        /// <summary>
        /// Updates the SpriteRenderer sorting order based on the world Y of the creature's feet.
        /// </summary>
        public void UpdateDepthSorting()
        {
            if (_spriteRenderer == null) return;

            float footWorldY = GetFeetWorldY();
            _spriteRenderer.sortingOrder = baseSortingOrder - Mathf.RoundToInt(footWorldY * sortingMultiplier);
        }

        /// <summary>
        /// Gets the world Y coordinate where the creature's feet touch the ground.
        /// </summary>
        public float GetFeetWorldY()
        {
            if (_boxCollider != null)
            {
                Vector3 localFeet = new Vector3(_boxCollider.offset.x, _boxCollider.offset.y - (_boxCollider.size.y * 0.5f), 0f);
                return transform.TransformPoint(localFeet).y;
            }
            return transform.position.y;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandlePlayerContact(collision.gameObject);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (encounterMode == EncounterMode.TriggerCombatOnInteract)
            {
                CheckPlayerInteractInput(collision.gameObject);
            }
        }

        private void HandlePlayerContact(GameObject other)
        {
            if (!IsPlayer(other)) return;

            onPlayerTouch?.Invoke();

            if (encounterMode == EncounterMode.TriggerCombatOnTouch)
            {
                TriggerCombatEncounter();
            }
        }

        private void CheckPlayerInteractInput(GameObject other)
        {
            if (!IsPlayer(other)) return;

            bool interactPressed = false;
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame))
            {
                interactPressed = true;
            }
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame))
            {
                interactPressed = true;
            }
#endif
            if (interactPressed)
            {
                onPlayerInteract?.Invoke();
                if (encounterMode == EncounterMode.TriggerCombatOnInteract)
                {
                    TriggerCombatEncounter();
                }
            }
        }

        private bool IsPlayer(GameObject go)
        {
            return go.CompareTag("Player") || go.GetComponent<OverworldPlayerController>() != null;
        }

        public void TriggerCombatEncounter()
        {
            if (Time.time - _lastEncounterTime < encounterCooldown) return;
            _lastEncounterTime = Time.time;

            Debug.Log($"[DIGIPURITY] Overworld encounter initiated with {creatureName} ({battleSceneOrId})!");

            var overworldEnemy = GetComponent<Combat.OverworldEnemy>();
            if (overworldEnemy == null)
            {
                overworldEnemy = gameObject.AddComponent<Combat.OverworldEnemy>();
            }
            if (overworldEnemy != null)
            {
                var player = FindAnyObjectByType<OverworldPlayerController>();
                if (player != null)
                {
                    overworldEnemy.TriggerEncounter(player);
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Application.isPlaying && !showInPlayMode) return;
            DrawGizmo(false);
        }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying && !showInPlayMode) return;
            DrawGizmo(true);
        }

        private void DrawGizmo(bool isSelected)
        {
            var col = GetComponent<BoxCollider2D>();
            if (col == null) return;

            Matrix4x4 prevMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Vector3 center = (Vector3)col.offset;
            Vector3 size = new Vector3(Mathf.Abs(col.size.x), Mathf.Abs(col.size.y), 0.05f);

            // Fill
            Gizmos.color = isSelected ? new Color(gizmoSelectedColor.r, gizmoSelectedColor.g, gizmoSelectedColor.b, 0.45f) : gizmoFillColor;
            Gizmos.DrawCube(center, size);

            // Outline
            Gizmos.color = isSelected ? gizmoSelectedColor : gizmoWireColor;
            Gizmos.DrawWireCube(center, size);

            Gizmos.matrix = prevMatrix;

            // Scene view label
            if (showLabel && !Application.isPlaying)
            {
                Vector3 worldCenter = transform.TransformPoint(col.offset);
                var style = new GUIStyle(UnityEditor.EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = isSelected ? gizmoSelectedColor : gizmoWireColor }
                };
                string modeText = colliderMode == ColliderFitMode.Footprint ? "Footprint" : "Solid";
                UnityEditor.Handles.Label(worldCenter, $"[NPC] {creatureName} ({modeText})", style);
            }
        }
#endif
    }
}
