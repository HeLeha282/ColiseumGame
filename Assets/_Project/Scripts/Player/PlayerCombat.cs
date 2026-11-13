using System.Collections;
using UnityEngine;

namespace ColiseumGame
{
    public enum WeaponType
    {
        Sword,
        Bow,
        Hammer
    }

    /// <summary>
    /// Оружие героя. 1 — меч (ЛКМ, серия из 3 ударов), 2 — лук (держать ЛКМ — натяжение), 3 — молоток (стройка башен).
    /// Удары накладывают стихию зачарования и метку. Уровни оружия дают новые механики (рывок, волна клинка,
    /// пробивание, веер стрел).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private Transform swordVisual;
        [SerializeField] private Transform bowVisual;
        [SerializeField] private Transform hammerVisual;
        [SerializeField] private Transform arrowSpawn;
        [SerializeField] private Arrow arrowPrefab;
        [SerializeField] private float swingDuration = 0.15f;
        [SerializeField] private float comboWindow = 1.1f;

        public WeaponType Weapon { get; private set; } = WeaponType.Sword;
        public bool IsCharging { get; private set; }
        public float Charge01 => IsCharging ? Mathf.Clamp01(chargeTime / ChargeTime) : 0f;
        public float SwordDamage => config.swordDamage * Prog(p => p.SwordMultiplier) * BerserkMultiplier;
        public float BowMaxDamage => config.bowMaxDamage * Prog(p => p.BowMultiplier) * BerserkMultiplier;

        private PlayerController player;
        private TowerPlacer placer;
        private float cooldown, chargeTime, comboTimer;
        private int combo;
        private Quaternion swordRestRotation, hammerRestRotation;

        private float ChargeTime => config.bowChargeTime * Prog(p => p.BowChargeMultiplier);
        private float BerserkMultiplier =>
            PlayerProgression.Instance != null && PlayerProgression.Instance.HasBerserk ? 1f + 0.6f * (1f - player.Health.Fraction) : 1f;
        private static float Prog(System.Func<PlayerProgression, float> f) => PlayerProgression.Instance != null ? f(PlayerProgression.Instance) : 1f;
        private static Element Enchant => PlayerProgression.Instance != null ? PlayerProgression.Instance.Enchant : Element.None;
        private static int SwordLevel => PlayerProgression.Instance != null ? PlayerProgression.Instance.SwordLevel : 1;
        private static int BowLevel => PlayerProgression.Instance != null ? PlayerProgression.Instance.BowLevel : 1;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            placer = GetComponent<TowerPlacer>();
            if (swordVisual != null) swordRestRotation = swordVisual.localRotation;
            if (hammerVisual != null) hammerRestRotation = hammerVisual.localRotation;
            SetWeapon(WeaponType.Sword);
        }

        private void Update()
        {
            cooldown -= Time.deltaTime;
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f) combo = 0;
            var input = InputReader.Instance;
            if (input == null || !player.IsAlive || UIState.GameplayBlocked)
            {
                CancelCharge();
                return;
            }

            if (input.Weapon1Pressed) SetWeapon(WeaponType.Sword);
            if (input.Weapon2Pressed) SetWeapon(WeaponType.Bow);
            // Молоток (стройка) доступен и в подготовке, и прямо в бою
            bool canBuild = GameManager.Instance == null || GameManager.Instance.CanManage;
            if (input.Weapon3Pressed && canBuild)
                SetWeapon(Weapon == WeaponType.Hammer ? WeaponType.Sword : WeaponType.Hammer);
            if (Weapon == WeaponType.Hammer)
            {
                if (input.AltFirePressed || input.CancelPressed || !canBuild)
                    SetWeapon(WeaponType.Sword);
                return;
            }

            if (Weapon == WeaponType.Sword)
            {
                if (input.AttackPressed && cooldown <= 0f) Swing();
            }
            else
            {
                if (input.AttackPressed && cooldown <= 0f)
                {
                    IsCharging = true;
                    chargeTime = 0f;
                    player.StrafeMode = true;
                }
                if (IsCharging)
                {
                    chargeTime += Time.deltaTime;
                    if (input.AttackReleased || !input.AttackHeld) Fire();
                }
            }
        }

        public void SetWeapon(WeaponType weapon)
        {
            CancelCharge();
            Weapon = weapon;
            if (swordVisual != null) swordVisual.gameObject.SetActive(weapon == WeaponType.Sword);
            if (bowVisual != null) bowVisual.gameObject.SetActive(weapon == WeaponType.Bow);
            if (hammerVisual != null) hammerVisual.gameObject.SetActive(weapon == WeaponType.Hammer);
            if (placer != null) placer.SetActive(weapon == WeaponType.Hammer);
        }

        private void CancelCharge()
        {
            IsCharging = false;
            if (player != null) player.StrafeMode = false;
        }

        // ---------- меч

        private void Swing()
        {
            cooldown = config.swordCooldown;
            combo = comboTimer > 0f ? combo + 1 : 1;
            comboTimer = comboWindow;
            player.FaceDirection(AimUtil.CameraForwardFlat());
            if (swordVisual != null) StartCoroutine(SwingAnimation(swordVisual, swordRestRotation));

            float dmg = SwordDamage;
            bool third = combo >= 3;
            if (third)
            {
                combo = 0;
                dmg *= 1.5f;
                if (SwordLevel >= 3) player.Dash(transform.forward, 4f, 0.15f);
            }
            MeleeArc(dmg, config.swordRange, config.swordArc);
            if (SwordLevel >= 5) BladeWave(dmg * 0.7f);
        }

        /// <summary>Урон дугой перед героем (меч, навыки). Возвращает число задетых.</summary>
        public int MeleeArc(float dmg, float reach, float arc, Element element = Element.None)
        {
            Vector3 center = transform.position + Vector3.up;
            Vector3 forward = transform.forward;
            int hits = 0;
            foreach (var enemy in Enemy.All.ToArray())
            {
                if (enemy == null || !enemy.IsAlive) continue;
                Vector3 to = enemy.AimPoint - center;
                if (Mathf.Abs(to.y) > config.swordVerticalReach + (enemy.IsBoss ? 1.5f : 0f)) continue;
                Vector3 flat = new Vector3(to.x, 0f, to.z);
                if (flat.magnitude > reach + enemy.Radius) continue;
                if (arc < 360f && flat.sqrMagnitude > 0.01f && Vector3.Angle(forward, flat) > arc * 0.5f) continue;
                HitEnemy(enemy, dmg, element);
                hits++;
            }
            return hits;
        }

        /// <summary>Удар героя по врагу: стихия зачарования, казнь помеченных.</summary>
        public void HitEnemy(Enemy enemy, float dmg, Element element = Element.None)
        {
            if (element == Element.None) element = Enchant;
            var prog = PlayerProgression.Instance;
            if (prog != null && prog.HasExecute && !enemy.IsBoss && enemy.IsMarked && enemy.Health.Fraction < 0.15f)
            {
                dmg = enemy.Health.Current + enemy.Armor + 1f;
                Vfx.Text(enemy.AimPoint + Vector3.up, "КАЗНЬ", new Color(1f, 0.3f, 0.3f), 1.2f);
            }
            enemy.TakeDamage(new DamageInfo(dmg, DamageSource.Player, transform.position, element));
            if (element != Element.None) Vfx.Flash(enemy.AimPoint, Elements.Color(element), 0.6f, 0.15f);
        }

        private void BladeWave(float dmg)
        {
            Vector3 from = transform.position + Vector3.up;
            Vector3 dir = transform.forward;
            Vfx.Beam(from + dir * 1f, from + dir * 7f, Enchant != Element.None ? Elements.Color(Enchant) : new Color(0.9f, 0.95f, 1f), 0.5f, 0.2f);
            foreach (var e in Enemy.All.ToArray())
            {
                if (e == null || !e.IsAlive) continue;
                Vector3 p = e.AimPoint - from;
                float along = Vector3.Dot(p, dir);
                if (along < 1f || along > 7f || (p - dir * along).magnitude > 1.2f + e.Radius) continue;
                HitEnemy(e, dmg);
            }
        }

        private IEnumerator SwingAnimation(Transform t, Quaternion rest)
        {
            float time = 0f;
            while (time < swingDuration)
            {
                time += Time.deltaTime;
                float angle = Mathf.Lerp(-70f, 70f, time / swingDuration);
                t.localRotation = rest * Quaternion.Euler(0f, angle, 0f);
                yield return null;
            }
            t.localRotation = rest;
        }

        /// <summary>Удар молотком при установке башни.</summary>
        public void PlayHammerSwing()
        {
            if (hammerVisual != null) StartCoroutine(HammerAnimation());
        }

        private IEnumerator HammerAnimation()
        {
            float time = 0f;
            while (time < 0.25f)
            {
                time += Time.deltaTime;
                float k = time / 0.25f;
                float angle = k < 0.4f ? Mathf.Lerp(0f, -50f, k / 0.4f) : Mathf.Lerp(-50f, 60f, (k - 0.4f) / 0.6f);
                hammerVisual.localRotation = hammerRestRotation * Quaternion.Euler(angle, 0f, 0f);
                yield return null;
            }
            hammerVisual.localRotation = hammerRestRotation;
        }

        // ---------- лук

        private void Fire()
        {
            float charge = Charge01;
            CancelCharge();
            cooldown = config.bowCooldown;

            Vector3 aimPoint = AimUtil.GetAimPoint(transform, 300f);
            Vector3 origin = arrowSpawn != null ? arrowSpawn.position : transform.position + Vector3.up * 1.5f;
            Vector3 dir = (aimPoint - origin).normalized;
            float speed = Mathf.Lerp(config.arrowMinSpeed, config.arrowMaxSpeed, charge);
            float damage = Mathf.Lerp(config.bowMinDamage * Prog(p => p.BowMultiplier) * BerserkMultiplier, BowMaxDamage, charge);
            int pierce = BowLevel >= 3 ? 2 : 0;

            Shoot(origin, dir, speed, damage, pierce);
            if (BowLevel >= 5 && charge >= 0.95f)
            {
                Shoot(origin, Quaternion.AngleAxis(-7f, Vector3.up) * dir, speed, damage * 0.7f, pierce);
                Shoot(origin, Quaternion.AngleAxis(7f, Vector3.up) * dir, speed, damage * 0.7f, pierce);
            }
        }

        private void Shoot(Vector3 origin, Vector3 dir, float speed, float damage, int pierce)
        {
            var arrow = Instantiate(arrowPrefab, origin, Quaternion.LookRotation(dir));
            arrow.Launch(dir * speed, damage, config.arrowGravity, transform, Enchant, pierce, this);
        }
    }
}
