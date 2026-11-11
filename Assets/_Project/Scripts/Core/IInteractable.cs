using System;
using System.Collections.Generic;

namespace ColiseumGame
{
    /// <summary>Пункт меню постройки/прокачки.</summary>
    public class MenuOption
    {
        public string Label;
        public bool Interactable;
        public Action OnSelect;

        public MenuOption(string label, bool interactable, Action onSelect)
        {
            Label = label;
            Interactable = interactable;
            OnSelect = onSelect;
        }
    }

    /// <summary>То, на что можно посмотреть и нажать F: слот башни, место для баррикады.</summary>
    public interface IInteractable
    {
        string Title { get; }
        /// <summary>Подсказка внизу экрана, пока игрок смотрит на объект.</summary>
        string Prompt { get; }
        /// <summary>Вторая строка в меню (состояние, HP, причина недоступности).</summary>
        string Description { get; }
        void FillMenu(List<MenuOption> options);
    }
}
