using System;
using System.Collections;
using UnityEngine;

namespace DigiPurity.World
{
    [ExecuteAlways]
    public class ScreenFader : MonoBehaviour
    {
        private static ScreenFader _instance;

        public static ScreenFader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ScreenFader>();
                    if (_instance == null)
                    {
                        var go = new GameObject("ScreenFader");
                        _instance = go.AddComponent<ScreenFader>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }

        [Header("Fade Settings")]
        [Tooltip("Fade color (solid black by default for Undertale/Deltarune style).")]
        [SerializeField] private Color fadeColor = Color.black;
        [Tooltip("Default duration in seconds for fading to black or fading back in.")]
        [SerializeField] private float defaultFadeDuration = 0.28f;

        private float _currentAlpha = 0f;
        private bool _isTransitioning = false;

        public float CurrentAlpha => _currentAlpha;
        public bool IsTransitioning => _isTransitioning;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (_instance != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void OnGUI()
        {
            if (_currentAlpha <= 0.001f) return;

            // Render on top of all 2D layers, sprites, and UI
            int originalDepth = GUI.depth;
            GUI.depth = -2000;

            Color oldColor = GUI.color;
            GUI.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, Mathf.Clamp01(_currentAlpha));
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = oldColor;

            GUI.depth = originalDepth;
        }

        /// <summary>
        /// Executes a complete Undertale/Deltarune area transition:
        /// 1. Smoothly fades screen to black.
        /// 2. Invokes onBlack (teleports player, swaps rooms, snaps camera).
        /// 3. Smoothly fades screen back to transparent.
        /// </summary>
        public void Transition(Action onBlack, float duration = -1f, Action onComplete = null)
        {
            if (!Application.isPlaying)
            {
                // In edit mode or test mode, invoke immediately
                onBlack?.Invoke();
                onComplete?.Invoke();
                return;
            }

            float fadeTime = duration > 0f ? duration : defaultFadeDuration;
            StartCoroutine(TransitionRoutine(onBlack, fadeTime, onComplete));
        }

        private IEnumerator TransitionRoutine(Action onBlack, float duration, Action onComplete)
        {
            _isTransitioning = true;

            // 1. Fade Out (Transparent -> Black)
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _currentAlpha = Mathf.Clamp01(t / duration);
                yield return null;
            }
            _currentAlpha = 1f;

            // 2. Perform teleport / room cut at pure black
            try
            {
                onBlack?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScreenFader] Error in transition onBlack callback: {ex}");
            }

            // Brief hold at pure black (1 frame) so camera and colliders settle
            yield return null;

            // 3. Fade In (Black -> Transparent)
            t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _currentAlpha = Mathf.Clamp01(1f - (t / duration));
                yield return null;
            }
            _currentAlpha = 0f;

            _isTransitioning = false;
            onComplete?.Invoke();
        }

        public void SetAlpha(float alpha)
        {
            _currentAlpha = Mathf.Clamp01(alpha);
        }
    }
}
