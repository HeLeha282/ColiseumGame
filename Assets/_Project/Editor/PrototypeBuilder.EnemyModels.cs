using System.Collections.Generic;
using UnityEngine;
using static ColiseumGame.EditorTools.BuilderUtil;

namespace ColiseumGame.EditorTools
{
    /// <summary>
    /// Временные «модели» врагов из примитивов — чтобы на поле сразу было видно, кто есть кто
    /// (скелет, сатир, циклоп, кентавр...). Настоящие модели заменят это позже.
    /// Враг смотрит в +Z, земля — y = 0, рост обычного врага ~2 м (масштаб задаёт EnemyLook.scale).
    /// Ноги/руки/крылья/хвосты собраны на «шарнирах» — их качает EnemyRig.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        private class EnemyModel
        {
            public Renderer body;
            public GameObject armor;
            public readonly List<Transform> legs = new List<Transform>(), arms = new List<Transform>(),
                wings = new List<Transform>(), tails = new List<Transform>();
            public float barY = 2.5f;
            public Vector3 core = new Vector3(0f, 1.1f, 0.4f);
        }

        private static Material EM(string name, Color c, float emission = 0f, float smooth = 0.2f)
            => Material(MatDir, "EM_" + name, c, emission, smooth);

        private static Material Bone => EM("Bone", new Color(0.88f, 0.85f, 0.74f));
        private static Material Hole => EM("Hole", new Color(0.04f, 0.03f, 0.03f));
        private static Material RedCloth => EM("RedCloth", new Color(0.62f, 0.1f, 0.08f));
        private static Material Bronze => EM("Bronze", new Color(0.75f, 0.52f, 0.22f), 0f, 0.6f);
        private static Material Gold => EM("Gold", new Color(1f, 0.8f, 0.3f), 0.35f, 0.7f);
        private static Material Skin => EM("Skin", new Color(0.86f, 0.66f, 0.5f));
        private static Material Fur => EM("Fur", new Color(0.36f, 0.24f, 0.14f));
        private static Material DarkFur => EM("DarkFur", new Color(0.12f, 0.1f, 0.1f));
        private static Material Hoof => EM("Hoof", new Color(0.1f, 0.08f, 0.07f));
        private static Material Horn => EM("Horn", new Color(0.92f, 0.88f, 0.78f));
        private static Material Leather => EM("Leather", new Color(0.4f, 0.26f, 0.15f));
        private static Material EyeGlow(Color c) => EM("Eye_" + ColorUtility.ToHtmlStringRGB(c), c, 2.5f);

        /// <summary>Конечность на шарнире: пустышка в точке крепления, меш свисает вниз на length.</summary>
        private static Transform Limb(string name, Transform parent, Vector3 pivot, float length, float thick, Material m,
            Quaternion? rot = null, PrimitiveType type = PrimitiveType.Cube)
        {
            var p = Empty(name, parent, pivot);
            if (rot.HasValue) p.localRotation = rot.Value;
            if (type == PrimitiveType.Cube) Box(name + "_Mesh", p, new Vector3(0f, -length * 0.5f, 0f), new Vector3(thick, length, thick), m);
            else Prim(type, name + "_Mesh", p, new Vector3(0f, -length * 0.5f, 0f), new Vector3(thick, length * 0.5f, thick), m);
            return p;
        }

        private static Transform Hand(Transform arm, float length) => Empty("Hand", arm, new Vector3(0f, -length, 0f));

        private static Renderer R(GameObject go) => go.GetComponent<Renderer>();

        /// <summary>Базовый гуманоид. Возвращает голову; руки/ноги добавляет в модель.</summary>
        private static Transform Humanoid(EnemyModel m, Transform v, float hip, Vector3 torso, float head, float limb, float armLen,
            Material legMat, Material torsoMat, Material headMat, Material armMat, out Transform handL, out Transform handR)
        {
            m.legs.Add(Limb("LegL", v, new Vector3(-0.17f, hip, 0f), hip, limb, legMat));
            m.legs.Add(Limb("LegR", v, new Vector3(0.17f, hip, 0f), hip, limb, legMat));
            m.body = R(Box("Torso", v, new Vector3(0f, hip + torso.y * 0.5f, 0f), torso, torsoMat));
            float shoulder = hip + torso.y - 0.06f;
            var armL = Limb("ArmL", v, new Vector3(-(torso.x * 0.5f + limb * 0.5f), shoulder, 0f), armLen, limb, armMat);
            var armR = Limb("ArmR", v, new Vector3(torso.x * 0.5f + limb * 0.5f, shoulder, 0f), armLen, limb, armMat);
            m.arms.Add(armL);
            m.arms.Add(armR);
            handL = Hand(armL, armLen);
            handR = Hand(armR, armLen);
            var h = Prim(PrimitiveType.Sphere, "Head", v, new Vector3(0f, hip + torso.y + head * 0.45f, 0f), Vector3.one * head, headMat).transform;
            m.barY = hip + torso.y + head + 0.35f;
            m.core = new Vector3(0f, hip + torso.y * 0.55f, torso.z * 0.5f + 0.05f);
            return h;
        }

        private static void Eyes(Transform head, Material m, float spread = 0.22f, float size = 0.18f, float y = 0.08f)
        {
            Prim(PrimitiveType.Sphere, "EyeL", head, new Vector3(-spread, y, 0.42f), Vector3.one * size, m);
            Prim(PrimitiveType.Sphere, "EyeR", head, new Vector3(spread, y, 0.42f), Vector3.one * size, m);
        }

        private static void Sword(Transform hand, float length = 0.7f)
        {
            Box("Grip", hand, new Vector3(0f, 0f, 0.05f), new Vector3(0.06f, 0.06f, 0.2f), Leather);
            Box("Guard", hand, new Vector3(0f, 0f, 0.16f), new Vector3(0.22f, 0.05f, 0.05f), Bronze);
            Box("Blade", hand, new Vector3(0f, 0f, 0.18f + length * 0.5f), new Vector3(0.09f, 0.025f, length), Mats["Steel"]);
        }

        private static void Spear(Transform hand, float length = 2.2f)
        {
            Prim(PrimitiveType.Cylinder, "Shaft", hand, new Vector3(0f, 0.3f, 0.1f), new Vector3(0.05f, length * 0.5f, 0.05f), Mats["Wood"],
                false, Quaternion.Euler(15f, 0f, 0f));
            Box("Tip", hand, new Vector3(0f, 0.3f + length * 0.48f, 0.1f + length * 0.13f), new Vector3(0.1f, 0.3f, 0.03f), Mats["Steel"],
                false, Quaternion.Euler(15f, 0f, 0f));
        }

        private static void Staff(Transform hand, Material orb, bool crystal)
        {
            Prim(PrimitiveType.Cylinder, "Staff", hand, new Vector3(0f, 0.45f, 0.05f), new Vector3(0.06f, 0.95f, 0.06f), Mats["Wood"]);
            if (crystal)
                Box("Crystal", hand, new Vector3(0f, 1.5f, 0.05f), new Vector3(0.22f, 0.4f, 0.22f), orb, false, Quaternion.Euler(0f, 45f, 15f));
            else
                Prim(PrimitiveType.Sphere, "Orb", hand, new Vector3(0f, 1.5f, 0.05f), Vector3.one * 0.3f, orb);
        }

        /// <summary>Хвост цепочкой сегментов (каждый следующий — дочерний, чтобы извивался).</summary>
        private static void Tail(EnemyModel m, Transform parent, Vector3 start, int count, float seg, float thick, Material mat, float droop = 15f)
        {
            Transform cur = Empty("Tail", parent, start);
            cur.localRotation = Quaternion.Euler(droop, 0f, 0f);
            for (int i = 0; i < count; i++)
            {
                float t = thick * (1f - i * 0.6f / count);
                Box("TailSeg" + i, cur, new Vector3(0f, 0f, -seg * 0.5f), new Vector3(t, t, seg), mat);
                m.tails.Add(cur);
                if (i < count - 1)
                {
                    var next = Empty("TailJoint" + i, cur, new Vector3(0f, 0f, -seg));
                    next.localRotation = Quaternion.Euler(droop * 0.4f, 0f, 0f);
                    cur = next;
                }
            }
        }

        /// <summary>4 ноги четвероногого: передняя Л/П, задняя Л/П.</summary>
        private static void FourLegs(EnemyModel m, Transform v, float y, float halfW, float front, float back, float length, float thick, Material mat, Material foot = null)
        {
            var pos = new[] { new Vector3(-halfW, y, front), new Vector3(halfW, y, front), new Vector3(-halfW, y, back), new Vector3(halfW, y, back) };
            string[] names = { "LegFL", "LegFR", "LegBL", "LegBR" };
            for (int i = 0; i < 4; i++)
            {
                var leg = Limb(names[i], v, pos[i], length, thick, mat);
                if (foot != null) Box("Foot", leg, new Vector3(0f, -length + 0.05f, 0.04f), new Vector3(thick * 1.15f, 0.1f, thick * 1.3f), foot);
                m.legs.Add(leg);
            }
        }

        private static void Wings(EnemyModel m, Transform v, Vector3 root, float span, float depth, Material mat, Material tipMat)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var w = Empty(side < 0 ? "WingL" : "WingR", v, new Vector3(root.x * side, root.y, root.z));
                w.localRotation = Quaternion.Euler(0f, 0f, side * 12f);
                Box("Wing", w, new Vector3(side * span * 0.5f, 0f, 0f), new Vector3(span, 0.05f, depth), mat);
                Box("Tip", w, new Vector3(side * span * 0.95f, 0f, -depth * 0.25f), new Vector3(span * 0.35f, 0.04f, depth * 0.75f), tipMat,
                    false, Quaternion.Euler(0f, side * 20f, 0f));
                m.wings.Add(w);
            }
        }

        // ================================================================ модели

        private static EnemyModel BuildEnemyModel(EnemyLook look, Transform v)
        {
            var m = new EnemyModel();
            var main = Material(MatDir, "Enemy_" + look.key, look.color);
            switch (look.key)
            {
                case "Walker": Skeleton(m, v, main); break;
                case "Satyr": Satyr(m, v, main); break;
                case "Cyclops": Cyclops(m, v, main); break;
                case "ShieldBearer": Legionary(m, v, main, false); break;
                case "Testudo": Legionary(m, v, main, true); break;
                case "Pyromancer": Mage(m, v, main, EyeGlow(new Color(1f, 0.45f, 0.1f)), false); break;
                case "Cryomancer": Mage(m, v, main, EyeGlow(new Color(0.5f, 0.85f, 1f)), true); break;
                case "Centaur": Centaur(m, v, main); break;
                case "Priest": Priest(m, v, main); break;
                case "Lemur": Ghost(m, v, main); break;
                case "Salamander": Salamander(m, v, main); break;
                case "IceGolem": Golem(m, v, main, EyeGlow(new Color(0.55f, 0.9f, 1f)), false); break;
                case "Harpy": Harpy(m, v, main, false); break;
                case "HarpyQueen": Harpy(m, v, main, true); break;
                case "Stymphalian": Bird(m, v, main); break;
                case "HydralingSmall": Serpent(m, v, main, 1); break;
                case "Hydraling": Serpent(m, v, main, 2); break;
                case "Minotaur": Minotaur(m, v, main); break;
                case "Cerberus": Hound(m, v, main); break;
                case "Hydra": HydraBody(m, v, main); break;
                case "Medusa": Gorgon(m, v, main); break;
                case "Titan": Golem(m, v, main, EyeGlow(new Color(1f, 0.5f, 0.15f)), true); break;
                default:
                    m.body = R(Prim(PrimitiveType.Capsule, "Body", v, new Vector3(0f, 1f, 0f), Vector3.one, main));
                    break;
            }
            return m;
        }

        // Скелет-легионер: кости, рёбра, череп с пустыми глазницами, бронзовый шлем с красным гребнем, гладий.
        private static void Skeleton(EnemyModel m, Transform v, Material main)
        {
            m.legs.Add(Limb("LegL", v, new Vector3(-0.15f, 0.92f, 0f), 0.92f, 0.09f, Bone));
            m.legs.Add(Limb("LegR", v, new Vector3(0.15f, 0.92f, 0f), 0.92f, 0.09f, Bone));
            Prim(PrimitiveType.Cylinder, "Skirt", v, new Vector3(0f, 0.9f, 0f), new Vector3(0.55f, 0.13f, 0.42f), RedCloth);
            m.body = R(Box("Spine", v, new Vector3(0f, 1.27f, -0.08f), new Vector3(0.1f, 0.72f, 0.1f), Bone));
            for (int i = 0; i < 4; i++)
                Box("Rib" + i, v, new Vector3(0f, 1.18f + i * 0.13f, 0f), new Vector3(0.46f - i * 0.02f, 0.05f, 0.28f), Bone);
            Box("Shoulders", v, new Vector3(0f, 1.62f, -0.02f), new Vector3(0.6f, 0.07f, 0.12f), Bone);
            var armL = Limb("ArmL", v, new Vector3(-0.32f, 1.6f, 0f), 0.72f, 0.07f, Bone);
            var armR = Limb("ArmR", v, new Vector3(0.32f, 1.6f, 0f), 0.72f, 0.07f, Bone, Quaternion.Euler(-35f, 0f, 0f));
            m.arms.Add(armL); m.arms.Add(armR);
            Sword(Hand(armR, 0.72f), 0.6f);
            var skull = Prim(PrimitiveType.Sphere, "Skull", v, new Vector3(0f, 1.86f, 0.02f), new Vector3(0.36f, 0.38f, 0.38f), Bone).transform;
            Box("SocketL", skull, new Vector3(-0.2f, 0.05f, 0.42f), new Vector3(0.24f, 0.2f, 0.12f), Hole);
            Box("SocketR", skull, new Vector3(0.2f, 0.05f, 0.42f), new Vector3(0.24f, 0.2f, 0.12f), Hole);
            Box("Jaw", skull, new Vector3(0f, -0.38f, 0.22f), new Vector3(0.6f, 0.2f, 0.5f), Bone);
            Prim(PrimitiveType.Sphere, "Helmet", v, new Vector3(0f, 1.95f, 0f), new Vector3(0.42f, 0.26f, 0.44f), Bronze);
            Box("Crest", v, new Vector3(0f, 2.12f, -0.02f), new Vector3(0.06f, 0.16f, 0.46f), RedCloth);
            m.barY = 2.45f;
            m.core = new Vector3(0f, 1.35f, 0.18f);
        }

        // Сатир: козлиные ноги коленом назад с копытами, мохнатые бёдра, рога, бородка, наклон вперёд.
        private static void Satyr(EnemyModel m, Transform v, Material main)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var thigh = Limb(side < 0 ? "LegL" : "LegR", v, new Vector3(0.16f * side, 1.0f, 0f), 0.5f, 0.2f, main, Quaternion.Euler(-30f, 0f, 0f));
                var shin = Limb("Shin", thigh, new Vector3(0f, -0.5f, 0f), 0.52f, 0.13f, main, Quaternion.Euler(55f, 0f, 0f));
                Box("Hoof", shin, new Vector3(0f, -0.55f, 0.04f), new Vector3(0.15f, 0.1f, 0.2f), Hoof);
                m.legs.Add(thigh);
            }
            Prim(PrimitiveType.Sphere, "FurHips", v, new Vector3(0f, 1.02f, 0f), new Vector3(0.55f, 0.35f, 0.42f), main);
            var torso = Empty("Upper", v, new Vector3(0f, 1.05f, 0f));
            torso.localRotation = Quaternion.Euler(14f, 0f, 0f);
            m.body = R(Box("Torso", torso, new Vector3(0f, 0.32f, 0f), new Vector3(0.5f, 0.6f, 0.3f), Skin));
            var armL = Limb("ArmL", torso, new Vector3(-0.31f, 0.58f, 0f), 0.6f, 0.11f, Skin);
            var armR = Limb("ArmR", torso, new Vector3(0.31f, 0.58f, 0f), 0.6f, 0.11f, Skin, Quaternion.Euler(-25f, 0f, 0f));
            m.arms.Add(armL); m.arms.Add(armR);
            Box("Dagger", Hand(armR, 0.6f), new Vector3(0f, 0f, 0.18f), new Vector3(0.05f, 0.03f, 0.35f), Mats["Steel"]);
            var head = Prim(PrimitiveType.Sphere, "Head", torso, new Vector3(0f, 0.8f, 0.05f), Vector3.one * 0.34f, Skin).transform;
            Box("Beard", head, new Vector3(0f, -0.45f, 0.35f), new Vector3(0.35f, 0.45f, 0.25f), main);
            Box("EarL", head, new Vector3(-0.55f, 0.05f, 0f), new Vector3(0.35f, 0.12f, 0.08f), Skin, false, Quaternion.Euler(0f, 0f, 20f));
            Box("EarR", head, new Vector3(0.55f, 0.05f, 0f), new Vector3(0.35f, 0.12f, 0.08f), Skin, false, Quaternion.Euler(0f, 0f, -20f));
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Horn", head, new Vector3(0.25f * side, 0.6f, -0.1f), new Vector3(0.14f, 0.5f, 0.14f), Hoof, false, Quaternion.Euler(-35f, 0f, -15f * side));
                Box("HornTip", head, new Vector3(0.32f * side, 0.85f, -0.45f), new Vector3(0.11f, 0.35f, 0.11f), Hoof, false, Quaternion.Euler(-100f, 0f, -15f * side));
            }
            Eyes(head, EyeGlow(new Color(1f, 0.85f, 0.2f)), 0.2f, 0.14f);
            m.barY = 2.3f;
            m.core = new Vector3(0f, 1.35f, 0.25f);
        }

        // Циклоп: толстое туловище-шар, один огромный глаз, набедренная повязка, дубина.
        private static void Cyclops(EnemyModel m, Transform v, Material main)
        {
            m.legs.Add(Limb("LegL", v, new Vector3(-0.25f, 0.75f, 0f), 0.75f, 0.3f, main));
            m.legs.Add(Limb("LegR", v, new Vector3(0.25f, 0.75f, 0f), 0.75f, 0.3f, main));
            Box("Loincloth", v, new Vector3(0f, 0.8f, 0.05f), new Vector3(0.75f, 0.35f, 0.6f), Leather);
            m.body = R(Prim(PrimitiveType.Sphere, "Belly", v, new Vector3(0f, 1.3f, 0f), new Vector3(1.05f, 1.0f, 0.85f), main));
            var armL = Limb("ArmL", v, new Vector3(-0.6f, 1.6f, 0f), 0.95f, 0.26f, main, Quaternion.Euler(0f, 0f, -12f));
            var armR = Limb("ArmR", v, new Vector3(0.6f, 1.6f, 0f), 0.95f, 0.26f, main, Quaternion.Euler(-20f, 0f, 12f));
            m.arms.Add(armL); m.arms.Add(armR);
            var hand = Hand(armR, 0.95f);
            Prim(PrimitiveType.Cylinder, "Club", hand, new Vector3(0f, 0.1f, 0.45f), new Vector3(0.12f, 0.5f, 0.12f), Mats["Wood"], false, Quaternion.Euler(80f, 0f, 0f));
            Prim(PrimitiveType.Sphere, "ClubHead", hand, new Vector3(0f, 0.18f, 0.95f), new Vector3(0.32f, 0.32f, 0.45f), Mats["Wood"]);
            var head = Prim(PrimitiveType.Sphere, "Head", v, new Vector3(0f, 1.95f, 0.08f), Vector3.one * 0.55f, main).transform;
            Prim(PrimitiveType.Sphere, "Eye", head, new Vector3(0f, 0.08f, 0.38f), new Vector3(0.6f, 0.6f, 0.4f), EM("EyeWhite", new Color(1f, 0.97f, 0.9f), 0.4f));
            Prim(PrimitiveType.Sphere, "Pupil", head, new Vector3(0f, 0.08f, 0.56f), Vector3.one * 0.22f, Hole);
            Box("Brow", head, new Vector3(0f, 0.42f, 0.38f), new Vector3(0.75f, 0.12f, 0.2f), DarkFur);
            m.barY = 2.55f;
            m.core = new Vector3(0f, 1.3f, 0.45f);
        }

        // Легионер-щитоносец (и «черепаха»-тестудо): туника, бронзовая лорика, шлем, большой щит-скутум, копьё.
        private static void Legionary(EnemyModel m, Transform v, Material main, bool testudo)
        {
            var head = Humanoid(m, v, 0.9f, new Vector3(0.58f, 0.72f, 0.34f), 0.36f, 0.15f, 0.72f, Leather, main, Skin, Skin, out var handL, out var handR);
            Box("Lorica", v, new Vector3(0f, 1.32f, 0.03f), new Vector3(0.62f, 0.5f, 0.36f), Bronze);
            Prim(PrimitiveType.Cylinder, "Skirt", v, new Vector3(0f, 0.92f, 0f), new Vector3(0.62f, 0.12f, 0.45f), main);
            Prim(PrimitiveType.Sphere, "Helmet", head, new Vector3(0f, 0.25f, 0f), new Vector3(1.15f, 0.8f, 1.15f), Bronze);
            Box("Crest", head, new Vector3(0f, 0.75f, 0f), new Vector3(0.15f, 0.4f, 1.1f), testudo ? Gold : RedCloth);
            Eyes(head, Hole, 0.2f, 0.12f);
            // Щит держится перед телом (а не в руке) — он работает спереди и должен быть там всегда.
            var shieldMat = EM("Scutum", main.color * 0.9f);
            var shield = Box("Scutum", v, new Vector3(0f, 1.15f, 0.48f), testudo ? new Vector3(1.3f, 1.8f, 0.12f) : new Vector3(0.95f, 1.45f, 0.12f), shieldMat);
            Prim(PrimitiveType.Sphere, "Boss", shield.transform, new Vector3(0f, 0f, 0.6f), new Vector3(0.25f, 0.18f, 1.2f), Gold);
            Box("RimTop", shield.transform, new Vector3(0f, 0.48f, 0.5f), new Vector3(1f, 0.04f, 0.3f), Gold);
            Box("RimBottom", shield.transform, new Vector3(0f, -0.48f, 0.5f), new Vector3(1f, 0.04f, 0.3f), Gold);
            if (testudo)
                Box("RoofShield", v, new Vector3(0f, 2.25f, 0.1f), new Vector3(1.3f, 0.1f, 1.2f), shieldMat);
            else
                Spear(handR);
            m.barY = testudo ? 2.65f : 2.45f;
            m.core = new Vector3(0f, 1.35f, 0.6f);
        }

        // Маг (пиромант/криомант): длинная мантия-конус, капюшон с горящими глазами, посох со стихией.
        private static void Mage(EnemyModel m, Transform v, Material main, Material glow, bool ice)
        {
            Prim(PrimitiveType.Cylinder, "RobeLow", v, new Vector3(0f, 0.4f, 0f), new Vector3(0.95f, 0.4f, 0.95f), main);
            Prim(PrimitiveType.Cylinder, "RobeMid", v, new Vector3(0f, 0.95f, 0f), new Vector3(0.72f, 0.3f, 0.72f), main);
            m.body = R(Box("Torso", v, new Vector3(0f, 1.42f, 0f), new Vector3(0.55f, 0.55f, 0.36f), main));
            Box("Sash", v, new Vector3(0f, 1.2f, 0.01f), new Vector3(0.58f, 0.08f, 0.38f), ice ? EM("IceTrim", new Color(0.85f, 0.95f, 1f)) : Gold);
            var armL = Limb("ArmL", v, new Vector3(-0.36f, 1.62f, 0f), 0.62f, 0.17f, main, Quaternion.Euler(-15f, 0f, 0f));
            var armR = Limb("ArmR", v, new Vector3(0.36f, 1.62f, 0f), 0.62f, 0.17f, main, Quaternion.Euler(-30f, 0f, 0f));
            m.arms.Add(armL); m.arms.Add(armR);
            Prim(PrimitiveType.Sphere, "HandGlow", Hand(armL, 0.62f), new Vector3(0f, 0f, 0.05f), Vector3.one * 0.18f, glow);
            Staff(Hand(armR, 0.62f), glow, ice);
            var hood = Prim(PrimitiveType.Sphere, "Hood", v, new Vector3(0f, 1.88f, -0.02f), new Vector3(0.44f, 0.46f, 0.46f), main).transform;
            Box("HoodTip", hood, new Vector3(0f, 0.55f, -0.25f), new Vector3(0.35f, 0.5f, 0.35f), main, false, Quaternion.Euler(-35f, 45f, 0f));
            Prim(PrimitiveType.Sphere, "Face", hood, new Vector3(0f, -0.05f, 0.3f), new Vector3(0.7f, 0.7f, 0.5f), Hole);
            Eyes(hood, glow, 0.15f, 0.12f, 0f);
            if (ice)
                for (int side = -1; side <= 1; side += 2)
                    Box("Spike", v, new Vector3(0.38f * side, 1.78f, 0f), new Vector3(0.12f, 0.35f, 0.12f), glow, false, Quaternion.Euler(0f, 45f, -25f * side));
            m.barY = 2.45f;
            m.core = new Vector3(0f, 1.42f, 0.22f);
        }

        // Кентавр: конское тело на 4 ногах с хвостом, человеческий торс спереди, лук и колчан.
        private static void Centaur(EnemyModel m, Transform v, Material main)
        {
            m.body = R(Box("HorseBody", v, new Vector3(0f, 1.15f, -0.15f), new Vector3(0.7f, 0.65f, 1.5f), main));
            Prim(PrimitiveType.Sphere, "Rump", v, new Vector3(0f, 1.15f, -0.85f), new Vector3(0.72f, 0.68f, 0.5f), main);
            FourLegs(m, v, 0.9f, 0.24f, 0.45f, -0.75f, 0.9f, 0.16f, main, Hoof);
            Tail(m, v, new Vector3(0f, 1.35f, -1.05f), 3, 0.28f, 0.14f, DarkFur, 40f);
            var upper = Empty("Upper", v, new Vector3(0f, 1.4f, 0.5f));
            Box("Torso", upper, new Vector3(0f, 0.38f, 0f), new Vector3(0.52f, 0.7f, 0.3f), Skin);
            var armL = Limb("ArmL", upper, new Vector3(-0.33f, 0.68f, 0f), 0.62f, 0.12f, Skin, Quaternion.Euler(-80f, 0f, 0f));
            var armR = Limb("ArmR", upper, new Vector3(0.33f, 0.68f, 0f), 0.55f, 0.12f, Skin, Quaternion.Euler(-70f, 0f, 20f));
            var bowHand = Hand(armL, 0.62f);
            Box("BowMid", bowHand, Vector3.zero, new Vector3(0.06f, 0.06f, 0.35f), Mats["Wood"], false, Quaternion.Euler(90f, 0f, 0f));
            Box("BowTop", bowHand, new Vector3(0f, 0.06f, 0.3f), new Vector3(0.05f, 0.05f, 0.35f), Mats["Wood"], false, Quaternion.Euler(120f, 0f, 0f));
            Box("BowBottom", bowHand, new Vector3(0f, 0.06f, -0.3f), new Vector3(0.05f, 0.05f, 0.35f), Mats["Wood"], false, Quaternion.Euler(60f, 0f, 0f));
            m.arms.Add(armL); m.arms.Add(armR);
            var head = Prim(PrimitiveType.Sphere, "Head", upper, new Vector3(0f, 0.92f, 0f), Vector3.one * 0.34f, Skin).transform;
            Box("Hair", head, new Vector3(0f, 0.2f, -0.2f), new Vector3(1.05f, 0.8f, 0.8f), DarkFur);
            Prim(PrimitiveType.Cylinder, "Quiver", upper, new Vector3(0.15f, 0.45f, -0.22f), new Vector3(0.16f, 0.3f, 0.16f), Leather, false, Quaternion.Euler(-15f, 0f, -20f));
            m.barY = 2.85f;
            m.core = new Vector3(0f, 1.8f, 0.68f);
        }

        // Жрец-лекарь: белые одежды, золотой нимб из кусочков, посох с солнечным диском.
        private static void Priest(EnemyModel m, Transform v, Material main)
        {
            Prim(PrimitiveType.Cylinder, "Robe", v, new Vector3(0f, 0.55f, 0f), new Vector3(0.8f, 0.55f, 0.8f), main);
            var head = Humanoid(m, v, 1.0f, new Vector3(0.55f, 0.62f, 0.34f), 0.34f, 0.15f, 0.62f, main, main, Skin, main, out var handL, out var handR);
            Box("Stole", v, new Vector3(0f, 1.3f, 0.18f), new Vector3(0.14f, 0.62f, 0.04f), Gold);
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                Box("Halo" + i, v, new Vector3(Mathf.Cos(a) * 0.32f, 2.25f, Mathf.Sin(a) * 0.32f - 0.05f), new Vector3(0.12f, 0.04f, 0.12f), Gold);
            }
            Prim(PrimitiveType.Cylinder, "Staff", handR, new Vector3(0f, 0.45f, 0.05f), new Vector3(0.06f, 0.95f, 0.06f), Gold);
            Prim(PrimitiveType.Cylinder, "SunDisk", handR, new Vector3(0f, 1.5f, 0.05f), new Vector3(0.45f, 0.03f, 0.45f), Gold, false, Quaternion.Euler(90f, 0f, 0f));
            Eyes(head, Hole, 0.18f, 0.1f);
            m.barY = 2.55f;
        }

        // Лемур-призрак: парящий балахон, исчезающий «хвост» из шаров, светящиеся глаза, длинные руки.
        private static void Ghost(EnemyModel m, Transform v, Material main)
        {
            m.body = R(Prim(PrimitiveType.Sphere, "Robe", v, new Vector3(0f, 1.35f, 0f), new Vector3(0.75f, 0.95f, 0.6f), main));
            var wisp = Empty("Wisp", v, new Vector3(0f, 0.9f, -0.05f));
            Prim(PrimitiveType.Sphere, "W1", wisp, new Vector3(0f, -0.15f, -0.05f), Vector3.one * 0.5f, main);
            Prim(PrimitiveType.Sphere, "W2", wisp, new Vector3(0f, -0.45f, -0.18f), Vector3.one * 0.32f, main);
            Prim(PrimitiveType.Sphere, "W3", wisp, new Vector3(0f, -0.68f, -0.32f), Vector3.one * 0.16f, main);
            m.tails.Add(wisp);
            var armL = Limb("ArmL", v, new Vector3(-0.35f, 1.65f, 0f), 0.85f, 0.09f, main, Quaternion.Euler(-70f, 0f, 0f));
            var armR = Limb("ArmR", v, new Vector3(0.35f, 1.65f, 0f), 0.85f, 0.09f, main, Quaternion.Euler(-70f, 0f, 0f));
            m.arms.Add(armL); m.arms.Add(armR);
            foreach (var arm in new[] { armL, armR })
                for (int f = -1; f <= 1; f++)
                    Box("Claw", Hand(arm, 0.85f), new Vector3(f * 0.05f, -0.1f, 0f), new Vector3(0.025f, 0.22f, 0.025f), Hole);
            var hood = Prim(PrimitiveType.Sphere, "Hood", v, new Vector3(0f, 2.0f, 0f), new Vector3(0.48f, 0.5f, 0.48f), main).transform;
            Prim(PrimitiveType.Sphere, "Face", hood, new Vector3(0f, -0.05f, 0.3f), new Vector3(0.7f, 0.7f, 0.5f), Hole);
            Eyes(hood, EyeGlow(new Color(0.6f, 1f, 0.7f)), 0.16f, 0.13f, 0f);
            m.barY = 2.55f;
            m.core = new Vector3(0f, 1.4f, 0.3f);
        }

        // Огненная саламандра: низкая ящерица на расставленных лапах, длинный хвост, огненный гребень.
        private static void Salamander(EnemyModel m, Transform v, Material main)
        {
            m.body = R(Box("Body", v, new Vector3(0f, 0.42f, 0f), new Vector3(0.6f, 0.38f, 1.3f), main));
            var head = Box("Head", v, new Vector3(0f, 0.48f, 0.85f), new Vector3(0.45f, 0.28f, 0.5f), main).transform;
            Box("Jaw", head, new Vector3(0f, -0.45f, 0.1f), new Vector3(0.9f, 0.3f, 0.85f), EM("SalamanderBelly", new Color(1f, 0.8f, 0.3f)));
            Eyes(head, EyeGlow(new Color(1f, 0.9f, 0.2f)), 0.35f, 0.22f, 0.3f);
            var legPos = new[] { new Vector3(-0.32f, 0.36f, 0.45f), new Vector3(0.32f, 0.36f, 0.45f), new Vector3(-0.32f, 0.36f, -0.45f), new Vector3(0.32f, 0.36f, -0.45f) };
            for (int i = 0; i < 4; i++)
                m.legs.Add(Limb("Leg" + i, v, legPos[i], 0.4f, 0.12f, main, Quaternion.Euler(0f, 0f, legPos[i].x < 0 ? -40f : 40f)));
            Tail(m, v, new Vector3(0f, 0.42f, -0.65f), 4, 0.3f, 0.26f, main, 6f);
            var flame = EyeGlow(new Color(1f, 0.4f, 0.05f));
            for (int i = 0; i < 4; i++)
                Box("Flame" + i, v, new Vector3(0f, 0.7f, 0.45f - i * 0.3f), new Vector3(0.1f, 0.22f - i * 0.03f, 0.18f), flame, false, Quaternion.Euler(-20f, 0f, 0f));
            m.barY = 1.35f;
            m.core = new Vector3(0f, 0.45f, 0.66f);
        }

        // Голем (ледяной голем и Титан): глыбы-кубы, огромные кулаки, светящиеся глаза-щель, кристаллы/трещины.
        private static void Golem(EnemyModel m, Transform v, Material main, Material glow, bool titan)
        {
            m.legs.Add(Limb("LegL", v, new Vector3(-0.28f, 0.85f, 0f), 0.85f, 0.38f, main));
            m.legs.Add(Limb("LegR", v, new Vector3(0.28f, 0.85f, 0f), 0.85f, 0.38f, main));
            m.body = R(Box("Torso", v, new Vector3(0f, 1.35f, 0f), new Vector3(1.1f, 0.95f, 0.75f), main));
            Box("Belly", v, new Vector3(0f, 0.95f, 0.02f), new Vector3(0.8f, 0.3f, 0.6f), main);
            for (int side = -1; side <= 1; side += 2)
            {
                var arm = Limb(side < 0 ? "ArmL" : "ArmR", v, new Vector3(0.75f * side, 1.7f, 0f), 0.9f, 0.34f, main, Quaternion.Euler(0f, 0f, 8f * side));
                Box("Fist", arm, new Vector3(0f, -0.95f, 0.04f), new Vector3(0.5f, 0.45f, 0.5f), main);
                m.arms.Add(arm);
            }
            var head = Box("Head", v, new Vector3(0f, 2.0f, 0.08f), new Vector3(0.45f, 0.4f, 0.42f), main).transform;
            Box("EyeSlit", head, new Vector3(0f, 0.08f, 0.5f), new Vector3(0.75f, 0.14f, 0.1f), glow);
            if (!titan)
            {
                for (int i = 0; i < 5; i++)
                    Box("Crystal" + i, v, new Vector3(-0.4f + i * 0.2f, 1.9f + (i % 2) * 0.15f, -0.3f), new Vector3(0.14f, 0.45f, 0.14f), glow,
                        false, Quaternion.Euler(-25f, 45f, (i - 2) * 12f));
                m.barY = 2.55f;
                m.core = new Vector3(0f, 1.35f, 0.42f);
                return;
            }
            // Титан: магмовые трещины и снимаемая бронза (наплечники, нагрудник, шлем с гребнем)
            for (int i = 0; i < 3; i++)
                Box("Crack" + i, v, new Vector3(-0.25f + i * 0.25f, 1.3f + (i % 2) * 0.2f, 0.38f), new Vector3(0.05f, 0.55f, 0.03f), glow,
                    false, Quaternion.Euler(0f, 0f, (i - 1) * 25f));
            var armor = Empty("Armor", v, Vector3.zero);
            Box("Chest", armor, new Vector3(0f, 1.45f, 0.4f), new Vector3(1.0f, 0.6f, 0.12f), Bronze);
            Box("PauldronL", armor, new Vector3(-0.72f, 1.88f, 0f), new Vector3(0.55f, 0.25f, 0.65f), Bronze, false, Quaternion.Euler(0f, 0f, 15f));
            Box("PauldronR", armor, new Vector3(0.72f, 1.88f, 0f), new Vector3(0.55f, 0.25f, 0.65f), Bronze, false, Quaternion.Euler(0f, 0f, -15f));
            Box("Helm", armor, new Vector3(0f, 2.18f, 0.06f), new Vector3(0.52f, 0.22f, 0.5f), Bronze);
            Box("HelmCrest", armor, new Vector3(0f, 2.38f, 0f), new Vector3(0.1f, 0.25f, 0.6f), RedCloth);
            m.armor = armor.gameObject;
            m.barY = 2.75f;
            m.core = new Vector3(0f, 1.3f, 0.5f);
        }

        // Гарпия (и Королева гарпий): женский торс, большие крылья, птичьи лапы с когтями, перья-хвост.
        private static void Harpy(EnemyModel m, Transform v, Material main, bool queen)
        {
            m.body = R(Box("Torso", v, new Vector3(0f, 0.15f, 0f), new Vector3(0.38f, 0.5f, 0.24f), Skin));
            Prim(PrimitiveType.Sphere, "Feathers", v, new Vector3(0f, -0.2f, -0.02f), new Vector3(0.48f, 0.42f, 0.42f), main);
            var head = Prim(PrimitiveType.Sphere, "Head", v, new Vector3(0f, 0.58f, 0.02f), Vector3.one * 0.3f, Skin).transform;
            Box("Hair", head, new Vector3(0f, 0.15f, -0.35f), new Vector3(1.0f, 1.0f, 0.9f), main);
            Eyes(head, EyeGlow(new Color(1f, 0.3f, 0.3f)), 0.2f, 0.15f);
            Wings(m, v, new Vector3(0.18f, 0.32f, -0.08f), 1.15f, 0.6f, main, EM("WingTip", main.color * 0.6f));
            for (int side = -1; side <= 1; side += 2)
            {
                var leg = Limb(side < 0 ? "LegL" : "LegR", v, new Vector3(0.12f * side, -0.35f, 0f), 0.4f, 0.06f, Hoof);
                for (int c = -1; c <= 1; c++)
                    Box("Talon", leg, new Vector3(c * 0.06f, -0.42f, 0.06f), new Vector3(0.03f, 0.03f, 0.16f), Hoof);
                m.legs.Add(leg);
            }
            Tail(m, v, new Vector3(0f, -0.25f, -0.2f), 2, 0.3f, 0.3f, main, 30f);
            if (queen)
            {
                for (int i = 0; i < 6; i++)
                {
                    float a = i * Mathf.PI * 2f / 6f;
                    Box("Crown" + i, head, new Vector3(Mathf.Cos(a) * 0.4f, 0.55f, Mathf.Sin(a) * 0.4f), new Vector3(0.14f, 0.35f, 0.14f), Gold);
                }
                Box("Necklace", v, new Vector3(0f, 0.36f, 0.12f), new Vector3(0.36f, 0.06f, 0.04f), Gold);
            }
            m.barY = 1.1f;
            m.core = new Vector3(0f, 0.15f, 0.2f);
        }

        // Стимфалийская птица: медная птица с длинным клювом и веером хвоста.
        private static void Bird(EnemyModel m, Transform v, Material main)
        {
            m.body = R(Prim(PrimitiveType.Sphere, "Body", v, Vector3.zero, new Vector3(0.45f, 0.4f, 0.75f), Bronze));
            var head = Prim(PrimitiveType.Sphere, "Head", v, new Vector3(0f, 0.18f, 0.42f), Vector3.one * 0.28f, Bronze).transform;
            Box("Beak", head, new Vector3(0f, -0.1f, 0.85f), new Vector3(0.22f, 0.22f, 1.2f), Mats["Steel"]);
            Eyes(head, EyeGlow(new Color(1f, 0.25f, 0.1f)), 0.3f, 0.25f, 0.15f);
            Wings(m, v, new Vector3(0.15f, 0.08f, 0f), 0.75f, 0.38f, main, Bronze);
            for (int i = -1; i <= 1; i++)
                Box("TailFeather", v, new Vector3(i * 0.08f, 0.05f, -0.48f), new Vector3(0.1f, 0.03f, 0.35f), main, false, Quaternion.Euler(-10f, i * 25f, 0f));
            m.barY = 0.75f;
            m.core = new Vector3(0f, 0.1f, 0.35f);
        }

        // Гидрёныш: приземистое змеиное тело на коротких лапах, одна-две шеи с головами, хвост.
        private static void Serpent(EnemyModel m, Transform v, Material main, int heads)
        {
            m.body = R(Prim(PrimitiveType.Sphere, "Body", v, new Vector3(0f, 0.65f, 0f), new Vector3(0.85f, 0.7f, 1.2f), main));
            FourLegs(m, v, 0.45f, 0.3f, 0.3f, -0.3f, 0.45f, 0.15f, main);
            Tail(m, v, new Vector3(0f, 0.6f, -0.55f), 3, 0.3f, 0.25f, main, 10f);
            var eye = EyeGlow(new Color(1f, 0.9f, 0.2f));
            for (int i = 0; i < heads; i++)
            {
                float yaw = heads == 1 ? 0f : (i == 0 ? -22f : 22f);
                var neck = Empty("Neck" + i, v, new Vector3(0f, 0.85f, 0.4f));
                neck.localRotation = Quaternion.Euler(-40f, yaw, 0f);
                Prim(PrimitiveType.Cylinder, "NeckMesh", neck, new Vector3(0f, 0f, 0.35f), new Vector3(0.2f, 0.35f, 0.2f), main, false, Quaternion.Euler(90f, 0f, 0f));
                var head = Box("Head", neck, new Vector3(0f, 0.02f, 0.78f), new Vector3(0.3f, 0.22f, 0.4f), main, false, Quaternion.Euler(40f, 0f, 0f)).transform;
                Eyes(head, eye, 0.32f, 0.25f, 0.3f);
                Box("Fang", head, new Vector3(0f, -0.55f, 0.35f), new Vector3(0.6f, 0.15f, 0.3f), Horn);
            }
            for (int i = 0; i < 3; i++)
                Box("Spike" + i, v, new Vector3(0f, 1.0f, 0.2f - i * 0.3f), new Vector3(0.08f, 0.2f, 0.12f), EM("SerpentSpike", main.color * 0.5f));
            m.barY = 1.65f;
            m.core = new Vector3(0f, 0.7f, 0.55f);
        }

        // Минотавр: копыта и мохнатые ноги, мощный торс, бычья голова с рогами и кольцом в носу, двуручный топор, броня.
        private static void Minotaur(EnemyModel m, Transform v, Material main)
        {
            m.legs.Add(Limb("LegL", v, new Vector3(-0.22f, 0.9f, 0f), 0.9f, 0.26f, Fur));
            m.legs.Add(Limb("LegR", v, new Vector3(0.22f, 0.9f, 0f), 0.9f, 0.26f, Fur));
            foreach (var leg in m.legs) Box("Hoof", leg, new Vector3(0f, -0.88f, 0.04f), new Vector3(0.3f, 0.14f, 0.34f), Hoof);
            Box("Loincloth", v, new Vector3(0f, 0.9f, 0.02f), new Vector3(0.7f, 0.3f, 0.5f), Leather);
            m.body = R(Box("Torso", v, new Vector3(0f, 1.38f, 0f), new Vector3(0.85f, 0.8f, 0.48f), main));
            for (int side = -1; side <= 1; side += 2)
            {
                var arm = Limb(side < 0 ? "ArmL" : "ArmR", v, new Vector3(0.55f * side, 1.7f, 0f), 0.85f, 0.24f, main, Quaternion.Euler(-30f, 0f, 0f));
                m.arms.Add(arm);
                if (side > 0)
                {
                    var hand = Hand(arm, 0.85f);
                    Prim(PrimitiveType.Cylinder, "AxeHandle", hand, new Vector3(0f, 0.35f, 0f), new Vector3(0.07f, 0.75f, 0.07f), Mats["Wood"]);
                    Box("BladeF", hand, new Vector3(0f, 0.95f, 0.22f), new Vector3(0.05f, 0.5f, 0.35f), Mats["Steel"]);
                    Box("BladeB", hand, new Vector3(0f, 0.95f, -0.22f), new Vector3(0.05f, 0.5f, 0.35f), Mats["Steel"]);
                }
            }
            var head = Box("BullHead", v, new Vector3(0f, 1.98f, 0.12f), new Vector3(0.48f, 0.45f, 0.5f), Fur).transform;
            Box("Snout", head, new Vector3(0f, -0.25f, 0.55f), new Vector3(0.75f, 0.55f, 0.5f), DarkFur);
            Prim(PrimitiveType.Cylinder, "NoseRing", head, new Vector3(0f, -0.5f, 0.82f), new Vector3(0.35f, 0.05f, 0.35f), Gold, false, Quaternion.Euler(90f, 0f, 0f));
            Eyes(head, EyeGlow(new Color(1f, 0.15f, 0.1f)), 0.3f, 0.2f, 0.15f);
            for (int side = -1; side <= 1; side += 2)
            {
                // Рога — на корне модели (не на голове-кубе), чтобы не было перекоса от неравномерного масштаба
                Box("Horn", v, new Vector3(0.38f * side, 2.12f, 0.12f), new Vector3(0.32f, 0.09f, 0.09f), Horn);
                Box("HornTip", v, new Vector3(0.55f * side, 2.3f, 0.12f), new Vector3(0.08f, 0.32f, 0.08f), Horn, false, Quaternion.Euler(0f, 0f, -18f * side));
            }
            var armor = Empty("Armor", v, Vector3.zero);
            Box("Chest", armor, new Vector3(0f, 1.45f, 0.26f), new Vector3(0.88f, 0.55f, 0.1f), Mats["Armor"]);
            Box("ShoulderL", armor, new Vector3(-0.58f, 1.78f, 0f), new Vector3(0.38f, 0.22f, 0.55f), Mats["Armor"]);
            Box("ShoulderR", armor, new Vector3(0.58f, 1.78f, 0f), new Vector3(0.38f, 0.22f, 0.55f), Mats["Armor"]);
            Box("Belt", armor, new Vector3(0f, 1.02f, 0.01f), new Vector3(0.9f, 0.14f, 0.52f), Mats["Armor"]);
            m.armor = armor.gameObject;
            m.barY = 2.65f;
            m.core = new Vector3(0f, 1.38f, 0.32f);
        }

        // Цербер: огромный пёс на 4 лапах, широкая грудь, ошейник с шипами, три шеи (головы добавляет BossCerberus).
        private static void Hound(EnemyModel m, Transform v, Material main)
        {
            m.body = R(Box("Body", v, new Vector3(0f, 1.25f, -0.25f), new Vector3(0.85f, 0.75f, 1.5f), main));
            Box("Chest", v, new Vector3(0f, 1.5f, 0.35f), new Vector3(1.25f, 0.95f, 0.65f), main);
            FourLegs(m, v, 1.05f, 0.38f, 0.35f, -0.75f, 1.05f, 0.24f, main, DarkFur);
            Tail(m, v, new Vector3(0f, 1.45f, -1.0f), 4, 0.28f, 0.14f, main, -25f);
            for (int i = -1; i <= 1; i++)
                Prim(PrimitiveType.Cylinder, "Neck" + i, v, new Vector3(i * 0.6f * 0.85f, 1.92f, 0.5f), new Vector3(0.32f, 0.22f, 0.32f), main,
                    false, Quaternion.Euler(-25f, 0f, -i * 15f));
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI / 9f;
                Box("Spike" + i, v, new Vector3(Mathf.Cos(a) * 0.62f, 1.78f + Mathf.Sin(a) * 0.08f, 0.55f), new Vector3(0.08f, 0.2f, 0.08f), Mats["Steel"],
                    false, Quaternion.Euler(-30f, 0f, (Mathf.Rad2Deg * a) - 90f));
            }
            m.barY = 2.75f;
            m.core = new Vector3(0f, 1.5f, 0.7f);
        }

        // Тело Гидры: огромная туша, гребень, длинный хвост, широкое основание шей (головы добавляет BossHydra).
        private static void HydraBody(EnemyModel m, Transform v, Material main)
        {
            m.body = R(Prim(PrimitiveType.Sphere, "Body", v, new Vector3(0f, 1.0f, -0.2f), new Vector3(1.6f, 1.15f, 2.0f), main));
            Prim(PrimitiveType.Sphere, "Belly", v, new Vector3(0f, 0.8f, 0.1f), new Vector3(1.3f, 0.8f, 1.4f), EM("HydraBelly", new Color(0.75f, 0.7f, 0.4f)));
            FourLegs(m, v, 0.6f, 0.62f, 0.45f, -0.75f, 0.6f, 0.3f, main, DarkFur);
            Tail(m, v, new Vector3(0f, 0.95f, -1.15f), 5, 0.38f, 0.42f, main, 8f);
            Box("NeckBase", v, new Vector3(0f, 1.85f, 0.35f), new Vector3(1.3f, 0.55f, 0.7f), main);
            for (int i = 0; i < 5; i++)
                Box("Ridge" + i, v, new Vector3(0f, 1.62f - i * 0.05f, -0.1f - i * 0.35f), new Vector3(0.12f, 0.35f, 0.25f), EM("HydraRidge", main.color * 0.5f));
            m.barY = 3.1f;
            m.core = new Vector3(0f, 1.1f, 0.75f);
        }

        // Медуза: змеиный хвост кольцами вместо ног, торс, золотые украшения, светящиеся глаза (волосы-змеи — BossMedusa).
        private static void Gorgon(EnemyModel m, Transform v, Material main)
        {
            var scales = EM("GorgonScales", new Color(0.25f, 0.45f, 0.25f));
            Prim(PrimitiveType.Cylinder, "Coil1", v, new Vector3(0f, 0.15f, 0f), new Vector3(1.25f, 0.15f, 1.25f), scales);
            Prim(PrimitiveType.Cylinder, "Coil2", v, new Vector3(0f, 0.42f, 0f), new Vector3(1.0f, 0.13f, 1.0f), scales);
            Prim(PrimitiveType.Cylinder, "Coil3", v, new Vector3(0f, 0.68f, 0f), new Vector3(0.72f, 0.13f, 0.72f), scales);
            Tail(m, v, new Vector3(0f, 0.15f, -0.6f), 4, 0.35f, 0.3f, scales, 0f);
            m.body = R(Box("Torso", v, new Vector3(0f, 1.18f, 0f), new Vector3(0.5f, 0.65f, 0.3f), main));
            Box("Bodice", v, new Vector3(0f, 1.3f, 0.02f), new Vector3(0.52f, 0.2f, 0.32f), Gold);
            for (int side = -1; side <= 1; side += 2)
            {
                var arm = Limb(side < 0 ? "ArmL" : "ArmR", v, new Vector3(0.32f * side, 1.45f, 0f), 0.62f, 0.11f, main, Quaternion.Euler(-20f, 0f, 15f * side));
                Box("Armlet", arm, new Vector3(0f, -0.2f, 0f), new Vector3(0.16f, 0.08f, 0.16f), Gold);
                m.arms.Add(arm);
            }
            var head = Prim(PrimitiveType.Sphere, "Head", v, new Vector3(0f, 1.88f, 0.02f), Vector3.one * 0.38f, main).transform;
            Eyes(head, EyeGlow(new Color(1f, 0.9f, 0.2f)), 0.2f, 0.16f);
            Box("Diadem", head, new Vector3(0f, 0.3f, 0.3f), new Vector3(0.9f, 0.12f, 0.4f), Gold);
            m.barY = 2.7f;
            m.core = new Vector3(0f, 1.2f, 0.2f);
        }
    }
}
