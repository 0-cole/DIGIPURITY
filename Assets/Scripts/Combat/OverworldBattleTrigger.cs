using System;
using System.Collections;
using UnityEngine;
using DigiPurity.World;

namespace DigiPurity.Combat
{
    [RequireComponent(typeof(Collider2D))]
    public class OverworldBattleTrigger : MonoBehaviour
    {
        [Header("Encounter Settings")]
        [SerializeField] private GameObject battleStagePrefab;
        [SerializeField] private Sprite cyanSoulSprite;

        private bool _isTriggered = false;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
            AutoLoadSoul();
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

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_isTriggered) return;

            var player = collision.GetComponent<OverworldPlayerController>();
            if (player != null)
            {
                _isTriggered = true;
                StartCoroutine(TriggerEncounterRoutine(player));
            }
        }

        private IEnumerator TriggerEncounterRoutine(OverworldPlayerController player)
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
            sr.sortingOrder = 3000; // Top of everything

            // 3. Screen darkens and Soul blinks 3 times (4 frames on, 3 frames off)
            for (int i = 0; i < 3; i++)
            {
                // Soul visible, screen black
                sr.enabled = true;
                ScreenFader.Instance.SetAlpha(1.0f);
                yield return new WaitForSeconds(0.12f);

                // Soul invisible, screen returns briefly
                sr.enabled = false;
                ScreenFader.Instance.SetAlpha(0.0f);
                yield return new WaitForSeconds(0.08f);
            }

            // 4. Final solid blackout
            sr.enabled = true;
            ScreenFader.Instance.SetAlpha(1.0f);
            yield return new WaitForSeconds(0.2f);

            Destroy(soulObj);

            // Locate or spawn Battle Stage in deep void
            var activeBattle = BattleStageController.EnsureBattleStageExists();

            // Delegate transition to ScreenFader so deactivating this GameObject does not kill the fade coroutine
            ScreenFader.Instance.Transition(
                onBlack: () =>
                {
                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        var roomCam = mainCam.GetComponent<DigiPurity.World.RoomCameraController>();
                        if (roomCam != null) roomCam.enabled = false;
                        mainCam.transform.position = new Vector3(1000f, 1000.5f, -10f);
                        mainCam.orthographicSize = 6.0f;
                    }

                    if (activeBattle != null)
                    {
                        activeBattle.gameObject.SetActive(true);
                        activeBattle.StartNewTurn();
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
