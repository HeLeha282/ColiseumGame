namespace ColiseumGame
{
    /// <summary>
    /// Общие флаги интерфейса: открыто ли меню/экран результата, свободен ли курсор.
    /// Игровые скрипты смотрят сюда, чтобы не стрелять и не крутить камеру, пока открыто меню.
    /// </summary>
    public static class UIState
    {
        public static bool MenuOpen;
        public static bool ResultOpen;
        public static bool FreeCursorHeld;

        public static bool CursorFree => MenuOpen || ResultOpen || FreeCursorHeld;
        public static bool GameplayBlocked => MenuOpen || ResultOpen;

        public static void Reset()
        {
            MenuOpen = false;
            ResultOpen = false;
            FreeCursorHeld = false;
        }
    }
}
