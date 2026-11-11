using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ColiseumGame.EditorTools
{
    /// <summary>Мелкие помощники генератора: кубы, материалы, ассеты, элементы UI.</summary>
    public static class BuilderUtil
    {
        // ---------- Геометрия ----------

        public static Vector3 Polar(float radius, float degrees, float y)
        {
            float a = degrees * Mathf.Deg2Rad;
            return new Vector3(radius * Mathf.Cos(a), y, radius * Mathf.Sin(a));
        }

        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale,
            Material mat, bool keepCollider = false, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = rotation ?? Quaternion.identity;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat,
            bool collider = false, Quaternion? rotation = null)
            => Prim(PrimitiveType.Cube, name, parent, localPos, size, mat, collider, rotation);

        public static Transform Empty(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        // ---------- Ассеты ----------

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Материал URP/Lit. Пересоздаётся только если его нет.</summary>
        public static Material Material(string folder, string name, Color color, float emission = 0f, float smoothness = 0.2f)
        {
            string path = $"{folder}/M_{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            if (emission > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * emission);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>
        /// Загрузить ScriptableObject или создать с начальными значениями.
        /// Существующие ассеты НЕ перезаписываются — баланс, поправленный руками, сохраняется.
        /// </summary>
        public static T LoadOrCreate<T>(string path, System.Action<T> init, out bool created) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            created = asset == null;
            if (created)
            {
                asset = ScriptableObject.CreateInstance<T>();
                init(asset);
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        public static GameObject SavePrefab(GameObject go, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ---------- UI ----------

        public static RectTransform UIRect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
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

        public static void Stretch(RectTransform rt, Vector2 pivot)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = pivot;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static Image UIImage(string name, Transform parent, Color color)
        {
            var rt = UIRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(rt, new Vector2(0.5f, 0.5f));
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static TextMeshProUGUI UIText(string name, Transform parent, string text, float size, TextAlignmentOptions align, Color color)
        {
            var rt = UIRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(rt, new Vector2(0.5f, 0.5f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.enableWordWrapping = true;
            return t;
        }

        public static Button UIButton(string name, Transform parent, string label, Color color, float fontSize)
        {
            var rt = UIRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 50));
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            var button = rt.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            button.colors = colors;
            UIText("Label", rt, label, fontSize, TextAlignmentOptions.Center, Color.white);
            return button;
        }

        /// <summary>Полоска: фон + заливка (масштабируется по X слева направо). Возвращает заливку.</summary>
        public static RectTransform UIBar(Transform parent, Color back, Color fill)
        {
            UIImage("Back", parent, back);
            var f = UIImage("Fill", parent, fill).rectTransform;
            Stretch(f, new Vector2(0f, 0.5f));
            return f;
        }
    }
}
