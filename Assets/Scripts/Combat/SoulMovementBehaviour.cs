using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DigiPurity.Combat
{
    public class SoulMovementBehaviour : MonoBehaviour
    {
        [SerializeField] private float baseSpeed = 4f;
        [SerializeField] private float diagonalMultiplier = 1.5f;
        [SerializeField] private Vector2 boundsMin = new Vector2(-5f, -5f);
        [SerializeField] private Vector2 boundsMax = new Vector2(5f, 5f);
        [SerializeField] private float borderMargin = 0.5f;

        private SoulMovementLogic _logic;
        private int _invulnerabilityFrames;

        public int InvulnerabilityFrames => _invulnerabilityFrames;
        public float DiagonalMultiplier => diagonalMultiplier;

        public void SetBounds(Vector2 min, Vector2 max, float margin = 0.15f)
        {
            boundsMin = min;
            boundsMax = max;
            borderMargin = margin;
            _logic = new SoulMovementLogic(baseSpeed, boundsMin, boundsMax, borderMargin, diagonalMultiplier);
        }

        private void Awake()
        {
            _logic = new SoulMovementLogic(baseSpeed, boundsMin, boundsMax, borderMargin, diagonalMultiplier);
        }

        private void Update()
        {
            float horizontal = 0f;
            float vertical = 0f;
            bool slowHeld = false;

#if ENABLE_INPUT_SYSTEM
            // 1. Read Unity New Input System Keyboard (WASD + Arrow Keys + Shift/X Slow-mode)
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) horizontal += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) horizontal -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) vertical += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) vertical -= 1f;

                slowHeld = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed || kb.xKey.isPressed;
            }

            // 2. Read Gamepad (Left Stick + D-Pad + Face/Shoulder button slow)
            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                if (Mathf.Abs(stick.x) > 0.15f) horizontal += Mathf.Sign(stick.x);
                if (Mathf.Abs(stick.y) > 0.15f) vertical += Mathf.Sign(stick.y);

                if (pad.dpad.right.isPressed) horizontal += 1f;
                if (pad.dpad.left.isPressed) horizontal -= 1f;
                if (pad.dpad.up.isPressed) vertical += 1f;
                if (pad.dpad.down.isPressed) vertical -= 1f;

                if (pad.buttonEast.isPressed || pad.leftShoulder.isPressed) slowHeld = true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            // Legacy Input Manager fallback if enabled
            if (Mathf.Approximately(horizontal, 0f) && Mathf.Approximately(vertical, 0f))
            {
                horizontal = Input.GetAxisRaw("Horizontal");
                vertical = Input.GetAxisRaw("Vertical");
                slowHeld = slowHeld || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.X);
            }
#endif

            Vector2 inputDir = new Vector2(horizontal, vertical);
            Vector2 next = _logic.CalculateNextPosition(transform.position, inputDir, slowHeld, Time.deltaTime);
            transform.position = new Vector3(next.x, next.y, transform.position.z);

            if (_invulnerabilityFrames > 0)
            {
                _invulnerabilityFrames = SoulMovementLogic.DecrementInvulnerability(_invulnerabilityFrames);
            }
        }

        public void TriggerInvulnerability(int frames = 30)
        {
            _invulnerabilityFrames = frames;
        }
    }
}
