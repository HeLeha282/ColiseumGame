using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Общая логика ИИ врага: атака цели (вблизи или снарядом издалека) и отбрасывание.
    /// Враги бьют только портал и игрока — башни неуязвимы.
    /// </summary>
    public abstract class EnemyAIBase : MonoBehaviour
    {
        [SerializeField] private float lungeDistance = 0.35f;
        [SerializeField] private Transform visualRoot;

        protected Enemy enemy;
        protected EnemyConfig Cfg => enemy.Config;
        protected Vector3 pushVelocity;
        private float attackTimer;
        private float lungeTimer;
        private Vector3 visualRest;

        public bool IsRanged => Cfg.attackRange > 4f;

        public virtual void Init(Enemy owner, Route route)
        {
            enemy = owner;
            if (visualRoot != null) visualRest = visualRoot.localPosition;
        }

        public virtual void InheritProgress(EnemyAIBase parent) { }

        public void Push(Vector3 impulse) => pushVelocity += impulse;

        protected float DifficultyDamage => GameManager.Instance != null ? GameManager.Instance.EnemyDamageMultiplier : 1f;

        /// <summary>Бить цель раз в attackInterval. Возвращает true, если удар состоялся.</summary>
        protected bool AttackTick(IDamageable target, Vector3 targetPoint)
        {
            if (enemy.SpeedMultiplier <= 0f) return false;  // заморожен/оглушён
            attackTimer -= Time.deltaTime;
            if (attackTimer > 0f || target == null || !target.IsAlive) return false;
            attackTimer = Cfg.attackInterval;
            float dmg = Cfg.attackDamage * DifficultyDamage;
            if (IsRanged)
            {
                EnemyProjectile.Spawn(enemy.AimPoint + transform.forward * 0.6f, targetPoint, Cfg, dmg, enemy.transform.position);
            }
            else
            {
                target.TakeDamage(new DamageInfo(dmg, DamageSource.Enemy, transform.position, Cfg.element));
                if (target is PlayerController p) p.OnHitByEnemy(Cfg, transform.position);
            }
            lungeTimer = 0.2f;
            return true;
        }

        protected void ResetAttackTimer() => attackTimer = Cfg.attackInterval * 0.5f;

        protected void Face(Vector3 point)
        {
            Vector3 dir = point - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 540f * Time.deltaTime);
        }

        protected virtual void LateUpdate()
        {
            pushVelocity = Vector3.MoveTowards(pushVelocity, Vector3.zero, Time.deltaTime * 12f);
            if (visualRoot == null) return;
            lungeTimer -= Time.deltaTime;
            float k = lungeTimer > 0f ? Mathf.Sin(lungeTimer / 0.2f * Mathf.PI) : 0f;
            visualRoot.localPosition = visualRest + Vector3.forward * (k * lungeDistance);
        }
    }
}
