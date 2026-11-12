using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColiseumGame
{
    public enum GamePhase
    {
        /// <summary>Выбор сложности перед началом.</summary>
        Setup,
        Preparation,
        Wave,
        Victory,
        Defeat
    }

    /// <summary>
    /// Главный цикл: сложность → [подготовка → волна → награда] × 30 → победа → (по желанию) бесконечный режим.
    /// Хранит эссенцию, номер волны, множители сложности. Волну можно вызвать досрочно (G во время волны) за бонус.
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
        public GamePhase Phase { get; private set; } = GamePhase.Setup;
        public int Essence { get; private set; }
        /// <summary>Индекс следующей волны (с нуля). Номер текущей/последней начатой волны = WaveIndex.</summary>
        public int WaveIndex { get; private set; }
        public int WaveCount => balance.waves.Length;
        public bool Endless { get; private set; }
        public int Kills { get; private set; }
        public int DifficultyIndex { get; private set; } = 1;
        public DifficultyPreset Difficulty => balance.difficulties.Length > 0 ? balance.difficulties[Mathf.Clamp(DifficultyIndex, 0, balance.difficulties.Length - 1)] : null;
        public float EnemyHpMultiplier => Difficulty?.enemyHp ?? 1f;
        public float EnemyDamageMultiplier => Difficulty?.enemyDamage ?? 1f;
        public float IncomeMultiplier => Difficulty?.income ?? 1f;
        public WaveSpawner Spawner { get; private set; }
        /// <summary>Номер волны для интерфейса: в подготовке — следующая, в бою — текущая.</summary>
        public int DisplayWave => Phase == GamePhase.Preparation || Phase == GamePhase.Setup ? WaveIndex + 1 : Mathf.Max(1, WaveIndex);
        public bool CanCallEarly => Phase == GamePhase.Wave && Spawner.AllSpawned && HasWave(WaveIndex) && pending.Count < 2;

        public event Action PhaseChanged;
        public event Action EssenceChanged;
        public event Action<string> MessageShown;

        private readonly List<(int bonus, int glory)> pending = new List<(int, int)>();
        private float portalHpAtWaveStart;
        private readonly Dictionary<int, WaveConfig> endlessCache = new Dictionary<int, WaveConfig>();

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            UIState.Reset();
            Enemy.ResetSeen();
            Spawner = GetComponent<WaveSpawner>();
            Essence = balance.startEssence;
            DifficultyIndex = balance.defaultDifficulty;
        }

        private void Start()
        {
            if (Portal.Instance != null) Portal.Instance.Destroyed += OnPortalDestroyed;
            SetPhase(balance.difficulties.Length > 0 ? GamePhase.Setup : GamePhase.Preparation);
        }

        private void Update()
        {
            var input = InputReader.Instance;
            if (input != null && input.ReadyPressed && !UIState.GameplayBlocked) StartWave();
        }

        // ---------------------------------------------------------------- сложность

        public void SelectDifficulty(int index)
        {
            if (Phase != GamePhase.Setup) return;
            DifficultyIndex = Mathf.Clamp(index, 0, balance.difficulties.Length - 1);
            ShowMessage($"Сложность: {Difficulty.name}");
            SetPhase(GamePhase.Preparation);
        }

        // ---------------------------------------------------------------- эссенция

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
            int v = Mathf.RoundToInt(value * IncomeMultiplier) + Tower.EssenceBonusAt(position);
            if (orbPrefab == null || v <= 0) return;
            var orb = Instantiate(orbPrefab, position, Quaternion.identity);
            orb.Init(v, balance);
        }

        public void RegisterKill(Enemy e) => Kills++;

        // ---------------------------------------------------------------- волны

        public bool HasWave(int index) => Endless || index < WaveCount;

        public WaveConfig GetWave(int index)
        {
            if (index < WaveCount) return balance.waves[index];
            if (!endlessCache.TryGetValue(index, out var w)) endlessCache[index] = w = GenerateEndless(index);
            return w;
        }

        /// <summary>Кнопка «Готов» (G). В подготовке — начать волну; в бою, когда всё вышло, — досрочный вызов.</summary>
        public void StartWave()
        {
            if (Phase == GamePhase.Preparation)
            {
                portalHpAtWaveStart = Portal.Instance != null ? Portal.Instance.Health.Current : 0f;
                SetPhase(GamePhase.Wave);
                LaunchNext();
            }
            else if (CanCallEarly)
            {
                var next = GetWave(WaveIndex);
                int early = Mathf.RoundToInt(next.completionBonus * balance.earlyCallBonus * IncomeMultiplier);
                AddEssence(early);
                ShowMessage($"Досрочный вызов! +{early} эссенции");
                LaunchNext();
            }
        }

        private void LaunchNext()
        {
            var wave = GetWave(WaveIndex);
            float hp = wave.hpMultiplier * (Endless && WaveIndex >= WaveCount ? 1f : 1f);
            pending.Add((wave.completionBonus, wave.gloryBonus));
            WaveIndex++;
            ShowMessage(string.IsNullOrEmpty(wave.title) ? $"Волна {WaveIndex} началась!" : $"Волна {WaveIndex}: {wave.title}");
            Spawner.Run(wave, hp, OnWavesCleared);
        }

        private void OnWavesCleared()
        {
            if (Phase != GamePhase.Wave) return;
            int bonus = 0, glory = 0;
            foreach (var p in pending) { bonus += p.bonus; glory += p.glory; }
            pending.Clear();
            var portal = Portal.Instance;
            bool clean = portal != null && portal.Health.Current >= portalHpAtWaveStart - 0.01f;
            if (clean) bonus += Mathf.RoundToInt(bonus * balance.cleanWaveBonus);
            bonus = Mathf.RoundToInt(bonus * IncomeMultiplier);
            AddEssence(bonus);
            PlayerProgression.Instance?.AddGlory(glory);
            if (portal != null) portal.Health.Heal(portal.Health.Max * balance.portalRegenPercent);
            ShowMessage($"Волна {WaveIndex} пройдена! +{bonus} эссенции" + (clean ? " (без урона порталу: +бонус)" : ""));
            if (!Endless && WaveIndex >= WaveCount) SetPhase(GamePhase.Victory);
            else SetPhase(GamePhase.Preparation);
        }

        /// <summary>После победы: «Хотите продолжить?» — бесконечный режим.</summary>
        public void ContinueEndless()
        {
            if (Phase != GamePhase.Victory) return;
            Endless = true;
            ShowMessage("Бесконечный режим: держитесь, пока стоит портал!");
            SetPhase(GamePhase.Preparation);
        }

        private WaveConfig GenerateEndless(int index)
        {
            int n = index + 1;
            var w = ScriptableObject.CreateInstance<WaveConfig>();
            var pool = new List<WaveGenerator.Entry>();
            foreach (var e in balance.endlessPool) pool.Add(new WaveGenerator.Entry { enemy = e, cost = CostOf(e), unlockWave = 0, squad = e.hasShield });
            var rng = new System.Random(n * 7919);
            var groups = WaveGenerator.Compose(n, WaveGenerator.Budget(n), pool, rng, 3);
            if (n % 5 == 0 && balance.bossPool.Length > 0)
            {
                groups.Insert(0, new SpawnGroup { enemy = balance.bossPool[rng.Next(balance.bossPool.Length)], count = 1 + (n >= 45 ? 1 : 0), startDelay = 3f, interval = 15f });
                w.title = "Босс бесконечности";
            }
            w.groups = groups.ToArray();
            w.hpMultiplier = WaveGenerator.HpMultiplier(WaveCount) * (1f + balance.endlessHpGrowth * (n - WaveCount));
            w.completionBonus = 80 + 4 * (n - WaveCount);
            w.gloryBonus = 30;
            w.act = 3;
            return w;
        }

        /// <summary>«Стоимость угрозы» врага для генератора волн.</summary>
        public static float CostOf(EnemyConfig e)
        {
            float c = (e.maxHP + e.armor) / 14f;
            if (e.isFlying) c *= 1.3f;
            if (e.hasShield) c *= 1.4f;
            if (e.attackRange > 4f || e.healRadius > 0f || e.invisible) c *= 1.5f;
            if (e.IsBoss) c *= 0.5f;
            return Mathf.Max(1f, c);
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
            UIState.ResultOpen = finished || phase == GamePhase.Setup;
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
