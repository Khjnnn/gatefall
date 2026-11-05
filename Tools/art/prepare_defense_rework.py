"""Fit the original generated tower art to the shared Unity sprite canvas."""
from pathlib import Path
import re
import uuid

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Docs/Art/defense-rework"
OUTPUT = ROOT / "Assets/Resources/Art/V12"

for key, sizes in {
    "shockgate": ((182, 174), (202, 193)),
    "tether": ((162, 179), (183, 200)),
    "searchlight": ((151, 218), (169, 241)),
}.items():
    original = Image.open(SOURCE / f"{key}.png").convert("RGBA")
    bounds = original.getchannel("A").point(lambda value: 255 if value >= 20 else 0).getbbox()
    original = original.crop(bounds)
    for level, size in enumerate(sizes, 1):
        piece = original.copy()
        piece.thumbnail(size, Image.Resampling.LANCZOS)
        sprite = Image.new("RGBA", (256, 320))
        sprite.alpha_composite(piece, ((256 - piece.width) // 2, 311 - piece.height))
        path = OUTPUT / f"tower-{key}-{level}.png"
        sprite.save(path, optimize=True)
        template = (OUTPUT / f"tower-spear-{level}.png.meta").read_text(encoding="utf-8")
        guid = uuid.uuid5(uuid.NAMESPACE_URL, f"gatefall-defense-rework/{path.name}").hex
        path.with_suffix(".png.meta").write_text(
            re.sub(r"(?m)^guid: [a-f0-9]{32}$", "guid: " + guid, template), encoding="utf-8"
        )
