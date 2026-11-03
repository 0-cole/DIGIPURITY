using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DigiPurity.Combat
{
    public struct ActiveAttackWaveContext
    {
        public EnemyAttackSlot slot;
        public float runtimeDamageMultiplier;
        public float runtimeSpeedMultiplier;
    }

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
        private readonly List<GameObject> _spawnedCustomPrefabs = new List<GameObject>();
        private Action<int> _onPlayerHit;
        private Action<int, HazardMovementRule> _onRuleContact;

        public Vector2 BoxCenter => boxCenter;
        public Vector2 BoxSize => boxSize;
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
            boxSize = new Vector2(Mathf.Max(1.0f, size.x), Mathf.Max(1.0f, size.y));
            UpdateBorderPositions(1f);
            if (soul != null)
            {
                soul.transform.position = new Vector3(boxCenter.x, boxCenter.y, 0f);
                RefreshSoulBounds();
            }
        }

        private void RefreshSoulBounds()
        {
            if (soul == null) return;
            float halfW = Mathf.Max(0.25f, (boxSize.x * 0.5f) - 0.18f);
            float halfH = Mathf.Max(0.25f, (boxSize.y * 0.5f) - 0.18f);
            soul.SetBounds(
                new Vector2(boxCenter.x - halfW, boxCenter.y - halfH),
                new Vector2(boxCenter.x + halfW, boxCenter.y + halfH),
                margin: 0.05f
            );
        }

        private void UpdateBorderPositions(float scale)
        {
            if (_borderLine == null) SetupBorder();
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
            var singleWave = new ActiveAttackWaveContext
            {
                slot = new EnemyAttackSlot
                {
                    presetType = EnemyAttackSlot.ConvertLegacyPattern(pattern),
                    duration = duration,
                    spawnInterval = 0.28f,
                    projectileSpeed = speed > 0f ? speed : 3.0f,
                    coreDamage = 10,
                    outerSplashDamage = 5
                },
                runtimeDamageMultiplier = 1f,
                runtimeSpeedMultiplier = 1f
            };

            StartModularDodgePhase(
                new List<ActiveAttackWaveContext> { singleWave },
                (val, rule) => { if (rule != HazardMovementRule.GreenHealOrb && rule != HazardMovementRule.GoldPurifyOrb) onPlayerHit?.Invoke(val); },
                onPhaseEnd);
        }

        /// <summary>
        /// Runs 1..N simultaneous modular enemy attack waves (Golfer baseline presets, Combined attacks, and Custom Attack Prefabs!).
        /// </summary>
        public void StartModularDodgePhase(
            List<ActiveAttackWaveContext> waves,
            Action<int, HazardMovementRule> onRuleContact,
            Action onPhaseEnd)
        {
            gameObject.SetActive(true);
            IsActive = true;
            _onRuleContact = onRuleContact;
            _onPlayerHit = (dmg) => _onRuleContact?.Invoke(dmg, HazardMovementRule.StandardWhite);

            EnsureSoul();

            if (_patternCoroutine != null) StopCoroutine(_patternCoroutine);
            _patternCoroutine = StartCoroutine(ModularDodgePhaseRoutine(waves, onPhaseEnd));
        }

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
            float t = 0f;
            Vector3 soulInitialScale = soul != null ? soul.transform.localScale : new Vector3(0.22f, 0.22f, 1f);
            while (t < 0.20f)
            {
                t += Time.deltaTime;
                float s = Mathf.SmoothStep(0f, 1f, t / 0.20f);
                UpdateBorderPositions(s);
                if (soul != null) soul.transform.localScale = soulInitialScale * s;
                yield return null;
            }
            UpdateBorderPositions(1f);
            if (soul != null) soul.transform.localScale = soulInitialScale;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            t = 0f;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                float s = Mathf.SmoothStep(1f, 0f, t / 0.18f);
                UpdateBorderPositions(s);
                if (soul != null) soul.transform.localScale = soulInitialScale * s;
                yield return null;
            }

            if (soul != null)
            {
                soul.transform.localScale = soulInitialScale;
                soul.gameObject.SetActive(false);
            }
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
            soul.transform.localScale = new Vector3(0.22f, 0.22f, 1f);

            var sr = soul.GetComponent<SpriteRenderer>();
            if (sr == null) sr = soul.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateSoulSprite(soulSprite);
            sr.material = GetUnlitMaterial(Color.white);
            sr.color = Color.white;
            sr.sortingOrder = 400;

            RefreshSoulBounds();
            soul.gameObject.SetActive(true);
        }

        private IEnumerator ModularDodgePhaseRoutine(List<ActiveAttackWaveContext> waves, Action onPhaseEnd)
        {
            if (waves == null || waves.Count == 0)
            {
                yield return StartCoroutine(HelplessDodgePhaseRoutine(1.5f, onPhaseEnd));
                yield break;
            }

            // 1. Expand Box smoothly
            float t = 0f;
            Vector3 soulInitialScale = soul != null ? soul.transform.localScale : new Vector3(0.22f, 0.22f, 1f);
            while (t < 0.20f)
            {
                t += Time.deltaTime;
                float s = Mathf.SmoothStep(0f, 1f, t / 0.20f);
                UpdateBorderPositions(s);
                if (soul != null) soul.transform.localScale = soulInitialScale * s;
                yield return null;
            }
            UpdateBorderPositions(1f);
            if (soul != null) soul.transform.localScale = soulInitialScale;

            // 2. Reaction grace buffer (0.85s) so player can move soul before bullets/hazards activate
            float graceElapsed = 0f;
            while (graceElapsed < 0.85f)
            {
                graceElapsed += Time.deltaTime;
                yield return null;
            }

            // 3. Instantiate any Custom Attack Wave Prefabs anchored at Dodge Box center & bind AttackDamageZones!
            float maxDuration = 1.5f;
            float[] waveTimers = new float[waves.Count];
            int[] waveStepCounts = new int[waves.Count];

            for (int i = 0; i < waves.Count; i++)
            {
                var w = waves[i];
                if (w.slot == null) continue;
                if (w.slot.duration > maxDuration) maxDuration = w.slot.duration;

                // Stagger initial spawn timers slightly in multi-enemy battles so emitters weave fairly
                waveTimers[i] = (i == 0) ? w.slot.spawnInterval : (w.slot.spawnInterval * 0.45f * i);

                if (w.slot.customAttackPrefab != null)
                {
                    GameObject prefabInstance = Instantiate(
                        w.slot.customAttackPrefab,
                        new Vector3(boxCenter.x, boxCenter.y, 0f),
                        Quaternion.identity);
                    _spawnedCustomPrefabs.Add(prefabInstance);

                    float dmgMult = w.runtimeDamageMultiplier > 0.01f ? w.runtimeDamageMultiplier : 1f;
                    var zones = prefabInstance.GetComponentsInChildren<AttackDamageZone>(includeInactive: true);
                    for (int z = 0; z < zones.Length; z++)
                    {
                        var zone = zones[z];
                        if (zone.damage > 0 && Mathf.Abs(dmgMult - 1f) > 0.01f)
                        {
                            zone.damage = Mathf.Max(1, Mathf.RoundToInt(zone.damage * dmgMult));
                        }
                        zone.BindRuntimeContext(soul, this, _onRuleContact);
                    }
                }
            }

            // 4. Step built-in Golfer / modular preset emitters across wave duration
            float elapsed = 0f;
            while (elapsed < maxDuration)
            {
                float dt = Time.deltaTime;
                elapsed += dt;

                for (int i = 0; i < waves.Count; i++)
                {
                    var w = waves[i];
                    if (w.slot == null || elapsed > w.slot.duration) continue;
                    if (w.slot.presetType == AttackPresetType.CustomAttackPrefab) continue;

                    float interval = Mathf.Max(0.08f, w.slot.spawnInterval);
                    // Slightly scale interval when 2+ enemies fire simultaneously so multi-enemy waves stay skill-based & fair
                    if (waves.Count > 1) interval *= (1f + 0.25f * (waves.Count - 1));

                    waveTimers[i] += dt;
                    if (waveTimers[i] >= interval)
                    {
                        waveTimers[i] = 0f;
                        waveStepCounts[i]++;
                        SpawnModularPresetStep(w, elapsed, waveStepCounts[i]);
                    }
                }

                yield return null;
            }

            // 5. Clear all bullets and custom prefabs
            ClearAllBullets();

            // 6. Shrink Box
            t = 0f;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                float s = Mathf.SmoothStep(1f, 0f, t / 0.18f);
                UpdateBorderPositions(s);
                if (soul != null) soul.transform.localScale = soulInitialScale * s;
                yield return null;
            }

            if (soul != null)
            {
                soul.transform.localScale = soulInitialScale;
                soul.gameObject.SetActive(false);
            }
            gameObject.SetActive(false);
            IsActive = false;
            _patternCoroutine = null;

            onPhaseEnd?.Invoke();
        }

        private void SpawnModularPresetStep(ActiveAttackWaveContext ctx, float elapsed, int stepIndex)
        {
            var slot = ctx.slot;
            float spd = Mathf.Max(0.5f, slot.projectileSpeed * (ctx.runtimeSpeedMultiplier > 0.05f ? ctx.runtimeSpeedMultiplier : 1f));
            float dmgMult = ctx.runtimeDamageMultiplier > 0.01f ? ctx.runtimeDamageMultiplier : 1f;
            int coreDmg = Mathf.Max(1, Mathf.RoundToInt(slot.coreDamage * dmgMult));
            int splashDmg = Mathf.Max(1, Mathf.RoundToInt(slot.outerSplashDamage * dmgMult));
            Color baseTint = ResolveRuleTint(slot.projectileColor, slot.movementRule);

            switch (slot.presetType)
            {
                case AttackPresetType.Golfer_GolfBallRain:
                {
                    int count = Mathf.Clamp(slot.projectilesPerStep, 1, 4);
                    for (int k = 0; k < count; k++)
                    {
                        float spawnX = boxCenter.x + UnityEngine.Random.Range(-boxSize.x * 0.44f, boxSize.x * 0.44f);
                        Vector2 pos = new Vector2(spawnX, boxCenter.y + (boxSize.y * 0.5f) + 0.35f + (k * 0.18f));
                        Vector2 vel = new Vector2(UnityEngine.Random.Range(-0.32f, 0.32f), -spd);
                        CreateModularBullet(pos, vel, coreDmg, slot.projectileSprite, baseTint, slot.projectileScale, slot.movementRule, bounceInBox: false);
                    }
                    break;
                }

                case AttackPresetType.Golfer_SandTrapSpread:
                {
                    // Center core golf ball (heavy damage) + flanking sand-spray pellets (lighter splash damage!)
                    Vector2 centerTop = new Vector2(boxCenter.x, boxCenter.y + (boxSize.y * 0.5f) + 0.25f);
                    Vector2 toSoul = soul != null ? ((Vector2)soul.transform.position - centerTop).normalized : Vector2.down;
                    int fanCount = Mathf.Max(3, slot.projectilesPerStep | 1); // Odd count so center ball is the heavy sweet spot
                    int half = fanCount / 2;

                    for (int i = -half; i <= half; i++)
                    {
                        bool isCoreBall = (i == 0);
                        float angle = i * 16f * Mathf.Deg2Rad;
                        Vector2 dir = new Vector2(
                            toSoul.x * Mathf.Cos(angle) - toSoul.y * Mathf.Sin(angle),
                            toSoul.x * Mathf.Sin(angle) + toSoul.y * Mathf.Cos(angle)
                        );

                        int hitDmg = isCoreBall ? coreDmg : splashDmg;
                        float bulletScale = isCoreBall ? slot.projectileScale * 1.12f : slot.projectileScale * 0.72f;
                        Color col = isCoreBall ? baseTint : new Color(1f, 0.86f, 0.52f, 0.95f); // Sandy gold outer spray
                        float speedVar = isCoreBall ? spd : spd * 0.90f;

                        CreateModularBullet(centerTop, dir * speedVar, hitDmg, slot.projectileSprite, col, bulletScale, slot.movementRule, bounceInBox: false);
                    }
                    break;
                }

                case AttackPresetType.Golfer_BouncingDrive:
                {
                    // Driven golf ball entering from top corner and ricocheting off the inside walls of the Dodge Box!
                    bool fromLeft = (stepIndex % 2) == 0;
                    Vector2 spawnPos = new Vector2(
                        fromLeft ? (boxCenter.x - boxSize.x * 0.38f) : (boxCenter.x + boxSize.x * 0.38f),
                        boxCenter.y + (boxSize.y * 0.42f));
                    float angleDeg = fromLeft ? UnityEngine.Random.Range(-58f, -34f) : UnityEngine.Random.Range(-146f, -122f);
                    float rad = angleDeg * Mathf.Deg2Rad;
                    Vector2 vel = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * spd;
                    CreateModularBullet(spawnPos, vel, coreDmg, slot.projectileSprite, baseTint, slot.projectileScale, slot.movementRule, bounceInBox: true);
                    break;
                }

                case AttackPresetType.Golfer_CrossFairway:
                {
                    bool fromLeft = (stepIndex % 2) == 0;
                    float yPos = boxCenter.y + UnityEngine.Random.Range(-boxSize.y * 0.38f, boxSize.y * 0.38f);
                    Vector2 sidePos = new Vector2(
                        fromLeft ? boxCenter.x - (boxSize.x * 0.5f) - 0.25f : boxCenter.x + (boxSize.x * 0.5f) + 0.25f,
                        yPos);
                    Vector2 sideVel = new Vector2(fromLeft ? spd : -spd, UnityEngine.Random.Range(-0.25f, 0.25f));
                    CreateModularBullet(sidePos, sideVel, coreDmg, slot.projectileSprite, baseTint, slot.projectileScale, slot.movementRule, bounceInBox: false);
                    break;
                }

                case AttackPresetType.RadialSpiralRing:
                {
                    int spokes = Mathf.Clamp(slot.projectilesPerStep, 4, 12);
                    float baseAngle = stepIndex * 18f;
                    Vector2 ringOrigin = new Vector2(boxCenter.x, boxCenter.y + (boxSize.y * 0.5f) + 0.35f);
                    for (int s = 0; s < spokes; s++)
                    {
                        float rad = (baseAngle + (s * (360f / spokes))) * Mathf.Deg2Rad;
                        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                        if (dir.y > 0.25f) continue; // Focus downward/across into arena
                        CreateModularBullet(ringOrigin, dir * spd, coreDmg, slot.projectileSprite, baseTint, slot.projectileScale * 0.85f, slot.movementRule, bounceInBox: false);
                    }
                    break;
                }
            }
        }

        private static Color ResolveRuleTint(Color authorColor, HazardMovementRule rule)
        {
            switch (rule)
            {
                case HazardMovementRule.CyanStopToAvoid: return new Color(0.15f, 0.92f, 1.0f, 1f);
                case HazardMovementRule.OrangeMoveToAvoid: return new Color(1.0f, 0.55f, 0.12f, 1f);
                case HazardMovementRule.GreenHealOrb: return new Color(0.20f, 1.0f, 0.35f, 1f);
                case HazardMovementRule.GoldPurifyOrb: return new Color(1.0f, 0.92f, 0.20f, 1f);
                default: return authorColor.a > 0.01f ? authorColor : Color.white;
            }
        }

        private void CreateModularBullet(
            Vector2 spawnPos,
            Vector2 velocity,
            int dmg,
            Sprite customSprite,
            Color tint,
            float scale,
            HazardMovementRule rule,
            bool bounceInBox)
        {
            GameObject bObj = new GameObject("BattleBullet");
            bObj.transform.position = new Vector3(spawnPos.x, spawnPos.y, 0f);
            float s = scale > 0.05f ? scale : 0.40f;
            bObj.transform.localScale = new Vector3(s, s, 1f);

            var sr = bObj.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateBulletSprite(customSprite != null ? customSprite : bulletSprite);
            sr.material = GetUnlitMaterial(tint);
            sr.color = tint;
            sr.sortingOrder = 350;

            var bullet = bObj.AddComponent<BattleBullet>();
            float hitR = Mathf.Clamp(0.20f * (s / 0.40f), 0.10f, 0.45f);
            bullet.InitializeModular(soul, this, velocity, dmg, rule, bounceInBox, hitR, _onRuleContact);

            _activeBullets.Add(bObj);
        }

        public void ClearAllBullets()
        {
            for (int i = _activeBullets.Count - 1; i >= 0; i--)
            {
                if (_activeBullets[i] != null) Destroy(_activeBullets[i]);
            }
            _activeBullets.Clear();

            for (int i = _spawnedCustomPrefabs.Count - 1; i >= 0; i--)
            {
                if (_spawnedCustomPrefabs[i] != null) Destroy(_spawnedCustomPrefabs[i]);
            }
            _spawnedCustomPrefabs.Clear();
        }
    }
}
