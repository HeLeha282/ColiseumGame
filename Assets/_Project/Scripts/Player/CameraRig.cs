using Cinemachine;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Связка камер: главная камера с Brain, орбитальная FreeLook (3-е лицо) и камера 1-го лица.</summary>
    public class CameraRig : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private CinemachineFreeLook freeLook;
        [SerializeField] private CinemachineVirtualCamera firstPersonCamera;

        public Camera MainCamera => mainCamera;
        public CinemachineFreeLook FreeLook => freeLook;
        public CinemachineVirtualCamera FirstPersonCamera => firstPersonCamera;
    }
}
