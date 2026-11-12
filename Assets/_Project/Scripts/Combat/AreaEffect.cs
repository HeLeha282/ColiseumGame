using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Зона на земле: ледяная лужа (замедляет), напалм/огненный след (жжёт). Накладывает стихию.</summary>
    public class AreaEffect : MonoBehaviour
    {
        private float radius, life, dps, slow, tick;
        private Element element;
        private Tower tower;
        private DamageSource source;

        public static void Spawn(Vector3 point, float radius, float duration, Element element, float dps, float slow, Tower tower,
            DamageSource source = DamageSource.Tower)
        {
            if (duration <= 0f) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(go.GetComponent<Collider>());
            go.name = "AreaEffect";
            go.transform.position = point + Vector3.up * 0.06f;
            go.transform.localScale = new Vector3(radius * 2f, 0.03f, radius * 2f);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Vfx.Mat(Elements.Color(element) * 0.6f, 1.2f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var a = go.AddComponent<AreaEffect>();
            a.radius = radius;
            a.life = duration;
            a.element = element;
            a.dps = dps;
            a.slow = slow;
            a.tower = tower;
            a.source = source;
        }

        private void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            if ((tick -= Time.deltaTime) > 0f) return;
            tick = 0.5f;
            Vector3 p = transform.position;
            foreach (var e in Enemy.All.ToArray())
            {
                if (e == null || !e.IsAlive || e.IsFlying) continue;
                Vector3 d = e.transform.position - p;
                d.y = 0f;
                if (d.magnitude > radius + e.Radius || Mathf.Abs(e.transform.position.y - p.y) > 2f) continue;
                if (dps > 0f)
                    e.TakeDamage(new DamageInfo(dps * 0.5f, source, p, element) { SourceTower = tower });
                else if (e.IsAlive) e.ApplyElement(element, new DamageInfo(5f, source, p, element));
                if (e.IsAlive && slow > 0f) e.Slow(slow, 1f);
                if (e.IsAlive && element == Element.Fire) e.Burn(Mathf.Max(dps * 0.3f, 3f), 1.5f);
            }
        }
    }
}
