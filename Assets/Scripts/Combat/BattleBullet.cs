using System;
using UnityEngine;

namespace DigiPurity.Combat
{
    public class BattleBullet : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private Vector2 velocity = Vector2.down * 3f;
        [SerializeField] private float lifetime = 5f;

        [Header("Collision & Graze")]
        [SerializeField] private float hitRadius = 0.16f;
        [SerializeField] private float grazeRadius = 0.55f;
        [SerializeField] private int damage = 8;

        private SoulMovementBehaviour _soul;
        private Action _onGrazeCallback;
        private Action<int> _onHitCallback;
        private bool _hasGrazed = false;
        private float _spawnTime;

        public Vector2 Velocity { get => velocity; set => velocity = value; }
        public int Damage { get => damage; set => damage = value; }

        public void Initialize(SoulMovementBehaviour soul, Vector2 vel, int dmg, Action onGraze, Action<int> onHit)
        {
            _soul = soul;
            velocity = vel;
            damage = dmg;
            _onGrazeCallback = onGraze;
            _onHitCallback = onHit;
            _spawnTime = Time.time;
            _hasGrazed = false;
        }

        private void Update()
        {
            transform.position += (Vector3)(velocity * Time.deltaTime);

            if (_soul != null && _soul.gameObject.activeInHierarchy)
            {
                float dist = Vector2.Distance(transform.position, _soul.transform.position);

                // 1. Graze detection (near-miss)
                if (!_hasGrazed && dist <= grazeRadius && dist > hitRadius)
                {
                    _hasGrazed = true;
                    _onGrazeCallback?.Invoke();
                }

                // 2. Direct hit
                if (dist <= hitRadius)
                {
                    if (_soul.InvulnerabilityFrames <= 0)
                    {
                        _onHitCallback?.Invoke(damage);
                        _soul.TriggerInvulnerability(30); // 1 sec @ 30fps
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
