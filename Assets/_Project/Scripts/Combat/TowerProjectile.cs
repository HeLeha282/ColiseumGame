using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Снаряд башни. Без взрыва — летит за целью (лучник). Со взрывом — летит в точку
    /// с упреждением и бьёт по области всех наземных врагов (баллиста).
    /// </summary>
    public class TowerProjectile : MonoBehaviour
    {
        [SerializeField] private GameObject explosionPrefab;
        [SerializeField] private float maxLifetime = 4f;

        private Enemy target;
        private Vector3 targetPoint;
        private float damage, speed, aoe;
        private bool hitsAir;
        private Vector3 origin;
        private float life;

        public void Launch(Enemy enemy, float dmg, float projectileSpeed, float aoeRadius, bool canHitAir, Vector3 sourcePosition)
        {
            target = enemy;
            damage = dmg;
            speed = projectileSpeed;
            aoe = aoeRadius;
            hitsAir = canHitAir;
            origin = sourcePosition;

            targetPoint = enemy.AimPoint;
            if (aoe > 0f)
            {
                // Упреждение: куда враг дойдёт за время полёта.
                float t = Vector3.Distance(transform.position, targetPoint) / speed;
                targetPoint = enemy.transform.position + enemy.Velocity * t;
            }
            life = maxLifetime;
        }

        private void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (aoe <= 0f && target != null && target.IsAlive) targetPoint = target.AimPoint;

            Vector3 next = Vector3.MoveTowards(transform.position, targetPoint, speed * Time.deltaTime);
            Vector3 dir = next - transform.position;
            if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir);
            transform.position = next;

            if (Vector3.Distance(next, targetPoint) < 0.05f) Impact();
        }

        private void Impact()
        {
            if (aoe > 0f)
            {
                foreach (var e in Enemy.All.ToArray())
                {
                    if (e == null || !e.IsAlive) continue;
                    if (e.IsFlying && !hitsAir) continue;
                    if (Vector3.Distance(e.transform.position, targetPoint) <= aoe + e.Radius)
                        e.TakeDamage(new DamageInfo(damage, DamageSource.Tower, origin));
                }
                if (explosionPrefab != null)
                {
                    var fx = Instantiate(explosionPrefab, targetPoint, Quaternion.identity);
                    fx.transform.localScale = Vector3.one * aoe * 2f;
                }
            }
            else if (target != null && target.IsAlive)
            {
                target.TakeDamage(new DamageInfo(damage, DamageSource.Tower, origin));
            }
            Destroy(gameObject);
        }
    }
}
