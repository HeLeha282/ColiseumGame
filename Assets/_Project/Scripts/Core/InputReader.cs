using UnityEngine;
using UnityEngine.InputSystem;

namespace ColiseumGame
{
    /// <summary>
    /// Единая точка чтения ввода (новый Input System, ассет ColiseumControls.inputactions).
    /// Остальные скрипты берут ввод отсюда, а не из Keyboard/Mouse напрямую.
    /// </summary>
    public class InputReader : MonoBehaviour
    {
        public static InputReader Instance { get; private set; }

        [SerializeField] private InputActionAsset actions;

        private InputAction move, look, jump, sprint, attack, weapon1, weapon2,
            switchCamera, interact, repair, ready, freeCursor, cancel;

        public Vector2 Move => move.ReadValue<Vector2>();
        public Vector2 Look => look.ReadValue<Vector2>();
        public bool JumpPressed => jump.WasPressedThisFrame();
        public bool SprintHeld => sprint.IsPressed();
        public bool AttackPressed => attack.WasPressedThisFrame();
        public bool AttackHeld => attack.IsPressed();
        public bool AttackReleased => attack.WasReleasedThisFrame();
        public bool Weapon1Pressed => weapon1.WasPressedThisFrame();
        public bool Weapon2Pressed => weapon2.WasPressedThisFrame();
        public bool SwitchCameraPressed => switchCamera.WasPressedThisFrame();
        public bool InteractPressed => interact.WasPressedThisFrame();
        public bool RepairHeld => repair.IsPressed();
        public bool ReadyPressed => ready.WasPressedThisFrame();
        public bool FreeCursorHeld => freeCursor.IsPressed();
        public bool CancelPressed => cancel.WasPressedThisFrame();

        private void Awake()
        {
            Instance = this;
            var map = actions.FindActionMap("Player", true);
            move = map.FindAction("Move", true);
            look = map.FindAction("Look", true);
            jump = map.FindAction("Jump", true);
            sprint = map.FindAction("Sprint", true);
            attack = map.FindAction("Attack", true);
            weapon1 = map.FindAction("Weapon1", true);
            weapon2 = map.FindAction("Weapon2", true);
            switchCamera = map.FindAction("SwitchCamera", true);
            interact = map.FindAction("Interact", true);
            repair = map.FindAction("Repair", true);
            ready = map.FindAction("Ready", true);
            freeCursor = map.FindAction("FreeCursor", true);
            cancel = map.FindAction("Cancel", true);
        }

        private void OnEnable() => actions.Enable();
        private void OnDisable() => actions.Disable();
    }
}
