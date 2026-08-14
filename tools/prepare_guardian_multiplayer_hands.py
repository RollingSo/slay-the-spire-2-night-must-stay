from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
HAND_DIR = ROOT / "guardian_assets" / "multiplayer_hands"

for gesture in ("point", "rock", "paper", "scissors"):
    path = HAND_DIR / f"multiplayer_hand_guardian_{gesture}.png"
    with Image.open(path) as source:
        image = source.convert("RGBA").resize((422, 1200), Image.Resampling.LANCZOS)
        alpha = image.getchannel("A")
        if alpha.getbbox() is None:
            raise RuntimeError(f"{path.name}: empty alpha mask")
        if any(image.getpixel(corner)[3] != 0 for corner in ((0, 0), (421, 0), (0, 1199), (421, 1199))):
            raise RuntimeError(f"{path.name}: canvas corners are not transparent")
        image.save(path, optimize=True)
        print(f"{path.name}\t{image.width}x{image.height}\talpha={alpha.getextrema()}")
