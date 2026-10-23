using System;
using System.Collections;
using UnityEngine;

namespace DigiPurity.Combat
{
    public class EnemyBattleAnimator : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Sprites")]
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite hurtSprite;

        [Header("Procedural Bob")]
        [SerializeField] private float bobFrequency = 2.5f;
        [SerializeField] private float bobAmplitude = 0.12f;

        private Vector3 _baseLocalPos;
        private bool _isBobbing = true;
        private Coroutine _hurtCoroutine;

        public bool IsBobbing { get => _isBobbing; set => _isBobbing = value; }

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            _baseLocalPos = transform.localPosition;
        }

        public void SetEnemyVisuals(Sprite battleSprite, Sprite hurt, float scale = 1.0f)
        {
            idleSprite = battleSprite;
            hurtSprite = hurt;

            if (spriteRenderer != null && idleSprite != null)
            {
                spriteRenderer.sprite = idleSprite;
            }

            transform.localScale = new Vector3(scale, scale, 1.0f);
        }

        private void Update()
        {
            if (_isBobbing && _hurtCoroutine == null)
            {
                float dy = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
                transform.localPosition = _baseLocalPos + new Vector3(0f, dy, 0f);
            }
        }

        public void SetBasePosition(Vector3 newBase)
        {
            _baseLocalPos = newBase;
            transform.localPosition = newBase;
        }

        public void PlayHurtShake(float duration = 0.35f, Action onComplete = null)
        {
            if (_hurtCoroutine != null) StopCoroutine(_hurtCoroutine);
            _hurtCoroutine = StartCoroutine(HurtShakeRoutine(duration, onComplete));
        }

        private IEnumerator HurtShakeRoutine(float duration, Action onComplete)
        {
            float elapsed = 0f;
            Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

            if (hurtSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = hurtSprite;
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float shake = Mathf.Sin(elapsed * 45f) * 0.22f * (1f - progress);
                transform.localPosition = _baseLocalPos + new Vector3(shake, 0f, 0f);

                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Mathf.Sin(elapsed * 30f) > 0f ? new Color(1f, 0.4f, 0.4f, 1f) : Color.white;
                }

                yield return null;
            }

            transform.localPosition = _baseLocalPos;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
                if (idleSprite != null) spriteRenderer.sprite = idleSprite;
            }

            _hurtCoroutine = null;
            onComplete?.Invoke();
        }
    }
}
