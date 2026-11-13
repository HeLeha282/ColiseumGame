using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ColiseumGame
{
    /// <summary>
    /// Настройки графики: «Низкая / Средняя / Высокая». Выбор сохраняется (PlayerPrefs), меняется на стартовом экране
    /// или клавишей F10 в любой момент. По умолчанию уровень подбирается по видеопамяти.
    ///
    /// Что меняется: разрешение рендера (render scale), дальность/качество теней, сглаживание (MSAA), SSAO,
    /// пост-обработка, полная/облегчённая модель Колизея, деревья и кусты.
    /// Настройки URP меняются только на время игры и возвращаются при выходе (чтобы не портить ассет в редакторе).
    /// </summary>
    public static class GraphicsQuality
    {
        public enum Level { Low = 0, Medium = 1, High = 2 }

        public static readonly string[] Names = { "Низкая", "Средняя", "Высокая" };
        private const string PrefKey = "ColiseumGraphics";

        public static Level Current { get; private set; } = Level.High;
        public static event System.Action Changed;

        private struct Preset
        {
            public float renderScale, shadowDistance;
            public int cascades, msaa, shadowRes;
            public bool softShadows, ssao, post, highModel, foliage;
        }

        private static readonly Preset[] Presets =
        {
            new Preset { renderScale = .75f, shadowDistance = 45f, cascades = 1, msaa = 1, shadowRes = 1024,
                softShadows = false, ssao = false, post = false, highModel = false, foliage = false },
            new Preset { renderScale = .9f, shadowDistance = 80f, cascades = 2, msaa = 2, shadowRes = 2048,
                softShadows = true, ssao = false, post = true, highModel = false, foliage = true },
            new Preset { renderScale = 1f, shadowDistance = 140f, cascades = 4, msaa = 4, shadowRes = 4096,
                softShadows = true, ssao = true, post = true, highModel = true, foliage = true },
        };

        // Исходные значения URP — восстанавливаются при выходе из игры
        private static bool saved;
        private static float origScale, origShadowDist;
        private static int origCascades, origMsaa, origShadowRes;
        private static bool origSoft;
        private static readonly List<(ScriptableRendererFeature f, bool active)> origFeatures = new List<(ScriptableRendererFeature, bool)>();

        /// <summary>Применить сохранённый (или автоматически подобранный) уровень. Вызывается при старте сцены.</summary>
        public static void ApplySaved()
        {
            int lvl = PlayerPrefs.HasKey(PrefKey) ? PlayerPrefs.GetInt(PrefKey) : (int)AutoDetect();
            Apply((Level)Mathf.Clamp(lvl, 0, 2), false);
        }

        /// <summary>Подбор по видеопамяти: встроенная графика ноутбуков обычно отдаёт ≤ 2 ГБ.</summary>
        public static Level AutoDetect()
        {
            int vram = SystemInfo.graphicsMemorySize;
            if (vram <= 2100) return Level.Low;
            if (vram < 4000) return Level.Medium;
            return Level.High;
        }

        public static void Cycle() => Apply((Level)(((int)Current + 2) % 3), true);   // F10: Высокая → Средняя → Низкая → …

        public static void Apply(Level level, bool save = true)
        {
            Current = level;
            if (save)
            {
                PlayerPrefs.SetInt(PrefKey, (int)level);
                PlayerPrefs.Save();
            }
            var p = Presets[(int)level];
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                Snapshot(urp);
                urp.renderScale = p.renderScale;
                urp.shadowDistance = p.shadowDistance;
                urp.shadowCascadeCount = p.cascades;
                urp.msaaSampleCount = p.msaa;
                SetField(urp, "m_MainLightShadowmapResolution", p.shadowRes);
                SetField(urp, "m_SoftShadowsSupported", p.softShadows);
                foreach (var f in Features(urp))
                    if (f.GetType().Name == "ScreenSpaceAmbientOcclusion") f.SetActive(p.ssao);
            }
            foreach (var cam in Object.FindObjectsOfType<Camera>(true))
                if (cam.TryGetComponent(out UniversalAdditionalCameraData data))
                {
                    data.renderPostProcessing = p.post;
                    data.antialiasing = p.post ? AntialiasingMode.SubpixelMorphologicalAntiAliasing : AntialiasingMode.None;
                }
            QualitySettings.lodBias = level == Level.High ? 2f : 1f;
            var lod = ColiseumArtLod.Instance != null ? ColiseumArtLod.Instance : Object.FindObjectOfType<ColiseumArtLod>();
            if (lod != null) lod.Apply(p.highModel, p.foliage);
            Changed?.Invoke();
        }

        private static void Snapshot(UniversalRenderPipelineAsset urp)
        {
            if (saved) return;
            saved = true;
            origScale = urp.renderScale;
            origShadowDist = urp.shadowDistance;
            origCascades = urp.shadowCascadeCount;
            origMsaa = urp.msaaSampleCount;
            origShadowRes = GetField<int>(urp, "m_MainLightShadowmapResolution");
            origSoft = GetField<bool>(urp, "m_SoftShadowsSupported");
            origFeatures.Clear();
            foreach (var f in Features(urp)) origFeatures.Add((f, f.isActive));
            Application.quitting += Restore;
        }

        /// <summary>Вернуть URP-ассет как было (в редакторе — при выходе из Play Mode).</summary>
        private static void Restore()
        {
            Application.quitting -= Restore;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null || !saved) return;
            urp.renderScale = origScale;
            urp.shadowDistance = origShadowDist;
            urp.shadowCascadeCount = origCascades;
            urp.msaaSampleCount = origMsaa;
            SetField(urp, "m_MainLightShadowmapResolution", origShadowRes);
            SetField(urp, "m_SoftShadowsSupported", origSoft);
            foreach (var (f, active) in origFeatures)
                if (f != null) f.SetActive(active);
            saved = false;
        }

        private static IEnumerable<ScriptableRendererFeature> Features(UniversalRenderPipelineAsset urp)
        {
            var list = GetField<ScriptableRendererData[]>(urp, "m_RendererDataList");
            if (list == null) yield break;
            foreach (var data in list)
                if (data != null)
                    foreach (var f in data.rendererFeatures)
                        if (f != null) yield return f;
        }

        private static T GetField<T>(object o, string name)
        {
            var fi = o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            return fi != null ? (T)fi.GetValue(o) : default;
        }

        private static void SetField(object o, string name, object value)
        {
            var fi = o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (fi != null) fi.SetValue(o, value);
        }
    }
}
