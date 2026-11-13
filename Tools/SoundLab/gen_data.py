# Пересобирает sounds_data.js и katana_glb.js. Запуск: python Tools/SoundLab/gen_data.py
import re, json, base64, os
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
md = open(os.path.join(ROOT, "ArtSource", "Audio", "AUDIO_PROMPTS.md"), encoding="utf-8").read()
part1 = md.split("## Часть 2")[0]
out, folder, title = [], None, None
for line in part1.splitlines():
    m = re.match(r"### (\d\.\d) (.+?) \(`Audio/SFX/(\w+)/`\)", line)
    if m:
        title, folder = m.group(2), m.group(3)
        continue
    if not folder or not line.startswith("| ") or line.startswith("| Файл") or line.startswith("|---"):
        continue
    cells = [c.strip() for c in line.strip().strip("|").split("|")]
    name = cells[0]
    slots = 1
    mm = re.match(r"(\w+?)_01\.\.(\d+)$", name)
    if mm:
        name, slots = mm.group(1), int(mm.group(2))
    dur = float(re.sub(r"[^\d.]", "", cells[2]) or 1)
    infl = cells[3]
    loop = (len(cells) > 4 and "loop" in cells[4]) or "loop" in infl
    infl = re.sub(r"\s*\(loop\)", "", infl)
    out.append(dict(name=name, slots=slots, folder=folder, group=title, prompt=cells[1], dur=dur, infl=infl, loop=bool(loop)))
with open(os.path.join(HERE, "sounds_data.js"), "w", encoding="utf-8") as f:
    f.write("// Автоматически из ArtSource/Audio/AUDIO_PROMPTS.md (python Tools/SoundLab/gen_data.py)\n")
    f.write("window.SOUNDS = " + json.dumps(out, ensure_ascii=False, indent=0) + ";\n")
glb = os.path.join(HERE, "katana.glb")
with open(os.path.join(HERE, "katana_glb.js"), "w", encoding="utf-8") as f:
    f.write("// Модель катаны (ArtSource/Hero/Katana) в base64 — чтобы страница открывалась прямо из папки, без сервера\n")
    f.write("window.KATANA_GLB = \"" + base64.b64encode(open(glb, "rb").read()).decode() + "\";\n")
print(len(out), "sounds;", sorted({s["folder"] for s in out}))
