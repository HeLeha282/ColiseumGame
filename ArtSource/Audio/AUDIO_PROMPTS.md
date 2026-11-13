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

## Часть 2. ElevenLabs → голоса, реплики и диалоги

### Лор в двух словах (чтобы попадать в интонацию)
- **Рэн**, 19 лет, японский «ронин» — так в Японии зовут тех, кто провалил экзамены в университет и год готовится
  к пересдаче. Капитан школьного клуба кендо, ходил и на кюдо. Вечером перед пересдачей засиделся в пустом додзё —
  и портал из старой монеты деда затянул его на арену. Иронизирует, храбрится, скучает по дому, переживает за экзамен.
- **Янус** — двуликий римский бог врат. Это он открыл портал и выбрал Рэна. Обещает вернуть домой, если тот
  выстоит 30 волн. Помогает, но что-то недоговаривает: Рэн — не первый его чемпион. «Предыдущий» — это **Титан**.
- **Глашатай** — голос арены. Обращается не к Рэну, а к **богам-зрителям** на трибунах: для них это зрелище.
- **Боссы** — чемпионы арены прошлых эпох. Они знают, кто такой «пришелец Януса», и не любят его.

### Как генерировать
1. **Голос — готовый, из библиотеки** (свой создавать не нужно): **Voices → Voice Library**, фильтры
   **Language: Russian**, пол и возраст — как в таблице ниже; в поиск — ключевые слова из таблицы. Слушай превью
   на русской фразе, понравившийся — **Add to my voices** и переименуй в имя персонажа.
   Русскоязычный голос важен: английские голоса (в т. ч. стандартные Default) говорят по-русски с акцентом.
2. **Text to Speech**, модель **Eleven v3** (или новее), выбери голос.
3. **Stability** — как указано у персонажа. Если голос «плывёт» — ближе к *Natural/Robust*.
4. Одна реплика — один файл. Слова в `[квадратных скобках]` — подсказки интонации, не озвучиваются. Если тег
   не сработал — убери его.
5. 2–3 дубля, сохрани лучший: `Audio/Voice/<Персонаж>/<файл>.mp3`. **Папка — того, кто говорит** (колонка «Кто»):
   реплики Рэна → `Voice/Hero`, Януса → `Voice/Janus`, глашатая → `Voice/Announcer`, боссов → их папки.
6. **Чудовищность добавлю в игре сам**, поэтому монстрам не нужен «идеальный монстр» в библиотеке — достаточно
   подходящего по характеру низкого/хриплого голоса: Минотавр и Титан — понижение тона + реверб; Цербер — три копии
   с разным тоном; Гидра — несколько копий с шипением; Королева гарпий — повышение тона + резкость; Янус — вторая
   копия на тон выше («два лица»). Главное — чтобы голос нормально играл интонацию.

### Голоса — что выбрать

Сначала ищи в Voice Library с фильтром **Russian** (колонки «Фильтры» и «Ищи»). Если русского подходящего нет — запасной
вариант из стандартных английских голосов (колонка «Запасной»; список стандартных у ElevenLabs меняется —
если такого нет, бери похожий по описанию).

| Персонаж | Папка | Фильтры | Ищи (ключевые слова) | Какой нужен | Запасной | Stability |
|---|---|---|---|---|---|---|
| Рэн (герой) | `Voice/Hero` | Male, Young | `young`, `energetic`, `anime`, `student`, `characters` | Молодой, светлый, живой; умеет и паниковать, и шутить, и кричать в бою. Не «диктор». | Liam / Harry | Natural |
| Янус | `Voice/Janus` | Male, Old | `old`, `wise`, `narrator`, `deep`, `storyteller`, `mysterious` | Старый, спокойный, глубокий, медленный; лёгкая ирония. Не злодейский. | George / Bill | Natural |
| Глашатай | `Voice/Announcer` | Male, Middle-aged | `announcer`, `epic`, `trailer`, `commentator`, `bold` | Громкий, театральный, «на публику». | Daniel / Brian | Natural |
| Минотавр | `Voice/Minotaur` | Male, Middle-aged/Old | `deep`, `gruff`, `raspy`, `villain`, `orc`, `monster` | Очень низкий, грубый, хриплый. | Clyde / Callum | Creative |
| Цербер | `Voice/Cerberus` | Male | `growl`, `demon`, `beast`, `villain`, `raspy` | Рычащий, злой, отрывистый (3 головы сделаю копиями). | Callum / Clyde | Creative |
| Гидра | `Voice/Hydra` | Male или Female | `whisper`, `creepy`, `sinister`, `snake`, `villain` | Шипящий, вкрадчивый, насмешливый. | Callum (шёпотом) | Creative |
| Медуза | `Voice/Medusa` | Female, Middle-aged | `seductive`, `cold`, `villain`, `sultry`, `elegant` | Холодный, гипнотический, взрослый, опасный. | Charlotte / Lily | Creative |
| Королева гарпий | `Voice/HarpyQueen` | Female | `witch`, `shrill`, `crazy`, `villain`, `raspy` | Резкий, визгливый, надменный. | Glinda / Mimi (если есть) | Creative |
| Титан | `Voice/Titan` | Male, Old | `deep`, `tired`, `sad`, `old`, `narrator`, `bass` | Очень низкий, медленный, усталый, горький (не орущий — он трагический). | Brian / Bill | Natural |

**Советы:** Янус и Титан должны звучать **по-разному** (Янус — мягкий и ироничный, Титан — тяжёлый и усталый) —
слушай их подряд перед выбором. Голос героя проверь на трёх репликах: паника (`intro_04`), шутка (`tut_build_hero`),
боевой крик (`hero_ult_*`) — молодые голоса из библиотеки часто хороши только в одном из этого.

> Если хотите дать герою другое имя — замените «Рэн» в репликах, больше ничего менять не нужно.

---

### 2.1 Вступление (для будущей катсцены в начале игры)
Сцену пока не делаем, но реплики можно записать заранее — я подключу их, когда появится катсцена.

| Файл | Кто | Реплика |
|---|---|---|
| intro_01 | Янус | [calm, slow] Проснись, чужеземец. Ты не спишь… хотя и хотел бы. |
| intro_02 | Рэн | [groggy, confused] Где я?.. Это что… Колизей?! |
| intro_03 | Янус | Арена, что стоит между мирами. Боги любят зрелища, а здешние бойцы им наскучили. Я открыл врата — и выбрал тебя. |
| intro_04 | Рэн | [panicking] Выбрал?! У меня завтра пересдача! Второй раз я её не завалю! |
| intro_05 | Янус | [amused] Тогда тебе стоит поторопиться. Эти врата — твоя единственная дорога домой. |
| intro_06 | Янус | [serious] Тридцать волн тварей придут, чтобы разбить их. Выстоишь — я открою путь назад. Падут врата — останешься здесь навсегда. |
| intro_07 | Рэн | [sarcastic] А если я откажусь? |
| intro_08 | Янус | Тогда арена решит за тебя. Возьми молот — строй башни. Возьми меч — сражайся. И помни: боги смотрят. |
| intro_09 | Янус | [quietly, almost to himself] …Не разочаруй меня, как предыдущий. |
| intro_10 | Рэн | [nervous laugh] «Как предыдущий»? Эй! Что значит — как предыдущий?! |
| intro_11 | Глашатай | [booming] Боги Олимпа! Сегодня на арене — чужеземец из далёкого мира! Да начнутся игры! |

Файлы: реплики Януса → `Voice/Janus/intro_0X`, Рэна → `Voice/Hero/intro_0X`, глашатая → `Voice/Announcer/intro_11`
(номер оставь как в таблице — по нему я выстрою порядок).

### 2.2 Первые волны — обучение и знакомство
Играют один раз, в нужный момент (при первом событии). Сначала реплика Януса, потом ответ Рэна.

| Файл | Кто | Когда | Реплика |
|---|---|---|---|
| tut_prepare_janus | Янус | первая подготовка | Пока тихо — строй. Возьми молот, клавиша три. Башни не устают, в отличие от тебя. |
| tut_prepare_hero | Рэн | сразу после | [muttering] Строить башни посреди Колизея… Ладно. Как в стратегии. Я в такое играл. |
| tut_build_janus | Янус | первая башня | [impressed] Молот слушается тебя. Ни один гладиатор здесь так не умел. |
| tut_build_hero | Рэн | сразу после | [proud] Вот это я понимаю — «игровой опыт»! |
| tut_wave1_janus | Янус | старт 1-й волны | Слышишь горн? Это скелеты легиона. Мертвецы, что не знают поражения… пока не встретят меч. |
| tut_wave1_hero | Рэн | сразу после | [deep breath] Кендо, не подведи. |
| tut_mark_janus | Янус | первый удар героя | Каждый твой удар оставляет метку. Башни бьют помеченных сильнее. Сражайтесь вместе. |
| tut_wave1_clear_hero | Рэн | 1-я волна пройдена | [exhausted, surprised] Я… жив? Я правда это сделал? |
| tut_wave1_clear_janus | Янус | сразу после | Одна из тридцати. Не празднуй слишком рано. |
| tut_levelup_janus | Янус | первый уровень героя | Боги заметили тебя. Их дары ждут — открой их, клавиша Tab. |
| tut_levelup_hero | Рэн | сразу после | [excited] Прокачка! Ну хоть что-то здесь работает как надо! |
| tut_shield_janus | Янус | первый щитоносец | Башни не пробьют щит спереди. Зайди сбоку — или ударь сам. |
| tut_flyer_janus | Янус | первая гарпия | Гарпии летят прямо к вратам. Лук — клавиша два. Целься выше. |
| tut_flyer_hero | Рэн | сразу после | [focused] Кюдо, второй клуб. Пригодился. |
| tut_satyr_janus | Янус | первый сатир | Сатиры перепрыгивают баррикады. Не надейся на дерево. |
| tut_mage_janus | Янус | первый маг | Маги бьют издалека. Огонь против льда, лёд против огня. |
| tut_elements_hero | Рэн | первая реакция стихий | [amazed] Огонь плюс лёд — и он разлетелся?! Ладно, это было круто. |
| tut_portal_hit_janus | Янус | первый удар по порталу | [warning] Они у врат. Каждая трещина — шаг прочь от дома. |
| tut_death_janus | Янус | первая смерть героя | Арена не отпускает так просто. Вставай, ронин. |
| tut_death_hero | Рэн | возрождение | [coughing] Ронин… Даже здесь меня так называют. |
| tut_early_janus | Янус | первая возможность досрочной волны | Смелые зовут следующую волну раньше. Боги щедры к смелым. |

### 2.3 Обычные реплики (случайные, по событиям)

| Файл | Кто | Когда | Реплика |
|---|---|---|---|
| hero_wave_start_01 | Рэн | начало волны | [determined] Ну, понеслось. |
| hero_wave_start_02 | Рэн | начало волны | [sighs] Вместо подготовки к экзамену… вот это. |
| hero_wave_start_03 | Рэн | начало волны | [confident] Я готов. Наверное. |
| hero_wave_clear_01 | Рэн | волна пройдена | [relieved] Ещё одна. Ещё чуть-чуть до дома. |
| hero_wave_clear_02 | Рэн | волна пройдена | [smug] Сенсей бы мной гордился. |
| hero_wave_clear_03 | Рэн | волна пройдена | [tired] Мама думает, что я в библиотеке… |
| hero_build_01 | Рэн | постройка башни | Здесь будет башня. |
| hero_build_02 | Рэн | постройка башни | [satisfied] Отлично встала. |
| hero_levelup_01 | Рэн | новый уровень | [determined] Я стал сильнее. |
| hero_levelup_02 | Рэн | новый уровень | [grinning] Вот бы на экзамене так. |
| hero_low_hp | Рэн | мало здоровья | [breathing heavily] Нужно… отступить… |
| hero_ult | Рэн | ультимейт | [shouting, powerful] Разлом Пустоты! |
| hero_ult_storm | Рэн | вариант ультимейта | [shouting] Алая буря! |
| hero_ult_zero | Рэн | вариант ультимейта | [cold, intense] Абсолютный ноль. |
| hero_finisher | Рэн | добивание босса (E) | [roaring] Это конец! |
| hero_portal_low | Рэн | портал при смерти | [panicking] Нет-нет-нет! Только не врата! |
| janus_wave_clear_01 | Янус | волна пройдена | Боги довольны. Продолжай. |
| janus_wave_clear_02 | Янус | волна пройдена | [amused] Ты удивляешь даже меня, ронин. |
| janus_portal_low | Янус | портал при смерти | [urgent] Врата трещат! Защищай их — или забудь о доме! |
| ann_wave_01 | Глашатай | начало волны | [loud] Боги, смотрите! Новая волна выходит на арену! |
| ann_wave_02 | Глашатай | начало волны | [dramatic] Твари идут! Устоит ли чужеземец? |
| ann_wave_03 | Глашатай | начало волны | [excited] Пусть арена услышит звон стали! |
| ann_wave_clear_01 | Глашатай | волна пройдена | [proud] Волна отбита! Боги рукоплещут! |
| ann_wave_clear_02 | Глашатай | волна пройдена | [impressed] Чужеземец всё ещё стоит! Кто бы мог подумать! |
| ann_clean_wave | Глашатай | волна без урона порталу | [delighted] Ни единой трещины на вратах! Великолепно! |
| ann_boss | Глашатай | волна с боссом | [ominous] Затихните, боги… На арену выходит чемпион. |
| ann_new_enemy | Глашатай | новый тип врага | [curious] А вот этого зверя арена ещё не видела! |

### 2.4 Боссы — диалоги с героем
В каждой сцене: глашатай объявляет → босс говорит → Рэн отвечает (в начале боя), реплики в бою, смерть босса, и
короткий комментарий Януса после победы — он понемногу приоткрывает тайну.

#### Минотавр (5-я волна)
| Файл | Кто | Когда | Реплика |
|---|---|---|---|
| ann_minotaur | Глашатай | объявление | [booming] Из лабиринта Крита — Минотавр! Сокрушитель героев! |
| minotaur_intro | Минотавр | появление | [snorts, angry] Пришелец Януса… Мелкий. Тощий. Я растопчу тебя, как всех до тебя! |
| hero_vs_minotaur | Рэн | ответ | [nervous joke] Бык в броне… Кендо такому точно не учит. |
| minotaur_taunt_01 | Минотавр | в бою | [growling] Твои стрелы — щекотка для моей брони! |
| minotaur_taunt_02 | Минотавр | в бою | [laughs menacingly] Беги, букашка… беги! |
| minotaur_armor | Минотавр | броня сбита | [roaring, furious] Моя броня! Ты за это заплатишь! |
| hero_minotaur_armor | Рэн | сразу после | [triumphant] Вот так-то лучше! |
| minotaur_death | Минотавр | смерть | [pained, fading] Лабиринт… пал… |
| janus_after_minotaur | Янус | после победы | Первый чемпион арены пал. Боги сделали ставки… и многие проиграли. |

#### Цербер (10-я волна)
| Файл | Кто | Когда | Реплика |
|---|---|---|---|
| ann_cerberus | Глашатай | объявление | [booming] Страж подземного мира! Трёхглавый Цербер! |
| cerberus_intro | Цербер | появление | [growling, three voices] Мы… стережём… врата. Чужие… врата… не нужны. |
| hero_vs_cerberus | Рэн | ответ | [disbelief] Три головы?! А кормить его кто будет?! |
| cerberus_head | Цербер | голова повержена | [howling in pain] Одна голова… остальные… ГОЛОДНЫ! |
| hero_cerberus_hint | Рэн | первая неудачная атака | [thinking] Огненную — льдом, ледяную — огнём… как задачка по химии. |
| cerberus_death | Цербер | смерть | [whimpering, fading] Врата… открыты… |
| janus_after_cerberus | Янус | после победы | [thoughtful] Даже Цербер не удержал тебя. Тот, кто был до тебя, тоже прошёл этот путь… |
| hero_ask_janus_1 | Рэн | сразу после | [suspicious] Тот, кто был до меня? Янус, кто это был? |
| janus_avoid_1 | Янус | сразу после | [cold] Готовься к следующей волне. |

#### Гидра (15-я волна)
| Файл | Кто | Когда | Реплика |
|---|---|---|---|
| ann_hydra | Глашатай | объявление | [booming] Из болот Лерны — бессмертная Гидра! |
| hydra_intro | Гидра | появление | [hissing, many voices] Ссруби одну голову… и вырасстут две! Сссмешной маленький ронин! |
| hero_vs_hydra | Рэн | ответ | [grim] Читал про тебя. Нужен огонь. Много огня. |
| hydra_regrow | Гидра | голова отросла | [laughing, hissing] Ещё голова! Ещё сссильнее! |
| hydra_burned | Гидра | обрубок прижжён | [screaming] Огонь! Нет! Не огонь! |
| hero_hydra_burn | Рэн | сразу после | [satisfied] Говорил же — огонь. |
| hydra_death | Гидра | смерть | [fading hiss] Ссслишком… много… огня… |
| janus_after_hydra | Янус | после победы | Половина пути позади. Боги начинают ставить на тебя. |
| hero_halfway | Рэн | сразу после | [hopeful] Половина… Держись, пересдача. Я иду. |

#### Медуза (20-я волна)
| Файл | Кто | Когда | Реплика |
|---|---|---|---|
| ann_medusa | Глашатай | объявление | [booming, cautious] Не смотрите ей в глаза, боги! Горгона Медуза! |
| medusa_intro | Медуза | появление | [whispers, seductive] Новая игрушка Януса… Посмотри мне в глаза, мальчик. Только раз. |
| hero_vs_medusa | Рэн | ответ | [nervous] Главное — не смотреть. Не смотреть… Блин, я посмотрел. |
| medusa_gaze | Медуза | взгляд (окаменение башен) | [hissing] Застынь… навеки. |
| medusa_taunt | Медуза | в бою | [laughs coldly] Твои башни теперь — мои статуи. |
| hero_unpetrify | Рэн | расколдовал башню | Нет уж, башня мне ещё нужна! |
| medusa_secret | Медуза | половина здоровья | [mocking] Знаешь, кем был тот, кто стоял здесь до тебя? Янус тебе не сказал? |
| medusa_stagger | Медуза | на коленях (можно E) | [gasping, angry] Нет… не смей… |
| medusa_death | Медуза | смерть | [fading whisper] Спроси Януса… о Титане… |
| hero_ask_janus_2 | Рэн | после победы | [angry] Янус! Что ещё за Титан?! |
| janus_avoid_2 | Янус | сразу после | [quietly] …Скоро узнаешь. Он ждёт тебя в конце. |

#### Королева гарпий (25-я волна, летает)
| Файл | Кто | Когда | Реплика |
|---|---|---|---|
| ann_harpy_queen | Глашатай | объявление | [booming] Владычица неба! Королева гарпий! |
| harpy_queen_intro | Королева | появление | [screeching, arrogant] Небо принадлежит мне! Ползай в пыли, пришелец! |
| hero_vs_harpy_queen | Рэн | ответ | [focused] Сбить на землю — и не дать взлететь. Как воздушный змей, только злой. |
| harpy_queen_summon | Королева | призыв гарпий | [shrieking] Сёстры! Растерзайте его! |
| harpy_queen_grounded | Королева | сбита на землю | [panicked screech] Земля! Нет! Только не земля! |
| hero_wings | Рэн | оторвал крылья (E) | [roaring] Больше не взлетишь! |
| harpy_queen_death | Королева | смерть | [fading screech] Небо… без меня… |
| janus_after_harpy | Янус | после победы | [serious] Последний чемпион ждёт тебя. Он… был моим. Как ты сейчас. |
| hero_realize | Рэн | сразу после | [quiet, shocked] Предыдущий… Это Титан. Он тоже хотел домой? |
| janus_answer | Янус | сразу после | [heavy, sad] Он не выстоял. Врата пали. И арена сделала его своим. |

#### Титан (30-я волна, финал)
| Файл | Кто | Когда | Реплика |
|---|---|---|---|
| ann_titan | Глашатай | объявление | [trembling, grand] Боги… встаньте. Последний чемпион арены. Титан! |
| titan_intro | Титан | появление | [slow, deep, bitter] Ещё один… Он и тебе обещал дорогу домой, мальчик? |
| hero_vs_titan | Рэн | ответ | [firm] Обещал. И я дойду. В отличие от тебя. |
| titan_reply | Титан | сразу после | [rumbling laugh] Я тоже так говорил. Тысячу лет назад. |
| titan_phase2 | Титан | вторая фаза (стихии) | [rumbling, angry] Огонь и лёд… склонятся передо мной. |
| hero_titan_phase2 | Рэн | сразу после | [gritting teeth] Тогда я ударю тем, что не склонится! |
| titan_taunt | Титан | в бою | [slow] Посмотри на эти стены. Здесь все однажды остаются. |
| titan_kneel | Титан | на коленях (можно E) | [strained, surprised] Ты… поставил меня… на колени? |
| hero_titan_finish | Рэн | добивание | [softly, then shouting] Прости. Мне нужно домой! |
| titan_death | Титан | смерть | [fading, almost grateful] Наконец… свобода… Иди, мальчик. Не оглядывайся. |

### 2.5 Финал, поражение, бесконечный режим

| Файл | Кто | Когда | Реплика |
|---|---|---|---|
| ann_victory | Глашатай | победа | [triumphant] Тридцать волн! Боги Олимпа приветствуют нового чемпиона арены! |
| janus_victory | Янус | победа | [warm] Ты сделал то, что не смог никто. Врата открыты, ронин. Ты свободен. |
| hero_victory | Рэн | победа | [relieved laugh] Свободен… А экзамен? Я ведь проспал пересдачу, да? |
| janus_offer_endless | Янус | кнопка «Продолжить?» | [amused] Или… останься ещё ненадолго. Боги хотят большего. |
| hero_endless | Рэн | бесконечный режим | [cocky] Ладно, ещё пара волн. Экзамен всё равно уже проспал. |
| ann_endless | Глашатай | бесконечный режим | [excited] Бесконечная битва! Сколько продержится чемпион?! |
| janus_defeat | Янус | поражение | [sad] Врата пали… Теперь ты принадлежишь арене. Как и он. |
| hero_defeat | Рэн | поражение | [desperate] Нет… Только не так… |
| ann_defeat | Глашатай | поражение | [somber] Портал пал… Арена погрузилась во тьму. |

### 2.6 Подготовка между актами (короткие сцены в начале фазы подготовки)

| Файл | Кто | Когда | Реплика |
|---|---|---|---|
| act2_janus | Янус | перед 11-й волной | Теперь враги умнее. Некоторые пойдут другой дорогой — развилками. |
| act2_hero | Рэн | сразу после | [sighs] Как будто сложность в игре подняли. Ну конечно. |
| act3_janus | Янус | перед 21-й волной | [serious] Арена злится. Сильнейшие пойдут в броне и с дарами богов. Будь готов. |
| act3_hero | Рэн | сразу после | [determined] Девять волн до дома. Я не остановлюсь. |

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
