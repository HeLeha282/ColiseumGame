# Колизей v2 — сборка в Blender по референсам из «Референсы для создания колизея» (stylized hand-painted).
# Запуск в Blender: exec(open(путь, encoding="utf-8").read()); затем build_all() / build_kit() / export_fbx(path).
#
# Геймплейная геометрия НЕ меняется (коллайдеры в Unity остаются из PrototypeBuilder.Level.cs):
#   арена r 0..30 (y=0), 1-й ярус r 30..38 (y=3), 2-й ярус r 38..45 (y=6), стена r 45..46.5 до y=11,
#   ворота на 45/135/225/315°, пандусы T2→T1 (r=37, a-80→a-98) и T1→арена (r=29, a-175→a-197).
# Здесь только визуал. Всё, что выше/дальше стены (трибуны, внешняя аркада, руины, холмы) — декор.
#
# Координаты: U(r, угол Unity, высота) даёт точку Blender, которая после экспорта FBX
# (axis_forward='-Z', axis_up='Y', bake_space_transform=True) встаёт в нужную точку Unity.

import bpy, bmesh, math, random, os
from mathutils import Vector, Matrix

V2_DIR = r"C:\Users\А ну типо да\Desktop\ColizeunGame\ArtSource\Coliseum\v2"
exec(open(os.path.join(V2_DIR, "textures_v2.py"), encoding="utf-8").read())

ARENA_R, T1_R, T1_Y, T2_R, T2_Y, WALL_R, WALL_Y = 30.0, 38.0, 3.0, 45.0, 6.0, 46.5, 11.0
ARCHES = [45.0, 135.0, 225.0, 315.0]
RUIN = (62.0, 113.0)            # обвалившийся сектор внешней аркады и трибун (между воротами 45° и 135°)
COLL, KIT = "ColiseumArt", "KitPreview"
UP = Vector((0, 0, 1))


# ================================================================ координаты

def U(r, deg, y):
    a = math.radians(deg)
    return Vector((-r * math.cos(a), -r * math.sin(a), y))

def radial(deg):
    return U(1.0, deg, 0.0)

def frame_x(deg):
    a = math.radians(deg)
    return Vector((-math.sin(a), math.cos(a), 0.0))

def dangle(a, b):
    return (a - b + 180.0) % 360.0 - 180.0

def near_any(deg, centers, half):
    return any(abs(dangle(deg, c)) < half for c in centers)

def in_ranges(deg, ranges):
    return any(abs(dangle(deg, (lo + hi) * .5)) <= (hi - lo) * .5 for lo, hi in ranges)

RAMP_T1_ARENA = [(a - 209 - 5.8, a - 209 + 5.8) for a in ARCHES]
RAMP_T2_T1 = [(a - 89 - 4.6, a - 89 + 4.6) for a in ARCHES]


class F:
    """Локальная система: x — вдоль, y — вглубь (от зрителя), z — вверх. Лицевая сторона смотрит в −y."""
    def __init__(s, o, x, y, z=UP):
        s.o, s.x, s.y, s.z = Vector(o), Vector(x).normalized(), Vector(y).normalized(), Vector(z).normalized()

    def p(s, x, y, z):
        return s.o + s.x * x + s.y * y + s.z * z

    def at(s, x, y, z, yaw=0.0):
        c, sn = math.cos(yaw), math.sin(yaw)
        return F(s.p(x, y, z), s.x * c + s.y * sn, -s.x * sn + s.y * c, s.z)

    def axes(s, yaw=0.0, tx=0.0, ty=0.0):
        """Повёрнутые оси (рысканье вокруг z, наклоны вокруг x и y) — для небрежно лежащих камней."""
        m = Matrix.Rotation(yaw, 3, 'Z') @ Matrix.Rotation(tx, 3, 'X') @ Matrix.Rotation(ty, 3, 'Y')
        basis = Matrix((s.x, s.y, s.z)).transposed()
        r = basis @ m
        return r.col[0], r.col[1], r.col[2]


def RF(r, deg, y=0.0):
    """Кадр на кольце: x по касательной, y наружу (вглубь стены), лицо стены смотрит к центру."""
    return F(U(r, deg, y), frame_x(deg), radial(deg))


def OF(r, deg, y=0.0):
    """Кадр на кольце для внешнего фасада: лицо смотрит наружу."""
    return F(U(r, deg, y), -frame_x(deg), -radial(deg))


# ================================================================ материалы

#  key: (текстура, режим UV, размер плитки м, цвет sRGB, metal, rough, emission, bevel, сегменты bevel, гладкость)
MATS = {
    "Stone":     ("T2_Stone", "block", 0, (.95, .84, .70), 0.0, .85, 0, .06, 2, True),
    "StoneFar":  ("T2_Stone", "block", 0, (.93, .82, .68), 0.0, .85, 0, .05, 1, True),
    "Paving":    ("T2_Paving", "block", 0, (.92, .83, .72), 0.0, .9, 0, .045, 1, True),
    "Seat":      ("T2_Paving", "block", 0, (.90, .80, .68), 0.0, .9, 0, .04, 1, True),
    "Brick":     ("T2_Brick", "world", 1.5, (1, 1, 1), 0.0, .9, 0, 0, 0, False),
    "Plaster":   ("T2_Plaster", "world", 4.5, (1, 1, 1), 0.0, .9, 0, 0, 0, False),
    "Sand":      ("T2_Sand", "world", 7.0, (1, 1, 1), 0.0, 1.0, 0, 0, 0, False),
    "Grass":     ("T2_Grass", "world", 32.0, (1, 1, 1), 0.0, 1.0, 0, 0, 0, True),
    "Roof":      ("T2_Roof", "world", 2.0, (1, 1, 1), 0.0, .8, 0, 0, 0, False),
    "Banner":    ("T2_Banner", "banner", 0, (1, 1, 1), 0.0, .85, 0, 0, 0, False),
    "Gold":      (None, "none", 0, (.98, .70, .24), 1.0, .32, 0, .015, 1, True),
    "Iron":      (None, "none", 0, (.46, .30, .19), .35, .55, 0, .012, 1, True),
    "Cloth":     (None, "none", 0, (.66, .10, .09), 0.0, .85, 0, 0, 0, True),
    "RedPanel":  (None, "none", 0, (.55, .10, .08), 0.0, .8, 0, 0, 0, False),
    "Shadow":    (None, "none", 0, (.22, .14, .10), 0.0, 1.0, 0, 0, 0, False),
    "Void":      (None, "none", 0, (.03, .02, .02), 0.0, 1.0, 0, 0, 0, False),
    "Moss":      (None, "none", 0, (.48, .56, .16), 0.0, 1.0, 0, 0, 0, True),
    "Leaf":      (None, "none", 0, (.36, .45, .14), 0.0, .9, 0, 0, 0, True),
    "LeafLight": (None, "none", 0, (.62, .64, .18), 0.0, .9, 0, 0, 0, True),
    "Bark":      (None, "none", 0, (.45, .28, .16), 0.0, .9, 0, 0, 0, True),
    "Fire":      (None, "none", 0, (1.0, .45, .10), 0.0, 1.0, 6.0, 0, 0, True),
    "Ember":     (None, "none", 0, (.85, .22, .06), 0.0, 1.0, 2.0, 0, 0, True),
    "PortalGlow": (None, "none", 0, (.25, .95, 1.0), 0.0, .5, 5.0, 0, 0, True),
}

STONES = {"Stone", "StoneFar", "Paving", "Seat"}


# ================================================================ построитель мешей

class Builder:
    def __init__(s, seed=7):
        s.bms = {}
        s.rnd = random.Random(seed)
        s.clip = None                    # руины: (кадр, функция высоты среза, rng)

    def bm(s, mat):
        if mat not in s.bms:
            bm = bmesh.new()
            bm.loops.layers.uv.verify()
            s.bms[mat] = bm
        return s.bms[mat]

    # ---------- UV
    def block_uv(s, bm, faces):
        """Каждой грани блока — свой нарисованный камень целиком (один из 4 вариантов атласа)."""
        uvl = bm.loops.layers.uv.active
        for f in faces:
            f.normal_update()
            vs = [l.vert.co for l in f.loops]
            best = None
            for i in range(len(vs)):
                e = vs[(i + 1) % len(vs)] - vs[i]
                if e.length < 1e-6:
                    continue
                score = abs(e.normalized().z) - e.length * .001
                if best is None or score < best[0]:
                    best = (score, e.normalized())
            e1 = best[1]
            e2 = f.normal.cross(e1).normalized()
            pts = [((v - vs[0]).dot(e1), (v - vs[0]).dot(e2)) for v in vs]
            u0, u1 = min(p[0] for p in pts), max(p[0] for p in pts)
            v0, v1 = min(p[1] for p in pts), max(p[1] for p in pts)
            du, dv = max(u1 - u0, 1e-6), max(v1 - v0, 1e-6)
            k = s.rnd.randrange(4)
            ox, oy = (k % 2) * .5, (k // 2) * .5
            flip = s.rnd.random() < .5
            for l, (a, b) in zip(f.loops, pts):
                u = (a - u0) / du
                v = (b - v0) / dv
                if flip:
                    u = 1 - u
                l[uvl].uv = (ox + .006 + u * .488, oy + .006 + v * .488)

    def cyl_uv(s, bm, faces, base, axis, height):
        """Цилиндр (ствол колонны) — один камень, обёрнутый по окружности."""
        uvl = bm.loops.layers.uv.active
        ref = axis.orthogonal().normalized()
        ref2 = axis.cross(ref)
        k = s.rnd.randrange(4)
        ox, oy = (k % 2) * .5, (k // 2) * .5
        sides = []
        for f in faces:
            f.normal_update()
            if abs(f.normal.dot(axis)) > .9:
                continue
            sides.append(f)
        for f in sides:
            us = []
            for l in f.loops:
                d = l.vert.co - base
                ang = math.atan2(d.dot(ref2), d.dot(ref)) / (2 * math.pi) % 1.0
                us.append(ang)
            if max(us) - min(us) > .5:
                us = [u + 1 if u < .5 else u for u in us]
            for l, u in zip(f.loops, us):
                v = (l.vert.co - base).dot(axis) / height
                l[uvl].uv = (ox + .006 + (u * .5 % 1.0) * .488, oy + .006 + min(max(v, 0), 1) * .488)
        s.block_uv(bm, [f for f in faces if f not in sides])

    # ---------- руины: срез по высоте
    def clipped(s, mat, center, size_z):
        """Возвращает (рисовать?, материал). В руинах всё выше линии среза пропадает, у края — обнажённый кирпич."""
        if s.clip is None or mat not in STONES and mat not in ("Plaster", "Gold"):
            return True, mat
        fr, cut, rng = s.clip
        lx = (center - fr.o).dot(fr.x)
        c = cut(lx)
        top = center.z + size_z * .5
        if center.z > c:
            return False, mat
        if top > c - .1 and mat in STONES and rng.random() < .45:
            return True, "Brick"
        return True, mat

    # ---------- примитивы
    def box(s, mat, c, ax, ay, az, sx, sy, sz):
        ok, mat = s.clipped(mat, c, abs(az.z) * sz + abs(ax.z) * sx + abs(ay.z) * sy)
        if not ok:
            return
        bm = s.bm(mat)
        m = Matrix(((ax.x * sx, ay.x * sy, az.x * sz, c.x),
                    (ax.y * sx, ay.y * sy, az.y * sz, c.y),
                    (ax.z * sx, ay.z * sy, az.z * sz, c.z),
                    (0, 0, 0, 1)))
        res = bmesh.ops.create_cube(bm, size=1.0, matrix=m)
        if MATS[mat][1] == "block":
            faces = {f for v in res["verts"] for f in v.link_faces}
            s.block_uv(bm, faces)

    def lbox(s, mat, f, x, y, z, sx, sy, sz, yaw=0.0, tx=0.0, ty=0.0):
        """Коробка в локальных координатах кадра (центр x, y, z), с необязательным поворотом."""
        if yaw or tx or ty:
            ax, ay, az = f.axes(yaw, tx, ty)
        else:
            ax, ay, az = f.x, f.y, f.z
        s.box(mat, f.p(x, y, z), ax, ay, az, sx, sy, sz)

    def cyl(s, mat, base, axis, r1, height, seg=12, r2=None):
        r2 = r1 if r2 is None else r2
        axis = Vector(axis).normalized()
        if s.clip is not None:
            ok, mat = s.clipped(mat, base + axis * height * .5, height * abs(axis.z))
            if not ok:
                return
        bm = s.bm(mat)
        rot = axis.to_track_quat('Z', 'Y').to_matrix().to_4x4()
        m = Matrix.Translation(base + axis * height * .5) @ rot
        res = bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r1, radius2=r2, depth=height, matrix=m)
        if MATS[mat][1] == "block":
            faces = {f for v in res["verts"] for f in v.link_faces}
            s.cyl_uv(bm, faces, base, axis, height)

    def prism(s, mat, outline, f, y0, depth):
        """Плоский контур [(x, z)] в плоскости кадра, выдавленный вглубь (вдоль y) от y0 на depth."""
        if s.clip is not None and mat in ("Plaster", "Brick") + tuple(STONES):
            fr, cut, _ = s.clip
            zmax = max(p[1] for p in outline)
            xs = [p[0] for p in outline]
            lo = min(cut((f.p(x, y0, 0) - fr.o).dot(fr.x)) for x in (min(xs), (min(xs) + max(xs)) / 2, max(xs)))
            if zmax > lo + .3:
                return
        bm = s.bm(mat)
        front = [bm.verts.new(f.p(u, y0, v)) for u, v in outline]
        back = [bm.verts.new(f.p(u, y0 + depth, v)) for u, v in outline]
        n = len(outline)
        faces = [bm.faces.new(front), bm.faces.new(list(reversed(back)))]
        for i in range(n):
            j = (i + 1) % n
            faces.append(bm.faces.new((front[i], back[i], back[j], front[j])))
        if MATS[mat][1] == "banner":
            uvl = bm.loops.layers.uv.active
            us = [p[0] for p in outline]
            vs = [p[1] for p in outline]
            u0, u1, v0, v1 = min(us), max(us), min(vs), max(vs)
            for face in faces[:2]:
                for l in face.loops:
                    d = l.vert.co - f.o
                    l[uvl].uv = ((d.dot(f.x) - u0) / (u1 - u0), (d.dot(f.z) - v0) / (v1 - v0))
        elif MATS[mat][1] == "block":
            s.block_uv(bm, faces)
        res = bmesh.ops.triangulate(bm, faces=faces[:2], quad_method='BEAUTY', ngon_method='EAR_CLIP')

    def blob(s, mat, c, rx, ry, rz, subdiv=1, jitter=.18, ax=None):
        bm = s.bm(mat)
        res = bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=1.0, matrix=Matrix.Identity(4))
        ax = ax or (Vector((1, 0, 0)), Vector((0, 1, 0)), UP)
        for v in res["verts"]:
            k = 1 + s.rnd.uniform(-jitter, jitter)
            p = v.co * k
            v.co = c + ax[0] * p.x * rx + ax[1] * p.y * ry + ax[2] * p.z * rz


# ================================================================ общие детали

def arch_outline(hw, spring, top, seg=12):
    """Контур «стена с полукруглым проёмом»: от пяты арки вверх до top (для пазух арок)."""
    pts = [(hw, spring), (hw, top), (-hw, top), (-hw, spring)]
    for i in range(1, seg):
        t = math.pi * (1 - i / seg)
        pts.append((hw * math.cos(t), spring + hw * math.sin(t)))
    return pts


def voussoirs(b, mat, f, cx, y0, hw, spring, thick, depth, n=9, key_mat=None, key_scale=1.25):
    """Клинчатые камни арки в кадре f (центр арки по x = cx, лицо на y = y0)."""
    for i in range(n):
        t = math.pi * (i + .5) / n
        r = hw + thick * .5
        c = f.p(cx + r * math.cos(t), y0 + depth * .5, spring + r * math.sin(t))
        bx = f.x * math.sin(t) - f.z * math.cos(t)
        bz = f.x * math.cos(t) + f.z * math.sin(t)
        length = math.pi * r / n * .94
        key = i == n // 2
        b.box(key_mat if (key and key_mat) else mat, c - (f.y * .06 if key else Vector()), bx, f.y, bz,
              length * (key_scale if key else 1.0), depth + (.14 if key else 0), thick * (1.2 if key else 1.0))


def course(b, mat, f, x0, x1, y0, y1, z0, z1, block=1.4, jitter=0.0):
    """Ряд блоков вдоль x от x0 до x1 (лицо y0, глубина до y1)."""
    length = x1 - x0
    n = max(1, int(round(length / block)))
    w = length / n
    for i in range(n):
        h = z1 - z0
        dz = b.rnd.uniform(-jitter, jitter) if jitter else 0
        dy = b.rnd.uniform(-jitter, jitter) * .5 if jitter else 0
        b.lbox(mat, f, x0 + w * (i + .5), (y0 + y1) * .5 + dy, (z0 + z1) * .5 + dz * .3, w - .04, y1 - y0, h + dz)


def arc_course(b, mat, r_in, r_out, z0, z1, deg0, deg1, block=1.8, jitter=0.0, keep=None, o=None):
    """Ряд блоков по кольцу между углами deg0 и deg1 (по возрастанию угла); o — центр кольца (по умолчанию 0)."""
    span = (deg1 - deg0) % 360 or 360
    rm = (r_in + r_out) * .5
    n = max(1, int(round(math.radians(span) * rm / block)))
    step = span / n
    chord = 2 * rm * math.sin(math.radians(step) / 2)
    for i in range(n):
        deg = deg0 + step * (i + .5)
        if keep and not keep(deg, i):
            continue
        f = RF(rm, deg)
        if o is not None:
            f.o = f.o + o
        dz = b.rnd.uniform(-jitter, jitter) if jitter else 0
        b.lbox(mat, f, 0, 0, (z0 + z1) * .5 + dz * .5, chord - .05, r_out - r_in, z1 - z0 + dz)


def laurel(b, f, cx, y, cz, radius, mat="Gold"):
    """Золотой лавровый венок из листиков (на плоскости кадра, лицо −y)."""
    for side in (-1, 1):
        for i in range(8):
            t = math.radians(-70 + i * 20)
            p = f.p(cx + side * radius * math.cos(t), y, cz + radius * math.sin(t))
            tang = (f.x * (-side * math.sin(t)) + f.z * math.cos(t)).normalized()
            bz = (tang * math.cos(.6) + f.x * side * math.sin(.6)).normalized()
            bx = f.y.cross(bz).normalized()
            b.box(mat, p, bx, f.y, bz, radius * .2, .05, radius * .45)
    b.lbox(mat, f, cx, y, cz - radius, radius * .5, .06, radius * .16)


def swag(b, f, x0, x1, z, y, sag=.55, thick=.18):
    """Провисающая драпировка (красная ткань) между двумя точками + золотые шишечки и хвосты."""
    n = 8
    pts = []
    for i in range(n + 1):
        t = i / n
        pts.append((x0 + (x1 - x0) * t, z - sag * math.sin(math.pi * t)))
    for (ax, az), (bx, bz) in zip(pts, pts[1:]):
        d = Vector(((bx - ax), 0, (bz - az)))
        ln = d.length
        ang = math.atan2(d.z, d.x)
        bxv = (f.x * math.cos(ang) + f.z * math.sin(ang)).normalized()
        bzv = f.y.cross(bxv).normalized() * -1
        c = f.p((ax + bx) * .5, y, (az + bz) * .5)
        b.box("Cloth", c, bxv, f.y, bzv, ln + .08, .12, thick + .1 * math.sin(math.pi * ((ax + bx) * .5 - x0) / (x1 - x0)))
    for x in (x0, x1):
        b.blob("Gold", f.p(x, y - .06, z + .02), .11, .11, .11, subdiv=1, jitter=0)
        b.lbox("Cloth", f, x, y + .02, z - .45, .16, .08, .8)


def moss_tuft(b, c, size=.3):
    b.blob("Moss", c, size * b.rnd.uniform(.8, 1.4), size * b.rnd.uniform(.8, 1.4), size * .35, subdiv=1, jitter=.25)


def ivy(b, f, x, y, z_top, length):
    """Плющ: свисающие гроздья листьев вдоль стены."""
    n = max(3, int(length / .35))
    for i in range(n):
        t = i / n
        c = f.p(x + math.sin(i * 1.7) * .25, y - .08, z_top - t * length)
        r = .26 * (1 - t * .5)
        b.blob("Leaf" if i % 3 else "LeafLight", c, r, r * .5, r, subdiv=1, jitter=.3)


# ================================================================ пропсы (по листам референсов)

def column(b, f, h=5.0, r=.42, broken=None, mat="Stone"):
    """Лист 1: колонна на двухступенчатой базе, ствол с трещинами, золотая капитель, абака. broken — высота слома."""
    s = r / .42
    b.lbox(mat, f, 0, 0, .22 * s, 1.25 * s, 1.25 * s, .44 * s)
    b.lbox(mat, f, 0, 0, .6 * s, 1.1 * s, 1.1 * s, .32 * s)
    b.cyl(mat, f.p(0, 0, .76 * s), f.z, r * 1.22, .14 * s, seg=16)
    b.cyl(mat, f.p(0, 0, .9 * s), f.z, r * 1.12, .12 * s, seg=16)
    z0 = 1.02 * s
    top = h - .95 * s
    if broken is not None:
        zb = min(broken, top)
        b.cyl(mat, f.p(0, 0, z0), f.z, r, zb - z0, seg=14)
        for k in range(3):
            ang = k * 2.1 + b.rnd.random()
            b.lbox(mat, f, math.cos(ang) * r * .45, math.sin(ang) * r * .45, zb + .1, r * .7, r * .6, .32,
                   yaw=ang, tx=b.rnd.uniform(-.4, .4))
        return
    b.cyl(mat, f.p(0, 0, z0), f.z, r, top - z0, seg=14)
    b.cyl(mat, f.p(0, 0, top), f.z, r * 1.08, .1 * s, seg=14)
    b.cyl("Gold", f.p(0, 0, top + .1 * s), f.z, r * 1.02, .45 * s, seg=14, r2=r * 1.45)
    b.cyl("Gold", f.p(0, 0, top + .55 * s), f.z, r * 1.5, .08 * s, seg=14)
    for k in range(8):
        a = k * math.pi / 4
        ax, ay, az = f.axes(a, .0, .0)
        c = f.p(math.cos(a) * r * 1.2, math.sin(a) * r * 1.2, top + .32 * s)
        b.box("Gold", c, ax, ay, (az - ax * .25).normalized(), .12 * s, .08 * s, .42 * s)
    b.lbox(mat, f, 0, 0, top + .74 * s, 1.15 * s, 1.15 * s, .22 * s)
    b.lbox(mat, f, 0, 0, top + .92 * s, 1.28 * s, 1.28 * s, .2 * s)


def banner(b, f, w=1.3, h=3.2):
    """Лист 8: знамя с лавровым венком на золотой перекладине. f — центр перекладины, лицо −y."""
    rod = w + .45
    b.cyl("Gold", f.p(-rod / 2, 0, 0), f.x, .055, rod, seg=10)
    for sx in (-1, 1):
        b.cyl("Gold", f.p(sx * rod / 2, 0, 0), f.x * sx, .1, .16, seg=10)
        b.lbox("Cloth", f, sx * w * .33, .0, -.04, .24, .14, .22)
    p = BANNER_POINT
    outline = [(-w / 2, -.08), (w / 2, -.08), (w / 2, -h * (1 - p)), (0, -h), (-w / 2, -h * (1 - p))]
    b.prism("Banner", outline, f, -.02, .04)


def brazier(b, f, s=1.0):
    """Лист 5: железная жаровня-корзина на четырёх ногах, угли и огонь."""
    for k in range(4):
        a = math.pi / 4 + k * math.pi / 2
        ax, ay, az = f.axes(a, 0, 0)
        leg_dir = (az - ax * .0).normalized()
        foot = f.p(math.cos(a) * .42 * s, math.sin(a) * .42 * s, 0)
        top = f.p(math.cos(a) * .3 * s, math.sin(a) * .3 * s, .82 * s)
        d = top - foot
        b.box("Iron", (foot + top) * .5, ax, ay, d.normalized(), .13 * s, .13 * s, d.length)
        b.box("Iron", foot + f.z * .05 * s, ax, ay, az, .22 * s, .22 * s, .1 * s)
    for k in range(4):           # распорки
        a = k * math.pi / 2
        b.lbox("Iron", f, math.cos(a) * .36 * s, math.sin(a) * .36 * s, .32 * s, .08 * s, .62 * s, .07 * s, yaw=a)
    ring = 8
    for z, rad in ((.86, .36), (1.08, .47), (1.3, .56)):
        for k in range(ring):
            a = k * 2 * math.pi / ring
            chord = 2 * rad * math.sin(math.pi / ring) * s
            b.lbox("Iron", f, math.cos(a) * rad * s, math.sin(a) * rad * s, z * s, .09 * s, chord + .02, .1 * s,
                   yaw=a)
    for k in range(ring):        # прутья корзины
        a = (k + .5) * 2 * math.pi / ring
        p0 = f.p(math.cos(a) * .36 * s, math.sin(a) * .36 * s, .86 * s)
        p1 = f.p(math.cos(a) * .56 * s, math.sin(a) * .56 * s, 1.3 * s)
        ax, ay, az = f.axes(a, 0, 0)
        b.box("Iron", (p0 + p1) * .5, ax, ay, (p1 - p0).normalized(), .08 * s, .1 * s, (p1 - p0).length)
        b.blob("Gold", f.p(math.cos(a) * .58 * s, math.sin(a) * .58 * s, 1.3 * s), .045 * s, .045 * s, .045 * s,
               jitter=0)
    for k in range(7):
        a = b.rnd.uniform(0, 6.3)
        rr = b.rnd.uniform(0, .3) * s
        b.blob("Ember", f.p(math.cos(a) * rr, math.sin(a) * rr, 1.2 * s), .13 * s, .13 * s, .09 * s)
    for k, (dx, dy, rr, hh) in enumerate(((0, 0, .34, 1.0), (.12, .08, .22, .8), (-.14, -.05, .2, .7))):
        b.cyl("Fire", f.p(dx * s, dy * s, 1.2 * s), (f.z + f.x * dx * .5).normalized(), rr * s, hh * s, seg=7, r2=0)


def rubble(b, f, size=1.0):
    """Лист 6: груда — крупные кремовые глыбы, комья кирпичной кладки, мелкие камни и мох."""
    rnd = b.rnd
    for k in range(rnd.randint(4, 6)):
        a = rnd.uniform(0, 6.3)
        rr = rnd.uniform(0, .9) * size
        sx, sy, sz = rnd.uniform(.6, 1.3) * size, rnd.uniform(.45, .8) * size, rnd.uniform(.35, .6) * size
        z = sz * .45 + (.35 * size if rr < .45 * size and k > 1 else 0)
        b.lbox("Stone", f, math.cos(a) * rr, math.sin(a) * rr, z, sx, sy, sz, yaw=rnd.uniform(0, 3.1),
               tx=rnd.uniform(-.25, .25), ty=rnd.uniform(-.3, .3))
    for k in range(rnd.randint(1, 3)):
        a = rnd.uniform(0, 6.3)
        rr = rnd.uniform(.2, .8) * size
        g = f.at(math.cos(a) * rr, math.sin(a) * rr, 0, yaw=rnd.uniform(0, 3.1))
        tilt = rnd.uniform(-.35, .35)
        for i in range(3):
            for j in range(3 - i):
                b.lbox("Brick", g, (j - (2 - i) * .5) * .5 * size, 0, (.12 + i * .23) * size, .48 * size, .3 * size,
                       .21 * size, tx=tilt)
    for k in range(rnd.randint(6, 10)):
        a = rnd.uniform(0, 6.3)
        rr = rnd.uniform(.5, 1.5) * size
        sz = rnd.uniform(.15, .3) * size
        b.lbox("Stone", f, math.cos(a) * rr, math.sin(a) * rr, sz * .4, sz * 1.3, sz, sz, yaw=rnd.uniform(0, 3.1),
               tx=rnd.uniform(-.3, .3))
    for k in range(rnd.randint(2, 4)):
        a = rnd.uniform(0, 6.3)
        rr = rnd.uniform(0, 1.0) * size
        moss_tuft(b, f.p(math.cos(a) * rr, math.sin(a) * rr, rnd.uniform(.2, .7) * size), .3 * size)


def cypress(b, f, h=9.0):
    """Лист 4: кипарис — ствол и веретено из листвы двух тонов."""
    rnd = b.rnd
    b.cyl("Bark", f.p(0, 0, -.3), f.z, .26 * h / 9, 1.4 * h / 9, seg=8, r2=.18 * h / 9)
    maxr = h * .14
    levels = 9
    for i in range(levels):
        t = (i + .5) / levels
        z = 1.0 * h / 9 + t * (h - 1.0 * h / 9)
        prof = math.sin(math.pi * min(1.0, t * 1.15 + .05)) ** .7 * (1 - .35 * t)
        rr = max(.25, maxr * prof)
        n = 3 if rr > .6 else 2
        for k in range(n):
            a = k * 2 * math.pi / n + i * .9
            off = rr * .38
            c = f.p(math.cos(a) * off, math.sin(a) * off, z)
            mat = "LeafLight" if (math.sin(a) < -.2 and rnd.random() < .7) or t > .85 else "Leaf"
            b.blob(mat, c, rr * .75, rr * .75, h / levels * .95, subdiv=2, jitter=.12)
    b.blob("Leaf", f.p(0, 0, h - .2), maxr * .25, maxr * .25, .6, subdiv=1)


def bush(b, f, s=1.0):
    """Лист 7: круглый куст из гроздей листвы двух тонов."""
    rnd = b.rnd
    for k in range(4):
        a = k * 1.6
        b.cyl("Bark", f.p(0, 0, 0), (f.z + f.x * math.cos(a) * .6 + f.y * math.sin(a) * .6).normalized(), .06 * s, .7 * s,
              seg=6)
    n = rnd.randint(9, 13)
    for k in range(n):
        a = rnd.uniform(0, 6.3)
        rr = rnd.uniform(0, .75) * s
        z = (.55 + rnd.uniform(0, .6) - rr * .4) * s
        r = rnd.uniform(.38, .55) * s
        mat = "LeafLight" if z > .9 * s or rnd.random() < .3 else "Leaf"
        b.blob(mat, f.p(math.cos(a) * rr, math.sin(a) * rr, z), r, r, r * .85, subdiv=2, jitter=.14)


def portal_pedestal(b, f):
    """Лист 12: ступенчатый круглый постамент портала с золотыми накладками и лавровыми табличками."""
    steps = ((3.3, 0.0, .32), (2.85, .32, .64), (2.4, .64, .96))
    for k, (r, z0, z1) in enumerate(steps):
        arc_course(b, "Stone", r - .55, r, z0, z1, 0, 360, block=1.25, o=Vector((f.o.x, f.o.y, f.o.z)))
        b.cyl("Shadow", f.p(0, 0, z0), f.z, r - .5, z1 - z0 - .01, seg=32)
        for i in range(8):
            deg = i * 45 + 22.5
            fr = F(f.o + U(r - .02, deg, 0), -frame_x(deg), -radial(deg))
            b.lbox("Gold", fr, 0, -.02, (z0 + z1) * .5, .34, .1, z1 - z0 + .02)
            b.lbox("Gold", fr, 0, .1, z1 + .005, .38, .5, .04)
            b.blob("Gold", fr.p(0, -.09, (z0 + z1) * .5), .06, .04, .06, jitter=0)
    # Таблички на нижней ступени
    for i in range(4):
        deg = i * 90
        fr = F(f.o + U(3.3, deg, 0), -frame_x(deg), -radial(deg))
        b.lbox("Gold", fr, 0, -.01, .16, .95, .06, .26)
        b.lbox("RedPanel", fr, 0, -.04, .16, .82, .04, .19)
        laurel(b, fr, 0, -.08, .17, .07)
    # Сияющая чаша сверху
    b.cyl("Gold", f.p(0, 0, .95), f.z, 1.95, .1, seg=40)
    b.cyl("PortalGlow", f.p(0, 0, .97), f.z, 1.75, .12, seg=40)


# ================================================================ кольцевые части Колизея

def retaining_wall(b, r_face, y0, y1, bays, skip):
    """Лист 13: подпорная стена яруса — пилоны с золотыми капителями и табличками, кирпич/штукатурка, драпировки."""
    step = 360.0 / bays
    for i in range(bays):
        dp = i * step
        dc = dp + step / 2
        f = RF(r_face, dc)
        chord = 2 * r_face * math.sin(math.radians(step) / 2)
        L = chord / 2
        course(b, "Stone", f, -L, L, -.02, .6, y0, y0 + .45, block=1.4)
        b.lbox("Plaster", f, 0, .25, (y0 + .45 + y1 - .38) * .5, chord, .3, y1 - .38 - y0 - .45)
        course(b, "Stone", f, -L, L, -.14, .64, y1 - .38, y1, block=1.6)
        skipped = in_ranges(dc, skip)
        pil = not in_ranges(dp, skip)
        if pil:
            pf = RF(r_face, dp)
            b.lbox("Stone", pf, 0, .25, y0 + .18, 1.18, 1.08, .36)
            hb = (y1 - .3 - y0 - .36) / 2
            for k in range(2):
                b.lbox("Stone", pf, 0, .25, y0 + .36 + hb * (k + .5), 1.0, .95, hb - .02)
            b.lbox("Gold", pf, 0, .25, y1 - .22, 1.08, 1.02, .16)
            for k in (-1, 0, 1):
                b.lbox("Gold", pf, k * .28, -.26, y1 - .22, .1, .06, .1)
            b.lbox("Stone", pf, 0, .25, y1 + .02, 1.2, 1.1, .32)
            zc = (y0 + y1) * .5 - .1
            b.lbox("Gold", pf, 0, -.23, zc, .6, .05, .75)
            b.lbox("RedPanel", pf, 0, -.26, zc, .48, .03, .62)
            laurel(b, pf, 0, -.29, zc + .02, .17)
        if not skipped and not in_ranges(dp + step, skip) and pil:
            swag(b, f, -L + .55, L - .55, y1 - .18, -.3)
        if b.rnd.random() < .3:
            moss_tuft(b, f.p(b.rnd.uniform(-L, L), -.05, y0 + .05), .25)


def paving_ring(b, r0, r1, y_top, rows, tile, skip=None):
    w = (r1 - r0) / rows
    for k in range(rows):
        ri = r0 + k * w
        off = (k % 2) * .5
        n = max(8, int(round(2 * math.pi * (ri + w / 2) / tile)))
        step = 360.0 / n
        arc_course(b, "Paving", ri + .03, ri + w - .03, y_top - .18, y_top + .02, off * step, off * step + 360,
                   block=tile, jitter=.015)


def podium_arcade(b):
    """Лист 2: аркада подиума (r=45, высота 6..11) — опоры с золотыми поясками, арки-ниши, красные пазухи, карниз."""
    bays = 64
    step = 360.0 / bays
    rf = 44.95
    chord = 2 * rf * math.sin(math.radians(step) / 2)
    pw = 1.1
    hw = (chord - pw) / 2
    zs = T2_Y + 2.45
    for i in range(bays):
        dp = i * step
        dc = dp + step / 2
        if not near_any(dp, ARCHES, 6.2):
            pf = RF(rf, dp)
            b.lbox("Stone", pf, 0, .75, T2_Y + .22, pw + .25, 1.95, .44)
            hh = (zs - .2 - T2_Y - .44) / 2
            for k in range(2):
                b.lbox("Stone", pf, 0, .75, T2_Y + .44 + hh * (k + .5), pw, 1.75, hh - .02)
            b.lbox("Gold", pf, 0, .75, zs - .14, pw + .1, 1.85, .12)
            b.lbox("Stone", pf, 0, .75, zs - .02, pw + .18, 1.9, .2)
            if i % 4 == 0:
                banner(b, pf.at(0, -.2, WALL_Y - .75), .9, 2.5)
        if near_any(dc, ARCHES, 6.5):
            continue
        f = RF(rf, dc)
        voussoirs(b, "Stone", f, 0, -.04, hw, zs, .42, 1.0, n=9, key_mat="Gold", key_scale=1.0)
        b.prism("Plaster", arch_outline(hw + .2, zs, WALL_Y - .55), f, .02, .9)
        b.lbox("Shadow", f, 0, 1.45, (T2_Y + WALL_Y) * .5, chord, .3, WALL_Y - T2_Y)
        b.lbox("Void", f, 0, 1.25, T2_Y + 1.6, hw * 1.5, .1, 2.6)
        b.lbox("Stone", f, 0, .4, T2_Y + .12, 2 * hw, 1.0, .24)
    # Антаблемент и карниз
    keep = lambda deg, i: not near_any(deg, ARCHES, 6.2)
    arc_course(b, "Stone", rf - .12, WALL_R + .1, WALL_Y - .55, WALL_Y - .05, 0, 360, block=1.6, keep=keep)
    arc_course(b, "Gold", rf - .16, rf - .1, WALL_Y - .58, WALL_Y - .5, 0, 360, block=3.0, keep=keep)
    arc_course(b, "Stone", rf - .38, WALL_R + .15, WALL_Y - .05, WALL_Y + .32, 0, 360, block=1.9, keep=keep)


def gate(b, deg):
    """Лист 11: парадные ворота-спавн — пилоны со знамёнами, арка с золотым замковым камнем,
    пояс с золотыми квадратами, люнет с лавровым медальоном, решётка и тёмный тоннель."""
    f = RF(44.95, deg)
    hw, zs = 2.75, T2_Y + 6.7
    for s in (-1, 1):
        x = s * (hw + .9)
        b.lbox("Stone", f, x, 1.0, T2_Y + .3, 2.15, 2.95, .6)
        bh = (zs - T2_Y - .6 - .35) / 4
        for k in range(4):
            b.lbox("Stone", f, x, 1.0, T2_Y + .6 + bh * (k + .5), 1.8, 2.6, bh - .03)
        b.lbox("Gold", f, x, 1.0, zs - .27, 1.95, 2.72, .16)
        b.lbox("Stone", f, x, 1.0, zs - .02, 2.0, 2.8, .35)
        top = T2_Y + 10.3
        hb = (top - zs - .16) / 3
        for k in range(3):
            b.lbox("Stone", f, x, 1.0, zs + .16 + hb * (k + .5), 1.8, 2.6, hb - .03)
        banner(b, f.at(x, -.35, zs - .55), 1.2, 3.6)
        for k in (-1, 1):
            b.lbox("Gold", f, x + k * .45, -.33, top - .35, .22, .08, .22)
    voussoirs(b, "Stone", f, 0, -.1, hw, zs, .72, 2.5, n=11, key_mat="Gold", key_scale=1.15)
    b.prism("Plaster", arch_outline(hw + .4, zs, T2_Y + 10.3), f, .05, 2.3)
    # Пояс
    zb = T2_Y + 10.3
    course(b, "Stone", f, -4.75, 4.75, -.2, 2.7, zb, zb + .75, block=1.9)
    b.lbox("Gold", f, 0, -.22, zb + .05, 9.4, .05, .08)
    b.lbox("Gold", f, 0, -.22, zb + .7, 9.4, .05, .08)
    for k in range(9):
        b.lbox("Gold", f, -4.0 + k * 1.0, -.25, zb + .38, .24, .08, .24)
    # Люнет с медальоном
    zl = zb + .75
    b.prism("Plaster", [(math.cos(math.pi * i / 16) * 3.7, zl + math.sin(math.pi * i / 16) * 3.7) for i in range(17)],
            f, .15, 2.2)
    voussoirs(b, "Stone", f, 0, -.05, 3.7, zl, .55, 2.55, n=13, key_mat="Gold", key_scale=1.0)
    mc = f.at(0, -.1, zl + 1.55)
    b.cyl("Gold", mc.p(0, 0, 0), -f.y, 1.0, .14, seg=24)
    b.cyl("RedPanel", mc.p(0, -.14, 0), -f.y, .86, .04, seg=24)
    laurel(b, mc, 0, -.2, .02, .58)
    b.lbox("Gold", f, -4.0, -.12, zl + .2, .5, .1, .3)
    b.lbox("Gold", f, 4.0, -.12, zl + .2, .5, .1, .3)
    # Решётка, тоннель
    for k in range(8):
        b.lbox("Iron", f, -2.45 + k * .7, .55, zs + 1.2, .12, .12, 2.3)
    b.lbox("Iron", f, 0, .55, zs + .2, 5.5, .14, .14)
    b.lbox("Iron", f, 0, .55, zs + 1.2, 5.5, .14, .14)
    b.lbox("Void", f, 0, 4.2, T2_Y + 4.5, 5.6, .3, 9.0)
    for s in (-1, 1):
        b.lbox("Shadow", f, s * 2.85, 2.6, T2_Y + 4.5, .3, 3.4, 9.0)
    b.lbox("Shadow", f, 0, 2.6, zs + 2.9, 5.9, 3.4, .3)
    b.lbox("Paving", f, 0, 1.9, T2_Y - .08, 5.5, 4.4, .2)
    for s in (-1, 1):
        brazier(b, f.at(s * 6.0, -1.3, T2_Y), 1.0)
        moss_tuft(b, f.p(s * (hw + 1.8), -.1, T2_Y + .05), .35)


# Лестницы между уровнями: прямые, перпендикулярно стене (как на референсе), выходят из верхнего уровня вниз.
# Стоят вразбежку: со 2-го яруса — через 89° от ворот, с 1-го яруса на арену — ещё через 120°,
# поэтому врагам приходится пройти часть каждого уровня. Те же числа — в PrototypeBuilder.Level.cs.
STAIR_W = 3.6
STAIR_T2 = (-89.0, T1_R, T2_Y, T1_R - 4.5, T1_Y)       # (смещение угла от ворот, r верх, y верх, r низ, y низ)
STAIR_T1 = (-209.0, ARENA_R, T1_Y, ARENA_R - 4.5, 0.0)


def stairs(b, deg, r_top, y_top, r_bot, y_bot, floor, width=STAIR_W):
    """Лист 14: широкая каменная лестница — сплошные ступени, боковые стенки с кирпичной вставкой и наклонным
    парапетом, квадратные столбы с шапками внизу и наверху."""
    top, bot = U(r_top, deg, y_top), U(r_bot, deg, y_bot)
    flat = Vector((top.x - bot.x, top.y - bot.y, 0))
    L = flat.length
    d = flat.normalized()
    side = UP.cross(d).normalized()
    f = F(Vector((bot.x, bot.y, 0)), side, d)          # x — поперёк, y — вдоль подъёма
    n = max(4, int(round((y_top - y_bot) / .3)))
    run = L / n
    rise = (y_top - y_bot) / n
    for i in range(n):
        zt = y_bot + rise * (i + 1)
        parts = 3
        for k in range(parts):
            w = width / parts
            b.lbox("Stone", f, -width / 2 + w * (k + .5), run * (i + .5), (floor + zt) * .5, w - .03, run + .02, zt - floor)
    for k in (-1, 1):
        x = k * (width / 2 + .3)
        wf = F(f.o + f.x * x, f.y, f.x, UP)        # плоскость стенки: «x» кадра — вдоль подъёма
        b.prism("Brick", [(.5, floor), (.5, y_bot + .6), (L - .5, y_top + .45), (L - .5, floor)], wf, -.22, .44)
        # каменная рамка вокруг кирпича
        b.lbox("Stone", f, x, L * .5, floor + .2, .62, L, .4)
        segs = max(2, int(L / 1.3))
        for i in range(segs):
            t0, t1 = i / segs, (i + 1) / segs
            p0 = f.p(x, L * t0, y_bot + (y_top - y_bot) * t0 + .72)
            p1 = f.p(x, L * t1, y_bot + (y_top - y_bot) * t1 + .72)
            ax = (p1 - p0).normalized()
            az = f.x.cross(ax).normalized()
            if az.z < 0:
                az = -az
            b.box("Stone", (p0 + p1) * .5, ax, f.x, az, (p1 - p0).length - .03, .68, .32)
        for t, zt in ((0.0, y_bot), (1.0, y_top)):
            yy = L * t + (.3 if t == 0 else -.3)
            h = zt + 1.05 - floor
            b.lbox("Stone", f, x, yy, floor + h / 2, .72, .72, h)
            b.lbox("Gold", f, x, yy, floor + h - .12, .76, .76, .12)
            b.lbox("Stone", f, x, yy, floor + h + .14, .86, .86, .28)
    moss_tuft(b, f.p(-width / 2 + .3, .4, y_bot + .3), .25)


def ramps(b):
    for a in ARCHES:
        o, rt, yt, rb, yb = STAIR_T2
        stairs(b, a + o, rt, yt, rb, yb, T1_Y - .2)
        o, rt, yt, rb, yb = STAIR_T1
        stairs(b, a + o, rt, yt, rb, yb, -.1)

def ruined(deg):
    return in_ranges(deg, [RUIN])


def ruin_depth(deg):
    """0 — целое, 1 — центр обвала."""
    lo, hi = RUIN
    if not ruined(deg):
        return 0.0
    t = (dangle(deg, lo)) / (hi - lo)
    return math.sin(math.pi * min(max(t, 0), 1)) ** .7


def cavea(b):
    """Листы 15/16: трибуны за стеной подиума (от r 47 и y 11.4 вверх до r 60 и y 20.6), проходы-лестницы,
    в секторе руин — выпавшие ряды, обнажённый кирпич, мох и кусты."""
    rows, depth, rise = 14, .95, .66
    aisles = [i * 15.0 + 7.5 for i in range(24)]
    rnd = b.rnd
    for k in range(rows):
        r_in = 47.0 + k * depth
        r_out = r_in + depth + .02
        top = 11.4 + (k + 1) * rise
        aw = 1.4 / r_in * 57.3 / 2           # полуширина прохода в градусах

        def keep(deg, i, k=k, r_in=r_in):
            if near_any(deg, aisles, aw):
                return False
            if k < 3 and near_any(deg, ARCHES, 7.5):
                return False
            rd = ruin_depth(deg)
            if rd > 0 and rnd.random() < rd * (.25 + .6 * k / rows):
                if rnd.random() < .35:
                    b.lbox("Brick", RF((r_in + r_in + depth) / 2, deg), 0, 0, top - rise - .2, 1.6, depth * .9, .6)
                return False
            return True
        arc_course(b, "Seat", r_in, r_out, top - rise - .32, top, k * 1.3, k * 1.3 + 360, block=1.9,
                   jitter=.02, keep=keep)
        for deg in aisles:                     # ступени прохода (две на ряд)
            if k < 3 and near_any(deg, ARCHES, 7.5):
                continue
            if ruin_depth(deg) > .3 and rnd.random() < .6:
                continue
            for h in range(2):
                f = RF(r_in + depth * (h + .5) / 2, deg)
                b.lbox("Stone" if k < 4 else "StoneFar", f, 0, 0, top - rise + rise * (h + 1) / 2 - .2,
                       1.35, depth / 2, .4)
    # Задний бортик трибун
    arc_course(b, "StoneFar", 60.2, 60.6, 20.4, 21.1, 0, 360, block=2.2)
    # Руины: груды, кусты, сдвинутые камни
    for i in range(26):
        deg = rnd.uniform(RUIN[0] + 3, RUIN[1] - 3)
        k = rnd.randint(2, rows - 1)
        r = 47.0 + k * depth
        z = 11.4 + k * rise
        g = RF(r, deg, z)
        if rnd.random() < .55:
            rubble(b, g, rnd.uniform(.7, 1.3))
        else:
            bush(b, g, rnd.uniform(.6, 1.0))
    for i in range(40):
        deg = rnd.uniform(0, 360)
        k = rnd.randint(0, rows - 1)
        moss_tuft(b, U(47.0 + k * depth + .4, deg, 11.4 + (k + 1) * rise), .3)


def engaged_column(b, f, x, z0, z1, mat="StoneFar", top=None):
    """Полуколонна на лице опоры внешней аркады (лист 9): база, ствол, золотая капитель.
    top — высота слома (руины): ниже z1 колонна обрывается без капители."""
    r = .34
    c = f.at(x, -.28, 0)
    if top is not None and top < z0 + .5:
        return
    b.lbox(mat, c, 0, .1, z0 + .2, .9, .7, .4)
    if top is not None and top < z1:
        if top > z0 + 1.0:
            b.cyl(mat, c.p(0, 0, z0 + .4), f.z, r, top - z0 - .6, seg=12)
        return
    b.cyl(mat, c.p(0, 0, z0 + .4), f.z, r, z1 - z0 - 1.0, seg=12)
    b.cyl("Gold", c.p(0, 0, z1 - .6), f.z, r * 1.02, .38, seg=12, r2=r * 1.5)
    b.lbox(mat, c, 0, .05, z1 - .11, 1.0, .8, .22)


CROWN_LEVELS = ((20.6, 25.2), (25.2, 29.4), (29.4, 33.0))
CROWN_TOP = 33.0


def crown_cut_heights(bays):
    """Высота сохранившейся опоры (по краям руин — целиком, в центре — около 1-го яруса)."""
    rnd = random.Random(31)
    hs = []
    step = 360.0 / bays
    for i in range(bays + 1):
        rd = ruin_depth(i * step)
        if rd <= 0:
            hs.append(99.0)
        else:
            full = CROWN_TOP + 1.4
            low = 21.0 + rnd.uniform(0, 3.0)
            k = rnd.choice((.45, .7, .85, 1.0, 1.0))      # некоторые опоры стоят выше соседей
            hs.append(full - (full - low) * rd * k + rnd.uniform(-1.5, 1.5) * rd)
    return hs


def crown(b):
    """Лист 9 / 10: внешняя трёхъярусная аркада поверх трибун (r 60.6, y 20.6..34). В секторе руин опоры
    обрываются на своей высоте (верхний камень — обнажённый кирпич), арки держатся только там, где обе
    опоры целы (иначе остаются лишь «пятки» арок), плющ свисает с обломанных опор, внизу груды."""
    bays = 72
    step = 360.0 / bays
    rf = 60.6
    depth = 1.6
    chord = 2 * rf * math.sin(math.radians(step) / 2)
    pw = 1.25
    hw = (chord - pw) / 2
    hs = crown_cut_heights(bays)
    rnd = random.Random(5)
    for i in range(bays):
        dp = i * step
        dc = dp + step / 2
        f = RF(rf, dc)
        pf = RF(rf, dp)
        hp = hs[i]                         # высота этой (левой) опоры
        hmin = min(hs[i], hs[i + 1])       # до этой высоты целы обе опоры пролёта
        intact = hmin >= 90
        for li, (z0, z1) in enumerate(CROWN_LEVELS):
            zs = z1 - .5 - hw
            # Опора: три блока, в руинах — только те, что ниже слома; верхний уцелевший — кирпичное ядро
            hb = (z1 - .5 - z0) / 3
            for k in range(3):
                bz0 = z0 + hb * k
                if bz0 + hb * .5 > hp:
                    break
                last = bz0 + hb * 1.5 > hp
                b.lbox("Brick" if (last and not intact) else "StoneFar", pf, 0, depth / 2, bz0 + hb / 2,
                       pw * (.85 if last and not intact else 1), depth * (.8 if last and not intact else 1), hb - .02)
            engaged_column(b, pf, 0, z0, z1 - .5, top=None if hp >= z1 else hp)
            if z1 <= hmin:
                voussoirs(b, "StoneFar", f, 0, 0, hw, zs, .38, depth, n=9)
                b.prism("Plaster", arch_outline(hw + .15, zs, z1 - .5), f, .04, depth - .08)
                course(b, "StoneFar", f, -chord / 2, chord / 2, -.2, depth + .1, z1 - .5, z1, block=2.0)
                b.lbox("Gold", f, 0, -.22, z1 - .48, chord, .04, .06)
            elif zs < hmin:
                # Арка обвалилась: остаются «пятки» — нижние клинчатые камни с обеих сторон
                n = 9
                for j in range(n):
                    t = math.pi * (j + .5) / n
                    if zs + (hw + .19) * math.sin(t) > hmin - .3 or 2 < j < n - 3:
                        continue
                    rr = hw + .19
                    c = f.p(rr * math.cos(t), depth * .5, zs + rr * math.sin(t))
                    bx = f.x * math.sin(t) - f.z * math.cos(t)
                    bz = f.x * math.cos(t) + f.z * math.sin(t)
                    b.box("StoneFar", c, bx, f.y, bz, math.pi * rr / n * .94, depth, .38)
            if z0 + .3 < hmin:
                b.lbox("StoneFar", f, 0, depth / 2, z0 + .15, 2 * hw, depth, .3)
            if li == 1 and i % 3 == 0 and intact:
                banner(b, pf.at(0, -.66, z1 - .9), 1.0, 2.6)
        if intact or hp >= 90:
            if intact:
                course(b, "StoneFar", f, -chord / 2, chord / 2, -.4, depth + .2, CROWN_TOP, CROWN_TOP + .4, block=2.4)
            if hp >= 90:
                b.lbox("StoneFar", pf, 0, depth / 2, CROWN_TOP + .75, 1.5, depth + .3, .7)
        if not intact:
            if hp < CROWN_TOP:
                if rnd.random() < .75:
                    ivy(b, pf, 0, -.05, hp - .2, rnd.uniform(1.2, min(4.0, hp - 21.0)))
                moss_tuft(b, pf.p(0, depth * .5, hp), .45)
            if rnd.random() < .7:
                rubble(b, RF(rf - 1.6, dc + rnd.uniform(-1.5, 1.5), 20.4), rnd.uniform(.9, 1.6))

def outer_facade(b):
    """Нижний внешний фасад (снаружи, r 62.2, y 0..20.6): опоры, глухие арки с кирпичом, карнизы. Виден с холмов."""
    bays = 72
    step = 360.0 / bays
    rf = 62.2
    chord = 2 * rf * math.sin(math.radians(step) / 2)
    pw = 1.3
    hw = (chord - pw) / 2
    for i in range(bays):
        dp = i * step
        dc = dp + step / 2
        f = OF(rf, dc)
        pf = OF(rf, dp)
        for z0, z1 in ((0.0, 10.0), (10.0, 20.6)):
            zs = z1 - .7 - hw
            b.lbox("StoneFar", pf, 0, .6, (z0 + z1 - .7) * .5, pw, 1.2, z1 - .7 - z0)
            voussoirs(b, "StoneFar", f, 0, 0, hw, zs, .4, .9, n=9)
            b.prism("Plaster", arch_outline(hw + .15, zs, z1 - .7), f, .05, .8)
            b.lbox("Brick", f, 0, .7, (z0 + zs + hw) * .5, 2 * hw, .2, zs + hw - z0)
            course(b, "StoneFar", f, -chord / 2, chord / 2, -.2, 1.0, z1 - .7, z1, block=2.2)
        b.lbox("StoneFar", f, 0, .5, .3, chord, 1.3, .6)


# ================================================================ окрестности

def hill_h(r, deg):
    """Высота холмов вокруг Колизея (0 у стен, до ~25 м на горизонте)."""
    if r < 66:
        return .03
    t = (r - 66) / 160
    a = math.radians(deg)
    n = (math.sin(a * 3 + 1.1) * .5 + math.sin(a * 7 + .3) * .25 + math.sin(a * 13 + 2.2) * .12
         + math.sin(r * .05 + a * 2) * .3)
    base = 3 + 22 * t ** 1.4
    h = .03 + (base + n * 6 * min(1, t * 2)) * min(1, (r - 66) / 25)
    # Холм под храм
    dx = r * math.cos(a) - TEMPLE[1] * math.cos(math.radians(TEMPLE[0]))
    dy = r * math.sin(a) - TEMPLE[1] * math.sin(math.radians(TEMPLE[0]))
    dd = math.hypot(dx, dy)
    if dd < 28:
        h = max(h, TEMPLE[2] - (dd / 28) ** 2 * 6 if dd > 13 else TEMPLE[2])
    return max(.03, h)


TEMPLE = (205.0, 112.0, 9.0)     # угол, радиус, высота площадки


def terrain(b):
    rings = [62.5 + i * 3.0 for i in range(6)] + [80 + i * 10 for i in range(16)]
    seg = 144
    bm = b.bm("Grass")
    grid = []
    for r in rings:
        row = []
        for j in range(seg):
            deg = j * 360 / seg
            row.append(bm.verts.new(U(r, deg, hill_h(r, deg))))
        grid.append(row)
    for i in range(len(rings) - 1):
        for j in range(seg):
            j2 = (j + 1) % seg
            bm.faces.new((grid[i][j], grid[i][j2], grid[i + 1][j2], grid[i + 1][j]))


def temple(b):
    """Лист 3: храм на холме — подиум, лестница, колоннада с золотыми капителями, фронтон с венком, черепица."""
    deg, r, h = TEMPLE
    f = F(U(r, deg, h), -frame_x(deg) * -1, radial(deg))     # лицо (−y) смотрит на Колизей
    W, Lg = 11.0, 16.0
    # Подиум
    for k in range(3):
        course(b, "StoneFar", f, -W / 2 - .6 + k * .3, W / 2 + .6 - k * .3, -1 + k * .3, Lg + .6 - k * .3,
               -.5 + k * .55, .05 + k * .55, block=2.0)
    for i in range(6):
        b.lbox("StoneFar", f, 0, -1.4 - i * .4, -.4 + i * .28 - 1.0, 5.0, .45, .3)
    zb = 1.65
    # Колонны: 4 спереди, по 5 по бокам, 4 сзади
    cols = [(x, .6) for x in (-4.2, -1.4, 1.4, 4.2)] + [(x, Lg - .6) for x in (-4.2, -1.4, 1.4, 4.2)]
    cols += [(s * 4.2, .6 + (Lg - 1.2) * t / 5) for s in (-1, 1) for t in range(1, 5)]
    for x, y in cols:
        column(b, f.at(x, y, zb), 6.2, .38, mat="StoneFar")
    # Целла
    course(b, "StoneFar", f, -3.0, 3.0, 3.2, 3.6, zb, zb + 6.0, block=2.0)
    for z in range(4):
        course(b, "StoneFar", f, -3.0, 3.0, 3.2, Lg - 1.6, zb + z * 1.5 + .75, zb + (z + 1) * 1.5, block=3.0)
    b.lbox("Void", f, 0, 3.15, zb + 2.0, 2.2, .1, 4.0)
    banner(b, f.at(-2.8, .3, zb + 5.6), 1.0, 3.0)
    banner(b, f.at(2.8, .3, zb + 5.6), 1.0, 3.0)
    # Антаблемент
    zt = zb + 6.2
    course(b, "StoneFar", f, -W / 2, W / 2, 0, Lg, zt, zt + .9, block=2.2)
    b.lbox("Gold", f, 0, -.05, zt + .45, W, .06, .12)
    # Фронтоны и крыша
    ph = 2.4
    for y0, dy in ((-.05, .5), (Lg - .45, .5)):
        b.prism("RedPanel", [(-W / 2, zt + .9), (W / 2, zt + .9), (0, zt + .9 + ph)], f, y0 + .1, dy - .2)
        b.prism("StoneFar", [(-W / 2 - .3, zt + .8), (W / 2 + .3, zt + .8), (W / 2 + .3, zt + 1.05), (0, zt + 1.05 + ph),
                             (-W / 2 - .3, zt + 1.05)], f, y0 + .3, .25)
    laurel(b, f, 0, -.1, zt + 1.7, .6)
    ridge = f.p(0, Lg / 2, zt + 1.05 + ph)
    for s in (-1, 1):
        ax = (f.x * s * (W / 2 + .4) - f.z * ph).normalized()       # от конька вниз к карнизу
        az = f.y.cross(ax).normalized()
        if az.z < 0:
            az = -az
        ln = math.hypot(W / 2 + .4, ph) + .3
        b.box("Roof", ridge + ax * ln / 2 + az * .2, ax, f.y, az, ln, Lg + .8, .35)
    for x in (-W / 2 - 1.5, W / 2 + 1.5):
        cypress(b, f.at(x, b.rnd.uniform(-1, 2), -.5), b.rnd.uniform(6, 8))
        bush(b, f.at(x * .9, -2.5, -.6), .8)


def aqueduct(b):
    """Лист 2 (вдали): двухъярусный акведук по холмам."""
    a0, a1, r = 292.0, 338.0, 170.0
    p0, p1 = U(r, a0, 0), U(r, a1, 0)
    d = (p1 - p0)
    L = d.length
    dx = d.normalized()
    side = UP.cross(dx)
    f = F(p0, dx, side)
    bay = 6.0
    n = int(L / bay)
    base = 14.0
    for i in range(n):
        x = (i + .5) * bay
        g = f.at(x, 0, 0)
        for z0, z1, w in ((base - 18, base + 8, 1.6), (base + 8, base + 14, 1.2)):
            zs = z1 - 1.0 - (bay - w) / 2
            b.lbox("StoneFar", f, i * bay, 0, (z0 + z1 - 1) / 2, w, 2.2, z1 - 1 - z0)
            voussoirs(b, "StoneFar", g, 0, -1.1, (bay - w) / 2, zs, .5, 2.2, n=7)
            b.prism("Plaster", arch_outline((bay - w) / 2 + .2, zs, z1 - 1.0), g, -1.0, 2.0)
            course(b, "StoneFar", g, -bay / 2, bay / 2, -1.2, 1.2, z1 - 1.0, z1, block=3.0)
    b.lbox("Brick", f, L / 2, 0, base + 14.4, L, 1.8, .8)


def scatter(b):
    rnd = random.Random(77)
    # кипарисы рощами
    groves = [(20, 78), (160, 92), (250, 84), (300, 76), (345, 96), (120, 110), (215, 100), (40, 130)]
    for gdeg, gr in groves:
        for k in range(rnd.randint(3, 6)):
            deg = gdeg + rnd.uniform(-5, 5)
            r = gr + rnd.uniform(-6, 6)
            cypress(b, F(U(r, deg, hill_h(r, deg) - .3), frame_x(deg), radial(deg)), rnd.uniform(7, 11))
    for k in range(70):
        deg = rnd.uniform(0, 360)
        r = rnd.uniform(64.5, 125)
        bush(b, F(U(r, deg, hill_h(r, deg) - .15), frame_x(deg), radial(deg)), rnd.uniform(.7, 1.4))
    # Обломки и упавшие колонны снаружи у руин
    for k in range(9):
        deg = rnd.uniform(RUIN[0] + 4, RUIN[1] - 4)
        r = rnd.uniform(63.8, 70)
        rubble(b, RF(r, deg, hill_h(r, deg)), rnd.uniform(1.0, 1.8))
    for k in range(4):
        deg = rnd.uniform(RUIN[0] + 6, RUIN[1] - 6)
        r = rnd.uniform(65, 70)
        g = RF(r, deg, hill_h(r, deg))
        column(b, g, 5.0, .42, broken=rnd.uniform(1.6, 3.2))
        for j in range(2):
            ax, ay, az = g.axes(rnd.uniform(0, 3), math.pi / 2, 0)
            b.cyl("Stone", g.p(rnd.uniform(1.5, 3), rnd.uniform(-1, 1), .45), ay, .42, 1.1, seg=14)


def decor(b):
    # Жаровни на кромках ярусов (не на пандусах)
    for i in range(12):
        deg = i * 30.0 + 7.5
        if not in_ranges(deg, RAMP_T1_ARENA) and not in_ranges(deg, RAMP_T2_T1):
            brazier(b, RF(30.9, deg, T1_Y), .85)
        deg2 = i * 30.0 + 22.5
        if not in_ranges(deg2, RAMP_T2_T1) and not near_any(deg2, ARCHES, 9):
            brazier(b, RF(38.95, deg2, T2_Y), .85)


def arena(b):
    bm = b.bm("Sand")
    res = bmesh.ops.create_circle(bm, cap_ends=True, segments=120, radius=28.75, matrix=Matrix.Translation((0, 0, .02)))
    arc_course(b, "Paving", 28.75, 29.98, -.1, .07, 0, 360, block=1.5, jitter=.01)
    arc_course(b, "Paving", 3.3, 4.4, -.1, .05, 0, 360, block=1.3, jitter=.01)
    # Немного камешков на песке
    for k in range(40):
        a = b.rnd.uniform(0, 360)
        r = b.rnd.uniform(6, 27)
        s = b.rnd.uniform(.12, .3)
        b.lbox("Stone", RF(r, a, 0), 0, 0, s * .3, s * 1.4, s, s, yaw=b.rnd.uniform(0, 3), tx=b.rnd.uniform(-.2, .2))


# ================================================================ материалы Blender, сборка, экспорт

def make_material(key):
    tex, mode, tile, color, metal, rough, emis, *_ = MATS[key]
    name = "M2_" + key
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    lin = [c ** 2.2 for c in color]
    bsdf.inputs["Base Color"].default_value = (*lin, 1)
    bsdf.inputs["Metallic"].default_value = metal
    bsdf.inputs["Roughness"].default_value = rough
    if emis > 0:
        bsdf.inputs["Emission Color"].default_value = (*lin, 1)
        bsdf.inputs["Emission Strength"].default_value = emis
    for n in [n for n in nt.nodes if n.type in ("TEX_IMAGE", "MIX")]:
        nt.nodes.remove(n)
    if tex:
        node = nt.nodes.new("ShaderNodeTexImage")
        node.image = bpy.data.images.get(tex)
        if node.image is None:
            node.image = bpy.data.images.load(os.path.join(TEX_DIR, tex + ".png"), check_existing=True)
        node.interpolation = 'Linear'
        if color != (1, 1, 1):
            mix = nt.nodes.new("ShaderNodeMix")
            mix.data_type = 'RGBA'
            mix.blend_type = 'MULTIPLY'
            mix.inputs[0].default_value = 1.0
            mix.inputs[7].default_value = (*lin, 1)
            nt.links.new(node.outputs["Color"], mix.inputs[6])
            nt.links.new(mix.outputs[2], bsdf.inputs["Base Color"])
        else:
            nt.links.new(node.outputs["Color"], bsdf.inputs["Base Color"])
    m.diffuse_color = (*lin, 1)
    return m


def world_uvs(bm, tile):
    uvl = bm.loops.layers.uv.active
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        for l in f.loops:
            co = l.vert.co
            if ax == 0:
                l[uvl].uv = (co.y / tile, co.z / tile)
            elif ax == 1:
                l[uvl].uv = (co.x / tile, co.z / tile)
            else:
                l[uvl].uv = (co.x / tile, co.y / tile)


def finalize(b, coll_name, prefix="Col_"):
    coll = bpy.data.collections.get(coll_name)
    if coll:
        for o in list(coll.objects):
            me = o.data
            bpy.data.objects.remove(o, do_unlink=True)
            if me and me.users == 0:
                bpy.data.meshes.remove(me)
    else:
        coll = bpy.data.collections.new(coll_name)
        bpy.context.scene.collection.children.link(coll)
    made = []
    for key, bm in b.bms.items():
        tex, mode, tile, *_ , bevel, seg, smooth_sh = MATS[key]
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        if mode == "world":
            world_uvs(bm, tile)
        name = prefix + key
        me = bpy.data.meshes.new(name)
        bm.to_mesh(me)
        bm.free()
        obj = bpy.data.objects.new(name, me)
        coll.objects.link(obj)
        me.materials.append(make_material(key))
        if smooth_sh:
            me.shade_smooth()
        if bevel > 0:
            mod = obj.modifiers.new("Bevel", "BEVEL")
            mod.width = bevel
            mod.segments = seg
            mod.limit_method = 'ANGLE'
            mod.angle_limit = math.radians(35)
            mod.harden_normals = True
        made.append((name, len(me.polygons)))
    b.bms = {}
    return made


def build_all(export_path=None):
    """Весь Колизей. export_path — папка Assets/_Project/Art/Coliseum (туда лягут обе версии FBX)."""
    make_all_textures()
    b = Builder(7)
    arena(b)
    portal_pedestal(b, F(Vector((0, 0, 0)), Vector((1, 0, 0)), Vector((0, 1, 0))))
    retaining_wall(b, ARENA_R, 0.0, T1_Y, 32, RAMP_T1_ARENA)
    paving_ring(b, ARENA_R + .66, T1_R - .2, T1_Y, 3, 2.3)
    retaining_wall(b, T1_R, T1_Y, T2_Y, 36, RAMP_T2_T1)
    paving_ring(b, T1_R + .66, T2_R - .1, T2_Y, 3, 2.4)
    podium_arcade(b)
    for a in ARCHES:
        gate(b, a)
    ramps(b)
    decor(b)
    cavea(b)
    crown(b)
    outer_facade(b)
    terrain(b)
    temple(b)
    aqueduct(b)
    scatter(b)
    made = finalize(b, COLL)
    split_sectors(COLL)
    if export_path:
        export_variants(export_path)
    return made


def build_kit():
    """Отдельный ряд пропсов для просмотра (далеко от Колизея, в экспорт не входит)."""
    b = Builder(3)
    o = Vector((0, -400, 0))
    def at(x):
        return F(o + Vector((x, 0, 0)), Vector((1, 0, 0)), Vector((0, 1, 0)))
    column(b, at(0), 5.0)
    column(b, at(3), 5.0, broken=2.6)
    banner(b, at(6).at(0, 0, 3.6), 1.3, 3.2)
    brazier(b, at(9))
    rubble(b, at(12.5), 1.0)
    rubble(b, at(16.5), 1.4)
    cypress(b, at(21), 9.0)
    bush(b, at(25), 1.0)
    bush(b, at(28), .8)
    portal_pedestal(b, at(34))
    # человек для масштаба (серый столбик)
    b.lbox("Shadow", at(-3), 0, 0, .9, .45, .3, 1.8)
    bm = b.bm("Sand")
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=30, matrix=Matrix.Translation(o + Vector((16, 0, 0))))
    return finalize(b, KIT, prefix="Kit_")


def export_fbx(path):
    bpy.ops.object.select_all(action='DESELECT')
    for o in bpy.data.collections[COLL].objects:
        o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, axis_forward='-Z', axis_up='Y',
                             apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
                             bake_space_transform=True, use_mesh_modifiers=True,
                             mesh_smooth_type='FACE', bake_anim=False, path_mode='STRIP')


# ================================================================ оптимизация: сектора и облегчённая версия

def split_sectors(coll_name=COLL, n=8):
    """Режет каждый объект-материал на n угловых секторов (Col_Stone → Col_Stone_S0..S7), чтобы в Unity
    работало отсечение по камере: рисуется только та часть Колизея, что в кадре."""
    coll = bpy.data.collections[coll_name]
    made = 0
    for o in list(coll.objects):
        if o.name.rsplit("_", 1)[-1].startswith("S") and o.name.rsplit("_", 1)[-1][1:].isdigit():
            continue
        me = o.data
        bm = bmesh.new()
        bm.from_mesh(me)
        groups = [set() for _ in range(n)]
        for fc in bm.faces:
            c = fc.calc_center_median()
            a = math.degrees(math.atan2(c.y, c.x)) % 360.0
            groups[int(a / (360.0 / n)) % n].add(fc.index)
        for k in range(n):
            if not groups[k]:
                continue
            bk = bm.copy()
            bk.faces.ensure_lookup_table()
            bmesh.ops.delete(bk, geom=[fc for fc in bk.faces if fc.index not in groups[k]], context='FACES')
            mk = bpy.data.meshes.new(f"{o.name}_S{k}")
            bk.to_mesh(mk)
            bk.free()
            for m in me.materials:
                mk.materials.append(m)
            ok = bpy.data.objects.new(mk.name, mk)
            coll.objects.link(ok)
            for mod in o.modifiers:
                nm = ok.modifiers.new(mod.name, mod.type)
                if mod.type == 'BEVEL':
                    nm.width, nm.segments, nm.limit_method = mod.width, mod.segments, mod.limit_method
                    nm.angle_limit, nm.harden_normals = mod.angle_limit, mod.harden_normals
            made += 1
        bm.free()
        bpy.data.objects.remove(o, do_unlink=True)
        if me.users == 0:
            bpy.data.meshes.remove(me)
    return made


def export_variants(folder):
    """Две версии для Unity: полная (с фасками) и облегчённая (без фасок) — для настроек графики «Средняя/Низкая»."""
    export_fbx(os.path.join(folder, "Coliseum_Art.fbx"))
    bevels = [m for o in bpy.data.collections[COLL].objects for m in o.modifiers if m.type == 'BEVEL']
    for m in bevels:
        m.show_viewport = m.show_render = False
    try:
        export_fbx(os.path.join(folder, "Coliseum_Art_Low.fbx"))
    finally:
        for m in bevels:
            m.show_viewport = m.show_render = True