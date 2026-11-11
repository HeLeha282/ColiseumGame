using Cinemachine;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Передаёт движение мыши из нового Input System в CinemachineFreeLook
    /// (Cinemachine сам находит этот компонент на объекте камеры).
    /// </summary>
    public class FreeLookInputProvider : MonoBehaviour, AxisState.IInputAxisProvider
    {
        /// <summary>Отключается в режиме 1-го лица, чтобы орбита не крутилась в фоне.</summary>
        public bool InputEnabled { get; set; } = true;

        public float GetAxisValue(int axis)
        {
            var input = InputReader.Instance;
            if (!InputEnabled || input == null || UIState.CursorFree) return 0f;
            Vector2 look = input.Look;
            return axis switch
            {
                0 => look.x,
                1 => look.y,
                _ => 0f
            };
        }
    }
}
