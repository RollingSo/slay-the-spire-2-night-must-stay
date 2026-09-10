"""Assemble actual Godot-rendered frames; does not draw or replace effect artwork."""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "design/特效预览/refined_remake_20260911"
for page in range(1, 5):
    paths = sorted((ROOT / f".tmp/vfx-scale/movie/page{page}").glob("frame_*.png"))
    assert len(paths) == 48, f"page {page}: expected 48 frames, found {len(paths)}"
    images = [Image.open(path).convert("RGB") for path in paths]
    # One palette per complete clip avoids color flicker between frames.
    atlas = Image.new("RGB", (320 * 8, 180 * 6))
    for i, img in enumerate(images):
        atlas.paste(img.resize((320, 180)), (i % 8 * 320, i // 8 * 180))
    palette = atlas.quantize(colors=192)
    frames = [img.quantize(palette=palette, dither=Image.Dither.NONE) for img in images]
    frames[0].save(OUTPUT / f"all_effects_{page}.gif", save_all=True,
                   append_images=frames[1:], duration=[40, 40, 40, 40, 40, 50] * 8,
                   loop=0, optimize=False, disposal=2)
    for img in images + frames:
        img.close()
    print(f"page {page}: 48 frames, 2.0 s, 1280 x 720")
