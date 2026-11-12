using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Аура героя — визуальный показатель Фокуса. С 50% вокруг героя кружат светящиеся частицы цвета зачарования,
    /// на 100% их вдвое больше, появляется пульсирующее кольцо и искры — ультимейт готов.
    /// </summary>
    [RequireComponent(typeof(PlayerProgression))]
    public class AuraVisual : MonoBehaviour
    {
        [SerializeField] private int maxOrbs = 10;
        [SerializeField] private float radius = 0.9f;

        private PlayerProgression prog;
        private PlayerController player;
        private Transform[] orbs;
        private Transform ring;
        private float sparkTimer;

        private void Start()
        {
            prog = GetComponent<PlayerProgression>();
            player = GetComponent<PlayerController>();
            orbs = new Transform[maxOrbs];
            for (int i = 0; i < maxOrbs; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(go.GetComponent<Collider>());
                go.name = "AuraOrb";
                go.transform.SetParent(transform, false);
                go.transform.localScale = Vector3.one * 0.12f;
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                orbs[i] = go.transform;
            }
            var r = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(r.GetComponent<Collider>());
            r.name = "AuraRing";
            r.transform.SetParent(transform, false);
            r.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring = r.transform;
        }

        private void LateUpdate()
        {
            if (orbs == null) return;
            float f = prog.FocusFraction;
            bool alive = player.IsAlive;
            Color c = prog.Enchant != Element.None ? Elements.Color(prog.Enchant) : Elements.Color(Element.Void);
            int count = !alive || f < 0.5f ? 0 : f >= 1f ? maxOrbs : Mathf.RoundToInt(Mathf.Lerp(2, maxOrbs / 2, (f - 0.5f) * 2f));
            var mat = Vfx.Mat(c, f >= 1f ? 4f : 2.5f);
            for (int i = 0; i < orbs.Length; i++)
            {
                bool on = i < count;
                if (orbs[i].gameObject.activeSelf != on) orbs[i].gameObject.SetActive(on);
                if (!on) continue;
                orbs[i].GetComponent<Renderer>().sharedMaterial = mat;
                float a = Time.time * (1.5f + i * 0.13f) + i * 2.4f;
                float h = 0.4f + Mathf.Repeat(i * 0.37f + Time.time * 0.25f, 1.6f);
                orbs[i].localPosition = new Vector3(Mathf.Cos(a) * radius, h, Mathf.Sin(a) * radius);
            }
            bool full = alive && f >= 1f;
            ring.gameObject.SetActive(full);
            if (full)
            {
                float p = 1f + Mathf.Sin(Time.time * 6f) * 0.15f;
                ring.localScale = new Vector3(2.6f * p, 0.01f, 2.6f * p);
                ring.localPosition = Vector3.up * 0.05f;
                ring.GetComponent<Renderer>().sharedMaterial = Vfx.Mat(c, 2f);
                if ((sparkTimer -= Time.deltaTime) <= 0f)
                {
                    sparkTimer = 0.12f;
                    Vfx.Flash(transform.position + Vector3.up * Random.Range(0.3f, 2f) + Random.insideUnitSphere * 0.8f, c, 0.18f, 0.25f);
                }
            }
        }
    }
}
