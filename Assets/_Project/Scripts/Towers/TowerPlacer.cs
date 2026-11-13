using System.Collections.Generic;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Режим строительства (молоток, клавиша 3): показывает полупрозрачный «призрак» выбранной башни там,
    /// куда смотрит прицел (зелёный — можно, красный — нельзя, с причиной), круг дальности и ауры святилищ.
    /// ЛКМ — поставить, колесо или Q/R — выбрать тип, ПКМ/1/2 — убрать молоток.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class TowerPlacer : MonoBehaviour
    {
        public static TowerPlacer Instance { get; private set; }

        [SerializeField] private float rayLength = 60f;

        public bool Active { get; private set; }
        public TowerConfig Selected => Types.Length > 0 ? Types[Mathf.Clamp(selected, 0, Types.Length - 1)] : null;
        public bool IsValid { get; private set; }
        public string Reason { get; private set; } = "";

        private TowerConfig[] Types => GameManager.Instance != null ? GameManager.Instance.Balance.towerTypes : new TowerConfig[0];
        private int selected;
        private readonly Dictionary<TowerConfig, GameObject> ghosts = new Dictionary<TowerConfig, GameObject>();
        private readonly Dictionary<Tower, LineRenderer> auraCircles = new Dictionary<Tower, LineRenderer>();
        private LineRenderer rangeCircle;
        private Material ghostOk, ghostBad;
        private Vector3 point;
        private static readonly RaycastHit[] Hits = new RaycastHit[16];

        private void Awake() => Instance = this;

        public void SetActive(bool value)
        {
            Active = value;
            if (!value) HideAll();
        }

        public void Cycle(int step)
        {
            if (Types.Length == 0) return;
            selected = (selected + step + Types.Length) % Types.Length;
        }

        private void Update()
        {
            if (!Active || UIState.GameplayBlocked)
            {
                HideAll();
                return;
            }
            var input = InputReader.Instance;
            if (input != null)
            {
                int s = input.ScrollStep;
                if (s != 0) Cycle(-s);
                if (input.Ability1Pressed) Cycle(-1);
                if (input.Ability2Pressed) Cycle(1);
            }
            var cfg = Selected;
            if (cfg == null) return;

            bool hasPoint = Aim(out point, out Vector3 normal, out bool hitTower);
            IsValid = false;
            if (!hasPoint) Reason = "Наведи прицел на площадку";
            else if (hitTower) Reason = "Здесь уже стоит башня";
            else IsValid = PlacementRules.Check(cfg, point, normal, transform.position, out string reason) ? true : (Reason = reason) == null;
            if (IsValid) Reason = "";

            ShowGhost(cfg, hasPoint);
            ShowAuras(true);
            if (hasPoint && input != null && input.AttackPressed)
            {
                if (IsValid) Place(cfg, point);
                else GameManager.Instance.ShowMessage(Reason);
            }
        }

        private bool Aim(out Vector3 p, out Vector3 normal, out bool hitTower)
        {
            p = normal = Vector3.zero;
            hitTower = false;
            var cam = Camera.main;
            if (cam == null) return false;
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int mask = ~LayerMask.GetMask("CameraBlocker");
            int count = Physics.RaycastNonAlloc(ray, Hits, rayLength, mask, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                var h = Hits[i];
                if (h.transform.IsChildOf(transform) || h.collider.GetComponentInParent<Enemy>() != null) continue;
                if (h.distance >= best) continue;
                best = h.distance;
                p = h.point;
                normal = h.normal;
                hitTower = h.collider.GetComponentInParent<Tower>() != null;
                found = true;
            }
            return found;
        }

        /// <summary>Поставить башню (используется и ботом для автотестов). Проверки — PlacementRules.</summary>
        public bool TryPlaceAt(TowerConfig cfg, Vector3 at)
        {
            if (!PlacementRules.Check(cfg, at, Vector3.up, transform.position, out string reason, false))
            {
                Reason = reason;
                return false;
            }
            Place(cfg, at);
            return true;
        }

        private void Place(TowerConfig cfg, Vector3 at)
        {
            int cost = cfg.levels[0].cost;
            if (!GameManager.Instance.TrySpend(cost)) return;
            Vector3 toCenter = -new Vector3(at.x, 0f, at.z);
            var tower = Instantiate(cfg.prefab, at, toCenter.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toCenter) : Quaternion.identity);
            tower.Init(cost);
            Vfx.Ring(at, new Color(1f, 0.85f, 0.4f), 3f, 0.5f);
            Vfx.Flash(at + Vector3.up * 0.3f, new Color(0.85f, 0.75f, 0.6f), 1.6f, 0.3f);
            GetComponent<PlayerCombat>()?.PlayHammerSwing();
        }

        // ---------- призрак

        private void ShowGhost(TowerConfig cfg, bool visible)
        {
            foreach (var kv in ghosts) if (kv.Key != cfg && kv.Value.activeSelf) kv.Value.SetActive(false);
            if (!ghosts.TryGetValue(cfg, out var ghost) || ghost == null)
            {
                ghost = MakeGhost(cfg);
                ghosts[cfg] = ghost;
            }
            ghost.SetActive(visible);
            if (rangeCircle == null) rangeCircle = MakeCircle("GhostRange");
            rangeCircle.enabled = visible;
            if (!visible) return;
            Vector3 toCenter = -new Vector3(point.x, 0f, point.z);
            ghost.transform.SetPositionAndRotation(point, toCenter.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toCenter) : Quaternion.identity);
            var mat = IsValid ? GhostMat(true) : GhostMat(false);
            foreach (var r in ghost.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mat;
            DrawCircle(rangeCircle, point, cfg.levels[0].range, IsValid ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.35f, 0.3f));
        }

        private GameObject MakeGhost(TowerConfig cfg)
        {
            // Создаём под выключенным родителем, чтобы скрипты башни не успели проснуться (Awake/OnEnable),
            // и вычищаем всё, кроме мешей.
            var holder = new GameObject("GhostHolder");
            holder.SetActive(false);
            var ghost = Instantiate(cfg.prefab.gameObject, holder.transform);
            ghost.name = "Ghost_" + cfg.displayName;
            foreach (var mb in ghost.GetComponentsInChildren<MonoBehaviour>(true)) DestroyImmediate(mb);
            foreach (var c in ghost.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
            foreach (var o in ghost.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>(true)) DestroyImmediate(o);
            foreach (var a in ghost.GetComponentsInChildren<Animation>(true)) DestroyImmediate(a);
            foreach (var lr in ghost.GetComponentsInChildren<LineRenderer>(true)) DestroyImmediate(lr);
            ghost.transform.SetParent(null, false);
            Destroy(holder);
            foreach (var r in ghost.GetComponentsInChildren<Renderer>(true))
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ghost;
        }

        private Material GhostMat(bool ok)
        {
            if (ok && ghostOk != null) return ghostOk;
            if (!ok && ghostBad != null) return ghostBad;
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            m.SetColor("_BaseColor", ok ? new Color(0.35f, 1f, 0.45f, 0.42f) : new Color(1f, 0.3f, 0.25f, 0.42f));
            if (ok) ghostOk = m; else ghostBad = m;
            return m;
        }

        // ---------- круги

        private LineRenderer MakeCircle(string name)
        {
            var go = new GameObject(name);
            var lr = go.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.positionCount = 64;
            lr.widthMultiplier = 0.08f;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return lr;
        }

        private static void DrawCircle(LineRenderer lr, Vector3 center, float radius, Color color)
        {
            lr.sharedMaterial = Vfx.Mat(color, 1.5f);
            for (int i = 0; i < lr.positionCount; i++)
            {
                float a = i * Mathf.PI * 2f / lr.positionCount;
                lr.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, 0.15f, Mathf.Sin(a) * radius));
            }
        }

        /// <summary>С молотком в руках видны ауры святилищ — чтобы ставить башни рядом с ними.</summary>
        private void ShowAuras(bool show)
        {
            foreach (var t in Tower.All)
            {
                if (t.Config.kind != TowerKind.Support) continue;
                if (!auraCircles.TryGetValue(t, out var lr) || lr == null)
                {
                    lr = MakeCircle("Aura");
                    auraCircles[t] = lr;
                }
                lr.enabled = show;
                if (show) DrawCircle(lr, t.transform.position, t.Range, new Color(1f, 0.85f, 0.35f));
            }
            var dead = new List<Tower>();
            foreach (var kv in auraCircles)
                if (kv.Key == null) { if (kv.Value != null) Destroy(kv.Value.gameObject); dead.Add(kv.Key); }
            foreach (var d in dead) auraCircles.Remove(d);
        }

        private void HideAll()
        {
            foreach (var g in ghosts.Values) if (g != null && g.activeSelf) g.SetActive(false);
            if (rangeCircle != null) rangeCircle.enabled = false;
            foreach (var lr in auraCircles.Values) if (lr != null) lr.enabled = false;
        }
    }
}
