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

        /// <summary>
        /// Calculates whether spriteRenderer.flipX should be true or false to face toward the target.
        /// </summary>
        public static bool CalculateFacingFlip(Vector2 toPlayer, bool spriteFacesLeftByDefault)
        {
            // If sprite faces left natively (like Golfer):
            // - Player is to the left (toPlayer.x < 0) => flipX = false (faces left, toward player)
            // - Player is to the right (toPlayer.x > 0) => flipX = true (faces right, toward player)
            // If sprite faces right natively:
            // - Player is to the left (toPlayer.x < 0) => flipX = true (faces left, toward player)
            // - Player is to the right (toPlayer.x > 0) => flipX = false (faces right, toward player)
            return spriteFacesLeftByDefault ? (toPlayer.x > 0f) : (toPlayer.x < 0f);
        }

        /// <summary>
        /// Calculates accelerated rush speed capped at maxSpeed.
        /// </summary>
        public static float CalculateAcceleratedSpeed(float currentSpeed, float acceleration, float maxSpeed, float deltaTime)
        {
            return Mathf.Min(currentSpeed + acceleration * deltaTime, maxSpeed);
        }

        private void Update()
        {
            if (_isEncounterTriggered || !agroOnSight || _playerTarget == null) return;

            // Don't agro if player's inputs are already locked (e.g. during a cutscene or another transition)
            if (_playerTarget.InputLocked) return;

            float dist = Vector2.Distance(transform.position, _playerTarget.transform.position);

            // 1. Detect Player in Sight Range
            if (!_isRushing && dist <= sightRadius)
            {
                _isRushing = true;
                _currentRushSpeed = rushSpeed;
            }

            // 2. Rush towards Player with continuous acceleration
            if (_isRushing)
            {
                _currentRushSpeed = CalculateAcceleratedSpeed(_currentRushSpeed, rushAcceleration, maxRushSpeed, Time.deltaTime);

                Vector3 toPlayer = (_playerTarget.transform.position - transform.position).normalized;
                float step = _currentRushSpeed * Time.deltaTime;
                transform.position += toPlayer * step;

                // Face the player so Golfer doesn't turn around and run backwards
                if (facePlayerOnRush && spriteRenderer != null && Mathf.Abs(toPlayer.x) > 0.05f)
                {
                    spriteRenderer.flipX = CalculateFacingFlip(toPlayer, spriteFacesLeftByDefault);
                }

                // If touching distance or overshooting in this frame
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
            // 1. Lock Owen's input instantly
            player.InputLocked = true;

            // 2. Spawn Cyan Soul directly over Owen's chest
            Vector3 chestPos = player.transform.position + new Vector3(0f, 0.45f, 0f);
            GameObject soulObj = new GameObject("EncounterAlertSoul");
            soulObj.transform.position = chestPos;
            soulObj.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

            var sr = soulObj.AddComponent<SpriteRenderer>();
            sr.sprite = cyanSoulSprite;
            sr.sortingOrder = 3000;

            // 3. Screen darkens and Soul blinks 3 times (4 frames on, 3 frames off)
            for (int i = 0; i < 3; i++)
            {
                sr.enabled = true;
                ScreenFader.Instance.SetAlpha(1.0f);
                yield return new WaitForSeconds(0.12f);

                sr.enabled = false;
                ScreenFader.Instance.SetAlpha(0.0f);
                yield return new WaitForSeconds(0.08f);
            }

            // Final blackout
            sr.enabled = true;
            ScreenFader.Instance.SetAlpha(1.0f);
            yield return new WaitForSeconds(0.15f);

            Destroy(soulObj);

            // Locate or spawn Battle Stage in deep void
            var battleStage = BattleStageController.EnsureBattleStageExists();

            // Delegate room/battle swap to ScreenFader.Instance so deactivating
            // this GameObject does NOT kill the fade-in coroutine!
            ScreenFader.Instance.Transition(
                onBlack: () =>
                {
                    // 1. Center Main Camera over the battle stage and disable overworld room clamping
                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        var roomCam = mainCam.GetComponent<RoomCameraController>();
                        if (roomCam != null) roomCam.enabled = false;

                        Vector3 stagePos = battleStage != null ? battleStage.transform.position : new Vector3(1000f, 1000f, 0f);
                        mainCam.transform.position = new Vector3(stagePos.x, stagePos.y + 0.5f, -10f);
                        mainCam.orthographicSize = 6.0f;
                    }

                    // 2. Activate battle stage and pass this enemy's data
                    if (battleStage != null)
                    {
                        battleStage.gameObject.SetActive(true);
                        battleStage.InitializeWithEnemy(enemyData, player, this);
                    }

                    // 3. Hide overworld player & this overworld enemy
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
