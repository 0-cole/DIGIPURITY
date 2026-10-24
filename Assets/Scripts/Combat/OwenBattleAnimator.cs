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
        private Vector3 _basePosition;

        public BattleAnimState CurrentState => _currentState;

        public SpriteRenderer EnsureRenderer()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }
            if (spriteRenderer.material == null || spriteRenderer.material.shader.name != "Universal Render Pipeline/2D/Sprite-Unlit-Default")
            {
                spriteRenderer.material = BattleDodgeBox.GetUnlitMaterial(Color.white);
            }
            spriteRenderer.color = Color.white;
            if (spriteRenderer.sortingOrder < 10) spriteRenderer.sortingOrder = 50;
            return spriteRenderer;
        }

        public void SetBasePosition(Vector3 newPos)
        {
            _basePosition = newPos;
            transform.position = newPos;
        }

        public static Sprite LoadSpriteDirect(string relativePath, Vector2 pivot, float ppu = 100f)
        {
#if UNITY_EDITOR
            Sprite asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
            if (asset != null) return asset;
#endif
            try
            {
                string fullPath = System.IO.Path.Combine(Application.dataPath, "..", relativePath);
                if (System.IO.File.Exists(fullPath))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.LoadImage(bytes);
                    tex.filterMode = FilterMode.Point;
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OwenBattleAnimator] Direct sprite load failed for {relativePath}: {ex.Message}");
            }
            return null;
        }

        private void Awake()
        {
            EnsureRenderer();
            AutoLoadSprites();
            _basePosition = transform.position;
        }

        public void AutoLoadSprites()
        {
            EnsureRenderer();

            if (idleFrames == null || idleFrames.Length == 0 || idleFrames[0] == null)
            {
                idleFrames = new Sprite[4];
                for (int i = 0; i < 4; i++)
                {
                    idleFrames[i] = LoadSpriteDirect($"Assets/Sprites/Owen/Battle/Owen_Battle_Idle_{i}.png", new Vector2(0.5f, 0f), 100f);
                }
            }

            if (attackFrames == null || attackFrames.Length == 0 || attackFrames[0] == null)
            {
                attackFrames = new Sprite[5];
                for (int i = 0; i < 5; i++)
                {
                    attackFrames[i] = LoadSpriteDirect($"Assets/Sprites/Owen/Battle/Owen_Battle_Attack_{i}.png", new Vector2(0.5f, 0f), 100f);
                }
            }

            if (defendFrames == null || defendFrames.Length == 0 || defendFrames[0] == null)
            {
                defendFrames = new Sprite[3];
                for (int i = 0; i < 3; i++)
                {
                    defendFrames[i] = LoadSpriteDirect($"Assets/Sprites/Owen/Battle/Owen_Battle_Defend_{i}.png", new Vector2(0.5f, 0f), 100f);
                }
            }

            if (hurtSprite == null)
            {
                hurtSprite = LoadSpriteDirect("Assets/Sprites/Owen/Battle/Owen_Battle_Hurt_0.png", new Vector2(0.5f, 0f), 100f);
            }

            if (downedSprite == null)
            {
                downedSprite = LoadSpriteDirect("Assets/Sprites/Owen/Battle/Owen_Battle_Downed_Kneel_0.png", new Vector2(0.5f, 0f), 100f);
            }

            if (spriteRenderer != null && idleFrames != null && idleFrames.Length > 0 && idleFrames[0] != null)
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
                        EnsureRenderer().sprite = defendFrames[0];
                    }
                    break;

                case BattleAnimState.Downed:
                    if (downedSprite != null)
                    {
                        EnsureRenderer().sprite = downedSprite;
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
                    EnsureRenderer().sprite = idleFrames[_currentFrame];
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
                EnsureRenderer().sprite = idleFrames[0];
            }
        }

        public void PlayDefend()
        {
            StopSpecialAnim();
            _currentState = BattleAnimState.Defend;
            if (defendFrames != null && defendFrames.Length > 0)
            {
                EnsureRenderer().sprite = defendFrames[0];
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
                EnsureRenderer().sprite = hurtSprite;
            }

            Vector3 origin = _basePosition != Vector3.zero ? _basePosition : transform.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float shake = Mathf.Sin(elapsed * 50f) * 0.15f * (1f - (elapsed / duration));
                transform.position = origin + new Vector3(shake, 0f, 0f);
                yield return null;
            }

            transform.position = origin;
            _specialAnimCoroutine = null;
            PlayIdle();
            onComplete?.Invoke();
        }

        public void PlayAttack(Action onComplete = null)
        {
            PlayClashAttack(0.15f, null, onComplete);
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
            var sr = EnsureRenderer();

            if (attackFrames != null && attackFrames.Length > 0)
            {
                // Play frame 0 and 1
                sr.sprite = attackFrames[0];
                yield return new WaitForSeconds(0.06f);

                if (attackFrames.Length > 1)
                {
                    sr.sprite = attackFrames[1];
                    yield return new WaitForSeconds(0.06f);
                }

                // Impact / clash held frame (frame 2)
                int clashFrameIdx = Mathf.Min(2, attackFrames.Length - 1);
                sr.sprite = attackFrames[clashFrameIdx];

                onClashImpact?.Invoke();

                // Hold frame for 0.25 seconds
                yield return new WaitForSeconds(holdDuration);

                // Finish swing frames
                for (int i = clashFrameIdx + 1; i < attackFrames.Length; i++)
                {
                    sr.sprite = attackFrames[i];
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
                EnsureRenderer().sprite = downedSprite;
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
