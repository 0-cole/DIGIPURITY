using System;
using UnityEngine;

namespace DigiPurity.Combat
{
    public class BattleBullet : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private Vector2 velocity = Vector2.down * 3.5f;
        [SerializeField] private float lifetime = 5.5f;
        [SerializeField] private bool bounceInsideBox = false;
        [SerializeField] private int maxBounces = 5;

        [Header("Collision & Movement Rule")]
        [SerializeField] private float hitRadius = 0.20f;
        [SerializeField] private int damage = 10;
        [SerializeField] private HazardMovementRule movementRule = HazardMovementRule.StandardWhite;

        private SoulMovementBehaviour _soul;
        private BattleDodgeBox _dodgeBox;
        private Action<int> _onHitCallback;
        private Action<int, HazardMovementRule> _onRuleHitCallback;
        private float _spawnTime;
        private int _bounceCount = 0;
        private Vector3 _lastSoulPos;
        private bool _hasLastSoulPos = false;

        public Vector2 Velocity { get => velocity; set => velocity = value; }
        public int Damage { get => damage; set => damage = value; }

        public void Initialize(SoulMovementBehaviour soul, Vector2 vel, int dmg, Action<int> onHit)
        {
            _soul = soul;
            velocity = vel;
            damage = dmg;
            _onHitCallback = onHit;
            _spawnTime = Time.time;
            if (_soul != null)
            {
                _lastSoulPos = _soul.transform.position;
                _hasLastSoulPos = true;
            }
        }

        public void InitializeModular(
            SoulMovementBehaviour soul,
            BattleDodgeBox dodgeBox,
            Vector2 vel,
            int dmg,
            HazardMovementRule rule,
            bool bounceInBox,
            float customHitRadius,
            Action<int, HazardMovementRule> onRuleHit)
        {
            _soul = soul;
            _dodgeBox = dodgeBox;
            velocity = vel;
            damage = dmg;
            movementRule = rule;
            bounceInsideBox = bounceInBox;
            if (customHitRadius > 0.04f) hitRadius = customHitRadius;
            _onRuleHitCallback = onRuleHit;
            _spawnTime = Time.time;
            if (_soul != null)
            {
                _lastSoulPos = _soul.transform.position;
                _hasLastSoulPos = true;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            transform.position += (Vector3)(velocity * dt);

            if (bounceInsideBox && _dodgeBox != null && _bounceCount < maxBounces)
            {
                Vector2 c = _dodgeBox.BoxCenter;
                Vector2 half = (_dodgeBox.BoxSize * 0.5f) - new Vector2(0.16f, 0.16f);
                Vector3 p = transform.position;
                bool bounced = false;

                if (p.x <= c.x - half.x && velocity.x < 0f) { p.x = c.x - half.x; velocity.x = -velocity.x; bounced = true; }
                else if (p.x >= c.x + half.x && velocity.x > 0f) { p.x = c.x + half.x; velocity.x = -velocity.x; bounced = true; }

                if (p.y <= c.y - half.y && velocity.y < 0f) { p.y = c.y - half.y; velocity.y = -velocity.y; bounced = true; }
                else if (p.y >= c.y + half.y && velocity.y > 0f) { p.y = c.y + half.y; velocity.y = -velocity.y; bounced = true; }

                if (bounced)
                {
                    _bounceCount++;
                    transform.position = p;
                }
            }

            bool soulMoving = false;
            if (_soul != null)
            {
                Vector3 cur = _soul.transform.position;
                if (_hasLastSoulPos)
                {
                    soulMoving = (cur - _lastSoulPos).magnitude > 0.0025f;
                }
                _lastSoulPos = cur;
                _hasLastSoulPos = true;
            }

            if (_soul != null && _soul.gameObject.activeInHierarchy)
            {
                float dist = Vector2.Distance(transform.position, _soul.transform.position);
                if (dist <= hitRadius)
                {
                    if (!AttackDamageZone.ShouldTriggerForMovementRule(movementRule, soulMoving))
                    {
                        return;
                    }

                    bool isBeneficial = (movementRule == HazardMovementRule.GreenHealOrb || movementRule == HazardMovementRule.GoldPurifyOrb);
                    if (isBeneficial || _soul.InvulnerabilityFrames <= 0)
                    {
                        if (_onRuleHitCallback != null)
                        {
                            _onRuleHitCallback.Invoke(damage, movementRule);
                        }
                        else
                        {
                            _onHitCallback?.Invoke(damage);
                        }

                        if (!isBeneficial)
                        {
                            _soul.TriggerInvulnerability(30);
                        }
                        Destroy(gameObject);
                        return;
                    }
                }
            }

            if (Time.time - _spawnTime > lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
