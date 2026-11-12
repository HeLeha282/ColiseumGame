using UnityEngine;
using UnityEngine.AI;

namespace ColiseumGame
{
    /// <summary>
    /// Наземный враг: идёт по точкам маршрута через NavMesh к порталу и бьёт его (дальнобойные — с дистанции).
    /// По пути упирается в баррикады и ломает их (сатиры перепрыгивают), отвлекается на игрока.
    /// На башни внимания не обращает.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class GroundEnemyAI : EnemyAIBase
    {
        private enum Mode { Route, Portal, Barricade, Player, Jump }

        [SerializeField] private float waypointReachDistance = 1.5f;
        [SerializeField] private float scanInterval = 0.2f;
        [SerializeField] private float barricadeBlockDistance = 1.2f;
        [SerializeField] private float jumpDuration = 0.7f;
        [SerializeField] private float jumpHeight = 2.2f;

        private NavMeshAgent agent;
        private Route route;
        private int waypoint;
        private Mode mode;
        private float scanTimer;
        private Barricade barricade;
        private Vector3 jumpFrom, jumpTo;
        private float jumpT;

        private void Awake() => agent = GetComponent<NavMeshAgent>();

        public override void Init(Enemy owner, Route r)
        {
            base.Init(owner, r);
            route = r;
            agent.speed = Cfg.moveSpeed;
            agent.Warp(transform.position);
            waypoint = 1;
            ResumeRoute();
        }

        public override void InheritProgress(EnemyAIBase parent)
        {
            if (parent is GroundEnemyAI g && route != null)
            {
                waypoint = Mathf.Clamp(g.waypoint, 1, route.Count);
                ResumeRoute();
            }
        }

        private void Update()
        {
            if (enemy == null || !enemy.IsAlive || route == null) return;
            if (mode == Mode.Jump)
            {
                UpdateJump();
                return;
            }

            agent.speed = Cfg.moveSpeed * enemy.SpeedMultiplier;
            if (pushVelocity.sqrMagnitude > 0.01f) agent.Move(pushVelocity * Time.deltaTime);
            enemy.Velocity = agent.velocity;

            scanTimer -= Time.deltaTime;
            if (scanTimer <= 0f)
            {
                scanTimer = scanInterval;
                Scan();
            }

            switch (mode)
            {
                case Mode.Route: UpdateRoute(); break;
                case Mode.Portal: UpdatePortal(); break;
                case Mode.Barricade: UpdateBarricade(); break;
                case Mode.Player: UpdatePlayer(); break;
            }
        }

        private void Scan()
        {
            if (mode == Mode.Barricade) return;
            var blocking = Barricade.FindBlocking(transform.position + Vector3.up, barricadeBlockDistance + enemy.Radius);
            if (blocking != null)
            {
                if (Cfg.jumpsBarricades) StartJump(blocking);
                else
                {
                    barricade = blocking;
                    SetMode(Mode.Barricade);
                }
                return;
            }

            var player = PlayerController.Instance;
            float aggro = Cfg.playerAggroRange + (IsRanged ? Cfg.attackRange * 0.6f : 0f);
            if (mode != Mode.Player && player != null && player.IsAlive &&
                Vector3.Distance(transform.position, player.transform.position) < aggro)
                SetMode(Mode.Player);
        }

        private void SetMode(Mode newMode)
        {
            mode = newMode;
            ResetAttackTimer();
            agent.isStopped = newMode == Mode.Barricade;
        }

        // ---------- маршрут

        private void UpdateRoute()
        {
            enemy.Progress = waypoint * 100f - (agent.hasPath ? agent.remainingDistance : 0f);
            if (agent.pathPending) return;
            if (agent.remainingDistance <= waypointReachDistance)
            {
                waypoint++;
                ResumeRoute();
            }
        }

        private void ResumeRoute()
        {
            agent.isStopped = false;
            if (waypoint >= route.Count)
            {
                mode = Mode.Portal;
                return;
            }
            mode = Mode.Route;
            agent.SetDestination(route.GetPoint(waypoint));
        }

        // ---------- портал

        private void UpdatePortal()
        {
            var portal = Portal.Instance;
            if (portal == null || !portal.IsAlive) return;
            enemy.Progress = route.Count * 100f + 50f;
            Vector3 surface = portal.ClosestPoint(transform.position + Vector3.up);
            if (InReach(surface))
            {
                agent.isStopped = true;
                Face(portal.transform.position);
                AttackTick(portal, surface);
            }
            else
            {
                agent.isStopped = false;
                Vector3 dir = transform.position - portal.transform.position;
                dir.y = 0f;
                float keep = IsRanged ? Cfg.attackRange * 0.8f : Cfg.attackRange * 0.5f;
                Vector3 target = surface + dir.normalized * keep;
                if (NavMesh.SamplePosition(target, out var hit, 3f, NavMesh.AllAreas)) target = hit.position;
                if (!agent.hasPath || Vector3.Distance(agent.destination, target) > 1f) agent.SetDestination(target);
            }
        }

        // ---------- баррикада

        private void UpdateBarricade()
        {
            if (barricade == null || !barricade.IsAlive)
            {
                barricade = null;
                ResumeRoute();
                return;
            }
            Face(barricade.transform.position);
            AttackTick(barricade, barricade.transform.position + Vector3.up);
        }

        private void StartJump(Barricade b)
        {
            Vector3 forward = agent.velocity.sqrMagnitude > 0.1f ? agent.velocity.normalized : transform.forward;
            forward.y = 0f;
            Vector3 land = b.transform.position + forward.normalized * 2.5f;
            if (!NavMesh.SamplePosition(land, out var hit, 3f, NavMesh.AllAreas))
            {
                barricade = b;
                SetMode(Mode.Barricade);
                return;
            }
            jumpFrom = transform.position;
            jumpTo = hit.position;
            jumpT = 0f;
            agent.isStopped = true;
            agent.updatePosition = false;
            mode = Mode.Jump;
        }

        private void UpdateJump()
        {
            jumpT += Time.deltaTime / jumpDuration;
            float k = Mathf.Clamp01(jumpT);
            transform.position = Vector3.Lerp(jumpFrom, jumpTo, k) + Vector3.up * (4f * jumpHeight * k * (1f - k));
            if (k >= 1f)
            {
                agent.Warp(jumpTo);
                agent.updatePosition = true;
                ResumeRoute();
            }
        }

        // ---------- игрок

        private void UpdatePlayer()
        {
            var player = PlayerController.Instance;
            if (player == null || !player.IsAlive ||
                Vector3.Distance(transform.position, player.transform.position) > Cfg.playerLeashRange + (IsRanged ? Cfg.attackRange : 0f))
            {
                ResumeRoute();
                return;
            }
            Vector3 target = player.transform.position;
            if (InReach(target + Vector3.up))
            {
                agent.isStopped = true;
                Face(target);
                AttackTick(player, target + Vector3.up);
            }
            else
            {
                agent.isStopped = false;
                agent.SetDestination(target);
            }
        }

        private bool InReach(Vector3 point)
        {
            Vector3 from = transform.position + Vector3.up;
            Vector3 d = point - from;
            if (!IsRanged && Mathf.Abs(d.y) > 2f) return false;
            d.y = 0f;
            return d.magnitude <= Cfg.attackRange + enemy.Radius;
        }
    }
}
