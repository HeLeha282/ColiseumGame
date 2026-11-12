using System.Collections.Generic;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Башня в слоте: ищет цель в радиусе, стреляет снарядами, прокачивается до 3 уровня,
    /// имеет HP (враги могут её ломать, игрок — чинить).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Tower : MonoBehaviour, IDamageable
    {
        public static readonly List<Tower> All = new List<Tower>();

        [SerializeField] private TowerConfig config;
        [SerializeField] private Transform head;
        [SerializeField] private Transform muzzle;
        [SerializeField] private TowerProjectile projectilePrefab;
        [SerializeField] private WorldHealthBar healthBar;
        [SerializeField] private Collider hitCollider;
        [Tooltip("Отметки уровня: включаются по одной на каждый уровень")]
        [SerializeField] private GameObject[] levelMarkers;
        [Tooltip("Модели по уровням: видна только модель текущего уровня")]
        [SerializeField] private GameObject[] levelModels;
        [SerializeField] private float retargetInterval = 0.2f;
        [SerializeField] private bool horizontalAim;

        public TowerConfig Config => config;
        public Health Health { get; private set; }
        public bool IsAlive => Health.IsAlive;
        /// <summary>Уровень с нуля (0 = первый).</summary>
        public int Level { get; private set; }
        public TowerLevelStats Stats => config.levels[Level];
        public bool CanUpgrade => Level < config.levels.Length - 1;
        public int UpgradeCost => CanUpgrade ? config.levels[Level + 1].cost : 0;
        public int TotalInvested { get; private set; }
        public bool IsDamaged => Health.Current < Health.Max;
        public float Height => hitCollider.bounds.max.y - transform.position.y;

        private TowerSlot slot;
        private Enemy target;
        private float fireTimer;
        private float retargetTimer;
        private CatapultVisual catapult;
        private Enemy pendingTarget;

        private void Awake()
        {
            Health = GetComponent<Health>();
            Health.Changed += () => { if (healthBar != null) healthBar.SetHealth(Health.Fraction); };
            Health.Died += OnDestroyed;
        }

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        public void Init(TowerSlot ownerSlot)
        {
            slot = ownerSlot;
            Level = 0;
            TotalInvested = Stats.cost;
            Health.Init(Stats.maxHP);
            RefreshLevelVisuals();
        }

        public void Upgrade()
        {
            if (!CanUpgrade) return;
            Level++;
            TotalInvested += Stats.cost;
            Health.SetMaxKeepFraction(Stats.maxHP);
            RefreshLevelVisuals();
        }

        public Vector3 ClosestPoint(Vector3 point) => hitCollider.ClosestPoint(point);

        public void TakeDamage(DamageInfo info) => Health.Damage(info.Amount);

        public float Repair(float hp) => Health.Heal(hp);

        private void RefreshLevelVisuals()
        {
            if (catapult != null) catapult.ResetPose();
            pendingTarget = null;
            catapult = null;
            if (levelMarkers != null)
            {
                for (int i = 0; i < levelMarkers.Length; i++)
                    if (levelMarkers[i] != null) levelMarkers[i].SetActive(i <= Level);
            }

            if (levelModels != null)
            {
                for (int i = 0; i < levelModels.Length; i++)
                    if (levelModels[i] != null) levelModels[i].SetActive(i == Level);
                if (Level < levelModels.Length && levelModels[Level] != null)
                    catapult = levelModels[Level].GetComponent<CatapultVisual>();
            }
            if (catapult != null) catapult.ResetPose();
        }

        private void Update()
        {
            retargetTimer -= Time.deltaTime;
            if (retargetTimer <= 0f || target == null || !IsValidTarget(target))
            {
                retargetTimer = retargetInterval;
                target = FindTarget();
            }

            if (target != null && head != null && (catapult == null || !catapult.IsCycling))
            {
                Vector3 dir = target.AimPoint - head.position;
                if (horizontalAim) dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                    head.rotation = Quaternion.Slerp(head.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);
            }

            fireTimer -= Time.deltaTime;
            if (catapult != null && catapult.Tick(Time.deltaTime))
            {
                if (IsValidTarget(pendingTarget)) Fire(pendingTarget, catapult.ShotPosition);
                pendingTarget = null;
            }
            if (target != null && fireTimer <= 0f && (catapult == null || !catapult.IsCycling))
            {
                if (catapult != null && head != null)
                {
                    Vector3 direction = target.AimPoint - head.position;
                    direction.y = 0f;
                    if (direction.sqrMagnitude > 0.01f && Vector3.Angle(head.forward, direction) > 8f) return;
                }
                fireTimer = Stats.fireInterval;
                if (catapult != null)
                {
                    pendingTarget = target;
                    catapult.BeginShot(Stats.fireInterval);
                }
                else Fire(target, Muzzle);
            }
        }

        private bool IsValidTarget(Enemy e)
        {
            if (e == null || !e.IsAlive) return false;
            if (e.IsFlying ? !config.targetsAir : !config.targetsGround) return false;
            return Vector3.Distance(Muzzle, e.AimPoint) <= Stats.range;
        }

        private Vector3 Muzzle => catapult != null ? catapult.Muzzle : muzzle != null ? muzzle.position : transform.position + Vector3.up * 3f;

        /// <summary>Цель — тот, кто дальше всех продвинулся к порталу.</summary>
        private Enemy FindTarget()
        {
            Enemy best = null;
            float bestProgress = float.MinValue;
            foreach (var e in Enemy.All)
            {
                if (!IsValidTarget(e)) continue;
                if (e.Progress > bestProgress)
                {
                    bestProgress = e.Progress;
                    best = e;
                }
            }
            return best;
        }

        private void Fire(Enemy e, Vector3 position)
        {
            var p = Instantiate(projectilePrefab, position, Quaternion.identity);
            p.Launch(e, Stats.damage, config.projectileSpeed, Stats.aoeRadius, config.targetsAir, transform.position);
        }

        private void OnDestroyed()
        {
            GameManager.Instance?.ShowMessage($"{config.displayName} разрушена!");
            if (slot != null) slot.OnTowerDestroyed();
            Destroy(gameObject);
        }
    }
}
