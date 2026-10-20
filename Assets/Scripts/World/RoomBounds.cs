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

        public Bounds Bounds
        {
            get
            {
                switch (source)
                {
                    case BoundsSource.Manual:
                        return new Bounds(transform.position + manualBounds.center, manualBounds.size);

                    case BoundsSource.SpriteRenderer:
                        if (roomSpriteRenderer != null && roomSpriteRenderer.sprite != null)
                            return roomSpriteRenderer.bounds;
                        var srComponent = GetComponent<SpriteRenderer>();
                        if (srComponent != null && srComponent.sprite != null)
                            return srComponent.bounds;
                        Bounds childSrBounds = GetCompositeRendererBounds();
                        if (childSrBounds.size != Vector3.zero) return childSrBounds;
                        break;

                    case BoundsSource.Collider:
                        if (roomCollider != null) return roomCollider.bounds;
                        Bounds colBounds = GetCompositeColliderBounds();
                        if (colBounds.size != Vector3.zero) return colBounds;
                        break;

                    case BoundsSource.Auto:
                    default:
                        // 1. Floor SpriteRenderer (visual room boundary)
                        var floorSr = roomSpriteRenderer != null ? roomSpriteRenderer : GetComponent<SpriteRenderer>();
                        if (floorSr != null && floorSr.enabled && floorSr.sprite != null)
                        {
                            return floorSr.bounds;
                        }

                        // 2. Trigger collider (designated room zone volume)
                        if (roomCollider != null && roomCollider.enabled && roomCollider.isTrigger)
                        {
                            return roomCollider.bounds;
                        }

                        var allColliders = GetComponents<Collider2D>();
                        for (int i = 0; i < allColliders.Length; i++)
                        {
                            if (allColliders[i].enabled && allColliders[i].isTrigger)
                            {
                                return allColliders[i].bounds;
                            }
                        }

                        // 3. Perimeter walls / multiple colliders: encapsulate all colliders
                        if (allColliders.Length > 1)
                        {
                            Bounds composite = GetCompositeColliderBounds();
                            if (composite.size != Vector3.zero) return composite;
                        }

                        // 4. Single collider
                        if (roomCollider != null && roomCollider.enabled)
                        {
                            return roomCollider.bounds;
                        }
                        if (allColliders.Length == 1 && allColliders[0].enabled)
                        {
                            return allColliders[0].bounds;
                        }

                        // 5. Children renderers or colliders fallback
                        Bounds childSr = GetCompositeRendererBounds();
                        if (childSr.size != Vector3.zero) return childSr;

                        Bounds childCol = GetCompositeColliderBounds();
                        if (childCol.size != Vector3.zero) return childCol;
                        break;
                }

                return new Bounds(transform.position, Vector3.one * 10f);
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
            if (roomCollider == null)
            {
                // Only auto-assign roomCollider if there is a single trigger collider
                if (colliders != null && colliders.Length == 1 && colliders[0].isTrigger)
                {
                    roomCollider = colliders[0];
                }
            }
            else if (colliders != null && colliders.Length > 1 && !roomCollider.isTrigger)
            {
                // If roomCollider was auto-assigned to one of multiple walls, clear it
                // so Auto bounds encapsulates all walls or uses the floor sprite
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

            Bounds b = Bounds;
            return point.x >= b.min.x && point.x <= b.max.x &&
                   point.y >= b.min.y && point.y <= b.max.y;
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
