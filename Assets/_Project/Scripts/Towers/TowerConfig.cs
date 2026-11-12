using System;
using UnityEngine;

namespace ColiseumGame
{
    public enum TowerKind
    {
        /// <summary>Снаряды (лучник, катапульта, скорпион).</summary>
        Projectile,
        /// <summary>Мгновенная цепная молния (обелиск).</summary>
        Chain,
        /// <summary>Урон по области вокруг себя (жаровня).</summary>
        Area,
        /// <summary>Не атакует, усиливает башни рядом (святилище).</summary>
        Support
    }

    /// <summary>Особое поведение перка. Числа — в TowerPerk.value.</summary>
    public enum TowerSpecial
    {
        None,
        // Лучник
        MultiShot, RampUp, Crit, Pierce, NetFlyers,
        // Катапульта
        IceShots, FrostZone, Shackles, FireShots, Napalm, Meteor, SteamVolley,
        // Обелиск
        ExtraChains, ChainStun, PowerGrid, Storm, StormFast, StormGround,
        // Жаровня
        Inferno, FirePillar, Ash, HolyFlame, Purify, Beacon,
        // Скорпион
        Volley, Shrapnel, RapidBolts, Harpoon, HarpoonDamage, HarpoonBoss,
        // Святилище
        Haste, Zeal, EssenceFlow, Resonance, ResonanceSlow, FocusWell
    }

    /// <summary>Параметры одного из уровней 1–3.</summary>
    [Serializable]
    public class TowerLevelStats
    {
        [Tooltip("Цена постройки (уровень 1) или улучшения до этого уровня")]
        public int cost = 60;
        public float damage = 12f;
        [Tooltip("Секунд между выстрелами / импульсами")]
        public float fireInterval = 0.8f;
        [Tooltip("Дальность атаки, а для Святилища — радиус ауры")]
        public float range = 16f;
        [Tooltip("Радиус взрыва. 0 — одиночная цель")]
        public float aoeRadius = 0f;
        [Tooltip("Святилище: бонус к урону башен рядом (0.1 = +10%)")]
        public float auraBonus = 0f;
    }

    /// <summary>
    /// Перк ветки прокачки. tier 4 — выбор ветки, tier 5 — продолжение (parent = id перка 4-го уровня),
    /// tier 6 — особый финал (если есть), иначе на 6-м уровне — гибрид: перк соседней ветки с силой 50%.
    /// </summary>
    [Serializable]
    public class TowerPerk
    {
        public string id;
        public string name;
        [TextArea] public string description;
        public int tier = 4;
        public string parent;
        public int cost = 120;
        public float damageMul = 1f;
        public float intervalMul = 1f;
        public float rangeMul = 1f;
        public float aoeAdd = 0f;
        public Element element = Element.None;
        public TowerSpecial special = TowerSpecial.None;
        public float value = 0f;
    }

    /// <summary>Тип башни. Уровни 1→2→3 — массив levels, дальше — перки.</summary>
    [CreateAssetMenu(menuName = "Coliseum/Tower Config", fileName = "Tower")]
    public class TowerConfig : ScriptableObject
    {
        public string displayName = "Башня";
        [TextArea] public string description = "";
        public Tower prefab;
        public TowerKind kind = TowerKind.Projectile;
        [Tooltip("Стихия башни по умолчанию (до выбора ветки)")]
        public Element element = Element.None;
        public bool targetsGround = true;
        public bool targetsAir = true;
        public float projectileSpeed = 40f;
        [Tooltip("Обелиск: сколько раз молния перепрыгивает на соседнего врага")]
        public int chainJumps = 3;
        [Tooltip("Радиус основания (для проверки расстояния между башнями)")]
        public float footprint = 1.3f;
        public TowerLevelStats[] levels = new TowerLevelStats[3];
        public TowerPerk[] perks = new TowerPerk[0];
        [Tooltip("Цена гибридного слота (6-й уровень)")]
        public int hybridCost = 220;

        public TowerPerk Perk(string id)
        {
            foreach (var p in perks) if (p.id == id) return p;
            return null;
        }
    }
}
