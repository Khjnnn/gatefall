"""Small vector-ish raster toolkit for Gatefall v0.5 art.

Every sprite is drawn at SS-times resolution and downsampled, so edges are
anti-aliased. Style rules live here so all assets share them:
  - 3/4 top-down view: shapes show a top face and the south-facing side.
  - Light from the north-west: side faces darken toward the east.
  - Actors (towers, units, core) get one ink outline width and a soft shadow
    cast to the south-east. Environment art gets neither.
"""
import math
import random

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter

SS = 4
INK = (27, 34, 48)


def hexc(value, alpha=255):
    value = value.lstrip('#')
    return (int(value[0:2], 16), int(value[2:4], 16), int(value[4:6], 16), alpha)


def shade(color, factor):
    r, g, b = color[:3]
    a = color[3] if len(color) > 3 else 255
    return (min(255, int(r * factor)), min(255, int(g * factor)), min(255, int(b * factor)), a)


def mix(a, b, t):
    a4 = a if len(a) > 3 else a + (255,)
    b4 = b if len(b) > 3 else b + (255,)
    return tuple(int(a4[i] + (b4[i] - a4[i]) * t) for i in range(4))


def alpha(color, a):
    return (color[0], color[1], color[2], int(a))


class Sprite:
    """Canvas in 1x pixel units; drawing happens at SS scale."""

    def __init__(self, width, height):
        self.w, self.h = width, height
        self.img = Image.new('RGBA', (width * SS, height * SS), (0, 0, 0, 0))
        self.glow_layer = Image.new('RGBA', (width * SS, height * SS), (0, 0, 0, 0))

    # -- primitive helpers -------------------------------------------------
    def _p(self, pts):
        return [(x * SS, y * SS) for x, y in pts]

    def mask_poly(self, pts):
        m = Image.new('L', self.img.size, 0)
        ImageDraw.Draw(m).polygon(self._p(pts), fill=255)
        return m

    def mask_ellipse(self, cx, cy, rx, ry):
        m = Image.new('L', self.img.size, 0)
        ImageDraw.Draw(m).ellipse([(cx - rx) * SS, (cy - ry) * SS, (cx + rx) * SS, (cy + ry) * SS], fill=255)
        return m

    def fill(self, mask, color):
        layer = Image.new('RGBA', self.img.size, color)
        self.img.paste(layer, (0, 0), ImageChops.multiply(mask, Image.new('L', mask.size, color[3] if len(color) > 3 else 255)))

    def fill_grad(self, mask, c1, c2, axis='y', box=None):
        """Linear gradient across the mask bounding box (or explicit box)."""
        bb = mask.getbbox() if box is None else tuple(int(v * SS) for v in box)
        if bb is None:
            return
        x0, y0, x1, y1 = bb
        w, h = self.img.size
        if axis == 'y':
            t = np.clip((np.arange(h) - y0) / max(1, y1 - y0), 0, 1)[:, None] * np.ones((1, w))
        else:
            t = np.clip((np.arange(w) - x0) / max(1, x1 - x0), 0, 1)[None, :] * np.ones((h, 1))
        c1 = np.array(c1 if len(c1) > 3 else c1 + (255,), dtype=np.float32)
        c2 = np.array(c2 if len(c2) > 3 else c2 + (255,), dtype=np.float32)
        arr = c1[None, None, :] * (1 - t[..., None]) + c2[None, None, :] * t[..., None]
        layer = Image.fromarray(arr.astype(np.uint8), 'RGBA')
        a = layer.getchannel('A')
        self.img.paste(layer, (0, 0), ImageChops.multiply(mask, a))

    def poly(self, pts, color):
        self.fill(self.mask_poly(pts), color)

    def poly_grad(self, pts, c1, c2, axis='y'):
        self.fill_grad(self.mask_poly(pts), c1, c2, axis)

    def ellipse(self, cx, cy, rx, ry, color):
        self.fill(self.mask_ellipse(cx, cy, rx, ry), color)

    def ellipse_grad(self, cx, cy, rx, ry, c1, c2, axis='y'):
        self.fill_grad(self.mask_ellipse(cx, cy, rx, ry), c1, c2, axis)

    def ring(self, cx, cy, rx, ry, width, color):
        m = self.mask_ellipse(cx, cy, rx, ry)
        inner = self.mask_ellipse(cx, cy, max(0.1, rx - width), max(0.1, ry - width * ry / max(rx, 0.1)))
        self.fill(ImageChops.subtract(m, inner), color)

    def arc(self, cx, cy, rx, ry, start, end, width, color):
        d = ImageDraw.Draw(self.img)
        d.arc([(cx - rx) * SS, (cy - ry) * SS, (cx + rx) * SS, (cy + ry) * SS], start, end, fill=color, width=int(width * SS))

    def line(self, pts, width, color):
        m = Image.new('L', self.img.size, 0)
        d = ImageDraw.Draw(m)
        d.line(self._p(pts), fill=255, width=int(width * SS), joint='curve')
        for x, y in pts:
            r = width / 2
            d.ellipse([(x - r) * SS, (y - r) * SS, (x + r) * SS, (y + r) * SS], fill=255)
        self.fill(m, color)

    def rect(self, x0, y0, x1, y1, color):
        self.poly([(x0, y0), (x1, y0), (x1, y1), (x0, y1)], color)

    def glow(self, cx, cy, r, color, strength=0.6):
        """Soft emissive bloom kept small; drawn on a separate layer under the outline pass."""
        m = self.mask_ellipse(cx, cy, r, r).filter(ImageFilter.GaussianBlur(r * SS * 0.45))
        layer = Image.new('RGBA', self.img.size, color[:3] + (255,))
        a = m.point(lambda v: int(v * strength))
        self.glow_layer.paste(layer, (0, 0), a)

    # -- 3/4 solids ----------------------------------------------------------
    def prism(self, base, height, top, side):
        """Extrude a convex polygon upward. Visible sides face south (screen down)."""
        n = len(base)
        topp = [(x, y - height) for x, y in base]
        cx = sum(x for x, _ in base) / n
        cy = sum(y for _, y in base) / n
        faces = []
        for i in range(n):
            a, b = base[i], base[(i + 1) % n]
            ex, ey = b[0] - a[0], b[1] - a[1]
            nx, ny = ey, -ex  # outward for clockwise screen polygons
            mx, my = (a[0] + b[0]) / 2 - cx, (a[1] + b[1]) / 2 - cy
            if nx * mx + ny * my < 0:
                nx, ny = -nx, -ny
            length = math.hypot(nx, ny) or 1
            nx, ny = nx / length, ny / length
            if ny > 0.02:
                faces.append((ny, nx, [a, b, (b[0], b[1] - height), (a[0], a[1] - height)]))
        for ny, nx, quad in sorted(faces, key=lambda f: f[0]):
            factor = 0.92 - 0.22 * max(0, nx) + 0.08 * max(0, -nx)
            self.poly_grad(quad, shade(side, factor * 1.06), shade(side, factor * 0.86))
        self.poly(topp, top)
        return topp

    def cylinder(self, cx, cy, rx, ry, height, top, side):
        """Vertical cylinder whose base ellipse centre is (cx, cy)."""
        body = Image.new('L', self.img.size, 0)
        d = ImageDraw.Draw(body)
        d.rectangle([(cx - rx) * SS, (cy - height) * SS, (cx + rx) * SS, cy * SS], fill=255)
        d.ellipse([(cx - rx) * SS, (cy - ry) * SS, (cx + rx) * SS, (cy + ry) * SS], fill=255)
        self.fill_grad(body, shade(side, 1.12), shade(side, 0.68), 'x', (cx - rx, cy - height, cx + rx, cy))
        self.ellipse(cx, cy - height, rx, ry, top)

    def octagon(self, cx, cy, rx, ry, rotate=22.5):
        return [(cx + rx * math.cos(math.radians(rotate + 45 * i)), cy + ry * math.sin(math.radians(rotate + 45 * i))) for i in range(8)]

    def ngon(self, cx, cy, rx, ry, n, rotate=0):
        return [(cx + rx * math.cos(math.radians(rotate + 360 / n * i)), cy + ry * math.sin(math.radians(rotate + 360 / n * i))) for i in range(n)]

    # -- finishing ---------------------------------------------------------
    def finish(self, outline=3.0, shadow=(10, 8), shadow_alpha=95, ground=None):
        """Return the 1x RGBA sprite with ink outline, glow and drop shadow."""
        base = self.img
        a = base.getchannel('A')
        result = Image.new('RGBA', base.size, (0, 0, 0, 0))
        if shadow:
            sh = a.point(lambda v: 255 if v > 30 else 0)
            if ground is not None:
                # A flattened ground shadow: squash vertically toward the base line.
                gx, gy = ground
                sh = sh.transform(sh.size, Image.AFFINE, (1, -0.55, gy * SS * 0.55, 0, 1.9, -gy * SS * 0.9), Image.BILINEAR)
            moved = Image.new('L', sh.size, 0)
            moved.paste(sh, (int(shadow[0] * SS), int(shadow[1] * SS)))  # no wrap-around
            sh = moved.filter(ImageFilter.GaussianBlur(5 * SS))
            result.paste(Image.new('RGBA', base.size, (12, 16, 26, 255)), (0, 0), sh.point(lambda v: int(v * shadow_alpha / 255)))
        if outline:
            k = int(outline * SS) * 2 + 1
            o = a.point(lambda v: 255 if v > 60 else 0).filter(ImageFilter.MaxFilter(k if k % 2 else k + 1))
            o = o.filter(ImageFilter.GaussianBlur(SS * 0.5))
            result.paste(Image.new('RGBA', base.size, INK + (255,)), (0, 0), o)
        result.alpha_composite(self.glow_layer)
        result.alpha_composite(base)
        return result.resize((self.w, self.h), Image.LANCZOS)


def noise(w, h, scale, seed, octaves=3):
    rng = np.random.default_rng(seed)
    out = np.zeros((h, w), np.float32)
    amp = 1.0
    total = 0
    for o in range(octaves):
        s = max(1, int(scale / (2 ** o)))
        small = rng.random((h // s + 2, w // s + 2)).astype(np.float32)
        im = Image.fromarray((small * 255).astype(np.uint8), 'L').resize(((w // s + 2) * s, (h // s + 2) * s), Image.BICUBIC)
        out += np.asarray(im, np.float32)[:h, :w] / 255 * amp
        total += amp
        amp *= 0.5
    return out / total


def contact_sheet(images, columns, background=(116, 122, 118, 255), pad=16, scale=1.0):
    images = [im.resize((int(im.width * scale), int(im.height * scale)), Image.LANCZOS) for im in images]
    cw = max(im.width for im in images) + pad
    ch = max(im.height for im in images) + pad
    rows = math.ceil(len(images) / columns)
    sheet = Image.new('RGBA', (cw * columns + pad, ch * rows + pad), background)
    for i, im in enumerate(images):
        x = pad + (i % columns) * cw
        y = pad + (i // columns) * ch
        sheet.alpha_composite(im, (x, y))
    return sheet


def rng(seed):
    return random.Random(seed)
