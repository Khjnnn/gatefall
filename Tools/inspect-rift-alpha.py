"""Read existing sprite opacity for HUD-overlap geometry; writes measurements, no edited image."""
import json
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parent.parent
path = root / "Assets/Resources/Art/V6/fx-rift.png"
alpha = Image.open(path).convert("RGBA").getchannel("A")
data = {"source": str(path.relative_to(root)), "width": alpha.width, "height": alpha.height,
        "ppu": 58, "pivot": [0.5, 0.47], "maxScale": 1.55,
        "thresholdBounds": {str(t): alpha.point(lambda p, t=t: 255 if p > t else 0).getbbox()
                            for t in [0, 16, 64, 128, 224]},
        "scope": "Opacity bounds of existing source pixels. Not new art or a rendered Android overlap count."}
(root / "TestResults/north-hud-v0203-rift-alpha.json").write_text(json.dumps(data, indent=2), encoding="utf-8")
print(json.dumps(data))
