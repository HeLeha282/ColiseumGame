using Cinemachine;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Переключение камеры 3-е лицо ↔ 1-е лицо (клавиша V).
    /// В 3-м лице мышь крутит орбиту FreeLook, в 1-м — поворачивает персонажа и голову.
    /// </summary>
    public class PlayerCameraController : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private Transform head;
        [SerializeField] private Renderer[] hideInFirstPerson;
        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority = 5;
        [SerializeField] private float maxPitch = 80f;

        public bool FirstPerson { get; private set; }

        private CameraRig rig;
        private FreeLookInputProvider freeLookInput;
        private float pitch;

        private void Start()
        {
            rig = FindObjectOfType<CameraRig>();
            if (rig == null)
            {
                Debug.LogError("В сцене нет CameraRig");
                enabled = false;
                return;
            }

            rig.FreeLook.Follow = transform;
            rig.FreeLook.LookAt = cameraTarget;
            rig.FreeLook.m_XAxis.m_MaxSpeed = config.orbitSensitivityX;
            rig.FreeLook.m_YAxis.m_MaxSpeed = config.orbitSensitivityY;
            freeLookInput = rig.FreeLook.GetComponent<FreeLookInputProvider>();

            // Камера 1-го лица просто сидит в голове персонажа.
            var fp = rig.FirstPersonCamera.transform;
            fp.SetParent(head, false);
            fp.localPosition = Vector3.zero;
            fp.localRotation = Quaternion.identity;

            SetFirstPerson(false);
        }

        private void Update()
        {
            var input = InputReader.Instance;
            if (input == null || rig == null) return;

            if (!UIState.GameplayBlocked && input.SwitchCameraPressed) SetFirstPerson(!FirstPerson);

            if (FirstPerson && !UIState.CursorFree)
            {
                Vector2 look = input.Look * config.firstPersonSensitivity;
                transform.Rotate(0f, look.x, 0f, Space.World);
                pitch = Mathf.Clamp(pitch - look.y, -maxPitch, maxPitch);
                head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        public void SetFirstPerson(bool value)
        {
            FirstPerson = value;
            if (value)
            {
                // Персонаж поворачивается туда, куда смотрела камера 3-го лица.
                Vector3 f = AimUtil.CameraForwardFlat();
                transform.rotation = Quaternion.LookRotation(f);
                pitch = 0f;
                head.localRotation = Quaternion.identity;
            }
            else
            {
                // Орбита встаёт за спину персонажа.
                rig.FreeLook.m_XAxis.Value = transform.eulerAngles.y;
                rig.FreeLook.m_YAxis.Value = 0.5f;
            }

            rig.FirstPersonCamera.Priority = value ? activePriority : inactivePriority;
            rig.FreeLook.Priority = value ? inactivePriority : activePriority;
            if (freeLookInput != null) freeLookInput.InputEnabled = !value;
            foreach (var r in hideInFirstPerson)
                if (r != null) r.enabled = !value;
        }
    }
}
