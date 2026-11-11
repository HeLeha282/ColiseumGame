using System.Collections;
using UnityEngine;

namespace ColiseumGame
{
    public enum WeaponType
    {
        Sword,
        Bow
    }

    /// <summary>
    /// Оружие игрока. 1 — меч (ЛКМ: удар дугой перед собой), 2 — лук (держать ЛКМ — натяжение, отпустить — выстрел).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private Transform swordVisual;
        [SerializeField] private Transform bowVisual;
        [SerializeField] private Transform arrowSpawn;
        [SerializeField] private Arrow arrowPrefab;
        [SerializeField] private float swingDuration = 0.15f;

        public WeaponType Weapon { get; private set; } = WeaponType.Sword;
        public bool IsCharging { get; private set; }
        public float Charge01 => IsCharging ? Mathf.Clamp01(chargeTime / config.bowChargeTime) : 0f;

        private PlayerController player;
        private float cooldown;
        private float chargeTime;
        private Quaternion swordRestRotation;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            if (swordVisual != null) swordRestRotation = swordVisual.localRotation;
            SetWeapon(WeaponType.Sword);
        }

        private void Update()
        {
            cooldown -= Time.deltaTime;
            var input = InputReader.Instance;
            if (input == null || !player.IsAlive || UIState.GameplayBlocked)
            {
                CancelCharge();
                return;
            }

            if (input.Weapon1Pressed) SetWeapon(WeaponType.Sword);
            if (input.Weapon2Pressed) SetWeapon(WeaponType.Bow);

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

        private void SetWeapon(WeaponType weapon)
        {
            CancelCharge();
            Weapon = weapon;
            if (swordVisual != null) swordVisual.gameObject.SetActive(weapon == WeaponType.Sword);
            if (bowVisual != null) bowVisual.gameObject.SetActive(weapon == WeaponType.Bow);
        }

        private void CancelCharge()
        {
            IsCharging = false;
            if (player != null) player.StrafeMode = false;
        }

        // ---------- Меч ----------

        private void Swing()
        {
            cooldown = config.swordCooldown;
            player.FaceDirection(AimUtil.CameraForwardFlat());
            if (swordVisual != null) StartCoroutine(SwingAnimation());

            Vector3 center = transform.position + Vector3.up;
            Vector3 forward = transform.forward;
            float halfArc = config.swordArc * 0.5f;

            // Копируем список: враг может умереть и удалиться из него прямо во время цикла.
            var enemies = Enemy.All.ToArray();
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                Vector3 to = enemy.AimPoint - center;
                if (Mathf.Abs(to.y) > config.swordVerticalReach) continue; // летунов мечом почти не достать
                Vector3 flat = new Vector3(to.x, 0f, to.z);
                if (flat.magnitude > config.swordRange + enemy.Radius) continue;
                if (flat.sqrMagnitude > 0.01f && Vector3.Angle(forward, flat) > halfArc) continue;
                enemy.TakeDamage(new DamageInfo(config.swordDamage, DamageSource.Player, transform.position));
            }
        }

        private IEnumerator SwingAnimation()
        {
            float t = 0f;
            while (t < swingDuration)
            {
                t += Time.deltaTime;
                float angle = Mathf.Lerp(-70f, 70f, t / swingDuration);
                swordVisual.localRotation = swordRestRotation * Quaternion.Euler(0f, angle, 0f);
                yield return null;
            }
            swordVisual.localRotation = swordRestRotation;
        }

        // ---------- Лук ----------

        private void Fire()
        {
            float charge = Charge01;
            CancelCharge();
            cooldown = config.bowCooldown;

            Vector3 aimPoint = AimUtil.GetAimPoint(transform, 300f);
            Vector3 origin = arrowSpawn != null ? arrowSpawn.position : transform.position + Vector3.up * 1.5f;
            Vector3 dir = (aimPoint - origin).normalized;
            float speed = Mathf.Lerp(config.arrowMinSpeed, config.arrowMaxSpeed, charge);
            float damage = Mathf.Lerp(config.bowMinDamage, config.bowMaxDamage, charge);

            var arrow = Instantiate(arrowPrefab, origin, Quaternion.LookRotation(dir));
            arrow.Launch(dir * speed, damage, config.arrowGravity, transform);
        }
    }
}
