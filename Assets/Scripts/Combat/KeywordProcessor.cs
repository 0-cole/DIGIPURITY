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

        public KeywordStatus GetStatus(KeywordType type)
        {
            _statuses.TryGetValue(type, out var s);
            return s;
        }

        public int ProcessTurnEndBurn(bool hasCobaltCombustion = false)
        {
            if (_statuses.TryGetValue(KeywordType.Burn, out var burn) && burn.count > 0)
            {
                int baseDamage = burn.potency;
                burn.count--;

                // Hybrid: Cobalt Combustion (Blue Burn)
                // Consumes Poise Count to deal guaranteed Critical Burn damage (x1.5)
                if (hasCobaltCombustion && _statuses.TryGetValue(KeywordType.Poise, out var poise) && poise.count > 0)
                {
                    poise.count--;
                    if (poise.count <= 0) _statuses.Remove(KeywordType.Poise);
                    baseDamage = Mathf.RoundToInt(baseDamage * 1.5f);
                }

                if (burn.count <= 0) _statuses.Remove(KeywordType.Burn);
                return baseDamage;
            }
            return 0;
        }

        public int ProcessCoinFlipBleed(bool hasCrimsonLacerate = false, bool isCrit = false)
        {
            if (_statuses.TryGetValue(KeywordType.Bleed, out var bleed) && bleed.count > 0)
            {
                int damage = bleed.potency;
                bleed.count--;

                // Hybrid: Crimson Lacerate (Poise + Bleed)
                // Critical coin flips cause double bleed damage
                if (hasCrimsonLacerate && isCrit)
                {
                    damage *= 2;
                }

                if (bleed.count <= 0) _statuses.Remove(KeywordType.Bleed);
                return damage;
            }
            return 0;
        }

        public bool CheckPoiseCrit(System.Random rng)
        {
            if (_statuses.TryGetValue(KeywordType.Poise, out var poise) && poise.count > 0)
            {
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

        public void ProcessDrainageAndStutter(int drainedCharge, bool hasStaticSeizure, StaggerAndGlitchSystem enemyGlitchSystem)
        {
            // Hybrid: Static Seizure (Drainage + Stutter)
            // When draining enemy charge, directly elevate their Glitch threshold
            if (hasStaticSeizure && enemyGlitchSystem != null)
            {
                enemyGlitchSystem.TriggerStutterBurst(drainedCharge);
            }
        }
    }
}
