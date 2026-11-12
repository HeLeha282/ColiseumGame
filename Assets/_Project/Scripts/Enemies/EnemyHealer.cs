using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Жрец Асклепия: раз в секунду лечит союзников вокруг на долю их максимального HP.</summary>
    [RequireComponent(typeof(Enemy))]
    public class EnemyHealer : MonoBehaviour
    {
        [SerializeField] private float pulseInterval = 1f;

        private Enemy self;
        private float timer;

        private void Awake() => self = GetComponent<Enemy>();

        private void Update()
        {
            if (!self.IsAlive || self.Config.healRadius <= 0f) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = pulseInterval;
            // Священное пламя (жаровня) рядом гасит лечение
            if (Tower.SuppressesHealingAt(transform.position)) return;
            bool healed = false;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.IsAlive || e.Health.Fraction >= 1f) continue;
                if (Vector3.Distance(e.transform.position, transform.position) > self.Config.healRadius) continue;
                e.Heal(e.Health.Max * self.Config.healPercentPerSecond * pulseInterval);
                healed = true;
            }
            if (healed) Vfx.Ring(transform.position, new Color(0.4f, 1f, 0.5f), self.Config.healRadius, 0.5f);
        }
    }
}
