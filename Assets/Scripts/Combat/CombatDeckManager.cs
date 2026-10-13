using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigiPurity.Combat
{
    public enum KeywordType
    {
        None,
        Burn,
        Bleed,
        Poise,
        Drainage,
        Stutter // DIGIPURITY adaptation of Tremor
    }

    [Serializable]
    public class KeywordStatus
    {
        public KeywordType type;
        public int potency;
        public int count;

        public KeywordStatus(KeywordType t, int pot, int cnt)
        {
            type = t;
            potency = pot;
            count = cnt;
        }
    }

    public enum SkillTier
    {
        Skill1_PureDamage,
        Skill2_HighPowerRare,
        DefenseSkill,
        Skill3_KeywordSpecialist,  // Unlocked based on your dominant build keyword
        Skill4_UltimateMove        // Rare draw, consumes Charge, massive keyword payload
    }

    [Serializable]
    public class CombatSkill
    {
        public string skillName = "Basic Purify";
        public SkillTier tier = SkillTier.Skill1_PureDamage;
        public int basePower = 4;
        public int coinPower = 3;
        public int coinCount = 2;
        public int chargeCost = 0; // For Ultimate moves (Skill 4)
        public List<KeywordStatus> appliedKeywords = new List<KeywordStatus>();

        public static CombatSkill CreateSkill1()
        {
            return new CombatSkill
            {
                skillName = "Purify Bolt",
                tier = SkillTier.Skill1_PureDamage,
                basePower = 4,
                coinPower = 3,
                coinCount = 2
            };
        }

        public static CombatSkill CreateSkill2()
        {
            return new CombatSkill
            {
                skillName = "Overclocked Purge",
                tier = SkillTier.Skill2_HighPowerRare,
                basePower = 6,
                coinPower = 5,
                coinCount = 3
            };
        }

        public static CombatSkill CreateDefense()
        {
            return new CombatSkill
            {
                skillName = "Barrier Guard",
                tier = SkillTier.DefenseSkill,
                basePower = 8,
                coinPower = 2,
                coinCount = 1
            };
        }

        public static CombatSkill CreateSkill3(KeywordType affinity)
        {
            var skill = new CombatSkill
            {
                skillName = $"{affinity} Protocol",
                tier = SkillTier.Skill3_KeywordSpecialist,
                basePower = 7,
                coinPower = 4,
                coinCount = 3
            };
            skill.appliedKeywords.Add(new KeywordStatus(affinity, pot: 5, cnt: 3));
            return skill;
        }

        public static CombatSkill CreateSkill4Ultimate(KeywordType affinity, int cost = 20)
        {
            var skill = new CombatSkill
            {
                skillName = $"TERMINAL PURGE: {affinity.ToString().ToUpper()}",
                tier = SkillTier.Skill4_UltimateMove,
                basePower = 15,
                coinPower = 6,
                coinCount = 4,
                chargeCost = cost
            };
            skill.appliedKeywords.Add(new KeywordStatus(affinity, pot: 12, cnt: 6));
            return skill;
        }
    }

    public class CombatDeckManager
    {
        private readonly List<CombatSkill> _deck = new List<CombatSkill>();
        private readonly System.Random _random;
        public KeywordType CurrentAffinity { get; set; } = KeywordType.None;
        public bool HasUnlockedSkill3 { get; set; } = false;
        public bool HasUnlockedSkill4 { get; set; } = false;

        public CombatDeckManager(int? seed = null)
        {
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            InitializeStandardDeck();
        }

        public void InitializeStandardDeck()
        {
            _deck.Clear();
            // Base deck: 3x Skill 1, 1x Skill 2, 2x Defense
            for (int i = 0; i < 3; i++) _deck.Add(CombatSkill.CreateSkill1());
            _deck.Add(CombatSkill.CreateSkill2());
            for (int i = 0; i < 2; i++) _deck.Add(CombatSkill.CreateDefense());

            if (HasUnlockedSkill3 && CurrentAffinity != KeywordType.None)
            {
                // Skill 3 is a specialized keyword skill
                _deck.Add(CombatSkill.CreateSkill3(CurrentAffinity));
            }

            if (HasUnlockedSkill4 && CurrentAffinity != KeywordType.None)
            {
                // Skill 4 is an ultimate move that appears very rarely
                _deck.Add(CombatSkill.CreateSkill4Ultimate(CurrentAffinity));
            }
        }

        public CombatSkill DrawSkill()
        {
            if (_deck.Count == 0) InitializeStandardDeck();
            int idx = _random.Next(_deck.Count);
            return _deck[idx];
        }
    }
}
