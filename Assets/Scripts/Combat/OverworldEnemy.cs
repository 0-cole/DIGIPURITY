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
        [SerializeField] private float rushSpeed = 3.8f;
        [SerializeField] private float stopDistance = 0.5f;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite cyanSoulSprite;

        private OverworldPlayerController _playerTarget;
        private Vector3 _startPosition;
        private bool _isRushing = false;
        private bool _isEncounterTriggered = false;

        public EnemyData Data => enemyData;

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
            }

            // 2. Rush towards Player
            if (_isRushing)
            {
                Vector3 toPlayer = (_playerTarget.transform.position - transform.position).normalized;
                transform.position += toPlayer * rushSpeed * Time.deltaTime;

                // Flip sprite to face player
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = toPlayer.x < 0f;
                }

                // If touching distance
                if (dist <= stopDistance)
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
