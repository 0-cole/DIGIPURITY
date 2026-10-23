using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DigiPurity.Combat
{
    public class BattleStageController : MonoBehaviour
    {
        public static BattleStageController Instance { get; private set; }

        public enum BattleMenuState
        {
            CommandSelect,   // SKILLS, TALK, ITEM, RUN
            SkillSelect,     // Viewing Owen's drawn skill & toggling defense
            TalkSelect,      // Deltarune TALK / ACT options
            TalkResponse,    // Displaying dialogue flavor text
            Clashing,        // Rushing, 0.25s held frame, spinning coins
            DodgeBoxPhase,   // Undertale bullet hell defense
            PurifyPurgePrompt,
            BattleEnded
        }

        [Header("Stage Positions")]
        [SerializeField] private Vector3 owenBattlePos = new Vector3(-5.0f, 0.5f, 0f);
        [SerializeField] private Vector3 enemyBattlePos = new Vector3(5.0f, 0.5f, 0f);
        [SerializeField] private Vector3 centerClashPos = new Vector3(0f, 0.5f, 0f);

        [Header("Controllers")]
        [SerializeField] private OwenBattleAnimator owenAnimator;
        [SerializeField] private EnemyBattleAnimator enemyAnimator;
        [SerializeField] private BattleCombatantUI owenHeader;
        [SerializeField] private BattleCombatantUI enemyHeader;
        [SerializeField] private BattleDodgeBox dodgeBox;
        [SerializeField] private AudioSource bgmAudioSource;

        [Header("Owen Stats")]
        public int owenMaxHp = 100;
        public int owenCurrentHp = 100;

        [Header("Active Enemy Configuration")]
        [SerializeField] private EnemyData activeEnemyData;
        public int enemyMaxHp = 100;
        public int enemyCurrentHp = 100;
        public int enemyGlitchThreshold = 50;
        public string activeEnemyName = "Enemy";

        private TensionAndChargeSystem _tensionChargeSystem;
        private CombatDeckManager _deckManager;
        private CoinClashResolver _clashResolver;
        private BattleMenuState _menuState = BattleMenuState.CommandSelect;

        private int _currentTurn = 1;
        private int _selectedCommandIndex = 0; // 0: SKILLS, 1: TALK, 2: ITEM, 3: RUN
        private readonly string[] _commandNames = new string[] { "SKILLS", "TALK", "ITEM", "RUN" };

        private CombatSkill _currentOwenSkill;
        private bool _isOwenDefending = false;

        // Enemy skill
        private SkillData _currentEnemySkill;
        private int _enemySlotCount = 2;

        // TALK state
        private int _selectedTalkIndex = 0;
        private string _activeTalkResponse = "";

        // Speech bark
        private string _currentSpeechBark = "";
        private float _speechBarkTimer = 0f;

        public BattleMenuState CurrentState => _menuState;
        public TensionAndChargeSystem TensionSystem => _tensionChargeSystem;

        private void Awake()
        {
            Instance = this;
            _tensionChargeSystem = new TensionAndChargeSystem(initialCharge: 0, initialTension: 0f);
            _deckManager = new CombatDeckManager();
            _clashResolver = new CoinClashResolver();

            AutoBindComponents();
        }

        private void AutoBindComponents()
        {
            if (owenAnimator == null) owenAnimator = FindAnyObjectByType<OwenBattleAnimator>();
            if (enemyAnimator == null) enemyAnimator = FindAnyObjectByType<EnemyBattleAnimator>();
            if (dodgeBox == null) dodgeBox = FindAnyObjectByType<BattleDodgeBox>();
            if (bgmAudioSource == null) bgmAudioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            if (activeEnemyData != null)
            {
                InitializeWithEnemy(activeEnemyData);
            }
            else
            {
                StartNewTurn();
            }
        }

        /// <summary>
        /// Universally configures the battle stage for ANY enemy loaded from an OverworldEnemy!
        /// </summary>
        public void InitializeWithEnemy(EnemyData data)
        {
            activeEnemyData = data;
            if (data != null)
            {
                activeEnemyName = data.enemyName;
                enemyMaxHp = data.maxHp;
                enemyCurrentHp = data.maxHp;
                enemyGlitchThreshold = data.glitchThreshold;

                if (enemyAnimator != null)
                {
                    enemyAnimator.SetEnemyVisuals(data.battleSprite, data.battleHurtSprite, data.battleSpriteScale);
                }

                if (data.battleBgm != null && bgmAudioSource != null)
                {
                    bgmAudioSource.clip = data.battleBgm;
                    bgmAudioSource.loop = true;
                    bgmAudioSource.Play();
                }
            }

            _currentTurn = 1;
            StartNewTurn();
        }

        public void StartNewTurn()
        {
            _menuState = BattleMenuState.CommandSelect;
            _selectedCommandIndex = 0;
            _selectedTalkIndex = 0;

            // Draw Owen Skill
            _currentOwenSkill = _deckManager.DrawSkill();
            _isOwenDefending = false;

            // Setup Enemy skill
            if (activeEnemyData != null)
            {
                _enemySlotCount = Mathf.Min(3, 2 + (_currentTurn - 1));
                _currentEnemySkill = new SkillData
                {
                    skillName = activeEnemyData.primarySkillName,
                    basePower = activeEnemyData.basePower,
                    coinPower = activeEnemyData.coinPower,
                    coinCount = activeEnemyData.coinCount
                };

                // Pick a random in-battle speech bark!
                if (activeEnemyData.inBattleBarks != null && activeEnemyData.inBattleBarks.Length > 0)
                {
                    int barkIdx = UnityEngine.Random.Range(0, activeEnemyData.inBattleBarks.Length);
                    _currentSpeechBark = activeEnemyData.inBattleBarks[barkIdx];
                    _speechBarkTimer = 3.5f;
                }
            }
            else
            {
                _currentEnemySkill = new SkillData { skillName = "Putter Cleave", basePower = 4, coinPower = 3, coinCount = 3 };
            }

            // Reset combatant positions
            if (owenAnimator != null)
            {
                owenAnimator.transform.position = owenBattlePos;
                owenAnimator.PlayIdle();
            }

            if (enemyAnimator != null)
            {
                enemyAnimator.transform.position = enemyBattlePos;
                enemyAnimator.SetBasePosition(enemyBattlePos);
                enemyAnimator.IsBobbing = true;
            }

            UpdateHeadersDisplay();
        }

        private void UpdateHeadersDisplay()
        {
            if (owenHeader != null && _currentOwenSkill != null)
            {
                owenHeader.SetVisible(true);
                string skillTitle = _isOwenDefending ? "BARRIER GUARD" : _currentOwenSkill.skillName;
                int basePwr = _isOwenDefending ? 8 : _currentOwenSkill.basePower;
                int cCount = _isOwenDefending ? 1 : _currentOwenSkill.coinCount;
                owenHeader.DisplaySkill(skillTitle, basePwr, cCount);
            }

            if (enemyHeader != null && _currentEnemySkill != null)
            {
                enemyHeader.SetVisible(true);
                enemyHeader.DisplaySkill(_currentEnemySkill.skillName, _currentEnemySkill.basePower, _currentEnemySkill.coinCount);
            }
        }

        private void Update()
        {
            if (_speechBarkTimer > 0f)
            {
                _speechBarkTimer -= Time.deltaTime;
            }

            switch (_menuState)
            {
                case BattleMenuState.CommandSelect:
                    HandleCommandNavigation();
                    break;

                case BattleMenuState.SkillSelect:
                    HandleSkillMenu();
                    break;

                case BattleMenuState.TalkSelect:
                    HandleTalkMenu();
                    break;

                case BattleMenuState.TalkResponse:
                    if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.X))
                    {
                        _menuState = BattleMenuState.CommandSelect;
                    }
                    break;
            }
        }

        private void HandleCommandNavigation()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                _selectedCommandIndex = (_selectedCommandIndex - 1 + _commandNames.Length) % _commandNames.Length;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                _selectedCommandIndex = (_selectedCommandIndex + 1) % _commandNames.Length;
            }

            if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return))
            {
                switch (_selectedCommandIndex)
                {
                    case 0: // SKILLS
                        _menuState = BattleMenuState.SkillSelect;
                        break;
                    case 1: // TALK
                        _menuState = BattleMenuState.TalkSelect;
                        _selectedTalkIndex = 0;
                        break;
                    case 2: // ITEM
                        Debug.Log("[DIGIPURITY] Item inventory empty.");
                        break;
                    case 3: // RUN
                        Debug.Log("[DIGIPURITY] Cannot run from corrupted entity!");
                        break;
                }
            }
        }

        private void HandleSkillMenu()
        {
            // Toggle Defend with Space or D
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.D))
            {
                _isOwenDefending = !_isOwenDefending;
                if (_isOwenDefending)
                {
                    _tensionChargeSystem.AddDefendTension();
                    owenAnimator?.PlayDefend();
                }
                else
                {
                    owenAnimator?.PlayIdle();
                }
                UpdateHeadersDisplay();
            }

            // Confirm skill -> Start Clash!
            if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return))
            {
                StartCoroutine(ExecuteClashSequence());
            }

            // Back to commands
            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Escape))
            {
                _menuState = BattleMenuState.CommandSelect;
            }
        }

        private void HandleTalkMenu()
        {
            if (activeEnemyData == null || activeEnemyData.talkOptions == null || activeEnemyData.talkOptions.Count == 0)
            {
                _menuState = BattleMenuState.CommandSelect;
                return;
            }

            int count = activeEnemyData.talkOptions.Count;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                _selectedTalkIndex = (_selectedTalkIndex - 1 + count) % count;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                _selectedTalkIndex = (_selectedTalkIndex + 1) % count;
            }

            if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return))
            {
                var opt = activeEnemyData.talkOptions[_selectedTalkIndex];
                _activeTalkResponse = opt.responseText;

                if (opt.tpReward != 0) _tensionChargeSystem.AddGrazeTension(opt.tpReward);
                if (opt.chargeDelta != 0) _tensionChargeSystem.ModifyCharge(opt.chargeDelta);

                _menuState = BattleMenuState.TalkResponse;
            }

            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Escape))
            {
                _menuState = BattleMenuState.CommandSelect;
            }
        }

        private IEnumerator ExecuteClashSequence()
        {
            _menuState = BattleMenuState.Clashing;

            int owenCoins = _isOwenDefending ? 1 : _currentOwenSkill.coinCount;
            int owenTotalCoins = owenCoins;
            int owenBasePower = _isOwenDefending ? 8 : _currentOwenSkill.basePower;
            int owenCoinPower = _isOwenDefending ? 2 : _currentOwenSkill.coinPower;

            int enemyCoins = _currentEnemySkill.coinCount;
            int enemyTotalCoins = enemyCoins;
            int enemyBasePower = _currentEnemySkill.basePower;
            int enemyCoinPower = _currentEnemySkill.coinPower;

            while (owenCoins > 0 && enemyCoins > 0)
            {
                // 1. Both rush towards center
                float rushTime = 0.25f;
                float elapsed = 0f;
                Vector3 owenStart = owenBattlePos;
                Vector3 enemyStart = enemyBattlePos;
                Vector3 owenClashTarget = centerClashPos + new Vector3(-0.8f, 0f, 0f);
                Vector3 enemyClashTarget = centerClashPos + new Vector3(0.8f, 0f, 0f);

                if (enemyAnimator != null) enemyAnimator.IsBobbing = false;

                while (elapsed < rushTime)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / rushTime;
                    if (owenAnimator != null) owenAnimator.transform.position = Vector3.Lerp(owenStart, owenClashTarget, t);
                    if (enemyAnimator != null) enemyAnimator.transform.position = Vector3.Lerp(enemyStart, enemyClashTarget, t);
                    yield return null;
                }

                // 2. Play attack & HOLD CLASH FRAME FOR 0.25 SECONDS!
                if (owenAnimator != null)
                {
                    owenAnimator.PlayClashAttack(clashHoldDuration: 0.25f);
                }

                yield return new WaitForSeconds(0.25f);

                // 3. Push back slightly
                elapsed = 0f;
                float pushTime = 0.15f;
                Vector3 owenPushed = centerClashPos + new Vector3(-1.8f, 0f, 0f);
                Vector3 enemyPushed = centerClashPos + new Vector3(1.8f, 0f, 0f);
                while (elapsed < pushTime)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / pushTime;
                    if (owenAnimator != null) owenAnimator.transform.position = Vector3.Lerp(owenClashTarget, owenPushed, t);
                    if (enemyAnimator != null) enemyAnimator.transform.position = Vector3.Lerp(enemyClashTarget, enemyPushed, t);
                    yield return null;
                }

                // 4. Spin coins and flip based on Charge
                float owenHeadsProb = CoinClashResolver.CalculateHeadsProbability(_tensionChargeSystem.CurrentCharge);
                float enemyHeadsProb = 0.50f;

                int owenRoundPower = owenBasePower;
                int enemyRoundPower = enemyBasePower;

                for (int c = 0; c < owenCoins; c++)
                {
                    bool heads = UnityEngine.Random.value < owenHeadsProb;
                    if (heads) owenRoundPower += owenCoinPower;
                    if (owenHeader != null)
                    {
                        yield return owenHeader.AnimateCoinRoll(c, heads, null);
                    }
                }
                owenHeader?.UpdatePowerNumber(owenRoundPower);

                for (int c = 0; c < enemyCoins; c++)
                {
                    bool heads = UnityEngine.Random.value < enemyHeadsProb;
                    if (heads) enemyRoundPower += enemyCoinPower;
                    if (enemyHeader != null)
                    {
                        yield return enemyHeader.AnimateCoinRoll(c, heads, null);
                    }
                }
                enemyHeader?.UpdatePowerNumber(enemyRoundPower);

                yield return new WaitForSeconds(0.35f);

                // 5. Compare Power & Shatter 1 Coin
                if (owenRoundPower > enemyRoundPower)
                {
                    enemyCoins--;
                    enemyHeader?.ShatterCoin(enemyCoins);
                    enemyAnimator?.PlayHurtShake(0.25f);
                }
                else if (enemyRoundPower > owenRoundPower)
                {
                    owenCoins--;
                    owenHeader?.ShatterCoin(owenCoins);
                    owenAnimator?.PlayHurt(0.25f);
                }

                yield return new WaitForSeconds(0.3f);
            }

            // Clash Resolved
            if (owenCoins > 0)
            {
                yield return ResolveOwenVictoryAttack(owenCoins, owenTotalCoins);
            }
            else
            {
                yield return ResolveEnemyVictoryBulletHell(enemyCoins, enemyTotalCoins);
            }
        }

        private IEnumerator ResolveOwenVictoryAttack(int survivingCoins, int totalCoins)
        {
            if (enemyAnimator != null) enemyAnimator.PlayHurtShake(0.6f);

            float headsProb = CoinClashResolver.CalculateHeadsProbability(_tensionChargeSystem.CurrentCharge);
            int headsRolled = 0;
            for (int i = 0; i < survivingCoins; i++)
            {
                if (UnityEngine.Random.value < headsProb) headsRolled++;
            }

            float damageRatio = Mathf.Clamp01((float)headsRolled / Mathf.Max(1, totalCoins));
            int maxPotentialDamage = 35;
            int finalDamage = Mathf.Max(5, Mathf.RoundToInt(maxPotentialDamage * (damageRatio > 0f ? damageRatio : 0.33f)));

            float elapsed = 0f;
            Vector3 startP = owenAnimator != null ? owenAnimator.transform.position : owenBattlePos;
            Vector3 targetP = enemyBattlePos + new Vector3(-1.2f, 0f, 0f);
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                if (owenAnimator != null) owenAnimator.transform.position = Vector3.Lerp(startP, targetP, elapsed / 0.2f);
                yield return null;
            }

            owenAnimator?.PlayClashAttack(0.2f);
            enemyAnimator?.PlayHurtShake(0.5f);
            enemyCurrentHp = Mathf.Max(0, enemyCurrentHp - finalDamage);

            _tensionChargeSystem.ModifyCharge(+15);

            yield return new WaitForSeconds(0.5f);

            if (owenAnimator != null) owenAnimator.transform.position = owenBattlePos;
            if (enemyAnimator != null) enemyAnimator.transform.position = enemyBattlePos;

            if (enemyCurrentHp <= 0)
            {
                _menuState = BattleMenuState.PurifyPurgePrompt;
            }
            else
            {
                _currentTurn++;
                StartNewTurn();
            }
        }

        private IEnumerator ResolveEnemyVictoryBulletHell(int survivingCoins, int totalCoins)
        {
            float elapsed = 0f;
            Vector3 startP = owenAnimator != null ? owenAnimator.transform.position : owenBattlePos;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                if (owenAnimator != null) owenAnimator.transform.position = Vector3.Lerp(startP, owenBattlePos, elapsed / 0.2f);
                yield return null;
            }
            if (owenAnimator != null) owenAnimator.transform.position = owenBattlePos;
            if (enemyAnimator != null) enemyAnimator.transform.position = enemyBattlePos;

            owenHeader?.FadeOutHeader(0.2f);
            enemyHeader?.FadeOutHeader(0.2f);
            yield return new WaitForSeconds(0.2f);

            // Proportional damage calculation
            float headsProb = 0.5f;
            int headsRolled = 0;
            for (int i = 0; i < survivingCoins; i++)
            {
                if (UnityEngine.Random.value < headsProb) headsRolled++;
            }
            float damageRatio = Mathf.Clamp01((float)headsRolled / Mathf.Max(1, totalCoins));
            int baseBulletDmg = activeEnemyData != null ? activeEnemyData.bulletDamage : 8;
            int finalBulletDmg = Mathf.Max(4, Mathf.RoundToInt(baseBulletDmg * 1.5f * (damageRatio > 0f ? damageRatio : 0.33f)));

            // Bullet Hell Phase
            _menuState = BattleMenuState.DodgeBoxPhase;
            bool dodgePhaseComplete = false;

            if (dodgeBox != null)
            {
                BulletPatternType pType = activeEnemyData != null ? activeEnemyData.primaryPattern : BulletPatternType.RainFromTop;
                Sprite bSprite = activeEnemyData != null ? activeEnemyData.bulletSprite : null;
                Color bCol = activeEnemyData != null ? activeEnemyData.bulletColor : Color.white;
                float bSpeed = activeEnemyData != null ? activeEnemyData.bulletSpeed : 3.2f;
                float duration = activeEnemyData != null ? activeEnemyData.dodgePhaseDuration : 5.0f;

                dodgeBox.StartDodgePhase(
                    duration,
                    pType,
                    bSprite,
                    bCol,
                    bSpeed,
                    onGraze: () => _tensionChargeSystem.AddGrazeTension(10f),
                    onPlayerHit: (dmg) =>
                    {
                        owenCurrentHp = Mathf.Max(0, owenCurrentHp - finalBulletDmg);
                        owenAnimator?.PlayHurt(0.3f);
                    },
                    onPhaseEnd: () => dodgePhaseComplete = true
                );
            }
            else
            {
                dodgePhaseComplete = true;
            }

            while (!dodgePhaseComplete) yield return null;

            if (owenCurrentHp <= 0)
            {
                owenAnimator?.PlayDowned();
                _menuState = BattleMenuState.BattleEnded;
            }
            else
            {
                _currentTurn++;
                StartNewTurn();
            }
        }

        private void OnGUI()
        {
            if (_menuState == BattleMenuState.BattleEnded) return;

            DrawPlayerFlankUI();
            DrawEnemyFlankUI();

            if (_speechBarkTimer > 0f && !string.IsNullOrEmpty(_currentSpeechBark))
            {
                DrawEnemySpeechBubble();
            }

            if (_menuState == BattleMenuState.CommandSelect || _menuState == BattleMenuState.SkillSelect ||
                _menuState == BattleMenuState.TalkSelect || _menuState == BattleMenuState.TalkResponse)
            {
                DrawBottomDashboard();
            }

            if (_menuState == BattleMenuState.PurifyPurgePrompt)
            {
                DrawPurifyPurgeModal();
            }
        }

        private void DrawEnemySpeechBubble()
        {
            // Speech bubble over enemy head
            Rect bubble = new Rect(Screen.width - 340, 180, 220, 60);
            GUI.color = Color.white;
            GUI.DrawTexture(bubble, Texture2D.whiteTexture);

            GUI.color = Color.black;
            GUI.Label(new Rect(bubble.x + 10, bubble.y + 10, 200, 40), $"* \"{_currentSpeechBark}\"");
            GUI.color = Color.white;
        }

        private void DrawPlayerFlankUI()
        {
            float tpPercent = _tensionChargeSystem != null ? _tensionChargeSystem.TensionPercentage : 0f;
            int owenCharge = _tensionChargeSystem != null ? _tensionChargeSystem.CurrentCharge : 0;

            Rect tpBgRect = new Rect(20, 80, 28, 220);
            GUI.color = new Color(0.1f, 0.05f, 0.02f, 0.9f);
            GUI.DrawTexture(tpBgRect, Texture2D.whiteTexture);

            float fillH = (tpPercent / 100f) * 216f;
            Rect tpFillRect = new Rect(22, 80 + (216f - fillH) + 2f, 24, fillH);
            GUI.color = Color.Lerp(new Color(1f, 0.5f, 0f), new Color(1f, 0.85f, 0.2f), tpPercent / 100f);
            GUI.DrawTexture(tpFillRect, Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(new Rect(16, 55, 60, 25), $"TP {Mathf.RoundToInt(tpPercent)}%");

            Rect chgRect = new Rect(70, 70, 160, 32);
            GUI.color = new Color(0.05f, 0.05f, 0.12f, 0.85f);
            GUI.DrawTexture(chgRect, Texture2D.whiteTexture);

            GUI.color = owenCharge >= 0 ? new Color(0.2f, 0.8f, 1f) : new Color(0.9f, 0.3f, 0.3f);
            GUI.Label(new Rect(75, 75, 150, 25), $"CHARGE: {(owenCharge >= 0 ? "+" : "")}{owenCharge} / 45");
            GUI.color = Color.white;
        }

        private void DrawEnemyFlankUI()
        {
            Rect eChgRect = new Rect(Screen.width - 230, 70, 160, 32);
            GUI.color = new Color(0.12f, 0.05f, 0.05f, 0.85f);
            GUI.DrawTexture(eChgRect, Texture2D.whiteTexture);

            GUI.color = new Color(1f, 0.6f, 0.2f);
            GUI.Label(new Rect(Screen.width - 225, 75, 150, 25), "CHARGE: 0 / 45");

            Rect eHpBg = new Rect(Screen.width - 230, 110, 200, 18);
            GUI.color = new Color(0.2f, 0.05f, 0.05f);
            GUI.DrawTexture(eHpBg, Texture2D.whiteTexture);

            float eHpRatio = (float)enemyCurrentHp / Mathf.Max(1, enemyMaxHp);
            Rect eHpFill = new Rect(Screen.width - 230, 110, 200 * eHpRatio, 18);
            GUI.color = new Color(1f, 0.25f, 0.25f);
            GUI.DrawTexture(eHpFill, Texture2D.whiteTexture);

            Rect stgMarker = new Rect(Screen.width - 230 + (200 * 0.5f) - 1, 108, 3, 22);
            GUI.color = Color.yellow;
            GUI.DrawTexture(stgMarker, Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width - 230, 132, 200, 25), $"{activeEnemyName.ToUpper()} HP {enemyCurrentHp} / {enemyMaxHp}");
        }

        private void DrawBottomDashboard()
        {
            float panelH = 150f;
            float panelY = Screen.height - panelH - 15f;
            float panelW = Screen.width - 40f;

            Rect bg = new Rect(20, panelY, panelW, panelH);
            GUI.color = new Color(0.04f, 0.02f, 0.08f, 0.94f);
            GUI.DrawTexture(bg, Texture2D.whiteTexture);

            // Owen Status Card (Left third)
            GUI.color = Color.white;
            GUI.Label(new Rect(35, panelY + 12, 140, 25), "OWEN  LV 1");

            Rect hpBg = new Rect(35, panelY + 40, 180, 16);
            GUI.color = new Color(0.3f, 0.05f, 0.05f);
            GUI.DrawTexture(hpBg, Texture2D.whiteTexture);

            float hpRatio = (float)owenCurrentHp / owenMaxHp;
            Rect hpFill = new Rect(35, panelY + 40, 180 * hpRatio, 16);
            GUI.color = hpRatio > 0.25f ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.9f, 0.2f, 0.2f);
            GUI.DrawTexture(hpFill, Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(new Rect(35, panelY + 60, 180, 25), $"HP {owenCurrentHp} / {owenMaxHp}");

            // Command / Submenu
            if (_menuState == BattleMenuState.CommandSelect)
            {
                float btnX = 260f;
                float btnSpacing = (panelW - 280f) / _commandNames.Length;
                for (int i = 0; i < _commandNames.Length; i++)
                {
                    bool isSelected = i == _selectedCommandIndex;
                    string prefix = isSelected ? "> " : "  ";
                    GUI.color = isSelected ? new Color(0.3f, 1f, 1f) : Color.white;
                    GUI.Label(new Rect(btnX + (i * btnSpacing), panelY + 45, 120, 30), $"{prefix}{_commandNames[i]}");
                }
            }
            else if (_menuState == BattleMenuState.SkillSelect)
            {
                string skillTitle = _isOwenDefending ? "BARRIER GUARD (DEFENSE)" : _currentOwenSkill.skillName;
                int basePwr = _isOwenDefending ? 8 : _currentOwenSkill.basePower;
                int cCount = _isOwenDefending ? 1 : _currentOwenSkill.coinCount;
                int cPwr = _isOwenDefending ? 2 : _currentOwenSkill.coinPower;

                GUI.color = new Color(0.2f, 1f, 1f);
                GUI.Label(new Rect(260, panelY + 15, 300, 25), $"ACTIVE SKILL: {skillTitle}");

                GUI.color = Color.white;
                GUI.Label(new Rect(260, panelY + 42, 350, 25), $"Base Power: {basePwr}   |   Coins: {cCount} (Power +{cPwr} on Heads)");
                GUI.Label(new Rect(260, panelY + 68, 380, 25), "[SPACE / D] Swap Attack <-> Defense   |   [ENTER / Z] Confirm Clash");

                GUI.color = Color.green;
                GUI.Label(new Rect(260, panelY + 98, 200, 25), "FORECAST: FAVORED");
            }
            else if (_menuState == BattleMenuState.TalkSelect)
            {
                GUI.color = new Color(1f, 0.9f, 0.3f);
                GUI.Label(new Rect(260, panelY + 12, 300, 25), "TALK / ACT:");

                if (activeEnemyData != null && activeEnemyData.talkOptions != null)
                {
                    for (int i = 0; i < activeEnemyData.talkOptions.Count; i++)
                    {
                        bool sel = i == _selectedTalkIndex;
                        GUI.color = sel ? new Color(0.3f, 1f, 1f) : Color.white;
                        GUI.Label(new Rect(260, panelY + 38 + (i * 24), 300, 25), $"{(sel ? "> " : "  ")}{activeEnemyData.talkOptions[i].optionName}");
                    }
                }
            }
            else if (_menuState == BattleMenuState.TalkResponse)
            {
                GUI.color = Color.white;
                GUI.Label(new Rect(260, panelY + 25, panelW - 280, 70), $"* {_activeTalkResponse}");
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                GUI.Label(new Rect(260, panelY + 105, 200, 25), "[Z / ENTER] Continue");
            }

            GUI.color = Color.white;
        }

        private void DrawPurifyPurgeModal()
        {
            Rect box = new Rect((Screen.width - 380) * 0.5f, (Screen.height - 180) * 0.5f, 380, 180);
            GUI.color = new Color(0.08f, 0.04f, 0.12f, 0.98f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(new Rect(box.x + 30, box.y + 20, 320, 30), $"{activeEnemyName.ToUpper()} DEFEATED — CHOOSE FATE:");

            GUI.color = new Color(0.3f, 1f, 1f);
            if (GUI.Button(new Rect(box.x + 30, box.y + 65, 320, 35), "[1] PURIFY (Peaceful Rest - 1.0x EXP)"))
            {
                Debug.Log($"[DIGIPURITY] {activeEnemyName} PURIFIED. System integrity restored.");
                _menuState = BattleMenuState.BattleEnded;
            }

            GUI.color = new Color(1f, 0.35f, 0.35f);
            if (GUI.Button(new Rect(box.x + 30, box.y + 115, 320, 35), "[2] PURGE (Ruthless Deletion - 1.5x EXP)"))
            {
                Debug.Log($"[DIGIPURITY] {activeEnemyName} PURGED. Network destabilization increased.");
                _menuState = BattleMenuState.BattleEnded;
            }

            GUI.color = Color.white;
        }
    }
}
