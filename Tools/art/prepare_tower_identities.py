"""Create the mobile-readable base tower sprites from Gatefall's own V6 art."""
from pathlib import Path
import re
import uuid

import numpy as np
from PIL import Image

from artlib import Sprite, hexc


ROOT = Path(__file__).resolve().parents[2] / 'Assets/Resources/Art'
# Preserve the textured materials and plinth. Replace the saturated armor and energy.
PALETTES = {
    'spear': (43, .13, .15),
    'binding': (152, .20, .23),
    'rupture': (7, .25, .10),
    'barrier': (218, .17, .23),
    'marker': (289, .22, .21),
}


def recolor(image, key):
    hue, saturation_gain, exposure = PALETTES[key]
    hsv = np.asarray(image.convert('HSV'), dtype=np.float32) / 255
    h, s, v = hsv.transpose(2, 0, 1)
    y = np.arange(image.height)[:, None]
    # Keep the flat stone foot and the mortar's white-hot inner aperture.
    strength = np.clip((s - .13) / .24, 0, 1) * np.clip((310 - y) / 32, 0, 1)
    if key == 'rupture':
        strength *= np.where((y < 177) & (v > .80) & (h > .055) & (h < .16), .20, 1)
    target_h = (hue + (v - .5) * 12) / 360
    target_s = np.minimum(1, .82 * s + saturation_gain)
    delta = (target_h - h + .5) % 1 - .5
    result = np.stack(((h + strength * delta) % 1,
                       s + strength * (target_s - s),
                       v + strength * exposure * v * (1 - v)), axis=-1)
    painted = Image.fromarray(np.round(result * 255).astype('uint8'), 'HSV').convert('RGBA')
    painted.putalpha(image.getchannel('A'))
    return painted


def marker_optics(image, level):
    # Reuse the stone base, and replace the shared tall spire head with a broad tracking sight.
    image.paste((0, 0, 0, 0), (0, 0, 256, 166))
    s = Sprite(256, 320)
    dark = hexc('1C172C')
    steel = hexc('4D395E')
    violet = hexc('DB7AE7')
    light = hexc('FFD1F5')
    s.poly_grad([(105, 199), (108, 155), (89, 139), (128, 130), (163, 142), (151, 160), (149, 196)],
                hexc('8C6599'), dark)
    for side in (-1, 1):
        def p(x, y):
            return (128 + side * x, y)

        s.poly([p(14, 162), p(43, 147), p(63, 104), p(75, 106), p(67, 146), p(45, 170)], dark)
        s.poly_grad([p(21, 159), p(46, 143), p(64, 110), p(69, 111), p(61, 143), p(43, 164)], violet, steel)
        if level == 2:
            s.line([p(45, 135), p(56, 116), p(56, 89)], 8, dark)
            s.line([p(48, 132), p(56, 115), p(56, 92)], 4, light)

    s.ellipse_grad(128, 120, 44, 44, hexc('715680'), dark)
    s.ellipse_grad(128, 118, 33, 33, hexc('422650'), hexc('20192F'))
    s.ring(128, 118, 38, 38, 8, violet)
    s.arc(128, 118, 36, 36, 190, 277, 4, light)
    s.line([(128, 68), (128, 88)], 8, violet)
    s.line([(80, 118), (98, 118)], 7, violet)
    s.line([(158, 118), (176, 118)], 7, violet)
    s.poly([(128, 100), (146, 118), (128, 136), (110, 118)], violet)
    s.poly([(128, 102), (128, 118), (112, 118)], light)
    image.alpha_composite(s.finish(outline=2.2, shadow_alpha=0))
    return image


def main():
    for key in PALETTES:
        for level in (1, 2):
            source = ROOT / 'V6' / f'tower-{key}-{level}.png'
            image = recolor(Image.open(source).convert('RGBA'), key)
            if key == 'marker':
                image = marker_optics(image, level)
            # Beacon retains its own appearance through the previous marker key.
            target_key = 'signal' if key == 'marker' else key
            dest = ROOT / 'V12' / f'tower-{target_key}-{level}.png'
            image.save(dest, optimize=True)
            meta = dest.with_suffix('.png.meta')
            if not meta.exists():
                template = source.with_suffix('.png.meta').read_text(encoding='utf-8')
                guid = uuid.uuid5(uuid.NAMESPACE_URL, 'gatefall-tower-identities/' + dest.name).hex
                meta.write_text(re.sub(r'(?m)^guid: [a-f0-9]{32}$', 'guid: ' + guid, template), encoding='utf-8')


if __name__ == '__main__':
    main()
