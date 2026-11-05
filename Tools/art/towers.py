"""Barrier pillars (결계주), build pad and city barrier core.

Silhouette language (see Docs/Design/GATEFALL_MOBILE_REDESIGN_V05.md):
  창   tall narrow spire          봉인 low wide ring of seal stones
  붕괴 squat heavy hex mortar     방벽 horizontal wall arc
  뇌격 spire + coils              파쇄 hex mortar + spire
  중력 ring + floating orb        종결 three-pillar crown
Level 2 adds height, parts and metal bands.
"""
import math

from artlib import Sprite, hexc, shade, mix, alpha

W, H = 256, 320
CX, BY = 128, 262          # base centre (ground contact)
STONE_TOP = hexc('7F848C')
STONE_SIDE = hexc('5F646E')
IRON = hexc('3B4250')
IRON_TOP = hexc('59626F')
STEEL = hexc('7E8C9E')

SPEAR = hexc('F0B340')
SEAL = hexc('39C2B4')
BLAST = hexc('F07438')
WARD = hexc('6E9CE0')
ARC = hexc('FFD45A')
GRAV = hexc('6B5BD6')
WHITE = hexc('F4EEDC')


def plinth(s, accent, level, wide=1.0):
    """Shared octagonal stone plinth with a rune inlay in the role colour."""
    rx, ry = 72 * wide, 33 * wide
    top = s.prism(s.octagon(CX, BY, rx, ry), 16 + 5 * (level - 1), STONE_TOP, STONE_SIDE)
    ty = BY - 16 - 5 * (level - 1)
    s.ellipse(CX, ty, rx * 0.78, ry * 0.78, shade(STONE_TOP, 0.9))
    s.ring(CX, ty, rx * 0.66, ry * 0.66, 3.5, alpha(accent, 210))
    for i in range(8):
        a = math.radians(22.5 + 45 * i)
        x, y = CX + rx * 0.72 * math.cos(a), ty + ry * 0.72 * math.sin(a)
        s.ellipse(x, y, 3.2, 2, alpha(accent, 170))
    if level >= 2:
        # Lv2: bronze band around the plinth edge.
        s.ring(CX, ty, rx * 0.96, ry * 0.96, 3, hexc('B98A4B'))
    return ty


def crystal(s, cx, cy, w, h, color, glow=True):
    """Elongated diamond crystal lit from the north-west."""
    s.poly([(cx, cy - h), (cx + w, cy - h * 0.35), (cx, cy), (cx - w, cy - h * 0.35)], shade(color, 0.78))
    s.poly([(cx, cy - h), (cx - w, cy - h * 0.35), (cx, cy - h * 0.2)], shade(color, 1.18))
    s.poly([(cx, cy - h), (cx, cy - h * 0.2), (cx + w * 0.55, cy - h * 0.42)], color)
    if glow:
        s.glow(cx, cy - h * 0.45, w * 2.4, color, 0.45)


def spear(level, coils=0, base_kind='plinth'):
    s = Sprite(W, H)
    ty = plinth(s, SPEAR if not coils else ARC, level)
    height = 128 if level == 1 else 158
    w = 20 if level == 1 else 23
    # Square obelisk seen at 3/4: diamond base.
    base = [(CX - w, ty - 4), (CX, ty - 4 + w * 0.5), (CX + w, ty - 4), (CX, ty - 4 - w * 0.5)]
    top = s.prism(base, height, IRON_TOP, IRON)
    accent = SPEAR if not coils else ARC
    # Rune inlay running up the lit face.
    s.line([(CX - w * 0.5, ty - 10), (CX - w * 0.5, ty - height + 6)], 2.4, alpha(accent, 230))
    if level >= 2:
        for k in (0.35, 0.7):
            y = ty - 4 - height * k
            s.poly([(CX - w - 2, y), (CX, y + w * 0.5 + 1), (CX + w + 2, y), (CX + w + 2, y - 6), (CX, y + w * 0.5 - 5), (CX - w - 2, y - 6)], hexc('B98A4B'))
        # Side prongs make the Lv2 spire read wider and sharper.
        for side in (-1, 1):
            px = CX + side * 34
            s.prism([(px - 6, ty - 2), (px, ty + 1), (px + 6, ty - 2), (px, ty - 5)], 58, IRON_TOP, IRON)
            crystal(s, px, ty - 60, 7, 26, accent, glow=False)
    if coils:
        for k in range(coils):
            y = ty - 34 - k * 36
            s.ring(CX, y, 30 - k * 3, 11 - k, 4, hexc('2E3644'))
            s.arc(CX, y, 30 - k * 3, 11 - k, 0, 180, 3, SEAL)
        # Electric zigzag accents (gold) - no neon halo, just crisp shapes.
        for side in (-1, 1):
            pts = [(CX + side * 26, ty - 40), (CX + side * 34, ty - 58), (CX + side * 24, ty - 70), (CX + side * 33, ty - 90)]
            s.line(pts, 2.5, ARC)
    crystal(s, CX, ty - 4 - height + 2, 13 if level == 1 else 16, 48 if level == 1 else 58, accent)
    return s.finish()


def seal_stone(s, x, y, h, accent):
    base = [(x - 9, y), (x, y + 5), (x + 9, y), (x, y - 5)]
    s.prism(base, h, STONE_TOP, STONE_SIDE)
    s.line([(x - 3, y - 6), (x - 3, y - h + 6)], 2.2, alpha(accent, 220))
    s.ellipse(x, y - h, 5, 3, alpha(accent, 230))


def binding(level, orb=None):
    s = Sprite(W, H)
    accent = SEAL if orb is None else GRAV
    ty = plinth(s, accent, level, wide=1.05)
    stones = 3 if level == 1 else 4
    h = 52 if level == 1 else 64
    rx, ry = 58, 24
    pts = []
    for i in range(stones):
        a = math.radians(-90 + 360 / stones * i + (30 if stones == 3 else 45))
        pts.append((CX + rx * math.cos(a), ty - 6 + ry * math.sin(a)))
    back = [p for p in pts if p[1] <= ty - 6]
    front = [p for p in pts if p[1] > ty - 6]
    for x, y in sorted(back, key=lambda p: p[1]):
        seal_stone(s, x, y, h, accent)
    # Floating seal ring: back half, (orb), front half, so it wraps around.
    ring_y = ty - h * 0.72
    s.arc(CX, ring_y, rx + 4, ry + 3, 180, 360, 8, shade(accent, 0.7))
    if level >= 2:
        s.arc(CX, ring_y - 16, rx - 12, ry - 6, 180, 360, 4, shade(accent, 0.7))
    if orb is None:
        s.ellipse(CX, ring_y - 8, 19, 19, shade(accent, 0.8))
        s.ellipse(CX - 6, ring_y - 14, 8, 8, mix(accent, (255, 255, 255), 0.55))
        s.glow(CX, ring_y - 6, 30, accent, 0.4)
    else:
        size = 30 if level == 1 else 38
        oy = ring_y - 30
        s.ellipse(CX, ty - 8, size * 0.9, size * 0.32, (20, 18, 40, 120))
        s.ellipse_grad(CX, oy, size, size, mix(GRAV, (255, 255, 255), 0.25), shade(GRAV, 0.45))
        s.ellipse(CX + 5, oy + 4, size * 0.55, size * 0.55, hexc('1C1838'))
        s.ellipse(CX - size * 0.38, oy - size * 0.4, size * 0.22, size * 0.16, (230, 225, 255, 200))
        s.arc(CX, oy, size + 14, 10, 200, 340, 3, hexc('B8B0FF'))
        for k, (dx, dy) in enumerate([(-size - 12, 6), (size + 10, -4), (6, -size - 12)]):
            s.poly(s.ngon(CX + dx, oy + dy, 6, 5, 5, k * 20), STONE_TOP)
        s.glow(CX, oy, size * 1.6, GRAV, 0.45)
    for x, y in sorted(front, key=lambda p: p[1]):
        seal_stone(s, x, y, h, accent)
    s.arc(CX, ring_y, rx + 4, ry + 3, 0, 180, 8, accent)
    if level >= 2:
        s.arc(CX, ring_y - 16, rx - 12, ry - 6, 0, 180, 4, accent)
    return s.finish()


def mortar(level, spire=False):
    s = Sprite(W, H)
    accent = BLAST
    ty = plinth(s, accent if not spire else SPEAR, level, wide=1.08)
    r = 56 if level == 1 else 64
    h = 40 if level == 1 else 48
    hexagon = s.ngon(CX, ty - 4, r, r * 0.48, 6, 0)
    top = s.prism(hexagon, h, hexc('4E4844'), hexc('3A3533'))
    tcy = ty - 4 - h
    # Armour ribs and the glowing breach.
    for i in range(6):
        a = math.radians(60 * i)
        x, y = CX + r * 0.82 * math.cos(a), tcy + r * 0.48 * 0.82 * math.sin(a)
        s.ellipse(x, y, 4, 2.5, hexc('8A7F72'))
    if level >= 2:
        for side in (-1, 1):
            bx = CX + side * (r + 8)
            s.prism([(bx - 8, ty + 6), (bx, ty + 10), (bx + 8, ty + 6), (bx, ty + 2)], 30, hexc('5A534D'), hexc('3A3533'))
        s.ring(CX, tcy, r * 0.95, r * 0.45, 4, hexc('B98A4B'))
    s.ellipse(CX, tcy, r * 0.56, r * 0.27, hexc('231C1A'))
    s.ellipse(CX + 2, tcy + 2, r * 0.42, r * 0.19, shade(accent, 0.75))
    s.ellipse(CX + 4, tcy + 3, r * 0.24, r * 0.1, hexc('FFD9A0'))
    s.glow(CX, tcy, r * 0.8, accent, 0.5)
    # Cracks on the lit side.
    s.line([(CX - r * 0.7, tcy + 16), (CX - r * 0.55, tcy + 30), (CX - r * 0.62, tcy + 40)], 2.2, shade(accent, 0.95))
    if spire:
        height = 92 if level == 1 else 116
        w = 13 if level == 1 else 16
        base = [(CX - w, tcy - 2), (CX, tcy + w * 0.5 - 2), (CX + w, tcy - 2), (CX, tcy - w * 0.5 - 2)]
        s.prism(base, height, IRON_TOP, IRON)
        s.line([(CX - w * 0.5, tcy - 8), (CX - w * 0.5, tcy - height + 4)], 2.2, SPEAR)
        crystal(s, CX, tcy - height, 12 if level == 1 else 15, 42 if level == 1 else 52, SPEAR)
        for k, dx in enumerate((-30, 26, 38)):
            crystal(s, CX + dx, tcy + 10 - k * 4, 6, 20, BLAST, glow=False)
    return s.finish()


def wall(level):
    s = Sprite(W, H)
    ty = plinth(s, WARD, level, wide=1.1)
    segments = 5
    h = 54 if level == 1 else 68
    rx, ry = 74, 26
    # Wall arc facing south: draw back-to-front panels along the lower half ellipse.
    angles = [math.radians(200 + i * (140 / (segments - 1))) for i in range(segments)]
    panels = []
    for i in range(segments - 1):
        a0, a1 = angles[i], angles[i + 1]
        p0 = (CX + rx * math.cos(a0), ty - 6 - ry * math.sin(a0) * -1)
        p1 = (CX + rx * math.cos(a1), ty - 6 - ry * math.sin(a1) * -1)
        panels.append((p0, p1))
    for p0, p1 in panels:
        quad_base = [p0, p1, (p1[0], p1[1] - 9), (p0[0], p0[1] - 9)]
        s.prism(quad_base, h, shade(STEEL, 1.15), shade(STEEL, 0.8))
        mx, my = (p0[0] + p1[0]) / 2, (p0[1] + p1[1]) / 2
        s.line([(mx - 6, my - h * 0.35 - 6), (mx + 6, my - h * 0.35 - 6)], 2.6, alpha(WARD, 240))
        s.line([(mx, my - h * 0.2 - 6), (mx, my - h * 0.5 - 6)], 2.6, alpha(WARD, 240))
    for p in (panels[0][0], panels[-1][1]):
        x, y = p
        s.prism([(x - 8, y + 2), (x, y + 6), (x + 8, y + 2), (x, y - 2)], h + 18, IRON_TOP, IRON)
        s.ellipse(x, y - h - 18, 6, 4, WARD)
        if level >= 2:
            s.glow(x, y - h - 18, 12, WARD, 0.4)
    if level >= 2:
        for p0, p1 in panels:
            s.line([(p0[0], p0[1] - h - 9), (p1[0], p1[1] - h - 9)], 3.5, hexc('B98A4B'))
    return s.finish()


def seal_crown():
    s = Sprite(W, H)
    ty = plinth(s, WHITE, 2, wide=1.1)
    pillars = [(-46, -6), (46, -6), (0, 20)]
    h = 112
    order = sorted(pillars, key=lambda p: p[1])
    for dx, dy in order[:2]:
        x, y = CX + dx, ty + dy - 6
        s.prism([(x - 13, y), (x, y + 7), (x + 13, y), (x, y - 7)], h, hexc('E8E1D0'), hexc('A79F8D'))
        s.prism([(x - 16, y - h), (x, y - h + 8), (x + 16, y - h), (x, y - h - 8)], 10, hexc('E3B866'), hexc('9C7637'))
    oy = ty - 120
    s.glow(CX, oy, 60, hexc('FFF2C4'), 0.55)
    crystal(s, CX, oy + 40, 22, 86, hexc('FFF6DA'))
    s.arc(CX, oy + 10, 58, 16, 0, 360, 3, hexc('E3B866'))
    dx, dy = order[2]
    x, y = CX + dx, ty + dy - 6
    s.prism([(x - 13, y), (x, y + 7), (x + 13, y), (x, y - 7)], h - 20, hexc('E8E1D0'), hexc('A79F8D'))
    s.prism([(x - 16, y - h + 20), (x, y - h + 28), (x + 16, y - h + 20), (x, y - h + 12)], 10, hexc('E3B866'), hexc('9C7637'))
    return s.finish()


def pad():
    """Build pad: low concrete plinth with an empty rune socket. No outline (environment tier)."""
    s = Sprite(220, 140)
    cx, by = 110, 84
    s.ellipse(cx + 8, by + 14, 96, 38, (18, 22, 30, 70))
    top = s.prism(s.octagon(cx, by, 92, 44), 12, hexc('9A9C9E'), hexc('6A6E74'))
    ty = by - 12
    s.ellipse(cx, ty, 70, 33, hexc('85888C'))
    s.ring(cx, ty, 62, 29, 3, hexc('5E6268'))
    s.ring(cx, ty, 46, 21, 2.5, alpha(hexc('3FD6C6'), 150))
    for i in range(4):
        a = math.radians(45 + 90 * i)
        s.ellipse(cx + 78 * math.cos(a), ty + 37 * math.sin(a), 4, 2.4, hexc('5E6268'))
    return s.finish(outline=0, shadow=None)


def core():
    """Central city barrier: two-tier plinth with a crystal pylon."""
    s = Sprite(340, 400)
    cx, by = 170, 330
    cyan = hexc('3FD6C6')
    s.prism(s.octagon(cx, by, 150, 70), 22, hexc('9C9890'), hexc('66635E'))
    s.prism(s.octagon(cx, by - 26, 108, 50), 20, hexc('AEA99F'), hexc('6F6B64'))
    ty = by - 46
    s.ellipse(cx, ty, 84, 38, hexc('C2BDB2'))
    s.ring(cx, ty, 76, 34, 4, alpha(cyan, 220))
    for i in range(6):
        a = math.radians(30 + 60 * i)
        x, y = cx + 96 * math.cos(a), ty + 44 * math.sin(a)
        if y < ty:
            s.prism([(x - 7, y), (x, y + 4), (x + 7, y), (x, y - 4)], 46, hexc('5A6270'), hexc('3B4250'))
            s.ellipse(x, y - 46, 5, 3, cyan)
    # Crystal cluster
    s.glow(cx, ty - 110, 80, cyan, 0.35)
    for dx, h, w in ((-34, 96, 16), (30, 108, 18), (0, 170, 30), (-14, 70, 12), (18, 74, 13)):
        s.poly([(cx + dx, ty - h), (cx + dx + w, ty - h * 0.42), (cx + dx + w * 0.4, ty - 6), (cx + dx - w * 0.4, ty - 6), (cx + dx - w, ty - h * 0.42)], shade(cyan, 0.62))
        s.poly([(cx + dx, ty - h), (cx + dx - w, ty - h * 0.42), (cx + dx - w * 0.4, ty - 6), (cx + dx, ty - 10)], mix(cyan, (255, 255, 255), 0.25))
        s.poly([(cx + dx, ty - h), (cx + dx, ty - 10), (cx + dx + w * 0.3, ty - h * 0.45)], cyan)
    s.ring(cx, ty - 60, 64, 20, 5, hexc('5A6270'))
    s.arc(cx, ty - 60, 64, 20, 0, 180, 5, hexc('8D97A6'))
    for i in range(6):
        a = math.radians(30 + 60 * i)
        x, y = cx + 96 * math.cos(a), ty + 44 * math.sin(a)
        if y >= ty:
            s.prism([(x - 7, y), (x, y + 4), (x + 7, y), (x, y - 4)], 46, hexc('5A6270'), hexc('3B4250'))
            s.ellipse(x, y - 46, 5, 3, cyan)
    return s.finish(outline=3.0, shadow=(12, 9))


def all_towers():
    return {
        'tower-spear-1': spear(1), 'tower-spear-2': spear(2),
        'tower-binding-1': binding(1), 'tower-binding-2': binding(2),
        'tower-rupture-1': mortar(1), 'tower-rupture-2': mortar(2),
        'tower-barrier-1': wall(1), 'tower-barrier-2': wall(2),
        'tower-arc-1': spear(1, coils=2), 'tower-arc-2': spear(2, coils=3),
        'tower-shatter-1': mortar(1, spire=True), 'tower-shatter-2': mortar(2, spire=True),
        'tower-gravity-1': binding(1, orb=True), 'tower-gravity-2': binding(2, orb=True),
        'tower-seal-1': seal_crown(),
        'pad': pad(), 'core': core(),
    }
