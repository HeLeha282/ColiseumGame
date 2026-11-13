using System;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Портал в центре арены. Его защищаем; HP = 0 — поражение.</summary>
    [RequireComponent(typeof(Health))]
    public class Portal : MonoBehaviour, IDamageable
    {
        public static Portal Instance { get; private set; }

        [SerializeField] private GameBalance balance;
        [SerializeField] private Collider hitCollider;
        [SerializeField] private Renderer glowRenderer;
        [SerializeField] private Color healthyColor = new Color(0.2f, 0.9f, 1f);
        [SerializeField] private Color dyingColor = new Color(1f, 0.15f, 0.1f);
        [SerializeField] private float glowIntensity = 2.5f;
        [SerializeField] private float pulseSpeed = 2f;

        public Health Health { get; private set; }
        public bool IsAlive => Health.IsAlive;
        public event Action Destroyed;

        private MaterialPropertyBlock block;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            Instance = this;
            Health = GetComponent<Health>();
            Health.Init(balance.portalMaxHP);
            Health.Died += () => Destroyed?.Invoke();
            block = new MaterialPropertyBlock();
        }

        /// <summary>Ближайшая к точке точка поверхности портала (для атак ближнего боя).</summary>
        public Vector3 ClosestPoint(Vector3 point) => hitCollider.ClosestPoint(point);

        /// <summary>Высота верха портала над его основанием.</summary>
        public float Height => hitCollider.bounds.max.y - transform.position.y;

        public void TakeDamage(DamageInfo info) => Health.Damage(info.Amount);

        private Transform ringA, ringB;

        private void Start()
        {
            ringA = transform.Find("RingA");
            ringB = transform.Find("RingB");
        }

        private void Update()
        {
            // Медленно вращающиеся кольца энергии
            if (ringA != null) ringA.Rotate(Vector3.up, 35f * Time.deltaTime, Space.World);
            if (ringB != null) ringB.Rotate(Vector3.up, -50f * Time.deltaTime, Space.World);
            if (glowRenderer == null) return;
            // Цвет уходит в красный по мере потери HP, плюс лёгкая пульсация.
            Color c = Color.Lerp(dyingColor, healthyColor, Health.Fraction);
            float pulse = 1f + 0.25f * Mathf.Sin(Time.time * pulseSpeed);
            glowRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            block.SetColor(EmissionId, c * glowIntensity * pulse);
            glowRenderer.SetPropertyBlock(block);
        }
    }
}
