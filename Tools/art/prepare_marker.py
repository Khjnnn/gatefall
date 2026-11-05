from PIL import Image
from pathlib import Path
import uuid,re
root=Path.cwd(); out=root/'Assets/Resources/Art/V6'; src=root/'Docs/Art/marker-source'; template=(out/'tower-spear-1.png.meta').read_text(encoding='utf8')
for name in ('marker','observer'):
    image=Image.open(src/(name+'.png')).convert('RGBA'); box=image.getchannel('A').getbbox(); image=image.crop(box)
    image.thumbnail((200,270),Image.Resampling.LANCZOS)
    canvas=Image.new('RGBA',(256,320));canvas.alpha_composite(image,((256-image.width)//2,310-image.height))
    for level in (1,2):
        dest=out/('tower-'+name+'-'+str(level)+'.png');canvas.save(dest)
        meta=re.sub(r'(?m)^guid: [a-f0-9]{32}$','guid: '+uuid.uuid5(uuid.NAMESPACE_URL,'gatefall-v0911/'+dest.name).hex,template)
        Path(str(dest)+'.meta').write_text(meta,encoding='utf8')
