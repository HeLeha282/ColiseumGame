using System.Collections.Generic;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Площадка под башню. Через меню (F) строим, улучшаем, продаём башню — только в фазе подготовки.</summary>
    public class TowerSlot : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform mount;
        [SerializeField] private GameObject padVisual;

        public Tower Tower { get; private set; }

        private static GameManager GM => GameManager.Instance;
        private static bool IsPrep => GM != null && GM.Phase == GamePhase.Preparation;

        public string Title => Tower != null ? $"{Tower.Config.displayName} — уровень {Tower.Level + 1}" : "Слот для башни";

        public string Prompt
        {
            get
            {
                if (Tower == null) return "[F] Построить башню";
                string repair = Tower.IsDamaged && GM != null && GM.Phase == GamePhase.Wave ? "   [E] Чинить (держать)" : "";
                return $"[F] {Tower.Config.displayName} ур.{Tower.Level + 1}{repair}";
            }
        }

        public string Description
        {
            get
            {
                string phase = IsPrep ? "" : "\nСтроить и улучшать можно только в фазе подготовки.";
                if (Tower == null) return "Выберите башню." + phase;
                var s = Tower.Stats;
                string kind = s.aoeRadius > 0f ? $"по области {s.aoeRadius:0.#} м" : "одиночная цель";
                return $"HP {Tower.Health.Current:0}/{Tower.Health.Max:0} · урон {s.damage:0} · {kind} · дальность {s.range:0} м" + phase;
            }
        }

        public void FillMenu(List<MenuOption> options)
        {
            if (GM == null) return;
            if (Tower == null)
            {
                foreach (var type in GM.Balance.towerTypes)
                {
                    var t = type;
                    int cost = t.levels[0].cost;
                    string air = t.targetsAir ? (t.targetsGround ? "земля+воздух" : "воздух") : "только земля";
                    options.Add(new MenuOption($"{t.displayName} ({air}) — {cost}", IsPrep && GM.CanAfford(cost), () => Build(t)));
                }
            }
            else
            {
                if (Tower.CanUpgrade)
                {
                    int cost = Tower.UpgradeCost;
                    options.Add(new MenuOption($"Улучшить до ур.{Tower.Level + 2} — {cost}", IsPrep && GM.CanAfford(cost), Upgrade));
                }
                int refund = Mathf.RoundToInt(Tower.TotalInvested * GM.Balance.sellRefund);
                options.Add(new MenuOption($"Продать (+{refund})", IsPrep, Sell));
            }
        }

        private void Build(TowerConfig type)
        {
            if (Tower != null || !IsPrep || !GM.TrySpend(type.levels[0].cost)) return;
            Tower = Instantiate(type.prefab, mount.position, mount.rotation, transform);
            Tower.Init(this);
            if (padVisual != null) padVisual.SetActive(false);
        }

        private void Upgrade()
        {
            if (Tower == null || !Tower.CanUpgrade || !IsPrep || !GM.TrySpend(Tower.UpgradeCost)) return;
            Tower.Upgrade();
        }

        private void Sell()
        {
            if (Tower == null || !IsPrep) return;
            GM.AddEssence(Mathf.RoundToInt(Tower.TotalInvested * GM.Balance.sellRefund));
            Destroy(Tower.gameObject);
            OnTowerDestroyed();
        }

        public void OnTowerDestroyed()
        {
            Tower = null;
            if (padVisual != null) padVisual.SetActive(true);
        }
    }
}
