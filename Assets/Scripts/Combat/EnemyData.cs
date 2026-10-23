using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigiPurity.Combat
{
    public enum BulletPatternType
    {
        RainFromTop,        // Vertical falling projectiles (e.g. golf balls, rain)
        AimedSpreadBurst,   // Aimed fan shot at Owen's soul
        AlternatingSide,    // Left-right crossfire
        RadialSpiral        // Expanding ring of bullets
    }

    [Serializable]
    public class TalkOption
    {
        public string optionName = "Check";
        [TextArea(2, 4)]
        public string responseText = "GOLFER - ATK 4 DEF 2. An obsolete virtual athlete puppeteered by rogue subroutines.";
        public int tpReward = 0;
        public int chargeDelta = 0;
    }

    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "DIGIPURITY/Enemy Data", order = 10)]
    public class EnemyData : ScriptableObject
    {
        [Header("General Identity")]
        public string enemyId = "golfer";
        public string enemyName = "Golfer";
        [TextArea(2, 3)]
        public string description = "A rogue sports simulation entity infected by primitive AI scripts.";

        [Header("Sprites & Visuals")]
        public Sprite overworldSprite;
        public Sprite battleSprite;
        public Sprite battleHurtSprite;
        public Sprite bulletSprite;
        public Color bulletColor = Color.white;
        public float battleSpriteScale = 1.0f;

        [Header("Audio")]
        public AudioClip battleBgm;
        public AudioClip attackSound;
        public AudioClip hurtSound;

        [Header("Stats & Power")]
        public int maxHp = 100;
        public int glitchThreshold = 50;
        public int basePower = 4;
        public int coinCount = 3;
        public int coinPower = 3;
        public string primarySkillName = "Putter Cleave";

        [Header("Deltarune-Style Dialogue & Speech Bubbles")]
        [TextArea(1, 2)]
        public string[] inBattleBarks = new string[]
        {
            "PAR FOR THE COURSE.",
            "NICE SHOT, OWEN.",
            "OUT OF BOUNDS ERROR.",
            "FORE! SYSTEM CORRUPT."
        };

        [Header("TALK / ACT Options")]
        public List<TalkOption> talkOptions = new List<TalkOption>()
        {
            new TalkOption
            {
                optionName = "Check",
                responseText = "GOLFER - ATK 4 DEF 2. Mindlessly repeating the 18th hole from 2004."
            },
            new TalkOption
            {
                optionName = "Analyze Swing",
                responseText = "You pointed out a hitch in his backswing. Golfer looks perturbed (+15 TP)!",
                tpReward = 38 // 15%
            },
            new TalkOption
            {
                optionName = "Heckle",
                responseText = "You yelled during his putting routine. His Charge dropped!",
                chargeDelta = -15
            }
        };

        [Header("Bullet Hell Pattern")]
        public BulletPatternType primaryPattern = BulletPatternType.RainFromTop;
        public float bulletSpeed = 3.2f;
        public int bulletDamage = 8;
        public float dodgePhaseDuration = 5.0f;
    }
}
