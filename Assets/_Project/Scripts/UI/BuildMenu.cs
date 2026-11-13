using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColiseumGame
{
    /// <summary>
    /// Меню постройки/прокачки (открывается на F, когда смотришь на слот или место баррикады).
    /// Кнопки создаются из шаблона под каждый пункт.
    /// </summary>
    public class BuildMenu : MonoBehaviour
    {
        public static BuildMenu Instance { get; private set; }

        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private Button buttonTemplate;
        [SerializeField] private Button closeButton;

        private IInteractable target;
        private readonly List<MenuOption> options = new List<MenuOption>();
        private readonly List<Button> spawned = new List<Button>();

        public bool IsOpen => panel.activeSelf;

        private void Awake()
        {
            Instance = this;
            buttonTemplate.gameObject.SetActive(false);
            closeButton.onClick.AddListener(Close);
            panel.SetActive(false);
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                // В бою эссенция меняется постоянно — помечаем «нужно обновить», а обновляем в Update
                GameManager.Instance.EssenceChanged += () => dirty = true;
                GameManager.Instance.PhaseChanged += OnPhaseChanged;
            }
        }

        private bool dirty;
        private float nextRefresh;

        // Башни можно качать и в подготовке, и в бою — меню закрываем только в конце игры
        private void OnPhaseChanged()
        {
            var gm = GameManager.Instance;
            if (gm != null && !gm.CanManage) Close();
            else dirty = true;
        }

        private void Update()
        {
            if (!dirty || !IsOpen) return;
            // Не перестраиваем кнопки посреди клика и не чаще раза в 0.3 с
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if ((mouse != null && mouse.leftButton.isPressed) || Time.unscaledTime < nextRefresh) return;
            Refresh();
        }

        public void Open(IInteractable interactable)
        {
            target = interactable;
            panel.SetActive(true);
            UIState.MenuOpen = true;
            Refresh();
        }

        public void Close()
        {
            target = null;
            if (panel != null) panel.SetActive(false);
            UIState.MenuOpen = false;
        }

        private void Refresh()
        {
            dirty = false;
            nextRefresh = Time.unscaledTime + 0.3f;
            if (!IsOpen || target == null) return;
            // Башню продали (объект уничтожен) — закрываем меню
            if (target is Object unityObject && unityObject == null) { Close(); return; }
            titleText.text = target.Title;
            descriptionText.text = target.Description;

            options.Clear();
            target.FillMenu(options);

            foreach (var b in spawned) Destroy(b.gameObject);
            spawned.Clear();

            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                var button = Instantiate(buttonTemplate, buttonContainer);
                button.gameObject.SetActive(true);
                button.GetComponentInChildren<TMP_Text>().text = option.Label;
                button.interactable = option.Interactable;
                button.onClick.AddListener(() =>
                {
                    option.OnSelect?.Invoke();
                    Refresh();
                });
                spawned.Add(button);
            }

            if (options.Count == 0) descriptionText.text += "\nДействий нет.";
        }
    }
}
