using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigiPurity.Combat
{
    public class KeywordProcessor
    {
        private readonly Dictionary<KeywordType, KeywordStatus> _statuses = new Dictionary<KeywordType, KeywordStatus>();

        public void ApplyKeyword(KeywordType type, int potency, int count)
        {
            if (type == KeywordType.None) return;

            if (_statuses.TryGetValue(type, out var current))
            {
                current.potency += potency;
                current.count += count;
            }
            else
            {
                _statuses[type] = new KeywordStatus(type, potency, count);
            }
        }

        public int ProcessTurnEndBurn()
        {
            if (_statuses.TryGetValue(KeywordType.Burn, out var burn) && burn.count > 0)
            {
                int damage = burn.potency;
                burn.count--;
                if (burn.count <= 0) _statuses.Remove(KeywordType.Burn);
                return damage;
            }
            return 0;
        }

        public int ProcessCoinFlipBleed()
        {
            if (_statuses.TryGetValue(KeywordType.Bleed, out var bleed) && bleed.count > 0)
            {
                int damage = bleed.potency;
                bleed.count--;
                if (bleed.count <= 0) _statuses.Remove(KeywordType.Bleed);
                return damage;
            }
            return 0;
        }

        public bool CheckPoiseCrit(System.Random rng)
        {
            if (_statuses.TryGetValue(KeywordType.Poise, out var poise) && poise.count > 0)
            {
                // Potency acts as critical percentage
                double roll = rng.NextDouble() * 100.0;
                if (roll < poise.potency)
                {
                    poise.count--;
                    if (poise.count <= 0) _statuses.Remove(KeywordType.Poise);
                    return true;
                }
            }
            return false;
        }
    }
}
