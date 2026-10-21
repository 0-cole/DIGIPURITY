using System.Collections.Generic;
using UnityEngine;

namespace DigiPurity.World
{
    [ExecuteAlways]
    public class RoomBounds : MonoBehaviour
    {
        public enum BoundsSource { Auto, Collider, SpriteRenderer, Manual }

        [Header("Room Identification")]
        [SerializeField] private string roomName = "Room";

        [Header("Bounds Source")]
        [Tooltip("Auto prioritizes Colliders (triggers & perimeter walls), then SpriteRenderer floor, or Manual bounds.")]
        [SerializeField] private BoundsSource source = BoundsSource.Auto;
        [SerializeField] private Collider2D roomCollider;
        [SerializeField] private SpriteRenderer roomSpriteRenderer;
        [SerializeField] private Bounds manualBounds = new Bounds(Vector3.zero, new Vector3(10f, 10f, 0f));

        private static readonly List<RoomBounds> _activeRooms = new List<RoomBounds>();
        public static IReadOnlyList<RoomBounds> ActiveRooms => _activeRooms;

        public string RoomName => string.IsNullOrEmpty(roomName) ? gameObject.name : roomName;
        public BoundsSource Source { get => source; set => source = value; }
        public Collider2D RoomCollider { get => roomCollider; set => roomCollider = value; }
        public SpriteRenderer RoomSpriteRenderer { get => roomSpriteRenderer; set => roomSpriteRenderer = value; }

        [Header("Connected Rooms")]
        [Tooltip("If true, automatically merges bounding boxes with any overlapping RoomBounds so the camera treats them as a single connected room.")]
        [SerializeField] private bool mergeConnectedRooms = true;

        public bool MergeConnectedRooms { get => mergeConnectedRooms; set => mergeConnectedRooms = value; }

        public Bounds Bounds
        {
            get
            {
                switch (source)
                {
                    case BoundsSource.Manual:
                        return new Bounds(transform.position + manualBounds.center, manualBounds.size);

                    case BoundsSource.Collider:
                        if (roomCollider != null && roomCollider.enabled) return roomCollider.bounds;
                        Bounds colBounds = GetCompositeColliderBounds();
                        if (colBounds.size != Vector3.zero) return colBounds;
                        break;

                    case BoundsSource.SpriteRenderer:
                        Bounds childSrBounds = GetCompositeRendererBounds();
                        if (childSrBounds.size != Vector3.zero) return childSrBounds;
                        if (roomSpriteRenderer != null && roomSpriteRenderer.sprite != null)
                            return roomSpriteRenderer.bounds;
                        var srComponent = GetComponent<SpriteRenderer>();
                        if (srComponent != null && srComponent.sprite != null)
                            return srComponent.bounds;
                        break;

                    case BoundsSource.Auto:
                    default:
                        // 1. Explicitly assigned collider
                        if (roomCollider != null && roomCollider.enabled)
                        {
                            return roomCollider.bounds;
                        }

                        // 2. Check for trigger collider on this GameObject (explicit room zone volume)
                        var allColliders = GetComponents<Collider2D>();
                        for (int i = 0; i < allColliders.Length; i++)
                        {
                            if (allColliders[i].enabled && allColliders[i].isTrigger)
                            {
                                return allColliders[i].bounds;
                            }
                        }

                        // 3. Perimeter wall colliders / multiple colliders: encapsulate all colliders
                        // (e.g. 4 perimeter walls enclosing a room or hallway)
                        if (allColliders != null && allColliders.Length > 1)
                        {
                            Bounds compositeCol = GetCompositeColliderBounds();
                            if (compositeCol.size != Vector3.zero) return compositeCol;
                        }

                        // 4. Single non-trigger collider on this GameObject
                        if (allColliders != null && allColliders.Length == 1 && allColliders[0].enabled)
                        {
                            return allColliders[0].bounds;
                        }

                        // 5. Child colliders (e.g. trigger or composite child wall colliders)
                        Bounds childColBounds = GetCompositeColliderBounds();
                        if (childColBounds.size != Vector3.zero)
                        {
                            return childColBounds;
                        }

                        // 6. Composite Floor Renderers (Parent + Child floor sprites & multifaceted hallways)
                        Bounds compositeRendererBounds = GetCompositeRendererBounds();
                        if (compositeRendererBounds.size != Vector3.zero)
                        {
                            return compositeRendererBounds;
                        }

                        // 7. Single SpriteRenderer fallback
                        var floorSr = roomSpriteRenderer != null ? roomSpriteRenderer : GetComponent<SpriteRenderer>();
                        if (floorSr != null && floorSr.enabled && floorSr.sprite != null)
                        {
                            return floorSr.bounds;
                        }
                        break;
                }

                return new Bounds(transform.position, Vector3.one * 10f);
            }
        }

        /// <summary>
        /// Returns the compound bounding box of this room and all directly or transitively overlapping RoomBounds.
        /// Enables seamless camera follow and movement across multifaceted rooms and connected hallways.
        /// </summary>
        public Bounds GetEffectiveBounds()
        {
            Bounds b = Bounds;
            if (!mergeConnectedRooms) return b;

            var visited = new HashSet<RoomBounds> { this };
            var queue = new Queue<RoomBounds>();
            queue.Enqueue(this);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                Bounds currentB = current.Bounds;

                for (int i = 0; i < _activeRooms.Count; i++)
                {
                    var other = _activeRooms[i];
                    if (other == null || !other.isActiveAndEnabled || visited.Contains(other)) continue;

                    if (currentB.Intersects(other.Bounds))
                    {
                        visited.Add(other);
                        b.Encapsulate(other.Bounds);
                        queue.Enqueue(other);
                    }
                }
            }

            return b;
        }
        }

        public Bounds GetCompositeColliderBounds()
        {
            var colliders = GetComponents<Collider2D>();
            if (colliders == null || colliders.Length == 0)
            {
                colliders = GetComponentsInChildren<Collider2D>();
            }

            if (colliders != null && colliders.Length > 0)
            {
                // If any trigger collider exists, prefer it as the room volume
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i].enabled && colliders[i].isTrigger)
                    {
                        return colliders[i].bounds;
                    }
                }

                // Encapsulate all enabled colliders (e.g. 4 perimeter walls)
                bool hasInitial = false;
                Bounds composite = new Bounds();
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (!colliders[i].enabled) continue;
                    if (!hasInitial)
                    {
                        composite = colliders[i].bounds;
                        hasInitial = true;
                    }
                    else
                    {
                        composite.Encapsulate(colliders[i].bounds);
                    }
                }
                if (hasInitial) return composite;
            }

            return default;
        }

        public Bounds GetCompositeRendererBounds()
        {
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            if (renderers != null && renderers.Length > 0)
            {
                bool hasInitial = false;
                Bounds composite = new Bounds();
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (!renderers[i].enabled || renderers[i].sprite == null) continue;
                    if (!hasInitial)
                    {
                        composite = renderers[i].bounds;
                        hasInitial = true;
                    }
                    else
                    {
                        composite.Encapsulate(renderers[i].bounds);
                    }
                }
                if (hasInitial) return composite;
            }

            return default;
        }

        private void OnEnable()
        {
            if (!_activeRooms.Contains(this))
            {
                _activeRooms.Add(this);
            }
            ResolveReferences();
        }

        private void OnDisable()
        {
            _activeRooms.Remove(this);
        }

        private void Awake()
        {
            ResolveReferences();
        }

        public void ResolveReferences()
        {
            if (string.IsNullOrEmpty(roomName)) roomName = gameObject.name;
            if (roomSpriteRenderer == null) roomSpriteRenderer = GetComponent<SpriteRenderer>();

            var colliders = GetComponents<Collider2D>();
            if (roomCollider == null && colliders != null)
            {
                // If there's an explicit trigger collider, bind to it
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i].isTrigger)
                    {
                        roomCollider = colliders[i];
                        break;
                    }
                }
            }
            else if (colliders != null && colliders.Length > 1 && roomCollider != null && !roomCollider.isTrigger)
            {
                // Clear single-wall collider binding so all perimeter walls are encapsulated
                roomCollider = null;
            }
        }

        public bool ContainsPoint(Vector2 point)
        {
            if (roomCollider != null && roomCollider.enabled && roomCollider.isTrigger)
            {
                if (roomCollider.OverlapPoint(point)) return true;
            }

            var colliders = GetComponents<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].enabled && colliders[i].isTrigger)
                {
                    if (colliders[i].OverlapPoint(point)) return true;
                }
            }

            // Room Bounds check with 0.05 margin so edge positions are inside
            Bounds b = Bounds;
            return point.x >= (b.min.x - 0.05f) && point.x <= (b.max.x + 0.05f) &&
                   point.y >= (b.min.y - 0.05f) && point.y <= (b.max.y + 0.05f);
        }

        public static RoomBounds FindRoomForPosition(Vector2 position, RoomBounds currentRoom = null)
        {
            RoomBounds bestRoom = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < _activeRooms.Count; i++)
            {
                RoomBounds r = _activeRooms[i];
                if (r == null || !r.isActiveAndEnabled) continue;

                if (r.ContainsPoint(position))
                {
                    if (r == currentRoom)
                    {
                        return r;
                    }

                    float dist = Vector2.Distance(position, r.Bounds.center);
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestRoom = r;
                    }
                }
            }

            if (bestRoom != null) return bestRoom;

            if (currentRoom != null && currentRoom.isActiveAndEnabled)
            {
                return currentRoom;
            }

            for (int i = 0; i < _activeRooms.Count; i++)
            {
                RoomBounds r = _activeRooms[i];
                if (r == null || !r.isActiveAndEnabled) continue;

                float dist = Vector2.Distance(position, r.Bounds.center);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestRoom = r;
                }
            }

            return bestRoom;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            Bounds b = Bounds;
            Gizmos.DrawWireCube(b.center, b.size);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.1f, 0.9f, 0.4f, 0.9f);
            Bounds b = Bounds;
            Gizmos.DrawWireCube(b.center, b.size);
            Gizmos.color = new Color(0.1f, 0.9f, 0.4f, 0.08f);
            Gizmos.DrawCube(b.center, b.size);
        }
#endif
    }
}
