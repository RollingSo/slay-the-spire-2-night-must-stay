from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageEnhance, ImageFilter, ImageOps


ROOT = Path(__file__).resolve().parents[1]
GENERATED = Path(r"C:\Users\wenti\.codex\generated_images\019f6b05-d0b0-7af1-a624-760fba7be29d")


SOURCES = {
    "select_bg": GENERATED / "exec-63304fe2-5e89-4142-b622-de2224e85694.png",
    "idle": Path(r"C:\Users\wenti\.codex\generated_images\01a00f23-ef57-7810-a22c-4e46ac8529ac\exec-e0f3a941-c6ac-4080-b330-3a589167b805.png"),
    "attack": Path(r"C:\Users\wenti\.codex\generated_images\01a00f23-ef57-7810-a22c-4e46ac8529ac\exec-60400cb8-9ff8-4d20-818f-412a19a6656e.png"),
    "hit": Path(r"C:\Users\wenti\.codex\generated_images\01a00f23-ef57-7810-a22c-4e46ac8529ac\exec-94b330e6-c86e-4a6e-b8e3-820ea6083330.png"),
    "portrait": GENERATED / "exec-8f9bb1d3-a2f5-465b-a72f-a69303634fda.png",
    "merchant": GENERATED / "exec-79bfe526-59f6-4911-a950-b59a5ee12bc6.png",
    "rest": GENERATED / "exec-655202a7-4657-4124-b03f-e7ace185dcff.png",
}


def remove_connected_checkerboard(source: Path) -> Image.Image:
    """Remove ImageGen's bright neutral checkerboard without deleting enclosed costume whites."""
    source_rgba = np.asarray(Image.open(source).convert("RGBA"), dtype=np.uint8)
    rgb = source_rgba[:, :, :3]
    source_alpha = source_rgba[:, :, 3]
    high = rgb.max(axis=2).astype(np.int16)
    low = rgb.min(axis=2).astype(np.int16)
    mean = rgb.mean(axis=2)
    candidate = (source_alpha == 0) | ((mean >= 224.0) & ((high - low) <= 18))

    h, w = candidate.shape
    visited = np.zeros((h, w), dtype=bool)
    queue: deque[tuple[int, int]] = deque()

    def seed(x: int, y: int) -> None:
        if candidate[y, x] and not visited[y, x]:
            visited[y, x] = True
            queue.append((x, y))

    for x in range(w):
        seed(x, 0)
        seed(x, h - 1)
    for y in range(h):
        seed(0, y)
        seed(w - 1, y)

    while queue:
        x, y = queue.popleft()
        for ny in range(max(0, y - 1), min(h, y + 2)):
            for nx in range(max(0, x - 1), min(w, x + 2)):
                if candidate[ny, nx] and not visited[ny, nx]:
                    visited[ny, nx] = True
                    queue.append((nx, ny))

    rgba = np.dstack((rgb, np.where(visited, 0, source_alpha).astype(np.uint8)))
    return Image.fromarray(rgba, "RGBA")


def save_pose(source: Path, target: Path) -> None:
    image = remove_connected_checkerboard(source)
    bbox = image.getbbox()
    if bbox:
        image = image.crop(bbox)
    image.thumbnail((1124, 1328), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (1156, 1360), (0, 0, 0, 0))
    x = (canvas.width - image.width) // 2
    y = canvas.height - 16 - image.height
    canvas.alpha_composite(image, (x, y))
    target.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(target, optimize=True)


def fit_alpha(image: Image.Image, size: tuple[int, int], padding: int = 0) -> Image.Image:
    image = image.convert("RGBA")
    bbox = image.getbbox()
    if bbox:
        image = image.crop(bbox)
    available = (max(1, size[0] - 2 * padding), max(1, size[1] - 2 * padding))
    image.thumbnail(available, Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    x = (size[0] - image.width) // 2
    y = (size[1] - image.height) // 2
    canvas.alpha_composite(image, (x, y))
    return canvas


def crop_alpha_to_aspect(
    image: Image.Image,
    size: tuple[int, int],
    *,
    padding: int = 0,
    center: tuple[float, float] = (0.5, 0.5),
) -> Image.Image:
    """Fill a fixed portrait slot while preserving transparency and a deliberate focal point."""
    image = image.convert("RGBA")
    bbox = image.getbbox()
    if bbox:
        image = image.crop(bbox)
    inner = (max(1, size[0] - 2 * padding), max(1, size[1] - 2 * padding))
    filled = ImageOps.fit(image, inner, Image.Resampling.LANCZOS, centering=center)
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    canvas.alpha_composite(filled, (padding, padding))
    return canvas


def make_contact_sheet(paths: list[Path], target: Path) -> None:
    thumbs: list[Image.Image] = []
    for path in paths:
        image = Image.open(path).convert("RGBA")
        background = Image.new("RGBA", (640, 480), (19, 25, 36, 255))
        fitted = fit_alpha(image, (620, 460), padding=8)
        background.alpha_composite(fitted, (10, 10))
        thumbs.append(background.convert("RGB"))

    sheet = Image.new("RGB", (1280, 1440), (10, 14, 22))
    for index, thumb in enumerate(thumbs[:6]):
        sheet.paste(thumb, ((index % 2) * 640, (index // 2) * 480))
    target.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(target, quality=92, optimize=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--combat-only", action="store_true")
    args = parser.parse_args()

    for source in SOURCES.values():
        if not source.exists():
            raise FileNotFoundError(source)

    background = Image.open(SOURCES["select_bg"]).convert("RGB")
    background = background.resize((2560, 1200), Image.Resampling.LANCZOS)
    background.save(ROOT / "revenant_assets/character_select_revenant_bg.png", optimize=True)

    save_pose(SOURCES["idle"], ROOT / "revenant_assets/combat/revenant_idle.png")
    save_pose(SOURCES["attack"], ROOT / "revenant_assets/combat/revenant_attack.png")
    save_pose(SOURCES["hit"], ROOT / "revenant_assets/combat/revenant_hit.png")
    if args.combat_only:
        return
    save_pose(SOURCES["merchant"], ROOT / "revenant_assets/merchant/revenant_merchant.png")
    save_pose(SOURCES["rest"], ROOT / "revenant_assets/rest_site/revenant_rest_site.png")

    portrait = Image.open(SOURCES["portrait"]).convert("RGBA")
    # Character-select tiles are tall head-and-neck portraits. Crop the square source
    # rather than letterboxing it, otherwise Revenant appears much smaller than the
    # original characters in the roster.
    select = crop_alpha_to_aspect(portrait, (132, 195), padding=3, center=(0.5, 0.44))
    select.save(ROOT / "revenant_assets/char_select_revenant.png", optimize=True)

    locked_rgb = ImageOps.grayscale(select.convert("RGB")).convert("RGBA")
    locked_rgb.putalpha(select.getchannel("A"))
    locked_rgb = ImageEnhance.Brightness(locked_rgb).enhance(0.55)
    locked_rgb.save(ROOT / "revenant_assets/char_select_revenant_locked.png", optimize=True)

    icon = fit_alpha(portrait, (64, 64), padding=1)
    icon.save(ROOT / "revenant_assets/character_icon_revenant.png", optimize=True)

    alpha = icon.getchannel("A")
    outline_alpha = alpha.filter(ImageFilter.MaxFilter(7))
    outline = Image.new("RGBA", icon.size, (84, 202, 238, 0))
    outline.putalpha(outline_alpha)
    outline.alpha_composite(icon)
    outline.save(ROOT / "revenant_assets/character_icon_revenant_outline.png", optimize=True)

    official_icon = Image.open(ROOT / "design/references/revenant_official_icon.png").convert("RGBA")
    marker = fit_alpha(official_icon, (49, 64), padding=2)
    marker.save(ROOT / "revenant_assets/map_marker_revenant.png", optimize=True)

    make_contact_sheet(
        [
            ROOT / "revenant_assets/combat/revenant_idle.png",
            ROOT / "revenant_assets/combat/revenant_attack.png",
            ROOT / "revenant_assets/combat/revenant_hit.png",
            ROOT / "revenant_assets/merchant/revenant_merchant.png",
            ROOT / "revenant_assets/rest_site/revenant_rest_site.png",
            ROOT / "revenant_assets/char_select_revenant.png",
        ],
        ROOT / "revenant_assets/revenant_art_qa_contact_sheet.png",
    )


if __name__ == "__main__":
    main()
