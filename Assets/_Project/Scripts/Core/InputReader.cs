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

        private InputAction move, look, jump, sprint, attack, weapon1, weapon2, weapon3,
            switchCamera, interact, use, ready, freeCursor, cancel, ability1, ability2, ultimate, menu, scroll, altFire;

        public Vector2 Move => move.ReadValue<Vector2>();
        public Vector2 Look => look.ReadValue<Vector2>();
        public bool JumpPressed => jump.WasPressedThisFrame();
        public bool SprintHeld => sprint.IsPressed();
        public bool AttackPressed => attack.WasPressedThisFrame();
        public bool AttackHeld => attack.IsPressed();
        public bool AttackReleased => attack.WasReleasedThisFrame();
        public bool AltFirePressed => altFire.WasPressedThisFrame();
        public bool Weapon1Pressed => weapon1.WasPressedThisFrame();
        public bool Weapon2Pressed => weapon2.WasPressedThisFrame();
        public bool Weapon3Pressed => weapon3.WasPressedThisFrame();
        public bool SwitchCameraPressed => switchCamera.WasPressedThisFrame();
        public bool InteractPressed => interact.WasPressedThisFrame();
        /// <summary>E — действие с боссом (отрубить голову, прижечь...).</summary>
        public bool UsePressed => use.WasPressedThisFrame();
        public bool UseHeld => use.IsPressed();
        public bool ReadyPressed => ready.WasPressedThisFrame();
        public bool FreeCursorHeld => freeCursor.IsPressed();
        public bool CancelPressed => cancel.WasPressedThisFrame();
        public bool Ability1Pressed => ability1.WasPressedThisFrame();
        public bool Ability2Pressed => ability2.WasPressedThisFrame();
        public bool UltimatePressed => ultimate.WasPressedThisFrame();
        public bool UltimateHeld => ultimate.IsPressed();
        public bool UltimateReleased => ultimate.WasReleasedThisFrame();
        public bool MenuPressed => menu.WasPressedThisFrame();
        /// <summary>Прокрутка колеса: +1 вверх, −1 вниз, 0 — нет.</summary>
        public int ScrollStep
        {
            get
            {
                float y = scroll.ReadValue<Vector2>().y;
                return y > 0.01f ? 1 : y < -0.01f ? -1 : 0;
            }
        }

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
            weapon3 = map.FindAction("Weapon3", true);
            switchCamera = map.FindAction("SwitchCamera", true);
            interact = map.FindAction("Interact", true);
            use = map.FindAction("Use", true);
            ready = map.FindAction("Ready", true);
            freeCursor = map.FindAction("FreeCursor", true);
            cancel = map.FindAction("Cancel", true);
            ability1 = map.FindAction("Ability1", true);
            ability2 = map.FindAction("Ability2", true);
            ultimate = map.FindAction("Ultimate", true);
            menu = map.FindAction("Menu", true);
            scroll = map.FindAction("Scroll", true);
            altFire = map.FindAction("AltFire", true);
        }

        private void OnEnable() => actions.Enable();
        private void OnDisable() => actions.Disable();
    }
}
