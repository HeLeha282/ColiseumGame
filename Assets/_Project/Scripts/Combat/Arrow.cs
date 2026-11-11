using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Стрела игрока: летит по дуге, на каждом шаге проверяет попадание лучом.</summary>
    public class Arrow : MonoBehaviour
    {
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private float stuckLifetime = 2f;

        private Vector3 velocity;
        private float damage;
        private float gravity;
        private Transform ignoreRoot;
        private bool stuck;
        private static readonly RaycastHit[] Hits = new RaycastHit[8];

        public void Launch(Vector3 initialVelocity, float arrowDamage, float arrowGravity, Transform owner)
        {
            velocity = initialVelocity;
            damage = arrowDamage;
            gravity = arrowGravity;
            ignoreRoot = owner;
            transform.rotation = Quaternion.LookRotation(velocity);
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (stuck) return;

            Vector3 step = velocity * Time.deltaTime;
            velocity += Vector3.down * gravity * Time.deltaTime;
            float length = step.magnitude;
            if (length < 0.0001f) return;

            int count = Physics.RaycastNonAlloc(transform.position, step / length, Hits, length, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            int bestIndex = -1;
            for (int i = 0; i < count; i++)
            {
                if (ignoreRoot != null && Hits[i].transform.IsChildOf(ignoreRoot)) continue;
                if (Hits[i].distance < best)
                {
                    best = Hits[i].distance;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0)
            {
                var hit = Hits[bestIndex];
                transform.position = hit.point;
                var enemy = hit.collider.GetComponentInParent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(new DamageInfo(damage, DamageSource.Player, ignoreRoot != null ? ignoreRoot.position : transform.position));
                    Destroy(gameObject);
                }
                else
                {
                    // Втыкаемся в стену/пол и вскоре исчезаем.
                    stuck = true;
                    Destroy(gameObject, stuckLifetime);
                }
                return;
            }

            transform.position += step;
            transform.rotation = Quaternion.LookRotation(velocity);
        }
    }
}
