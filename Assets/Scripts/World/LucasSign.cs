using System.Collections;
using System.IO;
using UnityEngine;
using DigiPurity.Combat;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DigiPurity.World
{
    public class LucasSign : MonoBehaviour
    {
        public static readonly Vector3 DEFAULT_SIGN_POS = new Vector3(10.5f, -0.8f, 0f);

        [Header("Visuals & Font")]
        [SerializeField] private Sprite signSprite;
        [SerializeField] private Font undertaleFont;
        [SerializeField] private Vector3 spawnPosition = DEFAULT_SIGN_POS;

        private SpriteRenderer _sr;
        private SpriteRenderer _shadowSr;
        private BoxCollider2D _collider;
        private OverworldPlayerController _player;
        private bool _isReading = false;
        private bool _isFizzling = false;
        private float _promptAlphaTimer = 0f;

        private Vector3 _basePos;
        private Sprite[] _glitchFrames;
        private int _currentFrameIndex = 0;
        private float _glitchTickTimer = 0f;
        private bool _isGlitchActive = false;

        private static Font _cachedFont;
        private static bool _signEverDismissed = false;

        public static Font GetUndertaleFont() { if (_cachedFont != null) return _cachedFont; _cachedFont = DigiPurity.Core.GameFonts.SignFont; return _cachedFont; }

        private void Awake()
        {
            if (_signEverDismissed)
            {
                Destroy(gameObject);
                return;
            }

            if (transform.position == Vector3.zero)
            {
                transform.position = spawnPosition;
            }
            _basePos = transform.position;

            // Make sign a tiny bit bigger per user feedback
            transform.localScale = new Vector3(1.18f, 1.18f, 1f);

            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();

            // Solid foot collider aligned with the grounded base & shadow
            _collider = GetComponent<BoxCollider2D>();
            if (_collider == null) _collider = gameObject.AddComponent<BoxCollider2D>();
            _collider.isTrigger = false;
            _collider.size = new Vector2(2.4f, 0.55f);
            _collider.offset = new Vector2(0f, -0.45f);

            // Ground contact shadow child to make it look grounded and "stuck" there
            EnsureContactShadow();

            LoadGlitchFrames();

            if (_glitchFrames != null && _glitchFrames.Length > 0)
            {
                _sr.sprite = _glitchFrames[0];
            }

            _sr.material = BattleDodgeBox.GetUnlitMaterial(Color.white);
            _sr.color = Color.white;
            _sr.sortingOrder = 950; // Just below Owen torso

            if (undertaleFont == null) undertaleFont = GetUndertaleFont();
        }

        private void EnsureContactShadow()
        {
            Transform shadowChild = transform.Find("Sign_Shadow");
            GameObject shadowObj;
            if (shadowChild != null)
            {
                shadowObj = shadowChild.gameObject;
            }
            else
            {
                shadowObj = new GameObject("Sign_Shadow");
                shadowObj.transform.SetParent(transform);
            }

            shadowObj.transform.localPosition = new Vector3(0f, -0.66f, 0f);
            shadowObj.transform.localScale = new Vector3(1.15f, 0.45f, 1f);

            _shadowSr = shadowObj.GetComponent<SpriteRenderer>();
            if (_shadowSr == null) _shadowSr = shadowObj.AddComponent<SpriteRenderer>();

            string shadowPath = Application.dataPath + "/Sprites/World/Drop_Shadow_64.png";
            if (File.Exists(shadowPath))
            {
                _shadowSr.sprite = LoadSpriteDirect(shadowPath, 32f);
            }
            if (_shadowSr.sprite == null)
            {
                _shadowSr.sprite = CreateProceduralShadowSprite();
            }

            _shadowSr.material = BattleDodgeBox.GetUnlitMaterial(Color.white);
            _shadowSr.color = new Color(0f, 0f, 0f, 0.65f);
            _shadowSr.sortingOrder = 930; // Resting on the floor below the sign
        }

        private void Start()
        {
            _player = FindAnyObjectByType<OverworldPlayerController>();
        }

        private void LoadGlitchFrames()
        {
            _glitchFrames = new Sprite[4];

#if UNITY_EDITOR
            for (int i = 0; i < 4; i++)
            {
                Sprite asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/World/Lucas_Sign_{i}.png");
                if (asset != null) _glitchFrames[i] = asset;
            }
            if (_glitchFrames[0] == null)
            {
                Sprite mainAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/World/Lucas_Sign.png");
                if (mainAsset != null)
                {
                    for (int i = 0; i < 4; i++) _glitchFrames[i] = mainAsset;
                }
            }
#endif

            if (_glitchFrames[0] == null)
            {
                string[] possibleDirs = new string[]
                {
                    Application.dataPath + "/Sprites/World/",
                    System.IO.Path.Combine(Application.dataPath, "..", "Assets/Sprites/World/"),
                    System.IO.Path.Combine(Application.dataPath, "Sprites/")
                };

                foreach (string dir in possibleDirs)
                {
                    if (System.IO.Directory.Exists(dir))
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            string p = System.IO.Path.Combine(dir, $"Lucas_Sign_{i}.png");
                            if (File.Exists(p)) _glitchFrames[i] = LoadSpriteDirect(p, 32f);
                        }
                        if (_glitchFrames[0] == null)
                        {
                            string mainP = System.IO.Path.Combine(dir, "Lucas_Sign.png");
                            if (File.Exists(mainP))
                            {
                                Sprite s = LoadSpriteDirect(mainP, 32f);
                                for (int i = 0; i < 4; i++) _glitchFrames[i] = s;
                            }
                        }
                        if (_glitchFrames[0] != null) break;
                    }
                }
            }

            // Ultimate procedural fallback
            if (_glitchFrames[0] == null)
            {
                Sprite proc = CreateProceduralSignSprite();
                for (int i = 0; i < 4; i++) _glitchFrames[i] = proc;
            }
        }

        private Sprite LoadSpriteDirect(string path, float ppu)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.LoadImage(bytes);
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
            }
            catch
            {
                return null;
            }
        }

        private Sprite CreateProceduralShadowSprite()
        {
            int w = 64, h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] cols = new Color[w * h];

            float cx = (w - 1) / 2f;
            float cy = (h - 1) / 2f;
            float rx = cx - 2f;
            float ry = cy - 2f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / rx;
                    float dy = (y - cy) / ry;
                    float d2 = (dx * dx) + (dy * dy);
                    if (d2 <= 1f)
                    {
                        float t = Mathf.Sqrt(d2);
                        float alpha = 1f - (3f * t * t - 2f * t * t * t);
                        cols[y * w + x] = new Color(0f, 0f, 0f, alpha * 0.75f);
                    }
                    else
                    {
                        cols[y * w + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        }

        private Sprite CreateProceduralSignSprite()
        {
            int w = 80, h = 44;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] cols = new Color[w * h];
            Color white = Color.white;
            Color fill = new Color(0.1f, 0.15f, 0.22f, 0.8f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = (x >= 8 && x <= 71 && (y == 3 || y == 26)) ||
                                    (y >= 3 && y <= 26 && (x == 8 || x == 71));
                    if (isBorder)
                    {
                        cols[y * w + x] = white;
                    }
                    else if (x > 8 && x < 71 && y > 3 && y < 26)
                    {
                        cols[y * w + x] = fill;
                    }
                    else if (x >= 28 && x <= 51 && y >= 37 && y <= 40)
                    {
                        cols[y * w + x] = new Color(0.6f, 0.7f, 0.85f, 1f);
                    }
                    else
                    {
                        cols[y * w + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        }

        private void Update()
        {
            if (_isFizzling) return;

            if (_player == null)
            {
                _player = FindAnyObjectByType<OverworldPlayerController>();
                if (_player == null) return;
            }

            _promptAlphaTimer += Time.deltaTime * 2.8f;

            // Glitch animation & micro-jitter on screen only (base stays grounded)
            UpdateGlitchAnimation();

            float dist = Vector2.Distance(transform.position, _player.transform.position);
            bool inRange = dist <= 2.6f;

            bool isInteractDown = IsInteractDown();

            if (!_isReading)
            {
                if (inRange && isInteractDown && !_player.InputLocked)
                {
                    _isReading = true;
                    _player.InputLocked = true;
                }
            }
            else
            {
                if (isInteractDown)
                {
                    _isReading = false;
                    _player.InputLocked = false;
                    StartCoroutine(FizzleRoutine());
                }
            }
        }

        private void UpdateGlitchAnimation()
        {
            _glitchTickTimer -= Time.deltaTime;

            if (_glitchTickTimer <= 0f)
            {
                if (_isGlitchActive)
                {
                    // Return to clean frame 0
                    _isGlitchActive = false;
                    _currentFrameIndex = 0;
                    _glitchTickTimer = Random.Range(0.7f, 1.8f); // calm interval
                    transform.position = _basePos;
                    if (_sr != null) _sr.color = Color.white;
                }
                else
                {
                    // Trigger a rapid glitch twitch
                    _isGlitchActive = true;
                    _currentFrameIndex = Random.Range(1, 4);
                    _glitchTickTimer = Random.Range(0.06f, 0.12f); // rapid twitch
                    float jitterX = Random.Range(-0.05f, 0.05f);
                    transform.position = _basePos + new Vector3(jitterX, 0f, 0f);

                    if (_sr != null)
                    {
                        _sr.color = Random.value > 0.5f ? new Color(0.7f, 0.95f, 1f, 1f) : Color.white;
                    }
                }

                if (_glitchFrames != null && _glitchFrames.Length > _currentFrameIndex && _glitchFrames[_currentFrameIndex] != null)
                {
                    _sr.sprite = _glitchFrames[_currentFrameIndex];
                }
            }
        }

        private bool IsInteractDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.buttonWest.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame))
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Space))
                return true;
#endif
            return false;
        }

        private IEnumerator FizzleRoutine()
        {
            _isFizzling = true;
            _signEverDismissed = true;

            if (_collider != null)
            {
                _collider.enabled = false;
            }

            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;
            Vector3 shadowStartScale = _shadowSr != null ? _shadowSr.transform.localScale : Vector3.one;

            float duration = 0.85f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // Violent glitch shake and digital tear
                float shakeX = Random.Range(-0.16f, 0.16f) * (1f + progress * 2f);
                float shakeY = Random.Range(-0.08f, 0.08f) * (1f + progress * 2f);
                transform.position = startPos + new Vector3(shakeX, shakeY, 0f);

                float scaleRatio = Mathf.Max(0f, 1f - Mathf.Pow(progress, 1.4f));
                transform.localScale = new Vector3(startScale.x * scaleRatio, startScale.y * (1f + progress * 0.4f), startScale.z);

                // Alternating cyber colors
                bool blink = (Mathf.FloorToInt(elapsed * 35f) % 2) == 0;
                if (_sr != null)
                {
                    Color c = blink ? new Color(0.2f, 0.95f, 1f, 1f - progress) : new Color(1f, 1f, 1f, (1f - progress) * 0.85f);
                    if (progress > 0.6f && Random.value > 0.6f) c = new Color(1f, 0.2f, 0.2f, 1f - progress);
                    _sr.color = c;
                }

                // Shadow collapses and dissolves simultaneously
                if (_shadowSr != null)
                {
                    _shadowSr.transform.localScale = shadowStartScale * scaleRatio;
                    _shadowSr.color = new Color(0f, 0f, 0f, 0.65f * (1f - progress));
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        private void OnGUI()
        {
            if (_isFizzling) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Font font = undertaleFont != null ? undertaleFont : GetUndertaleFont();

            // 1. Faint text under the sign saying 'E to interact'
            if (!_isReading)
            {
                if (_player == null) return;
                float dist = Vector2.Distance(transform.position, _player.transform.position);
                if (dist > 3.4f) return;

                Vector3 screenPos = cam.WorldToScreenPoint(transform.position - new Vector3(0f, 0.95f, 0f));
                if (screenPos.z < 0) return;

                float promptY = Screen.height - screenPos.y;
                float promptW = 160f;
                float promptH = 24f;
                Rect promptRect = new Rect(screenPos.x - (promptW * 0.5f), promptY, promptW, promptH);

                float pulse = 0.55f + (Mathf.Sin(_promptAlphaTimer) * 0.25f);
                GUI.color = new Color(0.85f, 0.94f, 1.0f, dist <= 2.6f ? pulse : 0.35f);

                var promptStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    fontStyle = FontStyle.Normal
                };
                if (font != null) promptStyle.font = font;

                GUI.Label(promptRect, "[E] to interact", promptStyle);
                GUI.color = Color.white;
            }
            else
            {
                int prevDepth = GUI.depth;
                GUI.depth = -500;

                // Frame dialogue box strictly inside the active 4:3 game viewport (never bleed into pillarbox bars)
                Rect vpRect = RoomCameraController.Get4By3PixelRect();
                float margin = 20f;
                float boxW = vpRect.width - (margin * 2f);
                float boxH = 108f;
                float boxX = vpRect.x + margin;
                float boxY = vpRect.y + vpRect.height - boxH - 16f;
                Rect dlgBox = new Rect(boxX, boxY, boxW, boxH);

                // Black fill, 3px white border
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

                string line1 = "* \"purify the world, owen.\"\n* \"or burn it all to the ground.\"";
                string line2 = "* \" - lucas.\"";
                GUI.Label(new Rect(padX, padY, textW, 46), line1, textStyle);

                GUI.color = new Color(0.85f, 0.85f, 0.95f);
                GUI.Label(new Rect(padX, padY + 48, textW, 24), line2, textStyle);

                // Dismiss hint
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                var hintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontSize = 11 };
                if (font != null) hintStyle.font = font;
                GUI.Label(new Rect(dlgBox.x, dlgBox.y + dlgBox.height - 24, dlgBox.width - 16, 20), "[E / ENTER] Close", hintStyle);
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureSignExistsInScene()
        {
            if (_signEverDismissed) return;

            var existing = FindAnyObjectByType<LucasSign>();
            if (existing == null)
            {
                GameObject signObj = new GameObject("Lucas_Sign");
                var sign = signObj.AddComponent<LucasSign>();
                sign.spawnPosition = DEFAULT_SIGN_POS;
                signObj.transform.position = DEFAULT_SIGN_POS;
            }
        }
    }
}
