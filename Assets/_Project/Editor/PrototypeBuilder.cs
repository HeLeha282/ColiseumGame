using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using static ColiseumGame.EditorTools.BuilderUtil;

namespace ColiseumGame.EditorTools
{
    /// <summary>
    /// Генератор серого прототипа: материалы, конфиги (ScriptableObject), префабы и сцена Prototype.
    /// Меню: ColiseumGame → Build Prototype.
    ///
    /// ВАЖНО: префабы и сцена пересоздаются заново (ручные правки в них пропадут),
    /// а конфиги баланса (.asset в ScriptableObjects) создаются только если их нет — их можно править.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        public const string Root = "Assets/_Project";
        public const string MatDir = Root + "/Art/Materials";
        public const string SODir = Root + "/ScriptableObjects";
        public const string PrefabDir = Root + "/Prefabs";
        public const string ScenePath = Root + "/Scenes/Prototype.unity";
        public const string NavMeshPath = Root + "/Scenes/Prototype_NavMesh.asset";
        public const string InputPath = Root + "/Settings/ColiseumControls.inputactions";

        private static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();

        [MenuItem("ColiseumGame/Build Prototype")]
        public static void BuildAll()
        {
            foreach (var f in new[]
                     {
                         MatDir, SODir + "/Player", SODir + "/Enemies", SODir + "/Towers", SODir + "/Waves",
                         PrefabDir + "/Environment", PrefabDir + "/Player", PrefabDir + "/Enemies", PrefabDir + "/Towers",
                         PrefabDir + "/UI", PrefabDir + "/Systems"
                     })
                EnsureFolder(f);

            CreateMaterials();
            var configs = CreateConfigs();
            var prefabs = CreatePrefabs(configs);
            BuildScene(prefabs);
            ColiseumArtIntegration.ApplyLook();
            AssetDatabase.SaveAssets();
            Debug.Log("[ColiseumGame] Прототип собран.");
        }

        /// <summary>Перезаписать все конфиги баланса значениями из кода генератора (ручные правки баланса пропадут).</summary>
        [MenuItem("ColiseumGame/Reset Balance To Defaults")]
        public static void ResetBalance()
        {
            OverwriteConfigs = true;
            try
            {
                CreateConfigs();
                AssetDatabase.SaveAssets();
                Debug.Log("[ColiseumGame] Баланс сброшен к значениям генератора.");
            }
            finally
            {
                OverwriteConfigs = false;
            }
        }

        [MenuItem("ColiseumGame/Rebake NavMesh")]
        public static void RebakeNavMesh()
        {
            var surface = Object.FindObjectOfType<NavMeshSurface>();
            if (surface == null)
            {
                Debug.LogError("В сцене нет NavMeshSurface");
                return;
            }
            Bake(surface);
            EditorSceneManager.SaveScene(surface.gameObject.scene);
        }

        // ---------- Материалы ----------

        private static void CreateMaterials()
        {
            Mats.Clear();
            void M(string n, Color c, float e = 0f, float s = 0.2f) => Mats[n] = Material(MatDir, n, c, e, s);
            M("Sand", new Color(0.82f, 0.74f, 0.58f));
            M("Ground", new Color(0.45f, 0.42f, 0.38f));
            M("Stone", new Color(0.62f, 0.6f, 0.57f));
            M("StoneDark", new Color(0.48f, 0.46f, 0.44f));
            M("Wall", new Color(0.55f, 0.52f, 0.48f));
            M("Ramp", new Color(0.72f, 0.62f, 0.45f));
            M("Arch", new Color(0.35f, 0.3f, 0.28f));
            M("Void", new Color(0.05f, 0.05f, 0.06f));
            M("Portal", new Color(0.2f, 0.9f, 1f), 2.5f, 0.8f);
            M("PortalBase", new Color(0.3f, 0.32f, 0.36f));
            M("Player", new Color(0.25f, 0.45f, 0.95f));
            M("Visor", new Color(0.1f, 0.1f, 0.15f));
            M("Steel", new Color(0.8f, 0.82f, 0.86f), 0f, 0.7f);
            M("Wood", new Color(0.5f, 0.33f, 0.18f));
            M("Walker", new Color(0.85f, 0.25f, 0.2f));
            M("ShieldBearer", new Color(0.95f, 0.55f, 0.15f));
            M("Shield", new Color(0.2f, 0.35f, 0.85f), 0f, 0.6f);
            M("Flyer", new Color(0.6f, 0.3f, 0.85f));
            M("Boss", new Color(0.45f, 0.08f, 0.08f));
            M("Armor", new Color(0.55f, 0.58f, 0.62f), 0f, 0.8f);
            M("TowerArcher", new Color(0.3f, 0.6f, 0.3f));
            M("TowerCatapult", new Color(0.55f, 0.38f, 0.22f));
            M("TowerTop", new Color(0.4f, 0.4f, 0.42f));
            M("LevelMark", new Color(1f, 0.85f, 0.2f), 1.5f);
            M("Slot", new Color(0.78f, 0.62f, 0.32f), 0.12f, 0.5f);  // приглушённое золото под стиль Колизея
            M("BarricadeSpot", new Color(0.72f, 0.38f, 0.16f), 0.15f);
            M("Orb", new Color(0.3f, 1f, 0.9f), 3f);
            M("ArrowPlayer", new Color(1f, 1f, 0.9f), 1f);
            M("ArrowTower", new Color(1f, 0.9f, 0.4f), 2f);
            M("Explosion", new Color(1f, 0.55f, 0.1f), 3f);
        }

        // ---------- Сцена ----------

        private static void BuildScene(PrefabSet p)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects()) Object.DestroyImmediate(go);

            GameObject Place(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(pos, rot);
                return go;
            }

            var coliseum = Place(p.Coliseum, Vector3.zero, Quaternion.identity);
            Place(p.Portal, Vector3.zero, Quaternion.identity);
            Place(p.Routes, Vector3.zero, Quaternion.identity);

            var spots = new GameObject("BarricadeSpots").transform;
            foreach (var (pos, rot) in BarricadeSpotPoses())
                Place(p.BarricadeSpot, pos, rot, spots);

            Place(p.CameraRig, Vector3.zero, Quaternion.identity);
            Place(p.Player, new Vector3(0f, 0.05f, -7f), Quaternion.LookRotation(Vector3.back));
            Place(p.GameSystems, Vector3.zero, Quaternion.identity);
            Place(p.HUD, Vector3.zero, Quaternion.identity);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.75f, 0.78f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.52f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.28f, 0.25f);

            Bake(coliseum.GetComponent<NavMeshSurface>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void Bake(NavMeshSurface surface)
        {
            surface.RemoveData();
            surface.navMeshData = null;
            surface.BuildNavMesh();
            var data = surface.navMeshData;
            if (AssetDatabase.LoadAssetAtPath<Object>(NavMeshPath) != null) AssetDatabase.DeleteAsset(NavMeshPath);
            AssetDatabase.CreateAsset(data, NavMeshPath);
            surface.navMeshData = data;
            EditorUtility.SetDirty(surface);
        }

        // ---------- Помощники для приватных полей ----------

        /// <summary>Записать ссылки в [SerializeField]-поля компонента.</summary>
        public static void Assign(Object target, params (string field, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in refs)
            {
                var prop = so.FindProperty(field);
                if (prop == null) Debug.LogError($"[Builder] {target.GetType().Name}: нет поля {field}");
                else prop.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void AssignArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetValue(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop.propertyType == SerializedPropertyType.Integer) prop.intValue = (int)value;
            else prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static InputActionAsset InputAsset => AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
    }
}
