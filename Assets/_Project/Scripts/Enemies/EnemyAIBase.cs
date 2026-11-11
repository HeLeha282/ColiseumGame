using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Общая логика ИИ врага: атака цели по таймеру и выбор башни для нападения.</summary>
    public abstract class EnemyAIBase : MonoBehaviour
    {
        [SerializeField] private float lungeDistance = 0.35f;
        [SerializeField] private Transform visualRoot;

        protected Enemy enemy;
        protected EnemyConfig Cfg => enemy.Config;
        private float attackTimer;
        private float lungeTimer;
        private Vector3 visualRest;

        public virtual void Init(Enemy owner, Route route)
        {
            enemy = owner;
            if (visualRoot != null) visualRest = visualRoot.localPosition;
        }

        /// <summary>Бить цель раз в attackInterval. Возвращает true, если удар состоялся.</summary>
        protected bool AttackTick(IDamageable target)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer > 0f || target == null || !target.IsAlive) return false;
            attackTimer = Cfg.attackInterval;
            target.TakeDamage(new DamageInfo(Cfg.attackDamage, DamageSource.Enemy, transform.position));
            lungeTimer = 0.2f;
            return true;
        }

        /// <summary>Сбросить таймер, чтобы первый удар по новой цели был не мгновенным.</summary>
        protected void ResetAttackTimer() => attackTimer = Cfg.attackInterval * 0.5f;

        /// <summary>Случайно выбрать ближайшую башню в радиусе агрессии (или null).</summary>
        protected Tower RollTowerAggro()
        {
            if (Random.value > Cfg.towerAggroChance) return null;
            Tower best = null;
            float bestDist = Cfg.towerAggroRange;
            foreach (var tower in Tower.All)
            {
                if (tower == null || !tower.IsAlive) continue;
                float d = Vector3.Distance(transform.position, tower.transform.position);
                if (!enemy.IsFlying && Mathf.Abs(tower.transform.position.y - transform.position.y) > 1.5f) continue;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = tower;
                }
            }
            return best;
        }

        protected void Face(Vector3 point)
        {
            Vector3 dir = point - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 540f * Time.deltaTime);
        }

        protected virtual void LateUpdate()
        {
            // Простой «выпад» вперёд при ударе вместо анимации.
            if (visualRoot == null) return;
            lungeTimer -= Time.deltaTime;
            float k = lungeTimer > 0f ? Mathf.Sin(lungeTimer / 0.2f * Mathf.PI) : 0f;
            visualRoot.localPosition = visualRest + Vector3.forward * (k * lungeDistance);
        }
    }
}
