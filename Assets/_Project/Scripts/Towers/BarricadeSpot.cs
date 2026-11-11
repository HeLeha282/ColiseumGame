using System.Collections.Generic;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Заданное место на маршруте, куда в фазе подготовки можно поставить баррикаду (F).</summary>
    public class BarricadeSpot : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameObject markerVisual;

        public Barricade Barricade { get; private set; }

        private static GameManager GM => GameManager.Instance;
        private static bool IsPrep => GM != null && GM.Phase == GamePhase.Preparation;

        public string Title => Barricade != null ? "Баррикада" : "Место для баррикады";
        public string Prompt => Barricade != null ? "[F] Баррикада" : "[F] Поставить баррикаду";

        public string Description
        {
            get
            {
                if (Barricade != null) return $"HP {Barricade.Health.Current:0}/{Barricade.Health.Max:0}. Враги остановятся и будут её ломать.";
                string phase = IsPrep ? "" : "\nСтавить можно только в фазе подготовки.";
                return "Баррикада не меняет маршрут: враги упрутся в неё и начнут ломать." + phase;
            }
        }

        public void FillMenu(List<MenuOption> options)
        {
            if (GM == null || Barricade != null) return;
            int cost = GM.Balance.barricadeCost;
            options.Add(new MenuOption($"Поставить баррикаду — {cost}", IsPrep && GM.CanAfford(cost), Build));
        }

        private void Build()
        {
            if (Barricade != null || !IsPrep || !GM.TrySpend(GM.Balance.barricadeCost)) return;
            Barricade = Instantiate(GM.BarricadePrefab, transform.position, transform.rotation, transform);
            Barricade.Init(this, GM.Balance.barricadeHP);
            if (markerVisual != null) markerVisual.SetActive(false);
        }

        public void OnBarricadeBroken()
        {
            Barricade = null;
            if (markerVisual != null) markerVisual.SetActive(true);
        }
    }
}
