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
            if (defaultGolferSprite == null)
            {
                defaultGolferSprite = OwenBattleAnimator.LoadSpriteDirect("Assets/Sprites/Golfer/Golfer_256.png", new Vector2(0.5f, 0f), 100f);
                if (defaultGolferSprite == null)
                {
                    defaultGolferSprite = OwenBattleAnimator.LoadSpriteDirect("Assets/Sprites/Golfer/Golfer.png", new Vector2(0.5f, 0f), 100f);
                }
            }

            if (defaultGolferSprite != null)
            {
                SetEnemyVisuals(defaultGolferSprite, null, 4.35f);
            }
        }
    }
}
