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

        /// <summary>
        /// Точка камеры сбоку-сверху от босса. Пробуем несколько ракурсов и берём первый, где между камерой
        /// и боссом нет стен; если таких нет — придвигаем камеру к ближайшему препятствию.
        /// </summary>
        private static Vector3 CameraPoint(Vector3 look, Vector3 back, Vector3 side)
        {
            Vector3[] offsets =
            {
                back * 7f + side * 3f + Vector3.up * 1f,
                back * 7f - side * 3f + Vector3.up * 1f,
                -back * 7f + side * 3f + Vector3.up * 1f,
                -back * 7f - side * 3f + Vector3.up * 1f,
            };
            Vector3 fallback = look + offsets[0];
            float bestDist = 0f;
            foreach (var o in offsets)
            {
                Vector3 p = look + o;
                if (!Physics.Linecast(look, p, out var hit, ~0, QueryTriggerInteraction.Ignore)
                    || hit.collider.GetComponentInParent<Enemy>() != null)
                    return p;
                if (hit.distance > bestDist)
                {
                    bestDist = hit.distance;
                    fallback = hit.point - o.normalized * 0.6f;
                }
            }
            return fallback;
        }

        // Если сцену перезапустили посреди катсцены — не оставлять игру «заблокированной».
        private void OnDestroy()
        {
            Playing = false;
            Caption = "";
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
            camGo.transform.position = CameraPoint(focus.position + Vector3.up * 2.5f, fromSide.normalized, side);
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
