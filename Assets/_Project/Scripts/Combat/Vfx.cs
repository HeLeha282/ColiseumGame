using System.Collections.Generic;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Временные эффекты из примитивов (пока нет настоящих VFX): вспышки, кольца, лучи, молнии, всплывающий текст.
    /// </summary>
    public static class Vfx
    {
        private static readonly Dictionary<Color, Material> Cache = new Dictionary<Color, Material>();
        private static Shader unlit;

        public static Material Mat(Color c, float glow = 2f)
        {
            if (Cache.TryGetValue(c, out var m) && m != null) return m;
            if (unlit == null) unlit = Shader.Find("Universal Render Pipeline/Unlit");
            m = new Material(unlit);
            m.SetColor("_BaseColor", c * glow);
            Cache[c] = m;
            return m;
        }

        private static GameObject Prim(PrimitiveType type, Vector3 pos, Color color, float glow = 2f)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = pos;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Mat(color, glow);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        /// <summary>Сфера, которая быстро сжимается (вспышка, взрыв).</summary>
        public static void Flash(Vector3 pos, Color color, float size, float duration = 0.3f)
        {
            var go = Prim(PrimitiveType.Sphere, pos, color);
            go.transform.localScale = Vector3.one * size;
            go.AddComponent<VfxAnim>().Init(duration, VfxAnim.Mode.Shrink);
        }

        /// <summary>Расширяющееся плоское кольцо на земле.</summary>
        public static void Ring(Vector3 pos, Color color, float radius, float duration = 0.4f)
        {
            var go = Prim(PrimitiveType.Cylinder, pos + Vector3.up * 0.05f, color);
            go.transform.localScale = new Vector3(0.2f, 0.02f, 0.2f);
            go.AddComponent<VfxAnim>().Init(duration, VfxAnim.Mode.Expand, radius * 2f);
        }

        /// <summary>Толстый луч между точками (ультимейт, выстрелы).</summary>
        public static void Beam(Vector3 from, Vector3 to, Color color, float width, float duration = 0.35f)
        {
            Vector3 d = to - from;
            var go = Prim(PrimitiveType.Cylinder, (from + to) * 0.5f, color, 3f);
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            go.transform.localScale = new Vector3(width, d.magnitude * 0.5f, width);
            go.AddComponent<VfxAnim>().Init(duration, VfxAnim.Mode.Thin);
        }

        /// <summary>Ломаная молния между точками.</summary>
        public static void Lightning(Vector3 from, Vector3 to, Color color, float duration = 0.18f)
        {
            var go = new GameObject("Lightning");
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = Mat(color, 3f);
            lr.widthMultiplier = 0.12f;
            const int n = 8;
            lr.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                Vector3 p = Vector3.Lerp(from, to, t);
                if (i > 0 && i < n - 1) p += Random.insideUnitSphere * 0.4f;
                lr.SetPosition(i, p);
            }
            Object.Destroy(go, duration);
        }

        /// <summary>Всплывающий текст над точкой (реакции, крит, «ИММУНИТЕТ»).</summary>
        public static void Text(Vector3 pos, string text, Color color, float size = 1f)
        {
            var go = new GameObject("FloatText");
            go.transform.position = pos;
            var tmp = go.AddComponent<TMPro.TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 6f * size;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = Color.black;
            go.AddComponent<VfxAnim>().Init(0.9f, VfxAnim.Mode.FloatUp);
        }
    }

    /// <summary>Простая анимация временного эффекта и его удаление.</summary>
    public class VfxAnim : MonoBehaviour
    {
        public enum Mode { Shrink, Expand, Thin, FloatUp }

        private float duration, t, target;
        private Mode mode;
        private Vector3 start;
        private Camera cam;

        public void Init(float d, Mode m, float targetSize = 0f)
        {
            duration = Mathf.Max(0.05f, d);
            mode = m;
            target = targetSize;
            start = transform.localScale;
        }

        private void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            switch (mode)
            {
                case Mode.Shrink:
                    transform.localScale = start * (1f - k);
                    break;
                case Mode.Expand:
                    float s = Mathf.Lerp(start.x, target, Mathf.Sqrt(k));
                    transform.localScale = new Vector3(s, start.y * (1f - k * 0.5f), s);
                    break;
                case Mode.Thin:
                    transform.localScale = new Vector3(start.x * (1f - k), start.y, start.z * (1f - k));
                    break;
                case Mode.FloatUp:
                    transform.position += Vector3.up * Time.deltaTime * 1.5f;
                    if (cam == null) cam = Camera.main;
                    if (cam != null) transform.rotation = cam.transform.rotation;
                    break;
            }
            if (t >= duration) Destroy(gameObject);
        }
    }
}
