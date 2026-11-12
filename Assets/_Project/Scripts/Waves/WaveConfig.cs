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
        [Tooltip("Отряд: враги выходят плотным строем (тестудо)")]
        public bool squad;
        [Tooltip("Доля врагов, идущих по развилке (обходному пути)")]
        [Range(0f, 1f)] public float altRouteChance = 0f;
        [Tooltip("Модификаторы элиты для всей группы")]
        public EliteModifier elite = EliteModifier.None;
    }

    /// <summary>Состав одной волны.</summary>
    [CreateAssetMenu(menuName = "Coliseum/Wave Config", fileName = "Wave")]
    public class WaveConfig : ScriptableObject
    {
        [Tooltip("Название для экрана подготовки")]
        public string title = "";
        public int act = 1;
        public SpawnGroup[] groups;
        [Tooltip("Множитель HP врагов волны")]
        public float hpMultiplier = 1f;
        [Tooltip("Бонус эссенции за прохождение волны")]
        public int completionBonus = 40;
        [Tooltip("Слава за прохождение волны")]
        public int gloryBonus = 10;
    }
}
