using System;
using System.Collections;
using UnityEngine;

namespace DigiPurity.Combat
{
    public class OwenBattleAnimator : MonoBehaviour
    {
        public enum BattleAnimState
        {
            Idle,
            Attack,
            Defend,
            Hurt,
            Downed
        }

        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Sprite Frames")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private Sprite[] defendFrames;
        [SerializeField] private Sprite hurtSprite;
        [SerializeField] private Sprite downedSprite;

        [Header("Timings")]
        [SerializeField] private float idleFrameDuration = 0.16f;

        private BattleAnimState _currentState = BattleAnimState.Idle;
        private float _animTimer = 0f;
        private int _currentFrame = 0;
        private Coroutine _specialAnimCoroutine;

        public BattleAnimState CurrentState => _currentState;

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

            AutoLoadSprites();
        }

        public void AutoLoadSprites()
        {
#if UNITY_EDITOR
            if (idleFrames == null || idleFrames.Length == 0)
            {
                idleFrames = new Sprite[4];
                for (int i = 0; i < 4; i++)
                {
                    idleFrames[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Owen/Battle/Owen_Battle_Idle_{i}.png");
                }
            }

            if (attackFrames == null || attackFrames.Length == 0)
            {
                attackFrames = new Sprite[5];
                for (int i = 0; i < 5; i++)
                {
                    attackFrames[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Owen/Battle/Owen_Battle_Attack_{i}.png");
                }
            }

            if (defendFrames == null || defendFrames.Length == 0)
            {
                defendFrames = new Sprite[3];
                for (int i = 0; i < 3; i++)
                {
                    defendFrames[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Owen/Battle/Owen_Battle_Defend_{i}.png");
                }
            }

            if (hurtSprite == null)
            {
                hurtSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Owen/Battle/Owen_Battle_Hurt_0.png");
            }

            if (downedSprite == null)
            {
                downedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Owen/Battle/Owen_Battle_Downed_Kneel_0.png");
            }
#endif
            if (spriteRenderer != null && spriteRenderer.sprite == null && idleFrames != null && idleFrames.Length > 0)
            {
                spriteRenderer.sprite = idleFrames[0];
            }
        }

        private void Update()
        {
            if (_specialAnimCoroutine != null) return;

            switch (_currentState)
            {
                case BattleAnimState.Idle:
                    UpdateIdleLoop();
                    break;

                case BattleAnimState.Defend:
                    if (defendFrames != null && defendFrames.Length > 0)
                    {
                        spriteRenderer.sprite = defendFrames[0];
                    }
                    break;

                case BattleAnimState.Downed:
                    if (downedSprite != null)
                    {
                        spriteRenderer.sprite = downedSprite;
                    }
                    break;
            }
        }

        private void UpdateIdleLoop()
        {
            if (idleFrames == null || idleFrames.Length == 0) return;

            _animTimer += Time.deltaTime;
            if (_animTimer >= idleFrameDuration)
            {
                _animTimer -= idleFrameDuration;
                _currentFrame = (_currentFrame + 1) % idleFrames.Length;
                if (idleFrames[_currentFrame] != null)
                {
                    spriteRenderer.sprite = idleFrames[_currentFrame];
                }
            }
        }

        public void PlayIdle()
        {
            StopSpecialAnim();
            _currentState = BattleAnimState.Idle;
            _animTimer = 0f;
            _currentFrame = 0;
            if (idleFrames != null && idleFrames.Length > 0)
            {
                spriteRenderer.sprite = idleFrames[0];
            }
        }

        public void PlayDefend()
        {
            StopSpecialAnim();
            _currentState = BattleAnimState.Defend;
            if (defendFrames != null && defendFrames.Length > 0)
            {
                spriteRenderer.sprite = defendFrames[0];
            }
        }

        public void PlayHurt(float duration = 0.4f, Action onComplete = null)
        {
            StopSpecialAnim();
            _specialAnimCoroutine = StartCoroutine(HurtRoutine(duration, onComplete));
        }

        private IEnumerator HurtRoutine(float duration, Action onComplete)
        {
            _currentState = BattleAnimState.Hurt;
            if (hurtSprite != null)
            {
                spriteRenderer.sprite = hurtSprite;
            }

            Vector3 origin = transform.localPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float shake = Mathf.Sin(elapsed * 50f) * 0.15f * (1f - (elapsed / duration));
                transform.localPosition = origin + new Vector3(shake, 0f, 0f);
                yield return null;
            }

            transform.localPosition = origin;
            _specialAnimCoroutine = null;
            PlayIdle();
            onComplete?.Invoke();
        }

        /// <summary>
        /// Plays attack animation up to clash frame, freezes for clashHoldDuration (.25s), then finishes.
        /// </summary>
        public void PlayClashAttack(float clashHoldDuration = 0.25f, Action onClashImpact = null, Action onComplete = null)
        {
            StopSpecialAnim();
            _specialAnimCoroutine = StartCoroutine(ClashAttackRoutine(clashHoldDuration, onClashImpact, onComplete));
        }

        private IEnumerator ClashAttackRoutine(float holdDuration, Action onClashImpact, Action onComplete)
        {
            _currentState = BattleAnimState.Attack;

            if (attackFrames != null && attackFrames.Length > 0)
            {
                // Play frame 0 and 1
                spriteRenderer.sprite = attackFrames[0];
                yield return new WaitForSeconds(0.06f);

                if (attackFrames.Length > 1)
                {
                    spriteRenderer.sprite = attackFrames[1];
                    yield return new WaitForSeconds(0.06f);
                }

                // Impact / clash held frame (frame 2)
                int clashFrameIdx = Mathf.Min(2, attackFrames.Length - 1);
                spriteRenderer.sprite = attackFrames[clashFrameIdx];

                onClashImpact?.Invoke();

                // Hold frame for 0.25 seconds
                yield return new WaitForSeconds(holdDuration);

                // Finish swing frames
                for (int i = clashFrameIdx + 1; i < attackFrames.Length; i++)
                {
                    spriteRenderer.sprite = attackFrames[i];
                    yield return new WaitForSeconds(0.05f);
                }
            }

            _specialAnimCoroutine = null;
            PlayIdle();
            onComplete?.Invoke();
        }

        public void PlayDowned()
        {
            StopSpecialAnim();
            _currentState = BattleAnimState.Downed;
            if (downedSprite != null)
            {
                spriteRenderer.sprite = downedSprite;
            }
        }

        private void StopSpecialAnim()
        {
            if (_specialAnimCoroutine != null)
            {
                StopCoroutine(_specialAnimCoroutine);
                _specialAnimCoroutine = null;
            }
        }
    }
}
