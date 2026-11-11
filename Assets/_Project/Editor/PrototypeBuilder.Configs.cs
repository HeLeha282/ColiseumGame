using UnityEditor;
using UnityEngine;
using static ColiseumGame.EditorTools.BuilderUtil;

namespace ColiseumGame.EditorTools
{
    public static partial class PrototypeBuilder
    {
        public class ConfigSet
        {
            public GameBalance Balance;
            public PlayerConfig Player;
            public EnemyConfig Walker, Shield, Flyer, Boss;
            public TowerConfig Archer, Ballista;
        }

        /// <summary>Стартовый баланс. Создаётся один раз; дальше правится в инспекторе.</summary>
        private static ConfigSet CreateConfigs()
        {
            var c = new ConfigSet();
            c.Player = LoadOrCreate<PlayerConfig>(SODir + "/Player/PlayerConfig.asset", _ => { }, out _);

            c.Walker = LoadOrCreate<EnemyConfig>(SODir + "/Enemies/Enemy_Walker.asset", e =>
            {
                e.displayName = "Легионер";
                e.maxHP = 60f; e.moveSpeed = 3.5f;
                e.attackDamage = 10f; e.attackInterval = 1.2f; e.attackRange = 1.2f;
                e.essenceReward = 5;
                e.towerAggroRange = 6f; e.towerAggroChance = 0.15f;
            }, out _);

            c.Shield = LoadOrCreate<EnemyConfig>(SODir + "/Enemies/Enemy_ShieldBearer.asset", e =>
            {
                e.displayName = "Щитоносец";
                e.maxHP = 100f; e.moveSpeed = 2.8f;
                e.attackDamage = 14f; e.attackInterval = 1.4f; e.attackRange = 1.2f;
                e.essenceReward = 9;
                e.hasShield = true; e.shieldHalfAngle = 70f;
                e.towerAggroRange = 6f; e.towerAggroChance = 0.25f;
            }, out _);

            c.Flyer = LoadOrCreate<EnemyConfig>(SODir + "/Enemies/Enemy_Flyer.asset", e =>
            {
                e.displayName = "Гарпия";
                e.isFlying = true;
                e.maxHP = 40f; e.moveSpeed = 7f;
                e.attackDamage = 8f; e.attackInterval = 1f;
                e.essenceReward = 7;
                e.bodyRadius = 0.7f; e.aimHeight = 0f;
                e.cruiseHeight = 0f; e.hoverHeight = 2.5f; e.hoverRadius = 3f; e.diveDistance = 18f;
                e.towerAggroRange = 14f; e.towerAggroChance = 0.2f;
                e.playerAggroRange = 0f;
            }, out _);

            c.Boss = LoadOrCreate<EnemyConfig>(SODir + "/Enemies/Enemy_Boss.asset", e =>
            {
                e.displayName = "Минотавр";
                e.maxHP = 900f; e.armor = 300f; e.armorTowerDamageMultiplier = 0.15f;
                e.moveSpeed = 2.2f;
                e.attackDamage = 40f; e.attackInterval = 1.6f; e.attackRange = 1.4f;
                e.essenceReward = 100;
                e.playerAggroRange = 4f; e.playerLeashRange = 10f;
                e.towerAggroRange = 7f; e.towerAggroChance = 0.3f;
            }, out _);

            c.Archer = LoadOrCreate<TowerConfig>(SODir + "/Towers/Tower_Archer.asset", t =>
            {
                t.displayName = "Лучник";
                t.targetsGround = true; t.targetsAir = true; t.projectileSpeed = 45f;
                t.levels = new[]
                {
                    new TowerLevelStats { cost = 60, damage = 12f, fireInterval = 0.8f, range = 16f, maxHP = 150f },
                    new TowerLevelStats { cost = 50, damage = 18f, fireInterval = 0.65f, range = 18f, maxHP = 220f },
                    new TowerLevelStats { cost = 80, damage = 26f, fireInterval = 0.5f, range = 20f, maxHP = 300f },
                };
            }, out _);

            c.Ballista = LoadOrCreate<TowerConfig>(SODir + "/Towers/Tower_Ballista.asset", t =>
            {
                t.displayName = "Баллиста";
                t.targetsGround = true; t.targetsAir = false; t.projectileSpeed = 28f;
                t.levels = new[]
                {
                    new TowerLevelStats { cost = 90, damage = 30f, fireInterval = 2.2f, range = 18f, maxHP = 200f, aoeRadius = 3f },
                    new TowerLevelStats { cost = 70, damage = 45f, fireInterval = 1.9f, range = 20f, maxHP = 280f, aoeRadius = 3.5f },
                    new TowerLevelStats { cost = 110, damage = 65f, fireInterval = 1.6f, range = 22f, maxHP = 360f, aoeRadius = 4f },
                };
            }, out _);

            var waves = CreateWaves(c);

            c.Balance = LoadOrCreate<GameBalance>(SODir + "/GameBalance.asset", b =>
            {
                b.towerTypes = new[] { c.Archer, c.Ballista };
                b.waves = waves;
            }, out _);

            return c;
        }

        private static SpawnGroup G(EnemyConfig e, int count, float interval, float delay = 0f, int arch = -1)
            => new SpawnGroup { enemy = e, count = count, interval = interval, startDelay = delay, arch = arch };

        private static WaveConfig[] CreateWaves(ConfigSet c)
        {
            var W = c.Walker;
            var S = c.Shield;
            var F = c.Flyer;
            var B = c.Boss;
            var defs = new (float hp, int bonus, SpawnGroup[] groups)[]
            {
                (1.0f, 40, new[] { G(W, 8, 2.5f) }),
                (1.05f, 45, new[] { G(W, 10, 2.2f), G(S, 3, 5f, 8f) }),
                (1.1f, 50, new[] { G(W, 12, 2f), G(S, 4, 5f, 6f), G(F, 4, 4f, 10f) }),
                (1.15f, 55, new[] { G(W, 16, 1.8f), G(S, 6, 4f, 5f), G(F, 6, 3.5f, 8f) }),
                (1.2f, 90, new[] { G(B, 1, 1f, 4f, 0), G(W, 12, 2f), G(S, 4, 5f, 10f), G(F, 4, 4f, 12f) }),
                (1.3f, 65, new[] { G(W, 20, 1.5f), G(S, 8, 3.5f, 5f), G(F, 8, 3f, 8f) }),
                (1.4f, 70, new[] { G(W, 24, 1.4f), G(S, 10, 3f, 5f), G(F, 10, 3f, 6f) }),
                (1.5f, 75, new[] { G(W, 28, 1.2f), G(S, 12, 3f, 4f), G(F, 12, 2.5f, 6f) }),
                (1.65f, 80, new[] { G(W, 32, 1.1f), G(S, 14, 2.5f, 4f), G(F, 16, 2.2f, 5f) }),
                (1.8f, 150, new[] { G(B, 2, 20f, 3f), G(W, 30, 1.1f), G(S, 14, 2.5f, 6f), G(F, 16, 2.2f, 8f) }),
            };

            var result = new WaveConfig[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                result[i] = LoadOrCreate<WaveConfig>($"{SODir}/Waves/Wave_{i + 1:00}.asset", w =>
                {
                    w.hpMultiplier = d.hp;
                    w.completionBonus = d.bonus;
                    w.groups = d.groups;
                }, out _);
            }
            return result;
        }

        /// <summary>Прописать в конфиги ссылки на свежие префабы.</summary>
        private static void LinkPrefabs(ConfigSet c, PrefabSet p)
        {
            void Link(Object cfg, Object prefab)
            {
                Assign(cfg, ("prefab", prefab));
                EditorUtility.SetDirty(cfg);
            }
            Link(c.Walker, p.Walker.GetComponent<Enemy>());
            Link(c.Shield, p.Shield.GetComponent<Enemy>());
            Link(c.Flyer, p.Flyer.GetComponent<Enemy>());
            Link(c.Boss, p.Boss.GetComponent<Enemy>());
            Link(c.Archer, p.Archer.GetComponent<Tower>());
            Link(c.Ballista, p.Ballista.GetComponent<Tower>());
        }
    }
}
