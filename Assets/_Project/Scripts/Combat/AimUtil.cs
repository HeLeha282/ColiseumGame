using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Куда смотрит прицел в центре экрана.</summary>
    public static class AimUtil
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[32];

        /// <summary>
        /// Точка, в которую указывает центр экрана. Пропускает коллайдеры самого игрока
        /// и всё, что находится между камерой и игроком (в режиме 3-го лица).
        /// </summary>
        public static Vector3 GetAimPoint(Transform ignoreRoot, float maxDistance)
        {
            var cam = Camera.main;
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float minDistance = ignoreRoot != null
                ? Vector3.Dot(ignoreRoot.position + Vector3.up - ray.origin, ray.direction) - 0.5f
                : 0f;

            int count = Physics.RaycastNonAlloc(ray, Hits, maxDistance, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            Vector3 point = ray.GetPoint(maxDistance);
            for (int i = 0; i < count; i++)
            {
                var hit = Hits[i];
                if (hit.distance < minDistance) continue;
                if (ignoreRoot != null && hit.transform.IsChildOf(ignoreRoot)) continue;
                // Прицел «видит» сквозь портал и башни (стрелы через них тоже пролетают).
                if (hit.collider.GetComponentInParent<Portal>() != null || hit.collider.GetComponentInParent<Tower>() != null) continue;
                if (hit.distance < best)
                {
                    best = hit.distance;
                    point = hit.point;
                }
            }
            return point;
        }

        /// <summary>Направление «вперёд» камеры по горизонтали.</summary>
        public static Vector3 CameraForwardFlat()
        {
            var cam = Camera.main;
            if (cam == null) return Vector3.forward;
            Vector3 f = cam.transform.forward;
            f.y = 0f;
            return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
        }
    }
}
