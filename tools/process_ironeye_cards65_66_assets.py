from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
PREVIEW_DIR = ROOT / "design" / "卡图预览" / "铁之眼_65-66_2026-08-05"
PORTRAIT_DIR = ROOT / "images" / "packed" / "card_portraits" / "ironeye"
POWER_DIR = ROOT / "images" / "powers"


def crop_to_ratio(image: Image.Image, ratio: float) -> Image.Image:
    current = image.width / image.height
    if current > ratio:
        width = round(image.height * ratio)
        left = (image.width - width) // 2
        return image.crop((left, 0, left + width, image.height))
    height = round(image.width / ratio)
    top = (image.height - height) // 2
    return image.crop((0, top, image.width, top + height))


def install_card(source: Path, slug: str, preview_only: bool) -> Image.Image:
    image = Image.open(source).convert("RGB")
    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    image.save(PREVIEW_DIR / f"{slug}_source.png")
    final = crop_to_ratio(image, 1000 / 760).resize(
        (1000, 760), Image.Resampling.LANCZOS
    )
    final.save(PREVIEW_DIR / f"{slug}.png")
    if not preview_only:
        PORTRAIT_DIR.mkdir(parents=True, exist_ok=True)
        final.save(PORTRAIT_DIR / f"{slug}.png")
    return image


def install_poison_scheme_power_icon(source: Image.Image) -> None:
    side = round(min(source.width, source.height) * 0.52)
    center_x = round(source.width * 0.27)
    center_y = round(source.height * 0.67)
    left = max(0, min(source.width - side, center_x - side // 2))
    top = max(0, min(source.height - side, center_y - side // 2))
    icon = source.crop((left, top, left + side, top + side)).resize(
        (256, 256), Image.Resampling.LANCZOS
    )
    POWER_DIR.mkdir(parents=True, exist_ok=True)
    icon.save(POWER_DIR / "poison_scheme_power.png")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--lucky-arrow-bag", type=Path, required=True)
    parser.add_argument("--poison-scheme", type=Path, required=True)
    parser.add_argument("--preview-only", action="store_true")
    args = parser.parse_args()

    install_card(args.lucky_arrow_bag, "lucky_arrow_bag", args.preview_only)
    poison_scheme = install_card(
        args.poison_scheme, "poison_scheme", args.preview_only
    )
    if not args.preview_only:
        install_poison_scheme_power_icon(poison_scheme)


if __name__ == "__main__":
    main()
