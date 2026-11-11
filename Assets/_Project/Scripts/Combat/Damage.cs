using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Кто нанёс урон. От этого зависят метка, щит и броня врагов.</summary>
    public enum DamageSource
    {
        Player,
        Tower,
        Enemy
    }

    /// <summary>Описание одного удара.</summary>
    public struct DamageInfo
    {
        public float Amount;
        public DamageSource Source;
        /// <summary>Откуда пришёл удар (позиция игрока/башни/врага). Нужна для щита.</summary>
        public Vector3 Origin;

        public DamageInfo(float amount, DamageSource source, Vector3 origin)
        {
            Amount = amount;
            Source = source;
            Origin = origin;
        }
    }

    /// <summary>Всё, что можно бить: враги, башни, баррикады, портал, игрок.</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(DamageInfo info);
    }
}
