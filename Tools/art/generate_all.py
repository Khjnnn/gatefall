"""Regenerate every Gatefall v0.5 art asset into Assets/Resources/Art/V5.

Usage (from the project root):  python -I Tools/art/generate_all.py [--only towers,units,env,ui]
Deterministic: fixed seeds, no network, no external images.
"""
import argparse
import json
import os
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import artlib  # noqa: E402
import env  # noqa: E402
import towers  # noqa: E402
import ui  # noqa: E402
import units  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Art', 'V5')
PREVIEW = os.path.join(ROOT, 'Docs', 'Art', 'v05-previews')


def save(images, manifest):
    for name, image in images.items():
        path = os.path.join(OUT, name + '.png')
        image.save(path, optimize=True)
        manifest[name] = {'width': image.width, 'height': image.height}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--only', default='towers,units,env,ui')
    groups = parser.parse_args().only.split(',')
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(PREVIEW, exist_ok=True)
    manifest_path = os.path.join(OUT, 'manifest.json')
    manifest = json.load(open(manifest_path, encoding='utf-8')) if os.path.exists(manifest_path) else {}
    start = time.time()
    if 'towers' in groups:
        t = towers.all_towers()
        save(t, manifest)
        artlib.contact_sheet(list(t.values()), 6, background=(140, 137, 129, 255), scale=0.5).convert('RGB').save(os.path.join(PREVIEW, 'towers.png'))
    if 'units' in groups:
        u = units.all_units()
        save(u, manifest)
        artlib.contact_sheet(list(u.values()), 4, background=(140, 137, 129, 255), scale=0.6).convert('RGB').save(os.path.join(PREVIEW, 'units.png'))
    if 'ui' in groups:
        items = {}
        items.update(ui.skins())
        items.update(ui.icons())
        items.update(ui.fx())
        save(items, manifest)
        artlib.contact_sheet(list(items.values()), 10, background=(60, 66, 74, 255), scale=0.6).convert('RGB').save(os.path.join(PREVIEW, 'ui.png'))
    if 'env' in groups:
        for i in range(6):
            im = env.stage(i)
            name = 'stage-%d' % i
            im.save(os.path.join(OUT, name + '.png'), optimize=True)
            manifest[name] = {'width': im.width, 'height': im.height, 'ppu': env.PPU, 'half_extent': [env.HX, env.HY]}
            im.resize((im.width // 4, im.height // 4)).save(os.path.join(PREVIEW, name + '.png'))
        im = env.district_map()
        im.save(os.path.join(OUT, 'district-map.png'), optimize=True)
        manifest['district-map'] = {'width': im.width, 'height': im.height, 'nodes': env.MAP_NODES}
    with open(manifest_path, 'w', encoding='utf-8') as f:
        json.dump(manifest, f, indent=1, ensure_ascii=False)
    print('generated', len(manifest), 'assets in %.1fs ->' % (time.time() - start), OUT)


if __name__ == '__main__':
    main()
