"""UI skin (9-slice panels/buttons), 2-tone icons and small effect sprites.

No text is baked into any image. Panels are flat slate with a 1px light top edge;
the primary button is the single warm amber accent.
"""
import math

from PIL import Image, ImageDraw, ImageFilter

from artlib import Sprite, hexc, shade, mix, alpha

SLATE = hexc('19212B')
AMBER = hexc('F0B340')


def rounded(size, radius, fill, border=None, top_light=None, shadow=False):
    s = 4
    W = size * s
    img = Image.new('RGBA', (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    inset = 6 * s if shadow else 0
    if shadow:
        sh = Image.new('L', (W, W), 0)
        ImageDraw.Draw(sh).rounded_rectangle([inset, inset + 3 * s, W - inset, W - inset + 3 * s], radius * s, fill=110)
        sh = sh.filter(ImageFilter.GaussianBlur(3 * s))
        img.paste(Image.new('RGBA', (W, W), (6, 9, 14, 255)), (0, 0), sh)
    d.rounded_rectangle([inset, inset, W - 1 - inset, W - 1 - inset], radius * s, fill=fill)
    if top_light:
        m = Image.new('L', (W, W), 0)
        ImageDraw.Draw(m).rounded_rectangle([inset, inset, W - 1 - inset, W - 1 - inset], radius * s, outline=255, width=int(1.5 * s))
        cut = Image.new('L', (W, W), 0)
        ImageDraw.Draw(cut).rectangle([0, 0, W, W // 2], fill=255)
        m = Image.composite(m, Image.new('L', (W, W), 0), cut)
        img.paste(Image.new('RGBA', (W, W), top_light), (0, 0), m.point(lambda v: v * top_light[3] // 255))
    if border:
        ImageDraw.Draw(img).rounded_rectangle([inset, inset, W - 1 - inset, W - 1 - inset], radius * s, outline=border, width=int(2 * s))
    return img.resize((size, size), Image.LANCZOS)


def skins():
    return {
        'ui-panel': rounded(96, 16, alpha(SLATE, 236), top_light=(255, 255, 255, 40), shadow=True),
        'ui-panel-flat': rounded(64, 12, (30, 39, 50, 230)),
        'ui-button': rounded(64, 14, hexc('2C3846'), top_light=(255, 255, 255, 55)),
        'ui-button-primary': rounded(64, 14, AMBER, top_light=(255, 250, 230, 120)),
        'ui-button-disabled': rounded(64, 14, hexc('222A33')),
        'ui-chip': rounded(48, 24, (16, 22, 30, 200)),
        'ui-selected': rounded(64, 14, (0, 0, 0, 0), border=AMBER),
        'ui-bar': rounded(32, 8, (255, 255, 255, 255)),
    }


def icon(draw_fn, size=96):
    s = Sprite(size, size)
    draw_fn(s, size)
    return s.finish(outline=2.2, shadow=None)


def stone(s, n):   # 마석 - amber crystal
    c = hexc('F0B340')
    s.poly([(48, 8), (74, 36), (60, 86), (36, 86), (22, 36)], shade(c, 0.72))
    s.poly([(48, 8), (22, 36), (36, 86), (46, 40)], mix(c, (255, 255, 255), 0.25))
    s.poly([(48, 8), (46, 40), (74, 36)], c)


def catalyst(s, n):  # 공명 촉매 - teal hex core
    c = hexc('3FD6C6')
    s.poly(s.ngon(48, 48, 36, 36, 6, 30), shade(c, 0.6))
    s.poly(s.ngon(48, 46, 26, 26, 6, 30), c)
    s.poly(s.ngon(48, 44, 12, 12, 6, 30), mix(c, (255, 255, 255), 0.6))


def core_gem(s, n):  # 마핵 - violet-red heart crystal
    c = hexc('C04A7A')
    s.ellipse(48, 52, 32, 34, shade(c, 0.6))
    s.poly([(48, 14), (76, 44), (48, 86), (20, 44)], c)
    s.poly([(48, 14), (20, 44), (48, 50)], mix(c, (255, 255, 255), 0.35))


def star(s, n, filled=True):
    pts = []
    for i in range(10):
        r = 40 if i % 2 == 0 else 17
        a = math.radians(-90 + 36 * i)
        pts.append((48 + r * math.cos(a), 52 + r * math.sin(a)))
    s.poly(pts, hexc('F0B340') if filled else hexc('3A4552'))
    if filled:
        s.poly([pts[0], pts[9], (48, 52), pts[1]], hexc('FFD98A'))


def pause(s, n):
    s.rect(28, 22, 42, 74, hexc('E8ECEF'))
    s.rect(54, 22, 68, 74, hexc('E8ECEF'))


def play(s, n):
    s.poly([(30, 18), (78, 48), (30, 78)], hexc('1B2230'))


def sound(s, n, on=True):
    s.poly([(18, 38), (34, 38), (54, 20), (54, 76), (34, 58), (18, 58)], hexc('E8ECEF'))
    if on:
        s.arc(54, 48, 18, 18, -50, 50, 6, hexc('E8ECEF'))
        s.arc(54, 48, 30, 30, -50, 50, 6, hexc('E8ECEF'))
    else:
        s.line([(64, 34), (84, 62)], 7, hexc('E0406A'))
        s.line([(84, 34), (64, 62)], 7, hexc('E0406A'))


def upgrade(s, n):
    c = hexc('F0B340')
    for k, y in enumerate((54, 32)):
        s.poly([(20, y + 18), (48, y - 8), (76, y + 18), (64, y + 28), (48, y + 12), (32, y + 28)], c if k else shade(c, 0.8))


def fusion(s, n):
    c = hexc('3FD6C6')
    s.ellipse(32, 38, 18, 18, hexc('F0B340'))
    s.ellipse(64, 38, 18, 18, c)
    s.poly([(30, 62), (66, 62), (48, 88)], hexc('E8ECEF'))


def sell(s, n):
    s.ellipse(48, 50, 34, 34, hexc('B9C4CF'))
    s.ellipse(48, 50, 24, 24, hexc('8D99A6'))
    s.poly([(20, 22), (40, 10), (40, 34)], hexc('E8ECEF'))


def lock(s, n):
    s.arc(48, 40, 20, 22, 180, 360, 9, hexc('8D99A6'))
    s.rect(24, 40, 72, 82, hexc('8D99A6'))
    s.ellipse(48, 58, 6, 6, hexc('2C3846'))


def barrier(s, n):
    c = hexc('3FD6C6')
    s.poly([(48, 10), (80, 22), (76, 56), (48, 86), (20, 56), (16, 22)], shade(c, 0.65))
    s.poly([(48, 18), (72, 28), (69, 54), (48, 76)], c)


def retreat(s, n):
    s.rect(24, 14, 30, 86, hexc('B9C4CF'))
    s.poly([(30, 16), (74, 28), (30, 46)], hexc('E8ECEF'))


def growth(s, n):
    s.ellipse(48, 30, 16, 16, hexc('E8ECEF'))
    s.poly([(20, 84), (28, 54), (68, 54), (76, 84)], hexc('E8ECEF'))
    s.poly([(62, 44), (86, 30), (78, 56)], hexc('F0B340'))


def settings(s, n):
    s.ellipse(48, 48, 34, 34, hexc('B9C4CF'))
    for i in range(8):
        a = math.radians(45 * i)
        s.ellipse(48 + math.cos(a) * 34, 48 + math.sin(a) * 34, 9, 9, hexc('B9C4CF'))
    s.ellipse(48, 48, 14, 14, hexc('2C3846'))


def back(s, n):
    s.poly([(18, 48), (50, 18), (50, 36), (80, 36), (80, 60), (50, 60), (50, 78)], hexc('E8ECEF'))


def skill(s, n, kind):
    if kind == 0:   # 회피 - dash chevrons
        for k in range(3):
            x = 22 + k * 20
            s.poly([(x, 24), (x + 22, 48), (x, 72), (x + 10, 72), (x + 32, 48), (x + 10, 24)], mix(hexc('3FD6C6'), (255, 255, 255), 0.2 * k))
    elif kind == 1:  # 질주 - wind lines
        for k, y in enumerate((30, 48, 66)):
            s.line([(16 + k * 8, y), (78, y)], 7, hexc('9BE3A6'))
        s.poly([(70, 20), (88, 48), (70, 76)], hexc('E8ECEF'))
    else:            # 방호 - shield
        s.poly([(48, 12), (80, 24), (76, 58), (48, 86), (20, 58), (16, 24)], hexc('6E9CE0'))
        s.poly([(48, 22), (70, 30), (67, 56), (48, 76)], hexc('B6CCF2'))


def hand(s, n):
    """Tutorial pointer: a bold downward arrow."""
    s.poly([(30, 8), (66, 8), (66, 44), (84, 44), (48, 88), (12, 44), (30, 44)], hexc('F0B340'))
    s.poly([(30, 8), (48, 8), (48, 80), (12, 44), (30, 44)], hexc('FFD98A'))


def icons():
    out = {}
    for name, fn in [('icon-stone', stone), ('icon-catalyst', catalyst), ('icon-core', core_gem), ('icon-pause', pause), ('icon-play', play),
                     ('icon-upgrade', upgrade), ('icon-fusion', fusion), ('icon-sell', sell), ('icon-lock', lock), ('icon-barrier', barrier),
                     ('icon-retreat', retreat), ('icon-growth', growth), ('icon-settings', settings), ('icon-back', back), ('icon-hand', hand)]:
        out[name] = icon(fn)
    out['icon-star'] = icon(lambda s, n: star(s, n, True))
    out['icon-star-empty'] = icon(lambda s, n: star(s, n, False))
    out['icon-sound-on'] = icon(lambda s, n: sound(s, n, True))
    out['icon-sound-off'] = icon(lambda s, n: sound(s, n, False))
    for k in range(3):
        out['icon-skill-%d' % k] = icon(lambda s, n, k=k: skill(s, n, k))
    return out


def fx():
    """Effect sprites: white/greyscale where Unity tints them, coloured where identity matters."""
    out = {}
    s = Sprite(64, 64)
    s.ellipse(32, 32, 30, 30, (255, 255, 255, 255))
    out['fx-disc'] = s.finish(outline=0, shadow=None)
    s = Sprite(128, 128)
    s.ring(64, 64, 60, 60, 5, (255, 255, 255, 255))
    out['fx-ring'] = s.finish(outline=0, shadow=None)
    s = Sprite(128, 128)
    for r, a in ((60, 60), (48, 110), (36, 200)):
        s.ring(64, 64, r, r, 3, (255, 255, 255, a))
    out['fx-ring-soft'] = s.finish(outline=0, shadow=None)
    # Spear bolt: long amber blade, pointing right.
    s = Sprite(96, 24)
    s.poly([(2, 12), (70, 6), (94, 12), (70, 18)], hexc('F0B340'))
    s.poly([(30, 12), (70, 9), (90, 12), (70, 15)], hexc('FFF0C2'))
    out['fx-bolt'] = s.finish(outline=0, shadow=None)
    # Hunter round: short cyan tracer.
    s = Sprite(48, 16)
    s.poly([(2, 8), (34, 5), (46, 8), (34, 11)], hexc('3FD6C6'))
    s.poly([(18, 8), (34, 6.5), (44, 8), (34, 9.5)], hexc('E6FFFB'))
    out['fx-tracer'] = s.finish(outline=0, shadow=None)
    # Mortar shell.
    s = Sprite(40, 40)
    s.ellipse(20, 20, 14, 14, hexc('3A3533'))
    s.ellipse(17, 17, 7, 7, hexc('F07438'))
    out['fx-shell'] = s.finish(outline=1.5, shadow=None)
    # Explosion burst (orange, shaped petals).
    s = Sprite(160, 160)
    for i in range(10):
        a = math.radians(36 * i)
        s.poly([(80, 80), (80 + math.cos(a - 0.2) * 40, 80 + math.sin(a - 0.2) * 40), (80 + math.cos(a) * 74, 80 + math.sin(a) * 74), (80 + math.cos(a + 0.2) * 40, 80 + math.sin(a + 0.2) * 40)], hexc('F07438', 230))
    s.ellipse(80, 80, 40, 40, hexc('FFB060', 240))
    s.ellipse(80, 80, 22, 22, hexc('FFF0D0', 255))
    out['fx-burst'] = s.finish(outline=0, shadow=None)
    # Vortex for gravity.
    s = Sprite(160, 160)
    for k in range(3):
        s.arc(80, 80, 66 - k * 16, 66 - k * 16, k * 50, k * 50 + 250, 7, hexc('8E80F0', 220 - k * 30))
    s.ellipse(80, 80, 16, 16, hexc('1C1838'))
    out['fx-vortex'] = s.finish(outline=0, shadow=None)
    # Light pillar for 종결.
    s = Sprite(64, 256)
    s.poly([(18, 256), (24, 0), (40, 0), (46, 256)], hexc('FFF2C4', 170))
    s.poly([(27, 256), (29, 0), (35, 0), (37, 256)], hexc('FFFFFF', 255))
    out['fx-pillar'] = s.finish(outline=0, shadow=None)
    # Shard for hits / shatter.
    s = Sprite(32, 32)
    s.poly([(4, 16), (16, 4), (28, 16), (16, 28)], (255, 255, 255, 255))
    out['fx-shard'] = s.finish(outline=0, shadow=None)
    # Rift (enemy gate) - flat tear on the ground.
    s = Sprite(220, 140)
    s.ellipse(110, 74, 100, 52, hexc('2A2433', 200))
    s.poly([(30, 74), (70, 52), (100, 34), (130, 40), (176, 60), (196, 76), (150, 98), (110, 110), (64, 98)], hexc('15111C'))
    s.poly([(60, 74), (96, 54), (126, 58), (160, 72), (124, 90), (90, 92)], hexc('E0406A'))
    s.poly([(84, 74), (104, 64), (130, 68), (140, 74), (118, 84), (96, 84)], hexc('FF8FB0'))
    for x, h in ((40, 40), (70, 52), (150, 46), (182, 34)):
        s.poly([(x - 10, 80), (x, 80 - h), (x + 10, 80)], hexc('2A2433'))
    out['fx-rift'] = s.finish(outline=2.4, shadow=None)
    # Danger telegraph: red dashed ring (boss / warning).
    s = Sprite(256, 256)
    for i in range(16):
        a0 = i * 22.5
        s.arc(128, 128, 120, 120, a0, a0 + 13, 8, hexc('E0406A'))
    s.ellipse(128, 128, 112, 112, hexc('E0406A', 60))
    out['fx-danger'] = s.finish(outline=0, shadow=None)
    # Thin range circle: 512 px at 100 px/unit -> 5.0 units diameter at scale 1.
    s = Sprite(512, 512)
    s.ring(256, 256, 250, 250, 3, (255, 255, 255, 255))
    out['fx-range'] = s.finish(outline=0, shadow=None)
    # Selection ring (amber, for selected pad/tower).
    s = Sprite(256, 160)
    s.ring(128, 80, 118, 64, 7, AMBER)
    out['fx-select'] = s.finish(outline=0, shadow=None)
    # Shield arc for 방벽 hold / 방호.
    s = Sprite(160, 160)
    s.arc(80, 80, 66, 66, 200, 340, 10, hexc('6E9CE0'))
    s.arc(80, 80, 66, 66, 210, 330, 4, hexc('D6E4FB'))
    out['fx-shield'] = s.finish(outline=0, shadow=None)
    # Fusion-ready glyph above a tower.
    s = Sprite(72, 72)
    s.ellipse(36, 36, 30, 30, hexc('1B2230', 220))
    s.ellipse(26, 30, 11, 11, hexc('F0B340'))
    s.ellipse(46, 30, 11, 11, hexc('3FD6C6'))
    s.poly([(24, 44), (48, 44), (36, 58)], hexc('E8ECEF'))
    out['fx-fusion-ready'] = s.finish(outline=0, shadow=None)
    return out
