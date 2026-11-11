using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Параметры игрока: движение, камера, меч, лук.</summary>
    [CreateAssetMenu(menuName = "Coliseum/Player Config", fileName = "PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("Здоровье")]
        public float maxHP = 200f;
        public float respawnTime = 5f;

        [Header("Движение")]
        public float walkSpeed = 6f;
        public float sprintSpeed = 9.5f;
        public float jumpHeight = 1.4f;
        public float gravity = -22f;
        [Tooltip("Скорость поворота персонажа в 3-м лице, град/с")]
        public float turnSpeed = 720f;

        [Header("Камера")]
        [Tooltip("Чувствительность мыши в 1-м лице, град за пиксель")]
        public float firstPersonSensitivity = 0.1f;
        [Tooltip("Чувствительность орбиты 3-го лица по горизонтали, град за пиксель")]
        public float orbitSensitivityX = 0.12f;
        [Tooltip("Чувствительность орбиты 3-го лица по вертикали, доля за пиксель")]
        public float orbitSensitivityY = 0.0015f;

        [Header("Меч (ЛКМ)")]
        public float swordDamage = 30f;
        public float swordRange = 2.8f;
        [Tooltip("Ширина дуги удара, градусы")]
        public float swordArc = 130f;
        [Tooltip("Насколько выше/ниже центра игрока достаёт меч")]
        public float swordVerticalReach = 2f;
        public float swordCooldown = 0.45f;

        [Header("Лук (удерживать ЛКМ)")]
        public float bowMinDamage = 10f;
        public float bowMaxDamage = 45f;
        [Tooltip("Время полного натяжения, с")]
        public float bowChargeTime = 1f;
        public float arrowMinSpeed = 25f;
        public float arrowMaxSpeed = 65f;
        public float arrowGravity = 6f;
        public float bowCooldown = 0.25f;

        [Header("Взаимодействие")]
        [Tooltip("Максимальное расстояние от игрока до слота/башни для меню (F)")]
        public float interactDistance = 6f;
    }
}
