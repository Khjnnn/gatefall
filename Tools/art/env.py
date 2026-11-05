"""Battlefield backgrounds for the six 종현동 operations and the district map.

Geometry is copied from Assets/Scripts/Core/DefenseSimulation.cs and RaonCombat.cs
(three layouts, stage index % 3). If those coordinates change, regenerate.
Environment tier: no ink outlines, mid value, low saturation. Actors pop on top.
"""
import math

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

from artlib import hexc, shade, mix, noise, rng

PPU = 54                 # pixels per world unit in the shipped texture
HX, HY = 24, 12          # drawn half extents (field is 18 x 10, margins for wide phones)
S2 = 2                   # supersample for environment

ROADS = [
    [[(-17, 0), (-11, 0), (-7, 3), (-3, 3), (0, 0)], [(0, 9), (-7, 7), (-7, 3), (-3, 3), (0, 0)], [(17, 0), (11, 0), (7, -3), (3, -3), (0, 0)]],
    [[(-17, -2), (-11, -2), (-7, -5), (-3, -5), (0, 0)], [(0, 9), (-5, 7), (-5, 3), (-2, 3), (0, 0)], [(17, 3), (11, 3), (7, 5), (3, 5), (0, 0)]],
    [[(-17, 0), (-10, 0), (-6, 5), (-2, 5), (0, 0)], [(0, 9), (5, 7), (5, 3), (2, 3), (0, 0)], [(17, 0), (10, 0), (6, -5), (2, -5), (0, 0)]],
]
SOCKETS = [
    [(0, -3.9), (-11, 2.5), (-4, .5), (-4, -4.5), (4, 1), (-6, 5.4), (7, -5.5), (11, -2.5)],
    [(0, -3.9), (-11, .5), (-7, -2.7), (3, 2), (7, 2.7), (11, .5), (-1, 5), (-4, -6.6)],
    [(0, -3.9), (-10, 2.5), (-6, 2.5), (-2, 2.4), (2, 5.5), (6, .5), (10, -2.5), (2, -2.5)],
]
STAGES = [  # (sockets, open lanes, theme)
    (5, [0], 'crossroads'), (6, [0, 2], 'underpass'), (6, [0, 1], 'ration'),
    (8, [0, 1, 2], 'checkpoint'), (8, [0, 2], 'outskirts'), (8, [0, 1, 2], 'plaza'),
]

PAVE = hexc('8C8981')
PAVE_DARK = hexc('7A776F')
ASPHALT = hexc('4F555E')
CURB = hexc('A9A69D')
PLAZA = hexc('A49F93')


class SortedDraw(ImageDraw.ImageDraw):
    """World y points up, image y points down: accept rectangle corners in any order."""

    def rectangle(self, xy, *args, **kwargs):
        x0, y0, x1, y1 = xy
        return super().rectangle([min(x0, x1), min(y0, y1), max(x0, x1), max(y0, y1)], *args, **kwargs)


class Env:
    def __init__(self, seed):
        self.W, self.H = HX * 2 * PPU * S2, HY * 2 * PPU * S2
        self.img = Image.new('RGBA', (self.W, self.H), PAVE)
        self.r = rng(seed)
        self.seed = seed

    def p(self, x, y):
        return ((x + HX) * PPU * S2, (HY - y) * PPU * S2)

    def u(self, v):
        return v * PPU * S2

    def draw(self):
        return SortedDraw(self.img, 'RGBA')

    def texture(self, color, strength, scale, seed, mask=None):
        n = noise(self.W // 4, self.H // 4, scale, seed)
        n = np.asarray(Image.fromarray((n * 255).astype(np.uint8)).resize((self.W, self.H), Image.BICUBIC), np.float32) / 255
        base = np.asarray(self.img, np.float32)
        delta = (n - 0.5)[..., None] * strength
        out = base.copy()
        out[..., :3] = np.clip(base[..., :3] * (1 + delta), 0, 255)
        if mask is not None:
            m = np.asarray(mask, np.float32)[..., None] / 255
            out = base * (1 - m) + out * m
        self.img = Image.fromarray(out.astype(np.uint8), 'RGBA')

    def polyline_mask(self, pts, width):
        m = Image.new('L', (self.W, self.H), 0)
        d = ImageDraw.Draw(m)
        d.line([self.p(*q) for q in pts], fill=255, width=int(self.u(width)), joint='curve')
        for q in pts:
            x, y = self.p(*q)
            r = self.u(width) / 2
            d.ellipse([x - r, y - r, x + r, y + r], fill=255)
        return m

    def paste(self, mask, color):
        layer = Image.new('RGBA', (self.W, self.H), color[:3] + (255,))
        a = mask if len(color) < 4 or color[3] == 255 else mask.point(lambda v: v * color[3] // 255)
        self.img.paste(layer, (0, 0), a)

    def shadow(self, mask, dx=0.35, dy=-0.28, alpha=70, blur=0.18):
        moved = Image.new('L', mask.size, 0)
        moved.paste(mask, (int(self.u(dx)), int(self.u(-dy))))
        moved = moved.filter(ImageFilter.GaussianBlur(self.u(blur)))
        self.paste(moved, (20, 24, 34, alpha))


def extend(road):
    """Continue the gate end of a road off the visible field."""
    (x0, y0), (x1, y1) = road[0], road[1]
    dx, dy = x0 - x1, y0 - y1
    length = math.hypot(dx, dy) or 1
    return [(x0 + dx / length * 9, y0 + dy / length * 9)] + road


def paving(env):
    d = env.draw()
    # Large paving slabs - faint seams every world unit, staggered rows.
    for row in range(-HY, HY):
        y = env.p(0, row)[1]
        d.line([(0, y), (env.W, y)], fill=(70, 68, 62, 24), width=2)
        off = 0.5 if row % 2 else 0
        for col in range(-HX, HX + 1):
            x = env.p(col + off, 0)[0]
            d.line([(x, y), (x, y + env.u(1))], fill=(70, 68, 62, 18), width=2)
    env.texture(PAVE, 0.16, 120, env.seed)
    # Worn patches of older paving.
    for k in range(26):
        x, y = env.r.uniform(-20, 20), env.r.uniform(-10, 10)
        m = Image.new('L', (env.W, env.H), 0)
        cx, cy = env.p(x, y)
        rx, ry = env.u(env.r.uniform(1.2, 3.2)), env.u(env.r.uniform(0.8, 2.0))
        ImageDraw.Draw(m).ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=255)
        m = m.filter(ImageFilter.GaussianBlur(env.u(0.6)))
        env.paste(m, PAVE_DARK[:3] + (60,))


def roads(env, layout, open_lanes):
    paths = [extend(r) for r in ROADS[layout]]
    curb = Image.new('L', (env.W, env.H), 0)
    asphalt = Image.new('L', (env.W, env.H), 0)
    for path in paths:
        curb.paste(255, (0, 0), env.polyline_mask(path, 3.05))
        asphalt.paste(255, (0, 0), env.polyline_mask(path, 2.6))
    env.paste(curb, CURB)
    env.paste(asphalt, ASPHALT)
    env.texture(ASPHALT, 0.22, 60, env.seed + 3, asphalt)
    d = env.draw()
    for lane, path in enumerate(paths):
        # Dashed, faded centre line.
        for i in range(1, len(path) - 1):
            (x0, y0), (x1, y1) = path[i], path[i + 1]
            seg = math.hypot(x1 - x0, y1 - y0)
            n = int(seg / 1.1)
            for k in range(n):
                t0, t1 = (k + 0.2) / n, (k + 0.55) / n
                if seg * t1 > seg - 1.8 and i == len(path) - 2:
                    continue
                d.line([env.p(x0 + (x1 - x0) * t0, y0 + (y1 - y0) * t0), env.p(x0 + (x1 - x0) * t1, y0 + (y1 - y0) * t1)], fill=(196, 176, 104, 120), width=int(env.u(0.09)))
        # Crosswalk near the first bend inside the field.
        (x0, y0), (x1, y1) = path[1], path[2]
        seg = math.hypot(x1 - x0, y1 - y0) or 1
        ux, uy = (x1 - x0) / seg, (y1 - y0) / seg
        nx, ny = -uy, ux
        cx, cy = x0 + ux * 2.2, y0 + uy * 2.2
        for k in range(-5, 6):
            px, py = cx + nx * k * 0.22, cy + ny * k * 0.22
            d.line([env.p(px - ux * 0.45, py - uy * 0.45), env.p(px + ux * 0.45, py + uy * 0.45)], fill=(214, 210, 198, 120), width=int(env.u(0.11)))
        # Manholes and cracks.
        for k in range(2):
            i = env.r.randrange(1, len(path) - 1)
            (ax, ay), (bx, by) = path[i], path[i + 1]
            t = env.r.uniform(0.25, 0.75)
            mx, my = ax + (bx - ax) * t + env.r.uniform(-.5, .5), ay + (by - ay) * t + env.r.uniform(-.5, .5)
            x, y = env.p(mx, my)
            r = env.u(0.28)
            d.ellipse([x - r, y - r * 0.8, x + r, y + r * 0.8], fill=(58, 62, 68, 255), outline=(92, 96, 100, 255), width=3)
    return paths


def crack(env, x, y, length, color=(52, 48, 50, 150), seed=0, glow=None):
    r = rng(seed)
    pts = [(x, y)]
    a = r.uniform(0, math.tau)
    for k in range(6):
        a += r.uniform(-0.8, 0.8)
        x += math.cos(a) * length / 6
        y += math.sin(a) * length / 6
        pts.append((x, y))
    d = env.draw()
    if glow:
        d.line([env.p(*q) for q in pts], fill=glow[:3] + (min(glow[3], 40),), width=int(env.u(0.1)), joint='curve')
    d.line([env.p(*q) for q in pts], fill=color, width=int(env.u(0.06)), joint='curve')


def plaza(env):
    d = env.draw()
    cx, cy = env.p(0, 0)
    for r, col in ((3.6, CURB), (3.45, PLAZA)):
        R = env.u(r)
        d.ellipse([cx - R, cy - R * 0.92, cx + R, cy + R * 0.92], fill=col)
    for r in (2.7, 1.9):
        R = env.u(r)
        d.ellipse([cx - R, cy - R * 0.92, cx + R, cy + R * 0.92], outline=(130, 124, 112, 200), width=int(env.u(0.06)))
    for k in range(16):
        a = math.tau * k / 16
        d.line([(cx + math.cos(a) * env.u(1.9), cy + math.sin(a) * env.u(1.9) * 0.92), (cx + math.cos(a) * env.u(3.4), cy + math.sin(a) * env.u(3.4) * 0.92)], fill=(130, 124, 112, 130), width=3)


def building(env, x0, y0, x1, y1, facade, seed):
    """Roof rectangle (x0..x1, y1 top .. y0) with a south facade of height `facade` below y0."""
    if x1 - x0 < 0.9 or y1 - y0 < 0.5:
        return
    r = rng(seed)
    roof_cols = [hexc('6F7479'), hexc('7D7169'), hexc('66707A'), hexc('857F72'), hexc('5F6A66')]
    roof = r.choice(roof_cols)
    wall = shade(roof, 0.62)
    d = env.draw()
    # Cast shadow (south-east).
    m = Image.new('L', (env.W, env.H), 0)
    SortedDraw(m).rectangle([*env.p(x0, y1), *env.p(x1, y0 - facade)], fill=255)
    env.shadow(m, 0.45, -0.35, 80, 0.25)
    d = env.draw()
    d.rectangle([*env.p(x0, y0), *env.p(x1, y0 - facade)], fill=wall)
    # Windows: a few warm lit, most dark glass.
    floors = max(1, int(facade / 0.55))
    for f in range(floors):
        wy = y0 - 0.18 - f * 0.55
        for wx in np.arange(x0 + 0.35, x1 - 0.3, 0.62):
            lit = r.random() < 0.12
            col = (236, 196, 120, 255) if lit else (52, 60, 70, 255)
            d.rectangle([*env.p(wx, wy), *env.p(wx + 0.3, wy - 0.26)], fill=col)
    # Shop sign band on the ground floor (no text, muted colour).
    if facade > 0.9 and r.random() < 0.8:
        sign = r.choice([hexc('7C5A52'), hexc('4E6E6C'), hexc('8A7448'), hexc('5A5E7A')])
        sx0 = x0 + r.uniform(0.2, 0.8)
        sx1 = min(x1 - 0.2, sx0 + r.uniform(1.4, 2.6))
        d.rectangle([*env.p(sx0, y0 - facade + 0.62), *env.p(sx1, y0 - facade + 0.38)], fill=sign)
        d.rectangle([*env.p(sx0, y0 - facade + 0.36), *env.p(sx1, y0 - facade)], fill=(42, 46, 54, 255))
    d.rectangle([*env.p(x0, y1), *env.p(x1, y0)], fill=roof)
    d.rectangle([*env.p(x0, y1), *env.p(x1, y0)], outline=shade(roof, 1.18), width=int(env.u(0.07)))
    # Rooftop clutter: AC units, water tank, vents.
    for k in range(r.randint(1, 3)):
        ax = r.uniform(x0 + 0.4, x1 - 1.0)
        ay = r.uniform(y0 + 0.4, y1 - 0.6)
        kind = r.random()
        if kind < 0.5:
            d.rectangle([*env.p(ax, ay + 0.5), *env.p(ax + 0.7, ay)], fill=hexc('A3A6A6'))
            d.rectangle([*env.p(ax, ay), *env.p(ax + 0.7, ay - 0.15)], fill=hexc('6F7273'))
            cx, cy = env.p(ax + 0.35, ay + 0.25)
            d.ellipse([cx - env.u(.18), cy - env.u(.18), cx + env.u(.18), cy + env.u(.18)], fill=hexc('5C5F61'))
        elif kind < 0.75:
            cx, cy = env.p(ax + 0.4, ay + 0.2)
            R = env.u(0.42)
            d.ellipse([cx - R, cy - R * 0.7 + env.u(.25), cx + R, cy + R * 0.7 + env.u(.25)], fill=hexc('5D6A73'))
            d.ellipse([cx - R, cy - R * 0.7, cx + R, cy + R * 0.7], fill=hexc('8C9AA3'))
        else:
            d.rectangle([*env.p(ax, ay + 0.3), *env.p(ax + 1.1, ay - 0.1)], fill=hexc('3E4F63'))
            for k2 in range(4):
                d.line([env.p(ax + 0.27 * k2 + 0.1, ay + 0.3), env.p(ax + 0.27 * k2 + 0.1, ay - 0.1)], fill=hexc('5D7290'), width=2)


def near_road(paths, x, y, margin):
    for path in paths:
        for i in range(len(path) - 1):
            (ax, ay), (bx, by) = path[i], path[i + 1]
            dx, dy = bx - ax, by - ay
            L = dx * dx + dy * dy or 1
            t = max(0, min(1, ((x - ax) * dx + (y - ay) * dy) / L))
            if math.hypot(ax + dx * t - x, ay + dy * t - y) < margin:
                return True
    return False


def city_edges(env, paths, seed):
    """Buildings frame the field on all four sides; gaps where roads leave."""
    r = rng(seed)
    bands = [('top', 9.1, 12.5), ('bottom', -12.5, -9.3)]
    for name, ylo, yhi in bands:
        x = -HX
        while x < HX:
            w = r.uniform(2.2, 4.4)
            x1 = min(HX, x + w)
            cx = (x + x1) / 2
            in_tower = name == 'top' and x1 > 10.5 and x < 20
            if not in_tower and not any(near_road(paths, sx, (ylo + yhi) / 2, 1.9) for sx in np.arange(x, x1, 0.3)):
                if name == 'top':
                    building(env, x + 0.1, ylo + 1.15, x1 - 0.1, yhi, r.uniform(0.9, 1.25), r.randrange(10 ** 6))
                else:
                    building(env, x + 0.1, ylo + 1.0, x1 - 0.1, yhi, r.uniform(0.6, 1.0), r.randrange(10 ** 6))
            x = x1 + r.uniform(0.15, 0.5)
    for side in (-1, 1):
        y = -HY + 2.4
        while y < HY - 2.4:
            h = r.uniform(2.0, 3.6)
            y1 = min(HY - 2.4, y + h)
            xs = (18.9, HX) if side > 0 else (-HX, -18.9)
            in_tower = side > 0 and y1 > 3.5
            if not in_tower and y1 - y > 1.6 and not any(near_road(paths, sx, sy, 1.8) for sx in np.arange(xs[0], xs[1], 0.4) for sy in np.arange(y, y1, 0.4)):
                building(env, xs[0] + 0.1, y + 0.9, xs[1] - 0.1, y1, 0.8, r.randrange(10 ** 6))
            y = y1 + r.uniform(0.2, 0.6)


def tree(env, x, y, size):
    d = env.draw()
    cx, cy = env.p(x, y)
    m = Image.new('L', (env.W, env.H), 0)
    R = env.u(size)
    ImageDraw.Draw(m).ellipse([cx - R, cy - R, cx + R, cy + R], fill=255)
    env.shadow(m, 0.3, -0.25, 70, 0.2)
    d = env.draw()
    d.ellipse([cx - env.u(size + .12), cy - env.u(size + .12) + env.u(.2), cx + env.u(size + .12), cy + env.u(size + .12) + env.u(.2)], fill=hexc('8E8A80'))
    d.ellipse([cx - R, cy - R, cx + R, cy + R], fill=hexc('4F6A4B'))
    d.ellipse([cx - R * 0.8, cy - R * 0.85, cx + R * 0.35, cy + R * 0.2], fill=hexc('62805B'))
    d.ellipse([cx - R * 0.55, cy - R * 0.7, cx - R * 0.05, cy - R * 0.25], fill=hexc('789A6E'))


def barrier_block(env, x, y, angle, color=hexc('C9C4B8')):
    """Concrete jersey barrier seen from above; long axis along `angle`."""
    d = env.draw()
    ux, uy = math.cos(angle), math.sin(angle)
    nx, ny = -uy, ux
    L, Wd = 0.75, 0.32
    pts = [(x - ux * L - nx * Wd, y - uy * L - ny * Wd), (x + ux * L - nx * Wd, y + uy * L - ny * Wd),
           (x + ux * L + nx * Wd, y + uy * L + ny * Wd), (x - ux * L + nx * Wd, y - uy * L + ny * Wd)]
    m = Image.new('L', (env.W, env.H), 0)
    ImageDraw.Draw(m).polygon([env.p(*q) for q in pts], fill=255)
    env.shadow(m, 0.18, -0.16, 90, 0.08)
    d = env.draw()
    d.polygon([env.p(*q) for q in pts], fill=color)
    inner = [(x - ux * L * 0.85 - nx * Wd * 0.4, y - uy * L * 0.85 - ny * Wd * 0.4), (x + ux * L * 0.85 - nx * Wd * 0.4, y + uy * L * 0.85 - ny * Wd * 0.4),
             (x + ux * L * 0.85 + nx * Wd * 0.4, y + uy * L * 0.85 + ny * Wd * 0.4), (x - ux * L * 0.85 + nx * Wd * 0.4, y - uy * L * 0.85 + ny * Wd * 0.4)]
    d.polygon([env.p(*q) for q in inner], fill=shade(color, 1.1))
    for k in (-0.45, 0, 0.45):
        a = (x + ux * k - nx * Wd * 0.4, y + uy * k - ny * Wd * 0.4)
        b = (x + ux * (k + 0.2) + nx * Wd * 0.4, y + uy * (k + 0.2) + ny * Wd * 0.4)
        d.line([env.p(*a), env.p(*b)], fill=hexc('D9A23A'), width=int(env.u(0.1)))


def close_lane(env, path):
    """Defence-corps barricade across a sealed entrance (environmental story, not a mechanic)."""
    (x0, y0), (x1, y1) = path[1], path[2]  # gate -> first waypoint (after extension index shift)
    seg = math.hypot(x1 - x0, y1 - y0) or 1
    ux, uy = (x1 - x0) / seg, (y1 - y0) / seg
    nx, ny = -uy, ux
    cx, cy = x0 + ux * 1.6, y0 + uy * 1.6
    ang = math.atan2(ny, nx)
    for k in (-1, 0, 1):
        barrier_block(env, cx + nx * k * 1.05, cy + ny * k * 1.05, ang)
    # Sandbags behind.
    d = env.draw()
    for k in range(-4, 5):
        px, py = cx + ux * 0.9 + nx * k * 0.33, cy + uy * 0.9 + ny * k * 0.33
        x, y = env.p(px, py)
        R = env.u(0.2)
        d.ellipse([x - R, y - R * 0.7, x + R, y + R * 0.7], fill=hexc('9C8C6A'), outline=hexc('7C6E52'), width=2)


def black_tower(env, scale, ash):
    """The Ash Tower rises at the north-east corner; only its base and lower shaft are on screen."""
    bx, by = 15.0, 6.6
    w = 3.0 * scale
    d = env.draw()
    # Ash spread and crystal growths on the ground.
    m = Image.new('L', (env.W, env.H), 0)
    cx, cy = env.p(bx, by)
    R = env.u(3.6 * ash)
    ImageDraw.Draw(m).ellipse([cx - R, cy - R * 0.6, cx + R, cy + R * 0.6], fill=255)
    m = m.filter(ImageFilter.GaussianBlur(env.u(1.0)))
    env.paste(m, (60, 56, 62, 150))
    for k in range(int(8 * ash)):
        a = k / (8 * ash) * math.tau
        crack(env, bx + math.cos(a) * 1.4, by + math.sin(a) * 0.8, 2.4 * ash, (44, 30, 44, 200), seed=k + 40, glow=(224, 64, 106, 70))
    d = env.draw()
    shaft = [env.p(bx - w, by), env.p(bx - w * 0.55, by + 14), env.p(bx + w * 0.55, by + 14), env.p(bx + w, by)]
    m = Image.new('L', (env.W, env.H), 0)
    ImageDraw.Draw(m).polygon(shaft, fill=255)
    env.shadow(m, 1.2, -0.7, 120, 0.6)
    d = env.draw()
    d.polygon(shaft, fill=hexc('211C27'))
    d.polygon([env.p(bx - w, by), env.p(bx - w * 0.55, by + 14), env.p(bx - w * 0.1, by + 14), env.p(bx - w * 0.25, by)], fill=hexc('3A3244'))
    for k in range(5):
        y0 = by + 1.2 + k * 2.3
        d.line([env.p(bx + w * 0.1 - k * 0.05, y0), env.p(bx + w * 0.35, y0 + 0.9), env.p(bx + w * 0.05, y0 + 1.8)], fill=(224, 64, 106, 200), width=int(env.u(0.08)))
    for k in range(7):
        a = env.r.uniform(-1.2, 1.2)
        gx = bx + math.sin(a) * w * 1.2 + env.r.uniform(-.5, .5)
        gy = by - 0.3 + env.r.uniform(-.6, .3)
        h = env.r.uniform(0.6, 1.6)
        d.polygon([env.p(gx - 0.25, gy), env.p(gx, gy + h), env.p(gx + 0.25, gy)], fill=hexc('2A2433'))
        d.polygon([env.p(gx - 0.08, gy), env.p(gx, gy + h), env.p(gx + 0.05, gy)], fill=hexc('5C4F6C'))


def scatter(env, paths, sockets, count, fn, seed, avoid=1.9, area=(-17, 17, -8.6, 8.4)):
    r = rng(seed)
    placed = []
    tries = 0
    while len(placed) < count and tries < count * 80:
        tries += 1
        x, y = r.uniform(area[0], area[1]), r.uniform(area[2], area[3])
        if near_road(paths, x, y, avoid) or math.hypot(x, y) < 4.6:
            continue
        if any(math.hypot(x - sx, y - sy) < 2.0 for sx, sy in sockets):
            continue
        if any(math.hypot(x - px, y - py) < 1.6 for px, py in placed):
            continue
        if x > 11 and y > 4:   # tower corner
            continue
        placed.append((x, y))
        fn(x, y, r)
    return placed


def decor(env, paths, sockets, theme):
    def planter(x, y, r):
        tree(env, x, y, r.uniform(0.55, 0.8))

    def debris(x, y, r):
        d = env.draw()
        for k in range(r.randint(3, 6)):
            px, py = x + r.uniform(-.5, .5), y + r.uniform(-.4, .4)
            s = r.uniform(.08, .2)
            d.polygon([env.p(px - s, py), env.p(px, py + s * .8), env.p(px + s, py - s * .2)], fill=r.choice([hexc('77736B'), hexc('5F5B55'), hexc('9A958A')]))

    def lamp(x, y, r):
        d = env.draw()
        m = Image.new('L', (env.W, env.H), 0)
        ImageDraw.Draw(m).line([env.p(x, y), env.p(x + .9, y + .2)], fill=255, width=int(env.u(.1)))
        env.shadow(m, 0.25, -0.2, 70, 0.05)
        d = env.draw()
        d.line([env.p(x, y), env.p(x + .9, y + .2)], fill=hexc('3E444C'), width=int(env.u(.1)))
        cx, cy = env.p(x + .9, y + .2)
        d.ellipse([cx - env.u(.16), cy - env.u(.1), cx + env.u(.16), cy + env.u(.1)], fill=hexc('D9CFA8'))

    scatter(env, paths, sockets, 7, planter, 11)
    scatter(env, paths, sockets, 10, debris, 12, avoid=1.6)
    scatter(env, paths, sockets, 5, lamp, 13, avoid=1.7)
    d = env.draw()
    if theme == 'underpass':
        for x, y in ((-14.5, 6.6), (13.5, -6.8)):
            if near_road(paths, x, y, 2.0):
                continue
            # Subway stair entrance: canopy roof + dark opening.
            d.rectangle([*env.p(x - 1.4, y + 0.8), *env.p(x + 1.4, y - 0.9)], fill=hexc('5D6670'))
            d.rectangle([*env.p(x - 1.2, y + 0.6), *env.p(x + 1.2, y - 0.4)], fill=hexc('2B3038'))
            for k in range(5):
                d.line([env.p(x - 1.1, y + 0.45 - k * 0.2), env.p(x + 1.1, y + 0.45 - k * 0.2)], fill=hexc('4A515B'), width=3)
            d.rectangle([*env.p(x - 1.4, y - 0.9), *env.p(x + 1.4, y - 1.15)], fill=hexc('3D7A6E'))
    if theme == 'ration':
        def tent(x, y, r):
            m = Image.new('L', (env.W, env.H), 0)
            pts = [env.p(x - 1.0, y - .6), env.p(x + 1.0, y - .6), env.p(x + 1.0, y + .6), env.p(x - 1.0, y + .6)]
            ImageDraw.Draw(m).polygon(pts, fill=255)
            env.shadow(m, .3, -.25, 80, .12)
            d2 = env.draw()
            d2.polygon(pts, fill=hexc('C8C2B0'))
            d2.polygon([env.p(x - 1.0, y), env.p(x + 1.0, y), env.p(x + 1.0, y + .6), env.p(x - 1.0, y + .6)], fill=hexc('DCD6C4'))
            d2.line([env.p(x - 1.0, y), env.p(x + 1.0, y)], fill=hexc('8A7D60'), width=3)
            d2.rectangle([*env.p(x - .2, y - .4), *env.p(x + .2, y - .2)], fill=hexc('C45A4A'))
        scatter(env, paths, sockets, 4, tent, 21, avoid=2.2, area=(-15, 15, -8, 7))
    if theme in ('checkpoint', 'plaza'):
        def bags(x, y, r):
            for k in range(-3, 4):
                px, py = env.p(x + k * .32, y + abs(k) * 0.05)
                R = env.u(.2)
                d.ellipse([px - R, py - R * .7, px + R, py + R * .7], fill=hexc('9C8C6A'), outline=hexc('7C6E52'), width=2)
        scatter(env, paths, sockets, 5, bags, 31, avoid=2.0)
        def container(x, y, r):
            m = Image.new('L', (env.W, env.H), 0)
            pts = [env.p(x - 1.5, y - .55), env.p(x + 1.5, y - .55), env.p(x + 1.5, y + .55), env.p(x - 1.5, y + .55)]
            ImageDraw.Draw(m).polygon(pts, fill=255)
            env.shadow(m, .4, -.3, 90, .15)
            d2 = env.draw()
            col = r.choice([hexc('6E7F5F'), hexc('7C6A55'), hexc('5D6E7C')])
            d2.polygon(pts, fill=col)
            for k in range(10):
                xx = x - 1.4 + k * 0.3
                d2.line([env.p(xx, y - .5), env.p(xx, y + .5)], fill=shade(col, 0.85), width=3)
        scatter(env, paths, sockets, 2, container, 32, avoid=2.4, area=(-16, 16, -8, 7))
    if theme in ('outskirts', 'plaza'):
        for k in range(14 if theme == 'plaza' else 9):
            x, y = env.r.uniform(-16, 16), env.r.uniform(-8.5, 8.5)
            if math.hypot(x, y) < 4:
                continue
            crack(env, x, y, env.r.uniform(1.5, 3.2), (54, 44, 54, 170), seed=100 + k, glow=(224, 64, 106, 45))


def vignette(env):
    """Gently darken drawn margins outside the playable 36 x 20 field."""
    W, H = env.W, env.H
    xs = (np.arange(W) / (PPU * S2)) - HX
    ys = HY - (np.arange(H) / (PPU * S2))
    dx = np.clip(np.abs(xs) - 18.3, 0, None)[None, :]
    dy = np.clip(np.abs(ys) - 10.2, 0, None)[:, None]
    f = 1 - np.clip(np.maximum(dx / 5, dy / 3), 0, 1) * 0.28
    arr = np.asarray(env.img, np.float32)
    arr[..., :3] *= f[..., None]
    env.img = Image.fromarray(arr.astype(np.uint8), 'RGBA')


def stage(index):
    sockets_n, lanes, theme = STAGES[index]
    layout = index % 3
    env = Env(1000 + index * 17)
    paving(env)
    paths = roads(env, layout, lanes)
    plaza(env)
    sockets = SOCKETS[layout][:sockets_n]
    decor(env, paths, sockets, theme)
    for lane in range(3):
        if lane not in lanes:
            close_lane(env, paths[lane])
    city_edges(env, paths, 77 + index)
    black_tower(env, 1.0 + 0.18 * max(0, index - 3), 1.0 + 0.25 * max(0, index - 3))
    vignette(env)
    return env.img.resize((env.W // S2, env.H // S2), Image.LANCZOS).convert('RGB')


def district_map():
    """Operation-select map of 종현동: abstract top-down blocks, river, route, tower."""
    W, H = 1600, 1000
    img = Image.new('RGBA', (W * 2, H * 2), hexc('8C8981'))
    d = SortedDraw(img, 'RGBA')
    r = rng(5)
    # River (라온강) along the south.
    d.polygon([(0, 1500), (900, 1560), (1900, 1460), (3200, 1580), (3200, 2000), (0, 2000)], fill=hexc('5E7680'))
    d.line([(0, 1500), (900, 1560), (1900, 1460), (3200, 1580)], fill=hexc('A9A69D'), width=18)
    # Street grid.
    for x in range(-200, 3400, 380):
        d.line([(x, 0), (x + 160, 1520)], fill=hexc('5A6068'), width=46)
    for y in range(120, 1500, 330):
        d.line([(0, y), (3200, y - 90)], fill=hexc('5A6068'), width=40)
    # Blocks
    for x in range(-200, 3400, 380):
        for y in range(120, 1450, 330):
            for k in range(2):
                bx = x + 50 + r.uniform(0, 140) + k * 150
                by = y + 50 + r.uniform(0, 60)
                bw, bh = r.uniform(90, 140), r.uniform(90, 170)
                col = r.choice([hexc('6F7479'), hexc('7D7169'), hexc('66707A'), hexc('857F72')])
                d.rectangle([bx + 14, by + 14, bx + bw + 14, by + bh + 14], fill=(30, 34, 44, 60))
                d.rectangle([bx, by, bx + bw, by + bh], fill=col)
    # Ash tower area (north-east).
    tx, ty = 2760, 620
    d.ellipse([tx - 360, ty - 200, tx + 360, ty + 200], fill=(56, 50, 60, 200))
    d.polygon([(tx - 70, ty + 40), (tx - 30, ty - 420), (tx + 30, ty - 420), (tx + 70, ty + 40)], fill=hexc('211C27'))
    d.polygon([(tx - 70, ty + 40), (tx - 30, ty - 420), (tx - 5, ty - 420), (tx - 20, ty + 40)], fill=hexc('3A3244'))
    for k in range(4):
        d.line([(tx + 10, ty - 360 + k * 90), (tx + 30, ty - 320 + k * 90), (tx + 6, ty - 290 + k * 90)], fill=(224, 64, 106, 220), width=8)
    out = img.resize((W, H), Image.LANCZOS)
    return out.convert('RGB')


# Operation node positions on the district map (fractions of width/height), 1-1 .. 1-6.
MAP_NODES = [(0.10, 0.70), (0.25, 0.56), (0.40, 0.66), (0.54, 0.46), (0.68, 0.40), (0.80, 0.33)]
