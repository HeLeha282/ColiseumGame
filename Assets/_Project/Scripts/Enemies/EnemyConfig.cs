using UnityEngine;

namespace ColiseumGame
{
    public enum BossKind
    {
        None,
        Minotaur,
        Cerberus,
        Hydra,
        Medusa,
        HarpyQueen,
        Titan
    }

    /// <summary>Параметры типа врага. Все числа — здесь, а не в коде.</summary>
    [CreateAssetMenu(menuName = "Coliseum/Enemy Config", fileName = "Enemy")]
    public class EnemyConfig : ScriptableObject
    {
        public string displayName = "Враг";
        [Tooltip("Кто это (показывается при первом появлении и в превью волны)")]
        [TextArea] public string description = "";
        [Tooltip("Чем контрить — подсказка игроку")]
        [TextArea] public string counterHint = "";
        [Tooltip("Префаб, который спавнит волна")]
        public Enemy prefab;

        [Header("Живучесть")]
        public float maxHP = 60f;
        [Tooltip("Броня: снимается только ударами игрока. Пока она есть, башни бьют слабо")]
        public float armor = 0f;
        [Range(0f, 1f)] public float armorTowerDamageMultiplier = 0.15f;
        [Tooltip("Щит спереди: башни не наносят урона, если стреляют в переднюю полусферу")]
        public bool hasShield;
        public float shieldHalfAngle = 70f;
        [Tooltip("Стихия врага: противоположная бьёт ×1.5, своя — не действует")]
        public Element element = Element.None;

        [Header("Движение")]
        public bool isFlying;
        public float moveSpeed = 3.5f;
        public float bodyRadius = 0.5f;
        public float aimHeight = 1f;
        [Tooltip("Перепрыгивает баррикады вместо того, чтобы ломать их")]
        public bool jumpsBarricades;

        [Header("Атака")]
        public float attackDamage = 6f;
        public float attackInterval = 1.5f;
        [Tooltip("Дистанция атаки. Больше 4 — враг стреляет снарядами издалека")]
        public float attackRange = 1.2f;
        public float projectileSpeed = 18f;
        [Tooltip("Радиус взрыва снаряда (маги). 0 — одиночный урон")]
        public float splashRadius = 0f;
        [Range(0f, 0.9f)] public float slowPlayerOnHit = 0f;
        public float knockbackPlayer = 0f;

        [Header("Особенности")]
        [Tooltip("Лечит союзников вокруг: доля их макс. HP в секунду")]
        public float healRadius = 0f;
        public float healPercentPerSecond = 0f;
        [Tooltip("Невидим, пока его не подсветит игрок или Священное пламя")]
        public bool invisible;
        public float deathExplosionRadius = 0f;
        public float deathExplosionDamage = 0f;
        [Tooltip("После смерти делится на этих врагов (если не добит огнём)")]
        public EnemyConfig splitInto;
        public int splitCount = 0;

        [Header("Награда")]
        public int essenceReward = 4;
        public int gloryReward = 3;

        [Header("Агрессия на игрока")]
        public float playerAggroRange = 3f;
        public float playerLeashRange = 8f;

        [Header("Полёт (только для летающих)")]
        public float cruiseHeight = 0f;
        public float hoverHeight = 0.5f;
        public float hoverRadius = 3f;
        public float diveDistance = 18f;

        [Header("Босс")]
        public BossKind bossKind = BossKind.None;
        [Tooltip("Подсказка на экране, что делать с боссом")]
        [TextArea] public string bossHint = "";

        public bool IsBoss => bossKind != BossKind.None;
    }
}
