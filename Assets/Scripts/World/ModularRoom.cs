using System.Collections.Generic;
using UnityEngine;

namespace DigiPurity.World
{
    [ExecuteAlways]
    [RequireComponent(typeof(RoomBounds))]
    public class ModularRoom : MonoBehaviour
    {
        public enum Direction { Left, Right, Top, Bottom }

        [Header("Room Identity & Dimensions")]
        [SerializeField] private string roomName = "Room";
        [Tooltip("Room dimensions (Width, Height) in local units.")]
        [SerializeField] private Vector2 roomSize = new Vector2(24f, 16f);
        [Tooltip("Thickness of the physical perimeter wall colliders.")]
        [SerializeField] private float wallThickness = 0.8f;

        [Header("Doorways / Openings")]
        [Tooltip("Open a doorway in the left wall.")]
        [SerializeField] private bool openLeft = false;
        [Tooltip("Open a doorway in the right wall.")]
        [SerializeField] private bool openRight = false;
        [Tooltip("Open a doorway in the top wall.")]
        [SerializeField] private bool openTop = false;
        [Tooltip("Open a doorway in the bottom wall.")]
        [SerializeField] private bool openBottom = false;
        [Tooltip("Width or height of the doorway opening for Owen to walk through.")]
        [SerializeField] private float doorwayWidth = 3.0f;

        [Header("Floor Graphics")]
        [SerializeField] private Sprite floorSprite;
        [SerializeField] private int sortingOrder = -10;

        private Vector2 _lastKnownRendererSize;
        private Vector3 _lastKnownScale;

        public Vector2 RoomSize { get => roomSize; set => roomSize = value; }
        public float WallThickness { get => wallThickness; set => wallThickness = value; }
        public bool OpenLeft { get => openLeft; set => openLeft = value; }
        public bool OpenRight { get => openRight; set => openRight = value; }
        public bool OpenTop { get => openTop; set => openTop = value; }
        public bool OpenBottom { get => openBottom; set => openBottom = value; }
        public float DoorwayWidth { get => doorwayWidth; set => doorwayWidth = value; }
        public string RoomName { get => roomName; set => roomName = value; }
        public Sprite FloorSprite { get => floorSprite; set => floorSprite = value; }

        private void Reset()
        {
            if (string.IsNullOrEmpty(roomName)) roomName = gameObject.name;
            // When first added to an existing room (like WalkableSquare), adapt to existing visual size
            FitToSpriteBounds();
        }

        private void Awake()
        {
            if (string.IsNullOrEmpty(roomName)) roomName = gameObject.name;
            LoadDefaultFloorSpriteIfNeeded();
        }

        private void OnEnable()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) _lastKnownRendererSize = sr.size;
            _lastKnownScale = transform.localScale;
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                SyncWithRectToolAndTransform();
            }
        }

        private void OnValidate()
        {
            if (roomSize.x < 1f) roomSize.x = 1f;
            if (roomSize.y < 1f) roomSize.y = 1f;
            if (wallThickness < 0.1f) wallThickness = 0.1f;
            if (doorwayWidth < 0.5f) doorwayWidth = 0.5f;
        }

        public void SyncWithRectToolAndTransform()
        {
            var sr = GetComponent<SpriteRenderer>();
            bool sizeChanged = false;

            // 1. Detect if user dragged Rect Tool on Tiled SpriteRenderer
            if (sr != null && sr.drawMode != SpriteDrawMode.Simple)
            {
                if (Vector2.Distance(sr.size, _lastKnownRendererSize) > 0.05f)
                {
                    roomSize = sr.size;
                    _lastKnownRendererSize = sr.size;
                    sizeChanged = true;
                }
            }

            // 2. Detect if user used Scale tool or Rect tool on Simple sprite
            if (Vector3.Distance(transform.localScale, _lastKnownScale) > 0.01f)
            {
                _lastKnownScale = transform.localScale;
                sizeChanged = true;
            }

            if (sizeChanged)
            {
                RebuildCollidersOnly();
            }
        }

        [ContextMenu("Fit Room Size to Current Sprite")]
        public void FitToSpriteBounds()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                floorSprite = sr.sprite;
                sortingOrder = sr.sortingOrder;

                // If drawing Simple, size is based on sprite bounds in local space
                float sx = Mathf.Abs(transform.localScale.x) > 0.001f ? Mathf.Abs(transform.localScale.x) : 1f;
                float sy = Mathf.Abs(transform.localScale.y) > 0.001f ? Mathf.Abs(transform.localScale.y) : 1f;

                if (sr.drawMode == SpriteDrawMode.Simple)
                {
                    roomSize = new Vector2(sr.bounds.size.x / sx, sr.bounds.size.y / sy);
                }
                else
                {
                    roomSize = sr.size;
                }
            }
            else
            {
                LoadDefaultFloorSpriteIfNeeded();
            }

            RebuildRoom();
        }

        public void LoadDefaultFloorSpriteIfNeeded()
        {
            if (floorSprite != null) return;

#if UNITY_EDITOR
            floorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Floor_Tile.png");
            if (floorSprite == null)
            {
                floorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/WalkableSquare.png");
            }
#endif
        }

        [ContextMenu("Rebuild Room & Walls")]
        public void RebuildRoom()
        {
            // 1. Setup Floor SpriteRenderer
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

            if (floorSprite != null)
            {
                sr.sprite = floorSprite;
                // Only change to Tiled if sprite supports it or is not custom WalkableSquare
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = roomSize;
            }
            sr.sortingOrder = sortingOrder;
            _lastKnownRendererSize = sr.size;
            _lastKnownScale = transform.localScale;

            // 2. Rebuild Colliders
            RebuildCollidersOnly();

            // 3. Ensure RoomBounds is configured
            var rb = GetComponent<RoomBounds>();
            if (rb == null) rb = gameObject.AddComponent<RoomBounds>();
            rb.Source = RoomBounds.BoundsSource.Auto;
            rb.ResolveReferences();
        }

        public void RebuildCollidersOnly()
        {
            var existingColliders = GetComponents<BoxCollider2D>();
            for (int i = existingColliders.Length - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    Destroy(existingColliders[i]);
                }
                else
                {
                    DestroyImmediate(existingColliders[i]);
                }
            }

            float halfW = roomSize.x * 0.5f;
            float halfH = roomSize.y * 0.5f;
            float t = wallThickness;
            float d = Mathf.Clamp(doorwayWidth, 1.0f, Mathf.Min(roomSize.x, roomSize.y) - 0.5f);

            // --- TOP WALL (y = +halfH) ---
            if (!openTop)
            {
                AddWallCollider(new Vector2(0f, halfH), new Vector2(roomSize.x, t));
            }
            else
            {
                float segmentLen = (roomSize.x - d) * 0.5f;
                float segCenterX = (d + segmentLen) * 0.5f;
                AddWallCollider(new Vector2(-segCenterX, halfH), new Vector2(segmentLen, t));
                AddWallCollider(new Vector2(+segCenterX, halfH), new Vector2(segmentLen, t));
            }

            // --- BOTTOM WALL (y = -halfH) ---
            if (!openBottom)
            {
                AddWallCollider(new Vector2(0f, -halfH), new Vector2(roomSize.x, t));
            }
            else
            {
                float segmentLen = (roomSize.x - d) * 0.5f;
                float segCenterX = (d + segmentLen) * 0.5f;
                AddWallCollider(new Vector2(-segCenterX, -halfH), new Vector2(segmentLen, t));
                AddWallCollider(new Vector2(+segCenterX, -halfH), new Vector2(segmentLen, t));
            }

            // --- LEFT WALL (x = -halfW) ---
            if (!openLeft)
            {
                AddWallCollider(new Vector2(-halfW, 0f), new Vector2(t, roomSize.y));
            }
            else
            {
                float segmentLen = (roomSize.y - d) * 0.5f;
                float segCenterY = (d + segmentLen) * 0.5f;
                AddWallCollider(new Vector2(-halfW, -segCenterY), new Vector2(t, segmentLen));
                AddWallCollider(new Vector2(-halfW, +segCenterY), new Vector2(t, segmentLen));
            }

            // --- RIGHT WALL (x = +halfW) ---
            if (!openRight)
            {
                AddWallCollider(new Vector2(+halfW, 0f), new Vector2(t, roomSize.y));
            }
            else
            {
                float segmentLen = (roomSize.y - d) * 0.5f;
                float segCenterY = (d + segmentLen) * 0.5f;
                AddWallCollider(new Vector2(+halfW, -segCenterY), new Vector2(t, segmentLen));
                AddWallCollider(new Vector2(+halfW, +segCenterY), new Vector2(t, segmentLen));
            }

            var rb = GetComponent<RoomBounds>();
            if (rb != null) rb.ResolveReferences();
        }

        private void AddWallCollider(Vector2 offset, Vector2 size)
        {
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.offset = offset;
            col.size = size;
        }

        public ModularRoom SpawnConnectedHallway(Direction dir, float hallwayLength = 24f, float hallwayBreadth = 4.5f)
        {
            float halfW = roomSize.x * 0.5f;
            float halfH = roomSize.y * 0.5f;
            float t = wallThickness;

            Vector3 spawnPos = transform.position;
            Vector2 newSize = Vector2.zero;
            bool newOpenL = false, newOpenR = false, newOpenT = false, newOpenB = false;

            float sx = Mathf.Abs(transform.localScale.x) > 0.001f ? Mathf.Abs(transform.localScale.x) : 1f;
            float sy = Mathf.Abs(transform.localScale.y) > 0.001f ? Mathf.Abs(transform.localScale.y) : 1f;

            switch (dir)
            {
                case Direction.Right:
                    openRight = true;
                    spawnPos += new Vector3((halfW * sx) + (hallwayLength * 0.5f) - (t * 0.5f), 0f, 0f);
                    newSize = new Vector2(hallwayLength, hallwayBreadth);
                    newOpenL = true;
                    newOpenR = false;
                    break;

                case Direction.Left:
                    openLeft = true;
                    spawnPos -= new Vector3((halfW * sx) + (hallwayLength * 0.5f) - (t * 0.5f), 0f, 0f);
                    newSize = new Vector2(hallwayLength, hallwayBreadth);
                    newOpenR = true;
                    newOpenL = false;
                    break;

                case Direction.Top:
                    openTop = true;
                    spawnPos += new Vector3(0f, (halfH * sy) + (hallwayLength * 0.5f) - (t * 0.5f), 0f);
                    newSize = new Vector2(hallwayBreadth, hallwayLength);
                    newOpenB = true;
                    newOpenT = false;
                    break;

                case Direction.Bottom:
                    openBottom = true;
                    spawnPos -= new Vector3(0f, (halfH * sy) + (hallwayLength * 0.5f) - (t * 0.5f), 0f);
                    newSize = new Vector2(hallwayBreadth, hallwayLength);
                    newOpenT = true;
                    newOpenB = false;
                    break;
            }

            RebuildRoom();

            GameObject hallwayObj = new GameObject(roomName + "_Hallway_" + dir.ToString());
            hallwayObj.transform.position = spawnPos;
            hallwayObj.transform.localScale = Vector3.one;

            var hallwayComp = hallwayObj.AddComponent<ModularRoom>();
            hallwayComp.RoomName = hallwayObj.name;
            hallwayComp.RoomSize = newSize;
            hallwayComp.WallThickness = wallThickness;
            hallwayComp.DoorwayWidth = doorwayWidth;
            hallwayComp.OpenLeft = newOpenL;
            hallwayComp.OpenRight = newOpenR;
            hallwayComp.OpenTop = newOpenT;
            hallwayComp.OpenBottom = newOpenB;
            hallwayComp.LoadDefaultFloorSpriteIfNeeded();
            hallwayComp.RebuildRoom();

#if UNITY_EDITOR
            UnityEditor.Selection.activeGameObject = hallwayObj;
            UnityEditor.Undo.RegisterCreatedObjectUndo(hallwayObj, "Spawn Connected Hallway");
#endif

            return hallwayComp;
        }

#if UNITY_EDITOR
        [ContextMenu("Attach Hallway to Right")]
        public void AttachRight() => SpawnConnectedHallway(Direction.Right);

        [ContextMenu("Attach Hallway to Left")]
        public void AttachLeft() => SpawnConnectedHallway(Direction.Left);

        [ContextMenu("Attach Hallway to Top")]
        public void AttachTop() => SpawnConnectedHallway(Direction.Top);

        [ContextMenu("Attach Hallway to Bottom")]
        public void AttachBottom() => SpawnConnectedHallway(Direction.Bottom);
#endif
    }
}
