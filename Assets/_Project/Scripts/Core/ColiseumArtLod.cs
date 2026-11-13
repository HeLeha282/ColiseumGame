using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Две версии модели Колизея: полная (с фасками, ~1.2 млн треугольников) и облегчённая (без фасок, ~0.44 млн).
    /// Полная всегда остаётся включённой ради коллайдеров камеры — у неё прячутся только рендереры.
    /// Переключает GraphicsQuality.
    /// </summary>
    public class ColiseumArtLod : MonoBehaviour
    {
        [SerializeField] private GameObject high;
        [SerializeField] private GameObject low;

        private static readonly string[] Foliage = { "Col_Leaf", "Col_LeafLight", "Col_Bark", "Col_Moss" };

        public static ColiseumArtLod Instance { get; private set; }

        private void Awake() => Instance = this;

        /// <param name="useHigh">полная модель (иначе облегчённая)</param>
        /// <param name="foliage">деревья, кусты и мох (выключаются на «Низкой»)</param>
        public void Apply(bool useHigh, bool foliage)
        {
            if (high != null)
                foreach (var r in high.GetComponentsInChildren<MeshRenderer>(true))
                    r.enabled = useHigh && (foliage || !IsFoliage(r.name));
            if (low != null)
            {
                low.SetActive(!useHigh);
                foreach (var r in low.GetComponentsInChildren<MeshRenderer>(true))
                    r.enabled = foliage || !IsFoliage(r.name);
            }
        }

        private static bool IsFoliage(string name)
        {
            foreach (var f in Foliage)
                if (name == f || name.StartsWith(f + "_S")) return true;
            return false;
        }
    }
}
