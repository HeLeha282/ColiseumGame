using System;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Запас здоровья. Сам ничего не знает о том, кто его бьёт: модификаторы урона
    /// (щит, метка, броня) считают владельцы — Enemy, Tower и т.д.
    /// </summary>
    public class Health : MonoBehaviour
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public bool IsAlive => Current > 0f;
        public float Fraction => Max > 0f ? Current / Max : 0f;

        public event Action Changed;
        public event Action Died;

        /// <summary>Задать максимум и вылечить полностью.</summary>
        public void Init(float max)
        {
            Max = Mathf.Max(1f, max);
            Current = Max;
            Changed?.Invoke();
        }

        /// <summary>Поменять максимум (прокачка), сохранив долю здоровья.</summary>
        public void SetMaxKeepFraction(float max)
        {
            float fraction = Fraction;
            Max = Mathf.Max(1f, max);
            Current = Mathf.Max(1f, Max * fraction);
            Changed?.Invoke();
        }

        /// <summary>Нанести урон. Возвращает, сколько реально снято.</summary>
        public float Damage(float amount)
        {
            if (!IsAlive || amount <= 0f) return 0f;
            float dealt = Mathf.Min(amount, Current);
            Current -= dealt;
            Changed?.Invoke();
            if (Current <= 0f)
            {
                Current = 0f;
                Died?.Invoke();
            }
            return dealt;
        }

        /// <summary>Вылечить. Возвращает, сколько реально вылечено.</summary>
        public float Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return 0f;
            float healed = Mathf.Min(amount, Max - Current);
            Current += healed;
            if (healed > 0f) Changed?.Invoke();
            return healed;
        }
    }
}
