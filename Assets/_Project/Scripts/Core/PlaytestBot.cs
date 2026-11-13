#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ColiseumGame
{
    /// <summary>
    /// ТОЛЬКО ДЛЯ ТЕСТОВ В РЕДАКТОРЕ. «Игрок-бот» для автоматических прогонов через MCP.
    /// Бой — теми же клавишами, что и человек (W/Shift/ЛКМ/1/2/Q/R/X/E — через очередь событий Input System),
    /// поворот — напрямую. Стройка/прокачка — напрямую через TowerPlacer.TryPlaceAt и меню башен (FillMenu).
    /// Выбирает сложность, карточки героя, жмёт E у боссов, сам запускает волны, после победы — бесконечный режим.
    /// Добавляется вручную: gameObject.AddComponent&lt;PlaytestBot&gt;() на игрока в Play Mode.
    /// Отчёт по волнам — PlaytestBot.Report и строки «BOT_WAVE» в консоли.
    /// </summary>
    public class PlaytestBot : MonoBehaviour
    {
        public float defendRadius = 18f;
        public float meleeDistance = 2.2f;
        /// <summary>Ускорение времени (кроме катсцен и паузы).</summary>
        public float speed = 1f;
        public int difficulty = 1;
        /// <summary>Сколько волн бесконечного режима пройти после победы (0 — остановиться).</summary>
        public int endlessWaves = 0;
        /// <summary>Сам ставить и качать башни.</summary>
        public bool autoBuild = true;
        /// <summary>Сам нажимать «Готов».</summary>
        public bool autoStart = true;
        /// <summary>Только строить — не драться (проверка «башни без героя»).</summary>
        public bool passive;

        public static readonly StringBuilder Report = new StringBuilder();

        private PlayerController player;
        private PlayerCombat combat;
        private PlayerCameraController cam;
        private float clickTimer;
        private bool mouseDown;
        private float prepTimer;
        private float abilityTimer;
        private float waveBuildTimer;
        private int typeCursor;
        private int lastWaveLogged = -1;
        private float waveStartTime;
        private float portalAtStart;
        private int endlessDone;
        private List<Vector3> spots;
        private readonly List<Key> keys = new List<Key>();

        private void Start()
        {
            player = GetComponent<PlayerController>();
            combat = GetComponent<PlayerCombat>();
            cam = GetComponent<PlayerCameraController>();
            cam.SetFirstPerson(true);
            Report.Clear();
        }

        private void OnDisable()
        {
            keys.Clear();
            Send(false);
        }

        private void Update()
        {
            keys.Clear();
            bool mouse = false;
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (gm.Phase == GamePhase.Setup) gm.SelectDifficulty(difficulty);
            if (gm.Phase == GamePhase.Victory && endlessDone < endlessWaves)
            {
                LogWave(gm);
                gm.ContinueEndless();
            }
            if (gm.Phase != GamePhase.Victory && gm.Phase != GamePhase.Defeat && !Cutscene.Playing && speed > 1f)
                Time.timeScale = speed;

            var prog = PlayerProgression.Instance;
            if (prog != null && prog.PendingChoices > 0 && prog.Offer.Count > 0) ChoosePerk(prog);

            if (gm.Phase == GamePhase.Preparation) Prepare(gm);
            else if (gm.Phase == GamePhase.Wave)
            {
                TrackWave(gm);
                // Стройка и прокачка теперь доступны и в бою — бот тратит эссенцию раз в пару секунд
                if (autoBuild && (waveBuildTimer -= Time.deltaTime) <= 0f)
                {
                    waveBuildTimer = 2f;
                    Build(gm);
                }
            }
            else LogWave(gm);

            if (!passive && player.IsAlive && gm.Phase == GamePhase.Wave && !UIState.GameplayBlocked)
                Think(ref mouse);
            Send(mouse);
        }

        // ---------------------------------------------------------------- подготовка

        private void Prepare(GameManager gm)
        {
            LogWave(gm);
            if (combat.Weapon == WeaponType.Hammer) keys.Add(Key.Digit1);
            prepTimer += Time.unscaledDeltaTime;
            if (prepTimer < 0.5f) return;
            if (autoBuild) Build(gm);
            if (autoStart && prepTimer > 1.5f)
            {
                prepTimer = 0f;
                gm.StartWave();
            }
        }

        private void Build(GameManager gm)
        {
            var types = gm.Balance.towerTypes;
            var prog = PlayerProgression.Instance;
            int limit = prog != null ? prog.TowerLimit : 4;
            // Новая башня, если есть место в лимите.
            if (Tower.All.Count < limit)
            {
                var cfg = types[typeCursor % types.Length];
                if (!gm.CanAfford(cfg.levels[0].cost)) return;   // копим на башню
                // Места ищем, только когда на башню хватает (иначе правила отбросят все точки), и пересчитываем пустой список
                if (spots == null || spots.Count == 0) spots = FindSpots(cfg);
                bool placed = false;
                foreach (var p in spots)
                    if (TowerPlacer.Instance.TryPlaceAt(cfg, p)) { typeCursor++; placed = true; break; }
                if (placed) return;
                // Свободных мест не осталось — дальше тратим на прокачку.
            }

            // Зачарование под босса следующей волны (как сделал бы человек, прочитав превью).
            if (prog != null && gm.HasWave(gm.WaveIndex))
            {
                Element want = prog.Enchant;
                foreach (var g in gm.GetWave(gm.WaveIndex).groups)
                    if (g.enemy != null)
                        switch (g.enemy.bossKind)
                        {
                            case BossKind.Cerberus: want = Element.None; break;   // грозовую голову бьют без стихии
                            case BossKind.Hydra: want = Element.Fire; break;
                            case BossKind.Titan: want = Element.Ice; break;
                        }
                if (want != prog.Enchant && want != Element.None) prog.SetEnchant(want);
            }

            // Оружие героя — иногда.
            if (prog != null && !passive && prog.SwordLevel < 5 && gm.Essence > prog.UpgradeCost(WeaponType.Sword) * 3)
                prog.UpgradeWeapon(WeaponType.Sword);

            // Прокачка: самая дешёвая доступная опция у самой слабой башни.
            Tower weakest = null;
            foreach (var t in Tower.All) if (weakest == null || t.TotalInvested < weakest.TotalInvested) weakest = t;
            if (weakest == null) return;
            var opts = new List<MenuOption>();
            ((IInteractable)weakest).FillMenu(opts);
            var choices = opts.FindAll(o => o.Interactable && !o.Label.StartsWith("Продать"));
            if (choices.Count > 0) choices[Random.Range(0, choices.Count)].OnSelect();
        }

        /// <summary>Места для башен: допустимые точки, отсортированные по тому, сколько маршрута в радиусе 12 м.</summary>
        private static List<Vector3> FindSpots(TowerConfig cfg)
        {
            var routes = FindObjectsOfType<Route>();
            var scored = new List<(Vector3 p, int score)>();
            for (float r = 8f; r < 45f; r += 1.5f)
                for (int a = 0; a < 360; a += 8)
                {
                    float y = r < 30f ? 0f : r < 38f ? 3f : 6f;
                    var p = new Vector3(r * Mathf.Cos(a * Mathf.Deg2Rad), y + 0.01f, r * Mathf.Sin(a * Mathf.Deg2Rad));
                    if (!PlacementRules.Check(cfg, p, Vector3.up, p + new Vector3(1000f, 0f, 0f), out _, false)) continue;
                    int score = 0;
                    foreach (var route in routes)
                        for (int i = 0; i < route.Count; i++)
                            if (Vector3.Distance(route.GetPoint(i), p) < 12f) score++;
                    // Арена у портала важнее: сюда сходятся все маршруты и долетают летуны.
                    scored.Add((p, score + (r < 14f ? 25 : r < 20f ? 10 : 0)));
                }
            scored.Sort((x, y) => y.score.CompareTo(x.score));
            return scored.ConvertAll(s => s.p);
        }

        private void ChoosePerk(PlayerProgression prog)
        {
            // Первые уровни — лимит башен, дальше случайно (лимит — с шансом 50%).
            HeroPerk pick = prog.Offer[Random.Range(0, prog.Offer.Count)];
            foreach (var p in prog.Offer)
                if (p.kind == PerkKind.TowerLimit && (prog.TowerLimit < 6 || Random.value < 0.5f)) pick = p;
            prog.Choose(pick);
            Report.AppendLine($"  перк: {pick.name} (ур.героя {prog.Level})");
        }

        // ---------------------------------------------------------------- отчёт

        private void TrackWave(GameManager gm)
        {
            if (lastWaveLogged != gm.WaveIndex && waveStartTime <= 0f)
            {
                waveStartTime = Time.time;
                portalAtStart = Portal.Instance != null ? Portal.Instance.Health.Current : 0f;
            }
        }

        private void LogWave(GameManager gm)
        {
            if (waveStartTime <= 0f || lastWaveLogged == gm.WaveIndex) return;
            lastWaveLogged = gm.WaveIndex;
            if (gm.Endless && gm.WaveIndex > gm.WaveCount) endlessDone++;
            float portal = Portal.Instance != null ? Portal.Instance.Health.Current : 0f;
            string line = $"BOT_WAVE {gm.WaveIndex} {gm.Phase}: время {Time.time - waveStartTime:0}с, портал {portalAtStart:0}→{portal:0}, " +
                          $"эссенция {gm.Essence}, башен {Tower.All.Count}, герой ур.{PlayerProgression.Instance?.Level}, убито {gm.Kills}";
            Report.AppendLine(line);
            Debug.Log(line);
            waveStartTime = 0f;
        }

        // ---------------------------------------------------------------- бой

        private void Think(ref bool mouse)
        {
            Vector3 portal = Portal.Instance != null ? Portal.Instance.transform.position : Vector3.zero;

            // Босс с доступным действием E — идём к нему и жмём.
            foreach (var b in BossBase.All)
            {
                if (b == null || b.InteractPrompt == null || !b.Enemy.IsAlive || b.Enemy.IsFlying && b.Enemy.transform.position.y > 4f) continue;
                if (b.CanInteract(transform.position))
                {
                    if (Time.frameCount % 2 == 0) keys.Add(Key.E);   // «нажатие», а не удержание
                    return;
                }
                MoveTo(b.transform.position, keys);
                return;
            }

            Enemy target = PickTarget(portal);
            if (target == null)
            {
                Vector3 goal = portal + new Vector3(0f, 0f, -6f);
                if (Flat(goal - transform.position).magnitude > 1.5f) MoveTo(goal, keys);
                return;
            }

            // Навыки и ультимейт — когда рядом враги.
            abilityTimer -= Time.deltaTime;
            if (abilityTimer <= 0f && Vector3.Distance(target.transform.position, transform.position) < 8f)
            {
                abilityTimer = 1.5f;
                var prog = PlayerProgression.Instance;
                if (prog != null && prog.UltimateReady) keys.Add(Key.X);
                else keys.Add(Random.value < 0.5f ? Key.Q : Key.R);
            }

            if (target.IsFlying)
            {
                if (combat.Weapon != WeaponType.Bow) keys.Add(Key.Digit2);
                Aim(target.AimPoint);
                if (Flat(target.transform.position - transform.position).magnitude > 12f) { MoveTo(target.transform.position, keys); Aim(target.AimPoint); }
                if (!combat.IsCharging || combat.Charge01 < 0.8f) mouse = true;
                return;
            }

            if (combat.Weapon != WeaponType.Sword) keys.Add(Key.Digit1);
            Aim(target.AimPoint);
            float dist = Flat(target.transform.position - transform.position).magnitude;
            if (dist > meleeDistance + target.Radius)
            {
                keys.Add(Key.W);
                keys.Add(Key.LeftShift);
            }
            else
            {
                clickTimer -= Time.deltaTime;
                if (clickTimer <= 0f)
                {
                    clickTimer = 0.25f;
                    mouseDown = !mouseDown;
                }
                mouse = mouseDown;
            }
        }

        private Enemy PickTarget(Vector3 portal)
        {
            Enemy best = null;
            float bestScore = float.MaxValue;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.IsAlive) continue;
                float toPortal = Flat(e.transform.position - portal).magnitude;
                float toMe = Vector3.Distance(e.transform.position, transform.position);
                if (toPortal > defendRadius && toMe > 6f) continue; // далеко — пусть работают башни
                float score = toMe + toPortal * 0.5f;
                if (e.Armor > 0f) score -= 30f;            // броню снимает только игрок
                if (e.Config.hasShield) score -= 8f;       // щитоносцы — тоже наша работа
                if (e.IsFlying) score += 10f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = e;
                }
            }
            return best;
        }


        private void MoveTo(Vector3 goal, List<Key> k)
        {
            Aim(goal + Vector3.up * 1.6f);
            k.Add(Key.W);
            k.Add(Key.LeftShift);
        }

        private void Aim(Vector3 point)
        {
            Vector3 eye = transform.position + Vector3.up * 1.65f;
            Vector3 d = point - eye;
            Vector3 flat = Flat(d);
            if (flat.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(flat);
            float pitch = -Mathf.Atan2(d.y, flat.magnitude) * Mathf.Rad2Deg;
            cam.SetPitch(pitch);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private void Send(bool mouse)
        {
            if (Keyboard.current != null)
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys.ToArray()));
            if (Mouse.current != null)
                InputSystem.QueueStateEvent(Mouse.current, new MouseState { buttons = (ushort)(mouse ? 1 : 0) });
        }
    }
}
#endif
