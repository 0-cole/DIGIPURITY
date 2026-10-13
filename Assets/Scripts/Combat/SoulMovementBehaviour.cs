using UnityEngine;

namespace DigiPurity.Combat
{
    public class SoulMovementBehaviour : MonoBehaviour
    {
        [SerializeField] private float baseSpeed = 4f;
        [SerializeField] private Vector2 boundsMin = new Vector2(-5f, -5f);
        [SerializeField] private Vector2 boundsMax = new Vector2(5f, 5f);
        [SerializeField] private float borderMargin = 0.5f;

        private SoulMovementLogic _logic;
        private int _invulnerabilityFrames;

        public int InvulnerabilityFrames => _invulnerabilityFrames;

        private void Awake()
        {
            _logic = new SoulMovementLogic(baseSpeed, boundsMin, boundsMax, borderMargin);
        }

        private void Update()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector2 inputDir = new Vector2(horizontal, vertical);

            bool slowHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.X);

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
