using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using static ColiseumGame.EditorTools.BuilderUtil;
using static ColiseumGame.EditorTools.PrototypeBuilder;
using Object = UnityEngine.Object;

namespace ColiseumGame.EditorTools
{
    public static class CatapultIntegration
    {
        public const string Art = "Assets/_Project/Art/Catapults";
        public const string PrefabPath = "Assets/_Project/Prefabs/Towers/Tower_Catapult.prefab";
        public const string ConfigPath = "Assets/_Project/ScriptableObjects/Towers/Tower_Catapult.asset";
        public const string ProjectilePath = "Assets/_Project/Prefabs/Towers/Projectile_Rock.prefab";
        public static readonly Vector3 ColliderSize = new Vector3(2.6f, 2.8f, 2.6f);

        [MenuItem("ColiseumGame/Install Catapult Art")]
        public static void Install()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            PrepareModels();
            Move("Assets/_Project/ScriptableObjects/Towers/Tower_Ballista.asset", ConfigPath);
            Move("Assets/_Project/Prefabs/Towers/Tower_Ballista.prefab", PrefabPath);
            Move("Assets/_Project/Prefabs/Towers/Projectile_Bolt.prefab", ProjectilePath);
            var cfg = AssetDatabase.LoadAssetAtPath<TowerConfig>(ConfigPath);
            if (cfg == null) throw new InvalidOperationException("Catapult configuration is missing.");
            var explosion = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Towers/FX_Explosion.prefab");
            var projectile = SavePrefab(CreateProjectile(explosion), ProjectilePath);
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                root.name = "Tower_Catapult";
                var tower = root.GetComponent<Tower>();
                var bar = root.GetComponentInChildren<WorldHealthBar>(true);
                foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                    if (bar == null || child != bar.transform) Object.DestroyImmediate(child.gameObject);
                var head = Empty("Head", root.transform, Vector3.zero);
                if (bar != null) bar.transform.localPosition = Vector3.up * 3f;
                var collider = root.GetComponent<BoxCollider>();
                collider.center = Vector3.up * (ColliderSize.y * .5f); collider.size = ColliderSize;
                var obstacle = root.GetComponent<NavMeshObstacle>();
                obstacle.center = collider.center; obstacle.size = ColliderSize;
                Assign(tower, ("config", cfg), ("head", head), ("muzzle", null),
                    ("projectilePrefab", projectile.GetComponent<TowerProjectile>()), ("healthBar", bar), ("hitCollider", collider));
                AssignArray(tower, "levelMarkers", Array.Empty<Object>());
                AddModels(tower, head);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            cfg.name = "Tower_Catapult";
            cfg.displayName = "Катапульта";
            cfg.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<Tower>();
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
            Debug.Log("CATAPULT_INSTALL_OK: three levels, baked textures, attack cycle and rock projectile.");
        }

        private static void Move(string oldPath, string newPath)
        {
            if (AssetDatabase.LoadMainAssetAtPath(oldPath) == null) return;
            if (AssetDatabase.LoadMainAssetAtPath(newPath) != null) throw new InvalidOperationException("Both assets exist: " + newPath);
            string error = AssetDatabase.MoveAsset(oldPath, newPath);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
        }

        public static GameObject CreateProjectile(GameObject explosion)
        {
            var root = new GameObject("Projectile_Rock");
            var projectile = root.AddComponent<TowerProjectile>();
            var stone = Material(Art, "CatapultStone", new Color(.29f, .25f, .20f), 0f, .12f);
            Prim(PrimitiveType.Sphere, "Stone", root.transform, Vector3.zero, Vector3.one * .32f, stone);
            Assign(projectile, ("explosionPrefab", explosion));
            var so = new SerializedObject(projectile); so.FindProperty("useArc").boolValue = true; so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static Transform Find(Transform root, string name)
        {
            var value = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
            if (value == null) throw new InvalidOperationException("Missing catapult node " + name);
            return value;
        }

        public static void AddModels(Tower tower, Transform head)
        {
            var models = new GameObject[3];
            var stone = AssetDatabase.LoadAssetAtPath<Material>(Art + "/M_CatapultStone.mat");
            var flax = Material(Art, "CatapultRope", new Color(.65f, .47f, .26f), 0f, .08f);
            for (int i = 0; i < 3; i++)
            {
                string path = $"{Art}/Catapult_L{i + 1}.fbx";
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null) throw new InvalidOperationException("Missing model: " + path);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(asset, head);
                // Модель из Blender «бросает» в сторону −Z, а башня наводит Head.forward (+Z) на цель —
                // разворачиваем модель, иначе катапульта стреляет спиной. Анимация крутит только дочерние узлы.
                model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                models[i] = model;
                // Clip paths are relative to this imported root, so keep its hierarchy intact.
                var animation = model.GetComponent<Animation>();
                if (animation != null) { animation.playAutomatically = false; animation.enabled = false; }
                var cycle = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview"));
                var socket = Find(model.transform, "ProjectileSocket");
                var rock = Prim(PrimitiveType.Sphere, "LoadedStone", socket, Vector3.zero, Vector3.one * .30f, stone);
                // Keep the stone in metres despite the FBX's centimetre conversion transform.
                Vector3 scale = socket.lossyScale;
                rock.transform.localScale = new Vector3(.30f / Mathf.Abs(scale.x), .30f / Mathf.Abs(scale.y), .30f / Mathf.Abs(scale.z));
                var ropeObject = new GameObject("DrawRope"); ropeObject.transform.SetParent(model.transform, false);
                var rope = ropeObject.AddComponent<LineRenderer>();
                rope.sharedMaterial = flax; rope.positionCount = 3; rope.useWorldSpace = true;
                rope.startWidth = rope.endWidth = .032f; rope.numCapVertices = 3; rope.numCornerVertices = 3;
                var visual = model.AddComponent<CatapultVisual>();
                Assign(visual, ("cycle", cycle), ("projectileSocket", socket), ("loadedRock", rock), ("drawRope", rope),
                    ("ropeDrum", Find(model.transform, "RopeDrum")), ("ropeGuide", Find(model.transform, "RopeGuide")), ("drawHook", Find(model.transform, "DrawHook")));
                visual.ResetPose();
                model.SetActive(i == 0);
            }
            AssignArray(tower, "levelModels", models);
            var serialized = new SerializedObject(tower);
            serialized.FindProperty("horizontalAim").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PrepareModels()
        {
            EnsureFolder(Art);
            for (int level = 1; level <= 3; level++)
            {
                string name = $"Catapult_L{level}";
                string colorPath = $"{Art}/{name}_BaseColor.png";
                string normalPath = $"{Art}/{name}_Normal.png";
                foreach (string texturePath in new[] { colorPath, normalPath })
                {
                    var ti = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                    ti.maxTextureSize = 2048; ti.mipmapEnabled = true; ti.anisoLevel = 4;
                    ti.textureType = texturePath == normalPath ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    ti.sRGBTexture = texturePath != normalPath;
                    ti.SaveAndReimport();
                }
                var color = AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath);
                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                string fbx = $"{Art}/{name}.fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(fbx);
                importer.importAnimation = true; importer.animationType = ModelImporterAnimationType.Legacy;
                importer.animationCompression = ModelImporterAnimationCompression.Off;
                importer.importCameras = false; importer.importLights = false; importer.addCollider = false;
                importer.preserveHierarchy = true; importer.importNormals = ModelImporterNormals.Import;
                foreach (string surface in new[] { "Base", "Metal", "Rune" })
                {
                    string matName = name + "_" + surface;
                    var mat = Material(Art, matName, Color.white, 0f, surface == "Metal" ? .62f : .28f);
                    mat.SetTexture("_BaseMap", color); mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP");
                    mat.SetFloat("_Metallic", surface == "Metal" ? .7f : 0f);
                    if (surface == "Rune")
                    {
                        mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(1f, .25f, .02f) * 2f);
                    }
                    EditorUtility.SetDirty(mat);
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), matName), mat);
                }
                importer.SaveAndReimport();
            }
        }
    }
}
