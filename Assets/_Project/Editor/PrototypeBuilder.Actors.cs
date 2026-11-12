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
            public GameObject Coliseum, Portal, Routes, TowerSlot, BarricadeSpot, Barricade, Orb;
            public GameObject Player, CameraRig, Arrow;
            public GameObject Walker, Shield, Flyer, Boss;
            public GameObject Archer, Catapult, ProjArrow, ProjRock, Explosion;
            public GameObject GameSystems, HUD;
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
            p.TowerSlot = SavePrefab(BuildTowerSlot(), env + "TowerSlot.prefab");
            p.BarricadeSpot = SavePrefab(BuildBarricadeSpot(), env + "BarricadeSpot.prefab");
            p.Barricade = SavePrefab(BuildBarricade(), env + "Barricade.prefab");
            p.Orb = SavePrefab(BuildOrb(), env + "EssenceOrb.prefab");

            p.Arrow = SavePrefab(BuildPlayerArrow(), pl + "Arrow.prefab");
            p.Player = SavePrefab(BuildPlayer(c.Player, p.Arrow), pl + "Player.prefab");
            p.CameraRig = SavePrefab(BuildCameraRig(), pl + "CameraRig.prefab");

            p.Walker = SavePrefab(BuildWalker(c.Walker), en + "Enemy_Walker.prefab");
            p.Shield = SavePrefab(BuildShieldBearer(c.Shield), en + "Enemy_ShieldBearer.prefab");
            p.Flyer = SavePrefab(BuildFlyer(c.Flyer), en + "Enemy_Flyer.prefab");
            p.Boss = SavePrefab(BuildBoss(c.Boss), en + "Enemy_Boss.prefab");

            p.Explosion = SavePrefab(BuildExplosion(), tw + "FX_Explosion.prefab");
            p.ProjArrow = SavePrefab(BuildProjectile("Projectile_Arrow", new Vector3(0.08f, 0.08f, 0.7f), Mats["ArrowTower"], null), tw + "Projectile_Arrow.prefab");
            p.ProjRock = SavePrefab(CatapultIntegration.CreateProjectile(p.Explosion), tw + "Projectile_Rock.prefab");
            p.Archer = SavePrefab(BuildArcherTower(c.Archer, p.ProjArrow), tw + "Tower_Archer.prefab");
            p.Catapult = SavePrefab(BuildCatapultTower(c.Catapult, p.ProjRock), tw + "Tower_Catapult.prefab");

            p.GameSystems = SavePrefab(BuildGameSystems(c.Balance, p.Orb, p.Barricade), PrefabDir + "/Systems/GameSystems.prefab");
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

        private static GameObject BuildPlayer(PlayerConfig config, GameObject arrowPrefab)
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

            var bow = Empty("Bow", holder, new Vector3(-0.1f, 0.1f, 0.15f));
            Box("Grip", bow, Vector3.zero, new Vector3(0.06f, 0.35f, 0.06f), Mats["Wood"]);
            Box("LimbTop", bow, new Vector3(0f, 0.38f, -0.08f), new Vector3(0.05f, 0.45f, 0.05f), Mats["Wood"], false, Quaternion.Euler(-20f, 0f, 0f));
            Box("LimbBottom", bow, new Vector3(0f, -0.38f, -0.08f), new Vector3(0.05f, 0.45f, 0.05f), Mats["Wood"], false, Quaternion.Euler(20f, 0f, 0f));

            var arrowSpawn = Empty("ArrowSpawn", head, new Vector3(0.15f, -0.1f, 0.7f));

            var controller = root.AddComponent<PlayerController>();
            Assign(controller, ("config", config), ("bodyRoot", body.gameObject));

            var cam = root.AddComponent<PlayerCameraController>();
            Assign(cam, ("config", config), ("cameraTarget", cameraTarget), ("head", head));
            AssignArray(cam, "hideInFirstPerson", new Object[] { capsule.GetComponent<Renderer>(), visor.GetComponent<Renderer>() });

            var combat = root.AddComponent<PlayerCombat>();
            Assign(combat, ("config", config), ("swordVisual", sword), ("bowVisual", bow), ("arrowSpawn", arrowSpawn),
                ("arrowPrefab", arrowPrefab.GetComponent<Arrow>()));

            var interactor = root.AddComponent<PlayerInteractor>();
            Assign(interactor, ("config", config));
            return root;
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
            collider.m_CollideAgainst = 1; // слой Default
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

        /// <summary>Общая заготовка наземного врага: капсула, NavMeshAgent, HP-бар.</summary>
        private static (GameObject root, Transform visual, Renderer body, Enemy enemy) GroundEnemyBase(string name, EnemyConfig cfg, Material mat)
        {
            var root = new GameObject(name);
            root.AddComponent<Health>();
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

            var enemy = root.AddComponent<Enemy>();
            var ai = root.AddComponent<GroundEnemyAI>();
            var visual = Empty("Visual", root.transform, Vector3.zero);
            var body = Prim(PrimitiveType.Capsule, "Body", visual, new Vector3(0f, 1f, 0f), Vector3.one, mat);
            Box("Face", visual, new Vector3(0f, 1.5f, 0.42f), new Vector3(0.5f, 0.15f, 0.15f), Mats["Visor"]);
            Assign(ai, ("visualRoot", visual));
            return (root, visual, body.GetComponent<Renderer>(), enemy);
        }

        private static void FinishEnemy(Enemy enemy, EnemyConfig cfg, WorldHealthBar bar, Renderer body, GameObject armor = null)
        {
            Assign(enemy, ("config", cfg), ("healthBar", bar), ("bodyRenderer", body), ("armorVisual", armor));
        }

        private static GameObject BuildWalker(EnemyConfig cfg)
        {
            var (root, visual, body, enemy) = GroundEnemyBase("Enemy_Walker", cfg, Mats["Walker"]);
            Box("Sword", visual, new Vector3(0.55f, 1f, 0.3f), new Vector3(0.08f, 0.08f, 0.9f), Mats["Steel"]);
            var bar = BuildWorldBar(root.transform, 2.5f, 1.2f, false, true, new Color(0.9f, 0.2f, 0.2f));
            FinishEnemy(enemy, cfg, bar, body);
            return root;
        }

        private static GameObject BuildShieldBearer(EnemyConfig cfg)
        {
            var (root, visual, body, enemy) = GroundEnemyBase("Enemy_ShieldBearer", cfg, Mats["ShieldBearer"]);
            Box("Shield", visual, new Vector3(0f, 1f, 0.65f), new Vector3(1.3f, 1.7f, 0.15f), Mats["Shield"]);
            var bar = BuildWorldBar(root.transform, 2.6f, 1.2f, false, true, new Color(0.9f, 0.2f, 0.2f));
            FinishEnemy(enemy, cfg, bar, body);
            return root;
        }

        private static GameObject BuildBoss(EnemyConfig cfg)
        {
            var (root, visual, body, enemy) = GroundEnemyBase("Enemy_Boss", cfg, Mats["Boss"]);
            root.transform.localScale = Vector3.one * 1.8f;
            var armor = Empty("Armor", visual, Vector3.zero);
            Box("Chest", armor, new Vector3(0f, 1.15f, 0.42f), new Vector3(0.95f, 0.8f, 0.2f), Mats["Armor"]);
            Box("ShoulderL", armor, new Vector3(-0.55f, 1.5f, 0f), new Vector3(0.35f, 0.25f, 0.6f), Mats["Armor"]);
            Box("ShoulderR", armor, new Vector3(0.55f, 1.5f, 0f), new Vector3(0.35f, 0.25f, 0.6f), Mats["Armor"]);
            Box("Helmet", armor, new Vector3(0f, 1.85f, 0f), new Vector3(0.7f, 0.3f, 0.7f), Mats["Armor"]);
            Box("HornL", visual, new Vector3(-0.35f, 2.05f, 0.1f), new Vector3(0.1f, 0.4f, 0.1f), Mats["Steel"], false, Quaternion.Euler(0f, 0f, 30f));
            Box("HornR", visual, new Vector3(0.35f, 2.05f, 0.1f), new Vector3(0.1f, 0.4f, 0.1f), Mats["Steel"], false, Quaternion.Euler(0f, 0f, -30f));
            Box("Axe", visual, new Vector3(0.65f, 1.1f, 0.35f), new Vector3(0.1f, 0.1f, 1.2f), Mats["Steel"]);
            var bar = BuildWorldBar(root.transform, 2.6f, 1.4f, true, true, new Color(0.9f, 0.2f, 0.2f));
            FinishEnemy(enemy, cfg, bar, body, armor.gameObject);
            return root;
        }

        private static GameObject BuildFlyer(EnemyConfig cfg)
        {
            var root = new GameObject("Enemy_Flyer");
            root.AddComponent<Health>();
            var col = root.AddComponent<SphereCollider>();
            col.radius = 0.7f;
            var enemy = root.AddComponent<Enemy>();
            var ai = root.AddComponent<FlyingEnemyAI>();
            var visual = Empty("Visual", root.transform, Vector3.zero);
            var body = Prim(PrimitiveType.Sphere, "Body", visual, Vector3.zero, new Vector3(0.9f, 0.6f, 1.3f), Mats["Flyer"]);
            Box("WingL", visual, new Vector3(-0.95f, 0.1f, 0f), new Vector3(1.4f, 0.06f, 0.6f), Mats["Flyer"], false, Quaternion.Euler(0f, 0f, 15f));
            Box("WingR", visual, new Vector3(0.95f, 0.1f, 0f), new Vector3(1.4f, 0.06f, 0.6f), Mats["Flyer"], false, Quaternion.Euler(0f, 0f, -15f));
            Box("Beak", visual, new Vector3(0f, 0f, 0.7f), new Vector3(0.2f, 0.2f, 0.35f), Mats["Visor"]);
            Assign(ai, ("visualRoot", visual));
            var bar = BuildWorldBar(root.transform, 1.1f, 1.2f, false, true, new Color(0.9f, 0.2f, 0.2f));
            FinishEnemy(enemy, cfg, bar, body.GetComponent<Renderer>());
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
            var proj = root.AddComponent<TowerProjectile>();
            Box("Visual", root.transform, Vector3.zero, size, mat);
            if (explosion != null) Assign(proj, ("explosionPrefab", explosion));
            return root;
        }

        private static (GameObject root, Tower tower) TowerBase(string name, Vector3 colliderSize)
        {
            var root = new GameObject(name);
            root.AddComponent<Health>();
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, colliderSize.y * 0.5f, 0f);
            col.size = colliderSize;
            var obstacle = root.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = col.center;
            obstacle.size = colliderSize;
            obstacle.carving = true;
            var tower = root.AddComponent<Tower>();
            Assign(tower, ("hitCollider", col));
            return (root, tower);
        }

        private static GameObject[] LevelMarkers(Transform parent, float z)
        {
            var marks = new GameObject[3];
            for (int i = 0; i < 3; i++)
                marks[i] = Box($"Level_{i + 1}", parent, new Vector3(-0.4f + i * 0.4f, 1f, z), new Vector3(0.25f, 0.25f, 0.08f), Mats["LevelMark"]);
            return marks;
        }

        private const string ArcherArtDir = Root + "/Art/ArcherTowers";
        private static readonly Vector3 ArcherColliderSize = new Vector3(1.6f, 3.63f, 1.66f);

        private static GameObject BuildArcherTower(TowerConfig cfg, GameObject projectile)
        {
            var (root, tower) = TowerBase("Tower_Archer", ArcherColliderSize);
            var t = root.transform;
            var head = Empty("Head", t, new Vector3(0f, 3.8f, 0f));
            var muzzle = Empty("Muzzle", head, new Vector3(0f, 0f, 0.5f));
            var bar = BuildWorldBar(t, 4.15f, 1.6f, false, false, new Color(0.3f, 0.9f, 0.3f));
            Assign(tower, ("config", cfg), ("head", head), ("muzzle", muzzle), ("projectilePrefab", projectile.GetComponent<TowerProjectile>()),
                ("healthBar", bar));
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
            var (root, tower) = TowerBase("Tower_Catapult", CatapultIntegration.ColliderSize);
            var head = Empty("Head", root.transform, Vector3.zero);
            var bar = BuildWorldBar(root.transform, 3f, 1.8f, false, false, new Color(0.3f, 0.9f, 0.3f));
            Assign(tower, ("config", cfg), ("head", head), ("projectilePrefab", projectile.GetComponent<TowerProjectile>()), ("healthBar", bar));
            CatapultIntegration.AddModels(tower, head);
            return root;
        }

        // ---------- Системы ----------

        private static GameObject BuildGameSystems(GameBalance balance, GameObject orb, GameObject barricade)
        {
            var root = new GameObject("GameSystems");
            var input = root.AddComponent<InputReader>();
            Assign(input, ("actions", InputAsset));
            root.AddComponent<WaveSpawner>();
            var gm = root.AddComponent<GameManager>();
            Assign(gm, ("balance", balance), ("orbPrefab", orb.GetComponent<EssenceOrb>()), ("barricadePrefab", barricade.GetComponent<Barricade>()));
            return root;
        }
    }
}
