using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Кто нанёс урон. От этого зависят метка, щит и броня врагов.</summary>
    public enum DamageSource
    {
        Player,
        Tower,
        Enemy,
        /// <summary>Урон от реакции стихий, горения, зон.</summary>
        Effect
    }

    /// <summary>Описание одного удара.</summary>
    public struct DamageInfo
    {
        public float Amount;
        public DamageSource Source;
        /// <summary>Откуда пришёл удар (позиция игрока/башни/врага). Нужна для щита.</summary>
        public Vector3 Origin;
        /// <summary>Стихия удара: накладывается на врага и может вызвать реакцию.</summary>
        public Element Element;
        /// <summary>Игнорировать щит и броню (ультимейт «Разлом Пустоты»).</summary>
        public bool Piercing;
        /// <summary>Башня, нанёсшая удар (для усилений Святилища), может быть null.</summary>
        public Tower SourceTower;

        public DamageInfo(float amount, DamageSource source, Vector3 origin, Element element = Element.None)
        {
            Amount = amount;
            Source = source;
            Origin = origin;
            Element = element;
            Piercing = false;
            SourceTower = null;
        }
    }

    /// <summary>То, что можно бить: враги, баррикады, портал, игрок.</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(DamageInfo info);
    }
}
