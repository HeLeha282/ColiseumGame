using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Прокачка героя. Слава за убийства → уровни → выбор 1 из 3 карточек. В выборе всегда есть «+1 башня»,
    /// поэтому лимит башен растёт ценой других усилений. Оружие улучшается за эссенцию, есть зачарование стихией.
    /// Фокус копится от ударов/реакций/убийств и тратится на ультимейт.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerProgression : MonoBehaviour
    {
        public static PlayerProgression Instance { get; private set; }

        [SerializeField] private HeroConfig hero;

        public HeroConfig Hero => hero;
        public int Level { get; private set; } = 1;
        public int Glory { get; private set; }
        public int GloryToNext => hero.XpForLevel(Level);
        public int PendingChoices { get; private set; }
        public List<HeroPerk> Offer { get; } = new List<HeroPerk>();
        public float Focus { get; private set; }
        public float FocusFraction => Focus / hero.focusMax;
        public bool UltimateReady => Focus >= hero.focusMax;
        public int SwordLevel { get; private set; } = 1;
        public int BowLevel { get; private set; } = 1;
        public Element Enchant { get; private set; } = Element.None;
        public string[] AbilitySlots { get; } = new string[2];
        public string UltimateId { get; private set; } = "void";

        public event Action Changed;
        public event Action LeveledUp;

        private readonly Dictionary<string, int> stacks = new Dictionary<string, int>();
        private bool enchantBought;

        public int Stacks(string id) => stacks.TryGetValue(id, out int n) ? n : 0;
        private float Sum(PerkKind kind)
        {
            float s = 0f;
            foreach (var p in hero.perks) if (p.kind == kind) s += p.value * Stacks(p.id);
            return s;
        }
        private bool HasKind(PerkKind kind) => Sum(kind) > 0f;

        // ---- производные характеристики
        public int TowerLimit => hero.baseTowerLimit + Mathf.RoundToInt(Sum(PerkKind.TowerLimit));
        public float MaxHPBonus => Sum(PerkKind.MaxHP);
        public float MoveSpeedMultiplier => 1f + Sum(PerkKind.MoveSpeed);
        public float SwordMultiplier => (1f + hero.weaponDamagePerLevel * (SwordLevel - 1)) * (1f + Sum(PerkKind.SwordDamage));
        public float BowMultiplier => (1f + hero.weaponDamagePerLevel * (BowLevel - 1)) * (1f + Sum(PerkKind.BowDamage));
        public float BowChargeMultiplier => 1f / (1f + Sum(PerkKind.BowDamage));
        public float ArmorBreakMultiplier => 1f + Sum(PerkKind.ArmorBreaker);
        public float MarkDuration => HasKind(PerkKind.HunterMark) ? 8f : (GameManager.Instance != null ? GameManager.Instance.Balance.markDuration : 4f);
        public float MarkMultiplier => HasKind(PerkKind.HunterMark) ? 2f : (GameManager.Instance != null ? GameManager.Instance.Balance.markTowerDamageMultiplier : 1.5f);
        public bool HasExecute => HasKind(PerkKind.Execute);
        public bool HasBerserk => HasKind(PerkKind.Berserk);
        public float ReactionPower => 1f + Sum(PerkKind.ReactionPower);

        private void Awake() => Instance = this;

        // ---------------------------------------------------------------- Слава и уровни

        public void AddGlory(int amount)
        {
            if (amount <= 0) return;
            Glory += amount;
            while (Glory >= GloryToNext)
            {
                Glory -= GloryToNext;
                Level++;
                PendingChoices++;
                if (Offer.Count == 0) RollOffer();
                LeveledUp?.Invoke();
                GameManager.Instance?.ShowMessage($"Уровень героя {Level}! Выбор усиления — Tab");
            }
            Changed?.Invoke();
        }

        /// <summary>Собрать 3 карточки. «+1 башня» есть всегда (если не исчерпан), остальные — случайные доступные.</summary>
        public void RollOffer()
        {
            Offer.Clear();
            var pool = new List<HeroPerk>();
            HeroPerk tower = null;
            foreach (var p in hero.perks)
            {
                if (Stacks(p.id) >= p.maxStacks || Level < p.minLevel) continue;
                if (p.kind == PerkKind.Ability && HasAbility(p.abilityId)) continue;
                if (p.kind == PerkKind.TowerLimit) { tower = p; continue; }
                pool.Add(p);
            }
            if (tower != null) Offer.Add(tower);
            while (Offer.Count < hero.choicesPerLevel && pool.Count > 0)
            {
                int i = UnityEngine.Random.Range(0, pool.Count);
                Offer.Add(pool[i]);
                pool.RemoveAt(i);
            }
            Changed?.Invoke();
        }

        public void Choose(HeroPerk perk)
        {
            if (PendingChoices <= 0 || !Offer.Contains(perk)) return;
            stacks[perk.id] = Stacks(perk.id) + 1;
            if (perk.kind == PerkKind.Ability) AssignAbility(perk.abilityId);
            if (perk.kind == PerkKind.UltimateVariant) UltimateId = perk.abilityId;
            if (perk.kind == PerkKind.MaxHP) GetComponent<PlayerController>().RefreshMaxHP();
            PendingChoices--;
            Offer.Clear();
            if (PendingChoices > 0) RollOffer();
            Changed?.Invoke();
        }

        public bool HasAbility(string id) => AbilitySlots[0] == id || AbilitySlots[1] == id;

        private void AssignAbility(string id)
        {
            if (AbilitySlots[0] == null) AbilitySlots[0] = id;
            else if (AbilitySlots[1] == null) AbilitySlots[1] = id;
            else AbilitySlots[1] = id;   // новый навык заменяет навык на R
        }

        /// <summary>Для подсказки на карточке: в какой слот встанет навык.</summary>
        public string AbilitySlotHint() => AbilitySlots[0] == null ? "Q" : AbilitySlots[1] == null ? "R" : "R (заменит текущий)";

        // ---------------------------------------------------------------- Фокус

        public void AddFocus(float amount)
        {
            if (amount <= 0f) return;
            Focus = Mathf.Min(hero.focusMax, Focus + amount * (1f + Sum(PerkKind.FocusGain)) * Tower.FocusMultiplierAt(transform.position));
            Changed?.Invoke();
        }

        public void SpendFocus() { Focus = 0f; Changed?.Invoke(); }

        public void OnPlayerHit(Enemy e, float dmg)
        {
            AddFocus(hero.focusPerHit);
            if (!e.IsAlive) AddFocus(hero.focusPerKill);
        }

        public void OnReaction(bool byPlayer) => AddFocus(hero.focusPerReaction * (byPlayer ? 1f : 0.4f));

        // ---------------------------------------------------------------- оружие

        public int UpgradeCost(WeaponType w)
        {
            int lvl = w == WeaponType.Sword ? SwordLevel : BowLevel;
            return lvl >= 5 ? 0 : hero.weaponUpgradeCosts[Mathf.Clamp(lvl - 1, 0, hero.weaponUpgradeCosts.Length - 1)];
        }

        public bool UpgradeWeapon(WeaponType w)
        {
            var gm = GameManager.Instance;
            int cost = UpgradeCost(w);
            if (cost <= 0 || gm.Phase != GamePhase.Preparation || !gm.TrySpend(cost)) return false;
            if (w == WeaponType.Sword) SwordLevel++; else BowLevel++;
            Changed?.Invoke();
            return true;
        }

        public int EnchantCost => enchantBought ? hero.enchantSwitchCost : hero.enchantCost;

        public bool SetEnchant(Element e)
        {
            var gm = GameManager.Instance;
            if (e == Enchant || gm.Phase != GamePhase.Preparation || !gm.TrySpend(EnchantCost)) return false;
            Enchant = e;
            enchantBought = true;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Описание особых свойств оружия по уровню (для меню).</summary>
        public static string WeaponPerks(WeaponType w, int level)
        {
            if (w == WeaponType.Sword)
                return level >= 5 ? "рывок на 3-м ударе + волна клинка" : level >= 3 ? "рывок на 3-м ударе серии" : "ур.3: рывок, ур.5: волна клинка";
            return level >= 5 ? "пробивание + веер из 3 стрел" : level >= 3 ? "стрела пробивает врагов" : "ур.3: пробивание, ур.5: веер из 3 стрел";
        }
    }
}
