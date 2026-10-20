using UnityEngine;

namespace DigiPurity.World
{
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class RoomCameraController : MonoBehaviour
    {
        [Header("Target & Following")]
        [SerializeField] private Transform target;
        [Tooltip("Additional manual offset applied from the target's visual center.")]
        [SerializeField] private Vector2 targetOffset = Vector2.zero;
        [SerializeField] private bool smoothFollow = true;
        [SerializeField] private float smoothTime = 0.08f;

        [Header("Fixed Camera Sizing & 4:3 Aspect Ratio")]
        [Tooltip("Fixed camera orthographic size. 6.0 gives a 16-unit wide 4:3 viewport that aligns with a 16x16 retro room.")]
        [SerializeField] private float setOrthographicSize = 6.0f;
        [Tooltip("When true, camera size is strictly locked to setOrthographicSize.")]
        [SerializeField] private bool lockCameraSize = true;
        [Tooltip("When true, enforces classic 4:3 aspect ratio (Undertale / Deltarune) with solid black pillarbox/letterbox bars.")]
        [SerializeField] private bool enforce4By3Aspect = true;

        [Header("Room Clamping")]
        [SerializeField] private RoomBounds currentRoom;
        [SerializeField] private bool autoDetectRooms = true;

        private Camera _camera;
        private Vector3 _currentVelocity;

        public const float TargetAspect4By3 = 4.0f / 3.0f; // ~1.333333f

        public Transform Target { get => target; set => target = value; }
        public RoomBounds CurrentRoom { get => currentRoom; set => currentRoom = value; }
        public bool SmoothFollow { get => smoothFollow; set => smoothFollow = value; }
        public float SmoothTime { get => smoothTime; set => smoothTime = value; }
        public Vector2 TargetOffset { get => targetOffset; set => targetOffset = value; }
        public float SetOrthographicSize { get => setOrthographicSize; set => setOrthographicSize = value; }
        public bool LockCameraSize { get => lockCameraSize; set => lockCameraSize = value; }
        public bool Enforce4By3Aspect { get => enforce4By3Aspect; set => enforce4By3Aspect = value; }

        public float EffectiveAspect => enforce4By3Aspect ? TargetAspect4By3 : (_camera != null && _camera.aspect > 0.001f ? _camera.aspect : TargetAspect4By3);
        public float EffectiveOrthographicSize => lockCameraSize ? setOrthographicSize : (_camera != null ? _camera.orthographicSize : setOrthographicSize);

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null) _camera = Camera.main;
            ApplyCameraSizingAndAspect();
            FindTargetIfNeeded();
            FindInitialRoomIfNeeded();
        }

        private void Start()
        {
            FindTargetIfNeeded();
            FindInitialRoomIfNeeded();
            ApplyCameraSizingAndAspect();
            // Starting position: instantly snaps to Owen's visual center clamped inside room (offset to side, no void)
            SnapToTarget();
        }

        private void OnEnable()
        {
            ApplyCameraSizingAndAspect();
            SnapToTarget();
        }

        private void OnValidate()
        {
            ApplyCameraSizingAndAspect();
        }

        private void Update()
        {
            ApplyCameraSizingAndAspect();
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
                if (_camera == null) return;
            }

            FindTargetIfNeeded();

            if (target == null) return;

            if (autoDetectRooms)
            {
                RoomBounds detected = RoomBounds.FindRoomForPosition(target.position, currentRoom);
                if (detected != null && detected != currentRoom)
                {
                    currentRoom = detected;
                    // Immediate cut/snap upon room transition, exactly like Undertale & Deltarune
                    SnapToTarget();
                    return;
                }
            }

            if (currentRoom == null)
            {
                FindInitialRoomIfNeeded();
                if (currentRoom == null) return;
            }

            Vector2 targetCenter = GetTargetCenter(target, targetOffset);

            Vector3 desiredPos = CalculateCameraPosition(
                targetCenter,
                Vector2.zero,
                currentRoom.Bounds,
                EffectiveOrthographicSize,
                EffectiveAspect
            );

            desiredPos.z = transform.position.z;

            if (Application.isPlaying && smoothFollow && smoothTime > 0.001f)
            {
                transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _currentVelocity, smoothTime);
            }
            else
            {
                transform.position = desiredPos;
                _currentVelocity = Vector3.zero;
            }
        }

        public void SnapToTarget()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (target == null || currentRoom == null || _camera == null) return;

            Vector2 targetCenter = GetTargetCenter(target, targetOffset);

            Vector3 desiredPos = CalculateCameraPosition(
                targetCenter,
                Vector2.zero,
                currentRoom.Bounds,
                EffectiveOrthographicSize,
                EffectiveAspect
            );
            desiredPos.z = transform.position.z;
            transform.position = desiredPos;
            _currentVelocity = Vector3.zero;
        }

        public static Vector2 GetTargetCenter(Transform targetTransform, Vector2 manualOffset)
        {
            if (targetTransform == null) return Vector2.zero;

            // Owen's sprite pivot is at his feet (y=0) and height is ~3.78 units.
            // Using SpriteRenderer.bounds.center points directly at the character's torso/chest center,
            // centering Owen vertically in the camera when moving up, down, left, and right.
            var sr = targetTransform.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                return new Vector2(sr.bounds.center.x + manualOffset.x, sr.bounds.center.y + manualOffset.y);
            }

            // Fallback if no sprite renderer: approximate body center at ~1.85 units above feet
            return new Vector2(targetTransform.position.x + manualOffset.x, targetTransform.position.y + manualOffset.y + 1.85f);
        }

        public void ApplyCameraSizingAndAspect()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera == null) return;

            _camera.orthographic = true;
            if (lockCameraSize)
            {
                _camera.orthographicSize = setOrthographicSize;
            }

            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.black;

            if (enforce4By3Aspect)
            {
                int w = Screen.width;
                int h = Screen.height;
                if (w > 0 && h > 0)
                {
                    _camera.rect = Calculate4By3ViewportRect(w, h);
                }
            }
            else
            {
                _camera.rect = new Rect(0f, 0f, 1f, 1f);
            }
        }

        public static Rect Calculate4By3ViewportRect(int screenWidth, int screenHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0) return new Rect(0f, 0f, 1f, 1f);

            float windowAspect = (float)screenWidth / screenHeight;

            if (Mathf.Abs(windowAspect - TargetAspect4By3) < 0.005f)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            if (windowAspect > TargetAspect4By3)
            {
                // Screen is wider than 4:3 (e.g. 16:9 widescreen) -> Pillarbox (black bars left and right)
                float insetWidth = TargetAspect4By3 / windowAspect;
                float x = (1.0f - insetWidth) * 0.5f;
                return new Rect(x, 0f, insetWidth, 1.0f);
            }
            else
            {
                // Screen is taller than 4:3 (e.g. 5:4 or portrait) -> Letterbox (black bars top and bottom)
                float insetHeight = windowAspect / TargetAspect4By3;
                float y = (1.0f - insetHeight) * 0.5f;
                return new Rect(0f, y, 1.0f, insetHeight);
            }
        }

        private void OnGUI()
        {
            if (!enforce4By3Aspect || _camera == null || Event.current.type != EventType.Repaint) return;

            Rect r = _camera.rect;
            int prevDepth = GUI.depth;
            GUI.depth = -1500;

            // Draw pure black bars over the non-rendered margins for widescreen / ultrawide displays
            if (r.x > 0.001f)
            {
                float leftWidth = r.x * Screen.width;
                GUI.DrawTexture(new Rect(0, 0, leftWidth, Screen.height), Texture2D.blackTexture);
                float rightX = (r.x + r.width) * Screen.width;
                GUI.DrawTexture(new Rect(rightX, 0, Screen.width - rightX, Screen.height), Texture2D.blackTexture);
            }
            if (r.y > 0.001f)
            {
                float barHeight = r.y * Screen.height;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, barHeight), Texture2D.blackTexture);
                float bottomY = (r.y + r.height) * Screen.height;
                GUI.DrawTexture(new Rect(0, bottomY, Screen.width, Screen.height - bottomY), Texture2D.blackTexture);
            }

            GUI.depth = prevDepth;
        }

        private void FindTargetIfNeeded()
        {
            if (target != null) return;

            var playerController = FindAnyObjectByType<OverworldPlayerController>();
            if (playerController != null)
            {
                target = playerController.transform;
                return;
            }

            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
                return;
            }

            var owen = GameObject.Find("Owen");
            if (owen != null)
            {
                target = owen.transform;
            }
        }

        private void FindInitialRoomIfNeeded()
        {
            if (currentRoom != null) return;

            if (target != null)
            {
                currentRoom = RoomBounds.FindRoomForPosition(target.position);
                if (currentRoom != null) return;
            }

            var sq = GameObject.Find("WalkableSquare");
            if (sq == null) sq = GameObject.Find("Square");

            if (sq != null)
            {
                var rb = sq.GetComponent<RoomBounds>();
                if (rb == null)
                {
                    rb = sq.AddComponent<RoomBounds>();
                }
                currentRoom = rb;
            }
        }

        public static Vector3 CalculateCameraPosition(
            Vector2 targetPos,
            Vector2 targetOffset,
            Bounds roomBounds,
            float orthographicSize,
            float aspect)
        {
            float halfHeight = orthographicSize;
            float actualAspect = aspect > 0.001f ? aspect : TargetAspect4By3;
            float halfWidth = halfHeight * actualAspect;
            float viewWidth = halfWidth * 2f;
            float viewHeight = halfHeight * 2f;

            float roomWidth = roomBounds.size.x;
            float roomHeight = roomBounds.size.y;

            float camX;
            if (roomWidth <= viewWidth)
            {
                // Undertale / Deltarune Rule: If room is small horizontally, stay still at the room center.
                // Camera remains set size, showing the room in full with black borders on the sides.
                camX = roomBounds.center.x;
            }
            else
            {
                // Large room / Hallway: follow Owen, clamped strictly to room bounds so void is never shown.
                // At starting door, Owen is on the side and camera edge aligns with room boundary.
                float minX = roomBounds.min.x + halfWidth;
                float maxX = roomBounds.max.x - halfWidth;
                if (minX >= maxX)
                {
                    camX = roomBounds.center.x;
                }
                else
                {
                    camX = Mathf.Clamp(targetPos.x + targetOffset.x, minX, maxX);
                }
            }

            float camY;
            if (roomHeight <= viewHeight)
            {
                // Undertale / Deltarune Rule: If room is small vertically, stay still at the room center.
                // Camera remains set size, showing the room in full with black borders above/below.
                camY = roomBounds.center.y;
            }
            else
            {
                // Large room / Tall corridor: follow Owen, clamped strictly to room bounds so void is never shown.
                float minY = roomBounds.min.y + halfHeight;
                float maxY = roomBounds.max.y - halfHeight;
                if (minY >= maxY)
                {
                    camY = roomBounds.center.y;
                }
                else
                {
                    camY = Mathf.Clamp(targetPos.y + targetOffset.y, minY, maxY);
                }
            }

            return new Vector3(camX, camY, -10f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera == null) return;

            float orthoSize = lockCameraSize ? setOrthographicSize : _camera.orthographicSize;
            float aspect = enforce4By3Aspect ? TargetAspect4By3 : (_camera.aspect > 0.001f ? _camera.aspect : TargetAspect4By3);
            float h = orthoSize * 2f;
            float w = h * aspect;

            // Gold wireframe represents the 4:3 camera viewport in Scene view
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(new Vector3(transform.position.x, transform.position.y, 0f), new Vector3(w, h, 0f));
        }
#endif
    }
}
