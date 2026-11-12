using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Снаряд врага (маги, кентавры, циклоп): летит в точку, бьёт игрока/портал, может взрываться.</summary>
    public class EnemyProjectile : MonoBehaviour
    {
        private EnemyConfig cfg;
        private Vector3 target, origin, start;
        private float damage, flight, t;

        public static void Spawn(Vector3 from, Vector3 to, EnemyConfig config, float dmg, Vector3 shooter)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.name = "EnemyProjectile";
            go.transform.position = from;
            go.transform.localScale = Vector3.one * (config.splashRadius > 0f ? 0.45f : 0.2f);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Vfx.Mat(config.element != Element.None ? Elements.Color(config.element) : new Color(0.9f, 0.8f, 0.6f));
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var p = go.AddComponent<EnemyProjectile>();
            p.cfg = config;
            p.start = from;
            p.target = to;
            p.origin = shooter;
            p.damage = dmg;
            p.flight = Mathf.Max(0.15f, Vector3.Distance(from, to) / Mathf.Max(1f, config.projectileSpeed));
        }

        private void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / flight);
            float arc = Vector3.Distance(start, target) * 0.12f;
            transform.position = Vector3.Lerp(start, target, k) + Vector3.up * (4f * arc * k * (1f - k));
            if (k >= 1f) Impact();
        }

        private void Impact()
        {
            float radius = Mathf.Max(cfg.splashRadius, 1.2f);
            if (cfg.splashRadius > 0f) Vfx.Flash(target, Elements.Color(cfg.element), cfg.splashRadius * 2f, 0.3f);

            var player = PlayerController.Instance;
            if (player != null && player.IsAlive && Vector3.Distance(player.transform.position + Vector3.up, target) <= radius + 0.5f)
            {
                player.TakeDamage(new DamageInfo(damage, DamageSource.Enemy, origin, cfg.element));
                player.OnHitByEnemy(cfg, origin);
            }
            var portal = Portal.Instance;
            if (portal != null && portal.IsAlive && Vector3.Distance(portal.ClosestPoint(target), target) <= radius)
                portal.TakeDamage(new DamageInfo(damage, DamageSource.Enemy, origin, cfg.element));
            foreach (var b in Barricade.All.ToArray())
                if (b != null && b.IsAlive && Vector3.Distance(b.transform.position, target) <= radius + 2f)
                    b.TakeDamage(new DamageInfo(damage, DamageSource.Enemy, origin));
            Destroy(gameObject);
        }
    }
}
