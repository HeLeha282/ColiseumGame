using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColiseumGame
{
    /// <summary>Создание простых элементов uGUI из кода (панели, тексты, кнопки, полоски).</summary>
    public static class UIFactory
    {
        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static Image Panel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            var rt = Rect(name, parent, anchor, pivot, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string text, float size, TextAlignmentOptions align, Color color)
        {
            var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(rt);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.enableWordWrapping = true;
            return t;
        }

        public static Button Button(string name, Transform parent, string label, Color color, float fontSize, System.Action onClick)
        {
            var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 50f));
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            var b = rt.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            b.colors = colors;
            var t = Text("Label", rt, label, fontSize, TextAlignmentOptions.Center, Color.white);
            t.margin = new Vector4(8f, 4f, 8f, 4f);
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }

        /// <summary>Полоска: фон + заливка слева направо. Возвращает заливку.</summary>
        public static RectTransform Bar(Transform parent, Color back, Color fill)
        {
            var bg = new GameObject("Back", typeof(RectTransform)).GetComponent<RectTransform>();
            bg.SetParent(parent, false);
            Stretch(bg);
            bg.gameObject.AddComponent<Image>().color = back;
            var f = new GameObject("Fill", typeof(RectTransform)).GetComponent<RectTransform>();
            f.SetParent(parent, false);
            Stretch(f);
            f.pivot = new Vector2(0f, 0.5f);
            f.gameObject.AddComponent<Image>().color = fill;
            return f;
        }

        public static VerticalLayoutGroup VLayout(GameObject go, int padding, float spacing)
        {
            var l = go.AddComponent<VerticalLayoutGroup>();
            l.padding = new RectOffset(padding, padding, padding, padding);
            l.spacing = spacing;
            l.childControlHeight = true;
            l.childControlWidth = true;
            l.childForceExpandHeight = false;
            l.childForceExpandWidth = true;
            return l;
        }

        public static LayoutElement Height(Component c, float h)
        {
            if (!c.TryGetComponent(out LayoutElement le)) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = h;
            return le;
        }
    }
}
