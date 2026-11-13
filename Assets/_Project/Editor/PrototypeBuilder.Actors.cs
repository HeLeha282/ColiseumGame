using Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static ColiseumGame.EditorTools.BuilderUtil;

namespace ColiseumGame.EditorTools
{
    public static partial class PrototypeBuilder
    {
        public class PrefabSet
        {
            public GameObject Coliseum, Portal, Routes, BarricadeSpot, Barricade;
            public GameObject Player, CameraRig, Arrow;
            public GameObject ProjArrow, ProjRock, ProjBolt, Explosion;
            public GameObject GameSystems, HUD;
            public readonly System.Collections.Generic.Dictionary<string, GameObject> Enemies = new System.Collections.Generic.Dictionary<string, GameObject>();
            public readonly System.Collections.Generic.Dictionary<string, GameObject> Towers = new System.Collections.Generic.Dictionary<string, GameObject>();
        }

        private static PrefabSet CreatePrefabs(ConfigSet c)
        {
            var p = new PrefabSet();
            string env = PrefabDir + "/Environment/";
            string pl = PrefabDir + "/Player/";
            string en = PrefabDir + "/Enemies/";
            string tw = PrefabDir + "/Towers/";

            p.Coliseum = SavePrefab(BuildColiseum(), env + "Coliseum.prefab");
            p.Portal = SavePrefab(BuildPortal(c.Balance), env + "Portal.prefab");
            p.Routes = SavePrefab(BuildRoutes(), env + "Routes.prefab");
            p.BarricadeSpot = SavePrefab(BuildBarricadeSpot(), env + "BarricadeSpot.prefab");
            p.Barricade = SavePrefab(BuildBarricade(), env + "Barricade.prefab");

            p.Arrow = SavePrefab(BuildPlayerArrow(), pl + "Arrow.prefab");
            p.Player = SavePrefab(BuildPlayer(c.Player, c.Hero, p.Arrow), pl + "Player.prefab");
            p.CameraRig = SavePrefab(BuildCameraRig(), pl + "CameraRig.prefab");

            // Сначала обычные враги (на них ссылаются деление и призыв), потом боссы
            foreach (var look in Looks)
                if (!c.Enemies[look.key].IsBoss) p.Enemies[look.key] = SavePrefab(BuildEnemy(look, c.Enemies[look.key], c), en + $"Enemy_{look.key}.prefab");
            LinkPrefabs(c, p);
            foreach (var look in Looks)
                if (c.Enemies[look.key].IsBoss) p.Enemies[look.key] = SavePrefab(BuildEnemy(look, c.Enemies[look.key], c), en + $"Enemy_{look.key}.prefab");

            p.Explosion = SavePrefab(BuildExplosion(), tw + "FX_Explosion.prefab");
            p.ProjArrow = SavePrefab(BuildProjectile("Projectile_Arrow", new Vector3(0.08f, 0.08f, 0.7f), Mats["ArrowTower"], null), tw + "Projectile_Arrow.prefab");
            p.ProjBolt = SavePrefab(BuildProjectile("Projectile_Bolt", new Vector3(0.18f, 0.18f, 1.3f), Mats["Steel"], null), tw + "Projectile_Bolt.prefab");
            p.ProjRock = SavePrefab(CatapultIntegration.CreateProjectile(p.Explosion), tw + "Projectile_Rock.prefab");
            p.Towers["Archer"] = SavePrefab(BuildArcherTower(c.Towers["Archer"], p.ProjArrow), tw + "Tower_Archer.prefab");
            p.Towers["Catapult"] = SavePrefab(BuildCatapultTower(c.Towers["Catapult"], p.ProjRock), tw + "Tower_Catapult.prefab");
            p.Towers["Obelisk"] = SavePrefab(BuildObelisk(c.Towers["Obelisk"]), tw + "Tower_Obelisk.prefab");
            p.Towers["Brazier"] = SavePrefab(BuildBrazier(c.Towers["Brazier"]), tw + "Tower_Brazier.prefab");
            p.Towers["Scorpion"] = SavePrefab(BuildScorpion(c.Towers["Scorpion"], p.ProjBolt), tw + "Tower_Scorpion.prefab");
            p.Towers["Sanctuary"] = SavePrefab(BuildSanctuary(c.Towers["Sanctuary"]), tw + "Tower_Sanctuary.prefab");

            p.GameSystems = SavePrefab(BuildGameSystems(c.Balance, p.Barricade), PrefabDir + "/Systems/GameSystems.prefab");
            p.HUD = SavePrefab(BuildHUD(), PrefabDir + "/UI/HUD.prefab");

            LinkPrefabs(c, p);
            return p;
        }
        // ---------- Полоска HP над головой ----------

        private static WorldHealthBar BuildWorldBar(Transform parent, float height, float widthMeters, bool armor, bool mark, Color fillColor)
        {
            var go = new GameObject("HealthBar", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.localPosition = new Vector3(0f, height, 0f);
            rt.sizeDelta = new Vector2(widthMeters * 100f, 12f);
            rt.localScale = Vector3.one * 0.01f;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var bar = go.AddComponent<WorldHealthBar>();

            var fill = UIBar(rt, new Color(0f, 0f, 0f, 0.65f), fillColor);

            RectTransform armorFill = null;
            GameObject armorRoot = null;
            if (armor)
            {
                var ar = UIRect("Armor", rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 3f), new Vector2(widthMeters * 100f, 8f));
                armorFill = UIBar(ar, new Color(0f, 0f, 0f, 0.65f), new Color(0.7f, 0.8f, 0.95f));
                armorRoot = ar.gameObject;
            }

            GameObject markIcon = null;
            if (mark)
            {
                var m = UIRect("Mark", rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, armor ? 18f : 6f), new Vector2(16f, 16f));
                m.localRotation = Quaternion.Euler(0f, 0f, 45f);
                m.gameObject.AddComponent<Image>().color = new Color(1f, 0.9f, 0.1f);
                markIcon = m.gameObject;
                markIcon.SetActive(false);
            }

            Assign(bar, ("fill", fill), ("armorFill", armorFill), ("armorRoot", armorRoot), ("markIcon", markIcon));
            return bar;
        }

        // ---------- Игрок и камеры ----------

        private static GameObject BuildPlayerArrow()
        {
            var root = new GameObject("Arrow");
            root.AddComponent<Arrow>();
            Box("Shaft", root.transform, Vector3.zero, new Vector3(0.05f, 0.05f, 0.8f), Mats["ArrowPlayer"]);
            return root;
        }

        private static GameObject BuildPlayer(PlayerConfig config, HeroConfig hero, GameObject arrowPrefab)
        {
            var root = new GameObject("Player") { tag = "Player" };
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.4f;
            cc.slopeLimit = 50f;
            root.AddComponent<Health>();

            var body = Empty("Body", root.transform, Vector3.zero);
            var capsule = Prim(PrimitiveType.Capsule, "Capsule", body, new Vector3(0f, 0.9f, 0f), new Vector3(0.8f, 0.9f, 0.8f), Mats["Player"]);
            var visor = Box("Visor", body, new Vector3(0f, 1.45f, 0.33f), new Vector3(0.5f, 0.15f, 0.2f), Mats["Visor"]);

            // Камера смотрит чуть выше головы — персонаж оказывается ниже прицела и не мешает целиться.
            var cameraTarget = Empty("CameraTarget", root.transform, new Vector3(0f, 2.3f, 0f));
            var head = Empty("Head", root.transform, new Vector3(0f, 1.65f, 0f));
            var holder = Empty("WeaponHolder", head, new Vector3(0.35f, -0.35f, 0.45f));

            var sword = Empty("Sword", holder, Vector3.zero);
            Box("Blade", sword, new Vector3(0f, 0f, 0.55f), new Vector3(0.06f, 0.12f, 1f), Mats["Steel"]);
            Box("Guard", sword, new Vector3(0f, 0f, 0.05f), new Vector3(0.3f, 0.06f, 0.06f), Mats["Wood"]);
            sword.localRotation = Quaternion.Euler(-10f, -15f, 0f);

            // Лук строится и анимируется кодом (BowVisual), здесь только объект и материалы
            var bow = Empty("Bow", holder, new Vector3(-0.1f, 0.1f, 0.15f));
            SetupBowVisual(bow.gameObject);

            // Молоток для режима стройки (клавиша 3)
            var hammer = Empty("Hammer", holder, new Vector3(0f, 0.05f, 0.1f));
            Prim(PrimitiveType.Cylinder, "Handle", hammer, new Vector3(0f, 0f, 0.3f), new Vector3(0.06f, 0.32f, 0.06f), Mats["Wood"], false, Quaternion.Euler(90f, 0f, 0f));
            Box("Head", hammer, new Vector3(0f, 0f, 0.65f), new Vector3(0.34f, 0.18f, 0.18f), Mats["Steel"]);
            Box("Band", hammer, new Vector3(0f, 0f, 0.65f), new Vector3(0.36f, 0.2f, 0.05f), Mats["LevelMark"]);
            hammer.localRotation = Quaternion.Euler(-25f, -10f, 0f);
            hammer.gameObject.SetActive(false);

            var arrowSpawn = Empty("ArrowSpawn", head, new Vector3(0.15f, -0.1f, 0.7f));

            var controller = root.AddComponent<PlayerController>();
            Assign(controller, ("config", config), ("bodyRoot", body.gameObject));

            var cam = root.AddComponent<PlayerCameraController>();
            Assign(cam, ("config", config), ("cameraTarget", cameraTarget), ("head", head));
            AssignArray(cam, "hideInFirstPerson", new Object[] { capsule.GetComponent<Renderer>(), visor.GetComponent<Renderer>() });

            var progression = root.AddComponent<PlayerProgression>();
            Assign(progression, ("hero", hero));
            root.AddComponent<TowerPlacer>();

            var combat = root.AddComponent<PlayerCombat>();
            Assign(combat, ("config", config), ("swordVisual", sword), ("bowVisual", bow), ("hammerVisual", hammer), ("arrowSpawn", arrowSpawn),
                ("arrowPrefab", arrowPrefab.GetComponent<Arrow>()));

            var abilities = root.AddComponent<PlayerAbilities>();
            Assign(abilities, ("head", head));
            root.AddComponent<AuraVisual>();

            var interactor = root.AddComponent<PlayerInteractor>();
            Assign(interactor, ("config", config));
            return root;
        }

        /// <summary>Добавить BowVisual на объект лука и назначить ему материалы (вызывается и для готового префаба).</summary>
        public static void SetupBowVisual(GameObject bow)
        {
            string dir = MatDir;
            var bv = bow.GetComponent<BowVisual>();
            if (bv == null) bv = bow.AddComponent<BowVisual>();
            Assign(bv,
                ("wood", Material(dir, "BowWood", new Color(0.48f, 0.27f, 0.13f), 0f, 0.35f)),
                ("leather", Material(dir, "BowLeather", new Color(0.25f, 0.14f, 0.08f), 0f, 0.2f)),
                ("gold", Material(dir, "BowGold", new Color(0.95f, 0.72f, 0.3f), 0f, 0.75f)),
                ("stringMat", Material(dir, "BowString", new Color(0.93f, 0.89f, 0.78f), 0f, 0.1f)),
                ("steel", Material(dir, "Steel", new Color(0.8f, 0.82f, 0.86f), 0f, 0.7f)),
                ("feather", Material(dir, "BowFeather", new Color(0.72f, 0.12f, 0.1f), 0f, 0.1f)));
            var gold = AssetDatabase.LoadAssetAtPath<Material>($"{dir}/M_BowGold.mat");
            gold.SetFloat("_Metallic", 1f);
            EditorUtility.SetDirty(gold);
        }

        private static GameObject BuildCameraRig()
        {
            var root = new GameObject("CameraRig");
            var rig = root.AddComponent<CameraRig>();

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(root.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 500f;
            camGo.AddComponent<AudioListener>();
            var brain = camGo.AddComponent<CinemachineBrain>();
            brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 0.3f);

            // 3-е лицо: свободная орбита вокруг персонажа (как в Genshin)
            var flGo = new GameObject("CM_ThirdPerson");
            flGo.transform.SetParent(root.transform, false);
            var fl = flGo.AddComponent<CinemachineFreeLook>();
            flGo.AddComponent<FreeLookInputProvider>();
            fl.m_Orbits = new[]
            {
                new CinemachineFreeLook.Orbit(6f, 3.5f),
                new CinemachineFreeLook.Orbit(2.8f, 6f),
                new CinemachineFreeLook.Orbit(0.6f, 4f),
            };
            fl.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
            fl.m_Lens.FieldOfView = 55f;
            fl.m_Lens.NearClipPlane = 0.05f;
            fl.m_Lens.FarClipPlane = 500f;
            ConfigureAxis(ref fl.m_XAxis, 0.12f, false);
            ConfigureAxis(ref fl.m_YAxis, 0.0015f, true);
            fl.m_YAxis.Value = 0.5f;
            fl.Priority = 20;
            var collider = flGo.AddComponent<CinemachineCollider>();
            collider.m_CollideAgainst = 1 | (1 << ColiseumArtIntegration.EnsureCameraBlockerLayer()); // Default + декор Колизея
            collider.m_IgnoreTag = "Player";
            collider.m_MinimumDistanceFromTarget = 0.5f;
            collider.m_Strategy = CinemachineCollider.ResolutionStrategy.PullCameraForward;
            collider.m_Damping = 0f;
            collider.m_DampingWhenOccluded = 0f;

            // 1-е лицо: камера без своих Body/Aim, во время игры прикрепляется к голове персонажа
            var fpGo = new GameObject("CM_FirstPerson");
            fpGo.transform.SetParent(root.transform, false);
            var fp = fpGo.AddComponent<CinemachineVirtualCamera>();
            fp.m_Lens.FieldOfView = 70f;
            fp.m_Lens.NearClipPlane = 0.05f;
            fp.m_Lens.FarClipPlane = 500f;
            fp.Priority = 5;

            Assign(rig, ("mainCamera", cam), ("freeLook", fl), ("firstPersonCamera", fp));
            return root;
        }

        private static void ConfigureAxis(ref AxisState axis, float gain, bool invert)
        {
            axis.m_SpeedMode = AxisState.SpeedMode.InputValueGain;
            axis.m_MaxSpeed = gain;
            axis.m_AccelTime = 0f;
            axis.m_DecelTime = 0f;
            axis.m_InputAxisName = "";
            axis.m_InvertInput = invert;
        }

        // ---------- Враги ----------

        /// <summary>Враг из примитивов по описанию внешности + компоненты по конфигу (ИИ, лекарь, босс).</summary>
        private static GameObject BuildEnemy(EnemyLook look, EnemyConfig cfg, ConfigSet c)
        {
            var root = new GameObject("Enemy_" + look.key);
            root.AddComponent<Health>();
            var visual = Empty("Visual", root.transform, Vector3.zero);
            if (cfg.isFlying)
            {
                var col = root.AddComponent<SphereCollider>();
                col.radius = look.small ? 0.45f : 0.7f;
            }
            else
            {
                var col = root.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 1f, 0f);
                col.radius = 0.5f;
                col.height = 2f;
                var agent = root.AddComponent<NavMeshAgent>();
                agent.radius = 0.45f;
                agent.height = 2f;
                agent.speed = cfg.moveSpeed;
                agent.angularSpeed = 540f;
                agent.acceleration = 20f;
                agent.stoppingDistance = 0.2f;
                agent.autoBraking = false;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            }

            // Узнаваемая фигура из примитивов (скелет, сатир, кентавр...) — см. PrototypeBuilder.EnemyModels
            var model = BuildEnemyModel(look, visual);
            if (cfg.isFlying && look.small) visual.localScale = Vector3.one * 0.75f;
            float barY = model.barY * visual.localScale.y;
            var body = model.body.gameObject;
            GameObject armorGo = model.armor;
            root.transform.localScale = Vector3.one * look.scale;
            var rig = visual.gameObject.AddComponent<EnemyRig>();
            AssignArray(rig, "legs", model.legs.ToArray());
            AssignArray(rig, "arms", model.arms.ToArray());
            AssignArray(rig, "wings", model.wings.ToArray());
            AssignArray(rig, "tails", model.tails.ToArray());

            if (cfg.element != Element.None)   // светящийся «камень стихии» на груди
                Prim(PrimitiveType.Sphere, "ElementCore", visual, model.core, Vector3.one * 0.22f, Material(MatDir, "EnemyElement_" + cfg.element, Elements.Color(cfg.element), 2f));

            var indicator = Prim(PrimitiveType.Sphere, "ElementAura", root.transform, new Vector3(0.55f, barY + 0.25f, 0f), Vector3.one * 0.22f, Mats["LevelMark"]);
            indicator.GetComponent<Renderer>().enabled = false;
            var bar = BuildWorldBar(root.transform, barY, 1.2f, cfg.armor > 0f, true, new Color(0.9f, 0.2f, 0.2f));

            var enemy = root.AddComponent<Enemy>();
            EnemyAIBase ai = cfg.isFlying ? root.AddComponent<FlyingEnemyAI>() : (EnemyAIBase)root.AddComponent<GroundEnemyAI>();
            Assign(ai, ("visualRoot", visual));
            if (cfg.healRadius > 0f) root.AddComponent<EnemyHealer>();
            switch (cfg.bossKind)
            {
                case BossKind.Minotaur: root.AddComponent<BossMinotaur>(); break;
                case BossKind.Cerberus: root.AddComponent<BossCerberus>(); break;
                case BossKind.Hydra: root.AddComponent<BossHydra>(); break;
                case BossKind.Medusa: root.AddComponent<BossMedusa>(); break;
                case BossKind.HarpyQueen: Assign(root.AddComponent<BossHarpyQueen>(), ("minion", c.E("Harpy"))); break;
                case BossKind.Titan: root.AddComponent<BossTitan>(); break;
            }
            Assign(enemy, ("config", cfg), ("healthBar", bar), ("bodyRenderer", body.GetComponent<Renderer>()), ("armorVisual", armorGo),
                ("elementIndicator", indicator.GetComponent<Renderer>()));
            return root;
        }

        // ---------- Башни ----------

        private static GameObject BuildExplosion()
        {
            var root = new GameObject("FX_Explosion");
            root.AddComponent<ShrinkAndDestroy>();
            Prim(PrimitiveType.Sphere, "Sphere", root.transform, Vector3.zero, Vector3.one, Mats["Explosion"]);
            return root;
        }

        private static GameObject BuildProjectile(string name, Vector3 size, Material mat, GameObject explosion)
        {
            var root = new GameObject(name);
            root.AddComponent<TowerProjectile>();
            Box("Visual", root.transform, Vector3.zero, size, mat);
            return root;
        }

        /// <summary>Общая основа башни: коллайдер, вырез в NavMesh, 6 отметок уровня, кристалл ветки.</summary>
        private static (GameObject root, Tower tower) TowerBase(string name, Vector3 colliderSize, float markerZ, float gemY)
        {
            var root = new GameObject(name);
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, colliderSize.y * 0.5f, 0f);
            col.size = colliderSize;
            var obstacle = root.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = col.center;
            obstacle.size = colliderSize;
            obstacle.carving = true;
            var tower = root.AddComponent<Tower>();
            var marks = new GameObject[6];
            for (int i = 0; i < 6; i++)
                marks[i] = Box($"Level_{i + 1}", root.transform, new Vector3(-0.5f + (i % 3) * 0.5f, 0.35f + (i / 3) * 0.3f, markerZ),
                    new Vector3(0.22f, 0.2f, 0.06f), Mats["LevelMark"]);
            var gem = Prim(PrimitiveType.Sphere, "BranchGem", root.transform, new Vector3(0f, gemY, 0f), Vector3.one * 0.4f, Mats["LevelMark"]);
            Assign(tower, ("hitCollider", col), ("branchGem", gem.GetComponent<Renderer>()));
            AssignArray(tower, "levelMarkers", marks);
            return (root, tower);
        }

        private const string ArcherArtDir = Root + "/Art/ArcherTowers";
        private static readonly Vector3 ArcherColliderSize = new Vector3(1.6f, 3.63f, 1.66f);

        private static GameObject BuildArcherTower(TowerConfig cfg, GameObject projectile)
        {
            var (root, tower) = TowerBase("Tower_Archer", ArcherColliderSize, 0.85f, 4.6f);
            var t = root.transform;
            var head = Empty("Head", t, new Vector3(0f, 3.8f, 0f));
            var muzzle = Empty("Muzzle", head, new Vector3(0f, 0f, 0.5f));
            Assign(tower, ("config", cfg), ("head", head), ("muzzle", muzzle), ("projectilePrefab", projectile.GetComponent<TowerProjectile>()));
            AddArcherLevelModels(tower);
            return root;
        }

        private static void AddArcherLevelModels(Tower tower)
        {
            var models = new GameObject[3];
            for (int i = 0; i < models.Length; i++)
            {
                string path = $"{ArcherArtDir}/ArcherTower_L{i + 1}.fbx";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null)
                    throw new System.InvalidOperationException($"Не найдена модель башни лучников: {path}");
                models[i] = (GameObject)PrefabUtility.InstantiatePrefab(model, tower.transform);
                models[i].SetActive(i == 0);
            }
            AssignArray(tower, "levelModels", models);
        }

        private static GameObject BuildCatapultTower(TowerConfig cfg, GameObject projectile)
        {
            var (root, tower) = TowerBase("Tower_Catapult", CatapultIntegration.ColliderSize, 1.35f, 3.3f);
            var head = Empty("Head", root.transform, Vector3.zero);
            Assign(tower, ("config", cfg), ("head", head), ("projectilePrefab", projectile.GetComponent<TowerProjectile>()));
            CatapultIntegration.AddModels(tower, head);
            return root;
        }

        private static GameObject BuildObelisk(TowerConfig cfg)
        {
            var (root, tower) = TowerBase("Tower_Obelisk", new Vector3(1.4f, 4.4f, 1.4f), 0.75f, 5f);
            var t = root.transform;
            var stone = Material(MatDir, "TowerObelisk", new Color(0.3f, 0.26f, 0.42f));
            Box("Base", t, new Vector3(0f, 0.3f, 0f), new Vector3(1.4f, 0.6f, 1.4f), Mats["Stone"]);
            Box("Pillar", t, new Vector3(0f, 2.1f, 0f), new Vector3(0.7f, 3.2f, 0.7f), stone, false, Quaternion.Euler(0f, 45f, 0f));
            var head = Empty("Head", t, new Vector3(0f, 4.1f, 0f));
            Prim(PrimitiveType.Sphere, "Crystal", head, Vector3.zero, Vector3.one * 0.7f, Material(MatDir, "TowerObeliskCrystal", Elements.Color(Element.Lightning), 2.5f));
            var muzzle = Empty("Muzzle", head, Vector3.zero);
            Assign(tower, ("config", cfg), ("head", head), ("muzzle", muzzle));
            return root;
        }

        private static GameObject BuildBrazier(TowerConfig cfg)
        {
            var (root, tower) = TowerBase("Tower_Brazier", new Vector3(1.4f, 1.6f, 1.4f), 0.72f, 2.4f);
            var t = root.transform;
            Prim(PrimitiveType.Cylinder, "Pedestal", t, new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.35f, 1.2f), Mats["Stone"]);
            Prim(PrimitiveType.Cylinder, "Bowl", t, new Vector3(0f, 1.0f, 0f), new Vector3(1.4f, 0.25f, 1.4f), Mats["Steel"]);
            Prim(PrimitiveType.Sphere, "Fire", t, new Vector3(0f, 1.4f, 0f), new Vector3(1.0f, 0.8f, 1.0f), Mats["Explosion"]);
            Assign(tower, ("config", cfg));
            return root;
        }

        private static GameObject BuildScorpion(TowerConfig cfg, GameObject projectile)
        {
            var (root, tower) = TowerBase("Tower_Scorpion", new Vector3(1.8f, 2.4f, 1.8f), 0.92f, 3.2f);
            var t = root.transform;
            Box("Base", t, new Vector3(0f, 0.6f, 0f), new Vector3(1.8f, 1.2f, 1.8f), Mats["Wood"]);
            var head = Empty("Head", t, new Vector3(0f, 1.6f, 0f));
            Box("Rail", head, Vector3.zero, new Vector3(0.3f, 0.3f, 2.2f), Mats["Wood"]);
            Box("Arms", head, new Vector3(0f, 0f, 0.6f), new Vector3(2.2f, 0.15f, 0.25f), Mats["Steel"]);
            Box("Bolt", head, new Vector3(0f, 0.22f, 0.4f), new Vector3(0.1f, 0.1f, 1.3f), Mats["Steel"]);
            var muzzle = Empty("Muzzle", head, new Vector3(0f, 0.22f, 1.2f));
            Assign(tower, ("config", cfg), ("head", head), ("muzzle", muzzle), ("projectilePrefab", projectile.GetComponent<TowerProjectile>()));
            return root;
        }

        private static GameObject BuildSanctuary(TowerConfig cfg)
        {
            var (root, tower) = TowerBase("Tower_Sanctuary", new Vector3(2.2f, 2.6f, 2.2f), 1.12f, 3.3f);
            var t = root.transform;
            Prim(PrimitiveType.Cylinder, "Platform", t, new Vector3(0f, 0.15f, 0f), new Vector3(2.4f, 0.15f, 2.4f), Mats["Stone"]);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                Prim(PrimitiveType.Cylinder, "Column", t, new Vector3(Mathf.Cos(a) * 0.85f, 1.2f, Mathf.Sin(a) * 0.85f), new Vector3(0.25f, 1.05f, 0.25f), Mats["Stone"]);
            }
            Box("Roof", t, new Vector3(0f, 2.35f, 0f), new Vector3(2.2f, 0.25f, 2.2f), Mats["StoneDark"]);
            Prim(PrimitiveType.Sphere, "Orb", t, new Vector3(0f, 1.2f, 0f), Vector3.one * 0.6f, Material(MatDir, "TowerSanctuaryOrb", new Color(1f, 0.85f, 0.4f), 2.5f));
            Assign(tower, ("config", cfg));
            return root;
        }

        // ---------- Системы ----------

        private static GameObject BuildGameSystems(GameBalance balance, GameObject barricade)
        {
            var root = new GameObject("GameSystems");
            var input = root.AddComponent<InputReader>();
            Assign(input, ("actions", InputAsset));
            root.AddComponent<WaveSpawner>();
            var gm = root.AddComponent<GameManager>();
            Assign(gm, ("balance", balance), ("barricadePrefab", barricade.GetComponent<Barricade>()));
            return root;
        }
    }
}
