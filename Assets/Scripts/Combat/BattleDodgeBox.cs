using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DigiPurity.Combat
{
    public class BattleDodgeBox : MonoBehaviour
    {
        [Header("Arena Settings")]
        [SerializeField] private Vector2 boxCenter = new Vector2(1000f, 1001.25f);
        [SerializeField] private Vector2 boxSize = new Vector2(3.6f, 2.7f);
        [SerializeField] private Color borderColor = new Color(0.15f, 1f, 0.25f, 1f); // Neon green
        [SerializeField] private float borderWidth = 0.08f;

        [Header("Soul & Bullets")]
        [SerializeField] private SoulMovementBehaviour soul;
        [SerializeField] private Sprite bulletSprite;
        [SerializeField] private Sprite soulSprite;

        private LineRenderer _borderLine;
        private Coroutine _patternCoroutine;
        private readonly List<GameObject> _activeBullets = new List<GameObject>();
        private Action<int> _onPlayerHit;
        private BulletPatternType _activePattern = BulletPatternType.RainFromTop;
        private float _activeBulletSpeed = 3.2f;

        public bool IsActive { get; private set; }

        private static Sprite _cachedSoulSprite;
        private static Sprite _cachedBulletSprite;
        private static Material _cachedUnlitMaterial;

        public static Material GetUnlitMaterial(Color tint)
        {
            if (_cachedUnlitMaterial == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (s == null) s = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
                if (s == null) s = Shader.Find("Sprites/Default");
                _cachedUnlitMaterial = new Material(s);
            }
            Material m = new Material(_cachedUnlitMaterial);
            m.color = tint;
            return m;
        }

        public static Sprite GetOrCreateSoulSprite(Sprite assigned)
        {
            if (assigned != null) return assigned;
            if (_cachedSoulSprite != null) return _cachedSoulSprite;
#if UNITY_EDITOR
            var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Combat/CyanSoul.png");
            if (loaded != null) { _cachedSoulSprite = loaded; return loaded; }
#endif
            int size = 24;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color cyan = new Color(0.0f, 0.92f, 1.0f, 1.0f);
            Color clear = Color.clear;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - 11.5f) / 10f;
                    float ny = (y - 11.5f) / 10f;
                    float val = (nx * nx) + Mathf.Pow(ny - Mathf.Sqrt(Mathf.Abs(nx)) * 0.7f, 2f);
                    tex.SetPixel(x, y, val <= 0.85f ? cyan : clear);
                }
            }
            tex.Apply();
            _cachedSoulSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
            return _cachedSoulSprite;
        }

        public static Sprite GetOrCreateBulletSprite(Sprite assigned)
        {
            if (assigned != null) return assigned;
            if (_cachedBulletSprite != null) return _cachedBulletSprite;
#if UNITY_EDITOR
            var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Combat/WhiteBullet_16.png");
            if (loaded != null) { _cachedBulletSprite = loaded; return loaded; }
#endif
            int size = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color white = Color.white;
            Color clear = Color.clear;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - 7.5f;
                    float dy = y - 7.5f;
                    tex.SetPixel(x, y, (dx * dx + dy * dy <= 42f) ? white : clear);
                }
            }
            tex.Apply();
            _cachedBulletSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
            return _cachedBulletSprite;
        }

        private void Awake()
        {
            AutoLoadSprites();
            SetupBorder();
            gameObject.SetActive(false);
        }

        public void AutoLoadSprites()
        {
#if UNITY_EDITOR
            if (bulletSprite == null)
                bulletSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Combat/WhiteBullet_16.png");
            if (soulSprite == null)
                soulSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Combat/CyanSoul.png");
#endif
        }

        private void SetupBorder()
        {
            _borderLine = GetComponent<LineRenderer>();
            if (_borderLine == null) _borderLine = gameObject.AddComponent<LineRenderer>();

            _borderLine.useWorldSpace = true;
            _borderLine.loop = true;
            _borderLine.positionCount = 4;
            _borderLine.startWidth = borderWidth;
            _borderLine.endWidth = borderWidth;

            _borderLine.material = GetUnlitMaterial(borderColor);
            _borderLine.startColor = borderColor;
            _borderLine.endColor = borderColor;
            _borderLine.sortingOrder = 300;

            UpdateBorderPositions(1f);
        }

        public void SetCenterAndSize(Vector2 center, Vector2 size)
        {
            boxCenter = center;
            boxSize = size;
            UpdateBorderPositions(1f);
            if (soul != null)
            {
                soul.transform.position = new Vector3(boxCenter.x, boxCenter.y, 0f);
                float halfW = (boxSize.x * 0.5f) - 0.18f;
                float halfH = (boxSize.y * 0.5f) - 0.18f;
                soul.SetBounds(
                    new Vector2(boxCenter.x - halfW, boxCenter.y - halfH),
                    new Vector2(boxCenter.x + halfW, boxCenter.y + halfH),
                    margin: 0.05f
                );
            }
        }

        private void UpdateBorderPositions(float scale)
        {
            if (_borderLine == null) return;
            float halfW = (boxSize.x * 0.5f) * scale;
            float halfH = (boxSize.y * 0.5f) * scale;

            Vector3 p0 = new Vector3(boxCenter.x - halfW, boxCenter.y - halfH, 0f);
            Vector3 p1 = new Vector3(boxCenter.x - halfW, boxCenter.y + halfH, 0f);
            Vector3 p2 = new Vector3(boxCenter.x + halfW, boxCenter.y + halfH, 0f);
            Vector3 p3 = new Vector3(boxCenter.x + halfW, boxCenter.y - halfH, 0f);

            _borderLine.SetPosition(0, p0);
            _borderLine.SetPosition(1, p1);
            _borderLine.SetPosition(2, p2);
            _borderLine.SetPosition(3, p3);
        }

        public void StartDodgePhase(float duration, BulletPatternType pattern, float speed, Action<int> onPlayerHit, Action onPhaseEnd)
        {
            gameObject.SetActive(true);
            IsActive = true;
            _activePattern = pattern;
            _activeBulletSpeed = speed > 0f ? speed : 3.0f;
            _onPlayerHit = onPlayerHit;

            EnsureSoul();

            if (_patternCoroutine != null) StopCoroutine(_patternCoroutine);
            _patternCoroutine = StartCoroutine(DodgePhaseRoutine(duration, onPhaseEnd));
        }

        /// <summary>
        /// When enemy has 0 HP, they are exhausted and helpless: the soul enters the arena for 1.5s
        /// with zero incoming bullets to represent their weakness, then safely exits!
        /// </summary>
        public void StartHelplessDodgePhase(float duration, Action onPhaseEnd)
        {
            gameObject.SetActive(true);
            IsActive = true;
            EnsureSoul();

            if (_patternCoroutine != null) StopCoroutine(_patternCoroutine);
            _patternCoroutine = StartCoroutine(HelplessDodgePhaseRoutine(duration, onPhaseEnd));
        }

        private IEnumerator HelplessDodgePhaseRoutine(float duration, Action onPhaseEnd)
        {
            // 1. Expand Box smoothly
            float t = 0f;
            while (t < 0.20f)
            {
                t += Time.deltaTime;
                float s = Mathf.SmoothStep(0f, 1f, t / 0.20f);
                UpdateBorderPositions(s);
                yield return null;
            }
            UpdateBorderPositions(1f);

            // 2. Calm silence: heart floats in empty box for duration (1.5s) with NO bullets
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 3. Shrink Box
            t = 0f;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                float s = Mathf.SmoothStep(1f, 0f, t / 0.18f);
                UpdateBorderPositions(s);
                yield return null;
            }

            if (soul != null) soul.gameObject.SetActive(false);
            gameObject.SetActive(false);
            IsActive = false;
            _patternCoroutine = null;

            onPhaseEnd?.Invoke();
        }

        public void EnsureSoul()
        {
            if (soul == null)
            {
                Transform t = transform.Find("CyanSoul");
                if (t != null) soul = t.GetComponent<SoulMovementBehaviour>();
            }

            if (soul == null)
            {
                GameObject soulObj = new GameObject("CyanSoul");
                soulObj.transform.SetParent(transform);
                soul = soulObj.AddComponent<SoulMovementBehaviour>();
            }

            soul.transform.position = new Vector3(boxCenter.x, boxCenter.y, 0f);
            soul.transform.localScale = new Vector3(0.22f, 0.22f, 1f); // Canonical Toby Fox Undertale/Deltarune proportion (~0.35 units)

            var sr = soul.GetComponent<SpriteRenderer>();
            if (sr == null) sr = soul.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateSoulSprite(soulSprite);
            sr.material = GetUnlitMaterial(Color.white);
            sr.color = Color.white;
            sr.sortingOrder = 400; // Above everything

            float halfW = (boxSize.x * 0.5f) - 0.18f;
            float halfH = (boxSize.y * 0.5f) - 0.18f;
            soul.SetBounds(
                new Vector2(boxCenter.x - halfW, boxCenter.y - halfH),
                new Vector2(boxCenter.x + halfW, boxCenter.y + halfH),
                margin: 0.05f
            );

            soul.gameObject.SetActive(true);
        }

        private IEnumerator DodgePhaseRoutine(float duration, Action onPhaseEnd)
        {
            // 1. Expand Box smoothly
            float t = 0f;
            while (t < 0.20f)
            {
                t += Time.deltaTime;
                float s = Mathf.SmoothStep(0f, 1f, t / 0.20f);
                UpdateBorderPositions(s);
                yield return null;
            }
            UpdateBorderPositions(1f);

            // 2. Initial reaction grace period: soul is active and controllable, player prepares for attack
            float graceDuration = 0.85f;
            float graceElapsed = 0f;
            while (graceElapsed < graceDuration)
            {
                graceElapsed += Time.deltaTime;
                yield return null;
            }

            // 3. Spawn bullets
            float elapsed = 0f;
            float spawnTimer = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                spawnTimer += Time.deltaTime;

                if (spawnTimer >= 0.28f)
                {
                    spawnTimer = 0f;
                    SpawnPatternStep(_activePattern, elapsed);
                }

                yield return null;
            }

            // 4. Clear active bullets
            ClearAllBullets();

            // 5. Shrink Box
            t = 0f;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                float s = Mathf.SmoothStep(1f, 0f, t / 0.18f);
                UpdateBorderPositions(s);
                yield return null;
            }

            if (soul != null) soul.gameObject.SetActive(false);
            gameObject.SetActive(false);
            IsActive = false;
            _patternCoroutine = null;

            onPhaseEnd?.Invoke();
        }

        private void SpawnPatternStep(BulletPatternType pattern, float elapsed)
        {
            switch (pattern)
            {
                case BulletPatternType.RainFromTop:
                    float spawnX = boxCenter.x + UnityEngine.Random.Range(-boxSize.x * 0.42f, boxSize.x * 0.42f);
                    Vector2 pos = new Vector2(spawnX, boxCenter.y + (boxSize.y * 0.5f) + 0.35f);
                    Vector2 vel = new Vector2(UnityEngine.Random.Range(-0.30f, 0.30f), -_activeBulletSpeed);
                    CreateBullet(pos, vel);
                    break;

                case BulletPatternType.AimedSpreadBurst:
                    Vector2 centerTop = new Vector2(boxCenter.x, boxCenter.y + (boxSize.y * 0.5f) + 0.25f);
                    Vector2 toSoul = soul != null ? ((Vector2)soul.transform.position - centerTop).normalized : Vector2.down;
                    for (int i = -1; i <= 1; i++)
                    {
                        float angle = i * 16f * Mathf.Deg2Rad;
                        Vector2 dir = new Vector2(
                            toSoul.x * Mathf.Cos(angle) - toSoul.y * Mathf.Sin(angle),
                            toSoul.x * Mathf.Sin(angle) + toSoul.y * Mathf.Cos(angle)
                        );
                        CreateBullet(centerTop, dir * _activeBulletSpeed);
                    }
                    break;

                case BulletPatternType.AlternatingSide:
                    bool fromLeft = (Mathf.FloorToInt(elapsed * 4f) % 2) == 0;
                    float yPos = boxCenter.y + UnityEngine.Random.Range(-boxSize.y * 0.35f, boxSize.y * 0.35f);
                    Vector2 sidePos = new Vector2(fromLeft ? boxCenter.x - (boxSize.x * 0.5f) - 0.25f : boxCenter.x + (boxSize.x * 0.5f) + 0.25f, yPos);
                    Vector2 sideVel = new Vector2(fromLeft ? _activeBulletSpeed : -_activeBulletSpeed, UnityEngine.Random.Range(-0.25f, 0.25f));
                    CreateBullet(sidePos, sideVel);
                    break;
            }
        }

        private void CreateBullet(Vector2 spawnPos, Vector2 velocity)
        {
            GameObject bObj = new GameObject("BattleBullet");
            bObj.transform.position = new Vector3(spawnPos.x, spawnPos.y, 0f);
            bObj.transform.localScale = new Vector3(0.40f, 0.40f, 1f); // Sharp, visible ~35px bullet!

            var sr = bObj.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateBulletSprite(bulletSprite);
            sr.material = GetUnlitMaterial(Color.white);
            sr.color = Color.white;
            sr.sortingOrder = 350; // Above box, below soul

            var bullet = bObj.AddComponent<BattleBullet>();
            bullet.Initialize(soul, velocity, dmg: 10, onHit: _onPlayerHit);

            _activeBullets.Add(bObj);
        }

        public void ClearAllBullets()
        {
            for (int i = _activeBullets.Count - 1; i >= 0; i--)
            {
                if (_activeBullets[i] != null) Destroy(_activeBullets[i]);
            }
            _activeBullets.Clear();
        }
    }
}
