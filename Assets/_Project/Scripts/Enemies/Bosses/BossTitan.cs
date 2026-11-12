using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Титан (финальный босс), три фазы:
    /// 1) огромная броня — снимает только герой;
    /// 2) после 60% HP меняет стихию (огонь/лёд) каждые 8 с: своя стихия не действует, противоположная бьёт ×2;
    /// 3) на 15% HP падает на колени — E рядом добивает (катсцена). Не успел — Титан встаёт и лечится.
    /// </summary>
    public class BossTitan : BossBase, IEnemyDamageModifier
    {
        [SerializeField] private float elementSwitch = 8f;
        [SerializeField] private float finishWindow = 12f;

        private int phase = 1;
        private Element current = Element.Fire;
        private float switchTimer, windowLeft;
        private bool kneeling;
        private Transform core;

        private void Start()
        {
            core = Part(PrimitiveType.Sphere, new Vector3(0f, 1.3f, 0.5f), Vector3.one * 0.5f, new Color(0.9f, 0.85f, 0.7f));
        }

        public override string Hint => phase switch
        {
            1 => enemy.Armor > 0f
                ? "ФАЗА 1: Титан в броне — башни почти не пробивают. Снимай броню сам!"
                : "ФАЗА 1: броня сбита — бей всем, что есть!",
            2 => $"ФАЗА 2: Титан окутан стихией «{Elements.Name(current)}» — она его не берёт. Бей противоположной: {Elements.Name(Elements.Opposite(current))} (×2)!",
            _ => kneeling ? $"ФАЗА 3: Титан на коленях! E рядом — ДОБИТЬ! ({windowLeft:0} с)" : "ФАЗА 3: последний рывок!"
        };

        public override string InteractPrompt => kneeling ? "[E] Добить Титана" : null;
        public override float InteractRange => 7f;

        public override void Interact()
        {
            if (!kneeling) return;
            kneeling = false;
            PlayCutscene("ПАДЕНИЕ ТИТАНА!", () =>
                enemy.TakeDamage(new DamageInfo(enemy.Health.Max * 10f, DamageSource.Player, transform.position) { Piercing = true }));
        }

        public float ModifyIncomingDamage(DamageInfo info, float amount)
        {
            if (phase != 2 || info.Element == Element.None) return amount;
            if (info.Element == current) return 0f;
            if (info.Element == Elements.Opposite(current)) return amount * 2f;
            return amount;
        }

        private void Update()
        {
            if (!enemy.IsAlive) return;
            float f = enemy.Health.Fraction;
            if (phase == 1 && f <= 0.6f)
            {
                phase = 2;
                Vfx.Text(transform.position + Vector3.up * 5f, "ФАЗА 2", Color.white, 2f);
            }
            if (phase == 2)
            {
                switchTimer -= Time.deltaTime;
                if (switchTimer <= 0f)
                {
                    switchTimer = elementSwitch;
                    current = current == Element.Fire ? Element.Ice : Element.Fire;
                    if (core != null) core.GetComponent<Renderer>().sharedMaterial = Vfx.Mat(Elements.Color(current), 2f);
                    Vfx.Ring(transform.position, Elements.Color(current), 6f, 0.6f);
                }
                if (f <= 0.15f)
                {
                    phase = 3;
                    kneeling = true;
                    windowLeft = finishWindow;
                    Vfx.Text(transform.position + Vector3.up * 5f, "ТИТАН ПАЛ НА КОЛЕНИ — ЖМИ E!", Color.white, 1.8f);
                }
            }
            else if (phase == 3)
            {
                if (kneeling)
                {
                    enemy.Stun(0.5f);
                    windowLeft -= Time.deltaTime;
                    if (windowLeft <= 0f)
                    {
                        kneeling = false;
                        enemy.Heal(enemy.Health.Max * 0.2f);
                        phase = 2;
                        Vfx.Text(transform.position + Vector3.up * 5f, "ТИТАН ПОДНЯЛСЯ", new Color(1f, 0.5f, 0.4f), 1.6f);
                    }
                }
            }
        }
    }
}
