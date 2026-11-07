"""Register original Urban Ward PNGs without editing their pixels. Requires Pillow for alpha inspection."""
import hashlib
import json
import uuid
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
FOLDER = ROOT / 'Assets/Resources/Art/UrbanWard'
KEYS = ['icon-stone', 'icon-catalyst', 'tower-spear-1', 'tower-spear-2', 'ward-impact']


def meta(path, kind='texture'):
    target = Path(str(path) + '.meta')
    if target.exists():
        return
    guid = uuid.uuid4().hex
    if kind == 'texture':
        template = (ROOT / 'Assets/Resources/Art/V6/tower-spear-1.png.meta').read_text(encoding='utf-8-sig')
        import re
        template = re.sub(r'(?m)^guid:.*$', 'guid: ' + guid, template)
        template = re.sub(r'maxTextureSize: \d+', 'maxTextureSize: 512', template)
        template = re.sub(r'textureCompression: \d+', 'textureCompression: 0', template)
        target.write_text(template, encoding='utf-8')
    else:
        importer = 'DefaultImporter' if kind == 'folder' else 'TextScriptImporter'
        target.write_text('fileFormatVersion: 2\nguid: ' + guid + ('\nfolderAsset: yes' if kind == 'folder' else '') +
                          '\n' + importer + ':\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n', encoding='utf-8')


entries = []
for key in KEYS:
    path = FOLDER / (key + '.png')
    with Image.open(path) as source:
        if source.mode != 'RGBA':
            raise ValueError('RGBA required: ' + key)
        alpha = source.getchannel('A')
        if alpha.getextrema() != (0, 255):
            raise ValueError('Real transparent background required: ' + key)
        box = alpha.point(lambda a: 255 if a > 32 else 0).getbbox()
        width, height = source.size
        if not box or min(box[0], box[1], width-box[2], height-box[3]) < 8:
            raise ValueError('Clipped silhouette: ' + key)
        visible = box[3] - box[1]
        tower = key.startswith('tower-')
        entries.append(dict(key=key, sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                            sourceWidth=width, sourceHeight=height, alphaBounds=list(box),
                            heightWorld=3.0 * height / visible if tower else 1.6 if key == 'ward-impact' else .96,
                            pivotX=.5, pivotY=(height-box[3]+visible*.10)/height if tower else .5))
    meta(path)
manifest = FOLDER / 'manifest.json'
manifest.write_text(json.dumps(dict(style='gatefall-urban-ward', version='1.2.0', assets=entries), indent=2) + '\n', encoding='utf-8')
meta(manifest, 'text')
meta(FOLDER, 'folder')
print('PASS: registered5 original transparent PNGs, scale-independent world heights and ground pivots; source pixels unchanged.')
