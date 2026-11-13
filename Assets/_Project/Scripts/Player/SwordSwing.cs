using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Анимация катаны: серия из трёх ударов + шлейф за клинком.
    /// 1 — косой рез справа-сверху налево-вниз (кэса-гири), 2 — восходящий слева-снизу направо-вверх,
    /// 3 — сильный удар сверху (сёмэн), замах дольше.
    ///
    /// Позы задаются плоскостью замаха: наклон плоскости (tilt), угол клинка в плоскости (yaw от/до),
    /// поворот клинка вокруг оси, чтобы лезвие шло вперёд (roll), и сдвиг рукояти.
    /// ⚠ Те же числа — в Tools/SoundLab/index.html (ATTACKS), чтобы анимации на странице звуков совпадали с игрой.
    /// Звук взмаха должен начинаться в момент Windup (начало реза), звук попадания — в HitTime.
    /// </summary>
    public class SwordSwing : MonoBehaviour
    {
        public struct Attack
        {
            public float tilt, yawFrom, yawTo, roll;
            public Vector3 posFrom, posTo;
            public float windup, slash, hold, recover;
            public float HitTime => windup + slash * 0.5f;
            public float Length => windup + slash + hold + recover;
        }

        public static readonly Attack[] Attacks =
        {
            new Attack { tilt = 35f, yawFrom = 105f, yawTo = -80f, roll = -90f,
                posFrom = new Vector3(0.12f, 0.18f, -0.12f), posTo = new Vector3(-0.42f, -0.16f, 0.08f),
                windup = 0.07f, slash = 0.11f, hold = 0.05f, recover = 0.22f },
            new Attack { tilt = 28f, yawFrom = -95f, yawTo = 85f, roll = 90f,
                posFrom = new Vector3(-0.38f, -0.2f, 0f), posTo = new Vector3(0.12f, 0.16f, 0.02f),
                windup = 0.07f, slash = 0.11f, hold = 0.05f, recover = 0.22f },
            new Attack { tilt = 82f, yawFrom = 125f, yawTo = -35f, roll = -90f,
                posFrom = new Vector3(-0.15f, 0.32f, -0.15f), posTo = new Vector3(-0.2f, -0.22f, 0.15f),
                windup = 0.14f, slash = 0.12f, hold = 0.08f, recover = 0.28f },
        };

        /// <summary>Поза покоя: клинок вперёд-вверх, лезвие вниз.</summary>
        public static readonly Quaternion RestRotation = Quaternion.Euler(-32f, -12f, 0f);

        public static Quaternion SwingRotation(Attack a, float yaw) =>
            Quaternion.AngleAxis(a.tilt, Vector3.forward) * Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(a.roll, Vector3.forward);

        /// <summary>Поза клинка в момент t от начала удара (локально к держателю оружия).</summary>
        public static void Evaluate(Attack a, float t, Vector3 startPos, Quaternion startRot, out Vector3 pos, out Quaternion rot)
        {
            if (t < a.windup)
            {
                float k = Smooth(t / a.windup);
                pos = Vector3.Lerp(startPos, a.posFrom, k);
                rot = Quaternion.Slerp(startRot, SwingRotation(a, a.yawFrom), k);
                return;
            }
            t -= a.windup;
            if (t < a.slash)
            {
                float k = 1f - Mathf.Pow(1f - t / a.slash, 2.2f);           // резко в начале, тормозит в конце
                pos = Vector3.Lerp(a.posFrom, a.posTo, k);
                rot = SwingRotation(a, Mathf.Lerp(a.yawFrom, a.yawTo, k));
                return;
            }
            t -= a.slash;
            pos = a.posTo;
            rot = SwingRotation(a, a.yawTo);
            if (t < a.hold) return;
            t -= a.hold;
            float r = Smooth(Mathf.Clamp01(t / a.recover));
            pos = Vector3.Lerp(a.posTo, Vector3.zero, r);
            rot = Quaternion.Slerp(rot, RestRotation, r);
        }

        private static float Smooth(float x) => x * x * (3f - 2f * x);

        [SerializeField] private TrailRenderer trail;

        private int current = -1;
        private float time;
        private Vector3 startPos;
        private Quaternion startRot;

        public bool Busy => current >= 0;

        private void OnEnable()
        {
            current = -1;
            transform.localPosition = Vector3.zero;
            transform.localRotation = RestRotation;
            if (trail != null)
            {
                trail.emitting = false;
                trail.Clear();
            }
        }

        /// <summary>Начать удар index (0..2) из текущей позы.</summary>
        public void Play(int index)
        {
            current = Mathf.Clamp(index, 0, Attacks.Length - 1);
            time = 0f;
            startPos = transform.localPosition;
            startRot = transform.localRotation;
        }

        private void Update()
        {
            if (current < 0) return;
            var a = Attacks[current];
            time += Time.deltaTime;
            Evaluate(a, time, startPos, startRot, out var pos, out var rot);
            transform.localPosition = pos;
            transform.localRotation = rot;
            if (trail != null) trail.emitting = time > a.windup * 0.8f && time < a.windup + a.slash + a.hold;
            if (time >= a.Length) current = -1;
        }
    }
}
