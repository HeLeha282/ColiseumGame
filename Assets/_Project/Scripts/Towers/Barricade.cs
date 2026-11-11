using System.Collections.Generic;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Разрушаемая баррикада на маршруте. Маршрут не меняет: враг упирается в неё
    /// и ломает, пока его бьют башни и игрок.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Barricade : MonoBehaviour, IDamageable
    {
        public static readonly List<Barricade> All = new List<Barricade>();

        [SerializeField] private Collider blockCollider;
        [SerializeField] private WorldHealthBar healthBar;

        public Health Health { get; private set; }
        public bool IsAlive => Health.IsAlive;

        private BarricadeSpot spot;

        private void Awake()
        {
            Health = GetComponent<Health>();
            Health.Changed += () => { if (healthBar != null) healthBar.SetHealth(Health.Fraction); };
            Health.Died += OnBroken;
        }

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        public void Init(BarricadeSpot ownerSpot, float maxHP)
        {
            spot = ownerSpot;
            Health.Init(maxHP);
        }

        public void TakeDamage(DamageInfo info) => Health.Damage(info.Amount);

        /// <summary>Баррикада, в которую упирается точка (враг), или null.</summary>
        public static Barricade FindBlocking(Vector3 point, float distance)
        {
            foreach (var b in All)
            {
                if (b == null || !b.IsAlive) continue;
                if (Vector3.Distance(b.blockCollider.ClosestPoint(point), point) <= distance) return b;
            }
            return null;
        }

        private void OnBroken()
        {
            GameManager.Instance?.ShowMessage("Баррикада сломана!");
            if (spot != null) spot.OnBarricadeBroken();
            Destroy(gameObject);
        }
    }
}
