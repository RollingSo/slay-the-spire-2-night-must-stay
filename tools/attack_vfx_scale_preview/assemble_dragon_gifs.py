"""Assemble native Godot captures, without modifying VFX artwork."""
from pathlib import Path
from PIL import Image
import sys

root = Path(__file__).resolve().parents[2]
output = root / "design/特效预览/dragon_lightning_20261007"
names = ["AncientDragonColumn", "DeathColumn", "AncientDragonSpear", "FortissaxSpears"]
filename = "dragon_lightning.gif"
if "--gold" in sys.argv:
    output = root / "design/特效预览/gold_lightning_20261007"
    names = ["GoldenLightningSpear", "GoldenLightningStrike"]
    filename = "gold_lightning.gif"
frames = []
for frame in range(49):
    board = Image.new("RGB", (210 * len(names), 570), "#252936")
    for index, name in enumerate(names):
        with Image.open(root / f".tmp/vfx-scale/dragon/{name}/frame_{frame:02}.png") as capture:
            if capture.width == 1200:
                capture = capture.crop((100, 0, 1000, 950))
            capture = capture.resize((210, 285), Image.Resampling.LANCZOS)
            board.paste(capture, (index * 210, 140), capture)
    frames.append(board)
palette = frames[6].quantize(colors=192)
indexed = [frame.quantize(palette=palette, dither=Image.Dither.NONE) for frame in frames]
indexed[0].save(output / filename, save_all=True, append_images=indexed[1:],
                duration=42, loop=0, disposal=2)
print("Saved production effects: " + ", ".join(names))
