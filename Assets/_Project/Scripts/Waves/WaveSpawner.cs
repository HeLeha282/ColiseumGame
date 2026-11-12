using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Спавнит врагов волн (может вести несколько волн сразу — при досрочном вызове), выбирает маршрут
    /// (основной или развилку), строит отряды, вешает элитные модификаторы. Сообщает, когда всё зачищено.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        private Route[] mainRoutes, altRoutes;
        private Action onCleared;
        private bool running;
        private int roundRobin;
        private readonly System.Random rng = new System.Random();

        public int RemainingToSpawn { get; private set; }
        public int Alive => Enemy.All.Count;
        public bool Running => running;
        public bool AllSpawned => RemainingToSpawn <= 0;

        private void Awake()
        {
            var routes = FindObjectsOfType<Route>();
            mainRoutes = routes.Where(r => r.Variant == 0).OrderBy(r => r.ArchIndex).ToArray();
            altRoutes = routes.Where(r => r.Variant != 0).OrderBy(r => r.ArchIndex).ToArray();
            if (mainRoutes.Length == 0) Debug.LogError("В сцене нет маршрутов (Route)");
        }

        /// <summary>Запустить волну. Можно вызывать, пока идёт предыдущая (досрочный вызов).</summary>
        public void Run(WaveConfig wave, float hpMultiplier, Action cleared)
        {
            onCleared = cleared;
            running = true;
            RemainingToSpawn += wave.groups.Sum(g => g.count);
            foreach (var g in wave.groups) StartCoroutine(SpawnGroup(g, hpMultiplier));
        }

        public void StopAll()
        {
            StopAllCoroutines();
            running = false;
            RemainingToSpawn = 0;
        }

        private IEnumerator SpawnGroup(SpawnGroup group, float hpMultiplier)
        {
            if (group.startDelay > 0f) yield return new WaitForSeconds(group.startDelay);
            Route squadRoute = null;
            for (int i = 0; i < group.count; i++)
            {
                if (group.enemy == null) { RemainingToSpawn--; continue; }
                Route route = group.squad ? (squadRoute ??= PickRoute(group)) : PickRoute(group);
                Vector3 offset = Vector3.zero;
                if (group.squad)
                {
                    // Строй: 2 ряда плечом к плечу
                    int col = i % 3, row = i / 3;
                    offset = new Vector3((col - 1) * 1.1f, 0f, -row * 1.3f);
                }
                Spawn(group.enemy, route, hpMultiplier, group.elite, offset);
                RemainingToSpawn--;
                if (i < group.count - 1) yield return new WaitForSeconds(group.interval);
            }
        }

        private Route PickRoute(SpawnGroup g)
        {
            int arch = g.arch >= 0 && g.arch < mainRoutes.Length ? g.arch : roundRobin++ % mainRoutes.Length;
            if (g.altRouteChance > 0f && rng.NextDouble() < g.altRouteChance)
            {
                var alt = altRoutes.FirstOrDefault(r => r.ArchIndex == arch);
                if (alt != null) return alt;
            }
            return mainRoutes[arch];
        }

        private Enemy Spawn(EnemyConfig config, Route route, float hpMultiplier, EliteModifier elite, Vector3 localOffset)
        {
            Vector3 pos = config.isFlying ? route.FlyerSpawnPoint : route.SpawnPoint;
            Vector3 dir = route.Count > 1 ? route.GetPoint(1) - route.SpawnPoint : Vector3.forward;
            dir.y = 0f;
            var rot = Quaternion.LookRotation(dir.sqrMagnitude > 0.01f ? dir : Vector3.forward);
            var enemy = Instantiate(config.prefab, pos + rot * localOffset, rot);
            float diff = GameManager.Instance != null ? GameManager.Instance.EnemyHpMultiplier : 1f;
            enemy.Init(route, hpMultiplier * diff, elite);
            return enemy;
        }

        /// <summary>Дочерний враг (деление гидрёныша, призыв гарпий) — сразу в точке.</summary>
        public void SpawnChild(EnemyConfig config, Route route, Vector3 position, float hpMultiplier, Enemy parent)
        {
            if (config == null || config.prefab == null || route == null) return;
            var enemy = Instantiate(config.prefab, position, parent != null ? parent.transform.rotation : Quaternion.identity);
            enemy.InitAsChild(route, hpMultiplier, parent);
            if (config.isFlying) enemy.transform.position = position;
        }

        private void Update()
        {
            if (running && RemainingToSpawn <= 0 && Enemy.All.Count == 0)
            {
                running = false;
                onCleared?.Invoke();
            }
        }
    }
}
