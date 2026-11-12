using System.Collections;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Активные навыки героя (Q, R) и ультимейт (X). Навыки открываются карточками при повышении уровня.
    /// Ультимейт «Разлом Пустоты»: алый (огонь) и синий (лёд) шары в руках схлопываются в фиолетовый луч,
    /// который стирает всё на линии, игнорируя броню и щиты. Варианты: «Алая буря», «Абсолютный ноль».
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(PlayerCombat), typeof(PlayerProgression))]
    public class PlayerAbilities : MonoBehaviour
    {
        [SerializeField] private Transform head;

        public float Cooldown(int slot) => cooldowns[slot];
        public bool IsCastingUltimate { get; private set; }

        private PlayerController player;
        private PlayerCombat combat;
        private PlayerProgression prog;
        private readonly float[] cooldowns = new float[2];
        private bool slamPending;
        private float slamDamage;

        private HeroConfig Hero => prog.Hero;
        private GameManager GM => GameManager.Instance;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            combat = GetComponent<PlayerCombat>();
            prog = GetComponent<PlayerProgression>();
            player.Landed += OnLanded;
        }

        private void Update()
        {
            for (int i = 0; i < 2; i++) if (cooldowns[i] > 0f) cooldowns[i] -= Time.deltaTime;
            var input = InputReader.Instance;
            if (input == null || !player.IsAlive || UIState.GameplayBlocked || IsCastingUltimate) return;
            if (combat.Weapon == WeaponType.Hammer) return;   // Q/R в режиме стройки листают башни
            if (input.Ability1Pressed) TryCast(0);
            if (input.Ability2Pressed) TryCast(1);
            if (input.UltimatePressed)
            {
                if (prog.UltimateReady) StartCoroutine(Ultimate());
                else GM?.ShowMessage($"Фокус {prog.Focus:0}/{Hero.focusMax:0} — бей врагов и вызывай реакции");
            }
        }

        /// <summary>Применить навык из слота (используется и ботом).</summary>
        public bool TryCast(int slot)
        {
            string id = prog.AbilitySlots[slot];
            if (string.IsNullOrEmpty(id) || cooldowns[slot] > 0f) return false;
            var def = Hero.Ability(id);
            if (def == null) return false;
            cooldowns[slot] = def.cooldown;
            Cast(def);
            return true;
        }

        private float Base => combat.SwordDamage;

        private void Cast(AbilityDef a)
        {
            Vector3 pos = transform.position;
            switch (a.id)
            {
                case "reap":   // Жатва: круговой удар с вампиризмом
                {
                    float before = TotalEnemyHp();
                    combat.MeleeArc(Base * a.damageMul, a.radius, 360f);
                    float dealt = Mathf.Max(0f, before - TotalEnemyHp());
                    player.Health.Heal(dealt * 0.3f);
                    Vfx.Ring(pos, new Color(0.8f, 0.05f, 0.05f), a.radius, 0.35f);
                    for (int i = 0; i < 8; i++)
                        Vfx.Flash(pos + Vector3.up + Random.insideUnitSphere * a.radius * 0.7f, new Color(0.6f, 0.02f, 0.02f), 0.35f, 0.4f);
                    break;
                }
                case "skyslam":   // Удар с неба: прыжок вперёд и удар по области при приземлении
                    slamPending = true;
                    slamDamage = Base * a.damageMul;
                    player.Leap(AimUtil.CameraForwardFlat() * 7f, 11f);
                    break;
                case "firedash":   // Огненный рывок с горящим следом
                {
                    Vector3 dir = AimUtil.CameraForwardFlat();
                    player.Dash(dir, a.radius, 0.22f);
                    StartCoroutine(FireTrail(pos, dir, a));
                    break;
                }
                case "frostnova":   // Ледяная нова: заморозка вокруг
                    Vfx.Ring(pos, Elements.Color(Element.Ice), a.radius, 0.5f);
                    Vfx.Flash(pos + Vector3.up, Elements.Color(Element.Ice), a.radius, 0.4f);
                    foreach (var e in Enemy.All.ToArray())
                    {
                        if (e == null || !e.IsAlive || Vector3.Distance(e.transform.position, pos) > a.radius) continue;
                        combat.HitEnemy(e, Base * a.damageMul, Element.Ice);
                        if (e.IsAlive) e.Freeze(a.duration);
                    }
                    break;
                case "chain":   // Цепная молния от врага под прицелом
                {
                    var first = EnemyNearCrosshair(30f);
                    if (first == null) { cooldowns[System.Array.IndexOf(prog.AbilitySlots, a.id)] = 0.5f; GM?.ShowMessage("Наведи прицел на врага"); break; }
                    var hit = new System.Collections.Generic.HashSet<Enemy>();
                    Vector3 from = transform.position + Vector3.up * 1.5f;
                    Enemy cur = first;
                    for (int i = 0; i < 7 && cur != null; i++)
                    {
                        Vfx.Lightning(from, cur.AimPoint, Elements.Color(Element.Lightning), 0.25f);
                        hit.Add(cur);
                        combat.HitEnemy(cur, Base * a.damageMul, Element.Lightning);
                        if (cur.IsAlive && cur.Config.isFlying) cur.Ground(2f);
                        from = cur.AimPoint;
                        Enemy next = null;
                        float best = a.radius;
                        foreach (var e in Enemy.All)
                        {
                            if (e == null || !e.IsAlive || hit.Contains(e)) continue;
                            float d = Vector3.Distance(e.AimPoint, from);
                            if (d < best) { best = d; next = e; }
                        }
                        cur = next;
                    }
                    break;
                }
                case "meteor":   // Метеор в точку прицела
                    StartCoroutine(Meteor(AimUtil.GetAimPoint(transform, 60f), a));
                    break;
            }
        }

        private IEnumerator FireTrail(Vector3 start, Vector3 dir, AbilityDef a)
        {
            var hit = new System.Collections.Generic.HashSet<Enemy>();
            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                Vector3 p = transform.position;
                AreaEffect.Spawn(new Vector3(p.x, p.y, p.z), 1.4f, a.duration, Element.Fire, Base * 0.5f, 0f, null, DamageSource.Player);
                foreach (var e in Enemy.All.ToArray())
                    if (e != null && e.IsAlive && !hit.Contains(e) && Vector3.Distance(e.transform.position, p) < 2f)
                    {
                        hit.Add(e);
                        combat.HitEnemy(e, Base * a.damageMul, Element.Fire);
                    }
                yield return new WaitForSeconds(0.05f);
            }
        }

        private IEnumerator Meteor(Vector3 point, AbilityDef a)
        {
            Vfx.Ring(point, Elements.Color(Element.Fire), a.radius, 0.8f);
            for (float t = 0f; t < 0.8f; t += Time.deltaTime)
            {
                if (Random.value < 0.3f) Vfx.Flash(point + Vector3.up * (12f * (1f - t / 0.8f)), Elements.Color(Element.Fire), 1.2f, 0.15f);
                yield return null;
            }
            Vfx.Flash(point + Vector3.up, Elements.Color(Element.Fire), a.radius * 2.2f, 0.5f);
            foreach (var e in Enemy.All.ToArray())
            {
                if (e == null || !e.IsAlive || Vector3.Distance(e.transform.position, point) > a.radius) continue;
                combat.HitEnemy(e, Base * a.damageMul, Element.Fire);
                if (e.IsAlive) e.Burn(Base * 0.4f, a.duration);
            }
        }

        private void OnLanded(float fall)
        {
            if (!slamPending) return;
            slamPending = false;
            var a = Hero.Ability("skyslam");
            float radius = a != null ? a.radius : 4f;
            float dmg = slamDamage * (fall > 2.5f ? 1.5f : 1f);
            Vfx.Ring(transform.position, new Color(0.9f, 0.75f, 0.5f), radius, 0.4f);
            Vfx.Flash(transform.position, new Color(0.8f, 0.7f, 0.55f), radius, 0.3f);
            foreach (var e in Enemy.All.ToArray())
            {
                if (e == null || !e.IsAlive || e.IsFlying) continue;
                Vector3 d = e.transform.position - transform.position;
                if (d.magnitude > radius) continue;
                combat.HitEnemy(e, dmg);
                if (e.IsAlive) { e.Knockback(d, 6f); e.Stun(0.6f); }
            }
        }

        // ---------------------------------------------------------------- ультимейт

        private IEnumerator Ultimate()
        {
            IsCastingUltimate = true;
            prog.SpendFocus();
            string variant = prog.UltimateId;
            Transform h = head != null ? head : transform;
            // Два шара в руках разгораются и сходятся
            Color c1 = variant == "zero" ? Elements.Color(Element.Ice) : Elements.Color(Element.Fire);
            Color c2 = variant == "storm" ? Elements.Color(Element.Lightning) : variant == "zero" ? Elements.Color(Element.Lightning) : new Color(0.2f, 0.4f, 1f);
            var left = Orb(c1);
            var right = Orb(c2);
            float charge = Hero.ultimateChargeTime;
            for (float t = 0f; t < charge; t += Time.deltaTime)
            {
                float k = t / charge;
                Vector3 fwd = h.forward;
                Vector3 side = Vector3.Cross(Vector3.up, fwd).normalized;
                Vector3 c = h.position + fwd * 0.9f - Vector3.up * 0.2f;
                left.position = c - side * Mathf.Lerp(0.7f, 0f, k * k);
                right.position = c + side * Mathf.Lerp(0.7f, 0f, k * k);
                left.localScale = right.localScale = Vector3.one * Mathf.Lerp(0.15f, 0.45f, k);
                yield return null;
            }
            Destroy(left.gameObject);
            Destroy(right.gameObject);

            int wave = GM != null ? GM.WaveIndex : 0;
            float dmg = Hero.ultimateDamage * (1f + Hero.ultimateDamagePerWave * wave) * prog.ReactionPower;
            switch (variant)
            {
                case "storm": UltimateStorm(dmg); break;
                case "zero": UltimateZero(dmg); break;
                default: UltimateVoid(dmg); break;
            }
            IsCastingUltimate = false;
        }

        private Transform Orb(Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = Vfx.Mat(c, 4f);
            return go.transform;
        }

        private void UltimateVoid(float dmg)
        {
            var cam = Camera.main;
            Vector3 from = (head != null ? head.position : transform.position + Vector3.up * 1.6f);
            Vector3 dir = cam != null ? cam.transform.forward : transform.forward;
            Vector3 to = from + dir * Hero.ultimateLength;
            Color purple = Elements.Color(Element.Void);
            Vfx.Beam(from, to, purple, Hero.ultimateWidth * 2f, 0.6f);
            Vfx.Beam(from, to, Color.white, Hero.ultimateWidth * 0.6f, 0.4f);
            Vfx.Flash(from + dir * 2f, purple, 4f, 0.5f);
            Vfx.Text(from + dir * 4f + Vector3.up, "РАЗЛОМ ПУСТОТЫ", purple, 2f);
            foreach (var e in Enemy.All.ToArray())
            {
                if (e == null || !e.IsAlive) continue;
                Vector3 p = e.AimPoint - from;
                float along = Vector3.Dot(p, dir);
                if (along < 0f || along > Hero.ultimateLength) continue;
                if ((p - dir * along).magnitude > Hero.ultimateWidth + e.Radius) continue;
                e.TakeDamage(new DamageInfo(dmg, DamageSource.Player, transform.position, Element.Void) { Piercing = true });
            }
        }

        private void UltimateStorm(float dmg)
        {
            StartCoroutine(StormRoutine(dmg));
        }

        private IEnumerator StormRoutine(float dmg)
        {
            Vfx.Text(transform.position + Vector3.up * 3f, "АЛАЯ БУРЯ", new Color(1f, 0.3f, 0.4f), 2f);
            for (int pulse = 0; pulse < 6; pulse++)
            {
                Vfx.Ring(transform.position, new Color(1f, 0.3f, 0.5f), 11f, 0.4f);
                foreach (var e in Enemy.All.ToArray())
                {
                    if (e == null || !e.IsAlive || Vector3.Distance(e.transform.position, transform.position) > 11f) continue;
                    Vfx.Lightning(e.AimPoint + Vector3.up * 8f, e.AimPoint, new Color(1f, 0.4f, 0.6f));
                    e.TakeDamage(new DamageInfo(dmg / 4f, DamageSource.Player, transform.position, pulse % 2 == 0 ? Element.Fire : Element.Lightning) { Piercing = true });
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void UltimateZero(float dmg)
        {
            Vfx.Text(transform.position + Vector3.up * 3f, "АБСОЛЮТНЫЙ НОЛЬ", Elements.Color(Element.Ice), 2f);
            Vfx.Ring(transform.position, Elements.Color(Element.Ice), 18f, 0.8f);
            foreach (var e in Enemy.All.ToArray())
            {
                if (e == null || !e.IsAlive || Vector3.Distance(e.transform.position, transform.position) > 18f) continue;
                e.TakeDamage(new DamageInfo(dmg * 0.6f, DamageSource.Player, transform.position, Element.Ice) { Piercing = true });
                if (e.IsAlive) { e.Freeze(4f); e.ApplyElement(Element.Lightning, new DamageInfo(dmg * 0.3f, DamageSource.Player, transform.position, Element.Lightning)); }
            }
        }

        // ---------- помощники

        private static float TotalEnemyHp()
        {
            float s = 0f;
            foreach (var e in Enemy.All) if (e != null) s += e.Health.Current;
            return s;
        }

        private Enemy EnemyNearCrosshair(float maxDist)
        {
            var cam = Camera.main;
            if (cam == null) return null;
            Enemy best = null;
            float bestScore = float.MaxValue;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.IsAlive || !e.IsRevealed) continue;
                Vector3 vp = cam.WorldToViewportPoint(e.AimPoint);
                if (vp.z <= 0f || Vector3.Distance(transform.position, e.transform.position) > maxDist) continue;
                float score = new Vector2(vp.x - 0.5f, vp.y - 0.5f).magnitude;
                if (score < 0.25f && score < bestScore) { bestScore = score; best = e; }
            }
            return best;
        }
    }
}
