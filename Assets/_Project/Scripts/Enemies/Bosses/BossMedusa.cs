using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Медуза: раз в несколько секунд взглядом превращает башни вокруг в камень (их расколдовывает игрок — E рядом с башней).
    /// Ослабев до 25% HP, падает на колени: подбеги и нажми E — отруби голову (катсцена). Не успел — она восстановится.
    /// </summary>
    public class BossMedusa : BossBase
    {
        [SerializeField] private float gazeInterval = 12f;
        [SerializeField] private float gazeRadius = 18f;
        [SerializeField] private float executeThreshold = 0.25f;
        [SerializeField] private float executeWindow = 15f;

        private float gazeTimer = 6f, windowLeft;
        private bool staggered, healedOnce;
        private Transform snakes;

        private void Start()
        {
            snakes = Part(PrimitiveType.Sphere, new Vector3(0f, 2.2f, 0f), new Vector3(0.9f, 0.5f, 0.9f), new Color(0.3f, 0.6f, 0.25f));
        }

        public override string Hint
        {
            get
            {
                if (staggered) return $"Медуза на коленях! Подбеги и нажми E — отруби ей голову! ({windowLeft:0} с)";
                int stone = 0;
                foreach (var t in Tower.All) if (t.IsPetrified) stone++;
                string s = "Взгляд Медузы превращает башни в камень. Подойди к окаменевшей башне и нажми E, чтобы её расколдовать.";
                if (stone > 0) s += $" Окаменело башен: {stone}.";
                return s + " Сбей ей HP до 25% — и отруби голову (E).";
            }
        }

        public override string InteractPrompt => staggered ? "[E] Отрубить голову Медузе" : null;

        public override void Interact()
        {
            if (!staggered) return;
            staggered = false;
            PlayCutscene("ОТСЕЧЕНИЕ!", () =>
            {
                if (snakes != null) { Vfx.Flash(snakes.position, new Color(0.3f, 0.8f, 0.3f), 3f, 0.4f); Destroy(snakes.gameObject); }
                foreach (var t in Tower.All) t.Unpetrify();
                enemy.TakeDamage(new DamageInfo(enemy.Health.Max * 10f, DamageSource.Player, transform.position) { Piercing = true });
            });
        }

        private void Update()
        {
            if (!enemy.IsAlive) return;
            if (staggered)
            {
                enemy.Stun(0.5f);
                windowLeft -= Time.deltaTime;
                if (windowLeft <= 0f)
                {
                    staggered = false;
                    enemy.Heal(enemy.Health.Max * 0.2f);
                    gazeInterval *= 0.8f;
                    Vfx.Text(transform.position + Vector3.up * 3.5f, "МЕДУЗА ОПРАВИЛАСЬ", new Color(0.5f, 1f, 0.5f), 1.3f);
                }
                return;
            }
            if (enemy.Health.Fraction <= executeThreshold && !healedOnce || enemy.Health.Fraction <= executeThreshold * 0.6f)
            {
                staggered = true;
                healedOnce = true;
                windowLeft = executeWindow;
                Vfx.Text(transform.position + Vector3.up * 3.5f, "МЕДУЗА ОСЛАБЛА — ЖМИ E!", Color.white, 1.5f);
                return;
            }
            gazeTimer -= Time.deltaTime;
            if (gazeTimer > 0f) return;
            gazeTimer = gazeInterval;
            Vfx.Ring(transform.position, new Color(0.6f, 0.9f, 0.4f), gazeRadius, 0.8f);
            foreach (var t in Tower.All.ToArray())
                if (Vector3.Distance(t.transform.position, transform.position) <= gazeRadius) t.Petrify();
        }
    }
}
