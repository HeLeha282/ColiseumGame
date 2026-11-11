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
                GameManager.Instance.EssenceChanged += Refresh;
                GameManager.Instance.PhaseChanged += Close;
            }
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
            if (!IsOpen || target == null) return;
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
