"""Prepare original v0.6 generated art for Unity, retaining v0.5 footprint sizes.

The inputs are preserved separately in Docs/Art/v06-source. This is a mechanical
crop/resize/placement step, not a generator. Existing V5 resources remain intact.
"""
from pathlib import Path
import re
import sys
import uuid
from collections import deque
from PIL import Image, ImageFilter
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
OLD = ROOT / 'Assets/Resources/Art/V5'
OUT = ROOT / 'Assets/Resources/Art/V6'
SRC = ROOT / 'Docs/Art/v06-source'
sys.path.insert(0, str(ROOT / 'Tools/art'))
from ui import rounded
import v06_ui


def meta(path, template):
    target = Path(str(path) + '.meta')
    if target.exists():
        return
    guid = uuid.uuid5(uuid.NAMESPACE_URL, 'gatefall-v06/' + path.name).hex
    text = template.read_text(encoding='utf-8')
    text = re.sub(r'(?m)^guid: [a-f0-9]{32}$', 'guid: ' + guid, text)
    target.write_text(text, encoding='utf-8')


def save(name, image):
    path = OUT / (name + '.png')
    image.save(path, optimize=True)
    meta(path, OLD / (name + '.png.meta'))


def remove_atlas_fragments(image):
    """Discard small disconnected pieces from neighbouring atlas cells.

    Work at a small mask size, keep meaningful isolated details, then dilate the
    retained mask before applying it to original alpha so antialiased edges survive.
    """
    w, h = image.size
    factor = min(1.0, 240 / max(w, h))
    size = (max(1, round(w * factor)), max(1, round(h * factor)))
    alpha = image.getchannel('A')
    mask = alpha.resize(size, Image.Resampling.LANCZOS).point(lambda x: 255 if x > 20 else 0).filter(ImageFilter.MaxFilter(3))
    a = np.asarray(mask) > 0
    seen = np.zeros(a.shape, dtype=bool)
    components = []
    for y, x in zip(*np.where(a)):
        if seen[y, x]:
            continue
        seen[y, x] = True
        queue = deque([(int(y), int(x))])
        coords = []
        while queue:
            cy, cx = queue.popleft()
            coords.append((cy, cx))
            for ny, nx in ((cy-1,cx), (cy+1,cx), (cy,cx-1), (cy,cx+1)):
                if 0 <= ny < a.shape[0] and 0 <= nx < a.shape[1] and a[ny,nx] and not seen[ny,nx]:
                    seen[ny,nx] = True
                    queue.append((ny,nx))
        components.append(coords)
    if not components:
        return image
    cutoff = max(len(c) for c in components) * .025
    kept = np.zeros(a.shape, dtype=np.uint8)
    for coords in components:
        if len(coords) >= cutoff:
            yy, xx = zip(*coords)
            kept[yy, xx] = 255
    keep = Image.fromarray(kept, 'L').resize((w, h), Image.Resampling.NEAREST).filter(ImageFilter.MaxFilter(15))
    image.putalpha(Image.composite(alpha, Image.new('L', (w, h)), keep))
    return image


def fit(name, image, clean=False):
    if clean:
        image = remove_atlas_fragments(image)
    old = Image.open(OLD / (name + '.png')).convert('RGBA')
    a = image.getchannel('A')
    bbox = a.point(lambda x: 255 if x > 8 else 0).getbbox()
    if bbox is None:
        raise ValueError('Empty alpha for ' + name)
    image = image.crop(bbox)
    bounds = old.getchannel('A').getbbox()
    if bounds is None:
        raise ValueError('Empty template for ' + name)
    x0, y0, x1, y1 = bounds
    scale = min((x1 - x0) / image.width, (y1 - y0) / image.height)
    image = image.resize((max(1, round(image.width * scale)), max(1, round(image.height * scale))), Image.Resampling.LANCZOS)
    canvas = Image.new('RGBA', old.size)
    # Center on footprint; base alignment matters for y-sorting and touch targets.
    canvas.alpha_composite(image, ((x0 + x1 - image.width) // 2, y1 - image.height))
    save(name, canvas)


def atlas(source, names):
    image = Image.open(SRC / source).convert('RGBA')
    w, h = image.size
    for name, (x0, y0, x1, y1) in names:
        fit(name, image.crop(tuple(round(v) for v in (x0*w, y0*h, x1*w, y1*h))), clean=True)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    meta(OUT, ROOT / 'Assets/Resources/Art/V5.meta')
    for i in range(6):
        open_stage = ROOT / 'Docs/Art/v07-source/stage-3-open.png'
        source = open_stage if i == 3 and open_stage.exists() else SRC / ('stage-%d.png' % (i % 3))
        image = Image.open(source).convert('RGB')
        save('stage-%d' % i, image.resize((2592, 1296), Image.Resampling.LANCZOS))
    if (SRC / 'district-map.png').exists():
        image = Image.open(SRC / 'district-map.png').convert('RGB')
        save('district-map', image.resize((1600, 1000), Image.Resampling.LANCZOS))
    cells = [(0, 0, .5, .5), (.5, 0, 1, .5), (0, .5, .5, 1), (.5, .5, 1, 1)]
    kinds = ['spear', 'binding', 'rupture', 'barrier']
    for level in (1, 2):
        atlas('towers-level%d.png' % level, [('tower-%s-%d' % (k, level), c) for k, c in zip(kinds, cells)])
    fusion = ['arc', 'shatter', 'gravity', 'seal']
    atlas('fusion.png', [('tower-%s-1' % k, c) for k, c in zip(fusion, cells)])
    fusion2 = 'fusion-level2.png' if (SRC / 'fusion-level2.png').exists() else 'fusion.png'
    atlas(fusion2, [('tower-%s-2' % k, c) for k, c in zip(fusion[:3], cells[:3])])
    # The first atlas has a long spear extending past the centre line.
    atlas('units.png', [
        ('hunter-0', (0, 0, .60, .49)),
        ('enemy-runner', (.60, 0, 1, .49)),
        ('enemy-grunt', (.02, .50, .46, 1)),
        ('enemy-brute', (.46, .46, 1, 1)),
    ])
    atlas('heroes.png', [
        ('hunter-1', (0, 0, .5, .49)), ('hunter-2', (.5, 0, 1, .49)),
        ('enemy-siege', (0, .50, .46, 1)), ('enemy-boss', (.46, .47, 1, 1)),
    ])
    fit('core', Image.open(SRC / 'core.png').convert('RGBA'))
    for name in ('pad', 'fx-rift'):
        if (SRC / (name + '.png')).exists():
            fit(name, Image.open(SRC / (name + '.png')).convert('RGBA'))
    # Simple nine-slice native UI art: dark ink, warm cream/gold and quieter edges.
    skins = {
        'ui-panel': rounded(96, 12, (23, 32, 40, 242), top_light=(227, 215, 187, 50), shadow=True),
        'ui-panel-flat': rounded(64, 9, (28, 37, 43, 237)),
        'ui-button': rounded(64, 10, (48, 59, 62, 255), top_light=(237, 229, 208, 45)),
        'ui-button-primary': rounded(64, 10, (226, 167, 69, 255), top_light=(255, 242, 207, 95)),
        'ui-button-disabled': rounded(64, 10, (34, 42, 46, 255)),
        'ui-chip': rounded(48, 18, (20, 27, 32, 220)),
        'ui-selected': rounded(64, 10, (0, 0, 0, 0), border=(226, 167, 69, 255)),
        'ui-bar': rounded(32, 6, (255, 255, 255, 255)),
    }
    for name, image in skins.items():
        save(name, image)
    # Reauthor the small functional symbols and effects as original vector-like
    # shapes at the same dimensions so existing UI/particle scale stays stable.
    for old in sorted(OLD.glob('icon-*.png')):
        save(old.stem, v06_ui.icon(old.stem, Image.open(old).size))
    for old in sorted(OLD.glob('fx-*.png')):
        if old.stem != 'fx-rift':
            save(old.stem, v06_ui.effect(old.stem, Image.open(old).size))
    print('Prepared', len(list(OUT.glob('*.png'))), 'v0.6 textures.')


if __name__ == '__main__':
    main()
