using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Передвижение игрока (WASD, бег на Shift, прыжок на пробел), HP, смерть и возрождение.
    /// В 3-м лице движение идёт относительно камеры, персонаж разворачивается по ходу (как в Genshin).
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
        public float RespawnTimeLeft { get; private set; }
        /// <summary>Пока true (натягиваем лук) — персонаж в 3-м лице смотрит туда же, куда камера.</summary>
        public bool StrafeMode { get; set; }

        private CharacterController controller;
        private PlayerCameraController cameraController;
        private float verticalVelocity;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;

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

        private void Update()
        {
            if (!IsAlive)
            {
                RespawnTimeLeft -= Time.deltaTime;
                if (RespawnTimeLeft <= 0f) Respawn();
                return;
            }

            var input = InputReader.Instance;
            bool blocked = input == null || UIState.GameplayBlocked;
            Vector2 moveInput = blocked ? Vector2.zero : input.Move;

            // Оси движения: в 1-м лице — от тела, в 3-м — от камеры.
            bool firstPerson = cameraController != null && cameraController.FirstPerson;
            Vector3 forward = firstPerson ? transform.forward : AimUtil.CameraForwardFlat();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 move = forward * moveInput.y + right * moveInput.x;
            if (move.sqrMagnitude > 1f) move.Normalize();

            float speed = !blocked && input.SprintHeld ? config.sprintSpeed : config.walkSpeed;

            if (!firstPerson)
            {
                Vector3 faceDir = StrafeMode ? AimUtil.CameraForwardFlat() : move;
                if (faceDir.sqrMagnitude > 0.01f)
                {
                    var target = Quaternion.LookRotation(faceDir);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, target, config.turnSpeed * Time.deltaTime);
                }
            }

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            if (!blocked && input.JumpPressed && controller.isGrounded)
                verticalVelocity = Mathf.Sqrt(2f * config.jumpHeight * -config.gravity);
            verticalVelocity += config.gravity * Time.deltaTime;

            controller.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        /// <summary>Мгновенно развернуться (например, при ударе мечом — в сторону камеры).</summary>
        public void FaceDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direction);
        }

        public void TakeDamage(DamageInfo info) => Health.Damage(info.Amount);

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
            controller.enabled = true;
            if (bodyRoot != null) bodyRoot.SetActive(true);
            Health.Init(config.maxHP);
        }
    }
}
