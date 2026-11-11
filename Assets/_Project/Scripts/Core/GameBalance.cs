using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Общий баланс игры: стартовая эссенция, метка, портал, баррикады, ремонт, список башен и волн.
    /// Все числа правятся в инспекторе ассета GameBalance, а не в коде.
    /// </summary>
    [CreateAssetMenu(menuName = "Coliseum/Game Balance", fileName = "GameBalance")]
    public class GameBalance : ScriptableObject
    {
        [Header("Эссенция")]
        public int startEssence = 150;
        [Tooltip("Через сколько секунд исчезает сфера эссенции с убитого врага")]
        public float orbLifetime = 10f;
        public float orbPickupRadius = 1.5f;
        [Tooltip("С какого расстояния сфера сама летит к игроку")]
        public float orbMagnetRadius = 4f;
        public float orbMagnetSpeed = 14f;

        [Header("Метка игрока")]
        [Tooltip("Сколько секунд враг остаётся помеченным после удара игрока")]
        public float markDuration = 4f;
        [Tooltip("Множитель урона башен по помеченному врагу")]
        public float markTowerDamageMultiplier = 1.5f;

        [Header("Портал")]
        public float portalMaxHP = 800f;

        [Header("Баррикады")]
        public int barricadeCost = 30;
        public float barricadeHP = 250f;

        [Header("Ремонт башен (только в фазе волны)")]
        public float repairHpPerSecond = 40f;
        [Tooltip("Цена ремонта в эссенции за 1 HP")]
        public float repairCostPerHP = 0.25f;
        [Tooltip("Как близко игрок должен стоять к башне")]
        public float repairRadius = 4.5f;

        [Header("Башни")]
        public TowerConfig[] towerTypes;
        [Range(0f, 1f)] public float sellRefund = 0.5f;

        [Header("Волны (по порядку)")]
        public WaveConfig[] waves;
    }
}
