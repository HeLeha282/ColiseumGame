using UnityEditor;
using UnityEngine;

namespace ColiseumGame.EditorTools
{
    /// <summary>
    /// Подключает красивую модель Колизея (ArtSource/Coliseum/v2 → Art/Coliseum/Coliseum_Art.fbx) к префабу Coliseum.
    /// Серые меши прототипа выключаются (рендер), но их коллайдеры остаются — геймплей и NavMesh не меняются.
    /// Меню: ColiseumGame → Install Coliseum Art. Генератор прототипа вызывает AddArt сам.
    /// </summary>
    public static class ColiseumArtIntegration
    {
        public const string Art = "Assets/_Project/Art/Coliseum";
        public const string Fbx = Art + "/Coliseum_Art.fbx";
        public const string MatDir = Art + "/Materials";
        public const string PrefabPath = "Assets/_Project/Prefabs/Environment/Coliseum.prefab";

        // Ключ = имя объекта в FBX без "Col_" (= материал в ArtSource/Coliseum/v2/build_coliseum_v2.py, словарь MATS).
        // texture — файл Textures/<texture>.png (null — однотонный), color умножается на текстуру,
        // solid — объект закрывает камеру (MeshCollider на слое CameraBlocker).
        private static readonly (string key, string texture, Color color, float metallic, float smoothness, float emission, bool solid)[] Materials =
        {
            ("Stone", "T2_Stone", new Color(.95f, .84f, .70f), 0f, .15f, 0f, true),
            ("StoneFar", "T2_Stone", new Color(.93f, .82f, .68f), 0f, .15f, 0f, true),
            ("Paving", "T2_Paving", new Color(.92f, .83f, .72f), 0f, .1f, 0f, true),
            ("Seat", "T2_Paving", new Color(.90f, .80f, .68f), 0f, .1f, 0f, true),
            ("Brick", "T2_Brick", Color.white, 0f, .1f, 0f, true),
            ("Plaster", "T2_Plaster", Color.white, 0f, .1f, 0f, true),
            ("Sand", "T2_Sand", Color.white, 0f, 0f, 0f, true),
            ("Grass", "T2_Grass", Color.white, 0f, 0f, 0f, true),
            ("Roof", "T2_Roof", Color.white, 0f, .2f, 0f, true),
            ("Banner", "T2_Banner", Color.white, 0f, .15f, 0f, false),
            ("Gold", null, new Color(.98f, .70f, .24f), 1f, .68f, 0f, false),
            ("Iron", null, new Color(.46f, .30f, .19f), .35f, .45f, 0f, false),
            ("Cloth", null, new Color(.66f, .10f, .09f), 0f, .15f, 0f, false),
            ("RedPanel", null, new Color(.55f, .10f, .08f), 0f, .2f, 0f, true),
            ("Shadow", null, new Color(.22f, .14f, .10f), 0f, 0f, 0f, true),
            ("Void", null, new Color(.03f, .02f, .02f), 0f, 0f, 0f, false),
            ("Moss", null, new Color(.48f, .56f, .16f), 0f, 0f, 0f, false),
            ("Leaf", null, new Color(.36f, .45f, .14f), 0f, .1f, 0f, false),
            ("LeafLight", null, new Color(.62f, .64f, .18f), 0f, .1f, 0f, false),
            ("Bark", null, new Color(.45f, .28f, .16f), 0f, .1f, 0f, false),
            ("Fire", null, new Color(1f, .45f, .10f), 0f, 0f, 4f, false),
            ("Ember", null, new Color(.85f, .22f, .06f), 0f, 0f, 2f, false),
            ("PortalGlow", null, new Color(.25f, .95f, 1f), 0f, .5f, 3f, false),
        };
        [MenuItem("ColiseumGame/Install Coliseum Art")]
        public static void Install()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (AddArt(root)) PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            ApplyLook();
            Debug.Log("[ColiseumGame] Модель Колизея установлена в префаб.");
        }

        // ---------------------------------------------------------------- свет и пост-обработка

        public const string ProfilePath = Art + "/PP_Coliseum.asset";
        public const string SkyPath = Art + "/Materials/M_Col_Sky.mat";

        /// <summary>
        /// Тени, SSAO, пост-обработка (тонмаппинг, bloom, цвет), солнце, небо и дымка для открытой сцены и префабов.
        /// </summary>
        [MenuItem("ColiseumGame/Apply Coliseum Lighting")]
        public static void ApplyLook()
        {
            // URP: дальние мягкие тени на весь Колизей, сглаживание
            var urp = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/_Project/Settings/URP_PipelineAsset.asset");
            if (urp != null)
            {
                urp.shadowDistance = 140f;
                urp.shadowCascadeCount = 4;
                urp.msaaSampleCount = 4;
                var so = new SerializedObject(urp);
                Set(so, "m_SoftShadowsSupported", true);
                Set(so, "m_MainLightShadowmapResolution", 4096);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(urp);
            }
            AddSsao();

            // Профиль пост-обработки
            var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            var tone = Comp<UnityEngine.Rendering.Universal.Tonemapping>(profile);
            tone.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.ACES);
            var bloom = Comp<UnityEngine.Rendering.Universal.Bloom>(profile);
            bloom.threshold.Override(1.0f);
            bloom.intensity.Override(0.7f);
            bloom.scatter.Override(0.65f);
            var color = Comp<UnityEngine.Rendering.Universal.ColorAdjustments>(profile);
            color.postExposure.Override(0.35f);
            color.contrast.Override(14f);
            color.saturation.Override(10f);
            var wb = Comp<UnityEngine.Rendering.Universal.WhiteBalance>(profile);
            wb.temperature.Override(8f);
            var vignette = Comp<UnityEngine.Rendering.Universal.Vignette>(profile);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.5f);
            EditorUtility.SetDirty(profile);

            // Префаб Колизея: солнце и глобальный Volume
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var sunT = root.transform.Find("Sun");
                if (sunT != null)
                {
                    sunT.rotation = Quaternion.Euler(38f, 150f, 0f);
                    var sun = sunT.GetComponent<Light>();
                    sun.color = new Color(1f, 0.88f, 0.72f);
                    sun.intensity = 1.7f;
                    sun.shadows = LightShadows.Soft;
                    sun.shadowStrength = 0.85f;
                }
                var fx = root.transform.Find("PostFX");
                if (fx == null)
                {
                    fx = new GameObject("PostFX").transform;
                    fx.SetParent(root.transform, false);
                }
                if (!fx.TryGetComponent(out UnityEngine.Rendering.Volume vol)) vol = fx.gameObject.AddComponent<UnityEngine.Rendering.Volume>();
                vol.isGlobal = true;
                vol.sharedProfile = profile;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            // Камера: включить пост-обработку и сглаживание
            const string rigPath = "Assets/_Project/Prefabs/Player/CameraRig.prefab";
            var rig = PrefabUtility.LoadPrefabContents(rigPath);
            try
            {
                foreach (var cam in rig.GetComponentsInChildren<Camera>(true))
                {
                    if (!cam.TryGetComponent(out UnityEngine.Rendering.Universal.UniversalAdditionalCameraData data))
                        data = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                    data.renderPostProcessing = true;
                    data.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    data.renderShadows = true;
                }
                foreach (var col in rig.GetComponentsInChildren<Cinemachine.CinemachineCollider>(true))
                    col.m_CollideAgainst |= 1 << EnsureCameraBlockerLayer();
                PrefabUtility.SaveAsPrefabAsset(rig, rigPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(rig);
            }

            ApplySceneSettings();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Небо, окружающий свет и дымка открытой сцены (вызывается и генератором сцены).</summary>
        public static void ApplySceneSettings()
        {
            BuilderUtil.EnsureFolder(MatDir);
            var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyPath);
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, SkyPath);
            }
            sky.SetFloat("_SunSize", 0.035f);
            sky.SetFloat("_AtmosphereThickness", 0.85f);
            sky.SetColor("_SkyTint", new Color(0.52f, 0.62f, 0.82f));
            sky.SetColor("_GroundColor", new Color(0.55f, 0.47f, 0.38f));
            sky.SetFloat("_Exposure", 1.25f);
            EditorUtility.SetDirty(sky);

            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.54f, 0.44f);
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.29f, 0.23f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0035f;
            RenderSettings.fogColor = new Color(0.82f, 0.78f, 0.72f);
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }

        private static void AddSsao()
        {
            var rd = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/_Project/Settings/URP_Renderer.asset");
            if (rd == null) return;
            var type = typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset).Assembly
                .GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");
            if (type == null) return;
            foreach (var f in rd.rendererFeatures)
                if (f != null && f.GetType() == type) return;
            var feature = (UnityEngine.Rendering.Universal.ScriptableRendererFeature)ScriptableObject.CreateInstance(type);
            feature.name = "SSAO";
            AssetDatabase.AddObjectToAsset(feature, rd);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);
            var so = new SerializedObject(rd);
            var list = so.FindProperty("m_RendererFeatures");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
            var map = so.FindProperty("m_RendererFeatureMap");
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            so.ApplyModifiedPropertiesWithoutUndo();
            var fso = new SerializedObject(feature);
            Set(fso, "m_Settings.Intensity", 1.6f);
            Set(fso, "m_Settings.Radius", 0.6f);
            Set(fso, "m_Settings.Falloff", 120f);
            fso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rd);
        }

        private static T Comp<T>(UnityEngine.Rendering.VolumeProfile profile) where T : UnityEngine.Rendering.VolumeComponent
        {
            if (!profile.TryGet(out T c))
            {
                c = profile.Add<T>(true);
                c.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(c, profile);
            }
            c.active = true;
            return c;
        }

        private static void Set(SerializedObject so, string path, object value)
        {
            var p = so.FindProperty(path);
            if (p == null)
            {
                Debug.LogWarning($"[ColiseumGame] Нет свойства {path} у {so.targetObject.GetType().Name}");
                return;
            }
            switch (value)
            {
                case bool b: p.boolValue = b; break;
                case int i: p.intValue = i; break;
                case float f: p.floatValue = f; break;
            }
        }

        /// <summary>Облегчённая версия (без фасок) — для настроек графики «Средняя» и «Низкая».</summary>
        public const string FbxLow = Art + "/Coliseum_Art_Low.fbx";

        /// <summary>Ключ материала из имени объекта FBX: «Col_Stone_S3» → «Stone».</summary>
        public static string KeyOf(string name)
        {
            string key = name.StartsWith("Col_") ? name.Substring(4) : name;
            int us = key.LastIndexOf("_S", System.StringComparison.Ordinal);
            if (us > 0 && us + 2 < key.Length && int.TryParse(key.Substring(us + 2), out _)) key = key.Substring(0, us);
            return key;
        }

        private static GameObject Instantiate(string path, GameObject root, string name)
        {
            var old = root.transform.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return null;
            PrepareImporter(path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform);
            go.name = name;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                string key = KeyOf(r.gameObject.name);
                var mat = GetMaterial(key);
                if (mat != null) r.sharedMaterial = mat;
                bool light = key == "Fire" || key == "Void" || key == "Ember" || key == "PortalGlow";
                r.shadowCastingMode = light ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            }
            return go;
        }

        /// <summary>Добавить модель в корень Колизея. Возвращает false, если FBX ещё нет.</summary>
        public static bool AddArt(GameObject root)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Fbx) == null) return false;

            // Серые меши прототипа — только коллайдеры. Земля за стенами остаётся видимой.
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
                if (r.gameObject.name != "Ground") r.enabled = false;

            var art = Instantiate(Fbx, root, "Art");
            var artLow = Instantiate(FbxLow, root, "ArtLow");
            if (artLow != null) artLow.SetActive(false);

            // Коллайдеры модели — только у полной версии и только для камеры и прицела (слой CameraBlocker ни с чем
            // физически не сталкивается и не попадает в NavMesh), иначе камера 3-го лица залезает в колонны и парапеты.
            int layer = EnsureCameraBlockerLayer();
            foreach (var r in art.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!IsSolid(KeyOf(r.gameObject.name))) continue;
                r.gameObject.layer = layer;
                if (!r.TryGetComponent(out MeshCollider mc)) mc = r.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = r.GetComponent<MeshFilter>().sharedMesh;
            }
            var surface = root.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
            if (surface != null) surface.layerMask &= ~(1 << layer);

            // Переключатель версий для настроек графики
            if (!root.TryGetComponent(out ColiseumArtLod lod)) lod = root.AddComponent<ColiseumArtLod>();
            var so = new SerializedObject(lod);
            so.FindProperty("high").objectReferenceValue = art;
            so.FindProperty("low").objectReferenceValue = artLow;
            so.ApplyModifiedPropertiesWithoutUndo();

            var ground = root.transform.Find("Ground");
            if (ground != null) ground.GetComponent<Renderer>().sharedMaterial = GetMaterial("Sand");
            return true;
        }
        public const string CameraBlockerLayer = "CameraBlocker";

        /// <summary>Слой для коллайдеров декора: не сталкивается ни с чем, его видят только камера и лучи прицела.</summary>
        public static int EnsureCameraBlockerLayer()
        {
            int layer = LayerMask.NameToLayer(CameraBlockerLayer);
            if (layer < 0)
            {
                var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                var layers = tm.FindProperty("layers");
                for (int i = 8; i < 32; i++)
                {
                    var p = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(p.stringValue))
                    {
                        p.stringValue = CameraBlockerLayer;
                        layer = i;
                        break;
                    }
                }
                tm.ApplyModifiedPropertiesWithoutUndo();
            }
            for (int i = 0; i < 32; i++) Physics.IgnoreLayerCollision(layer, i, true);
            return layer;
        }

        private static void PrepareImporter(string path)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            bool dirty = importer.materialImportMode != ModelImporterMaterialImportMode.None
                         || importer.importAnimation || importer.animationType != ModelImporterAnimationType.None
                         || importer.importNormals != ModelImporterNormals.Import;
            if (!dirty) return;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
        }

        private static bool IsSolid(string key)
        {
            foreach (var def in Materials) if (def.key == key) return def.solid;
            return false;
        }

        private static Material GetMaterial(string key)
        {
            foreach (var def in Materials)
            {
                if (def.key != key) continue;
                BuilderUtil.EnsureFolder(MatDir);
                string path = $"{MatDir}/M_Col_{key}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(mat, path);
                }
                if (def.texture != null)
                {
                    string texPath = $"{Art}/Textures/{def.texture}.png";
                    PrepareTexture(texPath);
                    mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
                }
                else mat.SetTexture("_BaseMap", null);
                mat.SetColor("_BaseColor", def.color);
                mat.SetFloat("_Metallic", def.metallic);
                mat.SetFloat("_Smoothness", def.smoothness);
                if (def.emission > 0f)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", def.color * def.emission);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                else
                {
                    mat.DisableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.black);
                }
                EditorUtility.SetDirty(mat);
                return mat;
            }
            return null;
        }
        private static void PrepareTexture(string path)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
            if (ti.wrapMode == TextureWrapMode.Repeat && ti.anisoLevel == 8 && ti.maxTextureSize == 1024) return;
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.anisoLevel = 8;
            ti.maxTextureSize = 1024;
            ti.mipmapEnabled = true;
            ti.SaveAndReimport();
        }
    }
}
