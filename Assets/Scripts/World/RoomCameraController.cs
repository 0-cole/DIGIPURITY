using UnityEngine;

namespace DigiPurity.World
{
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class RoomCameraController : MonoBehaviour
    {
        [Header("Target & Following")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 targetOffset = Vector2.zero;
        [SerializeField] private bool smoothFollow = true;
        [SerializeField] private float smoothTime = 0.12f;

        [Header("Room Clamping")]
        [SerializeField] private RoomBounds currentRoom;
        [SerializeField] private bool autoDetectRooms = true;

        private Camera _camera;
        private Vector3 _currentVelocity;

        public Transform Target { get => target; set => target = value; }
        public RoomBounds CurrentRoom { get => currentRoom; set => currentRoom = value; }
        public bool SmoothFollow { get => smoothFollow; set => smoothFollow = value; }
        public float SmoothTime { get => smoothTime; set => smoothTime = value; }
        public Vector2 TargetOffset { get => targetOffset; set => targetOffset = value; }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null) _camera = Camera.main;
            FindTargetIfNeeded();
            FindInitialRoomIfNeeded();
        }

        private void Start()
        {
            FindTargetIfNeeded();
            FindInitialRoomIfNeeded();

            if (target != null && currentRoom != null && _camera != null)
            {
                float aspect = _camera.aspect > 0.001f ? _camera.aspect : (16f / 9f);
                Vector3 startPos = CalculateCameraPosition(
                    target.position,
                    targetOffset,
                    currentRoom.Bounds,
                    _camera.orthographicSize,
                    aspect
                );
                transform.position = new Vector3(startPos.x, startPos.y, transform.position.z);
            }
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
                }
            }

            if (currentRoom == null)
            {
                FindInitialRoomIfNeeded();
                if (currentRoom == null) return;
            }

            float aspect = _camera.aspect > 0.001f ? _camera.aspect : (16f / 9f);
            Vector3 desiredPos = CalculateCameraPosition(
                target.position,
                targetOffset,
                currentRoom.Bounds,
                _camera.orthographicSize,
                aspect
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

            float aspect = _camera.aspect > 0.001f ? _camera.aspect : (16f / 9f);
            Vector3 desiredPos = CalculateCameraPosition(
                target.position,
                targetOffset,
                currentRoom.Bounds,
                _camera.orthographicSize,
                aspect
            );
            desiredPos.z = transform.position.z;
            transform.position = desiredPos;
            _currentVelocity = Vector3.zero;
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
            float actualAspect = aspect > 0.001f ? aspect : (16f / 9f);
            float halfWidth = halfHeight * actualAspect;
            float viewWidth = halfWidth * 2f;
            float viewHeight = halfHeight * 2f;

            float roomWidth = roomBounds.size.x;
            float roomHeight = roomBounds.size.y;

            float camX;
            if (roomWidth <= viewWidth)
            {
                // Undertale / Deltarune Rule: If room is small horizontally, stay still at the room center
                camX = roomBounds.center.x;
            }
            else
            {
                // Hallway / large room: follow target, clamped so viewport never sees past room edges
                float minX = roomBounds.min.x + halfWidth;
                float maxX = roomBounds.max.x - halfWidth;
                if (minX > maxX)
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
                // Undertale / Deltarune Rule: If room is small vertically, stay still at the room center
                camY = roomBounds.center.y;
            }
            else
            {
                // Vertical corridor / tall room: follow target, clamped so viewport never sees past room edges
                float minY = roomBounds.min.y + halfHeight;
                float maxY = roomBounds.max.y - halfHeight;
                if (minY > maxY)
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
    }
}
