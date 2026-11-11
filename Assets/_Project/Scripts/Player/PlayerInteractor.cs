using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Что игрок видит в прицеле: слот башни или место для баррикады → [F] открывает меню.
    /// Ремонт: держать E рядом с повреждённой башней в фазе волны, тратится эссенция.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private float rayLength = 40f;

        /// <summary>Объект под прицелом (или null).</summary>
        public IInteractable Current { get; private set; }
        /// <summary>Башня, которую сейчас чиним (или null).</summary>
        public Tower Repairing { get; private set; }

        private PlayerController player;
        private float repairCostDebt;
        private static readonly RaycastHit[] Hits = new RaycastHit[16];

        private void Awake() => player = GetComponent<PlayerController>();

        private void Update()
        {
            var input = InputReader.Instance;
            var menu = BuildMenu.Instance;
            Repairing = null;
            if (input == null || !player.IsAlive)
            {
                Current = null;
                return;
            }

            if (UIState.MenuOpen)
            {
                if (menu != null && (input.InteractPressed || input.CancelPressed)) menu.Close();
                return;
            }
            if (UIState.ResultOpen)
            {
                Current = null;
                return;
            }

            Current = FindLookTarget();
            if (Current != null && input.InteractPressed && menu != null) menu.Open(Current);

            if (input.RepairHeld) TryRepair();
            else repairCostDebt = 0f;
        }

        private IInteractable FindLookTarget()
        {
            var cam = Camera.main;
            if (cam == null) return null;
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int count = Physics.RaycastNonAlloc(ray, Hits, rayLength, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(Hits, 0, count, HitComparer.Instance);
            for (int i = 0; i < count; i++)
            {
                var hit = Hits[i];
                if (hit.transform.IsChildOf(transform)) continue;
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    var comp = (Component)interactable;
                    float dist = Vector3.Distance(transform.position, comp.transform.position);
                    return dist <= config.interactDistance ? interactable : null;
                }
                // Луч упёрся в стену/пол/врага — дальше не ищем.
                if (!hit.collider.isTrigger) return null;
            }
            return null;
        }

        private void TryRepair()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Phase != GamePhase.Wave) return;
            var balance = gm.Balance;

            // Ближайшая повреждённая башня рядом с игроком.
            Tower best = null;
            float bestDist = balance.repairRadius;
            foreach (var t in Tower.All)
            {
                if (t == null || !t.IsAlive || !t.IsDamaged) continue;
                Vector3 closest = t.ClosestPoint(transform.position + Vector3.up);
                float d = Vector3.Distance(closest, transform.position + Vector3.up);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = t;
                }
            }
            if (best == null) return;

            float hp = balance.repairHpPerSecond * Time.deltaTime;
            float cost = hp * balance.repairCostPerHP;
            // Копим дробную стоимость и списываем целыми единицами эссенции.
            if (repairCostDebt + cost >= 1f)
            {
                int whole = Mathf.FloorToInt(repairCostDebt + cost);
                if (!gm.CanAfford(whole))
                {
                    gm.ShowMessage("Недостаточно эссенции для ремонта");
                    return;
                }
                gm.TrySpend(whole);
                repairCostDebt = repairCostDebt + cost - whole;
            }
            else
            {
                repairCostDebt += cost;
            }
            best.Repair(hp);
            Repairing = best;
        }

        private class HitComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly HitComparer Instance = new HitComparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
