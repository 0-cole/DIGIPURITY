using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigiPurity.Combat
{
    [Serializable]
    public class SkillData
    {
        public string skillName = "Purify Pulse";
        public int basePower = 4;
        public int coinPower = 3;
        public int coinCount = 2;
    }

    public struct CoinFlipResult
    {
        public bool isHeads;
        public int contribution;
    }

    public struct ClashRoundResult
    {
        public int playerPower;
        public int enemyPower;
        public int playerCoinsRemaining;
        public int enemyCoinsRemaining;
        public bool isDraw;
        public bool playerWonRound;
    }

    public class CoinClashResolver
    {
        private readonly System.Random _random;

        public CoinClashResolver(int? seed = null)
        {
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        public static float CalculateHeadsProbability(int sanity)
        {
            int clampedSanity = Mathf.Clamp(sanity, -45, 45);
            float prob = 0.50f + (clampedSanity / 100.0f);
            return Mathf.Clamp(prob, 0.05f, 0.95f);
        }

        public int RollSkillPower(SkillData skill, int sanity, int activeCoins, out List<CoinFlipResult> flips)
        {
            flips = new List<CoinFlipResult>();
            int total = skill.basePower;
            float headsChance = CalculateHeadsProbability(sanity);

            for (int i = 0; i < activeCoins; i++)
            {
                bool heads = _random.NextDouble() < headsChance;
                int added = heads ? skill.coinPower : 0;
                total += added;
                flips.Add(new CoinFlipResult { isHeads = heads, contribution = added });
            }

            return total;
        }

        public ClashRoundResult ResolveClashRound(SkillData pSkill, int pSanity, ref int pCoins,
                                                  SkillData eSkill, int eSanity, ref int eCoins)
        {
            int pPower = RollSkillPower(pSkill, pSanity, pCoins, out _);
            int ePower = RollSkillPower(eSkill, eSanity, eCoins, out _);

            bool isDraw = pPower == ePower;
            bool pWon = pPower > ePower;

            if (!isDraw)
            {
                if (pWon)
                {
                    eCoins = Mathf.Max(0, eCoins - 1);
                }
                else
                {
                    pCoins = Mathf.Max(0, pCoins - 1);
                }
            }

            return new ClashRoundResult
            {
                playerPower = pPower,
                enemyPower = ePower,
                playerCoinsRemaining = pCoins,
                enemyCoinsRemaining = eCoins,
                isDraw = isDraw,
                playerWonRound = pWon
            };
        }
    }
}
