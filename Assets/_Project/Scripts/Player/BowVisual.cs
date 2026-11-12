using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Временный «красивый» лук из примитивов с процедурной анимацией (пока нет модели):
    /// гнущиеся плечи, тетива, стрела на тетиве при натяжении, вскидывание лука, отдача и дрожание тетивы после выстрела.
    /// Вешается на объект Bow; при старте сам строит геометрию, старые дочерние объекты удаляет.
    /// </summary>
    public class BowVisual : MonoBehaviour
    {
        [SerializeField] private PlayerCombat combat;
        [Header("Материалы")]
        [SerializeField] private Material wood;
        [SerializeField] private Material leather;
        [SerializeField] private Material gold;
        [SerializeField] private Material stringMat;
        [SerializeField] private Material steel;
        [SerializeField] private Material feather;

        [Header("Форма")]
        [SerializeField] private float limbLength = 0.4f;
        [SerializeField] private int limbSegments = 7;
        [SerializeField] private float limbThickness = 0.026f;
        [SerializeField] private float restCurve = 0.08f;
        [SerializeField] private float drawBend = 0.09f;
        [SerializeField] private float maxPull = 0.3f;
        [SerializeField] private float arrowLength = 0.62f;

        [Header("Движение")]
        [SerializeField] private Vector3 restLocalPosition = new Vector3(0.02f, -0.02f, 0.3f);
        [SerializeField] private Vector3 restLocalEuler = new Vector3(8f, -12f, -38f);
        [SerializeField] private Vector3 aimLocalPosition = new Vector3(-0.08f, 0.1f, 0.32f);
        [SerializeField] private Vector3 aimLocalEuler = new Vector3(0f, -4f, -22f);
        [SerializeField] private float raiseSpeed = 12f;

        private Transform[] upper, lower;
        private Transform tipTop, tipBottom, arrow;
        private LineRenderer bowString;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private float raise, shownCharge, vibration, kick;
        private bool wasCharging;

        private void Awake()
        {
            if (combat == null) combat = GetComponentInParent<PlayerCombat>();
            restPosition = restLocalPosition;
            restRotation = Quaternion.Euler(restLocalEuler);
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            Build();
            UpdateShape(0f, 0f);
        }

        private void OnDisable()
        {
            // При смене оружия лук возвращается в обычное положение
            raise = shownCharge = vibration = kick = 0f;
            wasCharging = false;
            transform.localPosition = restPosition;
            transform.localRotation = restRotation;
        }

        // ---------------------------------------------------------------- геометрия

        private void Build()
        {
            var grip = Part(PrimitiveType.Cylinder, "Grip", leather, transform);
            grip.localScale = new Vector3(0.04f, 0.075f, 0.04f);
            foreach (float y in new[] { 0.08f, -0.08f })
            {
                var ring = Part(PrimitiveType.Cylinder, "GripRing", gold, transform);
                ring.localPosition = new Vector3(0f, y, 0f);
                ring.localScale = new Vector3(0.047f, 0.007f, 0.047f);
            }

            upper = new Transform[limbSegments];
            lower = new Transform[limbSegments];
            for (int i = 0; i < limbSegments; i++)
            {
                upper[i] = Part(PrimitiveType.Cylinder, "LimbTop", wood, transform);
                lower[i] = Part(PrimitiveType.Cylinder, "LimbBottom", wood, transform);
            }
            tipTop = Part(PrimitiveType.Sphere, "TipTop", gold, transform);
            tipBottom = Part(PrimitiveType.Sphere, "TipBottom", gold, transform);
            tipTop.localScale = tipBottom.localScale = Vector3.one * 0.035f;

            var go = new GameObject("String");
            go.transform.SetParent(transform, false);
            bowString = go.AddComponent<LineRenderer>();
            bowString.useWorldSpace = false;
            bowString.positionCount = 3;
            bowString.widthMultiplier = 0.007f;
            bowString.numCapVertices = 2;
            bowString.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bowString.sharedMaterial = stringMat != null ? stringMat : FallbackMaterial(new Color(0.92f, 0.88f, 0.78f));

            // Стрела на тетиве (видна только при натяжении)
            arrow = new GameObject("NockedArrow").transform;
            arrow.SetParent(transform, false);
            var shaft = Part(PrimitiveType.Cylinder, "Shaft", wood, arrow);
            shaft.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shaft.localPosition = new Vector3(0f, 0f, arrowLength * 0.5f);
            shaft.localScale = new Vector3(0.014f, arrowLength * 0.5f, 0.014f);
            var head = Part(PrimitiveType.Cube, "Head", steel, arrow);
            head.localPosition = new Vector3(0f, 0f, arrowLength + 0.03f);
            head.localRotation = Quaternion.Euler(0f, 45f, 45f);
            head.localScale = new Vector3(0.045f, 0.045f, 0.045f);
            for (int i = 0; i < 3; i++)
            {
                var f = Part(PrimitiveType.Cube, "Fletch", feather, arrow);
                f.localRotation = Quaternion.Euler(0f, 0f, i * 120f);
                f.localPosition = f.localRotation * new Vector3(0f, 0.018f, 0.07f);
                f.localScale = new Vector3(0.004f, 0.03f, 0.1f);
            }
            arrow.gameObject.SetActive(false);
        }

        private Transform Part(PrimitiveType type, string name, Material mat, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat != null ? mat : FallbackMaterial(Color.gray);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        private static Material fallback;

        private static Material FallbackMaterial(Color c)
        {
            if (fallback == null) fallback = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var m = new Material(fallback) { color = c };
            return m;
        }

        // ---------------------------------------------------------------- анимация

        private void LateUpdate()
        {
            if (combat == null) return;
            float dt = Time.deltaTime;
            bool charging = combat.IsCharging;

            // Выстрел: тетива срывается вперёд и дрожит, лук отдаёт назад
            if (wasCharging && !charging && shownCharge > 0.05f)
            {
                vibration = Mathf.Clamp01(shownCharge + 0.3f);
                kick = 1f;
                shownCharge = 0f;
            }
            wasCharging = charging;

            float target = charging ? combat.Charge01 : 0f;
            shownCharge = Mathf.MoveTowards(shownCharge, target, dt * 6f);
            raise = Mathf.MoveTowards(raise, charging ? 1f : 0f, dt * raiseSpeed * (charging ? 1f : 0.5f));
            vibration = Mathf.MoveTowards(vibration, 0f, dt * 2.5f);
            kick = Mathf.MoveTowards(kick, 0f, dt * 6f);

            float wobble = vibration * Mathf.Sin(Time.time * 70f) * 0.05f;
            UpdateShape(shownCharge, wobble);

            // Вскидывание лука к центру экрана при натяжении + отдача + лёгкое покачивание
            float e = raise * raise * (3f - 2f * raise);
            Vector3 sway = new Vector3(Mathf.Sin(Time.time * 1.3f), Mathf.Sin(Time.time * 2.1f), 0f) * 0.004f * (1f - e);
            transform.localPosition = Vector3.Lerp(restPosition, aimLocalPosition, e)
                                      + Vector3.back * (kick * 0.07f) + sway
                                      + Vector3.right * (Mathf.PerlinNoise(Time.time * 3f, 0f) - 0.5f) * 0.006f * shownCharge;
            transform.localRotation = Quaternion.Slerp(restRotation, Quaternion.Euler(aimLocalEuler), e)
                                      * Quaternion.Euler(-kick * 8f, 0f, 0f);
        }

        /// <summary>Перестроить плечи, тетиву и стрелу под натяжение charge (0..1).</summary>
        private void UpdateShape(float charge, float stringWobble)
        {
            float bend = restCurve + drawBend * charge;
            Vector3 Point(float t, float sign) => new Vector3(0f, sign * (0.08f + t * limbLength), -bend * t * t);

            PlaceLimb(upper, 1f, Point);
            PlaceLimb(lower, -1f, Point);
            Vector3 top = Point(1f, 1f), bottom = Point(1f, -1f);
            tipTop.localPosition = top;
            tipBottom.localPosition = bottom;

            float pull = maxPull * charge;
            Vector3 nock = new Vector3(0f, 0f, top.z - pull + stringWobble);
            bowString.SetPosition(0, top);
            bowString.SetPosition(1, nock);
            bowString.SetPosition(2, bottom);

            bool showArrow = charge > 0.02f;
            if (arrow.gameObject.activeSelf != showArrow) arrow.gameObject.SetActive(showArrow);
            arrow.localPosition = nock;
        }

        private void PlaceLimb(Transform[] segs, float sign, System.Func<float, float, Vector3> point)
        {
            for (int i = 0; i < segs.Length; i++)
            {
                Vector3 a = point((float)i / segs.Length, sign);
                Vector3 b = point((float)(i + 1) / segs.Length, sign);
                Vector3 d = b - a;
                float taper = Mathf.Lerp(1f, 0.6f, (i + 0.5f) / segs.Length);
                segs[i].localPosition = (a + b) * 0.5f;
                segs[i].localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
                segs[i].localScale = new Vector3(limbThickness * taper, d.magnitude * 0.55f, limbThickness * taper * 1.3f);
            }
        }
    }
}
