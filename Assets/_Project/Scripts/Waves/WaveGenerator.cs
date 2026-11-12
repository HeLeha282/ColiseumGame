using System.Collections.Generic;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Генератор состава волн по «бюджету угрозы»: бюджет = base × growth^(n−1) × ритм «пилы».
    /// Им собираются волны 11–30 кампании (в генераторе прототипа, в ассеты) и волны бесконечного режима (во время игры).
    /// </summary>
    public static class WaveGenerator
    {
        /// <summary>Ритм внутри каждых 5 волн: обычная, обычная, пик, передышка, босс.</summary>
        public static readonly float[] Rhythm = { 1.0f, 1.1f, 1.35f, 0.8f, 0.7f };

        public class Entry
        {
            public EnemyConfig enemy;
            public float cost;
            public int unlockWave;
            public float weight = 1f;
            public bool squad;
            public int minCount = 1;
        }

        public static float Budget(int wave, float baseBudget = 90f, float growth = 1.075f)
            => baseBudget * Mathf.Pow(growth, wave - 1) * Rhythm[(wave - 1) % 5];

        public static float HpMultiplier(int wave) => 1f + 0.09f * (wave - 1);

        /// <summary>Потолок размера одной группы — чтобы волна не превращалась в сотни врагов.</summary>
        public const int MaxGroup = 24;

        /// <summary>Собрать группы на бюджет из доступных к этой волне врагов.</summary>
        public static List<SpawnGroup> Compose(int wave, float budget, List<Entry> pool, System.Random rng, int act)
        {
            var groups = new List<SpawnGroup>();
            var available = pool.FindAll(e => e.unlockWave <= wave);
            if (available.Count == 0) return groups;
            // Основа волны — легионеры (25% бюджета), остальное — смесь ролей
            var basic = available[0];
            float spent = 0f;
            int basicCount = Mathf.Clamp(Mathf.RoundToInt(budget * 0.25f / basic.cost), 4, MaxGroup);
            groups.Add(new SpawnGroup { enemy = basic.enemy, count = basicCount, interval = Mathf.Clamp(14f / basicCount, 0.6f, 2f) });
            spent += basicCount * basic.cost;

            int kinds = Mathf.Min(available.Count - 1, 2 + act);
            var chosen = new List<Entry>();
            var candidates = available.GetRange(1, available.Count - 1);
            for (int i = 0; i < kinds && candidates.Count > 0; i++)
            {
                float total = 0f;
                foreach (var c in candidates) total += c.weight * (c.unlockWave >= wave - 3 ? 2f : 1f);   // свежих врагов чаще
                float roll = (float)rng.NextDouble() * total;
                foreach (var c in candidates)
                {
                    roll -= c.weight * (c.unlockWave >= wave - 3 ? 2f : 1f);
                    if (roll <= 0f) { chosen.Add(c); candidates.Remove(c); break; }
                }
            }
            float delay = 4f;
            foreach (var c in chosen)
            {
                float share = (budget - spent) / Mathf.Max(1, chosen.Count - chosen.IndexOf(c));
                int count = Mathf.Clamp(Mathf.RoundToInt(share / c.cost), c.minCount, MaxGroup);
                if (count <= 0) continue;
                spent += count * c.cost;
                var g = new SpawnGroup
                {
                    enemy = c.enemy,
                    count = count,
                    startDelay = delay,
                    interval = c.squad ? 0.25f : Mathf.Clamp(16f / count, 0.5f, 4f),
                    squad = c.squad,
                    altRouteChance = act >= 2 ? 0.35f : 0f,
                    arch = c.squad ? rng.Next(0, 4) : -1
                };
                if (act >= 3 && rng.NextDouble() < 0.4)
                    g.elite = (EliteModifier)(1 << rng.Next(0, 5));
                groups.Add(g);
                delay += 3f;
            }
            return groups;
        }
    }
}
