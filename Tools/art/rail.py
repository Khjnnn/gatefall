"""Original Raon station art. Regenerate from the project root with python Tools/art/rail.py."""
from pathlib import Path
from random import Random
from math import cos, sin, tau

from PIL import Image, ImageDraw
from artlib import Sprite, hexc


def concrete():
    size = 512
    rng = Random(322)
    image = Image.new("RGB", (size, size))
    phases = [rng.random() * tau for _ in range(4)]
    broad_x = [sin(tau * x / size + phases[0]) for x in range(size)]
    broad_y = [cos(tau * y / size + phases[1]) for y in range(size)]
    values = []
    for y in range(size):
        for x in range(size):
            row = y // 128
            col = ((x + (row % 2) * 128) % size) // 256
            grit = rng.randrange(-3, 4)
            worn = (5 * broad_x[x] * broad_y[y] + 3 * sin(tau * (2*x + y) / size + phases[2])
                    + 2 * cos(tau * (x - 3*y) / size + phases[3]))
            value = round((87, 91, 89, 86, 91, 88, 88, 90)[row * 2 + col] + grit + worn)
            seam = (x + (row % 2) * 128) % 256
            if seam < 2 or y % 128 < 2:
                value -= 13
            elif seam == 2 or y % 128 == 2:
                value += 10
            values.append((value + 6, value + 5, value + 3))
    image.putdata(values)
    return image


def ballast():
    rng = Random(306)
    image = Image.new("RGB", (256, 256), (49, 50, 51))
    d = ImageDraw.Draw(image)
    for _ in range(6800):
        x, y = rng.randrange(256), rng.randrange(256)
        color = rng.randrange(46, 66)
        w, h = rng.randrange(1, 5), rng.randrange(1, 4)
        for dx in (0, -256):
            for dy in (0, -256):
                d.rectangle((x + dx, y + dy, x + w + dx, y + h + dy), fill=(color, color + 1, color + 1))
    return image


def block(sprite, x, y, w, depth, height, top, side):
    return sprite.prism([(x, y-depth), (x+w, y-depth), (x+w, y), (x, y)], height, hexc(top), hexc(side))


def props():
    sprites = []
    s = Sprite(256, 192)
    block(s, 26, 151, 204, 35, 14, "474E54", "242B31")
    block(s, 33, 136, 190, 39, 43, "6F8587", "4D666B")
    s.line([(37, 95), (220, 95)], 2, hexc("83918B"))
    s.line([(38, 132), (220, 132)], 3, hexc("77735E"))
    for x in (48, 79, 110, 141, 172, 203):
        s.line([(x, 96), (x, 131)], 3, hexc("344B50"))
        s.line([(x+3, 97), (x+3, 130)], 1, hexc("7C908E"))
    for x in (116, 146):
        s.rect(x, 117, x+4, 122, hexc("9E8D64"))
    for x in (64, 193):
        s.ellipse(x, 152, 13, 9, hexc("1E2529"))
        s.ellipse(x, 151, 7, 5, hexc("464B4A"))
        s.ellipse(x, 151, 2, 2, hexc("857962"))
    sprites.append(s)  # Freight car with visible wheel bogies

    s = Sprite(256, 192)
    for x in (42, 199):
        block(s, x, 153, 13, 14, 54, "717978", "494D50")
        s.line([(x+2, 101), (x+2, 145)], 2, hexc("8E8063"))
        s.line([(x+8, 115), (x+24 if x<100 else x-8, 99)], 3, hexc("766850"))
    block(s, 26, 99, 204, 48, 12, "638081", "3E575A")
    for x in (36, 80, 124, 168, 212):
        s.line([(x+4, 43), (x, 84)], 2, hexc("426265"))
        s.line([(x+7, 44), (x+3, 84)], 1, hexc("8E9690"))
    s.line([(30, 92), (226, 92)], 3, hexc("827354"))
    for x in (58, 143):
        s.line([(x, 97), (x+43, 97)], 2, hexc("B2A276"))
    sprites.append(s)  # Platform canopy

    s = Sprite(256, 192)
    block(s, 51, 153, 154, 36, 13, "616867", "373D41")
    for x in (72, 124, 176):
        block(s, x, 147, 13, 12, 50, "89918B", "48524F")
        s.rect(x, 87, x+30, 98, hexc("727B76"))
        s.rect(x+16, 88, x+26, 92, hexc("81795B"))
        s.line([(x+12, 102), (x+27, 114), (x+26, 120)], 3, hexc("8B8D81"))
    sprites.append(s)  # Ticket gates

    s = Sprite(256, 192)
    block(s, 42, 151, 166, 53, 33, "8D765D", "624E42")
    block(s, 83, 120, 129, 48, 33, "6F8180", "455959")
    for x in range(88, 201, 15):
        s.line([(x, 94), (x, 118)], 2, hexc("556968"))
    for x in range(48, 201, 17):
        s.line([(x, 126), (x, 150)], 2, hexc("79604C"))
    for x in (45, 193):
        s.rect(x, 141, x+5, 146, hexc("AF9470"))
    s.rect(167, 106, 186, 112, hexc("A99C82"))
    sprites.append(s)  # Stacked containers

    s = Sprite(256, 192)
    for x in (51, 184):
        block(s, x, 152, 12, 12, 19, "636C68", "3C4747")
    block(s, 38, 135, 175, 24, 7, "967D63", "6C5548")
    block(s, 38, 112, 175, 9, 13, "8C7865", "695348")
    s.line([(40, 130), (209, 130)], 1, hexc("BAA181"))
    s.line([(38, 107), (211, 107)], 2, hexc("A88B6A"))
    for x in (49, 193):
        s.ellipse(x, 125, 2, 2, hexc("4C4B45"))
    sprites.append(s)  # Bench

    s = Sprite(256, 192)
    block(s, 85, 149, 78, 29, 9, "797D75", "464B46")
    block(s, 121, 139, 13, 15, 70, "79817F", "414A4C")
    block(s, 101, 88, 52, 18, 41, "626F72", "303B41")
    s.line([(104, 47), (148, 47)], 2, hexc("8D8570"))
    for y, color in ((58, "826453"), (73, "6D887B")):
        s.ellipse(127, y, 10, 7, hexc("242B2D"))
        s.ellipse(127, y, 6, 5, hexc(color))
    sprites.append(s)  # Signal pole
    return [s.finish(outline=0, shadow=(7, 5), shadow_alpha=68) for s in sprites]


def tunnel():
    s = Sprite(256, 192)
    outer = [(34, 147), (34, 94), (54, 62), (92, 44), (164, 44), (202, 62), (222, 94), (222, 147)]
    inner = [(60, 147), (60, 102), (79, 76), (106, 66), (150, 66), (176, 77), (196, 102), (196, 147)]
    s.poly_grad(outer, hexc("848680"), hexc("4C5153"))
    s.poly_grad(inner, hexc("151D23"), hexc("353739"))
    for (x, y), (a, b) in zip(outer[1:-1], inner[1:-1]):
        s.line([(x, y), (a, b)], 2, hexc("555B5B"))
    s.line(inner[1:5], 2, hexc("A29B84"))
    s.rect(113, 47, 144, 55, hexc("8B7754"))
    for x in (118, 139):
        s.ellipse(x, 51, 2, 2, hexc("454743"))
    return s.finish(outline=0, shadow=(7, 5), shadow_alpha=68)


def main():
    out = Path(__file__).resolve().parents[2] / "Assets/Resources/Art/V12"
    concrete().save(out / "rail-concrete-painted.png")
    ballast().save(out / "rail-ballast-painted.png")
    atlas = Image.new("RGBA", (768, 384))
    for i, sprite in enumerate(props()):
        atlas.paste(sprite, ((i % 3) * 256, (i // 3) * 192))
    atlas.save(out / "rail-props-painted.png")
    tunnel().save(out / "rail-tunnel-painted.png")


if __name__ == "__main__":
    main()
