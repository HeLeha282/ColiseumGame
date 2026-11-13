using System;
using UnityEngine;

namespace ColiseumGame
{
    [Serializable]
    public class DifficultyPreset
    {
        public string name = "Центурион";
        [TextArea] public string description = "";
        public float enemyHp = 1f;
        public float enemyDamage = 1f;
        public float income = 1f;
    }

    /// <summary>
    /// Общий баланс игры: эссенция, стихии, установка башен, экономика волн, сложность, список башен и волн.
    /// Все числа правятся в инспекторе ассета GameBalance, а не в коде.
    /// </summary>
    [CreateAssetMenu(menuName = "Coliseum/Game Balance", fileName = "GameBalance")]
    public class GameBalance : ScriptableObject
    {
        [Header("Эссенция")]
        public int startEssence = 150;

        [Header("Метка игрока")]
        public float markDuration = 4f;
        public float markTowerDamageMultiplier = 1.5f;

        [Header("Стихии и реакции")]
        [Tooltip("Сколько секунд стихия висит на враге в ожидании реакции")]
        public float elementAuraDuration = 5f;
        [Tooltip("Раскол (огонь+лёд): доп. урон = удар × множитель")]
        public float shatterMultiplier = 2f;
        [Tooltip("Перегрузка (огонь+молния): урон всем в радиусе = удар × множитель")]
        public float overloadMultiplier = 1.2f;
        public float overloadRadius = 3.5f;
        public float overloadKnockback = 5f;
        [Tooltip("Хрупкость (лёд+молния)")]
        public float brittleDuration = 5f;
        public float brittleDamageMultiplier = 1.4f;
        [Tooltip("Распад (пустота + любая): доля макс. HP")]
        public float decayPercent = 0.06f;
        public float decayBossPercent = 0.02f;
        [Tooltip("Урон противоположной стихией по стихийному врагу")]
        public float elementWeaknessMultiplier = 1.5f;

        [Header("Портал")]
        public float portalMaxHP = 800f;
        [Tooltip("Восстановление портала после каждой волны (доля макс. HP)")]
        public float portalRegenPercent = 0.05f;

        [Header("Баррикады")]
        public int barricadeCost = 30;
        public float barricadeHP = 250f;

        [Header("Установка башен")]
        [Tooltip("Минимальное расстояние между центрами башен")]
        public float minTowerDistance = 4f;
        [Tooltip("Отступ от пути врагов")]
        public float routeClearance = 1.6f;
        public float portalClearance = 7f;
        public float barricadeClearance = 3f;
        [Tooltip("Как далеко от игрока можно ставить")]
        public float maxBuildDistance = 16f;
        [Range(0f, 1f)] public float sellRefund = 0.75f;

        [Header("Экономика волн")]
        [Tooltip("Бонус за волну без урона порталу (доля от бонуса волны)")]
        public float cleanWaveBonus = 0.2f;
        [Tooltip("Бонус за досрочный вызов следующей волны (доля её бонуса)")]
        public float earlyCallBonus = 0.25f;

        [Header("Сложность (выбирается в начале)")]
        public DifficultyPreset[] difficulties = new DifficultyPreset[0];
        public int defaultDifficulty = 1;

        [Header("Башни")]
        public TowerConfig[] towerTypes;

        [Header("Герой")]
        public HeroConfig hero;

        [Header("Волны (кампания)")]
        public WaveConfig[] waves;

        [Header("Бесконечный режим (после кампании)")]
        public EnemyConfig[] endlessPool = new EnemyConfig[0];
        public EnemyConfig[] bossPool = new EnemyConfig[0];
        public float endlessHpGrowth = 0.12f;
    }
}
