using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Передвижение героя (WASD, бег на Shift, прыжок на пробел), HP, смерть и возрождение,
    /// рывки и прыжки навыков, замедление/отбрасывание от ударов врагов.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        public static PlayerController Instance { get; private set; }

        [SerializeField] private PlayerConfig config;
        [SerializeField] private GameObject bodyRoot;

        public PlayerConfig Config => config;
        public Health Health { get; private set; }
        public bool IsAlive => Health.IsAlive;
        public bool IsGrounded => controller.isGrounded;
        public float RespawnTimeLeft { get; private set; }
        public bool StrafeMode { get; set; }
        /// <summary>Вызывается в момент приземления (Удар с неба), с высотой падения.</summary>
        public event System.Action<float> Landed;

        private CharacterController controller;
        private PlayerCameraController cameraController;
        private float verticalVelocity, slowTimer, slowAmount, dashTime, airStartY;
        private Vector3 spawnPosition, external, dashVelocity;
        private Quaternion spawnRotation;
        private bool wasGrounded = true;

        private void Awake()
        {
            Instance = this;
            controller = GetComponent<CharacterController>();
            cameraController = GetComponent<PlayerCameraController>();
            Health = GetComponent<Health>();
            Health.Init(config.maxHP);
            Health.Died += OnDied;
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        public void RefreshMaxHP()
        {
            float max = config.maxHP + (PlayerProgression.Instance != null ? PlayerProgression.Instance.MaxHPBonus : 0f);
            float add = max - Health.Max;
            Health.SetMaxKeepFraction(max);
            if (add > 0f) Health.Heal(add);
        }

        private void Update()
        {
            if (!IsAlive)
            {
                RespawnTimeLeft -= Time.deltaTime;
                if (RespawnTimeLeft <= 0f) Respawn();
                return;
            }
            float dt = Time.deltaTime;
            if (slowTimer > 0f) slowTimer -= dt;

            var input = InputReader.Instance;
            bool blocked = input == null || UIState.GameplayBlocked;
            Vector2 moveInput = blocked ? Vector2.zero : input.Move;

            bool firstPerson = cameraController != null && cameraController.FirstPerson;
            Vector3 forward = firstPerson ? transform.forward : AimUtil.CameraForwardFlat();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 move = forward * moveInput.y + right * moveInput.x;
            if (move.sqrMagnitude > 1f) move.Normalize();

            float speed = !blocked && input.SprintHeld ? config.sprintSpeed : config.walkSpeed;
            if (PlayerProgression.Instance != null) speed *= PlayerProgression.Instance.MoveSpeedMultiplier;
            if (slowTimer > 0f) speed *= 1f - slowAmount;

            if (!firstPerson)
            {
                Vector3 faceDir = StrafeMode ? AimUtil.CameraForwardFlat() : move;
                if (faceDir.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(faceDir), config.turnSpeed * dt);
            }

            bool grounded = controller.isGrounded;
            if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;
            if (!blocked && input.JumpPressed && grounded)
                verticalVelocity = Mathf.Sqrt(2f * config.jumpHeight * -config.gravity);
            verticalVelocity += config.gravity * dt;

            Vector3 velocity = move * speed + Vector3.up * verticalVelocity + external;
            if (dashTime > 0f)
            {
                dashTime -= dt;
                velocity = dashVelocity + Vector3.up * Mathf.Min(verticalVelocity, 0f);
            }
            external = Vector3.MoveTowards(external, Vector3.zero, dt * 15f);
            controller.Move(velocity * dt);

            grounded = controller.isGrounded;
            if (!grounded && wasGrounded) airStartY = transform.position.y;
            if (!wasGrounded && grounded) Landed?.Invoke(Mathf.Max(0f, airStartY - transform.position.y));
            if (!grounded) airStartY = Mathf.Max(airStartY, transform.position.y);
            wasGrounded = grounded;
        }

        public void FaceDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direction);
        }

        /// <summary>Рывок на distance метров за time секунд.</summary>
        public void Dash(Vector3 direction, float distance, float time)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) direction = transform.forward;
            dashVelocity = direction.normalized * (distance / Mathf.Max(0.05f, time));
            dashTime = time;
        }

        /// <summary>Подбросить героя (Удар с неба).</summary>
        public void Leap(Vector3 horizontal, float upSpeed)
        {
            verticalVelocity = upSpeed;
            external = horizontal;
            airStartY = transform.position.y;
        }

        public void TakeDamage(DamageInfo info) => Health.Damage(info.Amount);

        public void OnHitByEnemy(EnemyConfig cfg, Vector3 from)
        {
            if (cfg.slowPlayerOnHit > 0f)
            {
                slowAmount = cfg.slowPlayerOnHit;
                slowTimer = 2f;
            }
            if (cfg.knockbackPlayer > 0f)
            {
                Vector3 d = transform.position - from;
                d.y = 0f;
                external = d.normalized * cfg.knockbackPlayer;
                verticalVelocity = Mathf.Max(verticalVelocity, 4f);
            }
        }

        private void OnDied()
        {
            RespawnTimeLeft = config.respawnTime;
            controller.enabled = false;
            if (bodyRoot != null) bodyRoot.SetActive(false);
            GameManager.Instance?.ShowMessage("Вы погибли!");
        }

        private void Respawn()
        {
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            verticalVelocity = 0f;
            external = Vector3.zero;
            dashTime = 0f;
            controller.enabled = true;
            if (bodyRoot != null) bodyRoot.SetActive(true);
            Health.Init(config.maxHP + (PlayerProgression.Instance != null ? PlayerProgression.Instance.MaxHPBonus : 0f));
        }
    }
}
