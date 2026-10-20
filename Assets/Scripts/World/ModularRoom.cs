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
        [Tooltip("Room dimensions (Width, Height) in world units.")]
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
        [Tooltip("When true, tiles the sprite across the room (use for seamless tiles like Floor_Tile). When false, draws as single image (use for WalkableSquare).")]
        [SerializeField] private bool tileFloor = false;
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
        public bool TileFloor { get => tileFloor; set => tileFloor = value; }

        private void Reset()
        {
            if (string.IsNullOrEmpty(roomName)) roomName = gameObject.name;
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
            if (sr == null) return;

            Vector2 targetSize = roomSize;
            bool changed = false;

            if (sr.drawMode == SpriteDrawMode.Simple)
            {
                // Simple mode (e.g. WalkableSquare): Rect Tool scales transform.localScale
                float sx = Mathf.Abs(transform.localScale.x);
                float sy = Mathf.Abs(transform.localScale.y);

                if (sr.sprite != null && (Mathf.Abs(sx - _lastKnownScale.x) > 0.005f || Mathf.Abs(sy - _lastKnownScale.y) > 0.005f))
                {
                    _lastKnownScale = transform.localScale;
                    float baseW = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
                    float baseH = sr.sprite.rect.height / sr.sprite.pixelsPerUnit;
                    targetSize = new Vector2(baseW * sx, baseH * sy);
                    changed = true;
                }
            }
            else
            {
                // Tiled mode (e.g. Floor_Tile): Rect Tool adjusts sr.size directly
                if (Vector2.Distance(sr.size, _lastKnownRendererSize) > 0.05f)
                {
                    _lastKnownRendererSize = sr.size;
                    targetSize = sr.size;
                    changed = true;
                }
            }

            if (changed && Vector2.Distance(targetSize, roomSize) > 0.02f)
            {
                roomSize = targetSize;
                // Smooth in-place collider update (NEVER DestroyImmediate during a drag!)
                UpdateCollidersSmoothly();
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

                // Auto-detect whether to tile: WalkableSquare is a single bordered image, Floor_Tile is seamless
                tileFloor = !sr.sprite.name.Contains("WalkableSquare");

                float sx = Mathf.Abs(transform.localScale.x) > 0.001f ? Mathf.Abs(transform.localScale.x) : 1f;
                float sy = Mathf.Abs(transform.localScale.y) > 0.001f ? Mathf.Abs(transform.localScale.y) : 1f;

                if (!tileFloor)
                {
                    float baseW = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
                    float baseH = sr.sprite.rect.height / sr.sprite.pixelsPerUnit;
                    roomSize = new Vector2(baseW * sx, baseH * sy);
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
            if (floorSprite != null)
            {
                tileFloor = !floorSprite.name.Contains("WalkableSquare");
            }
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
                if (tileFloor)
                {
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.size = roomSize;
                }
                else
                {
                    sr.drawMode = SpriteDrawMode.Simple;
                }
            }
            sr.sortingOrder = sortingOrder;
            _lastKnownRendererSize = sr.size;
            _lastKnownScale = transform.localScale;

            // 2. Rebuild Colliders
            RebuildCollidersStructure();

            // 3. Ensure RoomBounds is configured
            var rb = GetComponent<RoomBounds>();
            if (rb == null) rb = gameObject.AddComponent<RoomBounds>();
            rb.Source = RoomBounds.BoundsSource.Auto;
            rb.ResolveReferences();
        }

        public void RebuildCollidersStructure()
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

            UpdateCollidersSmoothly();

            var rb = GetComponent<RoomBounds>();
            if (rb != null) rb.ResolveReferences();
        }

        public void UpdateCollidersSmoothly()
        {
            float sx = Mathf.Abs(transform.localScale.x) > 0.001f ? Mathf.Abs(transform.localScale.x) : 1f;
            float sy = Mathf.Abs(transform.localScale.y) > 0.001f ? Mathf.Abs(transform.localScale.y) : 1f;

            // Compute local-space dimensions accounting for transform scale
            float localW = roomSize.x / sx;
            float localH = roomSize.y / sy;
            float halfW = localW * 0.5f;
            float halfH = localH * 0.5f;
            float tX = wallThickness / sx;
            float tY = wallThickness / sy;
            float dX = Mathf.Clamp(doorwayWidth / sx, 1.0f, localW - 0.5f);
            float dY = Mathf.Clamp(doorwayWidth / sy, 1.0f, localH - 0.5f);

            int expectedCount = (openTop ? 2 : 1) + (openBottom ? 2 : 1) + (openLeft ? 2 : 1) + (openRight ? 2 : 1);
            var colliders = GetComponents<BoxCollider2D>();

            if (colliders.Length != expectedCount)
            {
                // Rebuild structure if count differs
                RebuildCollidersStructure();
                return;
            }

            int idx = 0;

            // --- TOP WALL (y = +halfH) ---
            if (!openTop)
            {
                SetCollider(colliders[idx++], new Vector2(0f, halfH), new Vector2(localW, tY));
            }
            else
            {
                float segLen = (localW - dX) * 0.5f;
                float segX = (dX + segLen) * 0.5f;
                SetCollider(colliders[idx++], new Vector2(-segX, halfH), new Vector2(segLen, tY));
                SetCollider(colliders[idx++], new Vector2(+segX, halfH), new Vector2(segLen, tY));
            }

            // --- BOTTOM WALL (y = -halfH) ---
            if (!openBottom)
            {
                SetCollider(colliders[idx++], new Vector2(0f, -halfH), new Vector2(localW, tY));
            }
            else
            {
                float segLen = (localW - dX) * 0.5f;
                float segX = (dX + segLen) * 0.5f;
                SetCollider(colliders[idx++], new Vector2(-segX, -halfH), new Vector2(segLen, tY));
                SetCollider(colliders[idx++], new Vector2(+segX, -halfH), new Vector2(segLen, tY));
            }

            // --- LEFT WALL (x = -halfW) ---
            if (!openLeft)
            {
                SetCollider(colliders[idx++], new Vector2(-halfW, 0f), new Vector2(tX, localH));
            }
            else
            {
                float segLen = (localH - dY) * 0.5f;
                float segY = (dY + segLen) * 0.5f;
                SetCollider(colliders[idx++], new Vector2(-halfW, -segY), new Vector2(tX, segLen));
                SetCollider(colliders[idx++], new Vector2(-halfW, +segY), new Vector2(tX, segLen));
            }

            // --- RIGHT WALL (x = +halfW) ---
            if (!openRight)
            {
                SetCollider(colliders[idx++], new Vector2(+halfW, 0f), new Vector2(tX, localH));
            }
            else
            {
                float segLen = (localH - dY) * 0.5f;
                float segY = (dY + segLen) * 0.5f;
                SetCollider(colliders[idx++], new Vector2(+halfW, -segY), new Vector2(tX, segLen));
                SetCollider(colliders[idx++], new Vector2(+halfW, +segY), new Vector2(tX, segLen));
            }
        }

        private void SetCollider(BoxCollider2D col, Vector2 offset, Vector2 size)
        {
            if (col == null) return;
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

            switch (dir)
            {
                case Direction.Right:
                    openRight = true;
                    spawnPos += new Vector3(halfW + (hallwayLength * 0.5f) - (t * 0.5f), 0f, 0f);
                    newSize = new Vector2(hallwayLength, hallwayBreadth);
                    newOpenL = true;
                    newOpenR = false;
                    break;

                case Direction.Left:
                    openLeft = true;
                    spawnPos -= new Vector3(halfW + (hallwayLength * 0.5f) - (t * 0.5f), 0f, 0f);
                    newSize = new Vector2(hallwayLength, hallwayBreadth);
                    newOpenR = true;
                    newOpenL = false;
                    break;

                case Direction.Top:
                    openTop = true;
                    spawnPos += new Vector3(0f, halfH + (hallwayLength * 0.5f) - (t * 0.5f), 0f);
                    newSize = new Vector2(hallwayBreadth, hallwayLength);
                    newOpenB = true;
                    newOpenT = false;
                    break;

                case Direction.Bottom:
                    openBottom = true;
                    spawnPos -= new Vector3(0f, halfH + (hallwayLength * 0.5f) - (t * 0.5f), 0f);
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
            hallwayComp.TileFloor = true; // Hallways use seamless tiled grid
#if UNITY_EDITOR
            hallwayComp.FloorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Floor_Tile.png");
#endif
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
