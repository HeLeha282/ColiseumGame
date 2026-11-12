using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Летающий враг: появляется высоко над аркой, летит по прямой к порталу и пикирует на него.
    /// Если его сбили (сеть Снайпера, гарпун Скорпиона, Громоотвод) — падает к земле и становится уязвим для меча.
    /// </summary>
    public class FlyingEnemyAI : EnemyAIBase
    {
        [SerializeField] private float turnSpeed = 360f;
        [SerializeField] private float fallSpeed = 12f;

        private Vector3 hoverOffset;
        private float cruiseY;

        public override void Init(Enemy owner, Route route)
        {
            base.Init(owner, route);
            transform.position = route.FlyerSpawnPoint + Vector3.up * Cfg.cruiseHeight + Random.insideUnitSphere * 2f;
            cruiseY = transform.position.y;
            var portal = Portal.Instance;
            Vector2 c = Random.insideUnitCircle.normalized * Cfg.hoverRadius;
            hoverOffset = new Vector3(c.x, (portal != null ? portal.Height : 4f) + Cfg.hoverHeight, c.y);
        }

        private void Update()
        {
            var portal = Portal.Instance;
            if (enemy == null || !enemy.IsAlive || portal == null) return;
            Vector3 pos = transform.position;

            // Сбит на землю: падает и стоит (почти) на месте
            if (enemy.GroundedTime > 0f)
            {
                float groundY = 0f;
                if (Physics.Raycast(pos + Vector3.up, Vector3.down, out var hit, 60f, ~LayerMask.GetMask("CameraBlocker"), QueryTriggerInteraction.Ignore))
                    groundY = hit.point.y;
                transform.position = Vector3.MoveTowards(pos, new Vector3(pos.x, groundY + 0.8f, pos.z), fallSpeed * Time.deltaTime);
                enemy.Velocity = Vector3.zero;
                return;
            }

            Vector3 hoverPoint = portal.transform.position + hoverOffset;
            Vector3 flat = new Vector3(hoverPoint.x - pos.x, 0f, hoverPoint.z - pos.z);
            float flatDist = flat.magnitude;
            float top = Mathf.Max(cruiseY, hoverPoint.y);
            float desiredY = flatDist > Cfg.diveDistance ? top : Mathf.Lerp(hoverPoint.y, top, flatDist / Cfg.diveDistance);
            Vector3 desired = new Vector3(hoverPoint.x, desiredY, hoverPoint.z);
            Vector3 next = Vector3.MoveTowards(pos, desired, Cfg.moveSpeed * enemy.SpeedMultiplier * Time.deltaTime) + pushVelocity * Time.deltaTime;
            enemy.Velocity = (next - pos) / Mathf.Max(Time.deltaTime, 0.0001f);
            transform.position = next;

            Vector3 look = flatDist > 0.5f ? flat : portal.transform.position - pos;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look), turnSpeed * Time.deltaTime);

            enemy.Progress = 1500f - Vector3.Distance(pos, portal.transform.position);
            if (Vector3.Distance(next, hoverPoint) < 0.8f) AttackTick(portal, portal.ClosestPoint(pos));
        }
    }
}
