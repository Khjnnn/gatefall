"""Original compact vector-style UI pictograms and combat effects for v0.6.

The images are intentionally simple: a phone must read them at small sizes.
All symbols are drawn here rather than copied from the earlier artwork.
"""
import math
from PIL import Image, ImageDraw

GOLD = (235, 180, 85, 255)
TEAL = (91, 216, 200, 255)
WHITE = (240, 237, 222, 255)
RED = (226, 89, 111, 255)
DIM = (132, 149, 155, 255)


def icon(name, size):
    s = 3
    im = Image.new('RGBA', (size[0]*s, size[1]*s))
    d = ImageDraw.Draw(im)
    def pt(p): return tuple(round(v * s) for v in p)
    def line(p, c=WHITE, w=7): d.line([pt(v) for v in p], fill=c, width=w*s, joint='curve')
    def poly(p, c): d.polygon([pt(v) for v in p], fill=c)
    def oval(box, fill=None, outline=None, width=6): d.ellipse(pt(box), fill=fill, outline=outline, width=width*s)
    def rect(box, fill, radius=0): d.rounded_rectangle(pt(box), radius=radius*s, fill=fill)
    # Coordinates below use a 96-square logical canvas; existing icon assets are 96x96.
    if name in ('icon-stone', 'icon-catalyst', 'icon-core'):
        c = GOLD if name == 'icon-stone' else TEAL if name == 'icon-catalyst' else RED
        poly([(48,8),(76,33),(65,75),(48,88),(30,75),(20,33)], c)
        poly([(48,8),(48,77),(30,75),(20,33)], tuple(min(255,int(x*1.18)) for x in c[:3])+(255,))
        line([(48,8),(48,77)], (50,57,61,180), 3)
    elif name in ('icon-star', 'icon-star-empty'):
        p=[]
        for i in range(10):
            a=math.pi*i/5-math.pi/2; r=39 if i%2==0 else 18
            p.append((48+math.cos(a)*r, 48+math.sin(a)*r))
        poly(p, GOLD if name == 'icon-star' else DIM)
    elif name == 'icon-play': poly([(29,17),(77,48),(29,79)], (28,35,37,255))
    elif name == 'icon-pause':
        rect((27,22,41,74),WHITE,3); rect((55,22,69,74),WHITE,3)
    elif name == 'icon-upgrade':
        line([(23,52),(48,28),(73,52)], GOLD,9); line([(23,72),(48,48),(73,72)], GOLD,9)
    elif name == 'icon-fusion':
        oval((11,18,45,52),outline=GOLD,width=7); oval((51,18,85,52),outline=TEAL,width=7)
        line([(29,58),(48,76),(67,58)],WHITE,6)
    elif name == 'icon-sell':
        oval((19,19,77,77),outline=WHITE,width=6); line([(50,28),(50,68)],GOLD,5); line([(36,40),(58,40)],GOLD,5); line([(38,56),(60,56)],GOLD,5)
    elif name == 'icon-lock':
        d.arc(pt((27,12,69,62)),180,360,fill=DIM,width=8*s); rect((22,44,74,82),DIM,5); oval((44,55,52,65),fill=(28,35,37,255))
    elif name in ('icon-barrier','icon-skill-2'):
        poly([(48,10),(77,23),(73,57),(48,84),(23,57),(19,23)], TEAL if name=='icon-barrier' else (131,175,225,255))
        line([(48,20),(48,69)],WHITE,4)
    elif name == 'icon-retreat':
        line([(27,14),(27,85)],WHITE,6); poly([(30,17),(74,25),(58,44),(30,49)],WHITE)
    elif name == 'icon-growth':
        oval((30,12,66,48),fill=WHITE); poly([(19,82),(26,58),(40,51),(58,51),(70,58),(77,82)],WHITE); poly([(68,24),(75,12),(82,24)],GOLD)
    elif name == 'icon-settings':
        oval((21,21,75,75),outline=WHITE,width=7); oval((39,39,57,57),outline=WHITE,width=5)
        for i in range(8):
            a=i*math.pi/4; line([(48+28*math.cos(a),48+28*math.sin(a)),(48+39*math.cos(a),48+39*math.sin(a))],WHITE,7)
    elif name == 'icon-back': line([(58,20),(29,48),(58,76)],WHITE,9); line([(31,48),(79,48)],WHITE,8)
    elif name == 'icon-hand':
        poly([(39,10),(57,10),(57,51),(76,51),(48,84),(20,51),(39,51)],GOLD)
    elif name in ('icon-sound-on','icon-sound-off'):
        poly([(13,37),(29,37),(51,20),(51,76),(29,59),(13,59)], WHITE)
        if name.endswith('on'):
            d.arc(pt((45,25,83,71)), -55, 55, fill=WHITE, width=6*s)
        else: line([(63,34),(82,63)],RED,7); line([(82,34),(63,63)],RED,7)
    elif name in ('icon-skill-0','icon-skill-1'):
        c = TEAL if name.endswith('0') else (155,220,171,255)
        for x in (18,38,58): line([(x,25),(x+20,48),(x,71)],c,7)
    else: raise ValueError('Unknown icon ' + name)
    return im.resize(size, Image.Resampling.LANCZOS)


def effect(name, size):
    s=3; w,h=size; im=Image.new('RGBA',(w*s,h*s)); d=ImageDraw.Draw(im)
    def box(x0,y0,x1,y1): return tuple(round(x*s) for x in (x0,y0,x1,y1))
    def ellipse(b, fill=None, outline=None, width=1): d.ellipse(box(*b),fill=fill,outline=outline,width=max(1,round(width*s)))
    def poly(points, color): d.polygon([(round(x*s),round(y*s)) for x,y in points],fill=color)
    def line(points,color,width): d.line([(round(x*s),round(y*s)) for x,y in points],fill=color,width=max(1,round(width*s)),joint='curve')
    cx,cy=w/2,h/2
    if name == 'fx-disc': ellipse((1,1,w-1,h-1),fill=WHITE)
    elif name in ('fx-ring','fx-range','fx-select','fx-ring-soft'):
        color = GOLD if name=='fx-select' else WHITE
        thickness = max(2,w*.035) if name!='fx-range' else max(2,w*.007)
        ellipse((thickness,thickness,w-thickness,h-thickness),outline=color,width=thickness)
        if name == 'fx-ring-soft': ellipse((w*.16,h*.16,w*.84,h*.84),outline=(240,237,222,90),width=2)
    elif name in ('fx-bolt','fx-tracer'):
        c = GOLD if name=='fx-bolt' else TEAL
        poly([(0,cy),(w*.73,h*.19),(w*.99,cy),(w*.73,h*.81)],c)
        poly([(w*.25,cy),(w*.75,h*.39),(w*.94,cy),(w*.75,h*.61)],WHITE)
    elif name == 'fx-shell':
        ellipse((w*.12,h*.12,w*.88,h*.88),fill=(48,43,39,255)); ellipse((w*.27,h*.22,w*.65,h*.6),fill=(249,132,54,255))
    elif name == 'fx-shard': poly([(cx,h*.03),(w*.9,cy),(cx,h*.97),(w*.1,cy)],WHITE)
    elif name == 'fx-pillar':
        poly([(w*.2,h),(w*.36,0),(w*.64,0),(w*.8,h)],(255,229,164,150)); poly([(w*.42,h),(w*.47,0),(w*.53,0),(w*.58,h)],WHITE)
    elif name == 'fx-burst':
        points=[]
        for i in range(24):
            a=i*math.pi/12; r=(.47 if i%2==0 else .20)*w
            points.append((cx+math.cos(a)*r,cy+math.sin(a)*r))
        poly(points,(241,143,73,225)); ellipse((w*.3,h*.3,w*.7,h*.7),fill=(255,225,166,240))
    elif name == 'fx-vortex':
        for i in range(3):
            r=w*(.42-i*.11); d.arc(box(cx-r,cy-r,cx+r,cy+r),i*55,i*55+260,fill=(153,129,224,230-i*35),width=max(1,round(w*.04*s)))
    elif name == 'fx-danger':
        ellipse((w*.08,h*.08,w*.92,h*.92),fill=(226,89,111,48))
        for i in range(16): d.arc(box(w*.04,h*.04,w*.96,h*.96),i*22.5,i*22.5+13,fill=RED,width=max(1,round(w*.025*s)))
    elif name == 'fx-shield': d.arc(box(w*.1,h*.1,w*.9,h*.9),200,340,fill=(137,187,235,255),width=max(1,round(w*.06*s)))
    elif name == 'fx-fusion-ready':
        ellipse((2,2,w-2,h-2),fill=(25,34,39,230)); ellipse((w*.19,h*.25,w*.48,h*.54),fill=GOLD); ellipse((w*.52,h*.25,w*.81,h*.54),fill=TEAL)
        poly([(w*.3,h*.65),(w*.7,h*.65),(w*.5,h*.83)],WHITE)
    else: raise ValueError('Unknown effect ' + name)
    return im.resize(size,Image.Resampling.LANCZOS)
