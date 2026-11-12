using System.Collections.Generic;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Гидра: пока живы головы, тело неуязвимо. Срубленная голова через несколько секунд отрастает ДВУМЯ,
    /// если обрубок не прижечь: огнём (башни/герой) или подбежать и нажать E (катсцена).
    /// </summary>
    public class BossHydra : BossBase, IEnemyDamageModifier
    {
        private class Head
        {
            public float hp, max;
            public float regrow;     // >0 — обрубок, отрастёт через столько секунд
            public Transform visual;
            public bool Alive => hp > 0f;
            public bool Stump => !Alive && regrow > 0f;
        }

        [SerializeField] private int startHeads = 3;
        [SerializeField] private int maxHeads = 6;
        [SerializeField] private float headHpShare = 0.18f;
        [SerializeField] private float regrowTime = 9f;

        private readonly List<Head> heads = new List<Head>();
        private float headMax;

        private void Start()
        {
            headMax = enemy.Health.Max * headHpShare;
            for (int i = 0; i < startHeads; i++) AddHead();
        }

        private void AddHead()
        {
            var h = new Head { hp = headMax, max = headMax };
            heads.Add(h);
            h.visual = Part(PrimitiveType.Capsule, Vector3.zero, new Vector3(0.35f, 0.6f, 0.35f), new Color(0.25f, 0.55f, 0.3f));
            Layout();
        }

        private void Layout()
        {
            int n = heads.Count;
            for (int i = 0; i < n; i++)
            {
                if (heads[i].visual == null) continue;
                float x = (i - (n - 1) * 0.5f) * 0.45f;
                heads[i].visual.localPosition = new Vector3(x, heads[i].Alive ? 2.6f : 1.9f, 0.4f);
                heads[i].visual.localScale = heads[i].Alive ? new Vector3(0.35f, 0.6f, 0.35f) : new Vector3(0.3f, 0.15f, 0.3f);
                heads[i].visual.GetComponent<Renderer>().sharedMaterial = Vfx.Mat(heads[i].Alive ? new Color(0.25f, 0.55f, 0.3f) : new Color(0.7f, 0.1f, 0.1f), 1.2f);
            }
        }

        private int Count(System.Func<Head, bool> f) { int n = 0; foreach (var h in heads) if (f(h)) n++; return n; }

        public override string Hint
        {
            get
            {
                int alive = Count(h => h.Alive), stumps = Count(h => h.Stump);
                if (alive == 0 && stumps == 0) return "Все шеи прижжены — добивай Гидру!";
                string s = $"Голов: {alive}. Пока они живы, тело неуязвимо — руби головы.";
                if (stumps > 0) s += $" ОБРУБКОВ: {stumps} — прижги огнём или подбеги и нажми E, иначе каждый отрастёт двумя головами!";
                return s;
            }
        }

        public override string InteractPrompt => Count(h => h.Stump) > 0 ? "[E] Прижечь обрубки" : null;

        public override void Interact()
        {
            PlayCutscene("ПРИЖЕЧЬ!", () =>
            {
                foreach (var h in heads)
                    if (h.Stump) Cauterize(h);
                Layout();
            });
        }

        private void Cauterize(Head h)
        {
            h.regrow = 0f;
            if (h.visual != null)
            {
                Vfx.Flash(h.visual.position, Elements.Color(Element.Fire), 1.6f, 0.4f);
                Destroy(h.visual.gameObject);
                h.visual = null;
            }
            Vfx.Text(transform.position + Vector3.up * 3.5f, "ПРИЖЖЕНО", Elements.Color(Element.Fire), 1.3f);
        }

        public float ModifyIncomingDamage(DamageInfo info, float amount)
        {
            // Огонь прижигает один обрубок
            if (info.Element == Element.Fire)
                foreach (var h in heads)
                    if (h.Stump) { Cauterize(h); Layout(); break; }

            Head target = null;
            foreach (var h in heads)
                if (h.Alive && (target == null || h.hp < target.hp)) target = h;
            if (target == null) return amount;   // голов нет — тело уязвимо

            target.hp -= amount;
            if (!target.Alive)
            {
                target.regrow = regrowTime;
                Vfx.Text(transform.position + Vector3.up * 3.5f, "ГОЛОВА СРУБЛЕНА — ПРИЖГИ!", Color.white, 1.2f);
                Layout();
            }
            return 0f;
        }

        private void Update()
        {
            bool changed = false;
            var grow = new List<Head>();
            foreach (var h in heads)
            {
                if (!h.Stump) continue;
                h.regrow -= Time.deltaTime;
                if (h.regrow <= 0f) grow.Add(h);
            }
            foreach (var h in grow)
            {
                h.hp = h.max;
                h.regrow = 0f;
                if (heads.Count < maxHeads) AddHead();
                Vfx.Text(transform.position + Vector3.up * 3.5f, "ОТРОСЛИ ДВЕ ГОЛОВЫ!", new Color(0.4f, 1f, 0.4f), 1.3f);
                changed = true;
            }
            if (changed) Layout();
        }
    }
}
