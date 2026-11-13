using System.Collections.Generic;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Цербер: три головы — огненная, ледяная и грозовая. Каждую можно убить только «её» уроном:
    /// огненную — льдом, ледяную — огнём, грозовую — ударами героя без стихии. Пока живы головы, тело почти неуязвимо.
    /// </summary>
    public class BossCerberus : BossBase, IEnemyDamageModifier
    {
        private class Head
        {
            public Element element;
            public Element weakness;
            public float hp, max;
            public Transform visual;
            public bool Alive => hp > 0f;
        }

        [SerializeField] private float headHpShare = 0.25f;
        [Tooltip("Доля урона, которая проходит в тело, пока живы головы (чтобы бой не был безвыходным)")]
        [SerializeField] private float bodyDamageWhileHeads = 0.2f;

        private readonly List<Head> heads = new List<Head>();

        private void Start()
        {
            float hp = enemy.Health.Max * headHpShare;
            AddHead(Element.Fire, Element.Ice, -0.6f, hp);
            AddHead(Element.Lightning, Element.None, 0f, hp);
            AddHead(Element.Ice, Element.Fire, 0.6f, hp);
        }

        private void AddHead(Element e, Element weakness, float x, float hp)
        {
            var v = Part(PrimitiveType.Sphere, new Vector3(x, 2.2f, 0.55f), Vector3.one * 0.55f, Elements.Color(e));
            heads.Add(new Head { element = e, weakness = weakness, hp = hp, max = hp, visual = v });
        }

        private int AliveHeads { get { int n = 0; foreach (var h in heads) if (h.Alive) n++; return n; } }

        public override string Hint
        {
            get
            {
                if (AliveHeads == 0) return "Все головы Цербера срублены — добивай тело!";
                var parts = new List<string>();
                foreach (var h in heads)
                    if (h.Alive)
                        parts.Add(h.element switch
                        {
                            Element.Fire => $"огненную — ЛЬДОМ ({h.hp / h.max * 100f:0}%)",
                            Element.Ice => $"ледяную — ОГНЁМ ({h.hp / h.max * 100f:0}%)",
                            _ => $"грозовую — МЕЧОМ/СТРЕЛАМИ без стихии ({h.hp / h.max * 100f:0}%)"
                        });
                return "Пока живы головы, тело почти неуязвимо. Бей " + string.Join(", ", parts) +
                       ". Лёд/огонь: зачарование оружия (Tab), Ледяная нова, снаряды катапульты, Жаровня.";
            }
        }

        public float ModifyIncomingDamage(DamageInfo info, float amount)
        {
            if (AliveHeads == 0) return amount;
            Element e = info.Element;
            bool physicalPlayer = info.Source == DamageSource.Player && e == Element.None;
            foreach (var h in heads)
            {
                if (!h.Alive) continue;
                bool fits = h.weakness == Element.None ? physicalPlayer : e == h.weakness;
                if (!fits) continue;
                h.hp -= amount;
                if (h.visual != null) h.visual.localScale = Vector3.one * (0.3f + 0.25f * Mathf.Clamp01(h.hp / h.max));
                if (!h.Alive)
                {
                    Vfx.Flash(h.visual.position, Elements.Color(h.element), 2.5f, 0.4f);
                    Vfx.Text(h.visual.position + Vector3.up, "ГОЛОВА СРУБЛЕНА", Color.white, 1.4f);
                    Destroy(h.visual.gameObject);
                }
                return amount * bodyDamageWhileHeads;
            }
            return amount * bodyDamageWhileHeads;
        }
    }
}
