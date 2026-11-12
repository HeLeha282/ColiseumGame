using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// F — меню башни или места баррикады под прицелом. E — действие рядом: добить/прижечь босса,
    /// расколдовать окаменевшую башню. Текст подсказки для HUD — Prompt.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private float rayLength = 40f;
        [SerializeField] private float unpetrifyRange = 4f;

        /// <summary>Объект под прицелом (или null).</summary>
        public IInteractable Current { get; private set; }
        /// <summary>Подсказка для E (босс/окаменевшая башня) или null.</summary>
        public string UsePrompt { get; private set; }

        private PlayerController player;
        private BossBase useBoss;
        private Tower useTower;
        private static readonly RaycastHit[] Hits = new RaycastHit[16];

        private void Awake() => player = GetComponent<PlayerController>();

        private void Update()
        {
            var input = InputReader.Instance;
            var menu = BuildMenu.Instance;
            if (input == null || !player.IsAlive)
            {
                Current = null;
                UsePrompt = null;
                return;
            }
            if (UIState.MenuOpen)
            {
                if (menu != null && menu.IsOpen && (input.InteractPressed || input.CancelPressed)) menu.Close();
                return;
            }
            if (UIState.GameplayBlocked)
            {
                Current = null;
                UsePrompt = null;
                return;
            }

            FindUseTarget();
            if (input.UsePressed)
            {
                if (useBoss != null) useBoss.Interact();
                else if (useTower != null) useTower.Unpetrify();
            }

            var combat = GetComponent<PlayerCombat>();
            Current = combat != null && combat.Weapon == WeaponType.Hammer ? null : FindLookTarget();
            if (Current != null && input.InteractPressed && menu != null) menu.Open(Current);
        }

        private void FindUseTarget()
        {
            useBoss = null;
            useTower = null;
            UsePrompt = null;
            foreach (var b in BossBase.All)
            {
                if (b != null && b.CanInteract(transform.position))
                {
                    useBoss = b;
                    UsePrompt = b.InteractPrompt;
                    return;
                }
            }
            float best = unpetrifyRange;
            foreach (var t in Tower.All)
            {
                if (!t.IsPetrified) continue;
                float d = Vector3.Distance(t.transform.position, transform.position);
                if (d < best) { best = d; useTower = t; }
            }
            if (useTower != null) UsePrompt = "[E] Расколдовать башню";
        }

        private IInteractable FindLookTarget()
        {
            var cam = Camera.main;
            if (cam == null) return null;
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int count = Physics.RaycastNonAlloc(ray, Hits, rayLength, ~LayerMask.GetMask("CameraBlocker"), QueryTriggerInteraction.Collide);
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
                    return dist <= config.interactDistance + 3f ? interactable : null;
                }
                if (!hit.collider.isTrigger) return null;
            }
            return null;
        }

        private class HitComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly HitComparer Instance = new HitComparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
