using UnityEngine;

namespace DigiPurity.World
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class OverworldPlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float slowMultiplier = 0.5f;

        [Header("Sprites & Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite idleDownSprite;
        [SerializeField] private Sprite idleUpSprite;
        [SerializeField] private Sprite idleSideSprite;

        [SerializeField] private Sprite[] walkDownFrames;
        [SerializeField] private Sprite[] walkUpFrames;
        [SerializeField] private Sprite[] walkSideFrames;
        [SerializeField] private float frameDuration = 0.15f; // 150ms per step

        public enum FacingDirection { Down, Up, Left, Right }
        [SerializeField] private FacingDirection currentFacing = FacingDirection.Down;

        private Rigidbody2D _rb;
        private Vector2 _moveInput;
        private float _animTimer;
        private int _currentFrame;
        private bool _isMoving;

        public bool IsMoving => _isMoving;
        public Vector2 MovementInput => _moveInput;
        public FacingDirection CurrentFacing => currentFacing;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (idleDownSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = idleDownSprite;
            }
        }

        private void Update()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");
            _moveInput = new Vector2(x, y).normalized;

            _isMoving = _moveInput.sqrMagnitude > 0.01f;

            if (_isMoving)
            {
                // Update facing direction prioritising horizontal or vertical
                if (Mathf.Abs(x) > Mathf.Abs(y))
                {
                    currentFacing = x > 0 ? FacingDirection.Right : FacingDirection.Left;
                }
                else
                {
                    currentFacing = y > 0 ? FacingDirection.Up : FacingDirection.Down;
                }
            }

            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            float speed = moveSpeed;
            // Undertale precision / cancel slow walk invariant
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.X))
            {
                speed *= slowMultiplier;
            }

            _rb.linearVelocity = _moveInput * speed;
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
                    activeIdleSprite = idleDownSprite != null ? idleDownSprite : (walkDownFrames != null && walkDownFrames.Length > 0 ? walkDownFrames[0] : null);
                    flipX = false;
                    break;

                case FacingDirection.Up:
                    activeWalkFrames = walkUpFrames != null && walkUpFrames.Length > 0 ? walkUpFrames : walkDownFrames;
                    activeIdleSprite = idleUpSprite != null ? idleUpSprite : (activeWalkFrames != null && activeWalkFrames.Length > 0 ? activeWalkFrames[0] : idleDownSprite);
                    flipX = false;
                    break;

                case FacingDirection.Left:
                    activeWalkFrames = walkSideFrames != null && walkSideFrames.Length > 0 ? walkSideFrames : walkDownFrames;
                    activeIdleSprite = idleSideSprite != null ? idleSideSprite : (activeWalkFrames != null && activeWalkFrames.Length > 0 ? activeWalkFrames[0] : idleDownSprite);
                    flipX = false;
                    break;

                case FacingDirection.Right:
                    // Auto-mirror left sprites for right facing
                    activeWalkFrames = walkSideFrames != null && walkSideFrames.Length > 0 ? walkSideFrames : walkDownFrames;
                    activeIdleSprite = idleSideSprite != null ? idleSideSprite : (activeWalkFrames != null && activeWalkFrames.Length > 0 ? activeWalkFrames[0] : idleDownSprite);
                    flipX = true;
                    break;
            }

            spriteRenderer.flipX = flipX;

            if (_isMoving && activeWalkFrames != null && activeWalkFrames.Length > 0)
            {
                _animTimer += Time.deltaTime;
                if (_animTimer >= frameDuration)
                {
                    _animTimer = 0f;
                    _currentFrame = (_currentFrame + 1) % activeWalkFrames.Length;
                    spriteRenderer.sprite = activeWalkFrames[_currentFrame];
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
