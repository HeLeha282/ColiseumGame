using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Простая процедурная анимация врага из примитивов (пока нет настоящих моделей):
    /// шаг ногами и мах руками в такт скорости, взмахи крыльев, покачивание хвоста.
    /// Ноги для четвероногих — в порядке «передняя Л, передняя П, задняя Л, задняя П» (шаг по диагонали).
    /// </summary>
    public class EnemyRig : MonoBehaviour
    {
        [SerializeField] private Transform[] legs = new Transform[0];
        [SerializeField] private Transform[] arms = new Transform[0];
        [SerializeField] private Transform[] wings = new Transform[0];
        [SerializeField] private Transform[] tails = new Transform[0];
        [SerializeField] private float stride = 32f;
        [SerializeField] private float armSwing = 22f;
        [SerializeField] private float wingFlap = 35f;
        [SerializeField] private float wingSpeed = 9f;

        private Enemy enemy;
        private Quaternion[] legRest, armRest, wingRest, tailRest;
        private float phase, seed;

        private void Awake()
        {
            enemy = GetComponentInParent<Enemy>();
            legRest = Rest(legs);
            armRest = Rest(arms);
            wingRest = Rest(wings);
            tailRest = Rest(tails);
            seed = Random.value * 10f;
        }

        private static Quaternion[] Rest(Transform[] t)
        {
            var r = new Quaternion[t.Length];
            for (int i = 0; i < t.Length; i++) r[i] = t[i] != null ? t[i].localRotation : Quaternion.identity;
            return r;
        }

        private void Update()
        {
            float speed = enemy != null && enemy.IsAlive ? enemy.Velocity.magnitude : 0f;
            float walk = Mathf.Clamp01(speed / 2.5f);
            phase += Time.deltaTime * (1.5f + speed * 2.4f);

            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i] == null) continue;
                float offset = ((i % 2) ^ (i / 2 % 2)) * Mathf.PI;
                legs[i].localRotation = legRest[i] * Quaternion.Euler(Mathf.Sin(phase + offset) * stride * walk, 0f, 0f);
            }
            for (int i = 0; i < arms.Length; i++)
            {
                if (arms[i] == null) continue;
                float offset = i % 2 == 0 ? Mathf.PI : 0f;
                float idle = Mathf.Sin(Time.time * 1.6f + seed + i) * 4f;
                arms[i].localRotation = armRest[i] * Quaternion.Euler(Mathf.Sin(phase + offset) * armSwing * walk + idle, 0f, 0f);
            }
            float flap = Mathf.Sin(Time.time * wingSpeed + seed) * wingFlap;
            for (int i = 0; i < wings.Length; i++)
                if (wings[i] != null)
                    wings[i].localRotation = wingRest[i] * Quaternion.Euler(0f, 0f, i % 2 == 0 ? flap : -flap);
            for (int i = 0; i < tails.Length; i++)
                if (tails[i] != null)
                    tails[i].localRotation = tailRest[i] * Quaternion.Euler(0f, Mathf.Sin(Time.time * 3f + seed + i * 0.7f) * 14f, 0f);
        }
    }
}
