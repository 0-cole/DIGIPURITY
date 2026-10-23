using System;
using UnityEngine;

namespace DigiPurity.Combat
{
    public class GolferBattleAnimator : EnemyBattleAnimator
    {
        [Header("Golfer Auto-Load Fallback")]
        [SerializeField] private Sprite defaultGolferSprite;

        private void Start()
        {
            AutoLoadSprite();
        }

        public void AutoLoadSprite()
        {
#if UNITY_EDITOR
            if (defaultGolferSprite == null)
            {
                defaultGolferSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Golfer/Golfer_256.png");
                if (defaultGolferSprite == null)
                {
                    defaultGolferSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Golfer/Golfer.png");
                }
            }
#endif
            if (defaultGolferSprite != null)
            {
                SetEnemyVisuals(defaultGolferSprite, null, 1.0f);
            }
        }
    }
}
