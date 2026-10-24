using System;
using UnityEngine;

namespace DigiPurity.Combat
{
    public class BattleBullet : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private Vector2 velocity = Vector2.down * 3.5f;
        [SerializeField] private float lifetime = 5f;

        [Header("Collision")]
        [SerializeField] private float hitRadius = 0.20f;
        [SerializeField] private int damage = 10;

        private SoulMovementBehaviour _soul;
        private Action<int> _onHitCallback;
        private float _spawnTime;

        public Vector2 Velocity { get => velocity; set => velocity = value; }
        public int Damage { get => damage; set => damage = value; }

        public void Initialize(SoulMovementBehaviour soul, Vector2 vel, int dmg, Action<int> onHit)
        {
            _soul = soul;
            velocity = vel;
            damage = dmg;
            _onHitCallback = onHit;
            _spawnTime = Time.time;
        }

        private void Update()
        {
            transform.position += (Vector3)(velocity * Time.deltaTime);

            if (_soul != null && _soul.gameObject.activeInHierarchy)
            {
                float dist = Vector2.Distance(transform.position, _soul.transform.position);
                if (dist <= hitRadius)
                {
                    if (_soul.InvulnerabilityFrames <= 0)
                    {
                        _onHitCallback?.Invoke(damage);
                        _soul.TriggerInvulnerability(30);
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
