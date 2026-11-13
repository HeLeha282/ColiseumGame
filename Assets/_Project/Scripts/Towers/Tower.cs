using System.Collections.Generic;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Башня: ставится игроком куда угодно (с лимитом), неуязвима. Прокачка: уровни 1–3 — цифры,
    /// 4 — выбор ветки, 5 — продолжение ветки, 6 — гибрид (перк соседней ветки с силой 50%) или особый финал.
    /// Меню прокачки — F, только в фазе подготовки.
    /// </summary>
    public class Tower : MonoBehaviour, IInteractable
    {
        public static readonly List<Tower> All = new List<Tower>();

        [SerializeField] private TowerConfig config;
        [SerializeField] private Transform head;
        [SerializeField] private Transform muzzle;
        [SerializeField] private TowerProjectile projectilePrefab;
        [SerializeField] private Collider hitCollider;
        [Tooltip("Отметки уровня: включаются по одной на каждый уровень (до 6)")]
        [SerializeField] private GameObject[] levelMarkers;
        [Tooltip("Модели по уровням 1–3 (с 4-го остаётся модель 3-го)")]
        [SerializeField] private GameObject[] levelModels;
        [Tooltip("Цветной кристалл ветки (меняет цвет по выбранной стихии/ветке)")]
        [SerializeField] private Renderer branchGem;
        [SerializeField] private float retargetInterval = 0.2f;
        [SerializeField] private bool horizontalAim;

        public TowerConfig Config => config;
        /// <summary>0..2 — уровни 1–3; 3, 4, 5 — уровни 4, 5, 6.</summary>
        public int Level => level + (perk4 != null ? 1 : 0) + (perk5 != null ? 1 : 0) + (hybrid != null ? 1 : 0);
        public int TotalInvested { get; private set; }
        public bool IsPetrified { get; private set; }
        public float Height => hitCollider != null ? hitCollider.bounds.max.y - transform.position.y : 3f;
        public float Range => range;

        private int level;
        private TowerPerk perk4, perk5, hybrid;
        private readonly Dictionary<TowerSpecial, float> specials = new Dictionary<TowerSpecial, float>();
        private float damage, interval, range, aoe, auraBonus;
        private Element element;

        private Enemy target;
        private float fireTimer, retargetTimer, stormTimer, pillarTimer, gridTimer, rampTime, idleTime, healTick;
        private int shotCounter;
        private bool steamToggle;
        private readonly Dictionary<Enemy, int> hitCounts = new Dictionary<Enemy, int>();
        private CatapultVisual catapult;
        private Enemy pendingTarget;
        private LineRenderer gridLine;

        private static GameManager GM => GameManager.Instance;
        private static bool IsPrep => GM != null && GM.Phase == GamePhase.Preparation;

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        public void Init(int investedCost)
        {
            level = 0;
            TotalInvested = investedCost;
            Recompute();
        }

        // ---------------------------------------------------------------- прокачка

        public bool Has(TowerSpecial s) => specials.ContainsKey(s);
        public float Power(TowerSpecial s) => specials.TryGetValue(s, out var p) ? p : 0f;
        private float Value(TowerSpecial s, float fallback)
        {
            foreach (var p in new[] { perk4, perk5, hybrid }) if (p != null && p.special == s && p.value != 0f) return p.value;
            return fallback;
        }

        private void Recompute()
        {
            var L = config.levels[Mathf.Clamp(level, 0, config.levels.Length - 1)];
            damage = L.damage;
            interval = L.fireInterval;
            range = L.range;
            aoe = L.aoeRadius;
            auraBonus = L.auraBonus;
            element = config.element;
            specials.Clear();
            Apply(perk4, 1f);
            Apply(perk5, 1f);
            Apply(hybrid, hybrid != null && hybrid.tier < 6 ? 0.5f : 1f);
            RefreshVisuals();
        }

        private void Apply(TowerPerk p, float power)
        {
            if (p == null) return;
            damage *= 1f + (p.damageMul - 1f) * power;
            interval *= 1f + (p.intervalMul - 1f) * power;
            range *= 1f + (p.rangeMul - 1f) * power;
            aoe += p.aoeAdd * power;
            if (p.element != Element.None && (element == Element.None || power >= 1f)) element = p.element;
            if (p.special != TowerSpecial.None) specials[p.special] = Mathf.Max(Power(p.special), power);
        }

        private void RefreshVisuals()
        {
            if (catapult != null) catapult.ResetPose();
            pendingTarget = null;
            catapult = null;
            if (levelMarkers != null)
                for (int i = 0; i < levelMarkers.Length; i++)
                    if (levelMarkers[i] != null) levelMarkers[i].SetActive(i < Level + 1);
            if (levelModels != null && levelModels.Length > 0)
            {
                int m = Mathf.Min(level, levelModels.Length - 1);
                for (int i = 0; i < levelModels.Length; i++)
                    if (levelModels[i] != null) levelModels[i].SetActive(i == m);
                if (levelModels[m] != null) catapult = levelModels[m].GetComponent<CatapultVisual>();
            }
            if (catapult != null) catapult.ResetPose();
            if (branchGem != null)
            {
                branchGem.enabled = perk4 != null;
                Color c = element != Element.None ? Elements.Color(element) : (perk4 != null && perk4 == config.perks[0] ? new Color(1f, 0.85f, 0.3f) : new Color(0.4f, 1f, 0.6f));
                branchGem.sharedMaterial = Vfx.Mat(c);
            }
        }

        // ---------------------------------------------------------------- меню (F)

        public string Title => $"{config.displayName} — уровень {Level + 1}";
        public string Prompt => IsPetrified ? "[E] Расколдовать башню" : $"[F] {config.displayName} ур.{Level + 1}";

        public string Description
        {
            get
            {
                string phase = IsPrep ? "" : "\nПрокачивать можно только в фазе подготовки.";
                string kind = config.kind switch
                {
                    TowerKind.Support => $"аура {range:0} м, +{auraBonus * 100f * SupportMultiplier():0}% урона башням",
                    TowerKind.Area => $"урон {damage:0} по области {range:0} м",
                    TowerKind.Chain => $"урон {damage:0}, цепь ×{ChainJumps}",
                    _ => $"урон {damage:0}" + (aoe > 0 ? $" по области {aoe:0.#} м" : "") + $", дальность {range:0} м"
                };
                string perks = "";
                foreach (var p in new[] { perk4, perk5, hybrid }) if (p != null) perks += (perks == "" ? "\n" : " · ") + p.name;
                return kind + (element != Element.None ? $" · {Elements.Name(element)}" : "") + perks + phase;
            }
        }

        public void FillMenu(List<MenuOption> options)
        {
            if (GM == null) return;
            bool prep = IsPrep;
            if (level < config.levels.Length - 1)
            {
                int cost = config.levels[level + 1].cost;
                options.Add(new MenuOption($"Улучшить до ур.{level + 2} — {cost}", prep && GM.CanAfford(cost), () => Buy(cost, () => level++)));
            }
            else if (perk4 == null)
            {
                foreach (var p in config.perks)
                    if (p.tier == 4) AddPerkOption(options, p, prep, () => perk4 = p);
            }
            else if (perk5 == null)
            {
                foreach (var p in config.perks)
                    if (p.tier == 5 && p.parent == perk4.id) AddPerkOption(options, p, prep, () => perk5 = p);
            }
            else if (hybrid == null)
            {
                TowerPerk final6 = null;
                foreach (var p in config.perks) if (p.tier == 6) final6 = p;
                if (final6 != null) AddPerkOption(options, final6, prep, () => hybrid = final6);
                else
                    foreach (var p in config.perks)
                    {
                        if (p == perk4 || p == perk5 || p.tier == 6) continue;
                        bool otherBranch = p.tier == 4 ? p != perk4 : p.parent != perk4.id;
                        if (!otherBranch) continue;
                        var perk = p;
                        options.Add(new MenuOption($"Гибрид: {p.name} (50%) — {config.hybridCost}", prep && GM.CanAfford(config.hybridCost),
                            () => Buy(config.hybridCost, () => hybrid = perk)));
                    }
            }
            int refund = Mathf.RoundToInt(TotalInvested * GM.Balance.sellRefund);
            options.Add(new MenuOption($"Продать (+{refund})", prep, Sell));
        }

        private void AddPerkOption(List<MenuOption> options, TowerPerk p, bool prep, System.Action set)
        {
            options.Add(new MenuOption($"{p.name} — {p.cost}\n<size=70%>{p.description}</size>", prep && GM.CanAfford(p.cost), () => Buy(p.cost, set)));
        }

        private void Buy(int cost, System.Action change)
        {
            if (!IsPrep || !GM.TrySpend(cost)) return;
            change();
            TotalInvested += cost;
            Recompute();
            Vfx.Ring(transform.position, new Color(1f, 0.85f, 0.3f), 3f, 0.5f);
        }

        private void Sell()
        {
            if (!IsPrep) return;
            GM.AddEssence(Mathf.RoundToInt(TotalInvested * GM.Balance.sellRefund));
            Destroy(gameObject);
        }

        // ---------------------------------------------------------------- Медуза

        public void Petrify()
        {
            if (IsPetrified) return;
            IsPetrified = true;
            foreach (var r in GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(Grey());
            Vfx.Text(transform.position + Vector3.up * (Height + 0.5f), "ОКАМЕНЕЛА", Color.gray, 1f);
        }

        public void Unpetrify()
        {
            if (!IsPetrified) return;
            IsPetrified = false;
            foreach (var r in GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(null);
            Vfx.Flash(transform.position + Vector3.up * 2f, new Color(1f, 0.95f, 0.8f), 3f);
        }

        private static MaterialPropertyBlock greyBlock;
        private static MaterialPropertyBlock Grey()
        {
            if (greyBlock == null)
            {
                greyBlock = new MaterialPropertyBlock();
                greyBlock.SetColor("_BaseColor", new Color(0.45f, 0.45f, 0.45f));
                greyBlock.SetColor("_EmissionColor", Color.black);
            }
            return greyBlock;
        }

        // ---------------------------------------------------------------- бой

        private int ChainJumps => config.chainJumps + (Has(TowerSpecial.ExtraChains) ? Mathf.RoundToInt(Value(TowerSpecial.ExtraChains, 3) * Power(TowerSpecial.ExtraChains)) : 0);
        private Vector3 Muzzle => catapult != null ? catapult.Muzzle : muzzle != null ? muzzle.position : transform.position + Vector3.up * 3f;

        private float EffectiveDamage(Enemy e)
        {
            float d = damage * (1f + DamageBonusFor(this));
            if (Has(TowerSpecial.HarpoonDamage) && e != null && e.Config.isFlying && e.GroundedTime > 0f) d *= 1f + 0.5f * Power(TowerSpecial.HarpoonDamage);
            if (Has(TowerSpecial.Crit) && Random.value < 0.25f * Power(TowerSpecial.Crit))
            {
                d *= 2f;
                if (e != null) Vfx.Text(e.AimPoint + Vector3.up * 0.8f, "КРИТ", new Color(1f, 0.9f, 0.3f), 0.8f);
            }
            return d;
        }

        private float EffectiveInterval()
        {
            float i = interval / (1f + HasteFor(this));
            if (Has(TowerSpecial.RampUp)) i *= Mathf.Lerp(1f, 1f - 0.5f * Power(TowerSpecial.RampUp), Mathf.Clamp01(rampTime / 4f));
            return i;
        }

        private void Update()
        {
            if (IsPetrified) return;
            float dt = Time.deltaTime;
            switch (config.kind)
            {
                case TowerKind.Support: UpdateSupport(dt); return;
                case TowerKind.Area: UpdateArea(dt); return;
            }

            retargetTimer -= dt;
            if (retargetTimer <= 0f || target == null || !IsValidTarget(target))
            {
                retargetTimer = retargetInterval;
                target = FindTarget(null);
            }
            if (target != null) { rampTime += dt; idleTime = 0f; }
            else if ((idleTime += dt) > 1.5f) rampTime = 0f;

            if (target != null && head != null && (catapult == null || !catapult.IsCycling))
            {
                Vector3 dir = target.AimPoint - head.position;
                if (horizontalAim || catapult != null) dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                    head.rotation = Quaternion.Slerp(head.rotation, Quaternion.LookRotation(dir), 12f * dt);
            }

            UpdateStorm(dt);
            UpdatePowerGrid(dt);

            fireTimer -= dt;
            if (catapult != null && catapult.Tick(dt))
            {
                if (IsValidTarget(pendingTarget)) FireProjectile(pendingTarget, catapult.ShotPosition);
                pendingTarget = null;
            }
            if (target == null || fireTimer > 0f || (catapult != null && catapult.IsCycling)) return;
            if (catapult != null && head != null)
            {
                Vector3 direction = target.AimPoint - head.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.01f && Vector3.Angle(head.forward, direction) > 8f) return;
            }
            fireTimer = EffectiveInterval();

            if (config.kind == TowerKind.Chain) { FireChain(target); return; }
            if (Has(TowerSpecial.Pierce)) { FirePierce(target); return; }
            if (catapult != null)
            {
                pendingTarget = target;
                catapult.BeginShot(fireTimer);
                return;
            }
            int shots = 1;
            if (Has(TowerSpecial.MultiShot)) shots += Mathf.RoundToInt(2f * Power(TowerSpecial.MultiShot));
            if (Has(TowerSpecial.Volley)) shots += Mathf.RoundToInt(2f * Power(TowerSpecial.Volley));
            var hit = new HashSet<Enemy> { target };
            FireProjectile(target, Muzzle);
            for (int i = 1; i < shots; i++)
            {
                var extra = FindTarget(hit);
                if (extra == null) break;
                hit.Add(extra);
                FireProjectile(extra, Muzzle);
            }
        }

        private bool IsValidTarget(Enemy e)
        {
            if (e == null || !e.IsAlive || !e.IsRevealed) return false;
            if (e.IsFlying ? !config.targetsAir : !config.targetsGround) return false;
            return Vector3.Distance(transform.position + Vector3.up * 2f, e.AimPoint) <= range;
        }

        /// <summary>Цель — тот, кто дальше всех продвинулся к порталу.</summary>
        private Enemy FindTarget(HashSet<Enemy> exclude)
        {
            Enemy best = null;
            float bestProgress = float.MinValue;
            foreach (var e in Enemy.All)
            {
                if (!IsValidTarget(e) || (exclude != null && exclude.Contains(e))) continue;
                float p = e.Progress + (Has(TowerSpecial.Volley) && e.IsFlying ? 10000f : 0f);
                if (p > bestProgress)
                {
                    bestProgress = p;
                    best = e;
                }
            }
            return best;
        }

        // ---------- снаряды

        private void FireProjectile(Enemy e, Vector3 from)
        {
            shotCounter++;
            var p = Instantiate(projectilePrefab, from, Quaternion.identity);
            bool area = aoe > 0f || (Has(TowerSpecial.Shrapnel) && e.IsFlying);
            p.Launch(this, e, config.projectileSpeed, area);
        }

        /// <summary>Попадание снаряда этой башни: урон, стихия и особые эффекты перков.</summary>
        public void ResolveImpact(Vector3 point, Enemy direct)
        {
            Element el = element;
            if (Has(TowerSpecial.SteamVolley)) { steamToggle = !steamToggle; el = steamToggle ? Element.Fire : Element.Ice; }
            float radius = aoe;
            float mult = 1f;
            bool meteor = Has(TowerSpecial.Meteor) && shotCounter % 4 == 0;
            if (meteor)
            {
                mult = 1f + 2f * Power(TowerSpecial.Meteor);
                radius *= 1.5f;
                Vfx.Flash(point, Elements.Color(Element.Fire), radius * 2.4f, 0.45f);
            }
            if (Has(TowerSpecial.Shrapnel) && direct != null && direct.IsFlying) radius = Mathf.Max(radius, 2.5f * Power(TowerSpecial.Shrapnel));

            if (radius > 0f)
            {
                Vfx.Flash(point, el != Element.None ? Elements.Color(el) : new Color(1f, 0.6f, 0.2f), radius * 2f, 0.3f);
                foreach (var e in Enemy.All.ToArray())
                {
                    if (e == null || !e.IsAlive) continue;
                    if (e.IsFlying && !config.targetsAir && !(Has(TowerSpecial.Shrapnel) && direct != null && direct.IsFlying)) continue;
                    if (Vector3.Distance(e.transform.position, point) > radius + e.Radius) continue;
                    HitEnemy(e, EffectiveDamage(e) * mult, el, meteor);
                }
            }
            else if (direct != null && direct.IsAlive) HitEnemy(direct, EffectiveDamage(direct), el, false);

            if (Has(TowerSpecial.FrostZone)) AreaEffect.Spawn(point, Mathf.Max(radius, 2.5f), 4f * Power(TowerSpecial.FrostZone), Element.Ice, 0f, 0.5f, this);
            if (Has(TowerSpecial.Napalm)) AreaEffect.Spawn(point, Mathf.Max(radius, 2.5f), 5f * Power(TowerSpecial.Napalm), Element.Fire, damage * 0.35f, 0f, this);
        }

        private void HitEnemy(Enemy e, float dmg, Element el, bool stun)
        {
            var info = new DamageInfo(dmg, DamageSource.Tower, transform.position, el) { SourceTower = this };
            e.TakeDamage(info);
            if (!e.IsAlive) return;
            if (el == Element.Ice) e.Slow(0.4f, 2f);
            if (el == Element.Fire && (Has(TowerSpecial.FireShots) || Has(TowerSpecial.SteamVolley))) e.Burn(dmg * 0.3f, 3f);
            if (stun) e.Stun(1f);
            if (Has(TowerSpecial.Shackles))
            {
                if (hitCounts.Count > 200) hitCounts.Clear();   // не копить мёртвых врагов
                hitCounts.TryGetValue(e, out int n);
                n++;
                if (n >= 3) { e.Freeze(1.5f * Power(TowerSpecial.Shackles)); n = 0; }
                hitCounts[e] = n;
            }
            if (e.Config.isFlying)
            {
                if (Has(TowerSpecial.Harpoon)) e.Ground(3f * Power(TowerSpecial.Harpoon) * (e.IsBoss && Has(TowerSpecial.HarpoonBoss) ? 2f : 1f));
                if (Has(TowerSpecial.NetFlyers) && shotCounter % 3 == 0) e.Ground(3f * Power(TowerSpecial.NetFlyers));
            }
            if (Has(TowerSpecial.ChainStun)) e.Stun(0.4f * Power(TowerSpecial.ChainStun));
        }

        // ---------- лучник: пробивная стрела

        private void FirePierce(Enemy first)
        {
            Vector3 from = Muzzle;
            Vector3 dir = (first.AimPoint - from).normalized;
            Vector3 to = from + dir * range;
            Vfx.Beam(from, to, new Color(1f, 0.95f, 0.7f), 0.12f, 0.2f);
            foreach (var e in Enemy.All.ToArray())
            {
                if (!IsValidTarget(e)) continue;
                Vector3 p = e.AimPoint - from;
                float along = Vector3.Dot(p, dir);
                if (along < 0f || along > range) continue;
                if ((p - dir * along).magnitude > 0.9f + e.Radius) continue;
                HitEnemy(e, EffectiveDamage(e) * (1f + 0.0f), element, false);
            }
        }

        // ---------- обелиск

        private void FireChain(Enemy first)
        {
            var hit = new HashSet<Enemy>();
            Enemy current = first;
            Vector3 from = Muzzle;
            float dmg = EffectiveDamage(first);
            for (int i = 0; i <= ChainJumps && current != null; i++)
            {
                Vfx.Lightning(from, current.AimPoint, Elements.Color(Element.Lightning));
                hit.Add(current);
                HitEnemy(current, dmg, Element.Lightning, false);
                from = current.AimPoint;
                dmg *= 0.85f;
                Enemy next = null;
                float best = 6f;
                foreach (var e in Enemy.All)
                {
                    if (e == null || !e.IsAlive || !e.IsRevealed || hit.Contains(e)) continue;
                    float d = Vector3.Distance(e.AimPoint, from);
                    if (d < best) { best = d; next = e; }
                }
                current = next;
            }
        }

        private void UpdateStorm(float dt)
        {
            if (!Has(TowerSpecial.Storm)) return;
            float period = Has(TowerSpecial.StormFast) ? 5f : 8f;
            if ((stormTimer -= dt) > 0f) return;
            var t = FindTarget(null);
            if (t == null) return;
            stormTimer = period;
            float radius = Has(TowerSpecial.StormFast) ? 8f : 6f;
            Vfx.Ring(t.transform.position, Elements.Color(Element.Lightning), radius, 0.5f);
            Vfx.Lightning(t.AimPoint + Vector3.up * 12f, t.AimPoint, Color.white, 0.3f);
            foreach (var e in Enemy.All.ToArray())
            {
                if (e == null || !e.IsAlive || Vector3.Distance(e.transform.position, t.transform.position) > radius) continue;
                HitEnemy(e, EffectiveDamage(e) * 3f, Element.Lightning, false);
                if (Has(TowerSpecial.StormGround) && e.IsAlive) e.Ground(3f * Power(TowerSpecial.StormGround));
            }
        }

        private void UpdatePowerGrid(float dt)
        {
            if (!Has(TowerSpecial.PowerGrid))
            {
                if (gridLine != null) gridLine.enabled = false;
                return;
            }
            Tower partner = null;
            float best = 14f;
            foreach (var t in All)
            {
                if (t == this || t.config.kind != TowerKind.Chain || t.IsPetrified) continue;
                float d = Vector3.Distance(t.transform.position, transform.position);
                if (d < best) { best = d; partner = t; }
            }
            if (gridLine == null)
            {
                gridLine = new GameObject("PowerGrid").AddComponent<LineRenderer>();
                gridLine.transform.SetParent(transform, false);
                gridLine.widthMultiplier = 0.08f;
                gridLine.sharedMaterial = Vfx.Mat(Elements.Color(Element.Lightning), 3f);
            }
            gridLine.enabled = partner != null;
            if (partner == null) return;
            Vector3 a = transform.position + Vector3.up * 1.2f, b = partner.transform.position + Vector3.up * 1.2f;
            gridLine.SetPosition(0, a + Random.insideUnitSphere * 0.1f);
            gridLine.SetPosition(1, b + Random.insideUnitSphere * 0.1f);
            if ((gridTimer -= dt) > 0f) return;
            gridTimer = 0.5f;
            Vector3 ab = b - a;
            foreach (var e in Enemy.All.ToArray())
            {
                if (e == null || !e.IsAlive || e.IsFlying) continue;
                Vector3 p = e.transform.position + Vector3.up - a;
                float t = Mathf.Clamp01(Vector3.Dot(p, ab) / ab.sqrMagnitude);
                if ((p - ab * t).magnitude > 1.2f) continue;
                HitEnemy(e, damage * 0.6f * Power(TowerSpecial.PowerGrid), Element.Lightning, false);
            }
        }

        // ---------- жаровня

        private void UpdateArea(float dt)
        {
            float r = range;
            if (Has(TowerSpecial.HolyFlame))
            {
                foreach (var e in Enemy.All)
                    if (e != null && Vector3.Distance(e.transform.position, transform.position) <= r + 4f) e.Reveal(1f);
                var pl = PlayerController.Instance;
                if (pl != null && pl.IsAlive && Vector3.Distance(pl.transform.position, transform.position) <= r && (healTick -= dt) <= 0f)
                {
                    healTick = 1f;
                    pl.Health.Heal(3f * Power(TowerSpecial.HolyFlame));
                }
            }
            if (Has(TowerSpecial.FirePillar) && (pillarTimer -= dt) <= 0f)
            {
                var t = FindTargetInRadius(10f);
                if (t != null)
                {
                    pillarTimer = 3f;
                    Vfx.Beam(t.transform.position, t.transform.position + Vector3.up * 6f, Elements.Color(Element.Fire), 0.8f, 0.4f);
                    HitEnemy(t, EffectiveDamage(t) * 2f * Power(TowerSpecial.FirePillar), Element.Fire, false);
                    if (t.IsAlive) t.Burn(damage, 3f);
                }
            }
            if ((fireTimer -= dt) > 0f) return;
            fireTimer = EffectiveInterval();
            bool any = false;
            foreach (var e in Enemy.All.ToArray())
            {
                if (e == null || !e.IsAlive || e.IsFlying) continue;
                if (Vector3.Distance(e.transform.position, transform.position) > r) continue;
                any = true;
                HitEnemy(e, EffectiveDamage(e), Element.Fire, false);
                if (!e.IsAlive) continue;
                e.Burn(damage * 0.5f, 2f);
                if (Has(TowerSpecial.Ash)) e.Expose(1f + 0.25f * Power(TowerSpecial.Ash), 3f);
            }
            if (any) Vfx.Ring(transform.position, Has(TowerSpecial.HolyFlame) ? new Color(1f, 0.9f, 0.5f) : Elements.Color(Element.Fire), r, 0.35f);
        }

        private Enemy FindTargetInRadius(float radius)
        {
            Enemy best = null;
            float bestP = float.MinValue;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.IsAlive || !e.IsRevealed || e.IsFlying) continue;
                if (Vector3.Distance(e.transform.position, transform.position) > radius) continue;
                if (e.Progress > bestP) { bestP = e.Progress; best = e; }
            }
            return best;
        }

        // ---------- святилище

        private void UpdateSupport(float dt)
        {
            if ((fireTimer -= dt) > 0f) return;
            fireTimer = 2.5f;
            Vfx.Ring(transform.position, Has(TowerSpecial.Resonance) ? new Color(0.8f, 0.5f, 1f) : new Color(1f, 0.85f, 0.4f), range, 0.8f);
        }

        private float SupportMultiplier() => 1f + (Has(TowerSpecial.Zeal) ? 0.5f * Power(TowerSpecial.Zeal) : 0f);

        private bool Covers(Vector3 pos) => !IsPetrified && Vector3.Distance(pos, transform.position) <= range;

        /// <summary>Суммарный бонус к урону от святилищ (и Маяка жаровни) рядом с башней.</summary>
        public static float DamageBonusFor(Tower t)
        {
            float bonus = 0f;
            foreach (var s in All)
            {
                if (s == t) continue;
                if (s.config.kind == TowerKind.Support && s.Covers(t.transform.position)) bonus += s.auraBonus * s.SupportMultiplier();
                if (s.Has(TowerSpecial.Beacon) && Vector3.Distance(s.transform.position, t.transform.position) <= 8f) bonus += 0.15f * s.Power(TowerSpecial.Beacon);
            }
            return bonus;
        }

        public static float HasteFor(Tower t)
        {
            float haste = 0f;
            foreach (var s in All)
                if (s != t && s.Has(TowerSpecial.Haste) && s.Covers(t.transform.position)) haste += 0.25f * s.Power(TowerSpecial.Haste) * s.SupportMultiplier();
            return haste;
        }

        /// <summary>Множитель урона реакций в точке (Стихийный резонанс святилища).</summary>
        public static float ReactionMultiplierAt(Vector3 pos)
        {
            float m = 1f;
            foreach (var s in All)
                if (s.Has(TowerSpecial.Resonance) && s.Covers(pos)) m = Mathf.Max(m, 1f + 0.5f * s.Power(TowerSpecial.Resonance) * s.SupportMultiplier());
            return m;
        }

        public static bool SlowsReactionsAt(Vector3 pos)
        {
            foreach (var s in All) if (s.Has(TowerSpecial.ResonanceSlow) && s.Covers(pos)) return true;
            return false;
        }

        public static bool SuppressesHealingAt(Vector3 pos)
        {
            foreach (var s in All)
                if (s.Has(TowerSpecial.HolyFlame) && Vector3.Distance(pos, s.transform.position) <= s.range + (s.Has(TowerSpecial.Purify) ? 6f : 0f)) return true;
            return false;
        }

        public static bool PurifiesAt(Vector3 pos)
        {
            foreach (var s in All) if (s.Has(TowerSpecial.Purify) && Vector3.Distance(pos, s.transform.position) <= s.range + 6f) return true;
            return false;
        }

        public static float FocusMultiplierAt(Vector3 pos)
        {
            foreach (var s in All) if (s.Has(TowerSpecial.FocusWell) && s.Covers(pos)) return 1f + s.Power(TowerSpecial.FocusWell);
            return 1f;
        }

        public static int EssenceBonusAt(Vector3 pos)
        {
            int bonus = 0;
            foreach (var s in All) if (s.Has(TowerSpecial.EssenceFlow) && s.Covers(pos)) bonus += Mathf.RoundToInt(2f * s.Power(TowerSpecial.EssenceFlow));
            return bonus;
        }
    }
}
