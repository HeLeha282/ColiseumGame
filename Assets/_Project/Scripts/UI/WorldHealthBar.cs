using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Полоска HP над головой (world-space Canvas). Всегда повёрнута к камере.
    /// Может показывать броню (босс) и значок метки игрока.
    /// </summary>
    public class WorldHealthBar : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;
        [SerializeField] private RectTransform armorFill;
        [SerializeField] private GameObject armorRoot;
        [SerializeField] private GameObject markIcon;

        private Camera cam;

        public void SetHealth(float fraction)
        {
            if (fill != null) fill.localScale = new Vector3(Mathf.Clamp01(fraction), 1f, 1f);
        }

        public void SetArmor(float fraction)
        {
            if (armorRoot != null) armorRoot.SetActive(fraction > 0f);
            if (armorFill != null) armorFill.localScale = new Vector3(Mathf.Clamp01(fraction), 1f, 1f);
        }

        public void SetMarked(bool marked)
        {
            if (markIcon != null && markIcon.activeSelf != marked) markIcon.SetActive(marked);
        }

        private void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}
