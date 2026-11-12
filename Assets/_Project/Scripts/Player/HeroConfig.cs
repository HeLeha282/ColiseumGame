using System;
using UnityEngine;

namespace ColiseumGame
{
    public enum PerkBranch { General, Warrior, Hunter, Elementalist }

    public enum PerkKind
    {
        /// <summary>+1 к лимиту башен.</summary>
        TowerLimit,
        /// <summary>Открывает активный навык (Q/R).</summary>
        Ability,
        MaxHP,
        MoveSpeed,
        FocusGain,
        SwordDamage,
        BowDamage,
        ArmorBreaker,
        HunterMark,
        Execute,
        Berserk,
        ReactionPower,
        /// <summary>Другой ультимейт (Алая буря / Абсолютный ноль).</summary>
        UltimateVariant
    }

    [Serializable]
    public class HeroPerk
    {
        public string id;
        public string name;
        [TextArea] public string description;
        public PerkBranch branch;
        public PerkKind kind;
        [Tooltip("Для Ability — id навыка, для UltimateVariant — id ультимейта")]
        public string abilityId;
        public float value;
        public int maxStacks = 1;
        [Tooltip("С какого уровня героя может выпасть")]
        public int minLevel = 1;
    }

    [Serializable]
    public class AbilityDef
    {
        public string id;
        public string name;
        [TextArea] public string description;
        public float cooldown = 10f;
        [Tooltip("Урон как множитель урона меча")]
        public float damageMul = 2f;
        public float radius = 4f;
        public float duration = 2f;
        public Element element;
    }

    /// <summary>Прокачка героя: Слава (опыт) → уровни → выбор перков; оружие за эссенцию; навыки и ультимейт.</summary>
    [CreateAssetMenu(menuName = "Coliseum/Hero Config", fileName = "HeroConfig")]
    public class HeroConfig : ScriptableObject
    {
        [Header("Слава и уровни")]
        public int xpBase = 50;
        public float xpGrowth = 1.2f;
        [Tooltip("Сколько карточек на выбор при повышении уровня")]
        public int choicesPerLevel = 3;

        [Header("Башни")]
        public int baseTowerLimit = 4;

        [Header("Фокус (шкала ультимейта)")]
        public float focusPerHit = 2f;
        public float focusPerKill = 3f;
        public float focusPerReaction = 6f;
        public float focusMax = 100f;

        [Header("Ультимейт")]
        public float ultimateDamage = 300f;
        [Tooltip("Рост урона ультимейта за каждую волну")]
        public float ultimateDamagePerWave = 0.15f;
        public float ultimateLength = 40f;
        public float ultimateWidth = 2.4f;
        public float ultimateChargeTime = 0.9f;

        [Header("Оружие (уровни 2..5 за эссенцию)")]
        public int[] weaponUpgradeCosts = { 70, 120, 180, 260 };
        [Tooltip("+урон за каждый уровень оружия")]
        public float weaponDamagePerLevel = 0.3f;
        public int enchantCost = 50;
        public int enchantSwitchCost = 20;

        [Header("Навыки")]
        public AbilityDef[] abilities = new AbilityDef[0];

        [Header("Перки (выпадают при повышении уровня)")]
        public HeroPerk[] perks = new HeroPerk[0];

        public AbilityDef Ability(string id)
        {
            foreach (var a in abilities) if (a.id == id) return a;
            return null;
        }

        public HeroPerk Perk(string id)
        {
            foreach (var p in perks) if (p.id == id) return p;
            return null;
        }

        public int XpForLevel(int level) => Mathf.RoundToInt(xpBase * Mathf.Pow(xpGrowth, level - 1));
    }
}
