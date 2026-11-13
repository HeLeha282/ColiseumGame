using System.Collections.Generic;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Модификаторы элитных врагов (акт III и бесконечный режим).</summary>
    [System.Flags]
    public enum EliteModifier
    {
        None = 0,
        Armored = 1,
        Fast = 2,
        Regenerating = 4,
        Exploding = 8,
        Warded = 16
    }

    /// <summary>Компонент может менять входящий урон врага (боссы: головы, фазы).</summary>
    public interface IEnemyDamageModifier
    {
        float ModifyIncomingDamage(DamageInfo info, float amount);
    }

    /// <summary>
    /// Общее для всех врагов: HP, броня, щит, стихии и реакции, статусы (горение, замедление, заморозка,
    /// хрупкость, приземление, раскрытие), метка игрока, смерть и награда.
    /// Движение — в EnemyAI (наземный/летающий), особые умения — отдельными компонентами.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour, IDamageable
    {
        public static readonly List<Enemy> All = new List<Enemy>();
        /// <summary>Тип врага впервые появился в этом забеге (для карточки-подсказки).</summary>
        public static event System.Action<EnemyConfig> FirstSeen;
        private static readonly HashSet<EnemyConfig> Seen = new HashSet<EnemyConfig>();

        [SerializeField] private EnemyConfig config;
        [SerializeField] private WorldHealthBar healthBar;
        [SerializeField] private GameObject armorVisual;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Renderer elementIndicator;
        [SerializeField] private float hitFlashTime = 0.1f;

        public EnemyConfig Config => config;
        public Health Health { get; private set; }
        public bool IsAlive => Health != null && Health.IsAlive;
        public bool IsFlying => config.isFlying && GroundedTime <= 0f;
        public bool IsBoss => config.IsBoss;
        public float Radius => config.bodyRadius * transform.lossyScale.x;
        public Vector3 AimPoint => transform.position + Vector3.up * (config.aimHeight * transform.lossyScale.y);
        public float Armor { get; private set; }
        public float Progress { get; set; }
        public Vector3 Velocity { get; set; }
        public EliteModifier Elite { get; private set; }
        public Route Route { get; private set; }
        public float HpMultiplier { get; private set; } = 1f;

        // ---- статусы
        public Element Aura { get; private set; }
        public float GroundedTime { get; private set; }
        public bool IsMarked => markTimer > 0f;
        public bool IsBrittle => brittleTimer > 0f;
        public bool IsBurning => burnTimer > 0f;
        public bool IsFrozen => freezeTimer > 0f;
        public bool IsRevealed => !config.invisible || revealTimer > 0f;
        /// <summary>Множитель скорости движения для ИИ (замедление, заморозка, оглушение).</summary>
        public float SpeedMultiplier => freezeTimer > 0f || stunTimer > 0f ? 0f : (slowTimer > 0f ? 1f - slowAmount : 1f) * (Elite.HasFlag(EliteModifier.Fast) ? 1.4f : 1f);
        /// <summary>Последний удар был огненным или враг горел — гидрёныш не делится.</summary>
        public bool KilledByFire { get; private set; }

        private float maxArmor, markTimer, markMultiplier = 1.5f, flashTimer;
        private float auraTimer, burnTimer, burnDps, slowTimer, slowAmount, freezeTimer, stunTimer, brittleTimer, revealTimer;
        private float wardShield, burnTick, exposeTimer, exposeMult = 1f, purifyTick;
        private EnemyAIBase ai;
        private IEnemyDamageModifier[] modifiers;
        private MaterialPropertyBlock block;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private static GameBalance Balance => GameManager.Instance != null ? GameManager.Instance.Balance : null;

        public static void ResetSeen() => Seen.Clear();

        private void Awake()
        {
            Health = GetComponent<Health>();
            ai = GetComponent<EnemyAIBase>();
            modifiers = GetComponents<IEnemyDamageModifier>();
            Health.Changed += RefreshBar;
            Health.Died += Die;
            block = new MaterialPropertyBlock();
            var list = new List<Renderer>();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (r != elementIndicator && r.GetComponentInParent<WorldHealthBar>() == null) list.Add(r);
            bodyRenderers = list.ToArray();
        }

        private Renderer[] bodyRenderers;
        private bool lastVisible = true;

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        /// <summary>Вызывается спавнером сразу после создания.</summary>
        public void Init(Route route, float hpMultiplier, EliteModifier elite = EliteModifier.None)
        {
            Route = route;
            Elite = elite;
            HpMultiplier = hpMultiplier;
            Health.Init(config.maxHP * hpMultiplier * (elite != EliteModifier.None ? 1.5f : 1f));
            maxArmor = config.armor + (elite.HasFlag(EliteModifier.Armored) ? config.maxHP * hpMultiplier * 0.6f : 0f);
            Armor = maxArmor;
            wardShield = elite.HasFlag(EliteModifier.Warded) ? Health.Max * 0.5f : 0f;
            if (armorVisual != null) armorVisual.SetActive(Armor > 0f);
            if (elite != EliteModifier.None) transform.localScale *= 1.15f;
            RefreshBar();
            if (ai != null) ai.Init(this, route);
            if (Seen.Add(config)) FirstSeen?.Invoke(config);
        }

        // ---------------------------------------------------------------- урон

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive) return;
            var b = Balance;
            float amount = info.Amount;

            if (info.Source == DamageSource.Player)
            {
                // Удар игрока ставит метку, раскрывает невидимку и в первую очередь снимает броню
                markTimer = PlayerProgression.Instance != null ? PlayerProgression.Instance.MarkDuration : (b != null ? b.markDuration : 4f);
                markMultiplier = PlayerProgression.Instance != null ? PlayerProgression.Instance.MarkMultiplier : (b != null ? b.markTowerDamageMultiplier : 1.5f);
                revealTimer = Mathf.Max(revealTimer, 5f);
                if (Armor > 0f && !info.Piercing)
                {
                    float absorbed = Mathf.Min(Armor, amount * (PlayerProgression.Instance != null ? PlayerProgression.Instance.ArmorBreakMultiplier : 1f));
                    Armor -= absorbed;
                    amount = Mathf.Max(0f, amount - absorbed);
                    if (Armor <= 0f && armorVisual != null)
                    {
                        armorVisual.SetActive(false);
                        Vfx.Text(AimPoint + Vector3.up, "БРОНЯ СБИТА", new Color(1f, 0.85f, 0.3f), 1.3f);
                    }
                }
            }
            else if (info.Source == DamageSource.Tower)
            {
                if (config.hasShield && !info.Piercing && !IsBrittle && IsInFront(info.Origin)) amount = 0f;
                if (Armor > 0f && !info.Piercing) amount *= config.armorTowerDamageMultiplier;
                if (IsMarked) amount *= markMultiplier;
            }

            // Стихия врага: своя не действует, противоположная — сильнее
            if (info.Element != Element.None && config.element != Element.None)
            {
                if (info.Element == config.element)
                {
                    amount = 0f;
                    Vfx.Text(AimPoint + Vector3.up * 0.6f, "ИММУНИТЕТ", Elements.Color(info.Element), 0.8f);
                }
                else if (info.Element == Elements.Opposite(config.element) && b != null) amount *= b.elementWeaknessMultiplier;
            }
            if (IsBrittle && b != null) amount *= b.brittleDamageMultiplier;
            if (exposeTimer > 0f) amount *= exposeMult;
            if (wardShield > 0f && !info.Piercing)
            {
                float absorbed = Mathf.Min(wardShield, amount);
                wardShield -= absorbed;
                amount -= absorbed;
            }
            if (modifiers != null)
                foreach (var m in modifiers) amount = m.ModifyIncomingDamage(info, amount);

            flashTimer = hitFlashTime;
            if (info.Element == Element.Fire || IsBurning) KilledByFire = true;
            else if (info.Source != DamageSource.Effect) KilledByFire = false;

            if (amount > 0f)
            {
                Health.Damage(amount);
                if (info.Source == DamageSource.Player) PlayerProgression.Instance?.OnPlayerHit(this, amount);
            }
            RefreshBar();
            if (IsAlive) ApplyElement(info.Element, info);
        }

        /// <summary>Наложить стихию. Если на враге уже другая — срабатывает реакция.</summary>
        public void ApplyElement(Element element, DamageInfo cause)
        {
            if (element == Element.None || !IsAlive || element == config.element) return;
            var b = Balance;
            if (Aura != Element.None && Aura != element)
            {
                var reaction = Elements.Resolve(Aura, element);
                Aura = Element.None;
                auraTimer = 0f;
                TriggerReaction(reaction, cause, b);
                return;
            }
            Aura = element;
            auraTimer = b != null ? b.elementAuraDuration : 5f;
        }

        private void TriggerReaction(Reaction reaction, DamageInfo cause, GameBalance b)
        {
            if (reaction == Reaction.None || b == null) return;
            float mult = Tower.ReactionMultiplierAt(transform.position);
            float baseHit = Mathf.Max(cause.Amount, 8f) * mult;
            Vfx.Text(AimPoint + Vector3.up * 1.1f, Elements.Name(reaction), Color.white, 1.4f);
            if (Tower.SlowsReactionsAt(transform.position)) Slow(0.3f, 2f);
            PlayerProgression.Instance?.OnReaction(cause.Source == DamageSource.Player);
            switch (reaction)
            {
                case Reaction.Shatter:
                    freezeTimer = 0f;
                    Vfx.Flash(AimPoint, new Color(0.9f, 0.95f, 1f), 2.2f);
                    Health.Damage(baseHit * b.shatterMultiplier);
                    break;
                case Reaction.Brittle:
                    brittleTimer = b.brittleDuration;
                    Vfx.Flash(AimPoint, Elements.Color(Element.Ice), 1.5f);
                    break;
                case Reaction.Overload:
                    Vfx.Flash(AimPoint, new Color(1f, 0.5f, 0.9f), b.overloadRadius * 2f);
                    foreach (var e in All.ToArray())
                    {
                        if (e == null || !e.IsAlive) continue;
                        Vector3 d = e.transform.position - transform.position;
                        if (d.magnitude > b.overloadRadius) continue;
                        e.Health.Damage(baseHit * b.overloadMultiplier);
                        e.Knockback(d.sqrMagnitude > 0.01f ? d.normalized : Random.insideUnitSphere, b.overloadKnockback);
                        e.RefreshBar();
                    }
                    break;
                case Reaction.Decay:
                    Vfx.Flash(AimPoint, Elements.Color(Element.Void), 2.5f);
                    Health.Damage(Health.Max * (IsBoss ? b.decayBossPercent : b.decayPercent) * mult);
                    break;
            }
            RefreshBar();
        }

        // ---------------------------------------------------------------- статусы

        public void Burn(float dps, float duration)
        {
            if (config.element == Element.Fire) return;
            burnDps = Mathf.Max(burnDps, dps);
            burnTimer = Mathf.Max(burnTimer, duration);
        }

        public void Slow(float amount, float duration)
        {
            if (config.element == Element.Ice) amount *= 0.3f;
            if (IsBoss) amount *= 0.5f;
            slowAmount = Mathf.Max(slowTimer > 0f ? slowAmount : 0f, Mathf.Clamp01(amount));
            slowTimer = Mathf.Max(slowTimer, duration);
        }

        /// <summary>Заморозка. Боссы не замерзают — вместо этого сильное замедление.</summary>
        public void Freeze(float duration)
        {
            if (config.element == Element.Ice) return;
            if (IsBoss) { Slow(0.6f, duration); return; }
            freezeTimer = Mathf.Max(freezeTimer, duration);
        }

        public void Stun(float duration)
        {
            if (IsBoss) duration *= 0.3f;
            stunTimer = Mathf.Max(stunTimer, duration);
        }

        /// <summary>Сбить летуна на землю: пока таймер идёт, он считается наземным и достаётся мечом.</summary>
        public void Ground(float duration)
        {
            if (!config.isFlying) return;
            GroundedTime = Mathf.Max(GroundedTime, duration);
        }

        public void Reveal(float duration) => revealTimer = Mathf.Max(revealTimer, duration);

        /// <summary>Уязвимость: весь урон по врагу ×mult (Пепел жаровни).</summary>
        public void Expose(float mult, float duration)
        {
            exposeMult = Mathf.Max(exposeTimer > 0f ? exposeMult : 1f, mult);
            exposeTimer = Mathf.Max(exposeTimer, duration);
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            Health.Heal(amount);
        }

        public void Knockback(Vector3 dir, float force)
        {
            if (IsBoss || ai == null) return;
            dir.y = 0f;
            ai.Push(dir.normalized * force);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (markTimer > 0f) markTimer -= dt;
            if (auraTimer > 0f && (auraTimer -= dt) <= 0f) Aura = Element.None;
            if (slowTimer > 0f) slowTimer -= dt;
            if (freezeTimer > 0f) freezeTimer -= dt;
            if (stunTimer > 0f) stunTimer -= dt;
            if (brittleTimer > 0f) brittleTimer -= dt;
            if (revealTimer > 0f) revealTimer -= dt;
            if (GroundedTime > 0f) GroundedTime -= dt;
            if (exposeTimer > 0f) exposeTimer -= dt;
            if ((purifyTick -= dt) <= 0f)
            {
                purifyTick = 0.5f;
                if (Tower.PurifiesAt(transform.position)) { wardShield = 0f; Elite &= ~EliteModifier.Regenerating; }
            }
            if (burnTimer > 0f && IsAlive)
            {
                burnTimer -= dt;
                burnTick -= dt;
                if (burnTick <= 0f)
                {
                    burnTick = 0.5f;
                    KilledByFire = true;
                    Health.Damage(burnDps * 0.5f);
                }
            }
            if (Elite.HasFlag(EliteModifier.Regenerating) && IsAlive) Health.Heal(Health.Max * 0.02f * dt);

            if (healthBar != null)
            {
                healthBar.SetMarked(IsMarked);
                healthBar.gameObject.SetActive(IsRevealed);
            }
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (elementIndicator != null)
            {
                bool show = Aura != Element.None && IsRevealed;
                if (elementIndicator.enabled != show) elementIndicator.enabled = show;
                if (show) elementIndicator.sharedMaterial = Vfx.Mat(Elements.Color(Aura));
            }
            if (bodyRenderer == null) return;
            flashTimer -= Time.deltaTime;
            bodyRenderer.GetPropertyBlock(block);
            block.Clear();
            Color? tint = null;
            if (flashTimer > 0f) tint = Color.white;
            else if (IsFrozen) tint = new Color(0.6f, 0.9f, 1f);
            else if (IsBurning) tint = Color.Lerp(new Color(1f, 0.4f, 0.1f), Color.white, Mathf.PingPong(Time.time * 4f, 0.4f));
            else if (!IsRevealed) tint = new Color(0.15f, 0.12f, 0.2f);
            if (tint.HasValue)
            {
                block.SetColor(BaseColorId, tint.Value);
                block.SetColor(EmissionId, tint.Value * 0.4f);
            }
            bodyRenderer.SetPropertyBlock(block);
            // Невидимку почти не видно, пока её не раскрыли (лёгкое мерцание)
            if (config.invisible)
            {
                bool visible = IsRevealed || Mathf.Repeat(Time.time * 3f, 1f) < 0.12f;
                if (visible != lastVisible)
                {
                    lastVisible = visible;
                    foreach (var r in bodyRenderers) if (r != null) r.enabled = visible;
                }
            }
        }

        /// <summary>Находится ли точка в секторе, который прикрывает щит.</summary>
        public bool IsInFront(Vector3 point)
        {
            Vector3 to = point - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) return true;
            return Vector3.Angle(transform.forward, to) <= config.shieldHalfAngle;
        }

        public void RefreshBar()
        {
            if (healthBar == null) return;
            healthBar.SetHealth(Health.Fraction);
            healthBar.SetArmor(maxArmor > 0f ? Armor / maxArmor : 0f);
        }

        private void Die()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.RegisterKill(this);
                // Эссенция начисляется сразу — подбирать ничего не нужно
                gm.GrantKillReward(AimPoint, Mathf.RoundToInt(config.essenceReward * (IsBoss ? 1f : Mathf.Sqrt(HpMultiplier))));
            }
            PlayerProgression.Instance?.AddGlory(config.gloryReward * (Elite != EliteModifier.None ? 2 : 1));

            float explR = config.deathExplosionRadius, explD = config.deathExplosionDamage;
            if (Elite.HasFlag(EliteModifier.Exploding)) { explR = Mathf.Max(explR, 3f); explD = Mathf.Max(explD, 20f); }
            if (explR > 0f) DeathExplosion(explR, explD);

            if (config.splitInto != null && config.splitCount > 0 && !KilledByFire && gm != null)
            {
                for (int i = 0; i < config.splitCount; i++)
                {
                    Vector3 off = Quaternion.Euler(0f, i * 360f / config.splitCount, 0f) * Vector3.forward * 0.8f;
                    gm.Spawner.SpawnChild(config.splitInto, Route, transform.position + off, HpMultiplier, this);
                }
                Vfx.Text(AimPoint + Vector3.up, "ДЕЛЕНИЕ! (добей огнём)", Elements.Color(Element.Fire), 0.9f);
            }
            Destroy(gameObject);
        }

        private void DeathExplosion(float radius, float damage)
        {
            Vfx.Flash(transform.position + Vector3.up, Elements.Color(Element.Fire), radius * 2f, 0.35f);
            var p = PlayerController.Instance;
            if (p != null && p.IsAlive && Vector3.Distance(p.transform.position, transform.position) <= radius)
                p.TakeDamage(new DamageInfo(damage, DamageSource.Enemy, transform.position, Element.Fire));
            foreach (var bar in Barricade.All.ToArray())
                if (bar != null && Vector3.Distance(bar.transform.position, transform.position) <= radius + 3f)
                    bar.TakeDamage(new DamageInfo(damage * 3f, DamageSource.Enemy, transform.position));
        }

        /// <summary>Тот же враг-образец для дочерних (деление).</summary>
        public void InitAsChild(Route route, float hpMultiplier, Enemy parent)
        {
            Init(route, hpMultiplier);
            if (ai != null) ai.InheritProgress(parent != null ? parent.GetComponent<EnemyAIBase>() : null);
        }
    }
}
