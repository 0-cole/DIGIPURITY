using UnityEngine;
using UnityEngine.InputSystem;

namespace DigiPurity.World
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class OverworldPlayerController : MonoBehaviour
    {
        [Header("Movement Settings (Deltarune Overworld Style)")]
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float runMultiplier = 1.6f;
        [SerializeField] private float diagonalSpeedMultiplier = 1.5f;
        [SerializeField] private bool clampToWalkableSquare = false;

        [Header("Sprites & Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite idleDownSprite;
        [SerializeField] private Sprite idleUpSprite;
        [SerializeField] private Sprite idleLeftSprite;
        [SerializeField] private Sprite idleSideSprite; // Right-facing idle (or shared side idle)

        [SerializeField] private Sprite[] walkDownFrames;
        [SerializeField] private Sprite[] walkUpFrames;
        [SerializeField] private Sprite[] walkLeftFrames;
        [SerializeField] private Sprite[] walkRightFrames;
        [SerializeField] private Sprite[] walkSideFrames; // Fallback if left/right aren't set separately
        [SerializeField] private float frameDuration = 0.16f; // ~160ms per walk step

        public enum FacingDirection { Down, Up, Left, Right }
        [SerializeField] private FacingDirection currentFacing = FacingDirection.Down;

        private Rigidbody2D _rb;
        private Vector2 _moveInput;
        private float _animTimer;
        private int _currentFrame;
        private bool _isMoving;
        private bool _wasMoving;
        private bool _isRunning;
        private bool _isMovingDiagonally;
        private FacingDirection _previousFacing;

        public bool IsMoving => _isMoving;
        public bool IsRunning => _isRunning;
        public bool IsMovingDiagonally => _isMovingDiagonally;
        public float WalkSpeed => walkSpeed;
        public float RunMultiplier => runMultiplier;
        public float DiagonalSpeedMultiplier
        {
            get => diagonalSpeedMultiplier;
            set => diagonalSpeedMultiplier = value;
        }
        public Vector2 MovementInput => _moveInput;
        public FacingDirection CurrentFacing => currentFacing;

        [Header("State / Controls")]
        [SerializeField] private bool inputLocked = false;
        public bool InputLocked
        {
            get => inputLocked;
            set
            {
                inputLocked = value;
                if (value)
                {
                    _moveInput = Vector2.zero;
                    _isMoving = false;
                    _isMovingDiagonally = false;
                    _isRunning = false;
                }
            }
        }

        public void SetFacingDirection(FacingDirection direction)
        {
            currentFacing = direction;
            _previousFacing = direction;
            _currentFrame = 0;
            _animTimer = 0f;
            UpdateAnimation();
        }

        private Collider2D _ownCollider;
        private ContactFilter2D _contactFilter;
        private readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[8];

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Kinematic;
                _rb.simulated = true;
                _rb.gravityScale = 0f;
                _rb.freezeRotation = true;
            }

            _ownCollider = GetComponent<Collider2D>();
            _contactFilter = new ContactFilter2D();
            _contactFilter.useTriggers = false;
            _contactFilter.SetLayerMask(Physics2D.AllLayers);

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer != null && spriteRenderer.sortingOrder <= 0)
            {
                spriteRenderer.sortingOrder = 10;
            }

            _previousFacing = currentFacing;

#if UNITY_EDITOR
            AutoBindSpritesIfNeeded();
#endif

            if (idleDownSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = idleDownSprite;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            AutoBindSpritesIfNeeded();
        }

        public void AutoBindSpritesIfNeeded()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (idleDownSprite == null)
                idleDownSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Owen/Owen_Idle_Down_0.png");
            if (idleUpSprite == null)
                idleUpSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Owen/Owen_Idle_Up_0.png");
            if (idleLeftSprite == null)
                idleLeftSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Owen/Owen_Idle_Left_0.png");
            if (idleSideSprite == null)
                idleSideSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Owen/Owen_Idle_Side_0.png");

            if (walkDownFrames == null || walkDownFrames.Length == 0 || walkDownFrames[0] == null)
                walkDownFrames = LoadFrameArray("Owen_Walk_Down", 4);
            if (walkUpFrames == null || walkUpFrames.Length == 0 || walkUpFrames[0] == null)
                walkUpFrames = LoadFrameArray("Owen_Walk_Up", 4);
            if (walkLeftFrames == null || walkLeftFrames.Length == 0 || walkLeftFrames[0] == null)
                walkLeftFrames = LoadFrameArray("Owen_Walk_Left", 4);
            if (walkRightFrames == null || walkRightFrames.Length == 0 || walkRightFrames[0] == null)
                walkRightFrames = LoadFrameArray("Owen_Walk_Right", 4);

            if (spriteRenderer != null && spriteRenderer.sprite == null && idleDownSprite != null)
            {
                spriteRenderer.sprite = idleDownSprite;
            }
        }

        private static Sprite[] LoadFrameArray(string prefix, int count)
        {
            Sprite[] arr = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                arr[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Owen/{prefix}_{i}.png");
            }
            return arr;
        }
#endif

        private void Update()
        {
            if (inputLocked)
            {
                _moveInput = Vector2.zero;
                _isMoving = false;
                _isMovingDiagonally = false;
                _isRunning = false;
                UpdateAnimation();
                return;
            }

            float x = 0f;
            float y = 0f;
            bool running = false;

            // 1. Read Unity New Input System (WASD + Arrow Keys + Shift/X Run)
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;

                running = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed || kb.xKey.isPressed;
            }

            // 2. Also check Legacy Input Manager if available
            try
            {
                if (Mathf.Approximately(x, 0f) && Mathf.Approximately(y, 0f))
                {
                    if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) x += 1f;
                    if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) x -= 1f;
                    if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) y += 1f;
                    if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) y -= 1f;
                }
                if (!running)
                {
                    running = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.X);
                }
            }
            catch
            {
                // Ignore if legacy Input Manager is inactive in current Editor session
            }

            Vector2 rawInput = new Vector2(x, y);
            bool rawMoving = rawInput.sqrMagnitude > 0.01f;
            _isRunning = running;
            _isMovingDiagonally = Mathf.Abs(x) > 0.01f && Mathf.Abs(y) > 0.01f;

            if (rawMoving)
            {
                _moveInput = rawInput.normalized;
                UpdateFacingDirection(x, y);
                MoveCharacter(Time.deltaTime);
            }
            else
            {
                _moveInput = Vector2.zero;
                _isMoving = false;
                _isMovingDiagonally = false;
            }

            UpdateAnimation();

            _wasMoving = _isMoving;
            _previousFacing = currentFacing;
        }

        public static Vector2 CalculateVelocity(Vector2 input, float baseWalkSpeed, bool isRunning, float runMultiplier, float diagonalMultiplier)
        {
            if (input.sqrMagnitude < 0.0001f) return Vector2.zero;

            Vector2 dir = input.normalized;
            float speed = baseWalkSpeed * (isRunning ? runMultiplier : 1f);
            bool isDiagonal = Mathf.Abs(input.x) > 0.01f && Mathf.Abs(input.y) > 0.01f;
            if (isDiagonal)
            {
                speed *= diagonalMultiplier;
            }

            return dir * speed;
        }

        private void MoveCharacter(float dt)
        {
            Vector2 velocity = CalculateVelocity(_moveInput, walkSpeed, _isRunning, runMultiplier, diagonalSpeedMultiplier);
            float targetDx = velocity.x * dt;
            float targetDy = velocity.y * dt;

            float actualDx = 0f;
            float actualDy = 0f;

            if (Mathf.Abs(targetDx) > 0.0001f)
            {
                actualDx = AttemptMoveAxis(targetDx, 0f);
            }

            if (Mathf.Abs(targetDy) > 0.0001f)
            {
                actualDy = AttemptMoveAxis(0f, targetDy);
            }

            Vector3 newPos = transform.position + new Vector3(actualDx, actualDy, 0f);

            // Optional room clamp bounds check if clampToWalkableSquare is enabled
            if (clampToWalkableSquare)
            {
                var activeRoom = RoomBounds.FindRoomForPosition(newPos);
                if (activeRoom == null) activeRoom = RoomBounds.FindRoomForPosition(transform.position);

                if (activeRoom != null)
                {
                    Bounds b = activeRoom.GetEffectiveBounds();
                    float halfWidth = 1.07f;
                    float fullHeight = 3.89f;
                    float footHeight = 0.18f;

                    float minX = b.min.x + halfWidth;
                    float maxX = b.max.x - halfWidth;
                    float minY = b.min.y + footHeight;
                    float maxY = b.max.y - fullHeight;

                    if (minX < maxX)
                    {
                        float clampedX = Mathf.Clamp(newPos.x, minX, maxX);
                        if (Mathf.Abs(clampedX - newPos.x) > 0.0001f)
                        {
                            actualDx = 0f; // Stopped at border
                            newPos.x = clampedX;
                        }
                    }

                    if (minY < maxY)
                    {
                        float clampedY = Mathf.Clamp(newPos.y, minY, maxY);
                        if (Mathf.Abs(clampedY - newPos.y) > 0.0001f)
                        {
                            actualDy = 0f; // Stopped at border
                            newPos.y = clampedY;
                        }
                    }
                }
            }

            transform.position = newPos;

            // Determine if Owen is actually moving in his current facing direction
            bool movedX = Mathf.Abs(actualDx) > 0.0001f;
            bool movedY = Mathf.Abs(actualDy) > 0.0001f;

            switch (currentFacing)
            {
                case FacingDirection.Left:
                    _isMoving = movedX && targetDx < -0.0001f;
                    break;
                case FacingDirection.Right:
                    _isMoving = movedX && targetDx > 0.0001f;
                    break;
                case FacingDirection.Up:
                    _isMoving = movedY && targetDy > 0.0001f;
                    break;
                case FacingDirection.Down:
                    _isMoving = movedY && targetDy < -0.0001f;
                    break;
                default:
                    _isMoving = movedX || movedY;
                    break;
            }

            // If completely blocked in facing direction but sliding sideways, update facing to the moving axis
            if (!_isMoving)
            {
                if (movedX && Mathf.Abs(targetDx) > 0.0001f)
                {
                    currentFacing = targetDx > 0f ? FacingDirection.Right : FacingDirection.Left;
                    _isMoving = true;
                }
                else if (movedY && Mathf.Abs(targetDy) > 0.0001f)
                {
                    currentFacing = targetDy > 0f ? FacingDirection.Up : FacingDirection.Down;
                    _isMoving = true;
                }
            }
        }

        private float AttemptMoveAxis(float dx, float dy)
        {
            Vector2 dir = new Vector2(dx, dy);
            float dist = dir.magnitude;
            if (dist < 0.00001f) return 0f;

            dir.Normalize();

            if (_ownCollider != null && _ownCollider.enabled)
            {
                int hitCount = _ownCollider.Cast(dir, _contactFilter, _hitBuffer, dist);
                float minAllowed = dist;
                bool hasHit = false;

                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit2D hit = _hitBuffer[i];
                    if (hit.collider != null && hit.collider != _ownCollider && !hit.collider.isTrigger)
                    {
                        // Check if hit distance is essentially zero (wall right in front)
                        float allowed = Mathf.Max(0f, hit.distance - 0.01f);
                        if (allowed < minAllowed)
                        {
                            minAllowed = allowed;
                            hasHit = true;
                        }
                    }
                }

                if (hasHit)
                {
                    return minAllowed * (dx != 0f ? Mathf.Sign(dx) : Mathf.Sign(dy));
                }
            }

            return dx != 0f ? dx : dy;
        }

        private void UpdateFacingDirection(float x, float y)
        {
            bool holdingCurrentDirection =
                (currentFacing == FacingDirection.Left && x < -0.01f) ||
                (currentFacing == FacingDirection.Right && x > 0.01f) ||
                (currentFacing == FacingDirection.Up && y > 0.01f) ||
                (currentFacing == FacingDirection.Down && y < -0.01f);

            // Deltarune diagonal rule: keep current facing direction if its key is still held
            if (_wasMoving && holdingCurrentDirection)
            {
                return;
            }

            if (Mathf.Abs(x) >= Mathf.Abs(y) && Mathf.Abs(x) > 0.01f)
            {
                currentFacing = x > 0f ? FacingDirection.Right : FacingDirection.Left;
            }
            else if (Mathf.Abs(y) > 0.01f)
            {
                currentFacing = y > 0f ? FacingDirection.Up : FacingDirection.Down;
            }
        }

        private void UpdateAnimation()
        {
            if (spriteRenderer == null) return;

            Sprite[] activeWalkFrames = null;
            Sprite activeIdleSprite = idleDownSprite;
            bool flipX = false;

            switch (currentFacing)
            {
                case FacingDirection.Down:
                    activeWalkFrames = walkDownFrames;
                    activeIdleSprite = idleDownSprite != null ? idleDownSprite : (walkDownFrames != null && walkDownFrames.Length > 0 ? walkDownFrames[1 % walkDownFrames.Length] : null);
                    flipX = false;
                    break;

                case FacingDirection.Up:
                    activeWalkFrames = walkUpFrames != null && walkUpFrames.Length > 0 ? walkUpFrames : walkDownFrames;
                    activeIdleSprite = idleUpSprite != null ? idleUpSprite : (activeWalkFrames != null && activeWalkFrames.Length > 0 ? activeWalkFrames[1 % activeWalkFrames.Length] : idleDownSprite);
                    flipX = false;
                    break;

                case FacingDirection.Left:
                    if (walkLeftFrames != null && walkLeftFrames.Length > 0)
                    {
                        activeWalkFrames = walkLeftFrames;
                        flipX = false;
                    }
                    else if (walkSideFrames != null && walkSideFrames.Length > 0)
                    {
                        activeWalkFrames = walkSideFrames;
                        flipX = false;
                    }
                    else
                    {
                        activeWalkFrames = walkRightFrames;
                        flipX = true;
                    }

                    if (!_isMoving)
                    {
                        if (idleLeftSprite != null)
                        {
                            activeIdleSprite = idleLeftSprite;
                            flipX = false;
                        }
                        else if (idleSideSprite != null)
                        {
                            activeIdleSprite = idleSideSprite;
                            flipX = true; // SideIdleSprite.ase faces Right natively
                        }
                    }
                    break;

                case FacingDirection.Right:
                    if (walkRightFrames != null && walkRightFrames.Length > 0)
                    {
                        activeWalkFrames = walkRightFrames;
                        flipX = false;
                    }
                    else if (walkLeftFrames != null && walkLeftFrames.Length > 0)
                    {
                        activeWalkFrames = walkLeftFrames;
                        flipX = true;
                    }
                    else if (walkSideFrames != null && walkSideFrames.Length > 0)
                    {
                        activeWalkFrames = walkSideFrames;
                        flipX = true;
                    }

                    if (!_isMoving)
                    {
                        activeIdleSprite = idleSideSprite != null ? idleSideSprite : idleLeftSprite;
                        flipX = idleSideSprite == null && idleLeftSprite != null;
                    }
                    break;
            }

            spriteRenderer.flipX = flipX;

            if (_isMoving && activeWalkFrames != null && activeWalkFrames.Length > 0)
            {
                if (!_wasMoving || _previousFacing != currentFacing)
                {
                    // Immediately show the first step frame (frame 0) when starting or turning
                    _currentFrame = 0;
                    _animTimer = 0f;
                    spriteRenderer.sprite = activeWalkFrames[0];
                }
                else
                {
                    float speedRatio = (_isRunning ? runMultiplier : 1f) * (_isMovingDiagonally ? diagonalSpeedMultiplier : 1f);
                    float effectiveFrameDuration = frameDuration / speedRatio;
                    _animTimer += Time.deltaTime;
                    if (_animTimer >= effectiveFrameDuration)
                    {
                        _animTimer -= effectiveFrameDuration;
                        _currentFrame = (_currentFrame + 1) % activeWalkFrames.Length;
                        spriteRenderer.sprite = activeWalkFrames[_currentFrame];
                    }
                }
            }
            else
            {
                _animTimer = 0f;
                _currentFrame = 0;
                if (activeIdleSprite != null)
                {
                    spriteRenderer.sprite = activeIdleSprite;
                }
            }
        }
    }
}
