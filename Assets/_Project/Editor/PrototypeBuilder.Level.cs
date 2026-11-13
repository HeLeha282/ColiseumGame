using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using static ColiseumGame.EditorTools.BuilderUtil;

namespace ColiseumGame.EditorTools
{
    /// <summary>
    /// Геометрия Колизея. Схема (радиусы от центра, высоты):
    ///   арена         r 0..30,  y = 0   (в центре портал)
    ///   1-й ярус      r 30..38, y = 3
    ///   2-й ярус      r 38..45, y = 6   (4 арки-спавна во внешней стене)
    ///   внешняя стена r 45..46.5, до y = 11
    /// Маршрут от каждой арки: по 2-му ярусу ~80° по часовой → пандус вниз на 1-й ярус →
    /// ещё ~80° по 1-му ярусу → пандус на арену → к порталу. Получается спираль.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        private const float ArenaR = 30f;
        private const float Tier1R = 38f, Tier1Y = 3f;
        private const float Tier2R = 45f, Tier2Y = 6f;
        private const float WallR = 46.5f, WallY = 11f;
        private static readonly float[] ArchAngles = { 45f, 135f, 225f, 315f };
        // Лестницы между уровнями (те же числа — в ArtSource/Coliseum/v2/build_coliseum_v2.py: STAIR_T2/STAIR_T1)
        private const float StairT2Offset = -89f;    // 2-й ярус → 1-й: через 89° от ворот по часовой
        private const float StairT1Offset = -209f;   // 1-й ярус → арена: ещё через 120°
        private const float StairRun = 4.5f;         // горизонтальная длина лестницы (3 м вниз)
        private const float StairWidth = 3.4f;
        private const float Tier1PathR = 31.6f;      // по 1-му ярусу враги идут ближе к арене — мимо подножий лестниц
        private static readonly Color[] RouteColors = { Color.yellow, Color.cyan, Color.magenta, Color.green };

        // ---------- Колизей ----------

        private static GameObject BuildColiseum()
        {
            var root = new GameObject("Coliseum");
            var t = root.transform;

            // Земля и песок арены
            Box("Ground", t, new Vector3(0f, -0.5f, 0f), new Vector3(130f, 1f, 130f), Mats["Ground"], true);
            Prim(PrimitiveType.Cylinder, "ArenaSand", t, new Vector3(0f, 0.005f, 0f), new Vector3(ArenaR * 2f, 0.005f, ArenaR * 2f), Mats["Sand"]);

            // Ярусы — кольца из сегментов
            Ring(t, "Tier1", ArenaR, Tier1R, 0f, Tier1Y, 72, Mats["Stone"]);
            Ring(t, "Tier2", Tier1R, Tier2R, 0f, Tier2Y, 72, Mats["StoneDark"]);
            Ring(t, "OuterWall", Tier2R, WallR, 0f, WallY, 72, Mats["Wall"], deg => NearArch(deg, 6f));

            // Арки-спавны
            var arches = Empty("Arches", t, Vector3.zero);
            for (int i = 0; i < ArchAngles.Length; i++) BuildArch(arches, i, ArchAngles[i]);

            // Лестницы (пандусы-коллайдеры): прямые, перпендикулярно стене, со своего уровня вниз.
            // Стоят вразбежку, чтобы враги проходили часть каждого уровня (см. StairT2Offset / StairT1Offset).
            var ramps = Empty("Ramps", t, Vector3.zero);
            for (int i = 0; i < ArchAngles.Length; i++)
            {
                float a = ArchAngles[i];
                RadialRamp(ramps, $"Ramp_T2_T1_{i}", a + StairT2Offset, Tier1R, Tier2Y, Tier1R - StairRun, Tier1Y, StairWidth, Mats["Ramp"]);
                RadialRamp(ramps, $"Ramp_T1_Arena_{i}", a + StairT1Offset, ArenaR, Tier1Y, ArenaR - StairRun, 0f, StairWidth, Mats["Ramp"]);
            }

            // Свет
            var sun = new GameObject("Sun");
            sun.transform.SetParent(t, false);
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            light.color = new Color(1f, 0.96f, 0.88f);

            // NavMesh собирается только из геометрии Колизея
            var surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            // Красивая модель поверх серых коллайдеров (если FBX уже есть в проекте)
            ColiseumArtIntegration.AddArt(root);
            return root;
        }

        private static bool NearArch(float deg, float halfWidth)
        {
            foreach (var a in ArchAngles)
                if (Mathf.Abs(Mathf.DeltaAngle(deg, a)) < halfWidth) return true;
            return false;
        }

        private static void Ring(Transform parent, string name, float rIn, float rOut, float yBottom, float yTop, int segments,
            Material mat, System.Func<float, bool> skip = null)
        {
            var group = Empty(name, parent, Vector3.zero);
            float step = 360f / segments;
            float rMid = (rIn + rOut) * 0.5f;
            float width = 2f * Mathf.PI * rOut / segments * 1.03f;
            for (int i = 0; i < segments; i++)
            {
                float deg = i * step + step * 0.5f;
                if (skip != null && skip(deg)) continue;
                Box($"{name}_{i:00}", group, Polar(rMid, deg, (yBottom + yTop) * 0.5f),
                    new Vector3(width, yTop - yBottom, rOut - rIn), mat, true,
                    Quaternion.LookRotation(Polar(1f, deg, 0f)));
            }
        }

        private static void BuildArch(Transform parent, int index, float deg)
        {
            var arch = Empty($"Arch_{index}", parent, Vector3.zero);
            var rot = Quaternion.LookRotation(Polar(1f, deg, 0f));
            float rMid = (Tier2R + WallR) * 0.5f;
            const float gapWidth = 9.5f;
            // Нижняя часть стены под проёмом и перемычка сверху
            Box("WallBelow", arch, Polar(rMid, deg, Tier2Y * 0.5f), new Vector3(gapWidth, Tier2Y, WallR - Tier2R), Mats["Wall"], true, rot);
            Box("Lintel", arch, Polar(rMid, deg, 10.25f), new Vector3(gapWidth, 1.5f, WallR - Tier2R + 0.4f), Mats["Arch"], true, rot);
            // Колонны по бокам проёма
            Vector3 side = rot * Vector3.right;
            Vector3 center = Polar(rMid, deg, 0f);
            for (int s = -1; s <= 1; s += 2)
                Box(s < 0 ? "PillarL" : "PillarR", arch, center + side * (s * 3.2f) + Vector3.up * 8f,
                    new Vector3(1.2f, 4f, WallR - Tier2R + 0.4f), Mats["Arch"], true, rot);
            // Тёмный «тоннель» за аркой
            float tunnelR = WallR + 2f;
            Box("TunnelFloor", arch, Polar(tunnelR, deg, Tier2Y - 0.25f), new Vector3(gapWidth, 0.5f, 4f), Mats["StoneDark"], true, rot);
            Box("TunnelBack", arch, Polar(tunnelR + 2.25f, deg, 8.5f), new Vector3(gapWidth, 5f, 0.5f), Mats["Void"], true, rot);
            Box("TunnelRoof", arch, Polar(tunnelR, deg, 11.25f), new Vector3(gapWidth, 0.5f, 4.5f), Mats["StoneDark"], true, rot);
        }

        /// <summary>Наклонная плита-лестница по радиусу: от (rTop, yTop) вниз к (rBottom, yBottom) на угле deg.</summary>
        private static void RadialRamp(Transform parent, string name, float deg, float rTop, float yTop, float rBottom, float yBottom,
            float width, Material mat)
        {
            Vector3 top = Polar(rTop, deg, yTop);
            Vector3 bottom = Polar(rBottom, deg, yBottom);
            Vector3 along = top - bottom;
            var rot = Quaternion.LookRotation(along.normalized, Vector3.up);
            const float thickness = 0.4f;
            Vector3 center = (top + bottom) * 0.5f - rot * Vector3.up * (thickness * 0.5f);
            Box(name, parent, center, new Vector3(width, thickness, along.magnitude + 0.3f), mat, true, rot);
        }

        // ---------- Маршруты ----------

        private static List<Vector3> RoutePoints(float a)
        {
            float s2 = a + StairT2Offset, s1 = a + StairT1Offset;
            var pts = new List<Vector3>
            {
                Polar(44f, a, Tier2Y),                      // спавн в арке
                Polar(42f, a - 15f, Tier2Y),
                Polar(42f, a - 35f, Tier2Y),
                Polar(42f, a - 55f, Tier2Y),
                Polar(41.5f, a - 72f, Tier2Y),
                Polar(40.2f, s2 + 5f, Tier2Y),
                Polar(Tier1R + 1.2f, s2, Tier2Y),           // верх лестницы на 1-й ярус
                Polar(Tier1R - StairRun - .9f, s2, Tier1Y), // низ лестницы
            };
            // По 1-му ярусу — от лестницы сверху до лестницы на арену
            for (float d = s2 - 12f; d > s1 + 8f; d -= 20f) pts.Add(Polar(Tier1PathR, d, Tier1Y));
            pts.Add(Polar(Tier1PathR, s1 + 6f, Tier1Y));
            pts.Add(Polar(ArenaR + 1.2f, s1, Tier1Y));      // верх лестницы на арену
            pts.Add(Polar(ArenaR - StairRun - .9f, s1, 0f)); // низ лестницы
            pts.Add(Polar(17f, s1 - 9f, 0f));
            pts.Add(Polar(9f, s1 - 16f, 0f));               // дальше — прямо к порталу
            return pts;
        }

        private static GameObject BuildRoutes()
        {
            var root = new GameObject("Routes");
            for (int i = 0; i < ArchAngles.Length; i++)
            {
                float a = ArchAngles[i];
                MakeRoute(root.transform, "Route_" + i, i, 0, RouteColors[i], RoutePoints(a));
                // Развилка (акт II+): пройти по 2-му ярусу мимо своей лестницы и спуститься по лестнице соседней арки
                var alt = RoutePoints(a).GetRange(0, 4);
                alt.Add(Polar(42.5f, a - 75f, Tier2Y));
                alt.Add(Polar(42.5f, a - 95f, Tier2Y));     // в стороне от верха своей лестницы
                alt.Add(Polar(42.5f, a - 115f, Tier2Y));
                alt.Add(Polar(42.5f, a - 135f, Tier2Y));
                var next = RoutePoints(a - 90f);
                alt.AddRange(next.GetRange(4, next.Count - 4));
                MakeRoute(root.transform, "Route_" + i + "_Fork", i, 1, RouteColors[i], alt);
            }
            return root;
        }

        private static void MakeRoute(Transform parent, string name, int arch, int variant, Color color, List<Vector3> pts)
        {
            var routeGo = new GameObject(name);
            routeGo.transform.SetParent(parent, false);
            var route = routeGo.AddComponent<Route>();
            SetValue(route, "archIndex", arch);
            SetValue(route, "variant", variant);
            var so = new UnityEditor.SerializedObject(route);
            so.FindProperty("gizmoColor").colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
            for (int k = 0; k < pts.Count; k++) Empty($"WP_{k:00}", routeGo.transform, pts[k]);
        }

        // ---------- Места под баррикады ----------
        private static IEnumerable<(Vector3, Quaternion)> BarricadeSpotPoses()
        {
            foreach (var a in ArchAngles)
            {
                // Посередине пути по 1-му ярусу (поперёк дорожки: от края арены до подпорной стены)
                float deg = a + (StairT2Offset + StairT1Offset) * 0.5f;
                float rad = deg * Mathf.Deg2Rad;
                var travel = new Vector3(Mathf.Sin(rad), 0f, -Mathf.Cos(rad)); // по часовой стрелке
                yield return (Polar(33.6f, deg, Tier1Y), Quaternion.LookRotation(travel));

                // На арене сразу после спуска с лестницы
                float s1 = a + StairT1Offset;
                Vector3 p1 = Polar(ArenaR - StairRun - .9f, s1, 0f);
                Vector3 p2 = Polar(17f, s1 - 9f, 0f);
                yield return ((p1 + p2) * 0.5f, Quaternion.LookRotation(p2 - p1));
            }
        }

        private const float BarricadeWidth = 7f;

        private static GameObject BuildBarricadeSpot()
        {
            var root = new GameObject("BarricadeSpot");
            var col = root.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 1.25f, 0f);
            col.size = new Vector3(BarricadeWidth, 2.5f, 1.5f);
            var spot = root.AddComponent<BarricadeSpot>();
            var marker = Empty("Marker", root.transform, Vector3.zero);
            Box("Stripe", marker, new Vector3(0f, 0.03f, 0f), new Vector3(BarricadeWidth, 0.04f, 0.6f), Mats["BarricadeSpot"]);
            Box("PostL", marker, new Vector3(-BarricadeWidth * 0.5f, 0.5f, 0f), new Vector3(0.2f, 1f, 0.2f), Mats["BarricadeSpot"]);
            Box("PostR", marker, new Vector3(BarricadeWidth * 0.5f, 0.5f, 0f), new Vector3(0.2f, 1f, 0.2f), Mats["BarricadeSpot"]);
            Assign(spot, ("markerVisual", marker.gameObject));
            return root;
        }

        private static GameObject BuildBarricade(GameObject healthBarSource = null)
        {
            var root = new GameObject("Barricade");
            root.AddComponent<Health>();
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.size = new Vector3(BarricadeWidth, 2f, 0.8f);
            var barricade = root.AddComponent<Barricade>();
            for (int i = 0; i < 3; i++)
                Box($"Plank_{i}", root.transform, new Vector3(0f, 0.5f + i * 0.6f, 0f), new Vector3(BarricadeWidth, 0.4f, 0.25f), Mats["Wood"],
                    false, Quaternion.Euler(0f, 0f, i == 1 ? 3f : -2f));
            foreach (var x in new[] { -BarricadeWidth * 0.47f, 0f, BarricadeWidth * 0.47f })
                Box("Post", root.transform, new Vector3(x, 1.1f, 0f), new Vector3(0.3f, 2.2f, 0.5f), Mats["Wood"]);
            var bar = BuildWorldBar(root.transform, 2.7f, 2f, false, false, new Color(0.95f, 0.6f, 0.2f));
            Assign(barricade, ("blockCollider", col), ("healthBar", bar));
            return root;
        }

        private static GameObject BuildPortal(GameBalance balance)
        {
            var root = new GameObject("Portal");
            root.AddComponent<Health>();
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 2f, 0f);
            col.radius = 2f;
            col.height = 4.2f;
            var portal = root.AddComponent<Portal>();
            var obstacle = root.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Capsule;
            obstacle.center = new Vector3(0f, 2f, 0f);
            obstacle.radius = 2.2f;
            obstacle.height = 4.2f;
            obstacle.carving = true;

            // Постамент — в модели Колизея; здесь только сама энергия портала: сгусток и два наклонных кольца
            var glow = Prim(PrimitiveType.Sphere, "Core", root.transform, new Vector3(0f, 2.7f, 0f), new Vector3(1.5f, 2.3f, 1.5f), Mats["Portal"]);
            Prim(PrimitiveType.Cylinder, "RingA", root.transform, new Vector3(0f, 2.7f, 0f), new Vector3(3.2f, 0.03f, 3.2f), Mats["Portal"],
                false, Quaternion.Euler(70f, 0f, 15f));
            Prim(PrimitiveType.Cylinder, "RingB", root.transform, new Vector3(0f, 2.7f, 0f), new Vector3(2.6f, 0.03f, 2.6f), Mats["Portal"],
                false, Quaternion.Euler(70f, 90f, -20f));
            Prim(PrimitiveType.Cylinder, "Pool", root.transform, new Vector3(0f, 1.0f, 0f), new Vector3(3.4f, 0.03f, 3.4f), Mats["Portal"]);
            var lightGo = Empty("Light", root.transform, new Vector3(0f, 3f, 0f));
            var light = lightGo.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.3f, 0.9f, 1f);
            light.intensity = 3f;
            light.range = 14f;

            Assign(portal, ("balance", balance), ("hitCollider", col), ("glowRenderer", glow.GetComponent<Renderer>()));
            return root;
        }
    }
}
