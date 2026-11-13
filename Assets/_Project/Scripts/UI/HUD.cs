using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColiseumGame
{
    /// <summary>
    /// Экранный интерфейс: HP игрока и портала, эссенция, волна и фаза, кнопка «Готов»,
    /// прицел и натяжение лука, подсказки, сообщения, экран результата.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        [Header("Полоски")]
        [SerializeField] private RectTransform playerHpFill;
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private RectTransform portalHpFill;
        [SerializeField] private TMP_Text portalHpText;

        [Header("Тексты")]
        [SerializeField] private TMP_Text essenceText;
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private TMP_Text weaponText;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text centerText;

        [Header("Прицел")]
        [SerializeField] private GameObject crosshair;
        [SerializeField] private GameObject chargeBar;
        [SerializeField] private RectTransform chargeFill;

        [Header("Кнопки и экраны")]
        [SerializeField] private Button readyButton;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultTitle;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button restartButton;

        [SerializeField] private float messageDuration = 2.5f;

        private float messageTimer;
        private GameManager gm;

        private void Start()
        {
            gm = GameManager.Instance;
            readyButton.onClick.AddListener(() => gm.StartWave());
            restartButton.onClick.AddListener(() => gm.Restart());
            gm.MessageShown += ShowMessage;
            gm.PhaseChanged += OnPhaseChanged;
            messageText.text = "";
            OnPhaseChanged();
        }

        private void OnDestroy()
        {
            if (gm != null)
            {
                gm.MessageShown -= ShowMessage;
                gm.PhaseChanged -= OnPhaseChanged;
            }
        }

        private void ShowMessage(string text)
        {
            messageText.text = text;
            messageTimer = messageDuration;
        }

        private void OnPhaseChanged()
        {
            
            bool finished = gm.Phase == GamePhase.Victory || gm.Phase == GamePhase.Defeat;
            resultPanel.SetActive(finished);
            if (finished)
            {
                bool win = gm.Phase == GamePhase.Victory;
                resultTitle.text = win ? "ПОБЕДА!" : "ПОРАЖЕНИЕ";
                resultTitle.color = win ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.4f, 0.35f);
                int wavesDone = Mathf.Max(0, gm.WaveIndex - 1);
                resultText.text = win
                    ? $"Портал выстоял все {gm.WaveCount} волн.\nУбито врагов: {gm.Kills}"
                    : gm.Endless
                        ? $"Бесконечный режим: портал пал на волне {gm.WaveIndex}.\nУбито врагов: {gm.Kills}"
                        : $"Портал разрушен на волне {gm.WaveIndex}.\nПройдено волн: {wavesDone}. Убито врагов: {gm.Kills}";
            }
        }

        private void Update()
        {
            if (gm == null) return;

            // Курсор: свободен в меню, на экране результата и пока зажат Alt.
            var input = InputReader.Instance;
            UIState.FreeCursorHeld = input != null && input.FreeCursorHeld;
            Cursor.lockState = UIState.CursorFree ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = UIState.CursorFree;

            var player = PlayerController.Instance;
            if (player != null)
            {
                SetFill(playerHpFill, player.Health.Fraction);
                playerHpText.text = $"Игрок  {player.Health.Current:0}/{player.Health.Max:0}";
                centerText.text = player.IsAlive ? "" : $"Вы погибли. Возрождение через {Mathf.CeilToInt(player.RespawnTimeLeft)}…";
            }

            var portal = Portal.Instance;
            if (portal != null)
            {
                SetFill(portalHpFill, portal.Health.Fraction);
                portalHpText.text = $"Портал  {portal.Health.Current:0}/{portal.Health.Max:0}";
            }

            essenceText.text = $"Эссенция: {gm.Essence}";

            string phase = gm.Phase switch
            {
                GamePhase.Setup => "Выбор сложности",
                GamePhase.Preparation => "Подготовка — 3: молоток, Tab: герой, G: начать волну",
                GamePhase.Wave => $"Бой — врагов: {gm.Spawner.Alive + gm.Spawner.RemainingToSpawn}" + (gm.CanCallEarly ? "  ·  <color=#FFD36B>G — следующая волна досрочно (+бонус)</color>" : "  ·  3: молоток, Tab: герой"),
                GamePhase.Victory => "Победа",
                _ => "Поражение"
            };
            string total = gm.Endless ? "∞" : gm.WaveCount.ToString();
            waveText.text = $"Волна {gm.DisplayWave}/{total}\n<size=70%>{phase}</size>";
            bool showReady = gm.Phase == GamePhase.Preparation || gm.CanCallEarly;
            if (readyButton.gameObject.activeSelf != showReady) readyButton.gameObject.SetActive(showReady);

            var combat = player != null ? player.GetComponent<PlayerCombat>() : null;
            if (combat != null)
            {
                weaponText.text = combat.Weapon switch { WeaponType.Sword => "Оружие: меч [1]", WeaponType.Bow => "Оружие: лук [2]", _ => "Молоток [3]" };
                chargeBar.SetActive(combat.IsCharging);
                SetFill(chargeFill, combat.Charge01);
            }
            crosshair.SetActive(!UIState.MenuOpen && !UIState.ResultOpen);

            var interactor = player != null ? player.GetComponent<PlayerInteractor>() : null;
            if (interactor != null && interactor.UsePrompt != null && !UIState.MenuOpen)
                promptText.text = $"<color=#FFD36B>{interactor.UsePrompt}</color>";
            else if (interactor != null && interactor.Current != null && !UIState.MenuOpen)
                promptText.text = interactor.Current.Prompt;
            else
                promptText.text = "";

            if (messageTimer > 0f)
            {
                messageTimer -= Time.unscaledDeltaTime;
                if (messageTimer <= 0f) messageText.text = "";
            }
        }

        private static void SetFill(RectTransform fill, float fraction)
        {
            if (fill != null) fill.localScale = new Vector3(Mathf.Clamp01(fraction), 1f, 1f);
        }
    }
}
