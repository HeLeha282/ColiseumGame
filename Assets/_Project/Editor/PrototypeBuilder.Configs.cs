using System.Collections.Generic;
using System.Linq;
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
            public HeroConfig Hero;
            public readonly Dictionary<string, EnemyConfig> Enemies = new Dictionary<string, EnemyConfig>();
            public readonly Dictionary<string, TowerConfig> Towers = new Dictionary<string, TowerConfig>();
            public EnemyConfig E(string key) => Enemies[key];
        }

        // ---------------------------------------------------------------- враги

        /// <summary>Внешний вид врага из примитивов (модели будут позже).</summary>
        public class EnemyLook
        {
            public string key;
            public Color color;
            public float scale = 1f;
            public bool shield, bigShield, horns, hat, eye, staff, bow, wings, armor, flat, boxy, small, robe;
        }

        public static readonly List<EnemyLook> Looks = new List<EnemyLook>();

        private static EnemyConfig DefineEnemy(ConfigSet c, string key, EnemyLook look, System.Action<EnemyConfig> init)
        {
            var cfg = LoadOrCreate<EnemyConfig>($"{SODir}/Enemies/Enemy_{key}.asset", init, out _);
            c.Enemies[key] = cfg;
            look.key = key;
            Looks.RemoveAll(l => l.key == key);
            Looks.Add(look);
            return cfg;
        }

        private static void DefineEnemies(ConfigSet c)
        {
            DefineEnemy(c, "Walker", new EnemyLook { color = new Color(0.85f, 0.82f, 0.72f) }, e =>
            {
                e.displayName = "Скелет-легионер";
                e.description = "Основа войска. Идёт по маршруту и бьёт портал.";
                e.counterHint = "любые башни; лучник и катапульта справляются отлично.";
                e.maxHP = 60f; e.moveSpeed = 3.5f; e.attackDamage = 6f; e.attackInterval = 1.5f; e.attackRange = 1.2f;
                e.essenceReward = 4; e.gloryReward = 3;
            });
            DefineEnemy(c, "Satyr", new EnemyLook { color = new Color(0.55f, 0.4f, 0.25f), scale = 0.7f, horns = true }, e =>
            {
                e.displayName = "Сатир";
                e.description = "Маленький и очень быстрый. Бегает стаями и ПЕРЕПРЫГИВАЕТ баррикады.";
                e.counterHint = "скорострельный лучник, ледяная катапульта (замедление), жаровня у пути.";
                e.maxHP = 32f; e.moveSpeed = 6.2f; e.attackDamage = 4f; e.attackInterval = 1f; e.attackRange = 1f;
                e.jumpsBarricades = true; e.essenceReward = 3; e.gloryReward = 2;
            });
            DefineEnemy(c, "Cyclops", new EnemyLook { color = new Color(0.45f, 0.5f, 0.6f), scale = 1.6f, eye = true }, e =>
            {
                e.displayName = "Циклоп";
                e.description = "Толстый и сильный. Больно бьёт портал и отшвыривает героя.";
                e.counterHint = "метка героя (башни бьют ×1.5), огненная катапульта, Снайпер; задерживай баррикадами.";
                e.maxHP = 420f; e.moveSpeed = 2f; e.attackDamage = 25f; e.attackInterval = 2.2f; e.attackRange = 1.4f;
                e.knockbackPlayer = 9f; e.essenceReward = 16; e.gloryReward = 12;
            });
            DefineEnemy(c, "ShieldBearer", new EnemyLook { color = new Color(0.95f, 0.55f, 0.15f), shield = true }, e =>
            {
                e.displayName = "Щитоносец";
                e.description = "Щит спереди: башни, стреляющие ему в лицо, не наносят урона.";
                e.counterHint = "герой бьёт с любой стороны; башни, стреляющие в спину; Хрупкость (лёд+молния) пробивает щит.";
                e.maxHP = 100f; e.moveSpeed = 2.8f; e.attackDamage = 8f; e.attackInterval = 1.5f; e.attackRange = 1.2f;
                e.hasShield = true; e.essenceReward = 7; e.gloryReward = 5;
            });
            DefineEnemy(c, "Pyromancer", new EnemyLook { color = new Color(0.9f, 0.35f, 0.1f), hat = true, robe = true }, e =>
            {
                e.displayName = "Пиромант";
                e.description = "Огненный маг. Издалека бросает огненные шары по порталу и герою.";
                e.counterHint = "ЛЁД (×1.5): ледяная катапульта, ледяное зачарование. Огонь его не берёт.";
                e.element = Element.Fire;
                e.maxHP = 70f; e.moveSpeed = 3f; e.attackDamage = 10f; e.attackInterval = 2f; e.attackRange = 12f;
                e.projectileSpeed = 16f; e.splashRadius = 2.5f; e.essenceReward = 8; e.gloryReward = 6;
            });
            DefineEnemy(c, "Cryomancer", new EnemyLook { color = new Color(0.45f, 0.75f, 1f), hat = true, robe = true }, e =>
            {
                e.displayName = "Криомант";
                e.description = "Ледяной маг. Обстреливает издалека и замедляет героя.";
                e.counterHint = "ОГОНЬ (×1.5): огненная катапульта, жаровня, огненное зачарование. Лёд не действует.";
                e.element = Element.Ice;
                e.maxHP = 70f; e.moveSpeed = 3f; e.attackDamage = 9f; e.attackInterval = 2f; e.attackRange = 12f;
                e.projectileSpeed = 16f; e.splashRadius = 2.5f; e.slowPlayerOnHit = 0.4f; e.essenceReward = 8; e.gloryReward = 6;
            });
            DefineEnemy(c, "Centaur", new EnemyLook { color = new Color(0.6f, 0.45f, 0.3f), bow = true, boxy = true }, e =>
            {
                e.displayName = "Кентавр-лучник";
                e.description = "Быстрый стрелок: останавливается далеко от портала и обстреливает его, стреляет в героя.";
                e.counterHint = "Снайпер (дальность), Пробивная стрела, лук героя.";
                e.maxHP = 90f; e.moveSpeed = 4.5f; e.attackDamage = 7f; e.attackInterval = 1.6f; e.attackRange = 20f;
                e.projectileSpeed = 28f; e.essenceReward = 9; e.gloryReward = 7;
            });
            DefineEnemy(c, "Testudo", new EnemyLook { color = new Color(0.6f, 0.15f, 0.12f), bigShield = true }, e =>
            {
                e.displayName = "Тестудо";
                e.description = "Отряд щитоносцев в плотном строю — весь строй прикрыт спереди.";
                e.counterHint = "Метеор катапульты, Перегрузка (огонь+молния) по скоплению, башни сбоку и сзади, герой.";
                e.maxHP = 120f; e.moveSpeed = 2.6f; e.attackDamage = 8f; e.attackInterval = 1.5f; e.attackRange = 1.2f;
                e.hasShield = true; e.shieldHalfAngle = 80f; e.essenceReward = 6; e.gloryReward = 5;
            });
            DefineEnemy(c, "Priest", new EnemyLook { color = new Color(0.95f, 0.95f, 0.9f), staff = true, robe = true }, e =>
            {
                e.displayName = "Жрец Асклепия";
                e.description = "Лекарь: постоянно лечит всех врагов вокруг себя.";
                e.counterHint = "убивай первым (герой, Снайпер); Священное пламя жаровни глушит лечение.";
                e.maxHP = 80f; e.moveSpeed = 3f; e.attackDamage = 4f; e.attackInterval = 1.5f; e.attackRange = 1.2f;
                e.healRadius = 7f; e.healPercentPerSecond = 0.05f; e.essenceReward = 10; e.gloryReward = 8;
            });
            DefineEnemy(c, "Lemur", new EnemyLook { color = new Color(0.3f, 0.2f, 0.4f), robe = true }, e =>
            {
                e.displayName = "Лемур (тень)";
                e.description = "Невидимка: башни его не видят, пока его не раскроют.";
                e.counterHint = "удар героя раскрывает его на 5 с; Священное пламя жаровни подсвечивает тени рядом.";
                e.maxHP = 55f; e.moveSpeed = 4f; e.attackDamage = 7f; e.attackInterval = 1.4f; e.attackRange = 1.2f;
                e.invisible = true; e.essenceReward = 7; e.gloryReward = 6;
            });
            DefineEnemy(c, "Salamander", new EnemyLook { color = new Color(1f, 0.45f, 0.1f), flat = true }, e =>
            {
                e.displayName = "Саламандра";
                e.description = "Огненная ящерица. Огонь её не берёт; при смерти взрывается — опасна для баррикад и героя.";
                e.counterHint = "ЛЁД (×1.5); не добивай вплотную.";
                e.element = Element.Fire;
                e.maxHP = 60f; e.moveSpeed = 4.2f; e.attackDamage = 6f; e.attackInterval = 1.2f; e.attackRange = 1f;
                e.deathExplosionRadius = 3f; e.deathExplosionDamage = 25f; e.essenceReward = 6; e.gloryReward = 4;
            });
            DefineEnemy(c, "IceGolem", new EnemyLook { color = new Color(0.7f, 0.9f, 1f), scale = 1.4f, boxy = true }, e =>
            {
                e.displayName = "Ледяной голем";
                e.description = "Ледяной танк. Удар замедляет героя.";
                e.counterHint = "ОГОНЬ (×1.5): огненная катапульта, жаровня, огненный рывок.";
                e.element = Element.Ice;
                e.maxHP = 320f; e.moveSpeed = 2.2f; e.attackDamage = 18f; e.attackInterval = 2f; e.attackRange = 1.4f;
                e.slowPlayerOnHit = 0.5f; e.essenceReward = 14; e.gloryReward = 10;
            });
            DefineEnemy(c, "Harpy", new EnemyLook { color = new Color(0.6f, 0.3f, 0.85f), wings = true }, e =>
            {
                e.displayName = "Гарпия";
                e.description = "Летает над маршрутом и пикирует прямо на портал.";
                e.counterHint = "лучник, Скорпион, лук героя; мечом — только в прыжке или если сбить на землю.";
                e.isFlying = true; e.maxHP = 40f; e.moveSpeed = 7f; e.attackDamage = 5f; e.attackInterval = 1.2f;
                e.bodyRadius = 0.7f; e.aimHeight = 0f; e.hoverHeight = 0.5f; e.hoverRadius = 3f; e.diveDistance = 18f;
                e.playerAggroRange = 0f; e.essenceReward = 6; e.gloryReward = 4;
            });
            DefineEnemy(c, "Stymphalian", new EnemyLook { color = new Color(0.8f, 0.55f, 0.25f), wings = true, small = true }, e =>
            {
                e.displayName = "Стимфалийская птица";
                e.description = "Стая мелких медных птиц, быстро пикирует.";
                e.counterHint = "Скорпион (Залп, Шрапнель), Шквал лучника, Цепная молния.";
                e.isFlying = true; e.maxHP = 18f; e.moveSpeed = 9f; e.attackDamage = 3f; e.attackInterval = 1f;
                e.bodyRadius = 0.45f; e.aimHeight = 0f; e.hoverHeight = 0.8f; e.hoverRadius = 4f; e.diveDistance = 14f;
                e.playerAggroRange = 0f; e.essenceReward = 2; e.gloryReward = 1;
            });
            var small = DefineEnemy(c, "HydralingSmall", new EnemyLook { color = new Color(0.35f, 0.7f, 0.35f), scale = 0.65f }, e =>
            {
                e.displayName = "Малый гидрёныш";
                e.description = "Осколок гидрёныша.";
                e.counterHint = "любой урон.";
                e.maxHP = 35f; e.moveSpeed = 3.8f; e.attackDamage = 4f; e.attackInterval = 1.2f; e.attackRange = 1f;
                e.essenceReward = 2; e.gloryReward = 1;
            });
            DefineEnemy(c, "Hydraling", new EnemyLook { color = new Color(0.25f, 0.55f, 0.3f), horns = true }, e =>
            {
                e.displayName = "Гидрёныш";
                e.description = "Если убит не огнём — распадается на двух мелких.";
                e.counterHint = "добивай ОГНЁМ (огненная катапульта, жаровня, огненное зачарование) или AoE по осколкам.";
                e.maxHP = 80f; e.moveSpeed = 3.2f; e.attackDamage = 6f; e.attackInterval = 1.4f; e.attackRange = 1.2f;
                e.splitInto = small; e.splitCount = 2; e.essenceReward = 6; e.gloryReward = 5;
            });

            // ---------- боссы
            DefineEnemy(c, "Minotaur", new EnemyLook { color = new Color(0.45f, 0.08f, 0.08f), scale = 1.8f, horns = true, armor = true }, e =>
            {
                e.displayName = "Минотавр"; e.bossKind = BossKind.Minotaur;
                e.description = "Бык-исполин в тяжёлой броне.";
                e.counterHint = "броню снимает только герой — бей его сам, потом башни добьют.";
                e.bossHint = "Броню Минотавра снимает только герой.";
                e.maxHP = 1200f; e.armor = 450f; e.moveSpeed = 2.2f; e.attackDamage = 30f; e.attackInterval = 2f; e.attackRange = 1.4f;
                e.playerAggroRange = 4f; e.playerLeashRange = 10f; e.knockbackPlayer = 8f; e.essenceReward = 100; e.gloryReward = 60;
            });
            DefineEnemy(c, "Cerberus", new EnemyLook { color = new Color(0.18f, 0.12f, 0.12f), scale = 1.9f, boxy = true }, e =>
            {
                e.displayName = "Цербер"; e.bossKind = BossKind.Cerberus;
                e.description = "Трёхглавый пёс. У каждой головы своя стихия.";
                e.counterHint = "огненную голову — льдом, ледяную — огнём, грозовую — мечом/стрелами без зачарования.";
                e.maxHP = 2600f; e.moveSpeed = 2.6f; e.attackDamage = 35f; e.attackInterval = 1.8f; e.attackRange = 1.6f;
                e.playerAggroRange = 4f; e.playerLeashRange = 10f; e.essenceReward = 150; e.gloryReward = 80;
            });
            DefineEnemy(c, "Hydra", new EnemyLook { color = new Color(0.2f, 0.45f, 0.25f), scale = 2.1f }, e =>
            {
                e.displayName = "Гидра"; e.bossKind = BossKind.Hydra;
                e.description = "Многоглавый змей. Срубленные головы отрастают двумя.";
                e.counterHint = "руби головы, затем прижигай обрубки огнём или подбеги и нажми E.";
                e.maxHP = 3000f; e.moveSpeed = 2f; e.attackDamage = 40f; e.attackInterval = 2f; e.attackRange = 1.8f;
                e.playerAggroRange = 4f; e.playerLeashRange = 10f; e.essenceReward = 180; e.gloryReward = 90;
            });
            DefineEnemy(c, "Medusa", new EnemyLook { color = new Color(0.3f, 0.6f, 0.5f), scale = 1.7f, robe = true }, e =>
            {
                e.displayName = "Медуза"; e.bossKind = BossKind.Medusa;
                e.description = "Её взгляд превращает башни в камень.";
                e.counterHint = "расколдовывай башни (E рядом), сбей до 25% и отруби ей голову (E).";
                e.maxHP = 3400f; e.moveSpeed = 2.4f; e.attackDamage = 30f; e.attackInterval = 1.6f; e.attackRange = 1.4f;
                e.playerAggroRange = 4f; e.playerLeashRange = 10f; e.essenceReward = 200; e.gloryReward = 100;
            });
            DefineEnemy(c, "HarpyQueen", new EnemyLook { color = new Color(0.45f, 0.15f, 0.6f), scale = 2.2f, wings = true }, e =>
            {
                e.displayName = "Королева гарпий"; e.bossKind = BossKind.HarpyQueen;
                e.description = "Летающий босс: кружит высоко и призывает гарпий.";
                e.counterHint = "сбей на землю (гарпун Скорпиона, сеть Снайпера, Громоотвод, Цепная молния) и нажми E рядом — оторви крылья.";
                e.isFlying = true; e.maxHP = 3000f; e.moveSpeed = 5f; e.attackDamage = 30f; e.attackInterval = 1.5f;
                e.bodyRadius = 1.2f; e.aimHeight = 0f; e.cruiseHeight = 4f; e.hoverHeight = 4f; e.hoverRadius = 7f; e.diveDistance = 20f;
                e.playerAggroRange = 0f; e.essenceReward = 220; e.gloryReward = 110;
            });
            DefineEnemy(c, "Titan", new EnemyLook { color = new Color(0.55f, 0.52f, 0.48f), scale = 2.8f, armor = true, boxy = true }, e =>
            {
                e.displayName = "Титан"; e.bossKind = BossKind.Titan;
                e.description = "Финальный босс. Три фазы: броня, смена стихий, добивание.";
                e.counterHint = "1) снимай броню сам; 2) бей противоположной стихией; 3) E — добить.";
                e.maxHP = 7000f; e.armor = 1500f; e.moveSpeed = 1.8f; e.attackDamage = 60f; e.attackInterval = 2.5f; e.attackRange = 2f;
                e.playerAggroRange = 5f; e.playerLeashRange = 12f; e.knockbackPlayer = 12f; e.essenceReward = 400; e.gloryReward = 200;
            });
        }

        // ---------------------------------------------------------------- башни

        private static TowerPerk P(string id, string name, string desc, int tier, string parent, int cost, TowerSpecial s = TowerSpecial.None,
            float dmg = 1f, float interval = 1f, float range = 1f, float aoe = 0f, Element el = Element.None, float value = 0f)
            => new TowerPerk { id = id, name = name, description = desc, tier = tier, parent = parent, cost = cost, special = s,
                damageMul = dmg, intervalMul = interval, rangeMul = range, aoeAdd = aoe, element = el, value = value };

        private static TowerLevelStats L(int cost, float dmg, float interval, float range, float aoe = 0f, float aura = 0f)
            => new TowerLevelStats { cost = cost, damage = dmg, fireInterval = interval, range = range, aoeRadius = aoe, auraBonus = aura };

        private static TowerConfig DefineTower(ConfigSet c, string key, System.Action<TowerConfig> init)
        {
            var cfg = LoadOrCreate<TowerConfig>($"{SODir}/Towers/Tower_{key}.asset", init, out _);
            c.Towers[key] = cfg;
            return cfg;
        }

        private static void DefineTowers(ConfigSet c)
        {
            DefineTower(c, "Archer", t =>
            {
                t.displayName = "Лучник"; t.kind = TowerKind.Projectile; t.description = "Одиночная цель, бьёт и землю, и воздух.";
                t.targetsGround = true; t.targetsAir = true; t.projectileSpeed = 45f; t.footprint = 1.3f; t.hybridCost = 220;
                t.levels = new[] { L(60, 12f, 0.8f, 16f), L(50, 18f, 0.65f, 18f), L(80, 26f, 0.5f, 20f) };
                t.perks = new[]
                {
                    P("rapid", "Скорострел", "+70% к скорости атаки", 4, null, 120, interval: 0.6f),
                    P("flurry", "Шквал", "Стреляет сразу по 3 целям", 5, "rapid", 170, TowerSpecial.MultiShot),
                    P("rampup", "Разогрев", "Пока стреляет без перерыва, скорость растёт до ×2", 5, "rapid", 160, TowerSpecial.RampUp),
                    P("sniper", "Снайпер", "+60% дальности, 25% шанс крита ×2", 4, null, 120, TowerSpecial.Crit, dmg: 1.2f, range: 1.6f),
                    P("pierce", "Пробивная стрела", "Стрела пробивает всю линию врагов", 5, "sniper", 170, TowerSpecial.Pierce, dmg: 1.3f),
                    P("net", "Ловчая сеть", "Каждый 3-й выстрел сбивает летуна на землю на 3 с — его можно бить мечом", 5, "sniper", 150, TowerSpecial.NetFlyers),
                };
            });
            DefineTower(c, "Catapult", t =>
            {
                t.displayName = "Катапульта"; t.kind = TowerKind.Projectile; t.description = "Бьёт по области, только наземных.";
                t.targetsGround = true; t.targetsAir = false; t.projectileSpeed = 28f; t.footprint = 1.5f; t.hybridCost = 240;
                t.levels = new[] { L(90, 30f, 2.2f, 18f, 3f), L(70, 45f, 1.9f, 20f, 3.5f), L(110, 65f, 1.6f, 22f, 4f) };
                t.perks = new[]
                {
                    P("ice", "Ледяные снаряды", "Лёд: замедляют на 40%, огненным врагам ×1.5 урона", 4, null, 130, TowerSpecial.IceShots, el: Element.Ice),
                    P("frostzone", "Вечная мерзлота", "После попадания — ледяная лужа: 4 с замедления", 5, "ice", 170, TowerSpecial.FrostZone),
                    P("shackles", "Оковы", "3 попадания по одной цели — заморозка на 1,5 с", 5, "ice", 170, TowerSpecial.Shackles),
                    P("fire", "Огненные снаряды", "Огонь: поджигают, ледяным врагам ×1.5 урона", 4, null, 130, TowerSpecial.FireShots, el: Element.Fire),
                    P("napalm", "Напалм", "Горящая земля на 5 с", 5, "fire", 170, TowerSpecial.Napalm),
                    P("meteor", "Метеор", "Каждый 4-й снаряд огромный и оглушает", 5, "fire", 170, TowerSpecial.Meteor),
                    P("steam", "Паровой залп", "Снаряды чередуются: лёд, огонь… — катапульта сама вызывает Раскол", 6, null, 240, TowerSpecial.SteamVolley),
                };
            });
            DefineTower(c, "Obelisk", t =>
            {
                t.displayName = "Молниевый обелиск"; t.kind = TowerKind.Chain; t.element = Element.Lightning;
                t.description = "Молния цепью перескакивает между врагами (земля и воздух).";
                t.targetsGround = true; t.targetsAir = true; t.chainJumps = 3; t.footprint = 1.1f; t.hybridCost = 220;
                t.levels = new[] { L(100, 18f, 1.4f, 14f), L(80, 26f, 1.2f, 15f), L(120, 36f, 1.0f, 16f) };
                t.perks = new[]
                {
                    P("chain", "Цепь", "+3 прыжка молнии", 4, null, 130, TowerSpecial.ExtraChains, value: 3f),
                    P("grid", "Электросеть", "Луч между двумя обелисками бьёт всех, кто проходит между ними", 5, "chain", 170, TowerSpecial.PowerGrid),
                    P("shock", "Шоковая цепь", "Каждый удар оглушает на 0,4 с", 5, "chain", 160, TowerSpecial.ChainStun),
                    P("storm", "Буря", "Раз в 8 с — разряд по области (урон ×3)", 4, null, 130, TowerSpecial.Storm),
                    P("eye", "Око бури", "Буря раз в 5 с и шире", 5, "storm", 170, TowerSpecial.StormFast),
                    P("rod", "Громоотвод", "Буря сбивает летунов на землю на 3 с", 5, "storm", 160, TowerSpecial.StormGround),
                };
            });
            DefineTower(c, "Brazier", t =>
            {
                t.displayName = "Жаровня"; t.kind = TowerKind.Area; t.element = Element.Fire;
                t.description = "Огонь по кругу у подножия — против толпы. Ставь у пути и баррикад.";
                t.targetsGround = true; t.targetsAir = false; t.footprint = 0.9f; t.hybridCost = 200;
                t.levels = new[] { L(70, 8f, 0.6f, 4f), L(60, 12f, 0.55f, 4.5f), L(90, 17f, 0.5f, 5f) };
                t.perks = new[]
                {
                    P("inferno", "Инферно", "+80% урона, круг шире", 4, null, 120, TowerSpecial.Inferno, dmg: 1.8f, range: 1.25f),
                    P("pillar", "Огненный столб", "Раз в 3 с бьёт столбом огня врага в 10 м", 5, "inferno", 160, TowerSpecial.FirePillar),
                    P("ash", "Пепел", "Горящие враги получают +25% урона от всего", 5, "inferno", 150, TowerSpecial.Ash),
                    P("holy", "Священное пламя", "Подсвечивает невидимок, глушит лечение жрецов, лечит героя рядом", 4, null, 120, TowerSpecial.HolyFlame),
                    P("purify", "Очищение", "Снимает с врагов магические щиты и регенерацию, глушит лечение шире", 5, "holy", 150, TowerSpecial.Purify),
                    P("beacon", "Маяк", "Башни в 8 м получают +15% урона", 5, "holy", 150, TowerSpecial.Beacon),
                };
            });
            DefineTower(c, "Scorpion", t =>
            {
                t.displayName = "Скорпион"; t.kind = TowerKind.Projectile; t.description = "Против воздуха: тяжёлые болты по летунам.";
                t.targetsGround = false; t.targetsAir = true; t.projectileSpeed = 55f; t.footprint = 1.3f; t.hybridCost = 220;
                t.levels = new[] { L(80, 25f, 1.2f, 22f), L(60, 38f, 1.0f, 24f), L(90, 55f, 0.85f, 26f) };
                t.perks = new[]
                {
                    P("volley", "Залп", "Стреляет по 3 летунам сразу", 4, null, 130, TowerSpecial.Volley),
                    P("shrapnel", "Шрапнель", "Болты взрываются шрапнелью по летунам вокруг", 5, "volley", 160, TowerSpecial.Shrapnel),
                    P("rapidbolts", "Стремительность", "+65% к скорости атаки", 5, "volley", 150, TowerSpecial.RapidBolts, interval: 0.6f),
                    P("harpoon", "Гарпун", "Попадание сбивает летуна на землю на 3 с", 4, null, 130, TowerSpecial.Harpoon),
                    P("barbed", "Зазубренный гарпун", "Сбитые летуны получают +50% урона", 5, "harpoon", 150, TowerSpecial.HarpoonDamage),
                    P("prey", "Добыча", "Летающих боссов держит на земле вдвое дольше", 5, "harpoon", 160, TowerSpecial.HarpoonBoss),
                };
            });
            DefineTower(c, "Sanctuary", t =>
            {
                t.displayName = "Святилище"; t.kind = TowerKind.Support;
                t.description = "Не атакует. Усиливает урон башен в своей ауре — ставь башни вокруг.";
                t.targetsGround = false; t.targetsAir = false; t.footprint = 1.4f; t.hybridCost = 200;
                t.levels = new[] { L(90, 0f, 2.5f, 8f, 0f, 0.10f), L(70, 0f, 2.5f, 9f, 0f, 0.15f), L(110, 0f, 2.5f, 10f, 0f, 0.20f) };
                t.perks = new[]
                {
                    P("haste", "Ускорение", "Башни в ауре стреляют на 25% быстрее", 4, null, 130, TowerSpecial.Haste),
                    P("zeal", "Фанатизм", "Все эффекты святилища ×1.5", 5, "haste", 170, TowerSpecial.Zeal),
                    P("flow", "Поток эссенции", "+2 эссенции за каждого врага, убитого в ауре", 5, "haste", 150, TowerSpecial.EssenceFlow),
                    P("resonance", "Стихийный резонанс", "Реакции стихий в ауре наносят +50% урона", 4, null, 130, TowerSpecial.Resonance),
                    P("resslow", "Резонансный купол", "Реакции в ауре замедляют врагов", 5, "resonance", 150, TowerSpecial.ResonanceSlow),
                    P("well", "Источник фокуса", "Герой в ауре копит Фокус вдвое быстрее", 5, "resonance", 150, TowerSpecial.FocusWell),
                };
            });
        }

        // ---------------------------------------------------------------- герой

        private static HeroPerk HP(string id, string name, string desc, PerkBranch branch, PerkKind kind, float value = 1f, int stacks = 1,
            string ability = null, int minLevel = 1)
            => new HeroPerk { id = id, name = name, description = desc, branch = branch, kind = kind, value = value, maxStacks = stacks, abilityId = ability, minLevel = minLevel };

        private static AbilityDef AB(string id, string name, string desc, float cd, float dmg, float radius, float duration, Element el = Element.None)
            => new AbilityDef { id = id, name = name, description = desc, cooldown = cd, damageMul = dmg, radius = radius, duration = duration, element = el };

        private static HeroConfig DefineHero()
        {
            return LoadOrCreate<HeroConfig>($"{SODir}/Player/HeroConfig.asset", h =>
            {
                h.xpBase = 50; h.xpGrowth = 1.2f; h.choicesPerLevel = 3; h.baseTowerLimit = 4;
                h.focusPerHit = 2f; h.focusPerKill = 3f; h.focusPerReaction = 6f; h.focusMax = 100f;
                h.ultimateDamage = 300f; h.ultimateDamagePerWave = 0.15f; h.ultimateLength = 40f; h.ultimateWidth = 2.4f; h.ultimateChargeTime = 0.9f;
                h.weaponUpgradeCosts = new[] { 70, 120, 180, 260 }; h.weaponDamagePerLevel = 0.3f; h.enchantCost = 50; h.enchantSwitchCost = 20;
                h.abilities = new[]
                {
                    AB("reap", "Жатва", "Круговой удар; 30% урона возвращается здоровьем", 9f, 2.2f, 3.8f, 0f),
                    AB("skyslam", "Удар с неба", "Прыжок вперёд и удар по области при приземлении (сильнее с высоты)", 11f, 3f, 4.5f, 0f),
                    AB("firedash", "Огненный рывок", "Рывок, оставляющий горящий след", 8f, 1.2f, 7f, 3f, Element.Fire),
                    AB("frostnova", "Ледяная нова", "Замораживает всех вокруг", 14f, 1f, 6f, 2.2f, Element.Ice),
                    AB("chain", "Цепная молния", "Молния от врага под прицелом на 7 целей; сбивает летунов", 10f, 1.4f, 7f, 0f, Element.Lightning),
                    AB("meteor", "Метеор", "Метеор в точку прицела, поджигает", 16f, 4f, 5f, 3f, Element.Fire),
                };
                h.perks = new[]
                {
                    HP("tower", "+1 башня", "Лимит башен +1", PerkBranch.General, PerkKind.TowerLimit, 1f, 8),
                    HP("vigor", "Закалка", "+30 к максимальному здоровью", PerkBranch.General, PerkKind.MaxHP, 30f, 3),
                    HP("swift", "Быстрые ноги", "+10% к скорости бега", PerkBranch.General, PerkKind.MoveSpeed, 0.1f, 2),
                    HP("focus", "Сосредоточение", "+25% к накоплению Фокуса", PerkBranch.General, PerkKind.FocusGain, 0.25f, 2),
                    HP("reap", "Жатва", "Навык: круговой удар с вампиризмом", PerkBranch.Warrior, PerkKind.Ability, ability: "reap"),
                    HP("skyslam", "Удар с неба", "Навык: прыжок и удар по области", PerkBranch.Warrior, PerkKind.Ability, ability: "skyslam"),
                    HP("berserk", "Берсерк", "Чем меньше здоровья, тем сильнее удары (до +60%)", PerkBranch.Warrior, PerkKind.Berserk),
                    HP("heavy", "Тяжёлая рука", "+20% к урону меча", PerkBranch.Warrior, PerkKind.SwordDamage, 0.2f, 3),
                    HP("breaker", "Разрушитель брони", "Броня боссов снимается вдвое быстрее", PerkBranch.Warrior, PerkKind.ArmorBreaker, 1f),
                    HP("huntmark", "Охотничья метка", "Метка держится 8 с и даёт башням ×2 урона", PerkBranch.Hunter, PerkKind.HunterMark),
                    HP("execute", "Казнь", "Удар по помеченному врагу ниже 15% HP убивает его", PerkBranch.Hunter, PerkKind.Execute),
                    HP("steady", "Твёрдая рука", "+20% к урону лука и быстрее натяжение", PerkBranch.Hunter, PerkKind.BowDamage, 0.2f, 3),
                    HP("firedash", "Огненный рывок", "Навык: рывок с горящим следом", PerkBranch.Elementalist, PerkKind.Ability, ability: "firedash"),
                    HP("frostnova", "Ледяная нова", "Навык: заморозка вокруг", PerkBranch.Elementalist, PerkKind.Ability, ability: "frostnova"),
                    HP("chain", "Цепная молния", "Навык: молния на 7 целей", PerkBranch.Elementalist, PerkKind.Ability, ability: "chain"),
                    HP("meteor", "Метеор", "Навык: метеор в точку прицела", PerkBranch.Elementalist, PerkKind.Ability, ability: "meteor", minLevel: 3),
                    HP("affinity", "Стихийное сродство", "+30% к силе ультимейта и реакций героя", PerkBranch.Elementalist, PerkKind.ReactionPower, 0.3f, 2),
                    HP("ult_storm", "Алая буря", "Ультимейт меняется: огненно-грозовая буря вокруг героя", PerkBranch.Elementalist, PerkKind.UltimateVariant, ability: "storm", minLevel: 6),
                    HP("ult_zero", "Абсолютный ноль", "Ультимейт меняется: всё вокруг замерзает", PerkBranch.Elementalist, PerkKind.UltimateVariant, ability: "zero", minLevel: 6),
                };
            }, out _);
        }

        // ---------------------------------------------------------------- всё вместе

        private static ConfigSet CreateConfigs()
        {
            var c = new ConfigSet();
            c.Player = LoadOrCreate<PlayerConfig>(SODir + "/Player/PlayerConfig.asset", _ => { }, out _);
            c.Hero = DefineHero();
            DefineEnemies(c);
            DefineTowers(c);
            var waves = CreateWaves(c);
            c.Balance = LoadOrCreate<GameBalance>(SODir + "/GameBalance.asset", b => { }, out _);
            // Ссылки и списки обновляются всегда (иначе старый ассет не увидит новые башни/волны)
            c.Balance.towerTypes = new[] { c.Towers["Archer"], c.Towers["Catapult"], c.Towers["Obelisk"], c.Towers["Brazier"], c.Towers["Scorpion"], c.Towers["Sanctuary"] };
            c.Balance.hero = c.Hero;
            c.Balance.waves = waves;
            c.Balance.endlessPool = new[] { "Walker", "Satyr", "ShieldBearer", "Harpy", "Pyromancer", "Cyclops", "Cryomancer", "Centaur", "Priest", "Testudo", "Stymphalian", "Lemur", "Salamander", "IceGolem", "Hydraling" }.Select(k => c.E(k)).ToArray();
            c.Balance.bossPool = new[] { "Minotaur", "Cerberus", "Hydra", "Medusa", "HarpyQueen", "Titan" }.Select(k => c.E(k)).ToArray();
            if (c.Balance.difficulties == null || c.Balance.difficulties.Length == 0 || OverwriteConfigs)
            {
                c.Balance.difficulties = new[]
                {
                    new DifficultyPreset { name = "Легионер", description = "Для знакомства с игрой: враги слабее, эссенции больше", enemyHp = 0.8f, enemyDamage = 0.8f, income = 1.15f },
                    new DifficultyPreset { name = "Центурион", description = "Задуманный баланс", enemyHp = 1f, enemyDamage = 1f, income = 1f },
                    new DifficultyPreset { name = "Император", description = "Для ветеранов арены: враги крепче и злее", enemyHp = 1.3f, enemyDamage = 1.25f, income = 0.9f },
                };
                c.Balance.defaultDifficulty = 1;
            }
            EditorUtility.SetDirty(c.Balance);
            return c;
        }

        private static SpawnGroup G(EnemyConfig e, int count, float interval, float delay = 0f, int arch = -1, bool squad = false)
            => new SpawnGroup { enemy = e, count = count, interval = interval, startDelay = delay, arch = arch, squad = squad };

        private static WaveConfig[] CreateWaves(ConfigSet c)
        {
            var W = c.E("Walker");
            var defs = new List<(string title, SpawnGroup[] groups)>
            {
                ("Первая кровь", new[] { G(W, 8, 2.2f) }),
                ("Быстрые ноги", new[] { G(W, 10, 1.8f), G(c.E("Satyr"), 6, 0.8f, 8f) }),
                ("Щиты и крылья", new[] { G(W, 10, 1.8f), G(c.E("ShieldBearer"), 4, 4f, 6f), G(c.E("Harpy"), 4, 3f, 10f) }),
                ("Огонь издалека", new[] { G(W, 12, 1.6f), G(c.E("Satyr"), 8, 0.7f, 5f), G(c.E("Pyromancer"), 3, 4f, 10f) }),
                ("БОСС: Минотавр", new[] { G(c.E("Minotaur"), 1, 1f, 4f, 0), G(W, 12, 1.8f), G(c.E("ShieldBearer"), 4, 4f, 10f) }),
                ("Тяжёлая поступь", new[] { G(W, 12, 1.6f), G(c.E("Cyclops"), 2, 10f, 6f), G(c.E("Cryomancer"), 3, 4f, 10f), G(c.E("Harpy"), 4, 3f, 12f) }),
                ("Стрелы и молитвы", new[] { G(W, 10, 1.6f), G(c.E("Centaur"), 4, 4f, 4f), G(c.E("Priest"), 2, 8f, 10f), G(c.E("Satyr"), 8, 0.6f, 14f) }),
                ("Черепаха", new[] { G(c.E("Testudo"), 6, 0.25f, 4f, 1, true), G(W, 12, 1.5f), G(c.E("Stymphalian"), 10, 0.5f, 10f) }),
                ("Тени и пламя", new[] { G(W, 12, 1.5f), G(c.E("Lemur"), 6, 2.5f, 5f), G(c.E("Salamander"), 6, 1.8f, 10f), G(c.E("Harpy"), 6, 2f, 14f) }),
                ("БОСС: Цербер", new[] { G(c.E("Cerberus"), 1, 1f, 4f, 2), G(W, 14, 1.4f), G(c.E("IceGolem"), 2, 10f, 10f), G(c.E("Hydraling"), 4, 3f, 14f) }),
            };

            // Пул для генерации волн 11–30: (ключ, волна открытия, вес)
            var pool = new List<WaveGenerator.Entry>();
            void Add(string key, int unlock, float weight = 1f, bool squad = false, int min = 1)
                => pool.Add(new WaveGenerator.Entry { enemy = c.E(key), cost = GameManager.CostOf(c.E(key)), unlockWave = unlock, weight = weight, squad = squad, minCount = min });
            Add("Walker", 1); Add("Satyr", 2, 1f, false, 6); Add("ShieldBearer", 3); Add("Harpy", 3); Add("Pyromancer", 4);
            Add("Cyclops", 6, 0.7f); Add("Cryomancer", 6); Add("Centaur", 7); Add("Priest", 7, 0.6f); Add("Testudo", 8, 0.7f, true, 5);
            Add("Stymphalian", 8, 0.8f, false, 8); Add("Lemur", 9); Add("Salamander", 9); Add("IceGolem", 10, 0.7f); Add("Hydraling", 10);

            var bosses = new Dictionary<int, (string key, string title)>
            {
                { 15, ("Hydra", "БОСС: Гидра") }, { 20, ("Medusa", "БОСС: Медуза") },
                { 25, ("HarpyQueen", "БОСС: Королева гарпий") }, { 30, ("Titan", "ФИНАЛ: Титан") },
            };
            var rng = new System.Random(2026);
            for (int n = 11; n <= 30; n++)
            {
                int act = n <= 20 ? 2 : 3;
                var groups = WaveGenerator.Compose(n, WaveGenerator.Budget(n), pool, rng, act);
                string title = act == 2 ? "Акт II — развилки" : "Акт III — элита";
                if (bosses.TryGetValue(n, out var boss))
                {
                    groups.Insert(0, G(c.E(boss.key), 1, 1f, 4f, rng.Next(0, 4)));
                    title = boss.title;
                }
                defs.Add((title, groups.ToArray()));
            }

            var result = new WaveConfig[defs.Count];
            for (int i = 0; i < defs.Count; i++)
            {
                var d = defs[i];
                int n = i + 1;
                result[i] = LoadOrCreate<WaveConfig>($"{SODir}/Waves/Wave_{n:00}.asset", w =>
                {
                    w.title = d.title;
                    w.act = n <= 10 ? 1 : n <= 20 ? 2 : 3;
                    w.groups = d.groups;
                    w.hpMultiplier = WaveGenerator.HpMultiplier(n);
                    w.completionBonus = 40 + 4 * (n - 1);
                    w.gloryBonus = 15 + 3 * n;
                }, out _);
            }
            return result;
        }

        /// <summary>Прописать в конфиги ссылки на свежие префабы.</summary>
        private static void LinkPrefabs(ConfigSet c, PrefabSet p)
        {
            foreach (var kv in c.Enemies)
                if (p.Enemies.TryGetValue(kv.Key, out var go)) { Assign(kv.Value, ("prefab", go.GetComponent<Enemy>())); EditorUtility.SetDirty(kv.Value); }
            foreach (var kv in c.Towers)
                if (p.Towers.TryGetValue(kv.Key, out var go)) { Assign(kv.Value, ("prefab", go.GetComponent<Tower>())); EditorUtility.SetDirty(kv.Value); }
        }
    }
}
