using System;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Группа одинаковых врагов внутри волны.</summary>
    [Serializable]
    public class SpawnGroup
    {
        public EnemyConfig enemy;
        public int count = 5;
        [Tooltip("Задержка от начала волны до первого врага группы, с")]
        public float startDelay = 0f;
        [Tooltip("Интервал между врагами группы, с")]
        public float interval = 2f;
        [Tooltip("Номер арки 0..3. -1 — по очереди из всех арок")]
        public int arch = -1;
    }

    /// <summary>Состав одной волны.</summary>
    [CreateAssetMenu(menuName = "Coliseum/Wave Config", fileName = "Wave")]
    public class WaveConfig : ScriptableObject
    {
        public SpawnGroup[] groups;
        [Tooltip("Множитель HP и брони всех врагов волны")]
        public float hpMultiplier = 1f;
        [Tooltip("Бонус эссенции за прохождение волны")]
        public int completionBonus = 40;
    }
}
