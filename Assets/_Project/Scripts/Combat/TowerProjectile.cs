using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Снаряд башни. Одиночный — летит за целью (лучник, скорпион). Площадной — летит в точку с упреждением
    /// (катапульта, по дуге). При попадании вся логика урона и перков — в Tower.ResolveImpact.
    /// </summary>
    public class TowerProjectile : MonoBehaviour
    {
        [SerializeField] private float maxLifetime = 4f;
        [SerializeField] private bool useArc;

        private Tower owner;
        private Enemy target;
        private Vector3 targetPoint, launchPoint;
        private float speed, life, flightTime, flightDuration, arcHeight;
        private bool area;

        public void Launch(Tower tower, Enemy enemy, float projectileSpeed, bool isArea)
        {
            owner = tower;
            target = enemy;
            speed = projectileSpeed;
            area = isArea;
            targetPoint = enemy.AimPoint;
            if (area && !enemy.IsFlying)
            {
                float t = Vector3.Distance(transform.position, targetPoint) / speed;
                targetPoint = enemy.transform.position + enemy.Velocity * t;
            }
            life = maxLifetime;
            launchPoint = transform.position;
            flightTime = 0f;
            flightDuration = Mathf.Clamp(Vector3.Distance(launchPoint, targetPoint) / Mathf.Max(1f, speed), 0.25f, 2.5f);
            arcHeight = Mathf.Clamp(Vector3.Distance(launchPoint, targetPoint) * 0.2f, 1.5f, 5f);
            if (useArc) life = Mathf.Max(life, flightDuration + 0.5f);
        }

        private void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f || owner == null)
            {
                Destroy(gameObject);
                return;
            }
            if (!area && target != null && target.IsAlive) targetPoint = target.AimPoint;

            Vector3 next = Vector3.MoveTowards(transform.position, targetPoint, speed * Time.deltaTime);
            if (useArc)
            {
                flightTime += Time.deltaTime;
                float t = Mathf.Clamp01(flightTime / flightDuration);
                next = Vector3.Lerp(launchPoint, targetPoint, t) + Vector3.up * (4f * arcHeight * t * (1f - t));
            }
            Vector3 dir = next - transform.position;
            if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir);
            transform.position = next;

            if (useArc ? flightTime >= flightDuration : Vector3.Distance(next, targetPoint) < 0.05f)
            {
                owner.ResolveImpact(targetPoint, target != null && target.IsAlive ? target : null);
                Destroy(gameObject);
            }
        }
    }
}
