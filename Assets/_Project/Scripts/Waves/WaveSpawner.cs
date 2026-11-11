using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Спавнит врагов волны по группам из WaveConfig и сообщает, когда волна зачищена.</summary>
    public class WaveSpawner : MonoBehaviour
    {
        private Route[] routes;
        private Action onCleared;
        private bool running;
        private int roundRobin;

        public int RemainingToSpawn { get; private set; }
        public int Alive => Enemy.All.Count;
        public bool Running => running;

        private void Awake()
        {
            routes = FindObjectsOfType<Route>().OrderBy(r => r.ArchIndex).ToArray();
            if (routes.Length == 0) Debug.LogError("В сцене нет маршрутов (Route)");
        }

        public void Run(WaveConfig wave, Action cleared)
        {
            StopAll();
            onCleared = cleared;
            running = true;
            RemainingToSpawn = wave.groups.Sum(g => g.count);
            foreach (var g in wave.groups) StartCoroutine(SpawnGroup(g, wave.hpMultiplier));
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
            for (int i = 0; i < group.count; i++)
            {
                Spawn(group.enemy, PickRoute(group.arch), hpMultiplier);
                RemainingToSpawn--;
                if (i < group.count - 1) yield return new WaitForSeconds(group.interval);
            }
        }

        private Route PickRoute(int arch)
        {
            if (arch >= 0 && arch < routes.Length) return routes[arch];
            return routes[roundRobin++ % routes.Length];
        }

        private void Spawn(EnemyConfig config, Route route, float hpMultiplier)
        {
            Vector3 pos = config.isFlying ? route.FlyerSpawnPoint : route.SpawnPoint;
            Vector3 dir = route.Count > 1 ? route.GetPoint(1) - route.SpawnPoint : Vector3.forward;
            dir.y = 0f;
            var enemy = Instantiate(config.prefab, pos, Quaternion.LookRotation(dir.sqrMagnitude > 0.01f ? dir : Vector3.forward));
            enemy.Init(route, hpMultiplier);
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
