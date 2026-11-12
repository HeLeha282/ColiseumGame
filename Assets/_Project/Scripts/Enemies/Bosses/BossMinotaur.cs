namespace ColiseumGame
{
    /// <summary>Минотавр: броню снимает только игрок.</summary>
    public class BossMinotaur : BossBase
    {
        public override string Hint => enemy.Armor > 0f
            ? "Броня Минотавра держит удары башен. Бей его сам мечом или луком, пока броня не слетит!"
            : "Броня сбита! Теперь башни бьют в полную силу.";
    }
}
