using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColiseumGame
{
    public enum GamePhase
    {
        Preparation,
        Wave,
        Victory,
        Defeat
    }

    /// <summary>
    /// Главный цикл: подготовка → волна → награда → подготовка ... → победа/поражение.
    /// Хранит эссенцию и номер волны.
    /// </summary>
    [RequireComponent(typeof(WaveSpawner))]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameBalance balance;
        [SerializeField] private EssenceOrb orbPrefab;
        [SerializeField] private Barricade barricadePrefab;

        public GameBalance Balance => balance;
        public Barricade BarricadePrefab => barricadePrefab;
        public GamePhase Phase { get; private set; } = GamePhase.Preparation;
        public int Essence { get; private set; }
        /// <summary>Индекс текущей (в фазе волны) или следующей (в подготовке) волны, с нуля.</summary>
        public int WaveIndex { get; private set; }
        public int WaveCount => balance.waves.Length;
        public int Kills { get; private set; }
        public WaveSpawner Spawner { get; private set; }

        public event Action PhaseChanged;
        public event Action EssenceChanged;
        public event Action<string> MessageShown;

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            UIState.Reset();
            Spawner = GetComponent<WaveSpawner>();
            Essence = balance.startEssence;
        }

        private void Start()
        {
            if (Portal.Instance != null) Portal.Instance.Destroyed += OnPortalDestroyed;
            SetPhase(GamePhase.Preparation);
        }

        private void Update()
        {
            var input = InputReader.Instance;
            if (input != null && input.ReadyPressed && !UIState.GameplayBlocked) StartWave();
        }

        // ---------- Эссенция ----------

        public bool CanAfford(int cost) => Essence >= cost;

        public bool TrySpend(int cost)
        {
            if (Essence < cost)
            {
                ShowMessage("Недостаточно эссенции");
                return false;
            }
            Essence -= cost;
            EssenceChanged?.Invoke();
            return true;
        }

        public void AddEssence(int amount)
        {
            if (amount <= 0) return;
            Essence += amount;
            EssenceChanged?.Invoke();
        }

        public void SpawnOrb(Vector3 position, int value)
        {
            if (orbPrefab == null || value <= 0) return;
            var orb = Instantiate(orbPrefab, position, Quaternion.identity);
            orb.Init(value, balance);
        }

        public void RegisterKill() => Kills++;

        // ---------- Фазы ----------

        /// <summary>Кнопка «Готов»: запустить следующую волну.</summary>
        public void StartWave()
        {
            if (Phase != GamePhase.Preparation) return;
            SetPhase(GamePhase.Wave);
            ShowMessage($"Волна {WaveIndex + 1} началась!");
            Spawner.Run(balance.waves[WaveIndex], OnWaveCleared);
        }

        private void OnWaveCleared()
        {
            if (Phase != GamePhase.Wave) return;
            var wave = balance.waves[WaveIndex];
            AddEssence(wave.completionBonus);
            ShowMessage($"Волна {WaveIndex + 1} пройдена! Бонус +{wave.completionBonus}");
            WaveIndex++;
            SetPhase(WaveIndex >= WaveCount ? GamePhase.Victory : GamePhase.Preparation);
        }

        private void OnPortalDestroyed()
        {
            if (Phase == GamePhase.Victory || Phase == GamePhase.Defeat) return;
            Spawner.StopAll();
            SetPhase(GamePhase.Defeat);
        }

        private void SetPhase(GamePhase phase)
        {
            Phase = phase;
            bool finished = phase == GamePhase.Victory || phase == GamePhase.Defeat;
            UIState.ResultOpen = finished;
            Time.timeScale = finished ? 0f : 1f;
            PhaseChanged?.Invoke();
        }

        public void ShowMessage(string text) => MessageShown?.Invoke(text);

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
