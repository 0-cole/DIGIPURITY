using System.Collections;
using UnityEngine;

namespace DigiPurity.World
{
    public class OverworldBgmManager : MonoBehaviour
    {
        private static OverworldBgmManager _instance;
        public static OverworldBgmManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var found = FindAnyObjectByType<OverworldBgmManager>();
                    if (found != null)
                    {
                        _instance = found;
                    }
                    else
                    {
                        GameObject obj = new GameObject("OverworldBgmManager");
                        DontDestroyOnLoad(obj);
                        _instance = obj.AddComponent<OverworldBgmManager>();
                    }
                }
                return _instance;
            }
        }

        private AudioSource _audioSource;
        private Coroutine _fadeCoroutine;

        [SerializeField] private AudioClip abandonwareClip;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.loop = true;
            _audioSource.playOnAwake = false;

            if (abandonwareClip == null)
            {
#if UNITY_EDITOR
                abandonwareClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Abandonware.mp3");
#endif
            }
        }

        public void PlayAbandonware(float targetVolume = 0.28f, float fadeInDuration = 3.0f)
        {
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

            if (abandonwareClip == null)
            {
#if UNITY_EDITOR
                abandonwareClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Abandonware.mp3");
#endif
            }

            if (abandonwareClip != null)
            {
                _audioSource.clip = abandonwareClip;
            }

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeInRoutine(targetVolume, fadeInDuration));
        }

        private IEnumerator FadeInRoutine(float targetVolume, float duration)
        {
            _audioSource.volume = 0f;
            _audioSource.loop = true;
            _audioSource.Play();

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _audioSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
                yield return null;
            }

            _audioSource.volume = targetVolume;
            _fadeCoroutine = null;
        }

        public void StopMusic()
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            if (_audioSource != null) _audioSource.Stop();
        }
    }
}
