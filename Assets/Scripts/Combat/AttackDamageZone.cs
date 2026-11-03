using System;
using UnityEngine;

namespace DigiPurity.Combat
{
    /// <summary>
    /// Attach AttackDamageZone to ANY child GameObject/Sprite inside a Custom Attack Wave Prefab
    /// to make different parts of your attack deal different damage or special effects!
    /// Examples:
    /// - Club Head / Laser Core: damage = 20 (Red Gizmo = Heavy Damage)
    /// - Outer Sparks / Shockwave Dust: damage = 5 (Orange Gizmo = Splash Damage)
    /// - Flashing Warning Box before Beam: damage = 0 (Yellow Gizmo = 0 DMG Telegraph)
    /// - Cyan / Orange Movement Hazard or Green Heal / Gold Purify Orb!
    /// </summary>
    public class AttackDamageZone : MonoBehaviour
    {
        [Header("Zone Identity & Per-Part Damage")]
        [Tooltip("Label for your own reference in the Inspector (e.g. 'Laser Core - Heavy DMG' or 'Edge Sparks - Light DMG').")]
        public string zoneName = "Core Hitbox";
        [Tooltip("Damage dealt when Owen's soul touches this specific part of the attack (0 = harmless warning telegraph!).")]
        public int damage = 12;
        public HazardMovementRule movementRule = HazardMovementRule.StandardWhite;

        [Header("Hitbox Shape (Uses Collider2D if attached, otherwise Circle Radius)")]
        [Tooltip("Fallback circular hit radius if you don't attach a BoxCollider2D / CircleCollider2D / PolygonCollider2D.")]
        public float fallbackHitRadius = 0.22f;
        public int invulnerabilityFramesOnHit = 30;
        [Tooltip("True for bullets/pellets that vanish on hit; False for persistent beams, clubs, or sweeping hazards!")]
        public bool destroyOnContact = false;

        [Header("Optional Built-in Motion & Lifetime")]
        [Tooltip("Optional world velocity so child hazards in a prefab can move automatically without needing custom scripts!")]
        public Vector2 linearVelocity = Vector2.zero;
        [Tooltip("Optional rotation speed in degrees/sec (great for spinning clubs, saws, or crossbeams!).")]
        public float angularVelocityDegPerSec = 0f;
        [Tooltip("If true, bounces off the inside walls of the active Dodge Box!")]
        public bool bounceInsideDodgeBox = false;
        [Tooltip("Seconds to wait after wave spawns before this hitbox turns active (great for 0.6s warning telegraphs that turn lethal!).")]
        public float activationDelaySeconds = 0f;
        [Tooltip("0 = lives until attack wave ends; > 0 = auto-destroys after N seconds.")]
        public float activeDurationSeconds = 0f;

        private SoulMovementBehaviour _soul;
        private BattleDodgeBox _dodgeBox;
        private Action<int, HazardMovementRule> _onZoneContact;
        private Collider2D _collider;
        private float _elapsed = 0f;
        private Vector3 _lastSoulPos;
        private bool _hasLastSoulPos = false;

        public bool IsCurrentlyActive => _elapsed >= activationDelaySeconds &&
                                         (activeDurationSeconds <= 0.001f || _elapsed <= activationDelaySeconds + activeDurationSeconds);

        public void BindRuntimeContext(
            SoulMovementBehaviour soul,
            BattleDodgeBox dodgeBox,
            Action<int, HazardMovementRule> onZoneContact)
        {
            _soul = soul;
            _dodgeBox = dodgeBox;
            _onZoneContact = onZoneContact;
            _collider = GetComponent<Collider2D>();
            if (_soul != null)
            {
                _lastSoulPos = _soul.transform.position;
                _hasLastSoulPos = true;
            }
        }

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _elapsed += dt;

            if (activeDurationSeconds > 0.001f && _elapsed > activationDelaySeconds + activeDurationSeconds)
            {
                Destroy(gameObject);
                return;
            }

            if (_elapsed < activationDelaySeconds)
            {
                TrackSoulMovement();
                return;
            }

            // Optional built-in motion
            if (linearVelocity.sqrMagnitude > 0.0001f)
            {
                transform.position += (Vector3)(linearVelocity * dt);

                if (bounceInsideDodgeBox && _dodgeBox != null)
                {
                    Vector2 c = _dodgeBox.BoxCenter;
                    Vector2 half = (_dodgeBox.BoxSize * 0.5f) - new Vector2(0.18f, 0.18f);
                    Vector3 p = transform.position;

                    if (p.x <= c.x - half.x && linearVelocity.x < 0f) { p.x = c.x - half.x; linearVelocity.x = -linearVelocity.x; }
                    else if (p.x >= c.x + half.x && linearVelocity.x > 0f) { p.x = c.x + half.x; linearVelocity.x = -linearVelocity.x; }

                    if (p.y <= c.y - half.y && linearVelocity.y < 0f) { p.y = c.y - half.y; linearVelocity.y = -linearVelocity.y; }
                    else if (p.y >= c.y + half.y && linearVelocity.y > 0f) { p.y = c.y + half.y; linearVelocity.y = -linearVelocity.y; }

                    transform.position = p;
                }
            }

            if (Mathf.Abs(angularVelocityDegPerSec) > 0.01f)
            {
                transform.Rotate(0f, 0f, angularVelocityDegPerSec * dt);
            }

            bool soulIsMoving = TrackSoulMovement();

            if (_soul == null || !_soul.gameObject.activeInHierarchy) return;
            if (damage <= 0 && movementRule == HazardMovementRule.StandardWhite) return; // Harmless telegraph zone

            if (CheckOverlapWithSoul(_soul.transform.position))
            {
                if (!ShouldTriggerForMovementRule(movementRule, soulIsMoving))
                {
                    return;
                }

                bool isBeneficial = (movementRule == HazardMovementRule.GreenHealOrb || movementRule == HazardMovementRule.GoldPurifyOrb);
                if (isBeneficial || _soul.InvulnerabilityFrames <= 0)
                {
                    _onZoneContact?.Invoke(damage, movementRule);
                    if (!isBeneficial && invulnerabilityFramesOnHit > 0)
                    {
                        _soul.TriggerInvulnerability(invulnerabilityFramesOnHit);
                    }
                    if (destroyOnContact || isBeneficial)
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }

        private bool TrackSoulMovement()
        {
            if (_soul == null) return false;
            Vector3 cur = _soul.transform.position;
            if (!_hasLastSoulPos)
            {
                _lastSoulPos = cur;
                _hasLastSoulPos = true;
                return false;
            }
            float movedDist = (cur - _lastSoulPos).magnitude;
            _lastSoulPos = cur;
            return movedDist > 0.0025f;
        }

        public static bool ShouldTriggerForMovementRule(HazardMovementRule rule, bool soulIsMoving)
        {
            switch (rule)
            {
                case HazardMovementRule.CyanStopToAvoid:
                    return soulIsMoving; // Only hurts if moving!
                case HazardMovementRule.OrangeMoveToAvoid:
                    return !soulIsMoving; // Only hurts if standing still!
                default:
                    return true;
            }
        }

        public bool CheckOverlapWithSoul(Vector2 soulWorldPos)
        {
            if (_collider == null) _collider = GetComponent<Collider2D>();
            if (_collider != null && _collider.enabled)
            {
                return _collider.OverlapPoint(soulWorldPos);
            }
            return Vector2.Distance(transform.position, soulWorldPos) <= fallbackHitRadius;
        }

        private void OnDrawGizmos()
        {
            // Color-coded Scene/Prefab View Gizmo so you immediately see which parts deal heavy vs light damage!
            if (movementRule == HazardMovementRule.GreenHealOrb)
                Gizmos.color = new Color(0.2f, 1.0f, 0.35f, 0.85f);
            else if (movementRule == HazardMovementRule.GoldPurifyOrb)
                Gizmos.color = new Color(1.0f, 0.9f, 0.2f, 0.85f);
            else if (movementRule == HazardMovementRule.CyanStopToAvoid)
                Gizmos.color = new Color(0.15f, 0.92f, 1.0f, 0.85f);
            else if (movementRule == HazardMovementRule.OrangeMoveToAvoid)
                Gizmos.color = new Color(1.0f, 0.55f, 0.1f, 0.85f);
            else if (damage <= 0)
                Gizmos.color = new Color(1.0f, 0.95f, 0.25f, 0.65f); // 0 DMG Telegraph
            else if (damage >= 14)
                Gizmos.color = new Color(1.0f, 0.15f, 0.2f, 0.92f);  // Heavy Sweet-Spot Damage
            else
                Gizmos.color = new Color(1.0f, 0.55f, 0.25f, 0.80f); // Light/Splash Damage

            var box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                Matrix4x4 old = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.offset, box.size);
                Gizmos.matrix = old;
                return;
            }

            var circle = GetComponent<CircleCollider2D>();
            if (circle != null)
            {
                float maxScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
                Gizmos.DrawWireSphere(transform.TransformPoint(circle.offset), circle.radius * maxScale);
                return;
            }

            Gizmos.DrawWireSphere(transform.position, fallbackHitRadius);
        }
    }
}
