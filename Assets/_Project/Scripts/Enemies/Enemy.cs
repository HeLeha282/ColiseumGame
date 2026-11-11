using System.Collections.Generic;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Общее для всех врагов: HP, броня, щит, метка игрока, смерть и награда.
    /// Движение и выбор цели — в компонентах EnemyAI (наземный/летающий).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour, IDamageable
    {
        /// <summary>Все живые враги на сцене (для башен, меча, подсчёта волны).</summary>
        public static readonly List<Enemy> All = new List<Enemy>();

        [SerializeField] private EnemyConfig config;
        [SerializeField] private WorldHealthBar healthBar;
        [SerializeField] private GameObject armorVisual;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private float hitFlashTime = 0.1f;

        public EnemyConfig Config => config;
        public Health Health { get; private set; }
        public bool IsAlive => Health != null && Health.IsAlive;
        public bool IsFlying => config.isFlying;
        public float Radius => config.bodyRadius * transform.lossyScale.x;
        public Vector3 AimPoint => transform.position + Vector3.up * (config.aimHeight * transform.lossyScale.y);
        public bool IsMarked => markTimer > 0f;
        public float Armor { get; private set; }
        /// <summary>Насколько враг продвинулся к порталу. Башни бьют самого продвинутого.</summary>
        public float Progress { get; set; }
        /// <summary>Текущая скорость (для упреждения у баллисты).</summary>
        public Vector3 Velocity { get; set; }

        private float maxArmor;
        private float markTimer;
        private float flashTimer;
        private EnemyAIBase ai;
        private MaterialPropertyBlock block;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            Health = GetComponent<Health>();
            ai = GetComponent<EnemyAIBase>();
            Health.Changed += RefreshBar;
            Health.Died += Die;
            block = new MaterialPropertyBlock();
        }

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        /// <summary>Вызывается спавнером сразу после создания.</summary>
        public void Init(Route route, float hpMultiplier)
        {
            Health.Init(config.maxHP * hpMultiplier);
            // Броня не растёт с волной: её снимает только игрок, и на поздних волнах она превращалась в стену.
            maxArmor = config.armor;
            Armor = maxArmor;
            if (armorVisual != null) armorVisual.SetActive(Armor > 0f);
            RefreshBar();
            if (ai != null) ai.Init(this, route);
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive) return;
            float amount = info.Amount;
            var balance = GameManager.Instance != null ? GameManager.Instance.Balance : null;

            if (info.Source == DamageSource.Player)
            {
                // Удар игрока ставит метку и в первую очередь снимает броню.
                if (balance != null) markTimer = balance.markDuration;
                if (Armor > 0f)
                {
                    float absorbed = Mathf.Min(Armor, amount);
                    Armor -= absorbed;
                    amount -= absorbed;
                    if (Armor <= 0f && armorVisual != null)
                    {
                        armorVisual.SetActive(false);
                        GameManager.Instance?.ShowMessage("Броня босса сбита!");
                    }
                }
            }
            else if (info.Source == DamageSource.Tower)
            {
                if (config.hasShield && IsInFront(info.Origin)) amount = 0f; // щит держит
                if (Armor > 0f) amount *= config.armorTowerDamageMultiplier;
                if (IsMarked && balance != null) amount *= balance.markTowerDamageMultiplier;
            }

            flashTimer = hitFlashTime;
            if (amount > 0f) Health.Damage(amount);
            RefreshBar();
        }

        /// <summary>Находится ли точка в секторе, который прикрывает щит.</summary>
        public bool IsInFront(Vector3 point)
        {
            Vector3 to = point - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) return true;
            return Vector3.Angle(transform.forward, to) <= config.shieldHalfAngle;
        }

        private void Update()
        {
            if (markTimer > 0f) markTimer -= Time.deltaTime;
            if (healthBar != null) healthBar.SetMarked(IsMarked);

            // Короткая белая вспышка при попадании.
            if (bodyRenderer != null)
            {
                flashTimer -= Time.deltaTime;
                bodyRenderer.GetPropertyBlock(block);
                if (flashTimer > 0f)
                {
                    block.SetColor(BaseColorId, Color.white);
                    block.SetColor(EmissionId, Color.white * 0.6f);
                }
                else
                {
                    block.Clear();
                }
                bodyRenderer.SetPropertyBlock(block);
            }
        }

        private void RefreshBar()
        {
            if (healthBar == null) return;
            healthBar.SetHealth(Health.Fraction);
            healthBar.SetArmor(maxArmor > 0f ? Armor / maxArmor : 0f);
        }

        private void Die()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.RegisterKill();
                Vector3 dropPos = transform.position + Vector3.up * 0.8f;
                // Летун умирает в воздухе — сфера падает на землю под ним.
                if (IsFlying && Physics.Raycast(transform.position, Vector3.down, out var hit, 100f, ~0, QueryTriggerInteraction.Ignore))
                    dropPos = hit.point + Vector3.up * 0.8f;
                gm.SpawnOrb(dropPos, config.essenceReward);
            }
            Destroy(gameObject);
        }
    }
}
