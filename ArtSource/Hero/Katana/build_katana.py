# Катана Рэна. Запуск в Blender: exec(open(r"...\build_katana.py", encoding="utf-8").read()); build(); export()
# Оси: клинок вдоль +Y, лезвие (ха) смотрит в -Z, плоскости клинка — ±X. Начало координат — место хвата
# (рука чуть ниже цубы). После экспорта: FBX → Assets/_Project/Art/Hero/Katana.fbx (Unity), GLB → Tools/SoundLab.
import bpy, bmesh, math, os
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else \
    r"C:\Users\А ну типо да\Desktop\ColizeunGame\ArtSource\Hero\Katana"
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))

# Размеры (метры) — чуть крупнее настоящей, чтобы читалась в игре
L_BLADE = 0.80      # нагаса
W_BASE = 0.034      # ширина у хабаки
W_TIP = 0.024       # ширина у ёкотэ
T_BASE = 0.0085     # толщина обуха
SORI = 0.022        # прогиб
KISSAKI = 0.075     # длина острия
GRIP_Y = 0.07       # от хвата до цубы
L_TSUKA = 0.28

MATS = {  # имя: (цвет, металл, гладкость)
    "Katana_Steel": ((0.62, 0.65, 0.70), 1.0, 0.80),
    "Katana_Edge":  ((0.90, 0.92, 0.95), 1.0, 0.92),
    "Katana_Gold":  ((0.85, 0.62, 0.22), 1.0, 0.70),
    "Katana_Iron":  ((0.10, 0.09, 0.09), 0.9, 0.45),
    "Katana_Wrap":  ((0.05, 0.05, 0.07), 0.0, 0.25),
    "Katana_Same":  ((0.86, 0.83, 0.76), 0.0, 0.35),
    "Katana_Red":   ((0.45, 0.04, 0.04), 0.0, 0.40),
}


def mat(name):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    col, met, smooth = MATS[name]
    m.use_nodes = True
    bsdf = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Base Color"].default_value = (*col, 1)
    bsdf.inputs["Metallic"].default_value = met
    bsdf.inputs["Roughness"].default_value = 1 - smooth
    m.diffuse_color = (*col, 1)
    return m


def new_obj(name, bm, mats):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for mn in mats:
        me.materials.append(mat(mn))
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    for p in me.polygons:
        p.use_smooth = True
    return ob


def blade_axis(s):
    """Центр обуха на расстоянии s от хабаки: прогиб уводит острие в сторону обуха (+Z)."""
    u = s / L_BLADE
    return Vector((0, GRIP_Y + 0.035 + s, W_BASE / 2 + SORI * (u ** 1.7)))


def build_blade():
    bm = bmesh.new()
    rings = []
    N = 90
    for i in range(N + 1):
        s = L_BLADE * i / N
        u = s / L_BLADE
        w = W_BASE + (W_TIP - W_BASE) * u
        t = T_BASE * (1 - 0.45 * u)
        tip_start = L_BLADE - KISSAKI
        if s > tip_start:  # кисаки: лезвие закругляется к обуху
            k = (s - tip_start) / KISSAKI
            w *= math.sqrt(max(0.0, 1 - k * k)) * 0.98 + 0.002
            t *= (1 - 0.8 * k)
        # хамон — волнистая граница закалки
        h = 0.62 + 0.07 * math.sin(s * 55) + 0.03 * math.sin(s * 137 + 1)
        c = blade_axis(s)
        # сечение: обух (0) → шиноги (0.38) → хамон (h) → лезвие (1); Z вниз = к лезвию
        prof = [(0.0, 0.30), (0.05, 0.45), (0.38, 0.50), (h, 0.32), (1.0, 0.0),
                (h, -0.32), (0.38, -0.50), (0.05, -0.45), (0.0, -0.30)]
        ring = [bm.verts.new(c + Vector((a * t, 0, -f * w))) for f, a in prof[:-1]]
        rings.append(ring)
    n = len(rings[0])
    for i in range(N):
        a, b = rings[i], rings[i + 1]
        for j in range(n):
            f = bm.faces.new((a[j], a[(j + 1) % n], b[(j + 1) % n], b[j]))
            f.material_index = 1 if j in (3, 4) else 0   # грани у лезвия — светлый хамон
    tip = bm.verts.new(blade_axis(L_BLADE) + Vector((0, 0.004, -0.002)))
    for j in range(n):
        f = bm.faces.new((rings[-1][j], rings[-1][(j + 1) % n], tip))
        f.material_index = 1 if j in (3, 4) else 0
    bm.faces.new(list(reversed(rings[0])))
    bm.normal_update()
    # острая кромка, шиноги и обух — без сглаживания (разрезаем рёбра по углу)
    sharp = [e for e in bm.edges if len(e.link_faces) == 2 and
             e.calc_face_angle(0) > math.radians(30)]
    bmesh.ops.split_edges(bm, edges=sharp)
    return new_obj("Katana_Blade", bm, ["Katana_Steel", "Katana_Edge"])


def tube(name, y0, y1, rx0, rz0, rx1=None, rz1=None, seg=24, mats=("Katana_Gold",), zoff=0.0, shape=None):
    rx1 = rx0 if rx1 is None else rx1
    rz1 = rz0 if rz1 is None else rz1
    bm = bmesh.new()
    rings = []
    steps = 1 if shape is None else 24
    for i in range(steps + 1):
        u = i / steps
        y = y0 + (y1 - y0) * u
        k = 1 if shape is None else shape(u)
        rx, rz = (rx0 + (rx1 - rx0) * u) * k, (rz0 + (rz1 - rz0) * u) * k
        rings.append([bm.verts.new((rx * math.cos(2 * math.pi * j / seg), y,
                                    zoff + rz * math.sin(2 * math.pi * j / seg))) for j in range(seg)])
    for i in range(steps):
        for j in range(seg):
            bm.faces.new((rings[i][j], rings[i][(j + 1) % seg], rings[i + 1][(j + 1) % seg], rings[i + 1][j]))
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])
    return new_obj(name, bm, list(mats))


def build_tsuba():
    """Цуба: «мокко» (четырёхлепестковая), тёмное железо с золотым ободком."""
    bm = bmesh.new()
    seg = 64
    y0, y1 = GRIP_Y + 0.006, GRIP_Y + 0.014

    def r(a, scale):
        lobes = 1 + 0.10 * math.cos(4 * a)          # четыре лепестка
        return 0.043 * lobes * scale

    rings = []
    for y, sc in ((y0, 0.97), (y0 + 0.0015, 1.0), (y1 - 0.0015, 1.0), (y1, 0.97)):
        rings.append([bm.verts.new((r(2 * math.pi * j / seg, sc) * math.cos(2 * math.pi * j / seg) * 0.9, y,
                                    r(2 * math.pi * j / seg, sc) * math.sin(2 * math.pi * j / seg)))
                      for j in range(seg)])
    for i in range(3):
        for j in range(seg):
            f = bm.faces.new((rings[i][j], rings[i][(j + 1) % seg], rings[i + 1][(j + 1) % seg], rings[i + 1][j]))
            f.material_index = 1 if i == 1 else 0
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])
    ob = new_obj("Katana_Tsuba", bm, ["Katana_Iron", "Katana_Gold"])
    return ob


def build_tsuka():
    """Рукоять: белая кожа ската (саме) под чёрной обмоткой ито с ромбами."""
    bm = bmesh.new()
    seg, steps = 64, 160
    y_top, y_bot = GRIP_Y + 0.004, GRIP_Y - L_TSUKA
    rings = []
    params = []
    for i in range(steps + 1):
        u = i / steps
        y = y_top + (y_bot - y_top) * u
        waist = 1 - 0.07 * math.sin(math.pi * u)        # лёгкая «талия»
        rx, rz = 0.0125 * waist, 0.0165 * waist
        ring = []
        for j in range(seg):
            a = 2 * math.pi * j / seg
            ring.append(bm.verts.new((rx * math.cos(a), y, rz * math.sin(a) + 0.003 * u)))
        rings.append(ring)
        params.append(u)
    N = 8  # ромбов по длине
    for i in range(steps):
        for j in range(seg):
            f = bm.faces.new((rings[i][j], rings[i][(j + 1) % seg], rings[i + 1][(j + 1) % seg], rings[i + 1][j]))
            u = (i + 0.5) / steps
            v = ((j + 0.5) / seg) % 1.0
            # ромбы на широких сторонах (v=0 и v=0.5 — по X); ито — всё, что вне ромба
            du = abs((u * N) % 1.0 - 0.5) * 2          # 0 в центре ромба, 1 на стыке
            dv = min(abs(v - 0.0), abs(v - 1.0), abs(v - 0.5)) / 0.2
            inside = du + dv < 0.85 and 0.04 < u < 0.96
            f.material_index = 1 if inside else 0
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])
    # обмотка выпуклая: поднимем вершины, у которых все соседние грани — ито
    for v in bm.verts:
        if v.link_faces and all(f.material_index == 0 for f in v.link_faces):
            n = Vector((v.co.x, 0, v.co.z)).normalized()
            v.co += n * 0.0018
    return new_obj("Katana_Tsuka", bm, ["Katana_Wrap", "Katana_Same"])


def build_details():
    parts = []
    # хабаки (золото у основания клинка) и сэппа (шайбы)
    parts.append(tube("Katana_Habaki", GRIP_Y + 0.014, GRIP_Y + 0.042, 0.0072, 0.022, 0.0062, 0.020,
                      seg=20))
    parts.append(tube("Katana_Seppa1", GRIP_Y + 0.004, GRIP_Y + 0.006, 0.016, 0.024))
    parts.append(tube("Katana_Seppa2", GRIP_Y + 0.014, GRIP_Y + 0.016, 0.014, 0.022))
    # фути (кольцо у цубы) и касира (навершие)
    parts.append(tube("Katana_Fuchi", GRIP_Y - 0.012, GRIP_Y + 0.004, 0.0142, 0.0185, mats=("Katana_Iron",)))
    parts.append(tube("Katana_Kashira", GRIP_Y - L_TSUKA - 0.006, GRIP_Y - L_TSUKA + 0.014, 0.0135, 0.0175,
                      mats=("Katana_Iron",), zoff=0.003,
                      shape=lambda u: 0.88 + 0.12 * math.sin(math.pi * min(1, u * 1.6 + 0.2))))
    # менуки (золотые украшения под обмоткой) и мекуги (бамбуковый штифт)
    for side in (-1, 1):
        bm = bmesh.new()
        bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=8, radius=1)
        c = Vector((side * 0.0145, GRIP_Y - 0.11 - 0.03 * side, 0.002))
        for v in bm.verts:
            v.co = c + Vector((v.co.x * 0.003, v.co.y * 0.018, v.co.z * 0.006))
        parts.append(new_obj("Katana_Menuki" + ("L" if side < 0 else "R"), bm, ["Katana_Gold"]))
    # кисточка-сагэо на навершии (красная) — для узнаваемости силуэта
    parts.append(tube("Katana_Tassel", GRIP_Y - L_TSUKA - 0.06, GRIP_Y - L_TSUKA - 0.008, 0.002, 0.002,
                      0.006, 0.006, seg=10, mats=("Katana_Red",), zoff=0.003))
    return parts


def build():
    for ob in list(bpy.data.objects):
        if ob.name.startswith("Katana"):
            bpy.data.objects.remove(ob, do_unlink=True)
    objs = [build_blade(), build_tsuba(), build_tsuka()] + build_details()
    # объединяем всё в один меш (один объект в Unity), материалы — общим списком
    names = list(MATS.keys())
    bm = bmesh.new()
    for ob in objs:
        part = bmesh.new()
        part.from_mesh(ob.data)
        remap = [names.index(m.name) for m in ob.data.materials]
        for f in part.faces:
            f.material_index = remap[f.material_index]
        tmp = bpy.data.meshes.new("tmp")
        part.to_mesh(tmp)
        part.free()
        bm.from_mesh(tmp)
        bpy.data.meshes.remove(tmp)
        me = ob.data
        bpy.data.objects.remove(ob, do_unlink=True)
        bpy.data.meshes.remove(me)
    used = sorted({f.material_index for f in bm.faces})
    k = new_obj("Katana", bm, names)
    return k


def export():
    k = bpy.data.objects["Katana"]
    bpy.ops.object.select_all(action="DESELECT")
    k.select_set(True)
    bpy.context.view_layer.objects.active = k
    fbx = os.path.join(ROOT, "Assets", "_Project", "Art", "Hero", "Katana.fbx")
    os.makedirs(os.path.dirname(fbx), exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, apply_scale_options="FBX_SCALE_UNITS",
                             mesh_smooth_type="FACE", add_leaf_bones=False)
    glb = os.path.join(ROOT, "Tools", "SoundLab", "katana.glb")
    os.makedirs(os.path.dirname(glb), exist_ok=True)
    bpy.ops.export_scene.gltf(filepath=glb, use_selection=True, export_format="GLB", export_yup=True)
    return fbx, glb
