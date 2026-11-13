# Процедурные «рисованные» текстуры Колизея v2 (stylized hand-painted, как в референсах).
# Запуск внутри Blender: exec(open(путь).read()); затем make_all_textures().
# Пишет PNG в ArtSource/Coliseum/v2/Textures и создаёт одноимённые картинки в bpy.data.images.
#
# Виды текстур:
#   атласы блоков 2x2 (каждый блок кладки получает свой вариант целиком — светлая фаска, тёмная кромка,
#   трещины, сколы, как нарисовано на референсах): T2_Stone, T2_Paving
#   бесшовные (по мировым координатам): T2_Brick, T2_Plaster, T2_Sand, T2_Grass, T2_Roof
#   по форме предмета: T2_Banner (знамя с золотой каймой и лавровым венком)

import os
import numpy as np
import bpy

V2_DIR = os.path.join(r"C:\Users\А ну типо да\Desktop\ColizeunGame\ArtSource\Coliseum\v2")
TEX_DIR = os.path.join(V2_DIR, "Textures")


# ---------------------------------------------------------------- шум

def vnoise(n, g, rng, h=None):
    """Бесшовный value noise n x n (или h x n) с сеткой g."""
    h = h or n
    grid = rng.random((g, g))
    def axis(m):
        x = np.arange(m) * g / m
        i0 = np.floor(x).astype(int)
        f = x - i0
        f = f * f * (3 - 2 * f)
        return i0, (i0 + 1) % g, f
    xi0, xi1, fx = axis(n)
    yi0, yi1, fy = axis(h)
    a = grid[np.ix_(yi0, xi0)]
    b = grid[np.ix_(yi0, xi1)]
    c = grid[np.ix_(yi1, xi0)]
    d = grid[np.ix_(yi1, xi1)]
    fx = fx[None, :]
    fy = fy[:, None]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def fbm(n, octaves, seed, h=None):
    rng = np.random.default_rng(seed)
    img = np.zeros((h or n, n))
    tot = 0.0
    for g, amp in octaves:
        img += amp * vnoise(n, g, rng, h)
        tot += amp
    img /= tot
    return (img - img.min()) / (img.max() - img.min() + 1e-9)


def stamp_polyline(mask, pts, radius):
    """Мягкая линия толщиной radius (в пикселях) по точкам pts (в пикселях) — для трещин."""
    h, w = mask.shape
    r = int(np.ceil(radius)) + 1
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        steps = max(2, int(np.hypot(x1 - x0, y1 - y0) * 1.5))
        for t in np.linspace(0, 1, steps):
            cx, cy = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            xa, xb = max(0, int(cx) - r), min(w, int(cx) + r + 1)
            ya, yb = max(0, int(cy) - r), min(h, int(cy) + r + 1)
            if xa >= xb or ya >= yb:
                continue
            yy, xx = np.mgrid[ya:yb, xa:xb]
            d = np.hypot(xx - cx, yy - cy)
            mask[ya:yb, xa:xb] = np.maximum(mask[ya:yb, xa:xb], np.clip(1 - d / radius, 0, 1))


def warm_shade(base, shade, low=None):
    """Цвет = base * shade, но тени уходят в тёплое (оранжево-коричневое), свет — в кремовое, как в рисованном стиле."""
    s = np.clip(shade, .05, 2.0)
    gam = (.80, 1.0, 1.35)
    rgb = np.stack([base[i] * np.where(s < 1, s ** gam[i], 1 + (s - 1) * (.9, .95, 1.0)[i]) for i in range(3)], -1)
    if low is not None:
        t = (low - .5)[..., None]
        rgb *= 1 + t * np.array((.04, .0, -.05))
    return rgb


def smooth(x, a, b):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


# ---------------------------------------------------------------- каменный блок

def block_tile(n, base, seed, worn=0.3, moss=0.0):
    """Один нарисованный блок: пятнистый камень, светлая фаска по краю, тёмная кромка, трещины и сколы."""
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:n, 0:n] / (n - 1)
    low = fbm(n, ((2, .6), (3, .4)), seed)
    mid = fbm(n, ((6, .5), (12, .3), (24, .2)), seed + 1)
    fine = fbm(n, ((48, .6), (96, .4)), seed + 2)
    stroke = fbm(n, ((4, .5), (40, .5)), seed + 3, h=n)  # мазки
    edge = np.minimum(np.minimum(xx, 1 - xx), np.minimum(yy, 1 - yy))
    edge = edge + (mid - .5) * .03 + (fine - .5) * .012

    shade = 1.02 + .10 * (low - .5) + .06 * (mid - .5) + .035 * (fine - .5) + .03 * (stroke - .5)
    shade += .05 * (yy - .5)                                     # свет сверху
    band = smooth(edge, .012, .03) * (1 - smooth(edge, .05, .11))  # фаска
    shade += .13 * band
    rim = 1 - smooth(edge, .0, .022)
    shade *= 1 - .38 * rim

    # Сколы по краям
    chips = np.zeros((n, n))
    for _ in range(rng.integers(2, 6)):
        side = rng.integers(4)
        t = rng.random()
        cx, cy = [(t, 0), (t, 1), (0, t), (1, t)][side]
        rad = rng.uniform(.03, .08)
        d = np.hypot(xx - cx, yy - cy) + (mid - .5) * .05
        chips = np.maximum(chips, 1 - smooth(d, rad * .7, rad))
    shade *= 1 - .22 * chips * worn * 2

    # Трещины: от края внутрь ломаной
    crack = np.zeros((n, n))
    for _ in range(rng.integers(1, 4)):
        side = rng.integers(4)
        t = rng.uniform(.2, .8)
        x, y = [(t, .0), (t, 1.), (.0, t), (1., t)][side]
        dirx, diry = [(0, 1), (0, -1), (1, 0), (-1, 0)][side]
        pts = [(x * (n - 1), y * (n - 1))]
        ln = rng.uniform(.15, .45)
        segs = rng.integers(3, 6)
        for _ in range(segs):
            ang = np.arctan2(diry, dirx) + rng.uniform(-.8, .8)
            step = ln / segs
            x += np.cos(ang) * step
            y += np.sin(ang) * step
            pts.append((x * (n - 1), y * (n - 1)))
        stamp_polyline(crack, pts, n / 260)
    hl = np.roll(np.roll(crack, 2, 0), 2, 1)
    shade *= 1 - .42 * crack
    shade += .07 * np.clip(hl - crack, 0, 1)

    # Поры
    shade *= 1 - .08 * (fine > .88)

    rgb = warm_shade(base, shade, low)
    if moss > 0:
        m = smooth(fbm(n, ((5, .6), (20, .4)), seed + 9) * (1 - yy) ** 1.5, .45, .62) * moss
        rgb = rgb * (1 - m[..., None]) + np.array((.42, .5, .17))[None, None] * shade[..., None] * m[..., None]
    return np.clip(rgb, 0, 1)


def block_atlas(name, base, seed, n=512, worn=.3, moss=0.0):
    tiles = []
    for k in range(4):
        b = np.array(base) * (1 + (k - 1.5) * .025)
        b[0] *= 1 + (k % 2) * .015
        b[2] *= 1 - (k // 2) * .02
        tiles.append(block_tile(n, b, seed + k * 17, worn, moss))
    top = np.concatenate([tiles[0], tiles[1]], 1)
    bot = np.concatenate([tiles[2], tiles[3]], 1)
    return save(name, np.concatenate([top, bot], 0))


# ---------------------------------------------------------------- кирпич и штукатурка

def brick_pattern(n, rows, cols, seed, mortar=.035):
    """Возвращает (shade, id, edge) для бесшовной кирпичной кладки."""
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:n, 0:n] / n
    row = np.floor(yy * rows).astype(int)
    fv = yy * rows - row
    u = (xx + (row % 2) * .5 / cols) % 1.0
    col = np.floor(u * cols).astype(int)
    fu = u * cols - col
    bid = (row % rows) * cols + col % cols
    rand = rng.random(rows * cols * 2)
    ex = np.minimum(fu, 1 - fu) / cols * n      # расстояние до вертикального шва, пиксели
    ey = np.minimum(fv, 1 - fv) / rows * n      # до горизонтального шва, пиксели
    e = np.minimum(ex, ey)
    return bid, rand, e, fv


def make_brick(name="T2_Brick", n=512):
    rows, cols = 7, 3
    bid, rand, e, fv = brick_pattern(n, rows, cols, 5)
    mid = fbm(n, ((8, .5), (32, .5)), 6)
    fine = fbm(n, ((64, .5), (128, .5)), 7)
    r = rand[bid]
    base = np.stack([.70 + .12 * r, .30 + .08 * r, .18 + .05 * r], -1)
    shade = .95 + .1 * (mid - .5) + .05 * (fine - .5)
    shade += .12 * smooth(fv, .78, .95) * (1 - smooth(fv, .95, 1))     # светлый верхний край
    shade *= 1 - .18 * (1 - smooth(fv, 0, .2))                          # тень снизу
    mortar = 1 - smooth(e, 2.0, 4.5)
    col = warm_shade(base[0, 0] * 0 + 1, shade)[..., :] * base
    mcol = np.array((.42, .30, .22)) * (.9 + .2 * fine[..., None])
    col = col * (1 - mortar[..., None]) + mcol * mortar[..., None]
    return save(name, col)


def make_plaster(name="T2_Plaster", n=512):
    bid, rand, e, fv = brick_pattern(n, 12, 5, 11)
    low = fbm(n, ((3, .6), (6, .4)), 12)
    mid = fbm(n, ((12, .5), (24, .5)), 13)
    fine = fbm(n, ((64, .5), (128, .5)), 14)
    base = np.array((.76, .34, .22))
    shade = .95 + .14 * (low - .5) + .07 * (mid - .5) + .04 * (fine - .5)
    shade *= 1 - .06 * (1 - smooth(e, 1.0, 3.0))                        # кирпич сквозь штукатурку
    col = warm_shade(base, shade)
    # Облупившиеся участки — светлый камень с тёмным краем
    peel = fbm(n, ((4, .5), (10, .3), (40, .2)), 15)
    pm = smooth(peel, .70, .73)
    outline = smooth(peel, .67, .70) * (1 - pm)
    stone = np.array((.90, .78, .61)) * (shade[..., None] * 1.02)
    col = col * (1 - pm[..., None]) + stone * pm[..., None]
    col *= (1 - .35 * outline)[..., None]
    return save(name, col)


# ---------------------------------------------------------------- земля и крыша

def make_sand(name="T2_Sand", n=512):
    low = fbm(n, ((3, .6), (6, .4)), 21)
    mid = fbm(n, ((14, .5), (28, .5)), 22)
    fine = fbm(n, ((96, .5), (192, .5)), 23)
    swirl = fbm(n, ((5, .5), (60, .5)), 24)
    shade = 1 + .14 * (low - .5) + .07 * (mid - .5) + .05 * (fine - .5) + .04 * (swirl - .5)
    col = np.array((.86, .70, .47)) * shade[..., None]
    peb = fine > .9
    col[peb] *= .82
    col[fine < .07] *= 1.08
    return save(name, col)


def make_grass(name="T2_Grass", n=512):
    a = fbm(n, ((3, .6), (8, .4)), 31)
    b = fbm(n, ((5, .5), (16, .5)), 32)
    fine = fbm(n, ((80, .5), (160, .5)), 33)
    grass = np.array((.50, .54, .22))
    dry = np.array((.74, .64, .33))
    dirt = np.array((.64, .48, .32))
    m1 = smooth(a, .35, .65)[..., None]
    m2 = smooth(b, .7, .8)[..., None]
    col = grass * (1 - m1) + dry * m1
    col = col * (1 - m2) + dirt * m2
    col *= (.9 + .2 * fine)[..., None]
    return save(name, col)


def make_roof(name="T2_Roof", n=256):
    yy, xx = np.mgrid[0:n, 0:n] / n
    rows, cols = 6, 5
    fv = (yy * rows) % 1
    u = (xx + (np.floor(yy * rows) % 2) * .5 / cols) % 1
    fu = (u * cols) % 1
    bump = np.sin(fu * np.pi)
    fine = fbm(n, ((32, .5), (64, .5)), 41)
    shade = .65 + .4 * bump * (.6 + .4 * fv) + .05 * (fine - .5)
    shade *= 1 - .35 * (1 - smooth(fv, 0, .12))
    col = np.array((.80, .38, .22)) * shade[..., None]
    return save(name, col)


# ---------------------------------------------------------------- знамя

BANNER_POINT = .22   # доля высоты, которую занимает «хвост» знамени (треугольник снизу)


def banner_sdf(xx, yy):
    """Расстояние внутрь от края пятиугольного знамени (0..1 по ширине/высоте; y=1 сверху)."""
    d = np.minimum(xx, 1 - xx)
    d = np.minimum(d, 1 - yy)
    # Нижний треугольник: линии от (0, P) к (0.5, 0) и от (1, P) к (0.5, 0)
    p = BANNER_POINT
    nl = np.array((p, .5))
    nl /= np.linalg.norm(nl)
    left = (xx * nl[0] + yy * nl[1]) - (.5 * nl[0])          # >0 над левой линией
    right = ((1 - xx) * nl[0] + yy * nl[1]) - (.5 * nl[0])
    return np.minimum(d, np.minimum(left, right))


def _ellipse(xx, yy, px, py, ang, a, b):
    dx, dy = xx - px, yy - py
    ca, sa = np.cos(ang), np.sin(ang)
    u = dx * ca + dy * sa
    v = -dx * sa + dy * ca
    return 1 - smooth((u / a) ** 2 + (v / b) ** 2, .75, 1.0)


def laurel_mask(w, h, cx, cy, rad, leaf_len, leaf_w):
    """Лавровый венок: две дуги (открыт сверху), листья парами наружу/внутрь, стебли перекрещены внизу.
    y растёт вверх (как в картинках Blender)."""
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    m = np.zeros((h, w))
    for side in (-1, 1):
        # Стебель-дуга
        ts = np.radians(np.linspace(-80, 75, 60))
        for t in ts:
            px = cx + side * rad * np.cos(t)
            py = cy + rad * np.sin(t)
            m = np.maximum(m, _ellipse(xx, yy, px, py, 0, leaf_w * .35, leaf_w * .35))
        # Листья: по 7 пар вдоль дуги, направлены вверх по дуге и чуть наружу/внутрь
        for i in range(7):
            t = np.radians(-65 + i * 21)
            px = cx + side * rad * np.cos(t)
            py = cy + rad * np.sin(t)
            tang = np.arctan2(np.cos(t), -side * np.sin(t))   # направление роста дуги (вверх)
            for k, off in ((1, .55), (-1, .55)):
                ang = tang + k * off * side * -1
                ox = np.cos(ang) * leaf_len * .8
                oy = np.sin(ang) * leaf_len * .8
                m = np.maximum(m, _ellipse(xx, yy, px + ox, py + oy, ang, leaf_len, leaf_w))
        # Верхний лист на конце ветки
        t = np.radians(80)
        px = cx + side * rad * np.cos(t)
        py = cy + rad * np.sin(t)
        tang = np.arctan2(np.cos(t), -side * np.sin(t))
        m = np.maximum(m, _ellipse(xx, yy, px + np.cos(tang) * leaf_len * .8, py + np.sin(tang) * leaf_len * .8,
                                   tang, leaf_len, leaf_w))
    # Перекрещённые стебли внизу
    for side in (-1, 1):
        m = np.maximum(m, _ellipse(xx, yy, cx + side * rad * .18, cy - rad * 1.08, side * .7, leaf_len * .9, leaf_w * .3))
    return m


def make_banner(name="T2_Banner", w=256, h=512):
    yy, xx = np.mgrid[0:h, 0:w]
    u, v = xx / (w - 1), yy / (h - 1)
    sd = banner_sdf(u, v)
    low = fbm(w, ((3, .6), (6, .4)), 51, h=h)
    folds = .5 + .5 * np.sin(u * np.pi * 5 + low * 2)
    red = np.array((.66, .10, .09))
    shade = .86 + .14 * folds * smooth(v, .6, 1.0) + .08 * (low - .5)
    col = red * shade[..., None]
    gold = np.array((.98, .72, .24))
    border = smooth(sd, .0, .005) * (1 - smooth(sd, .045, .055))
    col = col * (1 - border[..., None]) + gold * (.85 + .2 * low[..., None]) * border[..., None]
    lm = laurel_mask(w, h, w * .5, h * .55, w * .27, w * .07, w * .028)
    col = col * (1 - lm[..., None]) + gold * (.9 + .15 * low[..., None]) * lm[..., None]
    col[sd < 0] = red * .5
    return save(name, col)


# ---------------------------------------------------------------- запись

def save(name, rgb):
    os.makedirs(TEX_DIR, exist_ok=True)
    h, w = rgb.shape[:2]
    rgba = np.concatenate([np.clip(rgb, 0, 1), np.ones((h, w, 1))], -1).astype(np.float32)
    img = bpy.data.images.get(name)
    if img is None or img.size[0] != w or img.size[1] != h:
        if img:
            bpy.data.images.remove(img)
        img = bpy.data.images.new(name, w, h, alpha=False)
    img.pixels.foreach_set(rgba.ravel())
    path = os.path.join(TEX_DIR, name + ".png")
    img.filepath_raw = path
    img.file_format = 'PNG'
    img.save()
    return img


def make_all_textures():
    out = []
    out.append(block_atlas("T2_Stone", (.93, .80, .62), 100, worn=.35))
    out.append(block_atlas("T2_Paving", (.83, .72, .57), 200, worn=.45, moss=.25))
    out.append(make_brick())
    out.append(make_plaster())
    out.append(make_sand())
    out.append(make_grass())
    out.append(make_roof())
    out.append(make_banner())
    return [i.name for i in out]
