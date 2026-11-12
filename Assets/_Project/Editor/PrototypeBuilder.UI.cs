using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static ColiseumGame.EditorTools.BuilderUtil;

namespace ColiseumGame.EditorTools
{
    public static partial class PrototypeBuilder
    {
        private const string HintsLine =
            "WASD — движение · Shift — бег · Пробел — прыжок · Мышь — камера · V — 1-е/3-е лицо · 1/2 — меч/лук · " +
            "ЛКМ — атака (лук: держать) · F — меню слота · E — ремонт (держать) · G — Готов · Alt — курсор";

        private static GameObject BuildHUD()
        {
            var root = new GameObject("HUD", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            var t = root.transform;

            var white = Color.white;
            var topLeft = new Vector2(0f, 1f);
            var topRight = new Vector2(1f, 1f);
            var top = new Vector2(0.5f, 1f);
            var center = new Vector2(0.5f, 0.5f);
            var bottom = new Vector2(0.5f, 0f);
            var bottomRight = new Vector2(1f, 0f);

            // HP игрока и портала
            var playerBar = UIRect("PlayerHP", t, topLeft, topLeft, new Vector2(24f, -24f), new Vector2(440f, 34f));
            var playerFill = UIBar(playerBar, new Color(0f, 0f, 0f, 0.6f), new Color(0.3f, 0.75f, 0.35f));
            var playerText = UIText("Text", playerBar, "Игрок", 22f, TextAlignmentOptions.Left, white);
            playerText.margin = new Vector4(10f, 0f, 0f, 0f);

            var portalBar = UIRect("PortalHP", t, topLeft, topLeft, new Vector2(24f, -66f), new Vector2(440f, 34f));
            var portalFill = UIBar(portalBar, new Color(0f, 0f, 0f, 0.6f), new Color(0.2f, 0.8f, 1f));
            var portalText = UIText("Text", portalBar, "Портал", 22f, TextAlignmentOptions.Left, white);
            portalText.margin = new Vector4(10f, 0f, 0f, 0f);

            // Волна, эссенция, оружие
            var wave = UIText("WaveText", UIRect("Wave", t, top, top, new Vector2(0f, -14f), new Vector2(1000f, 100f)), "Волна", 38f,
                TextAlignmentOptions.Top, white);
            var essence = UIText("EssenceText", UIRect("Essence", t, topRight, topRight, new Vector2(-24f, -20f), new Vector2(460f, 50f)),
                "Эссенция", 36f, TextAlignmentOptions.Right, new Color(0.4f, 1f, 0.95f));
            var weapon = UIText("WeaponText", UIRect("Weapon", t, topRight, topRight, new Vector2(-24f, -70f), new Vector2(460f, 40f)),
                "Оружие", 26f, TextAlignmentOptions.Right, white);

            // Прицел и натяжение лука
            var crosshair = UIText("Crosshair", UIRect("CrosshairRoot", t, center, center, Vector2.zero, new Vector2(40f, 40f)), "+", 40f,
                TextAlignmentOptions.Center, white);
            var charge = UIRect("ChargeBar", t, center, center, new Vector2(0f, -40f), new Vector2(160f, 10f));
            var chargeFill = UIBar(charge, new Color(0f, 0f, 0f, 0.6f), new Color(1f, 0.85f, 0.3f));
            charge.gameObject.SetActive(false);

            // Сообщения и подсказки
            var message = UIText("MessageText", UIRect("Message", t, top, top, new Vector2(0f, -130f), new Vector2(1200f, 60f)), "", 34f,
                TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.4f));
            var centerText = UIText("CenterText", UIRect("Center", t, center, center, new Vector2(0f, 90f), new Vector2(1000f, 60f)), "", 38f,
                TextAlignmentOptions.Center, new Color(1f, 0.45f, 0.4f));
            var prompt = UIText("PromptText", UIRect("Prompt", t, bottom, bottom, new Vector2(0f, 150f), new Vector2(1000f, 50f)), "", 30f,
                TextAlignmentOptions.Center, white);
            var hintsBack = UIRect("Hints", t, bottom, bottom, new Vector2(0f, 12f), new Vector2(1880f, 40f));
            hintsBack.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
            var hints = UIText("HintsText", hintsBack, HintsLine, 20f, TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.85f));
            hints.enableAutoSizing = true;
            hints.fontSizeMin = 10f;
            hints.fontSizeMax = 20f;
            hints.enableWordWrapping = false;

            // Кнопка «Готов»
            var ready = UIButton("ReadyButton", t, "Готов (G)", new Color(0.2f, 0.6f, 0.25f), 32f);
            var readyRt = (RectTransform)ready.transform;
            readyRt.anchorMin = readyRt.anchorMax = bottomRight;
            readyRt.pivot = bottomRight;
            readyRt.anchoredPosition = new Vector2(-24f, 70f);
            readyRt.sizeDelta = new Vector2(280f, 76f);

            // Меню постройки
            var menu = BuildMenuPanel(t, out var menuTitle, out var menuDesc, out var menuContainer, out var menuTemplate, out var menuClose);

            // Экран результата
            var result = UIImage("ResultPanel", t, new Color(0f, 0f, 0f, 0.75f)).gameObject;
            var resultTitle = UIText("Title", UIRect("TitleRoot", result.transform, center, center, new Vector2(0f, 120f), new Vector2(1200f, 120f)),
                "ПОБЕДА", 90f, TextAlignmentOptions.Center, white);
            var resultText = UIText("Text", UIRect("TextRoot", result.transform, center, center, new Vector2(0f, 10f), new Vector2(1200f, 120f)),
                "", 34f, TextAlignmentOptions.Center, white);
            var restart = UIButton("RestartButton", result.transform, "Заново", new Color(0.25f, 0.45f, 0.8f), 34f);
            var restartRt = (RectTransform)restart.transform;
            restartRt.anchoredPosition = new Vector2(0f, -120f);
            restartRt.sizeDelta = new Vector2(300f, 80f);
            result.SetActive(false);

            root.AddComponent<GameUI>();
            var hud = root.AddComponent<HUD>();
            Assign(hud,
                ("playerHpFill", playerFill), ("playerHpText", playerText),
                ("portalHpFill", portalFill), ("portalHpText", portalText),
                ("essenceText", essence), ("waveText", wave), ("weaponText", weapon),
                ("promptText", prompt), ("messageText", message), ("centerText", centerText),
                ("crosshair", crosshair.transform.parent.gameObject), ("chargeBar", charge.gameObject), ("chargeFill", chargeFill),
                ("readyButton", ready), ("resultPanel", result), ("resultTitle", resultTitle), ("resultText", resultText),
                ("restartButton", restart));

            var buildMenu = root.AddComponent<BuildMenu>();
            Assign(buildMenu, ("panel", menu), ("titleText", menuTitle), ("descriptionText", menuDesc),
                ("buttonContainer", menuContainer), ("buttonTemplate", menuTemplate), ("closeButton", menuClose));

            // EventSystem с модулем нового Input System
            var es = new GameObject("EventSystem");
            es.transform.SetParent(t, false);
            es.AddComponent<EventSystem>();
            var module = es.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
            return root;
        }

        private static GameObject BuildMenuPanel(Transform parent, out TMP_Text title, out TMP_Text description,
            out Transform container, out Button template, out Button close)
        {
            var center = new Vector2(0.5f, 0.5f);
            var panel = UIRect("BuildMenu", parent, center, center, new Vector2(0f, 40f), new Vector2(760f, 300f));
            panel.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.92f);
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 20, 20);
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            title = UIText("Title", panel, "Слот", 34f, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.5f));
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;
            description = UIText("Description", panel, "", 22f, TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.85f));
            description.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;

            var list = UIRect("Options", panel, center, center, Vector2.zero, new Vector2(100f, 100f));
            var listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 8f;
            listLayout.childControlHeight = true;
            listLayout.childControlWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childForceExpandWidth = true;
            container = list;

            template = UIButton("OptionTemplate", list, "Пункт", new Color(0.25f, 0.4f, 0.65f), 26f);
            template.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;

            close = UIButton("CloseButton", panel, "Закрыть (F / Esc)", new Color(0.35f, 0.2f, 0.2f), 24f);
            close.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }
    }
}
