using System;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Параметры одного уровня башни.</summary>
    [Serializable]
    public class TowerLevelStats
    {
        [Tooltip("Цена постройки (уровень 1) или улучшения до этого уровня")]
        public int cost = 60;
        public float damage = 12f;
        [Tooltip("Секунд между выстрелами")]
        public float fireInterval = 0.8f;
        public float range = 16f;
        public float maxHP = 150f;
        [Tooltip("Радиус взрыва. 0 — бьёт одну цель")]
        public float aoeRadius = 0f;
    }

    /// <summary>Тип башни (лучник, баллиста ...). Уровни 1→2→3 — массив levels.</summary>
    [CreateAssetMenu(menuName = "Coliseum/Tower Config", fileName = "Tower")]
    public class TowerConfig : ScriptableObject
    {
        public string displayName = "Башня";
        public Tower prefab;
        public bool targetsGround = true;
        public bool targetsAir = true;
        public float projectileSpeed = 40f;
        public TowerLevelStats[] levels = new TowerLevelStats[3];
    }
}
