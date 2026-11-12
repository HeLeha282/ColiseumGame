using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Где можно ставить башню. Башня встаёт на ровные площадки арены и ярусов, но не на путь врагов,
    /// не у портала, не на баррикады и не вплотную к другим башням. Числа — в GameBalance.
    /// </summary>
    public static class PlacementRules
    {
        // Площадки Колизея (как в PrototypeBuilder.Level): высота и кольцо радиусов
        private static readonly (float y, float rMin, float rMax)[] Levels =
        {
            (0f, 0f, 28.6f),
            (3f, 30.6f, 37.4f),
            (6f, 38.6f, 44.4f),
        };

        public static bool Check(TowerConfig cfg, Vector3 point, Vector3 normal, Vector3 playerPos, out string reason)
        {
            var gm = GameManager.Instance;
            var b = gm.Balance;
            reason = "";
            if (gm.Phase != GamePhase.Preparation) { reason = "Строить можно только в фазе подготовки"; return false; }
            int limit = PlayerProgression.Instance != null ? PlayerProgression.Instance.TowerLimit : b.hero.baseTowerLimit;
            if (Tower.All.Count >= limit) { reason = $"Достигнут лимит башен ({limit}). Увеличивается при повышении уровня героя"; return false; }
            if (!gm.CanAfford(cfg.levels[0].cost)) { reason = "Не хватает эссенции"; return false; }
            if (normal.y < 0.92f) { reason = "Нужна ровная площадка"; return false; }

            Vector3 flat = new Vector3(point.x, 0f, point.z);
            float r = flat.magnitude;
            bool onLevel = false;
            foreach (var l in Levels)
                if (Mathf.Abs(point.y - l.y) < 0.4f && r >= l.rMin + cfg.footprint * 0.5f && r <= l.rMax - cfg.footprint * 0.5f) onLevel = true;
            if (!onLevel) { reason = "Сюда нельзя: только арена и дорожки ярусов"; return false; }
            if (point.y < 1f && r < b.portalClearance) { reason = "Слишком близко к порталу"; return false; }

            Vector3 pp = playerPos; pp.y = point.y;
            if (Vector3.Distance(pp, point) > b.maxBuildDistance) { reason = "Слишком далеко — подойди ближе"; return false; }

            foreach (var t in Tower.All)
            {
                float min = Mathf.Max(b.minTowerDistance, cfg.footprint + t.Config.footprint + 0.5f);
                if (Vector3.Distance(t.transform.position, point) < min) { reason = $"Слишком близко к другой башне (нужно {min:0.#} м)"; return false; }
            }

            foreach (var spot in Object.FindObjectsOfType<BarricadeSpot>())
                if (Vector3.Distance(spot.transform.position, point) < b.barricadeClearance + cfg.footprint) { reason = "Здесь место для баррикады"; return false; }

            float clearance = b.routeClearance + cfg.footprint * 0.5f;
            foreach (var route in Object.FindObjectsOfType<Route>())
                if (DistanceToRoute(route, point) < clearance) { reason = "Нельзя на пути врагов"; return false; }
            return true;
        }

        /// <summary>Расстояние от точки до ломаной маршрута (только сегменты примерно на той же высоте).</summary>
        public static float DistanceToRoute(Route route, Vector3 point)
        {
            float best = float.MaxValue;
            int n = route.Count;
            var portal = Portal.Instance;
            for (int i = 0; i < n; i++)
            {
                Vector3 a = route.GetPoint(i);
                Vector3 c = i + 1 < n ? route.GetPoint(i + 1) : (portal != null ? portal.transform.position : a);
                if (Mathf.Abs(point.y - a.y) > 1.6f && Mathf.Abs(point.y - c.y) > 1.6f) continue;
                Vector3 a2 = new Vector3(a.x, 0f, a.z), c2 = new Vector3(c.x, 0f, c.z), p2 = new Vector3(point.x, 0f, point.z);
                Vector3 ac = c2 - a2;
                float t = ac.sqrMagnitude > 0.001f ? Mathf.Clamp01(Vector3.Dot(p2 - a2, ac) / ac.sqrMagnitude) : 0f;
                best = Mathf.Min(best, Vector3.Distance(p2, a2 + ac * t));
            }
            return best;
        }
    }
}
