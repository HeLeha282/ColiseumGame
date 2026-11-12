using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ColiseumGame.UIFactory;

namespace ColiseumGame
{
    /// <summary>
    /// Интерфейс новых систем, собирается из кода на канвасе HUD: выбор сложности, меню героя (Tab),
    /// Слава/Фокус/навыки, счётчик башен, режим стройки, превью волны с подсказками, карточка нового врага,
    /// панель босса, катсцена, «Продолжить» после победы.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.07f, 0.06f, 0.08f, 0.88f);
        private static readonly Color Gold = new Color(1f, 0.85f, 0.45f);

        private GameManager gm;
        private PlayerProgression prog;
        private PlayerAbilities abilities;
        private TowerPlacer placer;

        private GameObject setupPanel, heroPanel, buildBar, previewPanel, introCard, bossPanel, cutTop, cutBottom, continueButton;
        private RectTransform heroContent, previewContent, xpFill, focusFill, bossFill;
        private TextMeshProUGUI towersText, levelText, focusText, buildName, buildReason, introText, bossName, bossHint, cutCaption;
        private readonly TextMeshProUGUI[] abilityTexts = new TextMeshProUGUI[3];
        private readonly Image[] abilityShades = new Image[3];
        private readonly Queue<EnemyConfig> introQueue = new Queue<EnemyConfig>();
        private float introTimer;
        private bool heroDirty = true;

        private void Start()
        {
            gm = GameManager.Instance;
            var player = PlayerController.Instance;
            prog = player != null ? player.GetComponent<PlayerProgression>() : null;
            abilities = player != null ? player.GetComponent<PlayerAbilities>() : null;
            placer = player != null ? player.GetComponent<TowerPlacer>() : null;
            BuildHudInfo();
            BuildAbilityBar();
            BuildBuildBar();
            BuildPreview();
            BuildIntroCard();
            BuildBossPanel();
            BuildCutscene();
            BuildHeroPanel();
            BuildSetup();
            BuildContinue();

            gm.PhaseChanged += OnPhaseChanged;
            gm.EssenceChanged += () => heroDirty = true;
            if (prog != null) prog.Changed += () => heroDirty = true;
            Enemy.FirstSeen += OnFirstSeen;
            OnPhaseChanged();
        }

        private void OnDestroy() => Enemy.FirstSeen -= OnFirstSeen;

        private void OnFirstSeen(EnemyConfig cfg) => introQueue.Enqueue(cfg);

        // ---------------------------------------------------------------- построение

        private void BuildHudInfo()
        {
            var tl = new Vector2(0f, 1f);
            towersText = Text("Towers", Rect("TowersRoot", transform, tl, tl, new Vector2(24f, -106f), new Vector2(440f, 30f)), "", 22f, TextAlignmentOptions.Left, Gold);
            var xp = Rect("XP", transform, tl, tl, new Vector2(24f, -140f), new Vector2(440f, 20f));
            xpFill = Bar(xp, new Color(0f, 0f, 0f, 0.6f), new Color(0.95f, 0.75f, 0.3f));
            levelText = Text("Level", xp, "", 16f, TextAlignmentOptions.Left, Color.white);
            levelText.margin = new Vector4(8f, 0f, 0f, 0f);

            var bc = new Vector2(0.5f, 0f);
            var focus = Rect("Focus", transform, bc, bc, new Vector2(0f, 100f), new Vector2(520f, 22f));
            focusFill = Bar(focus, new Color(0f, 0f, 0f, 0.6f), Elements.Color(Element.Void));
            focusText = Text("FocusText", focus, "", 16f, TextAlignmentOptions.Center, Color.white);
        }

        private void BuildAbilityBar()
        {
            var bl = new Vector2(0f, 0f);
            string[] keys = { "Q", "R", "X" };
            for (int i = 0; i < 3; i++)
            {
                var box = Panel("Ability" + keys[i], transform, bl, bl, new Vector2(24f + i * 150f, 70f), new Vector2(140f, 64f), PanelColor);
                var shade = Panel("Cooldown", box.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(140f, 64f), new Color(0f, 0f, 0f, 0.6f));
                shade.rectTransform.pivot = new Vector2(0.5f, 0f);
                abilityShades[i] = shade;
                abilityTexts[i] = Text("Text", box.transform, "", 17f, TextAlignmentOptions.Center, Color.white);
            }
        }

        private void BuildBuildBar()
        {
            var bc = new Vector2(0.5f, 0f);
            var p = Panel("BuildBar", transform, bc, bc, new Vector2(0f, 190f), new Vector2(1100f, 84f), PanelColor);
            buildBar = p.gameObject;
            buildName = Text("Name", Rect("NameRoot", p.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(1080f, 40f)), "", 24f, TextAlignmentOptions.Center, Gold);
            buildReason = Text("Reason", Rect("ReasonRoot", p.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(1080f, 34f)), "", 19f, TextAlignmentOptions.Center, Color.white);
            buildBar.SetActive(false);
        }

        private void BuildPreview()
        {
            var tl = new Vector2(0f, 1f);
            var p = Panel("WavePreview", transform, tl, tl, new Vector2(24f, -176f), new Vector2(560f, 100f), PanelColor);
            previewPanel = p.gameObject;
            VLayout(previewPanel, 12, 4f);
            previewPanel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            previewContent = p.rectTransform;
        }

        private void BuildIntroCard()
        {
            var tr = new Vector2(1f, 1f);
            var p = Panel("EnemyIntro", transform, tr, tr, new Vector2(-24f, -120f), new Vector2(480f, 220f), new Color(0.12f, 0.05f, 0.05f, 0.92f));
            introCard = p.gameObject;
            introText = Text("Text", p.transform, "", 19f, TextAlignmentOptions.TopLeft, Color.white);
            introText.margin = new Vector4(14f, 12f, 14f, 12f);
            introCard.SetActive(false);
        }

        private void BuildBossPanel()
        {
            var tc = new Vector2(0.5f, 1f);
            var p = Panel("BossPanel", transform, tc, tc, new Vector2(0f, -112f), new Vector2(980f, 128f), new Color(0.15f, 0.04f, 0.06f, 0.88f));
            bossPanel = p.gameObject;
            bossName = Text("Name", Rect("NameRoot", p.transform, tc, tc, new Vector2(0f, -4f), new Vector2(960f, 32f)), "", 26f, TextAlignmentOptions.Center, new Color(1f, 0.55f, 0.45f));
            var bar = Rect("Bar", p.transform, tc, tc, new Vector2(0f, -38f), new Vector2(900f, 18f));
            bossFill = Bar(bar, new Color(0f, 0f, 0f, 0.7f), new Color(0.85f, 0.15f, 0.15f));
            bossHint = Text("Hint", Rect("HintRoot", p.transform, tc, tc, new Vector2(0f, -60f), new Vector2(950f, 64f)), "", 19f, TextAlignmentOptions.Top, Color.white);
            bossPanel.SetActive(false);
        }

        private void BuildCutscene()
        {
            var top = new Vector2(0.5f, 1f);
            var bottom = new Vector2(0.5f, 0f);
            cutTop = Panel("CutTop", transform, top, top, Vector2.zero, new Vector2(4000f, 140f), Color.black).gameObject;
            cutBottom = Panel("CutBottom", transform, bottom, bottom, Vector2.zero, new Vector2(4000f, 140f), Color.black).gameObject;
            cutCaption = Text("Caption", cutBottom.transform, "", 54f, TextAlignmentOptions.Center, Gold);
            cutTop.SetActive(false);
            cutBottom.SetActive(false);
        }

        private void BuildSetup()
        {
            var full = Panel("Setup", transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.85f));
            Stretch(full.rectTransform);
            setupPanel = full.gameObject;
            var box = Rect("Box", full.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 560f));
            VLayout(box.gameObject, 10, 14f);
            Height(Text("Title", box, "КОЛИЗЕЙ\n<size=60%>Выбери сложность</size>", 54f, TextAlignmentOptions.Center, Gold), 140f);
            var diffs = gm.Balance.difficulties;
            for (int i = 0; i < diffs.Length; i++)
            {
                int idx = i;
                var b = Button("Diff" + i, box, $"{diffs[i].name}\n<size=65%>{diffs[i].description}</size>",
                    i == gm.Balance.defaultDifficulty ? new Color(0.35f, 0.25f, 0.12f) : new Color(0.2f, 0.18f, 0.22f), 30f, () => gm.SelectDifficulty(idx));
                Height(b, 110f);
            }
        }

        private void BuildContinue()
        {
            var b = Button("Continue", transform, "Хотите продолжить дальше?\n<size=70%>Бесконечный режим — до падения портала</size>",
                new Color(0.45f, 0.2f, 0.6f), 28f, () => gm.ContinueEndless());
            var rt = (RectTransform)b.transform;
            rt.anchoredPosition = new Vector2(0f, -230f);
            rt.sizeDelta = new Vector2(560f, 100f);
            continueButton = b.gameObject;
            continueButton.SetActive(false);
        }

        private void BuildHeroPanel()
        {
            var p = Panel("HeroPanel", transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1000f, 700f), new Color(0.06f, 0.05f, 0.07f, 0.95f));
            heroPanel = p.gameObject;
            heroContent = Rect("Content", p.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1000f, 700f));
            VLayout(heroContent.gameObject, 18, 8f);
            heroPanel.SetActive(false);
        }

        // ---------------------------------------------------------------- меню героя

        private void ToggleHero(bool open)
        {
            if (open && BuildMenu.Instance != null && BuildMenu.Instance.IsOpen) BuildMenu.Instance.Close();
            heroPanel.SetActive(open);
            UIState.MenuOpen = open;
            heroDirty = true;
        }

        private void RebuildHero()
        {
            heroDirty = false;
            foreach (Transform c in heroContent) Destroy(c.gameObject);
            if (prog == null) return;
            bool prep = gm.Phase == GamePhase.Preparation;
            var hero = prog.Hero;

            Height(Text("Header", heroContent, $"ГЕРОЙ — уровень {prog.Level}   <size=70%>Слава {prog.Glory}/{prog.GloryToNext} · Башен максимум: {prog.TowerLimit} · Эссенция: {gm.Essence}</size>",
                30f, TextAlignmentOptions.Center, Gold), 46f);

            if (prog.PendingChoices > 0)
            {
                if (prog.Offer.Count == 0) prog.RollOffer();
                Height(Text("ChoiceTitle", heroContent, $"Выбери усиление (доступно выборов: {prog.PendingChoices})", 22f, TextAlignmentOptions.Center, Color.white), 30f);
                var row = Rect("Cards", heroContent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 190f));
                Height(row, 190f);
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 12f;
                h.childControlWidth = h.childControlHeight = true;
                h.childForceExpandWidth = h.childForceExpandHeight = true;
                foreach (var perk in prog.Offer)
                {
                    var pk = perk;
                    string slot = perk.kind == PerkKind.Ability ? $" → {prog.AbilitySlotHint()}" : "";
                    string branch = perk.branch switch { PerkBranch.Warrior => "Воин", PerkBranch.Hunter => "Охотник", PerkBranch.Elementalist => "Элементалист", _ => "Общее" };
                    Color c = perk.kind == PerkKind.TowerLimit ? new Color(0.35f, 0.28f, 0.1f) : perk.branch switch
                    {
                        PerkBranch.Warrior => new Color(0.38f, 0.1f, 0.1f),
                        PerkBranch.Hunter => new Color(0.12f, 0.3f, 0.15f),
                        PerkBranch.Elementalist => new Color(0.22f, 0.12f, 0.38f),
                        _ => new Color(0.2f, 0.2f, 0.25f)
                    };
                    Button("Perk", row, $"<b>{perk.name}</b>{slot}\n<size=65%>{branch}</size>\n<size=72%>{perk.description}</size>", c, 22f, () => prog.Choose(pk));
                }
            }
            else Height(Text("NoChoice", heroContent, "Новых выборов нет — набирай Славу (убийства и пройденные волны).", 20f, TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.8f)), 30f);

            Height(Text("WeaponsTitle", heroContent, "ОРУЖИЕ" + (prep ? "" : " <size=70%>(улучшать можно в подготовке)</size>"), 24f, TextAlignmentOptions.Center, Gold), 34f);
            WeaponRow(WeaponType.Sword, "Меч", prog.SwordLevel, prep);
            WeaponRow(WeaponType.Bow, "Лук", prog.BowLevel, prep);

            Height(Text("EnchantTitle", heroContent, $"Зачарование оружия: <b>{Elements.Name(prog.Enchant)}</b> <size=70%>(удары накладывают стихию — реакции с башнями; цена {prog.EnchantCost})</size>",
                20f, TextAlignmentOptions.Center, Color.white), 30f);
            var er = Rect("Enchants", heroContent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 56f));
            Height(er, 56f);
            var eh = er.gameObject.AddComponent<HorizontalLayoutGroup>();
            eh.spacing = 10f;
            eh.childControlWidth = eh.childControlHeight = true;
            eh.childForceExpandWidth = eh.childForceExpandHeight = true;
            foreach (var e in new[] { Element.Fire, Element.Ice, Element.Lightning })
            {
                var el = e;
                var b = Button("Ench", er, Elements.Name(e), Elements.Color(e) * 0.55f, 22f, () => prog.SetEnchant(el));
                b.interactable = prep && prog.Enchant != e && gm.CanAfford(prog.EnchantCost);
            }

            string q = prog.AbilitySlots[0] != null ? hero.Ability(prog.AbilitySlots[0])?.name : "—";
            string r = prog.AbilitySlots[1] != null ? hero.Ability(prog.AbilitySlots[1])?.name : "—";
            string ult = prog.UltimateId switch { "storm" => "Алая буря", "zero" => "Абсолютный ноль", _ => "Разлом Пустоты" };
            Height(Text("Abilities", heroContent, $"Навыки: Q — {q} · R — {r} · X — {ult}", 20f, TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.9f)), 30f);
            Height(Button("Close", heroContent, "Закрыть (Tab / Esc)", new Color(0.35f, 0.2f, 0.2f), 22f, () => ToggleHero(false)), 46f);
        }

        private void WeaponRow(WeaponType w, string name, int level, bool prep)
        {
            int cost = prog.UpgradeCost(w);
            string label = cost > 0
                ? $"{name} ур.{level} → ур.{level + 1} — {cost}   <size=70%>{PlayerProgression.WeaponPerks(w, level + 1)}</size>"
                : $"{name} ур.{level} (максимум)   <size=70%>{PlayerProgression.WeaponPerks(w, level)}</size>";
            var b = Button(name, heroContent, label, new Color(0.25f, 0.22f, 0.15f), 22f, () => prog.UpgradeWeapon(w));
            b.interactable = cost > 0 && prep && gm.CanAfford(cost);
            Height(b, 52f);
        }

        // ---------------------------------------------------------------- превью волны

        private void OnPhaseChanged()
        {
            setupPanel.SetActive(gm.Phase == GamePhase.Setup);
            continueButton.SetActive(gm.Phase == GamePhase.Victory);
            previewPanel.SetActive(gm.Phase == GamePhase.Preparation);
            if (gm.Phase == GamePhase.Preparation) RebuildPreview();
            if (gm.Phase != GamePhase.Preparation && heroPanel.activeSelf) heroDirty = true;
        }

        private void RebuildPreview()
        {
            foreach (Transform c in previewContent) Destroy(c.gameObject);
            if (!gm.HasWave(gm.WaveIndex)) return;
            var wave = gm.GetWave(gm.WaveIndex);
            string title = $"<b>Далее: волна {gm.WaveIndex + 1}</b>" + (string.IsNullOrEmpty(wave.title) ? "" : $" — {wave.title}");
            Height(Text("Title", previewContent, title, 22f, TextAlignmentOptions.Left, Gold), 30f);
            var counts = new Dictionary<EnemyConfig, int>();
            foreach (var g in wave.groups)
            {
                if (g.enemy == null) continue;
                counts.TryGetValue(g.enemy, out int n);
                counts[g.enemy] = n + g.count;
            }
            foreach (var kv in counts)
            {
                string line = $"<b>{kv.Key.displayName}</b> ×{kv.Value}";
                if (kv.Key.element != Element.None) line += $" <color=#{ColorUtility.ToHtmlStringRGB(Elements.Color(kv.Key.element))}>[{Elements.Name(kv.Key.element)}]</color>";
                if (!string.IsNullOrEmpty(kv.Key.counterHint)) line += $"\n<size=80%><color=#BBBBBB>Контр: {kv.Key.counterHint}</color></size>";
                var t = Text("Line", previewContent, line, 18f, TextAlignmentOptions.Left, Color.white);
                Height(t, string.IsNullOrEmpty(kv.Key.counterHint) ? 24f : 48f);
            }
            Height(Text("Hint", previewContent, "<size=85%>3 — молоток (стройка) · Tab — герой · G — начать</size>", 17f, TextAlignmentOptions.Left, new Color(0.7f, 0.7f, 0.7f)), 22f);
        }

        // ---------------------------------------------------------------- кадр

        private void Update()
        {
            if (gm == null) return;
            var input = InputReader.Instance;
            if (input != null && prog != null && gm.Phase != GamePhase.Setup && !UIState.ResultOpen)
            {
                if (input.MenuPressed) ToggleHero(!heroPanel.activeSelf);
                else if (heroPanel.activeSelf && input.CancelPressed) ToggleHero(false);
            }
            if (heroPanel.activeSelf && heroDirty) RebuildHero();

            if (prog != null)
            {
                towersText.text = $"Башни: {Tower.All.Count}/{prog.TowerLimit}";
                levelText.text = $"Герой ур.{prog.Level} · Слава {prog.Glory}/{prog.GloryToNext}" + (prog.PendingChoices > 0 ? "  <color=#FFD36B>★ выбор усиления — Tab</color>" : "");
                xpFill.localScale = new Vector3(Mathf.Clamp01(prog.Glory / (float)prog.GloryToNext), 1f, 1f);
                focusFill.localScale = new Vector3(prog.FocusFraction, 1f, 1f);
                focusFill.GetComponent<Image>().color = prog.UltimateReady ? Color.Lerp(Elements.Color(Element.Void), Color.white, Mathf.PingPong(Time.time * 2f, 0.5f)) : Elements.Color(Element.Void);
                focusText.text = prog.UltimateReady ? "ФОКУС ПОЛОН — X: ультимейт!" : $"Фокус {prog.Focus:0}/{prog.Hero.focusMax:0}";
                UpdateAbilities();
            }
            UpdateBuildBar();
            UpdateIntro();
            UpdateBoss();
            bool cut = Cutscene.Playing;
            cutTop.SetActive(cut);
            cutBottom.SetActive(cut);
            if (cut) cutCaption.text = Cutscene.Caption;
        }

        private void UpdateAbilities()
        {
            for (int i = 0; i < 2; i++)
            {
                string id = prog.AbilitySlots[i];
                var def = id != null ? prog.Hero.Ability(id) : null;
                abilityTexts[i].text = def != null ? $"<b>{(i == 0 ? "Q" : "R")}</b>\n<size=75%>{def.name}</size>" : $"<b>{(i == 0 ? "Q" : "R")}</b>\n<size=70%>навык не изучен</size>";
                float cd = abilities != null && def != null ? abilities.Cooldown(i) / def.cooldown : 0f;
                abilityShades[i].rectTransform.localScale = new Vector3(1f, Mathf.Clamp01(cd), 1f);
            }
            string ult = prog.UltimateId switch { "storm" => "Алая буря", "zero" => "Абс. ноль", _ => "Разлом Пустоты" };
            abilityTexts[2].text = $"<b>X</b>\n<size=75%>{ult}</size>";
            abilityShades[2].rectTransform.localScale = new Vector3(1f, 1f - prog.FocusFraction, 1f);
        }

        private void UpdateBuildBar()
        {
            bool on = placer != null && placer.Active && !UIState.GameplayBlocked;
            if (buildBar.activeSelf != on) buildBar.SetActive(on);
            if (!on) return;
            var cfg = placer.Selected;
            if (cfg == null) return;
            buildName.text = $"МОЛОТОК: <b>{cfg.displayName}</b> — {cfg.levels[0].cost}   <size=70%>{cfg.description}</size>";
            buildReason.text = placer.IsValid
                ? "<color=#8CFF9A>ЛКМ — поставить</color> · колесо или Q/R — другая башня · ПКМ — убрать молоток"
                : $"<color=#FF7A6A>{placer.Reason}</color> · колесо или Q/R — другая башня · ПКМ — убрать молоток";
        }

        private void UpdateIntro()
        {
            if (introTimer > 0f)
            {
                introTimer -= Time.unscaledDeltaTime;
                if (introTimer <= 0f) introCard.SetActive(false);
                return;
            }
            if (introQueue.Count == 0) return;
            var cfg = introQueue.Dequeue();
            string el = cfg.element != Element.None ? $"  <color=#{ColorUtility.ToHtmlStringRGB(Elements.Color(cfg.element))}>[{Elements.Name(cfg.element)}]</color>" : "";
            introText.text = $"<size=120%><color=#FF8A6A><b>{(cfg.IsBoss ? "БОСС" : "НОВЫЙ ВРАГ")}: {cfg.displayName}</b></color></size>{el}\n{cfg.description}\n\n<color=#FFD36B>Контр:</color> {cfg.counterHint}";
            introCard.SetActive(true);
            introTimer = cfg.IsBoss ? 10f : 7f;
        }

        private void UpdateBoss()
        {
            BossBase boss = null;
            foreach (var b in BossBase.All) if (b != null && b.Enemy.IsAlive) { boss = b; break; }
            bool on = boss != null && !UIState.ResultOpen;
            if (bossPanel.activeSelf != on) bossPanel.SetActive(on);
            if (!on) return;
            bossName.text = boss.Name + (boss.Enemy.Armor > 0f ? $"  <size=70%>броня {boss.Enemy.Armor:0}</size>" : "");
            bossFill.localScale = new Vector3(boss.Enemy.Health.Fraction, 1f, 1f);
            bossHint.text = boss.Hint;
        }
    }
}
