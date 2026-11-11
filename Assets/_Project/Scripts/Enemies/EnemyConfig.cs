using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Параметры типа врага (пеший, щитоносец, летающий, босс).</summary>
    [CreateAssetMenu(menuName = "Coliseum/Enemy Config", fileName = "Enemy")]
    public class EnemyConfig : ScriptableObject
    {
        public string displayName = "Враг";
        [Tooltip("Префаб, который спавнит волна")]
        public Enemy prefab;

        [Header("Живучесть")]
        public float maxHP = 60f;
        [Tooltip("Броня: снимается только ударами игрока. Пока она есть, башни бьют слабо")]
        public float armor = 0f;
        [Range(0f, 1f)] public float armorTowerDamageMultiplier = 0.15f;
        [Tooltip("Щит спереди: башни не наносят урона, если стреляют в переднюю полусферу")]
        public bool hasShield;
        [Tooltip("Половина угла, который закрывает щит, градусы")]
        public float shieldHalfAngle = 70f;

        [Header("Движение")]
        public bool isFlying;
        public float moveSpeed = 3.5f;
        [Tooltip("Радиус тела, нужен для дальности меча и атак")]
        public float bodyRadius = 0.5f;
        [Tooltip("Высота точки прицеливания (центр тела)")]
        public float aimHeight = 1f;

        [Header("Атака")]
        public float attackDamage = 10f;
        public float attackInterval = 1.2f;
        public float attackRange = 1.5f;

        [Header("Награда")]
        public int essenceReward = 6;

        [Header("Агрессия")]
        [Tooltip("Если игрок ближе — враг отвлекается на него")]
        public float playerAggroRange = 3f;
        [Tooltip("Если игрок убежал дальше — враг возвращается на маршрут")]
        public float playerLeashRange = 8f;
        [Tooltip("Башни ближе этого расстояния могут стать целью")]
        public float towerAggroRange = 7f;
        [Range(0f, 1f)] public float towerAggroChance = 0.2f;
        public float towerAggroCheckInterval = 2f;
        [Tooltip("Сколько секунд враг бьёт башню, прежде чем вернуться на маршрут")]
        public float towerAggroDuration = 8f;

        [Header("Полёт (только для летающих)")]
        [Tooltip("Высота полёта над точкой спавна")]
        public float cruiseHeight = 0f;
        [Tooltip("На какой высоте над целью зависает для атаки")]
        public float hoverHeight = 3f;
        public float hoverRadius = 3.5f;
        [Tooltip("С какого горизонтального расстояния до цели начинается пике")]
        public float diveDistance = 16f;
    }
}
