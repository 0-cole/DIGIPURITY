using System;
using System.Collections;
using UnityEngine;
using DigiPurity.World;

namespace DigiPurity.Combat
{
    [RequireComponent(typeof(Collider2D))]
    public class OverworldEnemy : MonoBehaviour
    {
        [Header("Enemy Data Profile")]
        [Tooltip("The ScriptableObject defining this enemy's stats, sprites, dialogue, TALK actions, and bullet patterns.")]
        [SerializeField] private EnemyData enemyData;

        [Header("Agro & Movement (Deltarune Overworld Encounter Style)")]
        [Tooltip("Whether this enemy notices Owen and rushes toward him when he gets close.")]
        [SerializeField] private bool agroOnSight = true;
        [SerializeField] private float sightRadius = 4.5f;
        [Tooltip("Initial rush speed when the enemy first spots Owen.")]
        [SerializeField] private float rushSpeed = 4.0f;
        [Tooltip("Acceleration rate in units/sec² while pursuing Owen so the enemy ramps up speed.")]
        [SerializeField] private float rushAcceleration = 7.5f;
        [Tooltip("Maximum pursuit speed cap, strictly faster than Owen's diagonal sprint (10.8) so the player cannot escape.")]
        [SerializeField] private float maxRushSpeed = 13.5f;
        [SerializeField] private float stopDistance = 0.8f;

        [Header("Sprite Facing")]
        [Tooltip("If true, the sprite faces the player during pursuit. If false, spriteRenderer.flipX is left untouched.")]
        [SerializeField] private bool facePlayerOnRush = true;
        [Tooltip("True if the base sprite natively faces Left (e.g. Golfer). False if it natively faces Right.")]
        [SerializeField] private bool spriteFacesLeftByDefault = true;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite cyanSoulSprite;

        private OverworldPlayerController _playerTarget;
        private Vector3 _startPosition;
        private bool _isRushing = false;
        private float _currentRushSpeed = 0f;
        private bool _isEncounterTriggered = false;

        public EnemyData Data => enemyData;
        public float CurrentRushSpeed => _currentRushSpeed;
        public float InitialRushSpeed { get => rushSpeed; set => rushSpeed = value; }
        public float RushAcceleration { get => rushAcceleration; set => rushAcceleration = value; }
        public float MaxRushSpeed { get => maxRushSpeed; set => maxRushSpeed = value; }
        public bool IsRushing => _isRushing;
        public bool FacePlayerOnRush { get => facePlayerOnRush; set => facePlayerOnRush = value; }
        public bool SpriteFacesLeftByDefault { get => spriteFacesLeftByDefault; set => spriteFacesLeftByDefault = value; }

        private void Awake()
        {
#if UNITY_EDITOR
            if (enemyData == null)
            {
                enemyData = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/Enemies/Golfer_Data.asset");
            }
#endif
            var col = GetComponent<Collider2D>();
            if (col != null && GetComponent<DigiPurity.World.OverworldCreature>() == null)
            {
                col.isTrigger = true;
            }

            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            _startPosition = transform.position;
            AutoLoadSoul();
            ApplyOverworldSprite();
        }

        public void SetEnemyData(EnemyData data)
        {
            enemyData = data;
            ApplyOverworldSprite();
        }

        private void ApplyOverworldSprite()
        {
            if (enemyData != null && spriteRenderer != null)
            {
                if (enemyData.overworldSprite != null)
                {
                    spriteRenderer.sprite = enemyData.overworldSprite;
                }
                else if (enemyData.battleSprite != null)
                {
                    spriteRenderer.sprite = enemyData.battleSprite;
                }
            }
        }

        public void AutoLoadSoul()
        {
#if UNITY_EDITOR
            if (cyanSoulSprite == null)
            {
                cyanSoulSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Combat/CyanSoul.png");
            }
#endif
        }

        private void Start()
        {
            _playerTarget = FindAnyObjectByType<OverworldPlayerController>();
        }

        public static bool CalculateFacingFlip(Vector2 toPlayer, bool spriteFacesLeftByDefault)
        {
            return spriteFacesLeftByDefault ? (toPlayer.x > 0f) : (toPlayer.x < 0f);
        }

        public static float CalculateAcceleratedSpeed(float currentSpeed, float acceleration, float maxSpeed, float deltaTime)
        {
            return Mathf.Min(currentSpeed + acceleration * deltaTime, maxSpeed);
        }

        private void Update()
        {
            if (_isEncounterTriggered || !agroOnSight || _playerTarget == null) return;
            if (_playerTarget.InputLocked) return;

            float dist = Vector2.Distance(transform.position, _playerTarget.transform.position);

            if (!_isRushing && dist <= sightRadius)
            {
                _isRushing = true;
                _currentRushSpeed = rushSpeed;
            }

            if (_isRushing)
            {
                _currentRushSpeed = CalculateAcceleratedSpeed(_currentRushSpeed, rushAcceleration, maxRushSpeed, Time.deltaTime);

                Vector3 toPlayer = (_playerTarget.transform.position - transform.position).normalized;
                float step = _currentRushSpeed * Time.deltaTime;
                transform.position += toPlayer * step;

                if (facePlayerOnRush && spriteRenderer != null && Mathf.Abs(toPlayer.x) > 0.05f)
                {
                    spriteRenderer.flipX = CalculateFacingFlip(toPlayer, spriteFacesLeftByDefault);
                }

                if (dist <= stopDistance || dist <= step)
                {
                    TriggerEncounter(_playerTarget);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_isEncounterTriggered) return;

            var player = collision.GetComponent<OverworldPlayerController>();
            if (player != null)
            {
                TriggerEncounter(player);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_isEncounterTriggered) return;

            var player = collision.gameObject.GetComponent<OverworldPlayerController>();
            if (player != null)
            {
                TriggerEncounter(player);
            }
        }

        public void TriggerEncounter(OverworldPlayerController player)
        {
            if (_isEncounterTriggered) return;
            _isEncounterTriggered = true;
            _isRushing = false;
            _currentRushSpeed = 0f;

            StartCoroutine(EncounterTransitionRoutine(player));
        }

        private IEnumerator EncounterTransitionRoutine(OverworldPlayerController player)
        {
            player.InputLocked = true;

            Vector3 chestPos = player.transform.position + new Vector3(0f, 0.45f, 0f);
            GameObject soulObj = new GameObject("EncounterAlertSoul");
            soulObj.transform.position = chestPos;
            soulObj.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

            var sr = soulObj.AddComponent<SpriteRenderer>();
            sr.sprite = cyanSoulSprite;
            sr.sortingOrder = 3000;

            for (int i = 0; i < 3; i++)
            {
                sr.enabled = true;
                ScreenFader.Instance.SetAlpha(1.0f);
                yield return new WaitForSeconds(0.12f);

                sr.enabled = false;
                ScreenFader.Instance.SetAlpha(0.0f);
                yield return new WaitForSeconds(0.08f);
            }

            sr.enabled = true;
            ScreenFader.Instance.SetAlpha(1.0f);
            yield return new WaitForSeconds(0.15f);

            Destroy(soulObj);

            var battleStage = BattleStageController.EnsureBattleStageExists();

            ScreenFader.Instance.Transition(
                onBlack: () =>
                {
                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        var roomCam = mainCam.GetComponent<RoomCameraController>();
                        if (roomCam != null) roomCam.enabled = false;

                        Vector3 stagePos = battleStage != null ? battleStage.transform.position : new Vector3(1000f, 1000f, 0f);
                        mainCam.transform.position = new Vector3(stagePos.x, stagePos.y + 0.5f, -10f);
                        mainCam.orthographicSize = 6.0f;
                    }

                    if (battleStage != null)
                    {
                        battleStage.gameObject.SetActive(true);
                        var encounterConfig = GetComponent<BattleEncounterConfig>();
                        if (encounterConfig != null)
                        {
                            battleStage.InitializeWithEncounter(encounterConfig, player, this);
                        }
                        else
                        {
                            battleStage.InitializeWithEnemy(enemyData, player, this);
                        }
                    }

                    player.gameObject.SetActive(false);
                    gameObject.SetActive(false);
                },
                duration: 0.3f,
                onComplete: () =>
                {
                    ScreenFader.Instance.SetAlpha(0f);
                }
            );
        }
    }
}
