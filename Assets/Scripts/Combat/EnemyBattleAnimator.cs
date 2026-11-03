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
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private float idleFps = 6f;
        [SerializeField] private Sprite hurtSprite;
        [SerializeField] private Sprite purifyReadySprite;
        [SerializeField] private Sprite zeroHpExhaustedSprite;

        [Header("Procedural Bob")]
        [SerializeField] private float bobFrequency = 2.5f;
        [SerializeField] private float bobAmplitude = 0.12f;

        private Vector3 _basePosition;
        private bool _isBobbing = true;
        private bool _isZeroHpState = false;
        private bool _isPurifyReadyState = false;
        private float _bobPhaseOffset = 0f;
        private Coroutine _hurtCoroutine;

        public bool IsBobbing { get => _isBobbing; set => _isBobbing = value; }

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
            spriteRenderer.enabled = true;
            if (spriteRenderer.sortingOrder < 10) spriteRenderer.sortingOrder = 50;
            return spriteRenderer;
        }

        private void Awake()
        {
            EnsureRenderer();
            _basePosition = transform.position;
        }

        public void SetEnemyVisuals(Sprite battleSprite, Sprite hurt, float scale = 1.0f)
        {
            idleSprite = battleSprite;
            idleFrames = battleSprite != null ? new Sprite[] { battleSprite } : null;
            hurtSprite = hurt;
            _isZeroHpState = false;
            _isPurifyReadyState = false;

            var sr = EnsureRenderer();
            if (idleSprite != null)
            {
                sr.sprite = idleSprite;
            }

            transform.localScale = new Vector3(scale, scale, 1.0f);
        }

        public void ConfigureFromVisualSetup(EnemyVisualSetup setup, EnemyData fallbackData = null, int slotIndex = 0)
        {
            var sr = EnsureRenderer();
            _isZeroHpState = false;
            _isPurifyReadyState = false;
            _bobPhaseOffset = slotIndex * 1.15f;

            if (setup != null && setup.idleFrames != null && setup.idleFrames.Length > 0 && setup.idleFrames[0] != null)
            {
                idleFrames = setup.idleFrames;
                idleSprite = setup.idleFrames[0];
            }
            else if (fallbackData != null && fallbackData.battleSprite != null)
            {
                idleSprite = fallbackData.battleSprite;
                idleFrames = new Sprite[] { fallbackData.battleSprite };
            }

            idleFps = (setup != null && setup.idleFps > 0.1f) ? setup.idleFps : 6f;
            hurtSprite = (setup != null && setup.hurtSprite != null)
                ? setup.hurtSprite
                : (fallbackData != null ? fallbackData.battleHurtSprite : null);
            purifyReadySprite = setup != null ? setup.purifyReadySprite : null;
            zeroHpExhaustedSprite = setup != null ? setup.zeroHpExhaustedSprite : null;

            if (idleSprite != null)
            {
                sr.sprite = idleSprite;
            }

            float scale = (setup != null && setup.scale > 0.05f) ? setup.scale : 4.35f;
            transform.localScale = new Vector3(scale, scale, 1.0f);
            sr.flipX = setup != null && setup.flipX;
            sr.sortingOrder = 50 + (slotIndex * 4) + (setup != null ? setup.sortingOrderOffset : 0);

            if (setup != null)
            {
                _isBobbing = setup.enableProceduralBob;
                bobFrequency = setup.bobFrequency > 0.05f ? setup.bobFrequency : 2.5f;
                bobAmplitude = setup.bobAmplitude >= 0f ? setup.bobAmplitude : 0.12f;
            }
        }

        public void UpdateStatusPosture(bool isAtZeroHp, bool isPurifyReady)
        {
            _isZeroHpState = isAtZeroHp;
            _isPurifyReadyState = isPurifyReady;
            if (isAtZeroHp) _isBobbing = false;

            if (_hurtCoroutine == null)
            {
                RefreshDisplayedIdleSprite();
            }
        }

        private void RefreshDisplayedIdleSprite()
        {
            var sr = EnsureRenderer();
            if (_isZeroHpState && zeroHpExhaustedSprite != null)
            {
                sr.sprite = zeroHpExhaustedSprite;
                return;
            }
            if (_isPurifyReadyState && purifyReadySprite != null)
            {
                sr.sprite = purifyReadySprite;
                return;
            }
            if (idleFrames != null && idleFrames.Length > 1)
            {
                int frame = Mathf.FloorToInt((Time.time + _bobPhaseOffset) * Mathf.Max(1f, idleFps)) % idleFrames.Length;
                if (idleFrames[frame] != null) sr.sprite = idleFrames[frame];
            }
            else if (idleSprite != null)
            {
                sr.sprite = idleSprite;
            }
        }

        private void Update()
        {
            if (_hurtCoroutine == null)
            {
                RefreshDisplayedIdleSprite();
                if (_isBobbing)
                {
                    float dy = Mathf.Sin((Time.time * bobFrequency) + _bobPhaseOffset) * bobAmplitude;
                    transform.position = _basePosition + new Vector3(0f, dy, 0f);
                }
            }
        }

        public void SetBasePosition(Vector3 newBase)
        {
            _basePosition = newBase;
            transform.position = newBase;
        }

        public void SetDimmed(bool dimmed, float dimAmount = 0.45f)
        {
            var sr = EnsureRenderer();
            if (sr != null)
            {
                sr.color = dimmed ? new Color(dimAmount, dimAmount, dimAmount, 1f) : Color.white;
            }
        }

        public void PlayHurt(float duration = 0.35f, Action onComplete = null)
        {
            PlayHurtShake(duration, onComplete);
        }

        public void PlayHurtShake(float duration = 0.35f, Action onComplete = null)
        {
            if (_hurtCoroutine != null) StopCoroutine(_hurtCoroutine);
            _hurtCoroutine = StartCoroutine(HurtShakeRoutine(duration, onComplete));
        }

        private IEnumerator HurtShakeRoutine(float duration, Action onComplete)
        {
            float elapsed = 0f;
            var sr = EnsureRenderer();
            Color originalColor = sr.color;

            if (hurtSprite != null)
            {
                sr.sprite = hurtSprite;
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float shake = Mathf.Sin(elapsed * 45f) * 0.22f * (1f - progress);
                transform.position = _basePosition + new Vector3(shake, 0f, 0f);

                sr.color = Mathf.Sin(elapsed * 30f) > 0f ? new Color(1f, 0.4f, 0.4f, 1f) : Color.white;
                yield return null;
            }

            transform.position = _basePosition;
            sr.color = originalColor;
            RefreshDisplayedIdleSprite();

            _hurtCoroutine = null;
            onComplete?.Invoke();
        }

        public void PlayPurifyShimmer(float duration = 1.8f, Action onComplete = null)
        {
            PlayPurifyWhiteLightExplosion(duration, onComplete);
        }

        public void PlayPurifyWhiteLightExplosion(float duration = 1.8f, Action onComplete = null)
        {
            if (_hurtCoroutine != null) StopCoroutine(_hurtCoroutine);
            _hurtCoroutine = StartCoroutine(PurifyWhiteLightExplosionRoutine(duration, onComplete));
        }

        private IEnumerator PurifyWhiteLightExplosionRoutine(float duration, Action onComplete)
        {
            float elapsed = 0f;
            var sr = EnsureRenderer();
            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;

            Vector3 torsoCenter = sr.bounds.center;
            if (sr.bounds.size.y < 0.1f)
            {
                torsoCenter = startPos + new Vector3(0f, 2.5f, 0f);
            }

            float flareDuration = 0.70f;
            while (elapsed < flareDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / flareDuration;

                transform.position = startPos + new Vector3(0f, progress * 0.45f, 0f);

                float shimmer = Mathf.Sin(elapsed * 25f) * 0.2f;
                Color glowCol = Color.Lerp(Color.white, new Color(0.75f, 1f, 1f, 1f), shimmer + 0.5f);
                sr.color = Color.Lerp(glowCol, Color.white, progress);

                yield return null;
            }

            float dissolveDuration = 0.35f;
            float dissolveElapsed = 0f;
            while (dissolveElapsed < dissolveDuration)
            {
                dissolveElapsed += Time.deltaTime;
                elapsed += Time.deltaTime;
                float dissolveProg = Mathf.Clamp01(dissolveElapsed / dissolveDuration);

                transform.localScale = startScale * Mathf.Lerp(1.0f, 1.25f, dissolveProg);
                sr.color = new Color(1f, 1f, 1f, 1f - dissolveProg);

                yield return null;
            }

            sr.enabled = false;
            transform.position = startPos;
            transform.localScale = startScale;

            float burstDuration = Mathf.Max(0.75f, duration - elapsed);
            yield return StartCoroutine(SpawnWhiteLightExplosionRoutine(torsoCenter, burstDuration));

            _hurtCoroutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator SpawnWhiteLightExplosionRoutine(Vector3 center, float duration)
        {
            int particleCount = 48;
            GameObject container = new GameObject("WhiteLightExplosion");
            container.transform.position = center;

            Texture2D tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    float dist = Mathf.Abs(x - 3.5f) + Mathf.Abs(y - 3.5f);
                    tex.SetPixel(x, y, dist <= 3.2f ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 16f);
            Material mat = BattleDodgeBox.GetUnlitMaterial(Color.white);

            Color[] palette = new Color[]
            {
                Color.white,
                new Color(1f, 1f, 1f, 1f),
                new Color(0.75f, 1f, 1f, 1f),
                new Color(1f, 0.95f, 0.65f, 1f),
                new Color(0.85f, 0.95f, 1f, 1f)
            };

            var particles = new Transform[particleCount];
            var renderers = new SpriteRenderer[particleCount];
            var velocities = new Vector3[particleCount];
            var baseColors = new Color[particleCount];
            var lifespans = new float[particleCount];
            var initialScales = new float[particleCount];

            for (int i = 0; i < particleCount; i++)
            {
                GameObject obj = new GameObject($"Light_{i}");
                obj.transform.SetParent(container.transform);
                obj.transform.position = center + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.2f);

                float initialScale = UnityEngine.Random.Range(0.35f, 0.85f);
                obj.transform.localScale = new Vector3(initialScale, initialScale, 1f);
                initialScales[i] = initialScale;

                var r = obj.AddComponent<SpriteRenderer>();
                r.sprite = sprite;
                r.material = mat;
                Color col = palette[UnityEngine.Random.Range(0, palette.Length)];
                r.color = col;
                r.sortingOrder = 75;

                particles[i] = obj.transform;
                renderers[i] = r;
                baseColors[i] = col;
                lifespans[i] = UnityEngine.Random.Range(duration * 0.65f, duration);

                float angle = (i * (360f / particleCount)) + UnityEngine.Random.Range(-12f, 12f);
                float rad = angle * Mathf.Deg2Rad;
                float speed = UnityEngine.Random.Range(2.8f, 7.5f);
                velocities[i] = new Vector3(Mathf.Cos(rad) * speed, Mathf.Sin(rad) * speed + 0.8f, 0f);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float dt = Time.deltaTime;
                elapsed += dt;

                for (int i = 0; i < particleCount; i++)
                {
                    if (particles[i] == null) continue;

                    float prog = Mathf.Clamp01(elapsed / lifespans[i]);
                    if (prog >= 1f)
                    {
                        Destroy(particles[i].gameObject);
                        particles[i] = null;
                        continue;
                    }

                    particles[i].position += (velocities[i] + new Vector3(0f, 0.45f, 0f)) * dt;
                    velocities[i] *= Mathf.Pow(0.18f, dt);
                    particles[i].position += (Vector3)(UnityEngine.Random.insideUnitCircle * (0.015f * (1f - prog)));

                    float curScale = initialScales[i] * (1f - Mathf.Pow(prog, 1.5f));
                    particles[i].localScale = new Vector3(curScale, curScale, 1f);

                    Color c = baseColors[i];
                    c.a = 1f - prog;
                    renderers[i].color = c;
                }

                yield return null;
            }

            if (container != null)
            {
                Destroy(container);
            }
        }

        public void PlayPurgeImplosion(float duration = 1.6f, Action onComplete = null)
        {
            if (_hurtCoroutine != null) StopCoroutine(_hurtCoroutine);
            _hurtCoroutine = StartCoroutine(PurgeImplosionRoutine(duration, onComplete));
        }

        private IEnumerator PurgeImplosionRoutine(float duration, Action onComplete)
        {
            float elapsed = 0f;
            var sr = EnsureRenderer();
            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;

            Vector3 torsoCenter = sr.bounds.center;
            if (sr.bounds.size.y < 0.1f)
            {
                torsoCenter = startPos + new Vector3(0f, 2.5f, 0f);
            }

            float spasmDuration = 0.40f;
            while (elapsed < spasmDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / spasmDuration;

                float shakeX = Mathf.Sin(elapsed * 75f) * 0.35f * (1f - progress * 0.3f);
                float shakeY = UnityEngine.Random.Range(-0.08f, 0.08f);
                transform.position = startPos + new Vector3(shakeX, shakeY, 0f);

                int strobe = Mathf.FloorToInt(elapsed * 35f) % 3;
                if (strobe == 0) sr.color = new Color(1f, 0.15f, 0.15f, 1f);
                else if (strobe == 1) sr.color = new Color(0.15f, 1f, 1f, 1f);
                else sr.color = Color.white;

                yield return null;
            }

            float collapseDuration = 0.35f;
            float collapseElapsed = 0f;
            while (collapseElapsed < collapseDuration)
            {
                collapseElapsed += Time.deltaTime;
                elapsed += Time.deltaTime;
                float collapseProg = Mathf.Clamp01(collapseElapsed / collapseDuration);

                float scaleFactor = Mathf.Max(0f, 1f - Mathf.Pow(collapseProg, 1.6f));
                transform.localScale = startScale * scaleFactor;
                transform.position = Vector3.Lerp(startPos, torsoCenter, 1f - scaleFactor);
                sr.color = Color.Lerp(new Color(1f, 0.3f, 0.2f, 1f), Color.white, collapseProg);

                yield return null;
            }

            sr.enabled = false;
            transform.position = startPos;
            transform.localScale = startScale;

            float sparkDuration = Mathf.Max(0.6f, duration - elapsed);
            yield return StartCoroutine(SpawnImplosionSparksRoutine(torsoCenter, sparkDuration));

            _hurtCoroutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator SpawnImplosionSparksRoutine(Vector3 center, float duration)
        {
            int sparkCount = 38;
            GameObject sparkContainer = new GameObject("ImplosionSparks");
            sparkContainer.transform.position = center;

            Texture2D sparkTex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            sparkTex.filterMode = FilterMode.Point;
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    float dist = Mathf.Abs(x - 3.5f) + Mathf.Abs(y - 3.5f);
                    sparkTex.SetPixel(x, y, dist <= 3.2f ? Color.white : Color.clear);
                }
            }
            sparkTex.Apply();
            Sprite sparkSprite = Sprite.Create(sparkTex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 16f);
            Material sparkMat = BattleDodgeBox.GetUnlitMaterial(Color.white);

            Color[] palette = new Color[]
            {
                Color.white,
                new Color(1f, 0.45f, 0.05f, 1f),
                new Color(1f, 0.95f, 0.20f, 1f),
                new Color(0.15f, 1f, 1f, 1f),
                new Color(1f, 0.15f, 0.55f, 1f)
            };

            var sparkObjects = new Transform[sparkCount];
            var sparkRenderers = new SpriteRenderer[sparkCount];
            var sparkVelocities = new Vector3[sparkCount];
            var sparkBaseColors = new Color[sparkCount];
            var sparkLifespans = new float[sparkCount];
            var sparkInitialScales = new float[sparkCount];

            for (int i = 0; i < sparkCount; i++)
            {
                GameObject sparkObj = new GameObject($"Spark_{i}");
                sparkObj.transform.SetParent(sparkContainer.transform);
                sparkObj.transform.position = center + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.15f);

                float initialScale = UnityEngine.Random.Range(0.25f, 0.65f);
                sparkObj.transform.localScale = new Vector3(initialScale, initialScale, 1f);
                sparkInitialScales[i] = initialScale;

                var sr = sparkObj.AddComponent<SpriteRenderer>();
                sr.sprite = sparkSprite;
                sr.material = sparkMat;
                Color col = palette[UnityEngine.Random.Range(0, palette.Length)];
                sr.color = col;
                sr.sortingOrder = 70;

                sparkObjects[i] = sparkObj.transform;
                sparkRenderers[i] = sr;
                sparkBaseColors[i] = col;
                sparkLifespans[i] = UnityEngine.Random.Range(duration * 0.6f, duration);

                float angle = (i * (360f / sparkCount)) + UnityEngine.Random.Range(-15f, 15f);
                float rad = angle * Mathf.Deg2Rad;
                float speed = UnityEngine.Random.Range(3.2f, 7.8f);
                sparkVelocities[i] = new Vector3(Mathf.Cos(rad) * speed, Mathf.Sin(rad) * speed, 0f);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float dt = Time.deltaTime;
                elapsed += dt;

                for (int i = 0; i < sparkCount; i++)
                {
                    if (sparkObjects[i] == null) continue;

                    float sparkProg = Mathf.Clamp01(elapsed / sparkLifespans[i]);
                    if (sparkProg >= 1f)
                    {
                        Destroy(sparkObjects[i].gameObject);
                        sparkObjects[i] = null;
                        continue;
                    }

                    sparkObjects[i].position += sparkVelocities[i] * dt;
                    sparkVelocities[i] *= Mathf.Pow(0.12f, dt);

                    float jitterMag = 0.035f * (1f - sparkProg);
                    sparkObjects[i].position += (Vector3)(UnityEngine.Random.insideUnitCircle * jitterMag);

                    float currentScale = sparkInitialScales[i] * (1f - sparkProg);
                    sparkObjects[i].localScale = new Vector3(currentScale, currentScale, 1f);

                    float alpha = 1f - sparkProg;
                    if (sparkProg > 0.45f && UnityEngine.Random.value > 0.40f)
                    {
                        alpha = 0f;
                    }

                    Color c = sparkBaseColors[i];
                    c.a = alpha;
                    sparkRenderers[i].color = c;
                }

                yield return null;
            }

            if (sparkContainer != null)
            {
                Destroy(sparkContainer);
            }
        }
    }
}
