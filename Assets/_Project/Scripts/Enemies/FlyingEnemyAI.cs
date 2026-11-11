using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Летающий враг: появляется высоко над аркой, летит по прямой к порталу и пикирует на него,
    /// игнорируя маршрут. Иногда отвлекается на башню по пути.
    /// </summary>
    public class FlyingEnemyAI : EnemyAIBase
    {
        [SerializeField] private float turnSpeed = 360f;

        private Transform targetTransform;
        private IDamageable target;
        private Vector3 hoverOffset;
        private float cruiseY;
        private float towerCheckTimer;
        private float towerTimeLeft;
        private bool attackingTower;

        public override void Init(Enemy owner, Route route)
        {
            base.Init(owner, route);
            transform.position = route.FlyerSpawnPoint + Vector3.up * Cfg.cruiseHeight;
            cruiseY = transform.position.y;
            towerCheckTimer = Cfg.towerAggroCheckInterval;
            TargetPortal();
        }

        private void TargetPortal()
        {
            attackingTower = false;
            var portal = Portal.Instance;
            targetTransform = portal != null ? portal.transform : null;
            target = portal;
            PickHoverOffset(portal != null ? portal.Height : 4f);
        }

        private void PickHoverOffset(float targetTop)
        {
            Vector2 c = Random.insideUnitCircle.normalized * Cfg.hoverRadius;
            hoverOffset = new Vector3(c.x, targetTop + Cfg.hoverHeight, c.y);
        }

        private void Update()
        {
            if (enemy == null || !enemy.IsAlive) return;
            if (targetTransform == null || target == null || !target.IsAlive)
            {
                if (attackingTower || targetTransform == null) TargetPortal();
                if (targetTransform == null) return;
            }

            if (attackingTower)
            {
                towerTimeLeft -= Time.deltaTime;
                if (towerTimeLeft <= 0f) TargetPortal();
            }
            else
            {
                towerCheckTimer -= Time.deltaTime;
                if (towerCheckTimer <= 0f)
                {
                    towerCheckTimer = Cfg.towerAggroCheckInterval;
                    var t = RollTowerAggro();
                    if (t != null)
                    {
                        attackingTower = true;
                        towerTimeLeft = Cfg.towerAggroDuration;
                        targetTransform = t.transform;
                        target = t;
                        PickHoverOffset(t.Height);
                    }
                }
            }

            Vector3 hoverPoint = targetTransform.position + hoverOffset;
            Vector3 pos = transform.position;
            Vector3 flat = new Vector3(hoverPoint.x - pos.x, 0f, hoverPoint.z - pos.z);
            float flatDist = flat.magnitude;

            // Пока далеко — держим высоту, ближе diveDistance — пикируем к точке зависания.
            float desiredY = flatDist > Cfg.diveDistance
                ? Mathf.Max(cruiseY, hoverPoint.y)
                : Mathf.Lerp(hoverPoint.y, Mathf.Max(cruiseY, hoverPoint.y), flatDist / Cfg.diveDistance);
            Vector3 desired = new Vector3(hoverPoint.x, desiredY, hoverPoint.z);
            Vector3 next = Vector3.MoveTowards(pos, desired, Cfg.moveSpeed * Time.deltaTime);
            enemy.Velocity = (next - pos) / Mathf.Max(Time.deltaTime, 0.0001f);
            transform.position = next;

            Vector3 look = flatDist > 0.5f ? flat : targetTransform.position - pos;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look), turnSpeed * Time.deltaTime);

            enemy.Progress = 1500f - Vector3.Distance(pos, Portal.Instance != null ? Portal.Instance.transform.position : pos);

            if (Vector3.Distance(next, hoverPoint) < 0.6f) AttackTick(target);
        }
    }
}
