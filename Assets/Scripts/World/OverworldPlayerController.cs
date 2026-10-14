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
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite[] walkDownFrames;
        [SerializeField] private float frameDuration = 0.15f; // 150ms per step

        private Rigidbody2D _rb;
        private Vector2 _moveInput;
        private float _animTimer;
        private int _currentFrame;
        private bool _isMoving;

        public bool IsMoving => _isMoving;
        public Vector2 MovementInput => _moveInput;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (idleSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = idleSprite;
            }
        }

        private void Update()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");
            _moveInput = new Vector2(x, y).normalized;

            _isMoving = _moveInput.sqrMagnitude > 0.01f;

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
            if (spriteRenderer == null || walkDownFrames == null || walkDownFrames.Length == 0)
                return;

            if (_isMoving)
            {
                _animTimer += Time.deltaTime;
                if (_animTimer >= frameDuration)
                {
                    _animTimer = 0f;
                    _currentFrame = (_currentFrame + 1) % walkDownFrames.Length;
                    spriteRenderer.sprite = walkDownFrames[_currentFrame];
                }
            }
            else
            {
                _animTimer = 0f;
                _currentFrame = 0;
                if (idleSprite != null)
                {
                    spriteRenderer.sprite = idleSprite;
                }
            }
        }
    }
}
