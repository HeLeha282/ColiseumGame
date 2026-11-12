using System.Collections;
using Cinemachine;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Короткая катсцена для добивания боссов: камера на босса, замедление времени, широкий формат и подпись.
    /// В момент кульминации выполняется действие (отрубить голову, прижечь, сорвать крылья...).
    /// </summary>
    public class Cutscene : MonoBehaviour
    {
        public static bool Playing { get; private set; }
        public static string Caption { get; private set; } = "";
        private static Cutscene runner;

        public static void Play(Transform focus, string caption, System.Action climax)
        {
            if (Playing) return;
            if (runner == null) runner = new GameObject("Cutscene").AddComponent<Cutscene>();
            runner.StartCoroutine(runner.Run(focus, caption, climax));
        }

        private IEnumerator Run(Transform focus, string caption, System.Action climax)
        {
            Playing = true;
            Caption = caption;
            var player = PlayerController.Instance;
            Vector3 fromSide = player != null ? (player.transform.position - focus.position) : focus.forward * 6f;
            fromSide.y = 0f;
            if (fromSide.sqrMagnitude < 1f) fromSide = focus.forward * 6f;
            Vector3 side = Vector3.Cross(Vector3.up, fromSide.normalized);

            var camGo = new GameObject("CutsceneCam");
            camGo.transform.position = focus.position + fromSide.normalized * 7f + side * 3f + Vector3.up * 3.5f;
            var vcam = camGo.AddComponent<CinemachineVirtualCamera>();
            var target = new GameObject("CutsceneTarget").transform;
            target.position = focus.position + Vector3.up * 2.5f;
            vcam.LookAt = target;
            vcam.AddCinemachineComponent<CinemachineComposer>();
            vcam.m_Lens.FieldOfView = 45f;
            vcam.Priority = 100;

            float oldScale = Time.timeScale;
            Time.timeScale = 0.35f;
            yield return new WaitForSecondsRealtime(0.7f);
            climax?.Invoke();
            if (focus != null) Vfx.Flash(focus.position + Vector3.up * 2.5f, Color.white, 6f, 0.3f);
            yield return new WaitForSecondsRealtime(1.0f);
            if (Time.timeScale > 0f) Time.timeScale = oldScale > 0f ? oldScale : 1f;
            Destroy(camGo);
            Destroy(target.gameObject);
            Playing = false;
            Caption = "";
        }
    }
}
