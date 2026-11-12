using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Маршрут врагов от одной арки до арены. Точки маршрута — дочерние объекты по порядку
    /// (первая — точка спавна в арке). Variant 0 — основной путь, 1 — развилка (открывается со 2-го акта).
    /// </summary>
    public class Route : MonoBehaviour
    {
        [Tooltip("Номер арки (0..3), к которой привязан маршрут")]
        [SerializeField] private int archIndex;
        [Tooltip("0 — основной путь, 1 — развилка/обход")]
        [SerializeField] private int variant;
        [Tooltip("На какой высоте над аркой появляются летающие враги")]
        [SerializeField] private float flyerSpawnHeight = 14f;
        [SerializeField] private Color gizmoColor = Color.yellow;

        public int ArchIndex => archIndex;
        public int Variant => variant;
        public int Count => transform.childCount;
        public Vector3 GetPoint(int index) => transform.GetChild(index).position;
        public Vector3 SpawnPoint => GetPoint(0);
        public Vector3 FlyerSpawnPoint => SpawnPoint + Vector3.up * flyerSpawnHeight;

        private void OnDrawGizmos()
        {
            Gizmos.color = variant == 0 ? gizmoColor : gizmoColor * 0.6f;
            for (int i = 0; i < transform.childCount; i++)
            {
                Vector3 p = transform.GetChild(i).position;
                Gizmos.DrawSphere(p + Vector3.up * 0.3f, variant == 0 ? 0.35f : 0.25f);
                if (i > 0) Gizmos.DrawLine(transform.GetChild(i - 1).position + Vector3.up * 0.3f, p + Vector3.up * 0.3f);
            }
        }
    }
}
