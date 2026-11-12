using System.Collections.Generic;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Стрела героя: летит по дуге, проверяет попадание лучом, может пробивать врагов (лук ур.3+).</summary>
    public class Arrow : MonoBehaviour
    {
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private float stuckLifetime = 2f;

        private Vector3 velocity;
        private float damage, gravity;
        private Transform ignoreRoot;
        private Element element;
        private int pierce;
        private PlayerCombat owner;
        private bool stuck;
        private readonly HashSet<Enemy> hit = new HashSet<Enemy>();
        private static readonly RaycastHit[] Hits = new RaycastHit[8];

        public void Launch(Vector3 initialVelocity, float arrowDamage, float arrowGravity, Transform ownerRoot,
            Element arrowElement = Element.None, int pierceCount = 0, PlayerCombat combat = null)
        {
            velocity = initialVelocity;
            damage = arrowDamage;
            gravity = arrowGravity;
            ignoreRoot = ownerRoot;
            element = arrowElement;
            pierce = pierceCount;
            owner = combat;
            transform.rotation = Quaternion.LookRotation(velocity);
            if (element != Element.None)
                foreach (var r in GetComponentsInChildren<Renderer>()) r.sharedMaterial = Vfx.Mat(Elements.Color(element));
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
            System.Array.Sort(Hits, 0, count, Comparer.Instance);
            for (int i = 0; i < count; i++)
            {
                var h = Hits[i];
                if (ignoreRoot != null && h.transform.IsChildOf(ignoreRoot)) continue;
                if (h.collider.GetComponentInParent<Tower>() != null) continue;
                var enemy = h.collider.GetComponentInParent<Enemy>();
                if (enemy != null)
                {
                    if (hit.Contains(enemy)) continue;
                    hit.Add(enemy);
                    if (owner != null) owner.HitEnemy(enemy, damage, element);
                    else enemy.TakeDamage(new DamageInfo(damage, DamageSource.Player, transform.position, element));
                    if (pierce-- > 0) { damage *= 0.8f; continue; }
                    Destroy(gameObject);
                    return;
                }
                transform.position = h.point;
                stuck = true;
                Destroy(gameObject, stuckLifetime);
                return;
            }
            transform.position += step;
            transform.rotation = Quaternion.LookRotation(velocity);
        }

        private class Comparer : IComparer<RaycastHit>
        {
            public static readonly Comparer Instance = new Comparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
