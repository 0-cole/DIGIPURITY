using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DigiPurity.Combat
{
    public class BattleDodgeBox : MonoBehaviour
    {
        [Header("Arena Settings")]
        [SerializeField] private Vector2 boxCenter = new Vector2(0f, 0.5f);
        [SerializeField] private Vector2 boxSize = new Vector2(3.5f, 3.5f);
        [SerializeField] private Color borderColor = new Color(0.17f, 1f, 0.17f, 1f); // Neon green #2bff2b
        [SerializeField] private float borderWidth = 0.08f;

        [Header("Soul & Bullets")]
        [SerializeField] private SoulMovementBehaviour soul;
        [SerializeField] private Sprite bulletSprite;
        [SerializeField] private Sprite soulSprite;

        private LineRenderer _borderLine;
        private Coroutine _patternCoroutine;
        private readonly List<GameObject> _activeBullets = new List<GameObject>();
        private Action _onGraze;
        private Action<int> _onPlayerHit;

        private BulletPatternType _activePattern = BulletPatternType.RainFromTop;
        private Sprite _activeBulletSprite;
        private Color _activeBulletColor = Color.white;
        private float _activeBulletSpeed = 3.2f;

        public bool IsActive { get; private set; }

        private void Awake()
        {
            SetupBorder();
            AutoLoadSprites();
            gameObject.SetActive(false);
        }

        public void AutoLoadSprites()
        {
#if UNITY_EDITOR
            if (bulletSprite == null)
            {
                bulletSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Combat/WhiteBullet_16.png");
            }
            if (soulSprite == null)
            {
                soulSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Combat/CyanSoul.png");
            }
#endif
            _activeBulletSprite = bulletSprite;
        }

        private void SetupBorder()
        {
            _borderLine = GetComponent<LineRenderer>();
            if (_borderLine == null)
            {
                _borderLine = gameObject.AddComponent<LineRenderer>();
            }

            _borderLine.useWorldSpace = true;
            _borderLine.loop = true;
            _borderLine.positionCount = 4;
            _borderLine.startWidth = borderWidth;
            _borderLine.endWidth = borderWidth;

            Material lineMat = new Material(Shader.Find("Sprites/Default"));
            lineMat.color = borderColor;
            _borderLine.material = lineMat;
            _borderLine.startColor = borderColor;
            _borderLine.endColor = borderColor;
            _borderLine.sortingOrder = 50;

            UpdateBorderPositions(1f);
        }

        private void UpdateBorderPositions(float scale)
        {
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

        public void StartDodgePhase(float duration, BulletPatternType pattern, Sprite customBullet, Color bulletCol, float speed, Action onGraze, Action<int> onPlayerHit, Action onPhaseEnd)
        {
            gameObject.SetActive(true);
            IsActive = true;
            _activePattern = pattern;
            _activeBulletSprite = customBullet != null ? customBullet : bulletSprite;
            _activeBulletColor = bulletCol;
            _activeBulletSpeed = speed > 0f ? speed : 3.2f;

            _onGraze = onGraze;
            _onPlayerHit = onPlayerHit;

            EnsureSoul();

            if (_patternCoroutine != null) StopCoroutine(_patternCoroutine);
            _patternCoroutine = StartCoroutine(DodgePhaseRoutine(duration, onPhaseEnd));
        }

        private void EnsureSoul()
        {
            if (soul == null)
            {
                GameObject soulObj = new GameObject("CyanSoul");
                soulObj.transform.SetParent(transform);
                soul = soulObj.AddComponent<SoulMovementBehaviour>();

                var sr = soulObj.AddComponent<SpriteRenderer>();
                sr.sprite = soulSprite;
                sr.sortingOrder = 100;
                soulObj.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            }

            soul.gameObject.SetActive(true);
            soul.transform.position = new Vector3(boxCenter.x, boxCenter.y, 0f);
        }

        private IEnumerator DodgePhaseRoutine(float duration, Action onPhaseEnd)
        {
            // 1. Expand Box smoothly
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                float s = Mathf.SmoothStep(0f, 1f, t / 0.2f);
                UpdateBorderPositions(s);
                yield return null;
            }
            UpdateBorderPositions(1f);

            // 2. Spawn bullet wave patterns over duration
            float elapsed = 0f;
            float spawnTimer = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                spawnTimer += Time.deltaTime;

                if (spawnTimer >= 0.22f)
                {
                    spawnTimer = 0f;
                    SpawnPatternStep(_activePattern, elapsed);
                }

                yield return null;
            }

            // 3. Clear active bullets
            ClearAllBullets();

            // 4. Shrink Box
            t = 0f;
            while (t < 0.15f)
            {
                t += Time.deltaTime;
                float s = Mathf.SmoothStep(1f, 0f, t / 0.15f);
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
                    // Rain from top
                    float spawnX = boxCenter.x + UnityEngine.Random.Range(-boxSize.x * 0.45f, boxSize.x * 0.45f);
                    Vector2 pos = new Vector2(spawnX, boxCenter.y + (boxSize.y * 0.5f) + 0.3f);
                    Vector2 vel = new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f), -_activeBulletSpeed);
                    CreateBullet(pos, vel);
                    break;

                case BulletPatternType.AimedSpreadBurst:
                    // Radial burst aimed at soul with spread
                    Vector2 centerTop = new Vector2(boxCenter.x, boxCenter.y + (boxSize.y * 0.5f) + 0.2f);
                    Vector2 toSoul = soul != null ? ((Vector2)soul.transform.position - centerTop).normalized : Vector2.down;
                    for (int i = -1; i <= 1; i++)
                    {
                        float angle = i * 18f * Mathf.Deg2Rad;
                        Vector2 dir = new Vector2(
                            toSoul.x * Mathf.Cos(angle) - toSoul.y * Mathf.Sin(angle),
                            toSoul.x * Mathf.Sin(angle) + toSoul.y * Mathf.Cos(angle)
                        );
                        CreateBullet(centerTop, dir * _activeBulletSpeed);
                    }
                    break;

                case BulletPatternType.AlternatingSide:
                    // Alternating side shots
                    bool fromLeft = (Mathf.FloorToInt(elapsed * 4f) % 2) == 0;
                    float yPos = boxCenter.y + UnityEngine.Random.Range(-boxSize.y * 0.35f, boxSize.y * 0.35f);
                    Vector2 sidePos = new Vector2(fromLeft ? boxCenter.x - (boxSize.x * 0.5f) - 0.2f : boxCenter.x + (boxSize.x * 0.5f) + 0.2f, yPos);
                    Vector2 sideVel = new Vector2(fromLeft ? _activeBulletSpeed : -_activeBulletSpeed, UnityEngine.Random.Range(-0.4f, 0.4f));
                    CreateBullet(sidePos, sideVel);
                    break;

                case BulletPatternType.RadialSpiral:
                    // Spiral pattern from center top
                    float spiralAngle = elapsed * 180f * Mathf.Deg2Rad;
                    Vector2 spiralDir = new Vector2(Mathf.Cos(spiralAngle), Mathf.Sin(spiralAngle));
                    CreateBullet(new Vector2(boxCenter.x, boxCenter.y + 1.2f), spiralDir * (_activeBulletSpeed * 0.85f));
                    break;
            }
        }

        private void CreateBullet(Vector2 spawnPos, Vector2 velocity)
        {
            GameObject bObj = new GameObject("BattleBullet");
            bObj.transform.position = spawnPos;
            bObj.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

            var sr = bObj.AddComponent<SpriteRenderer>();
            sr.sprite = _activeBulletSprite != null ? _activeBulletSprite : bulletSprite;
            sr.color = _activeBulletColor;
            sr.sortingOrder = 75;

            var bullet = bObj.AddComponent<BattleBullet>();
            bullet.Initialize(soul, velocity, dmg: 8, onGraze: _onGraze, onHit: _onPlayerHit);

            _activeBullets.Add(bObj);
        }

        private void ClearAllBullets()
        {
            for (int i = _activeBullets.Count - 1; i >= 0; i--)
            {
                if (_activeBullets[i] != null)
                {
                    Destroy(_activeBullets[i]);
                }
            }
            _activeBullets.Clear();
        }
    }
}
