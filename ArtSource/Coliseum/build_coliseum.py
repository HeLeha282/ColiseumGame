# Генератор модели Колизея для ColiseumGame (Blender 5.x).
# Запуск: в Blender → Scripting → открыть файл → Run, или exec(open(путь).read()).
#
# Размеры совпадают с геймплейной геометрией из Assets/_Project/Editor/PrototypeBuilder.Level.cs:
#   арена r 0..30 (y=0), 1-й ярус r 30..38 (y=3), 2-й ярус r 38..45 (y=6), стена r 45..46.5 до y=11,
#   арки на углах 45/135/225/315°, пандусы на тех же углах. Здесь только визуал — коллайдеры в Unity остаются серыми.
#
# Координаты: функция U(r, угол Unity, высота Unity) сразу даёт точку Blender, которая после экспорта
# FBX (axis_forward='-Z', axis_up='Y', bake_space_transform=True) встанет в нужную точку Unity:
# Blender (x, y, z) -> Unity (-x, z, -y).

import bpy, bmesh, math, random, os
import numpy as np
from mathutils import Vector, Matrix

HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else bpy.path.abspath("//")
TEX_DIR = os.path.join(HERE, "Textures")

ARENA_R, T1_R, T1_Y, T2_R, T2_Y, WALL_R, WALL_Y = 30.0, 38.0, 3.0, 45.0, 6.0, 46.5, 11.0
ARCHES = [45.0, 135.0, 225.0, 315.0]
COLL = "ColiseumArt"
rnd = random.Random(7)


# ---------------------------------------------------------------- координаты

def U(r, deg, y):
    a = math.radians(deg)
    return Vector((-r * math.cos(a), -r * math.sin(a), y))

def radial(deg):
    return U(1.0, deg, 0.0)

def frame_x(deg):
    # направление уменьшения угла Unity (так базис x, radial, up правый)
    a = math.radians(deg)
    return Vector((-math.sin(a), math.cos(a), 0.0))

UP = Vector((0, 0, 1))

def dangle(a, b):
    return (a - b + 180.0) % 360.0 - 180.0

def near_any(deg, centers, half):
    return any(abs(dangle(deg, c)) < half for c in centers)

def in_ranges(deg, ranges):
    for lo, hi in ranges:
        mid = (lo + hi) * 0.5
        if abs(dangle(deg, mid)) <= (hi - lo) * 0.5:
            return True
    return False

# Зоны пандусов (углы Unity), чтобы не ставить туда декор
RAMP_T1_ARENA = [(a - 199, a - 173) for a in ARCHES]
RAMP_T2_T1 = [(a - 100, a - 78) for a in ARCHES]


# ---------------------------------------------------------------- построитель мешей

class Builder:
    def __init__(self):
        self.bms = {}

    def bm(self, mat):
        if mat not in self.bms:
            self.bms[mat] = bmesh.new()
        return self.bms[mat]

    def box(self, mat, center, bx, by, bz, sx, sy, sz):
        """Коробка с центром center и осями bx, by, bz (единичные, правый базис) и размерами sx, sy, sz."""
        m = Matrix((
            (bx.x * sx, by.x * sy, bz.x * sz, center.x),
            (bx.y * sx, by.y * sy, bz.y * sz, center.y),
            (bx.z * sx, by.z * sy, bz.z * sz, center.z),
            (0, 0, 0, 1)))
        bmesh.ops.create_cube(self.bm(mat), size=1.0, matrix=m)

    def ring_box(self, mat, r_in, r_out, y0, y1, deg, length, jitter=0.0):
        rm = (r_in + r_out) * 0.5
        c = U(rm, deg, (y0 + y1) * 0.5)
        h = y1 - y0
        if jitter:
            c.z += rnd.uniform(-jitter, jitter) * 0.5
            h += rnd.uniform(-jitter, jitter)
        self.box(mat, c, frame_x(deg), radial(deg), UP, length, r_out - r_in, h)

    def cylinder(self, mat, base, axis, radius, height, seg=12):
        axis = axis.normalized()
        rot = axis.to_track_quat('Z', 'Y').to_matrix().to_4x4()
        m = Matrix.Translation(base + axis * height * 0.5) @ rot
        bmesh.ops.create_cone(self.bm(mat), cap_ends=True, segments=seg, radius1=radius, radius2=radius,
                              depth=height, matrix=m)

    def cone(self, mat, base, r1, r2, height, seg=10):
        m = Matrix.Translation(base + UP * height * 0.5)
        bmesh.ops.create_cone(self.bm(mat), cap_ends=True, segments=seg, radius1=r1, radius2=r2,
                              depth=height, matrix=m)

    def prism(self, mat, outline, origin, fx, fy, fz, depth):
        """Плоский многоугольник outline [(u, v)] в плоскости (fx, fz), выдавленный на depth вдоль fy."""
        bm = self.bm(mat)
        front = [bm.verts.new(origin + fx * u + fz * v) for u, v in outline]
        back = [bm.verts.new(origin + fx * u + fz * v + fy * depth) for u, v in outline]
        n = len(outline)
        faces = [bm.faces.new(front), bm.faces.new(list(reversed(back)))]
        for i in range(n):
            j = (i + 1) % n
            faces.append(bm.faces.new((front[i], back[i], back[j], front[j])))
        bmesh.ops.triangulate(bm, faces=faces[:2], quad_method='BEAUTY', ngon_method='EAR_CLIP')


def ring_course(b, mat, r_in, r_out, y0, y1, block_len, offset=0.0, gap=0.05, skip=None, jitter=0.0):
    rm = (r_in + r_out) * 0.5
    n = max(8, int(round(2 * math.pi * rm / block_len)))
    step = 360.0 / n
    chord = 2 * rm * math.sin(math.radians(step) / 2)
    for i in range(n):
        deg = i * step + offset * step
        if skip and skip(deg):
            continue
        m = mat
        if mat == "Stone" and rnd.random() < 0.35:
            m = "StoneB"
        b.ring_box(m, r_in, r_out, y0, y1, deg, chord - gap, jitter)


# ---------------------------------------------------------------- части Колизея

def build_arena(b):
    # Песок арены
    bm = b.bm("Sand")
    bmesh.ops.create_circle(bm, cap_ends=True, segments=96, radius=ARENA_R,
                            matrix=Matrix.Translation((0, 0, 0.02)))
    # Кольцо плит у подиума
    ring_course(b, "Paving", 28.5, 29.75, -0.08, 0.06, 1.6, jitter=0.02)
    # Мощёная площадка вокруг портала: два кольца плит и тёмный бордюр
    ring_course(b, "Paving", 2.9, 4.4, -0.08, 0.07, 1.3, jitter=0.01)
    ring_course(b, "StoneB", 4.4, 5.9, -0.08, 0.07, 1.5, offset=0.5, jitter=0.01)
    ring_course(b, "StoneDark", 5.9, 6.35, -0.08, 0.11, 1.2)


def build_podium(b):
    """Внутренняя стена 1-го яруса (r=30, высота 0..3) из рустованных блоков."""
    courses = [(0.0, 0.95), (0.95, 1.9), (1.9, 2.65)]
    for k, (y0, y1) in enumerate(courses):
        ring_course(b, "Stone", 29.75, 30.35, y0 + 0.03, y1 - 0.03, 2.1, offset=0.5 * (k % 2), jitter=0.03)
    # Карниз-кап — он же край дорожки 1-го яруса
    ring_course(b, "StoneDark", 29.6, 30.45, 2.65, 3.02, 2.6)


def build_tier_paving(b, r_from, r_to, y_top, rows, tile, skip=None):
    w = (r_to - r_from) / rows
    for k in range(rows):
        r0 = r_from + k * w
        ring_course(b, "Paving", r0 + 0.03, r0 + w - 0.03, y_top - 0.16, y_top + 0.02, tile,
                    offset=0.5 * (k % 2), gap=0.07, skip=skip, jitter=0.015)


def build_tier2_riser(b):
    """Подпорная стена 2-го яруса (r=38, высота 3..6)."""
    for k, (y0, y1) in enumerate([(3.0, 4.0), (4.0, 5.0), (5.0, 5.7)]):
        ring_course(b, "Stone", 37.8, 38.4, y0 + 0.03, y1 - 0.03, 2.3, offset=0.5 * (k % 2), jitter=0.03)
    ring_course(b, "StoneDark", 37.65, 38.5, 5.7, 6.02, 2.8)
    # Пилястры (не там, где пандусы)
    for i in range(48):
        deg = i * 7.5 + 3.75
        if in_ranges(deg, RAMP_T2_T1):
            continue
        b.ring_box("StoneDark", 37.55, 37.85, 3.0, 5.7, deg, 0.7)


def arch_outline(hw, spring, top, seg=12):
    pts = [(hw, spring), (hw, top), (-hw, top), (-hw, spring)]
    for i in range(1, seg):
        t = math.pi * (1 - i / seg)
        pts.append((hw * math.cos(t), spring + hw * math.sin(t)))
    return pts


def voussoirs(b, mat, origin, fx, fy, hw, spring, thick, depth, n=9, key_mat=None):
    for i in range(n):
        t = math.pi * (i + 0.5) / n
        r = hw + thick * 0.5
        c = origin + fx * (r * math.cos(t)) + UP * (spring + r * math.sin(t))
        bx = fx * math.sin(t) - UP * math.cos(t)
        bz = fx * math.cos(t) + UP * math.sin(t)
        length = math.pi * r / n * 0.92
        key = i == n // 2
        b.box(key_mat if (key and key_mat) else mat, c, bx, fy, bz,
              length * (1.25 if key else 1.0), depth + (0.12 if key else 0.0), thick * (1.2 if key else 1.0))


def build_outer_arcade(b):
    """Внутренний фасад внешней стены (r=45, высота 6..11): аркада с полуколоннами и антаблементом."""
    bay = 7.5
    pier_w = 1.25
    for i in range(48):
        deg = i * bay + bay / 2
        if near_any(deg, ARCHES, 6.5):
            continue
        fx, fy = frame_x(deg), radial(deg)
        r_face = 44.75
        half_w = 2 * r_face * math.sin(math.radians(bay) / 2) / 2
        # Задняя стенка ниши
        b.ring_box("Shadow", 45.9, 46.6, 6.0, 10.2, deg, half_w * 2)
        # Панель с аркой между опорами
        hw = half_w - pier_w / 2
        origin = U(r_face, deg, 0.0)
        b.prism("Stone", arch_outline(hw, 7.7, 10.2), origin, fx, fy, UP, 1.15)
        voussoirs(b, "Stone", origin + fy * -0.08, fx, fy, hw, 7.7, 0.45, 1.3, n=9, key_mat="StoneDark")
        # Нижний подоконник ниши
        b.ring_box("StoneDark", 44.9, 46.0, 6.0, 6.35, deg, hw * 2)
    # Опоры с полуколоннами на границах пролётов
    for i in range(48):
        deg = i * bay
        if near_any(deg, ARCHES, 7.0):
            continue
        b.ring_box("Stone", 44.7, 46.5, 6.0, 10.2, deg, pier_w)
        base = U(44.55, deg, 6.0)
        b.ring_box("StoneDark", 44.1, 44.8, 6.0, 6.45, deg, 0.95)          # база
        b.cylinder("Stone", base + UP * 0.45, UP, 0.34, 3.2, seg=12)        # ствол
        b.ring_box("StoneDark", 44.05, 44.8, 9.65, 10.05, deg, 1.0)        # капитель
        b.ring_box("Gold", 44.02, 44.12, 9.72, 9.98, deg, 0.8)              # золотой поясок
    # Антаблемент и карниз
    ring_course(b, "Stone", 44.35, 46.6, 10.2, 11.0, 3.0)
    ring_course(b, "StoneDark", 44.1, 46.6, 11.0, 11.35, 3.4, offset=0.5)
    # Низкий парапет над карнизом
    ring_course(b, "Stone", 44.35, 44.85, 11.35, 12.05, 2.2, gap=0.08)


def build_stands(b):
    """Зрительские трибуны, поднимающиеся от стены наружу (видны с арены над стеной)."""
    rows = 12
    depth, rise = 0.85, 0.5
    r0, y0 = 46.6, 11.35
    for k in range(rows):
        ri = r0 + k * depth
        top = y0 + (k + 1) * rise
        ring_course(b, "Seats" if k % 2 == 0 else "SeatsB", ri, ri + depth + 0.02, 11.0, top, 3.2, offset=0.5 * (k % 2), gap=0.06)
    # Лестницы-проходы каждые 30°
    for i in range(12):
        deg = i * 30.0 + 15.0
        for k in range(rows * 2):
            ri = r0 + k * depth / 2
            top = y0 + (k + 1) * rise / 2 + 0.02
            b.ring_box("StoneDark", ri, ri + depth / 2, 11.0, top, deg, 1.4)
    # Задняя стена-аттик с окнами
    r_back = r0 + rows * depth
    top_y = y0 + rows * rise
    bay = 7.5
    for i in range(48):
        deg = i * bay + bay / 2
        fx, fy = frame_x(deg), radial(deg)
        half_w = 2 * r_back * math.sin(math.radians(bay) / 2) / 2
        b.ring_box("Shadow", r_back + 0.9, r_back + 1.4, top_y, top_y + 3.4, deg, half_w * 2)
        hw_w = min(half_w - 0.6, 1.5)
        b.prism("Stone", arch_outline(hw_w, top_y + 3.4 - hw_w - 0.35, top_y + 3.4, seg=10),
                U(r_back, deg, 0.0), fx, fy, UP, 0.9)
    ring_course(b, "Stone", r_back - 0.1, r_back + 1.45, top_y + 3.4, top_y + 4.0, 3.0)
    ring_course(b, "StoneDark", r_back - 0.3, r_back + 1.5, top_y + 4.0, top_y + 4.35, 3.4, offset=0.5)
    # Мачты с вымпелами
    for i in range(24):
        deg = i * 15.0
        base = U(r_back + 0.6, deg, top_y + 4.35)
        b.cylinder("Wood", base, UP, 0.12, 3.6, seg=8)
        fx = frame_x(deg)
        b.box("Cloth", base + UP * 3.0 + fx * 0.55, fx, radial(deg), UP, 1.0, 0.04, 0.7)


def build_gate(b, deg):
    """Парадная арка-спавн на 2-м ярусе."""
    fx, fy = frame_x(deg), radial(deg)
    # Пилоны
    for s in (-1, 1):
        c = U(45.4, deg, 0.0) + fx * (s * 4.6)
        b.box("Stone", c + UP * 9.0, fx, fy, UP, 1.8, 2.4, 6.0)
        b.box("StoneDark", c + UP * 6.25, fx, fy, UP, 2.1, 2.7, 0.5)
        b.box("StoneDark", c + UP * 12.15, fx, fy, UP, 2.2, 2.8, 0.3)
        # Знамя на пилоне
        bc = c - fy * 1.25 + UP * 8.6
        b.box("Cloth", bc, fx, fy, UP, 1.2, 0.05, 2.6)
        b.box("Gold", bc + UP * 1.35, fx, fy, UP, 1.4, 0.12, 0.12)
        laurel(b, bc - fy * 0.05 + UP * 0.3, fx, fy, 0.38)
        # Жаровня у подножия
        brazier(b, U(43.2, deg, T2_Y) + fx * (s * 4.2))
    # Арка: панель и клинчатые камни
    origin = U(44.35, deg, 0.0)
    b.prism("Stone", arch_outline(3.3, 8.4, 12.0), origin, fx, fy, UP, 2.1)
    voussoirs(b, "Stone", origin + fy * -0.1, fx, fy, 3.3, 8.4, 0.6, 2.3, n=11, key_mat="StoneDark")
    # Аттик с лавровым венком
    att = U(44.6, deg, 13.6)
    b.box("Stone", att, fx, fy, UP, 10.6, 2.6, 3.2)
    b.box("StoneDark", att + UP * 1.75, fx, fy, UP, 11.0, 2.9, 0.35)
    b.box("StoneDark", att - UP * 1.55, fx, fy, UP, 11.0, 2.9, 0.3)
    b.box("Gold", att - fy * 1.31 - UP * 0.95, fx, fy, UP, 8.0, 0.08, 0.16)
    b.box("Cloth", att - fy * 1.33 + UP * 0.15, fx, fy, UP, 3.0, 0.05, 2.2)
    laurel(b, att - fy * 1.4 + UP * 0.2, fx, fy, 0.95)
    # Большие знамёна, свисающие с аттика по бокам арки
    for s in (-1, 1):
        bc = att - fy * 1.33 + fx * (s * 3.3) - UP * 1.9
        b.box("Cloth", bc, fx, fy, UP, 1.1, 0.05, 3.2)
        b.box("Cloth", bc - UP * 1.75 + fx * 0.3, fx, fy, UP, 0.4, 0.05, 0.4)
        b.box("Cloth", bc - UP * 1.75 - fx * 0.3, fx, fy, UP, 0.4, 0.05, 0.4)
    # Поднятая решётка
    for k in range(9):
        u = -2.8 + k * 0.7
        b.box("Iron", U(45.6, deg, 11.0) + fx * u, fx, fy, UP, 0.12, 0.12, 1.6)
    b.box("Iron", U(45.6, deg, 10.3), fx, fy, UP, 6.0, 0.14, 0.14)
    # Тоннель за аркой
    t = U(48.7, deg, 0.0)
    b.box("StoneDark", t + UP * 5.85, fx, fy, UP, 7.0, 4.6, 0.3)          # пол
    b.box("Void", t + fy * 2.2 + UP * 8.5, fx, fy, UP, 7.0, 0.3, 5.0)      # тьма в глубине
    b.box("StoneDark", t + UP * 11.4, fx, fy, UP, 7.6, 4.6, 0.6)          # свод
    for s in (-1, 1):
        b.box("StoneDark", t + fx * (s * 3.6) + UP * 8.6, fx, fy, UP, 0.5, 4.6, 5.2)


def laurel(b, center, fx, fy, radius):
    """Золотой лавровый венок из листиков."""
    for side in (-1, 1):
        for i in range(8):
            t = math.radians(-70 + i * 20)
            p = center + fx * (side * radius * math.cos(t)) + UP * (radius * math.sin(t))
            tang = (fx * (-side * math.sin(t)) + UP * math.cos(t)).normalized()
            tilt = (tang * math.cos(0.6) + fx * side * math.sin(0.6)).normalized()
            bz = tilt
            bx = fy.cross(bz).normalized()
            b.box("Gold", p, bx, fy, bz, radius * 0.18, 0.05, radius * 0.42)
    b.box("Gold", center - UP * radius * 1.0, fx, fy, UP, radius * 0.5, 0.06, radius * 0.16)


def brazier(b, base):
    fx, fy = Vector((1, 0, 0)), Vector((0, 1, 0))
    for i in range(3):
        a = math.radians(i * 120)
        foot = base + Vector((math.cos(a) * 0.45, math.sin(a) * 0.45, 0))
        top = base + UP * 1.05
        b.cylinder("Iron", foot, (top - foot), 0.05, (top - foot).length, seg=6)
    b.cone("Iron", base + UP * 1.0, 0.25, 0.55, 0.35, seg=10)
    b.cone("Fire", base + UP * 1.3, 0.38, 0.0, 0.75, seg=7)
    b.cone("Fire", base + UP * 1.3 + Vector((0.12, 0.05, 0)), 0.2, 0.0, 1.0, seg=6)


def build_ramp(b, r, deg_top, y_top, deg_bot, y_bot, width, floor_y):
    """Пандус ступенями: сплошной клин из блоков от пола до поверхности."""
    top, bot = U(r, deg_top, y_top), U(r, deg_bot, y_bot)
    along = top - bot
    flat = Vector((along.x, along.y, 0))
    length = flat.length
    d = flat.normalized()
    side = UP.cross(d).normalized()      # правый базис: side, d, up
    steps = max(4, int(length / 0.45))
    step = length / steps
    rise = (y_top - y_bot) / steps
    # Профиль лестницы сбоку (u — вдоль пандуса, v — высота), выдавленный на ширину пандуса
    pts = [(0.0, floor_y)]
    for i in range(steps):
        h = y_bot + rise * (i + 0.5) + 0.06
        pts.append((step * i, h))
        pts.append((step * (i + 1), h))
    pts.append((length, floor_y))
    origin = Vector((bot.x, bot.y, 0.0)) - side * (width / 2)
    b.prism("Paving", pts, origin, d, side, UP, width)
    # Бортики по краям
    for s in (0.0, width):
        o = Vector((bot.x, bot.y, 0.0)) - side * (width / 2) + side * s
        rail = [(0.0, floor_y), (0.0, y_bot + 0.45), (length, y_top + 0.45), (length, floor_y)]
        b.prism("StoneDark", rail, o - side * 0.12, d, side, UP, 0.24)


def build_ramps(b):
    for a in ARCHES:
        build_ramp(b, 37.0, a - 80, T2_Y, a - 98, T1_Y, 2.2, T1_Y - 0.2)
        build_ramp(b, 29.0, a - 175, T1_Y, a - 197, 0.0, 2.2, -0.1)


def build_decor(b):
    # Знамёна на подиуме арены и жаровни на кромке 1-го яруса
    for i in range(12):
        deg = i * 30.0 + 15.0
        if in_ranges(deg, RAMP_T1_ARENA):
            continue
        fx, fy = frame_x(deg), radial(deg)
        c = U(29.55, deg, 1.75)
        b.box("Cloth", c, fx, fy, UP, 1.5, 0.05, 1.7)
        b.box("Gold", c + UP * 0.9, fx, fy, UP, 1.7, 0.12, 0.12)
        b.box("Cloth", c - UP * 0.95 + fx * 0.4, fx, fy, UP, 0.5, 0.05, 0.3)
        b.box("Cloth", c - UP * 0.95 - fx * 0.4, fx, fy, UP, 0.5, 0.05, 0.3)
        laurel(b, c - fy * 0.05 + UP * 0.1, fx, fy, 0.42)
    for i in range(12):
        deg = i * 30.0
        if in_ranges(deg, RAMP_T1_ARENA) or in_ranges(deg, RAMP_T2_T1):
            continue
        brazier(b, U(30.15, deg, T1_Y))
    # Знамёна на парапете над аркадой
    for i in range(16):
        deg = i * 22.5 + 11.25
        if near_any(deg, ARCHES, 9):
            continue
        fx, fy = frame_x(deg), radial(deg)
        c = U(44.28, deg, 10.4)
        b.box("Cloth", c, fx, fy, UP, 1.3, 0.05, 2.2)
        b.box("Gold", c + UP * 1.15, fx, fy, UP, 1.5, 0.12, 0.12)
        laurel(b, c - fy * 0.05 + UP * 0.2, fx, fy, 0.38)


# ---------------------------------------------------------------- материалы и текстуры

MATS = {
    #           base color (sRGB)        metal  rough  emission  texture  bevel
    "Stone":     ((0.90, 0.81, 0.65), 0.0, 0.85, 0.0, "stone", 0.07),
    "StoneDark": ((0.68, 0.56, 0.42), 0.0, 0.88, 0.0, "stone", 0.06),
    "StoneB":    ((0.84, 0.72, 0.55), 0.0, 0.85, 0.0, "stone", 0.07),
    "Shadow":    ((0.40, 0.32, 0.25), 0.0, 0.95, 0.0, "stone", 0.04),
    "Mortar":    ((0.30, 0.24, 0.19), 0.0, 1.0, 0.0, None, 0.0),
    "Paving":    ((0.82, 0.72, 0.57), 0.0, 0.9, 0.0, "stone", 0.05),
    "Seats":     ((0.86, 0.77, 0.61), 0.0, 0.9, 0.0, "stone", 0.05),
    "SeatsB":    ((0.74, 0.64, 0.49), 0.0, 0.9, 0.0, "stone", 0.05),
    "Sand":      ((0.87, 0.75, 0.53), 0.0, 1.0, 0.0, "sand", 0.0),
    "Wood":      ((0.52, 0.32, 0.17), 0.0, 0.8, 0.0, None, 0.03),
    "Iron":      ((0.16, 0.15, 0.15), 0.6, 0.45, 0.0, None, 0.02),
    "Gold":      ((0.95, 0.70, 0.28), 1.0, 0.3, 0.0, None, 0.01),
    "Cloth":     ((0.64, 0.11, 0.11), 0.0, 0.85, 0.0, None, 0.0),
    "Fire":      ((1.00, 0.55, 0.12), 0.0, 1.0, 6.0, None, 0.0),
    "Void":      ((0.03, 0.025, 0.02), 0.0, 1.0, 0.0, None, 0.0),
}

TILE = {"stone": 3.0, "sand": 8.0}


def tileable_noise(n, octaves, seed):
    rng = np.random.default_rng(seed)
    img = np.zeros((n, n))
    total = 0.0
    for g, amp in octaves:
        grid = rng.random((g, g))
        x = np.arange(n) * g / n
        i0 = np.floor(x).astype(int)
        f = x - i0
        f = f * f * (3 - 2 * f)
        i1 = (i0 + 1) % g
        a, bb = grid[i0][:, i0], grid[i0][:, i1]
        c, d = grid[i1][:, i0], grid[i1][:, i1]
        top = a * (1 - f) + bb * f
        bot = c * (1 - f) + d * f
        img += amp * (top * (1 - f)[:, None] + bot * f[:, None])
        total += amp
    img /= total
    return (img - img.min()) / (img.max() - img.min())


def make_texture(name, color, kind, seed):
    os.makedirs(TEX_DIR, exist_ok=True)
    n = 512
    if kind == "sand":
        v = tileable_noise(n, ((4, 0.5), (16, 0.3), (128, 0.35)), seed)
        bright = 0.88 + 0.12 * v
    else:
        v = tileable_noise(n, ((3, 0.45), (8, 0.3), (24, 0.2), (96, 0.12)), seed)
        bright = 0.84 + 0.16 * v
        speck = tileable_noise(n, ((256, 1.0),), seed + 99)
        bright -= (speck > 0.9) * 0.08
    rgb = np.stack([np.clip(bright * color[i] * (1.0 + (v - 0.5) * 0.03 * (i - 1)), 0, 1) for i in range(3)], -1)
    rgba = np.concatenate([rgb, np.ones((n, n, 1))], -1).astype(np.float32)
    img = bpy.data.images.get(name) or bpy.data.images.new(name, n, n, alpha=False)
    img.scale(n, n)
    img.pixels.foreach_set(rgba.ravel())
    path = os.path.join(TEX_DIR, name + ".png")
    img.filepath_raw = path
    img.file_format = 'PNG'
    img.save()
    return img


def make_material(key):
    color, metal, rough, emis, tex, _ = MATS[key]
    name = "M_Col_" + key
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
    for n in [n for n in nt.nodes if n.type == "TEX_IMAGE"]:
        nt.nodes.remove(n)
    if tex:
        img = make_texture("T_Col_" + key, color, tex, seed=abs(hash(key)) % 1000)
        node = nt.nodes.new("ShaderNodeTexImage")
        node.image = img
        nt.links.new(node.outputs["Color"], bsdf.inputs["Base Color"])
    m.diffuse_color = (*lin, 1)
    return m


# ---------------------------------------------------------------- сборка

def apply_uvs(bm, tile):
    uv = bm.loops.layers.uv.verify()
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        for l in f.loops:
            co = l.vert.co
            if ax == 0:
                l[uv].uv = (co.y / tile, co.z / tile)
            elif ax == 1:
                l[uv].uv = (co.x / tile, co.z / tile)
            else:
                l[uv].uv = (co.x / tile, co.y / tile)


def finalize(b):
    coll = bpy.data.collections.get(COLL)
    if coll:
        for o in list(coll.objects):
            bpy.data.objects.remove(o, do_unlink=True)
    else:
        coll = bpy.data.collections.new(COLL)
        bpy.context.scene.collection.children.link(coll)
    made = []
    for key, bm in b.bms.items():
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        tex = MATS[key][4]
        apply_uvs(bm, TILE.get(tex, 3.0))
        me = bpy.data.meshes.get("Col_" + key)
        if me:
            bpy.data.meshes.remove(me)
        me = bpy.data.meshes.new("Col_" + key)
        bm.to_mesh(me)
        bm.free()
        obj = bpy.data.objects.new("Col_" + key, me)
        coll.objects.link(obj)
        me.materials.append(make_material(key))
        bevel = MATS[key][5]
        if bevel > 0:
            for p in me.polygons:
                p.use_smooth = True
            mod = obj.modifiers.new("Bevel", "BEVEL")
            mod.width = bevel
            mod.segments = 2
            mod.limit_method = 'ANGLE'
            mod.angle_limit = math.radians(35)
            mod.harden_normals = True
        made.append((obj.name, len(me.polygons)))
    return made


def build_mortar(b):
    """Сплошные тёмные кольца за кладкой и под плитами, чтобы в швах не просвечивало небо."""
    def solid(r_in, r_out, y0, y1):
        ring_course(b, "Mortar", r_in, r_out, y0, y1, 2.5, gap=-0.06)
    solid(29.95, 30.3, 0.0, 2.7)            # за подиумом
    solid(28.5, 29.8, -0.12, -0.04)         # под плитами у подиума
    solid(30.3, 37.9, T1_Y - 0.2, T1_Y - 0.12)   # под плитами 1-го яруса
    solid(38.0, 38.35, T1_Y, T2_Y - 0.25)   # за подпорной стеной 2-го яруса
    solid(38.4, 44.8, T2_Y - 0.2, T2_Y - 0.12)   # под плитами 2-го яруса
    solid(44.9, 46.55, 10.1, 11.3)          # за антаблементом
    solid(46.6, 57.0, 10.95, 11.05)         # под трибунами
    solid(2.9, 6.35, -0.12, -0.04)          # под площадкой портала


def build_all():
    b = Builder()
    build_mortar(b)
    build_arena(b)
    build_podium(b)
    build_tier_paving(b, 30.45, 37.8, T1_Y, 3, 2.3)
    build_tier2_riser(b)
    build_tier_paving(b, 38.5, 44.7, T2_Y, 3, 2.4)
    build_outer_arcade(b)
    build_stands(b)
    for a in ARCHES:
        build_gate(b, a)
    build_ramps(b)
    build_decor(b)
    return finalize(b)


def export_fbx(path):
    bpy.ops.object.select_all(action='DESELECT')
    for o in bpy.data.collections[COLL].objects:
        o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, axis_forward='-Z', axis_up='Y',
                             apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
                             bake_space_transform=True, use_mesh_modifiers=True,
                             mesh_smooth_type='OFF', bake_anim=False, path_mode='STRIP')


if __name__ == "__main__":
    for o in list(bpy.data.objects):
        if o.name == "Cube":
            bpy.data.objects.remove(o, do_unlink=True)
    print(build_all())
