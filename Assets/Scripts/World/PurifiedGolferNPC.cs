using System.Collections;
using UnityEngine;
using DigiPurity.Combat;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DigiPurity.World
{
    public class PurifiedGolferNPC : MonoBehaviour
    {
        [Header("Dialogue Content")]
        [SerializeField] private string[] dialogueLines = new string[]
        {
            "* \"Thanks for pulling me out of that corrupted loop, Owen.\"",
            "* \"My code feels light as a feather. You've got real heart, kid.\"",
            "* \"Since this is just the demo, there's nothing more out on this green.\"",
            "* \"You can close the game or take a well-deserved breather!\"",
            "* \"Good luck finding Lucas out there. He believed in you.\""
        };

        [Header("Voice & Audio")]
        [SerializeField] private AudioClip voiceBlipClip;

        private AudioSource _voiceAudioSource;
        private SpriteRenderer _sr;
        private OverworldPlayerController _player;
        private bool _isTalking = false;
        private int _dialogueIndex = 0;
        private float _shimmerTimer = 0f;

        // Typewriter scrolling state
        private string _visibleText = "";
        private int _typedChars = 0;
        private float _charTimer = 0f;
        private bool _isTyping = false;
        private float _charSpeed = 0.030f;
        private float _inputDebounce = 0f;

        private void Awake()
        {
            _sr = GetComponentInChildren<SpriteRenderer>();
            if (_sr != null)
            {
                _sr.material = BattleDodgeBox.GetUnlitMaterial(Color.white);
            }
            EnsureVoiceAudio();
        }

        private void EnsureVoiceAudio()
        {
            if (_voiceAudioSource == null)
            {
                Transform child = transform.Find("VoiceAudio");
                if (child != null)
                {
                    _voiceAudioSource = child.GetComponent<AudioSource>();
                }
                if (_voiceAudioSource == null)
                {
                    GameObject vObj = new GameObject("VoiceAudio");
                    vObj.transform.SetParent(transform);
                    _voiceAudioSource = vObj.AddComponent<AudioSource>();
                }
                _voiceAudioSource.playOnAwake = false;
                _voiceAudioSource.loop = false;
                _voiceAudioSource.spatialBlend = 0f;
            }

            if (voiceBlipClip == null)
            {
#if UNITY_EDITOR
                voiceBlipClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/snd_txt_golfer.wav");
                if (voiceBlipClip == null)
                {
                    voiceBlipClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/snd_txt_default.wav");
                }
#endif
            }
        }

        private void PlayVoiceBlip()
        {
            if (_voiceAudioSource == null) EnsureVoiceAudio();
            if (_voiceAudioSource != null && voiceBlipClip != null)
            {
                _voiceAudioSource.pitch = Random.Range(0.96f, 1.04f);
                _voiceAudioSource.PlayOneShot(voiceBlipClip, 0.50f);
            }
        }

        private void Start()
        {
            _player = FindAnyObjectByType<OverworldPlayerController>();
        }

        private void Update()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<OverworldPlayerController>();
                if (_player == null) return;
            }

            if (_inputDebounce > 0f) _inputDebounce -= Time.deltaTime;

            // Peaceful white / cyan aura shimmer
            _shimmerTimer += Time.deltaTime * 3f;
            if (_sr != null)
            {
                float shimmer = 0.88f + (Mathf.Sin(_shimmerTimer) * 0.12f);
                _sr.color = new Color(shimmer, 1f, 1f, 1f);
            }

            float dist = Vector2.Distance(transform.position, _player.transform.position);
            bool inRange = dist <= 2.5f;
            bool isInteractDown = IsInteractDown();

            if (!_isTalking)
            {
                if (inRange && isInteractDown && !_player.InputLocked && _inputDebounce <= 0f)
                {
                    _inputDebounce = 0.20f;
                    _isTalking = true;
                    _player.InputLocked = true;
                    _dialogueIndex = 0;
                    StartTypingLine(dialogueLines[_dialogueIndex]);
                }
            }
            else
            {
                // Advance typewriter text
                if (_isTyping)
                {
                    string fullText = dialogueLines[_dialogueIndex];
                    _charTimer += Time.deltaTime;
                    while (_charTimer >= _charSpeed && _typedChars < fullText.Length)
                    {
                        _typedChars++;
                        _charTimer -= _charSpeed;
                        char c = fullText[_typedChars - 1];
                        if (!char.IsWhiteSpace(c) && (_typedChars % 2 == 0))
                        {
                            PlayVoiceBlip();
                        }
                    }
                    _visibleText = fullText.Substring(0, _typedChars);
                    if (_typedChars >= fullText.Length)
                    {
                        _isTyping = false;
                    }
                }

                if (isInteractDown && _inputDebounce <= 0f)
                {
                    _inputDebounce = 0.18f;
                    if (_isTyping)
                    {
                        // Skip typing to end of current line instantly
                        _typedChars = dialogueLines[_dialogueIndex].Length;
                        _visibleText = dialogueLines[_dialogueIndex];
                        _isTyping = false;
                    }
                    else
                    {
                        // Advance to next line
                        _dialogueIndex++;
                        if (_dialogueIndex >= dialogueLines.Length)
                        {
                            _isTalking = false;
                            _player.InputLocked = false;
                        }
                        else
                        {
                            StartTypingLine(dialogueLines[_dialogueIndex]);
                        }
                    }
                }
            }
        }

        private void StartTypingLine(string line)
        {
            _typedChars = 0;
            _charTimer = 0f;
            _isTyping = true;
            _visibleText = "";
            PlayVoiceBlip();
        }

        private bool IsInteractDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame))
                return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.buttonWest.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame))
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z))
                return true;
#endif
            return false;
        }

        private void OnGUI()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Font font = DigiPurity.Core.GameFonts.MainFont;

            if (!_isTalking)
            {
                if (_player == null) return;
                float dist = Vector2.Distance(transform.position, _player.transform.position);
                if (dist > 3.0f) return;

                Vector3 screenPos = cam.WorldToScreenPoint(transform.position + new Vector3(0f, 1.2f, 0f));
                if (screenPos.z < 0) return;

                float promptY = Screen.height - screenPos.y;
                float promptW = 140f;
                float promptH = 24f;
                Rect promptRect = new Rect(screenPos.x - (promptW * 0.5f), promptY, promptW, promptH);

                float pulse = 0.65f + (Mathf.Sin(_shimmerTimer) * 0.25f);
                GUI.color = new Color(1f, 0.95f, 0.5f, dist <= 2.5f ? pulse : 0.35f);

                var promptStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    fontStyle = FontStyle.Bold
                };
                if (font != null) promptStyle.font = font;

                GUI.Label(promptRect, "[E] Talk", promptStyle);
                GUI.color = Color.white;
            }
            else
            {
                int prevDepth = GUI.depth;
                GUI.depth = -500;

                // Frame dialogue box strictly inside the active 4:3 game viewport
                Rect vpRect = RoomCameraController.Get4By3PixelRect();
                float margin = 20f;
                float boxW = vpRect.width - (margin * 2f);
                float boxH = 105f;
                float boxX = vpRect.x + margin;
                float boxY = vpRect.y + vpRect.height - boxH - 16f;
                Rect dlgBox = new Rect(boxX, boxY, boxW, boxH);

                DrawBorderedBox(dlgBox, Color.black, Color.white, 3f);

                var textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    wordWrap = true
                };
                if (font != null) textStyle.font = font;

                GUI.color = Color.white;
                float padX = dlgBox.x + 22f;
                float padY = dlgBox.y + 16f;
                float textW = dlgBox.width - 44f;

                GUI.Label(new Rect(padX, padY, textW, 55), _visibleText, textStyle);

                // Continue / close indicator
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                var hintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontSize = 11 };
                if (font != null) hintStyle.font = font;
                string hint = _isTyping ? "[E / ENTER] Skip" : ((_dialogueIndex < dialogueLines.Length - 1) ? "[E / ENTER] Next" : "[E / ENTER] Close");
                GUI.Label(new Rect(dlgBox.x, dlgBox.y + dlgBox.height - 24, dlgBox.width - 16, 20), hint, hintStyle);
                GUI.color = Color.white;

                GUI.depth = prevDepth;
            }
        }

        private void DrawBorderedBox(Rect r, Color fillColor, Color borderColor, float borderWidth)
        {
            GUI.color = fillColor;
            GUI.DrawTexture(r, Texture2D.whiteTexture);

            GUI.color = borderColor;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, borderWidth), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - borderWidth, r.width, borderWidth), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, borderWidth, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x + r.width - borderWidth, r.y, borderWidth, r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
