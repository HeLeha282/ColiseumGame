using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Маршрут врагов от одной арки до арены. Точки маршрута — дочерние объекты по порядку
    /// (первая — точка спавна в арке). Чтобы поменять маршрут, двигайте/добавляйте дочерние точки.
    /// </summary>
    public class Route : MonoBehaviour
    {
        [Tooltip("Номер арки (0..3), к которой привязан маршрут")]
        [SerializeField] private int archIndex;
        [Tooltip("На какой высоте над аркой появляются летающие враги")]
        [SerializeField] private float flyerSpawnHeight = 14f;
        [SerializeField] private Color gizmoColor = Color.yellow;

        public int ArchIndex => archIndex;
        public int Count => transform.childCount;
        public Vector3 GetPoint(int index) => transform.GetChild(index).position;
        public Vector3 SpawnPoint => GetPoint(0);
        public Vector3 FlyerSpawnPoint => SpawnPoint + Vector3.up * flyerSpawnHeight;

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            for (int i = 0; i < transform.childCount; i++)
            {
                Vector3 p = transform.GetChild(i).position;
                Gizmos.DrawSphere(p + Vector3.up * 0.3f, 0.35f);
                if (i > 0) Gizmos.DrawLine(transform.GetChild(i - 1).position + Vector3.up * 0.3f, p + Vector3.up * 0.3f);
            }
        }
    }
}
