using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DigiPurity.World
{
    [RequireComponent(typeof(BoxCollider2D))]
    [ExecuteAlways]
    public class AreaDoor : MonoBehaviour
    {
        [Header("Door Identification")]
        [Tooltip("Identifier of this door (e.g. 'A', 'Forest_East', 'Room1_Exit').")]
        [SerializeField] private string doorId = "A";

        [Tooltip("Identifier of the target door to arrive at.")]
        [SerializeField] private string targetDoorId = "B";

        [Tooltip("Optional scene name to load. If left blank, warps within the current scene/area.")]
        [SerializeField] private string targetSceneName = "";

        [Header("Arrival Settings (When Player Emerges Here)")]
        [Tooltip("Offset from this door's center where the player will appear when arriving from another door.")]
        [SerializeField] private Vector2 arrivalSpawnOffset = new Vector2(0f, -1.2f);

        [Tooltip("Facing direction of the player upon emerging from this door.")]
        [SerializeField] private OverworldPlayerController.FacingDirection arrivalFacing = OverworldPlayerController.FacingDirection.Down;

        [Header("Fade & Timing")]
        [Tooltip("Duration of the fade-out and fade-in (default 0.28s matches Undertale/Deltarune).")]
        [SerializeField] private float fadeDuration = 0.28f;

        [Header("Deltarune Secret Warp (Egg Room)")]
        [Tooltip("When enabled, entering this door has a percentage chance to divert to a secret room instead of the normal target!")]
        [SerializeField] private bool hasSecretChance = false;

        [Range(0f, 100f)]
        [Tooltip("Percentage chance to trigger secret warp (2.0% = 1 in 50, identical to Deltarune Chapter 1 & 2).")]
        [SerializeField] private float secretChancePercent = 2.0f;

        [Tooltip("Target door ID inside the secret room.")]
        [SerializeField] private string secretTargetDoorId = "EggRoom_Door";

        [Tooltip("Optional scene name containing the secret room.")]
        [SerializeField] private string secretTargetScene = "";

        [Tooltip("If true, secret warp only triggers if the Egg has not been collected yet.")]
        [SerializeField] private bool onlyBeforeEggCollected = true;

        [Tooltip("Check this on the EXIT door inside the secret room. It will return the player back to where they were originally heading before the secret warp!")]
        [SerializeField] private bool isSecretRoomExit = false;

        // Static tracking across doors/scenes (Deltarune style)
        public static bool EggCollected = false;
        public static string DivertedTargetDoorId = "";
        public static string DivertedTargetScene = "";

        private float _cooldownTimer = 0f;
        private static bool _isAnyTransitionActive = false;

        public string DoorId { get => doorId; set => doorId = value; }
        public string TargetDoorId { get => targetDoorId; set => targetDoorId = value; }
        public string TargetSceneName { get => targetSceneName; set => targetSceneName = value; }
        public Vector2 ArrivalSpawnOffset { get => arrivalSpawnOffset; set => arrivalSpawnOffset = value; }
        public OverworldPlayerController.FacingDirection ArrivalFacing { get => arrivalFacing; set => arrivalFacing = value; }
        public bool HasSecretChance { get => hasSecretChance; set => hasSecretChance = value; }
        public float SecretChancePercent { get => secretChancePercent; set => secretChancePercent = value; }
        public string SecretTargetDoorId { get => secretTargetDoorId; set => secretTargetDoorId = value; }
        public bool IsSecretRoomExit { get => isSecretRoomExit; set => isSecretRoomExit = value; }

        private void Reset()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Awake()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }
        }

        public void SetCooldown(float duration = 1.0f)
        {
            _cooldownTimer = duration;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!Application.isPlaying) return;
            if (_cooldownTimer > 0f || _isAnyTransitionActive) return;

            // Check if player triggered the door
            var player = other.GetComponent<OverworldPlayerController>();
            if (player == null) player = other.GetComponentInParent<OverworldPlayerController>();
            if (player == null) return;

            StartTransition(player);
        }

        public void StartTransition(OverworldPlayerController player)
        {
            if (_isAnyTransitionActive) return;
            StartCoroutine(DoDoorTransition(player));
        }

        private IEnumerator DoDoorTransition(OverworldPlayerController player)
        {
            _isAnyTransitionActive = true;
            _cooldownTimer = 2.0f;

            // 1. Lock Player Input (Toby Fox: global.interact = 3)
            player.InputLocked = true;

            // 2. Determine Destination (Normal vs Egg Room Divert)
            string destDoor = targetDoorId;
            string destScene = targetSceneName;

            if (isSecretRoomExit && !string.IsNullOrEmpty(DivertedTargetDoorId))
            {
                // Returning from secret room -> resume original path!
                destDoor = DivertedTargetDoorId;
                destScene = DivertedTargetScene;
                DivertedTargetDoorId = "";
                DivertedTargetScene = "";
                Debug.Log($"[AreaDoor] Exiting secret room -> returning to original destination '{destDoor}'.");
            }
            else if (hasSecretChance && (!onlyBeforeEggCollected || !EggCollected))
            {
                // Roll secret chance (e.g. 2% / 1 in 50)
                float roll = Random.Range(0f, 100f);
                if (roll < secretChancePercent)
                {
                    // Save original intended destination so exiting the secret room brings you back
                    DivertedTargetDoorId = targetDoorId;
                    DivertedTargetScene = targetSceneName;

                    destDoor = secretTargetDoorId;
                    destScene = secretTargetScene;
                    Debug.Log($"[AreaDoor] ✨ Deltarune Secret Room Warp Triggered! (Rolled {roll:F1}% < {secretChancePercent}%). Warping to '{destDoor}'");
                }
            }

            // 3. Fade Out to Black
            bool blackReached = false;
            ScreenFader.Instance.Transition(
                onBlack: () => { blackReached = true; },
                duration: fadeDuration,
                onComplete: () => { }
            );

            while (!blackReached)
            {
                yield return null;
            }

            // 4. Pure Black: Load target scene if specified
            if (!string.IsNullOrEmpty(destScene) && destScene != SceneManager.GetActiveScene().name)
            {
                AsyncOperation loadOp = SceneManager.LoadSceneAsync(destScene);
                while (!loadOp.isDone)
                {
                    yield return null;
                }
                // Allow scene start frames to initialize
                yield return null;
            }

            // 5. Locate matching target door in destination
            AreaDoor arrivalDoor = FindDoorById(destDoor);
            Vector3 arrivalPos = transform.position;
            var facing = arrivalFacing;

            if (arrivalDoor != null)
            {
                arrivalPos = arrivalDoor.transform.position + (Vector3)arrivalDoor.arrivalSpawnOffset;
                facing = arrivalDoor.arrivalFacing;
                arrivalDoor.SetCooldown(1.2f);
            }
            else
            {
                Debug.LogWarning($"[AreaDoor] Target door with ID '{destDoor}' not found! Spawning at default location.");
            }

            // 6. Reposition Player & Set Facing Direction
            if (player != null)
            {
                player.transform.position = arrivalPos;
                player.SetFacingDirection(facing);
            }

            // 7. Snap Camera to Player & Room Bounds
            var cam = FindAnyObjectByType<RoomCameraController>();
            if (cam != null)
            {
                cam.SnapToTarget();
            }

            // Wait for ScreenFader to finish fading back in
            while (ScreenFader.Instance.IsTransitioning)
            {
                yield return null;
            }

            // 8. Unlock Player Input
            if (player != null)
            {
                player.InputLocked = false;
            }

            _isAnyTransitionActive = false;
        }

        public static AreaDoor FindDoorById(string id)
        {
            var doors = FindObjectsByType<AreaDoor>();
            foreach (var d in doors)
            {
                if (d.doorId == id) return d;
            }
            return null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var col = GetComponent<BoxCollider2D>();
            Vector3 center = transform.position;
            Vector3 size = Vector3.one;

            if (col != null)
            {
                center = transform.TransformPoint(col.offset);
                size = new Vector3(col.size.x * transform.lossyScale.x, col.size.y * transform.lossyScale.y, 0.1f);
            }

            // Draw door trigger box
            Gizmos.color = hasSecretChance ? new Color(1f, 0.4f, 0.9f, 0.4f) : new Color(0.2f, 0.8f, 1f, 0.4f);
            Gizmos.DrawCube(center, size);
            Gizmos.color = hasSecretChance ? Color.magenta : Color.cyan;
            Gizmos.DrawWireCube(center, size);

            // Draw arrival spawn preview point and arrow
            Vector3 spawnPoint = transform.position + (Vector3)arrivalSpawnOffset;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(spawnPoint, 0.2f);
            Gizmos.DrawLine(center, spawnPoint);

            UnityEditor.Handles.Label(center + Vector3.up * (size.y * 0.5f + 0.3f), $"🚪 Door [{doorId}] ➔ [{targetDoorId}]");
        }
#endif
    }
}
