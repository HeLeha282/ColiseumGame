using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Стихии игры. Ими оперируют башни, оружие игрока, навыки и враги.</summary>
    public enum Element
    {
        None,
        Fire,
        Ice,
        Lightning,
        Void
    }

    /// <summary>Реакции при встрече двух стихий на одном враге.</summary>
    public enum Reaction
    {
        None,
        /// <summary>Огонь + Лёд: большой разовый урон, снимает заморозку.</summary>
        Shatter,
        /// <summary>Лёд + Молния: враг получает больше урона, щит не держит.</summary>
        Brittle,
        /// <summary>Огонь + Молния: взрыв по области с отбрасыванием.</summary>
        Overload,
        /// <summary>Любая + Пустота: урон в % от максимального HP.</summary>
        Decay
    }

    public static class Elements
    {
        public static Reaction Resolve(Element a, Element b)
        {
            if (a == Element.None || b == Element.None || a == b) return Reaction.None;
            if (a == Element.Void || b == Element.Void) return Reaction.Decay;
            if (Is(a, b, Element.Fire, Element.Ice)) return Reaction.Shatter;
            if (Is(a, b, Element.Ice, Element.Lightning)) return Reaction.Brittle;
            if (Is(a, b, Element.Fire, Element.Lightning)) return Reaction.Overload;
            return Reaction.None;
        }

        private static bool Is(Element a, Element b, Element x, Element y) => (a == x && b == y) || (a == y && b == x);

        public static Element Opposite(Element e) => e switch
        {
            Element.Fire => Element.Ice,
            Element.Ice => Element.Fire,
            _ => Element.None
        };

        public static Color Color(Element e) => e switch
        {
            Element.Fire => new Color(1f, 0.42f, 0.1f),
            Element.Ice => new Color(0.45f, 0.85f, 1f),
            Element.Lightning => new Color(0.75f, 0.6f, 1f),
            Element.Void => new Color(0.6f, 0.15f, 1f),
            _ => new Color(0.9f, 0.9f, 0.9f)
        };

        public static string Name(Element e) => e switch
        {
            Element.Fire => "Огонь",
            Element.Ice => "Лёд",
            Element.Lightning => "Молния",
            Element.Void => "Пустота",
            _ => "Без стихии"
        };

        public static string Name(Reaction r) => r switch
        {
            Reaction.Shatter => "РАСКОЛ",
            Reaction.Brittle => "ХРУПКОСТЬ",
            Reaction.Overload => "ПЕРЕГРУЗКА",
            Reaction.Decay => "РАСПАД",
            _ => ""
        };
    }
}
