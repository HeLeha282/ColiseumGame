using UnityEngine;
using UnityEngine.AI;

namespace ColiseumGame
{
    /// <summary>
    /// Наземный враг: идёт по точкам маршрута через NavMesh, у портала бьёт портал.
    /// По пути может застрять на баррикаде (ломает её), отвлечься на игрока или на башню.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class GroundEnemyAI : EnemyAIBase
    {
        private enum Mode
        {
            Route,
            Portal,
            Barricade,
            Tower,
            Player
        }

        [Tooltip("На каком расстоянии точка маршрута считается пройденной")]
        [SerializeField] private float waypointReachDistance = 1.5f;
        [Tooltip("Как часто враг оглядывается в поисках баррикад/игрока, с")]
        [SerializeField] private float scanInterval = 0.2f;
        [Tooltip("Расстояние до баррикады, на котором враг упирается в неё")]
        [SerializeField] private float barricadeBlockDistance = 1.2f;

        private NavMeshAgent agent;
        private Route route;
        private int waypoint;
        private Mode mode;
        private float scanTimer;
        private float towerCheckTimer;
        private float towerTimeLeft;
        private Barricade barricade;
        private Tower tower;

        private void Awake() => agent = GetComponent<NavMeshAgent>();

        public override void Init(Enemy owner, Route r)
        {
            base.Init(owner, r);
            route = r;
            agent.speed = Cfg.moveSpeed;
            agent.Warp(transform.position);
            waypoint = 1;
            towerCheckTimer = Cfg.towerAggroCheckInterval;
            ResumeRoute();
        }

        private void Update()
        {
            if (enemy == null || !enemy.IsAlive || route == null) return;
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
                case Mode.Tower: UpdateTower(); break;
                case Mode.Player: UpdatePlayer(); break;
            }
        }

        // ---------- Поиск помех и отвлечений ----------

        private void Scan()
        {
            if (mode == Mode.Barricade) return;

            var blocking = Barricade.FindBlocking(transform.position + Vector3.up, barricadeBlockDistance + enemy.Radius);
            if (blocking != null)
            {
                barricade = blocking;
                SetMode(Mode.Barricade);
                return;
            }

            var player = PlayerController.Instance;
            if (mode != Mode.Player && player != null && player.IsAlive &&
                Vector3.Distance(transform.position, player.transform.position) < Cfg.playerAggroRange)
            {
                SetMode(Mode.Player);
                return;
            }

            if (mode == Mode.Route)
            {
                towerCheckTimer -= scanInterval;
                if (towerCheckTimer <= 0f)
                {
                    towerCheckTimer = Cfg.towerAggroCheckInterval;
                    var t = RollTowerAggro();
                    if (t != null)
                    {
                        tower = t;
                        towerTimeLeft = Cfg.towerAggroDuration;
                        SetMode(Mode.Tower);
                    }
                }
            }
        }

        private void SetMode(Mode newMode)
        {
            mode = newMode;
            ResetAttackTimer();
            agent.isStopped = newMode == Mode.Barricade;
        }

        // ---------- Режимы ----------

        private void UpdateRoute()
        {
            enemy.Progress = waypoint * 100f - (agent.hasPath ? agent.remainingDistance : 0f);
            if (agent.pathPending) return;
            if (agent.remainingDistance <= waypointReachDistance)
            {
                waypoint++;
                if (waypoint >= route.Count) SetMode(Mode.Portal);
                ResumeRoute();
            }
        }

        private void ResumeRoute()
        {
            if (waypoint >= route.Count)
            {
                mode = Mode.Portal;
                agent.isStopped = false;
                return;
            }
            mode = Mode.Route;
            agent.isStopped = false;
            agent.SetDestination(route.GetPoint(waypoint));
        }

        private void UpdatePortal()
        {
            var portal = Portal.Instance;
            if (portal == null || !portal.IsAlive) return;
            enemy.Progress = route.Count * 100f;
            if (MeleeReach(portal.ClosestPoint(transform.position + Vector3.up)))
            {
                agent.isStopped = true;
                Face(portal.transform.position);
                AttackTick(portal);
            }
            else
            {
                agent.isStopped = false;
                Vector3 dir = transform.position - portal.transform.position;
                dir.y = 0f;
                Vector3 target = portal.ClosestPoint(transform.position + Vector3.up) + dir.normalized * (Cfg.attackRange * 0.5f);
                if (!agent.hasPath || Vector3.Distance(agent.destination, target) > 1f) agent.SetDestination(target);
            }
        }

        private void UpdateBarricade()
        {
            if (barricade == null || !barricade.IsAlive)
            {
                barricade = null;
                ResumeRoute();
                return;
            }
            Face(barricade.transform.position);
            AttackTick(barricade);
        }

        private void UpdateTower()
        {
            towerTimeLeft -= Time.deltaTime;
            if (tower == null || !tower.IsAlive || towerTimeLeft <= 0f)
            {
                tower = null;
                ResumeRoute();
                return;
            }
            Vector3 closest = tower.ClosestPoint(transform.position + Vector3.up);
            if (MeleeReach(closest))
            {
                agent.isStopped = true;
                Face(tower.transform.position);
                AttackTick(tower);
            }
            else
            {
                agent.isStopped = false;
                if (!agent.hasPath || Vector3.Distance(agent.destination, closest) > 1f) agent.SetDestination(closest);
            }
        }

        private void UpdatePlayer()
        {
            var player = PlayerController.Instance;
            if (player == null || !player.IsAlive ||
                Vector3.Distance(transform.position, player.transform.position) > Cfg.playerLeashRange)
            {
                ResumeRoute();
                return;
            }
            Vector3 target = player.transform.position;
            if (MeleeReach(target + Vector3.up))
            {
                agent.isStopped = true;
                Face(target);
                AttackTick(player);
            }
            else
            {
                agent.isStopped = false;
                agent.SetDestination(target);
            }
        }

        private bool MeleeReach(Vector3 point)
        {
            Vector3 from = transform.position + Vector3.up;
            Vector3 d = point - from;
            if (Mathf.Abs(d.y) > 2f) return false;
            d.y = 0f;
            return d.magnitude <= Cfg.attackRange + enemy.Radius;
        }
    }
}
