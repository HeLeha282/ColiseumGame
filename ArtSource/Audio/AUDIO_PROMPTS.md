# Звуки, голоса и музыка ColiseumGame — промты для ElevenLabs и Suno

Готовые файлы клади **ровно под указанными именами** в `Assets/_Project/Audio/...` (папки создай, если нет).
Формат — **MP3** (или WAV). По именам я подключу всё в игру автоматически.
Где написано `_01.._03` — сгенерируй 3 варианта (кнопкой ещё раз) и сохрани все: игра будет выбирать случайный,
чтобы частые звуки (удары, шаги) не надоедали.

---

## Часть 1. ElevenLabs → Sound Effects (звуки)

**Где:** ElevenLabs → *Sound Effects*. Промт пиши **на английском** (так модель понимает лучше).

**Настройки:**
- **Duration (длительность)** — ставь ту, что в таблице (иначе модель решит сама и часто делает длиннее, чем нужно).
- **Prompt influence** — в таблице. Ниже (~30%) — креативнее, выше (~60–70%) — строже по тексту. Для ударов и
  интерфейса нужна точность, для магии — свобода.
- **Loop** — включай только там, где в таблице «loop» (фоновые звуки, огонь, аура). Если галочки нет — не страшно,
  я сам сделаю зацикливание.
- Генерируется сразу несколько вариантов — выбирай тот, где **нет лишнего** (музыки, голоса, тишины в начале).
- Если звук начинается с тишины — не беда, я обрежу.

### 1.1 Меч, лук, молоток (`Audio/SFX/Player/`)

| Файл | Промт | Длит. | Influence |
|---|---|---|---|
| sword_swing_01..03 | Fast sword swing whoosh, sharp air cut, no impact, close up | 0.6 с | 60% |
| sword_swing_heavy | Powerful heavy sword slash, deep whoosh with a short metallic ring, no impact | 0.9 с | 60% |
| sword_hit_flesh_01..03 | Sword slicing into a monster, meaty impact, short and punchy, no voice | 0.5 с | 60% |
| sword_hit_bone_01..02 | Sword striking a skeleton, dry bone crack and clatter, short | 0.6 с | 60% |
| sword_hit_armor_01..02 | Sword hitting thick bronze armor, loud metallic clang with ring | 0.8 с | 65% |
| sword_hit_shield | Sword blocked by a wooden shield with bronze rim, dull thud and clank | 0.7 с | 60% |
| sword_dash | Quick lunge dash whoosh with cloth flutter | 0.5 с | 50% |
| sword_wave | Magical blade wave projectile, shimmering energy slash flying forward | 1.0 с | 40% |
| bow_draw | Wooden bow string slowly drawn, creaking wood and tense string | 1.2 с | 60% |
| bow_release_01..02 | Bow string release twang with arrow whoosh | 0.6 с | 60% |
| arrow_hit_flesh | Arrow thudding into a creature, short wet thunk | 0.4 с | 60% |
| arrow_hit_stone | Arrow hitting a stone wall, sharp crack and wooden shaft vibration | 0.6 с | 60% |
| hammer_swing | Heavy builder hammer swing whoosh | 0.5 с | 50% |
| tower_build | Stone tower assembled: heavy stone blocks thumping into place, hammer strikes, dust, small magical chime at the end | 1.8 с | 45% |
| footstep_stone_01..04 | Single footstep, leather boot on dusty stone floor | 0.3 с | 70% |
| footstep_sand_01..04 | Single footstep, leather boot on sand | 0.3 с | 70% |
| jump | Short leather and armor rustle as a warrior jumps | 0.4 с | 50% |
| land | Feet landing on stone with armor rattle | 0.5 с | 55% |
| player_hurt_01..03 | Male warrior short pain grunt, no words | 0.5 с | 55% |
| player_death | Male warrior dying groan and body falling with armor clatter | 2.0 с | 45% |
| respawn | Magical resurrection: rising shimmer and warm chime | 1.5 с | 35% |
| level_up | Heroic level up: bright brass flourish with magical sparkle | 2.0 с | 35% |
| weapon_upgrade | Blacksmith anvil strikes then a magical ring as a weapon is reforged | 1.8 с | 45% |
| enchant_fire | Sword enchanted with fire: ignition whoosh and crackling flames | 1.5 с | 40% |
| enchant_ice | Sword enchanted with ice: crystalline freezing shimmer | 1.5 с | 40% |
| enchant_lightning | Sword enchanted with lightning: electric charge-up crackle | 1.5 с | 40% |

### 1.2 Навыки героя и ультимейт (`Audio/SFX/Abilities/`)

| Файл | Промт | Длит. | Influence |
|---|---|---|---|
| reap | Spinning crimson blade sweep around the warrior, circular whoosh with dark magical hum | 1.2 с | 35% |
| skyslam_jump | Warrior leaps into the air, powerful whoosh upward | 0.7 с | 45% |
| skyslam_impact | Heavy ground slam from above, stone cracking, dust shockwave boom | 1.5 с | 45% |
| firedash | Fiery dash, rushing flame whoosh leaving a burning trail | 1.2 с | 40% |
| frostnova | Ice nova burst: instant freezing explosion, ice shards cracking outward | 1.5 с | 40% |
| chain_lightning | Chain lightning: sharp electric zaps jumping between several targets, crackling arcs | 1.5 с | 40% |
| meteor | Meteor falling from the sky, rising roar then massive fiery explosion | 3.0 с | 40% |
| ult_charge | Two opposing energies charging in the hands, red fire and blue ice humming, rising in pitch, tense | 2.5 с | 30% |
| ult_void_beam | Enormous purple void energy beam firing, deep bass roar, reality-tearing distortion with shimmering highs | 3.0 с | 30% |
| ult_storm | Crimson fire storm unleashed, crackling thunder and roaring burning wind | 3.0 с | 30% |
| ult_zero | Absolute zero blast, deep freezing boom and everything cracking into ice | 3.0 с | 30% |

### 1.3 Башни и баррикады (`Audio/SFX/Towers/`)

| Файл | Промт | Длит. | Influence | Loop |
|---|---|---|---|---|
| archer_shot_01..02 | Archer tower firing an arrow, crisp string thwack, short | 0.5 с | 60% | |
| catapult_launch | Wooden catapult arm swinging and hurling a boulder, creaking ropes, heavy thunk | 1.5 с | 50% | |
| catapult_impact_01..02 | Boulder smashing into the ground, heavy rocky explosion with debris | 1.5 с | 50% | |
| obelisk_zap_01..02 | Magical lightning obelisk discharging, sharp electric crack | 0.8 с | 50% | |
| brazier_fire_loop | Large iron brazier burning, steady crackling flames | 6 с | 50% | loop |
| brazier_burst | Burst of flame whooshing out of a brazier | 0.8 с | 45% | |
| scorpion_shot | Heavy ballista firing a big bolt, powerful twang and wooden thunk | 0.9 с | 55% | |
| sanctuary_hum_loop | Holy sanctuary aura, soft warm choir-like magical hum, calm | 8 с | 35% | loop |
| sanctuary_pulse | Gentle holy chime pulse with soft shimmer | 1.2 с | 40% | |
| tower_upgrade | Tower upgrade: stone grinding, metallic clank and rising magical chime | 1.5 с | 45% | |
| tower_branch | Powerful upgrade unlocked: deep magical whoosh and golden bell | 2.0 с | 40% | |
| tower_sell | Tower dismantled: stones settling softly and coins jingling | 1.5 с | 45% | |
| barricade_build | Wooden barricade hammered together, planks and nails | 1.5 с | 55% | |
| barricade_hit_01..02 | Weapon striking wooden planks, splintery thud | 0.5 с | 60% | |
| barricade_break | Wooden barricade smashed apart, planks splintering | 1.2 с | 55% | |

### 1.4 Стихии и реакции (`Audio/SFX/Elements/`)

| Файл | Промт | Длит. | Influence |
|---|---|---|---|
| shatter | Block of ice shattering into pieces, glassy crash | 0.8 с | 55% |
| overload | Fire and lightning colliding, crackling electric explosion boom | 1.0 с | 45% |
| brittle | Ice creeping over armor, brittle crackling and creaking | 0.8 с | 45% |
| decay | Dark void energy decay, warping whoosh with hollow echo | 1.0 с | 35% |
| burn_ignite | Enemy catching fire, quick whoosh ignition | 0.5 с | 55% |
| freeze | Enemy freezing solid, fast ice crystallization crackle | 0.6 с | 55% |
| stun | Quick dazed impact ring, like a bell hit | 0.5 с | 50% |

### 1.5 Враги (`Audio/SFX/Enemies/`)

| Файл | Промт | Длит. | Influence |
|---|---|---|---|
| skeleton_attack_01..02 | Skeleton soldier swinging a rusty sword, bones rattling | 0.7 с | 50% |
| skeleton_death_01..02 | Skeleton collapsing into a pile of bones, dry clattering | 1.2 с | 55% |
| satyr_laugh | Mischievous goat-like satyr bleating laugh | 1.2 с | 40% |
| satyr_jump | Hooves leaping, quick whoosh | 0.6 с | 50% |
| satyr_death | Satyr goat-like bleat of pain | 0.8 с | 45% |
| cyclops_roar | Giant one-eyed cyclops roaring, deep guttural | 2.0 с | 40% |
| cyclops_smash | Huge wooden club smashing the ground, heavy thud | 1.2 с | 50% |
| cyclops_death | Giant groaning and falling to the ground with a heavy thud | 2.0 с | 45% |
| shield_bash | Soldier bashing with a large wooden shield, thud and bronze clank | 0.7 с | 55% |
| legionary_death | Armored soldier falling, metal armor clatter | 1.2 с | 55% |
| testudo_march_loop | Roman shield wall marching in unison, shields clanking, synchronized heavy steps | 4 с | 45% (loop) |
| fire_cast | Fire mage casting a fireball, whoosh and ignition | 1.0 с | 45% |
| fire_impact | Fireball exploding, burst of flames | 1.0 с | 50% |
| ice_cast | Ice mage casting a frost bolt, crystalline shimmer | 1.0 с | 45% |
| ice_impact | Frost bolt shattering on impact | 0.8 с | 50% |
| mage_death | Sorcerer death cry fading into magical dissipation | 1.5 с | 40% |
| centaur_shot | Centaur archer loosing an arrow from a strong bow | 0.6 с | 55% |
| centaur_gallop_loop | Horse galloping on stone and sand | 3 с | 50% (loop) |
| centaur_death | Horse-like whinny of pain and heavy fall | 1.5 с | 45% |
| priest_heal | Dark priest healing chant, low hum with warm magical glow | 2.0 с | 35% |
| priest_death | Old priest gasping and collapsing, robes rustling | 1.5 с | 45% |
| lemur_appear | Ghost appearing, eerie whisper and distant wail | 1.5 с | 30% |
| lemur_death | Ghost dissipating with a fading wail | 1.5 с | 35% |
| salamander_hiss | Fire salamander hissing and sizzling | 1.0 с | 45% |
| salamander_explode | Fiery creature exploding on death, burst of flame | 1.2 с | 45% |
| golem_step_01..02 | Ice golem heavy footstep, ice cracking under weight | 0.8 с | 50% |
| golem_death | Ice golem crumbling into heavy ice chunks | 2.0 с | 45% |
| harpy_screech_01..02 | Harpy shrieking, piercing bird-woman screech | 1.2 с | 40% |
| harpy_death | Harpy death screech, falling | 1.2 с | 40% |
| wings_loop | Large feathered wings flapping steadily | 2 с | 50% (loop) |
| stymphalian_screech | Small flock of metallic bronze birds screeching | 1.0 с | 40% |
| hydraling_hiss | Small serpent creature hissing with two voices | 1.0 с | 45% |
| hydraling_split | Wet squelch as a creature splits into two | 0.8 с | 45% |
| enemy_hit_01..03 | Generic monster hit, short punchy impact grunt, no words | 0.4 с | 55% |

### 1.6 Боссы — звуки (`Audio/SFX/Bosses/`)

| Файл | Промт | Длит. | Influence |
|---|---|---|---|
| minotaur_roar | Enraged minotaur bull roar, deep snorting bellow | 2.5 с | 40% |
| minotaur_axe | Giant double axe swing and heavy impact on stone | 1.2 с | 50% |
| minotaur_armor_break | Heavy bronze armor bursting off, metallic explosion and clatter | 1.5 с | 45% |
| cerberus_howl | Three-headed hellhound howling, three layered growling voices | 2.5 с | 35% |
| cerberus_bite | Giant hound snapping jaws, vicious bite | 0.7 с | 50% |
| cerberus_head_down | Monstrous head defeated, pained yelp fading | 1.2 с | 40% |
| hydra_roar | Many-headed hydra roaring, layered serpent hisses and roars | 3.0 с | 35% |
| hydra_head_cut | Hydra neck severed, wet slice | 1.0 с | 50% |
| hydra_regrow | Two new heads bursting out, wet squelch and double hiss | 1.5 с | 40% |
| hydra_cauterize | Red-hot iron searing flesh, loud sizzling | 1.5 с | 50% |
| medusa_gaze | Medusa's petrifying gaze: hissing snakes and stone crackling spreading outward | 2.5 с | 35% |
| petrify | Turning to stone, grinding crackle | 1.2 с | 50% |
| unpetrify | Stone shell cracking and falling away, magical release | 1.2 с | 45% |
| harpy_queen_screech | Giant harpy queen screech, piercing and powerful, echoing | 2.0 с | 35% |
| harpy_summon | Flock of harpies called in, many screeches and wing flaps | 2.0 с | 35% |
| wings_ripped | Huge feathered wings torn off, ripping and a pained screech | 1.5 с | 40% |
| titan_step | Colossal stone titan footstep, earthquake rumble | 1.5 с | 45% |
| titan_roar | Ancient titan roar, enormous deep echoing voice of stone, no words | 3.0 с | 35% |
| titan_phase | Titan's armor bursting, heavy metallic explosion and rumble | 2.0 с | 40% |
| titan_fall | Colossus collapsing, massive rumble and crash of stone | 4.0 с | 40% |
| cutscene_slowmo | Slow-motion whoosh with a deep heartbeat | 1.5 с | 35% |
| finisher_hit | Epic finishing blow, heavy slash and booming impact with reverb | 1.5 с | 40% |

### 1.7 Интерфейс и игра (`Audio/SFX/UI/`)

| Файл | Промт | Длит. | Influence | Loop |
|---|---|---|---|---|
| ui_click | Soft UI click, wooden and stone tap, very short | 0.15 с | 70% | |
| ui_hover | Very soft subtle UI tick | 0.1 с | 70% | |
| ui_error | Soft dull error thud, short and low | 0.4 с | 60% | |
| ui_open | Parchment scroll unrolling, short | 0.6 с | 55% | |
| ui_perk | Magical card chosen, golden shimmer and soft chime | 1.0 с | 40% | |
| essence_gain | Tiny magical crystal tinkle, like a coin | 0.3 с | 60% | |
| new_enemy | Mysterious reveal stinger, short ominous chime | 1.5 с | 35% | |
| wave_start | Roman war horn (cornu) blast signaling battle, echoing in an arena | 3.0 с | 45% | |
| wave_clear | Short triumphant brass fanfare | 3.0 с | 40% | |
| boss_warning | Ominous deep war drums and low horn, a boss approaches | 4.0 с | 35% | |
| early_call | Short urgent horn call | 1.5 с | 45% | |
| portal_hit | Magic portal being struck, crackling energy distortion | 0.8 с | 45% | |
| portal_low | Portal in danger: pulsing low magical alarm | 2.0 с | 40% | |
| victory_sting | Victory stinger, bright orchestral hit with choir | 4.0 с | 35% | |
| defeat_sting | Defeat stinger, low somber brass and fading drum | 4.0 с | 35% | |

### 1.8 Окружение (`Audio/SFX/Ambience/`)

| Файл | Промт | Длит. | Influence | Loop |
|---|---|---|---|---|
| arena_wind_loop | Ancient ruined colosseum ambience: gentle wind through stone arches, distant birds, faint echoes | 20 с | 35% | loop |
| torch_loop | Torch fire crackling softly | 6 с | 50% | loop |
| crowd_ghost_loop | Distant echoing crowd of a phantom arena cheering faintly, ghostly | 15 с | 30% | loop |

---

## Часть 2. ElevenLabs → голоса и реплики боссов

**Порядок для каждого персонажа:**
1. **Voices → Voice Design** (создать голос по описанию). Вставь описание голоса (на английском, из блока ниже),
   послушай 3 варианта, сохрани лучший под именем персонажа (`Minotaur`, `Medusa`…).
2. **Text to Speech**: модель **Eleven v3** (или новее, если есть), выбери этот голос, язык — **русский**.
3. **Stability**: режим как указано (*Creative* — максимум эмоций, *Natural* — баланс). Если голос «плывёт» или
   бормочет — сдвинь ближе к *Natural/Robust*.
4. Вставляй **по одной реплике** (вместе с тегами в квадратных скобках — это подсказки интонации для v3, они не
   озвучиваются). Генерируй 2–3 раза, сохраняй лучший дубль.
5. Если тег не сработал — просто убери его, а нужную эмоцию передай знаками: «…», «!», тире.

Папка: `Audio/Voice/<Персонаж>/`, имя файла — из первой колонки.

### Глашатай арены (Announcer) — объявляет волны
**Voice Design:** `Deep, booming male voice of an ancient Roman arena herald, middle-aged, theatrical and commanding, slight echo of a stadium announcer, proud and dramatic.`
**Stability:** Natural

| Файл | Реплика |
|---|---|
| ann_wave_01 | [loud, commanding] Волна начинается! К оружию! |
| ann_wave_02 | [dramatic] Они идут! Защищайте портал! |
| ann_wave_03 | [excited] Новая волна! Пусть арена услышит звон стали! |
| ann_wave_clear_01 | [proud] Волна отбита! Арена ликует! |
| ann_wave_clear_02 | [satisfied] Прекрасный бой, воин! Готовься к следующему. |
| ann_boss | [ominous, slow] Затихните… На арену выходит чемпион тьмы. |
| ann_portal_low | [urgent, shouting] Портал слабеет! Держите их! |
| ann_victory | [triumphant, loud] Тридцать волн! Ты — легенда Колизея! |
| ann_defeat | [somber, slow] Портал пал… Арена погрузилась во тьму. |
| ann_endless | [excited, intense] Бесконечная битва! Посмотрим, сколько ты продержишься! |
| ann_new_enemy | [curious] Это ещё что за тварь? Смотри внимательно! |

### Герой — короткие возгласы
**Voice Design:** `Young male warrior, confident and determined, strong mid-range voice, slightly rough, heroic.`
**Stability:** Natural

| Файл | Реплика |
|---|---|
| hero_ult | [shouting, powerful] Разлом Пустоты! |
| hero_ult_storm | [shouting] Алая буря! |
| hero_ult_zero | [cold, intense] Абсолютный ноль. |
| hero_levelup | [determined] Я стал сильнее. |
| hero_low_hp | [breathing heavily] Нужно… отступить… |
| hero_build | [focused] Здесь будет башня. |
| hero_finisher | [roaring] Конец тебе! |

### Минотавр (босс 5-й волны)
**Voice Design:** `Huge brutal half-bull monster, extremely deep and gravelly growling male voice, snorting breaths between words, slow and menacing.`
**Stability:** Creative

| Файл | Реплика |
|---|---|
| minotaur_intro | [snorts, angry] Кто посмел войти в мой лабиринт? Я растопчу тебя! |
| minotaur_taunt_01 | [growling] Твои стрелы — щекотка для моей брони! |
| minotaur_taunt_02 | [laughs menacingly] Беги, букашка… беги! |
| minotaur_armor | [roaring, furious] Моя броня! Ты за это заплатишь! |
| minotaur_death | [pained, fading] Лабиринт… пал… |

### Цербер (босс 10-й волны) — без слов
**Voice Design:** `Three-headed hellhound speaking in three overlapping monstrous voices, deep demonic growls, barely human words.`
**Stability:** Creative

| Файл | Реплика |
|---|---|
| cerberus_intro | [growling, three voices] Мы… стережём… врата. Ты… не пройдёшь. |
| cerberus_head | [howling in pain] Одна голова… ещё две… ГОЛОДНЫ! |
| cerberus_death | [whimpering, fading] Врата… открыты… |

### Гидра (босс 15-й волны)
**Voice Design:** `Ancient sea serpent with many heads, several hissing reptilian voices speaking together, sibilant and wet, eerie.`
**Stability:** Creative

| Файл | Реплика |
|---|---|
| hydra_intro | [hissing, many voices] Ссруби одну голову… и вырасстут две! |
| hydra_regrow | [laughing, hissing] Ещё голова! Ещё сссильнее! |
| hydra_burned | [screaming] Огонь! Нет! Не огонь! |
| hydra_death | [fading hiss] Ссслишком… много… огня… |

### Медуза (босс 20-й волны)
**Voice Design:** `Seductive and cold female voice of a gorgon, smooth and hypnotic with a hissing undertone, mature, elegant and dangerous.`
**Stability:** Creative

| Файл | Реплика |
|---|---|
| medusa_intro | [whispers, seductive] Посмотри мне в глаза, герой… Только один раз. |
| medusa_gaze | [hissing] Застынь… навеки. |
| medusa_taunt | [laughs coldly] Твои башни теперь — мои статуи. |
| medusa_stagger | [gasping, angry] Нет… не смей… |
| medusa_death | [fading whisper] Я… ещё… увижу тебя… в камне… |

### Королева гарпий (босс 25-й волны, летающая)
**Voice Design:** `Shrill, screeching female voice of a monstrous bird queen, wild and arrogant, piercing high pitch with raspy edges.`
**Stability:** Creative

| Файл | Реплика |
|---|---|
| harpy_queen_intro | [screeching, arrogant] Небо принадлежит мне! Ползай в пыли, человек! |
| harpy_queen_summon | [shrieking] Сёстры! Растерзайте его! |
| harpy_queen_grounded | [panicked screech] Мои крылья! Нет! |
| harpy_queen_death | [fading screech] Небо… без меня… |

### Титан (финальный босс 30-й волны)
**Voice Design:** `Colossal ancient titan made of stone, extremely deep slow resonant male voice, rumbling like an earthquake, calm, ancient and overwhelming.`
**Stability:** Natural

| Файл | Реплика |
|---|---|
| titan_intro | [slow, deep, calm] Я старше этих стен. Старше богов. Ты — пыль. |
| titan_phase2 | [rumbling, angry] Огонь и лёд… склонятся передо мной. |
| titan_taunt | [slow] Тридцать волн… и всё напрасно. |
| titan_kneel | [strained, surprised] Ты… поставил меня… на колени? |
| titan_death | [fading, almost respectful] Наконец… достойный… противник. |

---

## Часть 3. Suno → музыка (`Audio/Music/`)

**Где:** Suno → *Create* → режим **Custom**.
- **Instrumental — ВКЛ** (без вокала; иначе Suno запоёт). Для гимна-победы можно хор без слов — включи Instrumental
  и всё равно напиши «choir» в стиле.
- **Style of Music** — короткий список (4–7 описаний), копируй из блока «Style».
- **Exclude styles** (в *Advanced Options*) — то, чего быть не должно. Не пиши «no guitar» в Style — только в Exclude.
- **Lyrics** — для инструментала туда можно вписать **структуру** в квадратных скобках (блок «Structure» ниже):
  это подсказка, как должна развиваться пьеса.
- **Ползунки** (если есть): *Weirdness* ≈ 30–40 %, *Style Influence* ≈ 70 % (держаться стиля).
- Модель — самая новая (v5 или новее). Длина — 2–3 минуты достаточно, я сделаю бесшовный цикл.
- Генерируется 2 варианта — сохрани **лучший**, а если оба хороши — оба (`_a`, `_b`), я поставлю их по очереди.

### music_menu — главное меню / выбор сложности
- **Style:** `epic orchestral fantasy, ancient Rome, heroic brass, war drums, choir, lyre`
- **Exclude:** `electric guitar, synthesizer, EDM, dubstep, rock drums, vocals with lyrics`
- **Structure:** `[Intro: solo lyre and soft strings] [Build: war drums enter] [Main theme: heroic brass and choir] [Bridge: calm strings] [Outro: brass fanfare]`

### music_prepare — фаза подготовки (спокойно, думаем)
- **Style:** `calm ambient orchestral, ancient Mediterranean, lyre, soft strings, light frame drum, strategic, mysterious`
- **Exclude:** `electric guitar, synthesizer, EDM, heavy drums, vocals`
- **Structure:** `[Intro: lyre] [Theme: soft strings and flute] [Interlude: frame drum pulse] [Theme] [Outro]`

### music_battle_1 — волны 1–10 (акт I)
- **Style:** `epic battle orchestral, Roman legion, energetic war drums, brass, strings ostinato, driving`
- **Exclude:** `electric guitar, synthesizer, EDM, dubstep, vocals`
- **Structure:** `[Intro: war drums] [Main: strings ostinato and brass] [Build] [Climax: full orchestra] [Loop back to main]`

### music_battle_2 — волны 11–20 (акт II, мрачнее)
- **Style:** `dark epic orchestral, ancient myth, intense taiko and war drums, low brass, choir chants, urgent`
- **Exclude:** `electric guitar, synthesizer, EDM, vocals with lyrics, cheerful`
- **Structure:** `[Intro: low choir] [Main: driving drums and low brass] [Build] [Climax: choir and full orchestra]`

### music_battle_3 — волны 21–30 (акт III, на пределе)
- **Style:** `apocalyptic epic orchestral, massive war drums, aggressive brass, fast strings, dark choir, relentless`
- **Exclude:** `electric guitar, synthesizer, EDM, dubstep, vocals with lyrics`
- **Structure:** `[Intro: thunderous drums] [Main: fast strings and brass stabs] [Build] [Climax: choir and drums] [Breakdown] [Climax]`

### music_boss — боссы 5–25 волн
- **Style:** `epic boss battle orchestral, menacing low brass, pounding drums, dark choir, tense strings, ancient Greek myth`
- **Exclude:** `electric guitar, synthesizer, EDM, cheerful, vocals with lyrics`
- **Structure:** `[Intro: ominous drums and low horn] [Main: aggressive choir and brass] [Build] [Climax] [Main]`

### music_titan — финальный босс (Титан)
- **Style:** `colossal epic orchestral, titanic, huge choir, organ, massive percussion, heroic and desperate, cinematic`
- **Exclude:** `electric guitar, synthesizer, EDM, dubstep, vocals with lyrics`
- **Structure:** `[Intro: deep organ and rumbling drums] [Main: huge choir] [Heroic theme: brass] [Climax: everything] [Final crescendo]`

### music_victory — победа (короткая, 30–60 с)
- **Style:** `triumphant orchestral fanfare, heroic brass, choir, celebration, ancient Rome`
- **Exclude:** `electric guitar, synthesizer, sad, vocals with lyrics`
- **Structure:** `[Fanfare] [Triumphant theme] [Final chord]`

### music_defeat — поражение (короткая, 30–60 с)
- **Style:** `somber orchestral lament, solo cello, soft choir, slow, tragic, ancient`
- **Exclude:** `drums, electric guitar, synthesizer, cheerful`
- **Structure:** `[Solo cello] [Choir enters softly] [Fade]`

### music_endless — бесконечный режим
- **Style:** `relentless epic orchestral, hypnotic ostinato, war drums, dark choir, rising intensity, endless battle`
- **Exclude:** `electric guitar, synthesizer, EDM, vocals with lyrics`
- **Structure:** `[Intro] [Ostinato] [Build] [Climax] [Ostinato]`

---

## Что делать с файлами

1. Сложи всё в `Assets/_Project/Audio/` по папкам из таблиц (`SFX/Player`, `SFX/Towers`, `Voice/Minotaur`,
   `Music` и т.д.), имена — как в таблицах, расширение `.mp3`.
2. Не обязательно делать всё сразу — можно частями (например, сначала меч/лук/башни и музыку боя). Я подключу то,
   что есть, а для отсутствующих файлов игра просто будет молчать.
3. Напиши мне — я пропишу подключение: громкость, случайные варианты, 3D-звук от врагов и башен, смену музыки
   по фазам и актам, реплики боссов в нужные моменты, ползунки громкости (музыка / звуки / голоса).
