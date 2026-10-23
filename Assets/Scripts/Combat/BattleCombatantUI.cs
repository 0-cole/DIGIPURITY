using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DigiPurity.Combat
{
    public class BattleCombatantUI : MonoBehaviour
    {
        [Header("Positioning")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.2f, 0f);

        [Header("Sprites")]
        [SerializeField] private Sprite coinHeads;
        [SerializeField] private Sprite coinTails;
        [SerializeField] private Sprite coinBroken;
        [SerializeField] private Sprite[] coinSpinFrames;
        [SerializeField] private Sprite swordIcon;

        private GameObject _headerRoot;
        private SpriteRenderer _slotIconRenderer;
        private TextMesh _skillNameText;
        private TextMesh _powerText;
        private readonly List<SpriteRenderer> _coinRenderers = new List<SpriteRenderer>();

        private void Awake()
        {
            AutoLoadSprites();
            BuildHeaderHierarchy();
        }

        public void AutoLoadSprites()
        {
#if UNITY_EDITOR
            if (coinHeads == null) coinHeads = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Combat/Coin_Heads.png");
            if (coinTails == null) coinTails = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Combat/Coin_Tails.png");
            if (coinBroken == null) coinBroken = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Combat/Coin_Broken.png");
            if (swordIcon == null) swordIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Combat/Icon_Sword.png");

            if (coinSpinFrames == null || coinSpinFrames.Length == 0)
            {
                coinSpinFrames = new Sprite[4];
                for (int i = 0; i < 4; i++)
                {
                    coinSpinFrames[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Combat/Coin_Spin_{i}.png");
                }
            }
#endif
        }

        private void BuildHeaderHierarchy()
        {
            if (_headerRoot != null) return;

            _headerRoot = new GameObject("OverheadHeader");
            _headerRoot.transform.SetParent(transform);
            _headerRoot.transform.localPosition = offset;

            // 1. Skill Icon Badge
            GameObject iconObj = new GameObject("SlotIcon");
            iconObj.transform.SetParent(_headerRoot.transform);
            iconObj.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            iconObj.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            _slotIconRenderer = iconObj.AddComponent<SpriteRenderer>();
            _slotIconRenderer.sprite = swordIcon;
            _slotIconRenderer.sortingOrder = 80;

            // 2. Skill Name Text
            GameObject nameObj = new GameObject("SkillName");
            nameObj.transform.SetParent(_headerRoot.transform);
            nameObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            nameObj.transform.localScale = new Vector3(0.08f, 0.08f, 1f);
            _skillNameText = nameObj.AddComponent<TextMesh>();
            _skillNameText.alignment = TextAlignment.Center;
            _skillNameText.anchor = TextAnchor.MiddleCenter;
            _skillNameText.fontSize = 32;
            _skillNameText.color = Color.white;
            var mr = nameObj.GetComponent<MeshRenderer>();
            mr.sortingOrder = 85;

            // 3. Power Text
            GameObject powerObj = new GameObject("PowerNumber");
            powerObj.transform.SetParent(_headerRoot.transform);
            powerObj.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            powerObj.transform.localScale = new Vector3(0.12f, 0.12f, 1f);
            _powerText = powerObj.AddComponent<TextMesh>();
            _powerText.alignment = TextAlignment.Center;
            _powerText.anchor = TextAnchor.MiddleCenter;
            _powerText.fontSize = 42;
            _powerText.color = new Color(1f, 0.9f, 0.2f, 1f); // Gold
            var mrPow = powerObj.GetComponent<MeshRenderer>();
            mrPow.sortingOrder = 85;
        }

        public void DisplaySkill(string skillName, int basePower, int coinCount, Sprite icon = null)
        {
            if (_headerRoot == null) BuildHeaderHierarchy();
            _headerRoot.SetActive(true);

            _skillNameText.text = skillName.ToUpper();
            _powerText.text = basePower.ToString();
            if (icon != null) _slotIconRenderer.sprite = icon;

            SetupCoinSlots(coinCount);
        }

        private void SetupCoinSlots(int count)
        {
            // Clear existing coin renderers
            foreach (var r in _coinRenderers)
            {
                if (r != null) Destroy(r.gameObject);
            }
            _coinRenderers.Clear();

            float spacing = 0.38f;
            float startX = -((count - 1) * spacing * 0.5f);

            for (int i = 0; i < count; i++)
            {
                GameObject cObj = new GameObject($"CoinSlot_{i}");
                cObj.transform.SetParent(_headerRoot.transform);
                cObj.transform.localPosition = new Vector3(startX + (i * spacing), 0f, 0f);
                cObj.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

                var sr = cObj.AddComponent<SpriteRenderer>();
                sr.sprite = coinTails; // Default unspun socket
                sr.color = new Color(0.7f, 0.7f, 0.7f, 0.9f); // Greyed out socket
                sr.sortingOrder = 82;
                _coinRenderers.Add(sr);
            }
        }

        public IEnumerator AnimateCoinRoll(int coinIndex, bool isHeads, Action onLanded)
        {
            if (coinIndex < 0 || coinIndex >= _coinRenderers.Count) yield break;

            SpriteRenderer sr = _coinRenderers[coinIndex];
            if (sr == null) yield break;

            // Spin animation
            if (coinSpinFrames != null && coinSpinFrames.Length > 0)
            {
                for (int spin = 0; spin < 3; spin++)
                {
                    for (int f = 0; f < coinSpinFrames.Length; f++)
                    {
                        sr.sprite = coinSpinFrames[f];
                        yield return new WaitForSeconds(0.04f);
                    }
                }
            }

            // Land on Heads or Tails
            sr.sprite = isHeads ? coinHeads : coinTails;
            sr.color = isHeads ? Color.white : new Color(0.5f, 0.5f, 0.6f, 1f);

            onLanded?.Invoke();
        }

        public void ShatterCoin(int coinIndex)
        {
            if (coinIndex >= 0 && coinIndex < _coinRenderers.Count)
            {
                var sr = _coinRenderers[coinIndex];
                if (sr != null)
                {
                    sr.sprite = coinBroken;
                    sr.color = new Color(1f, 0.3f, 0.3f, 1f); // Red broken tint
                }
            }
        }

        public void UpdatePowerNumber(int currentPower)
        {
            if (_powerText != null)
            {
                _powerText.text = currentPower.ToString();
            }
        }

        public void SetVisible(bool visible)
        {
            if (_headerRoot != null)
            {
                _headerRoot.SetActive(visible);
            }
        }

        public void FadeOutHeader(float duration = 0.25f)
        {
            StartCoroutine(FadeOutRoutine(duration));
        }

        private IEnumerator FadeOutRoutine(float duration)
        {
            float elapsed = 0f;
            Vector3 originalScale = _headerRoot.transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float s = Mathf.Lerp(1f, 0f, elapsed / duration);
                _headerRoot.transform.localScale = originalScale * s;
                yield return null;
            }

            _headerRoot.SetActive(false);
            _headerRoot.transform.localScale = originalScale;
        }
    }
}
