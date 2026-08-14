from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE_DIRECTORY = ROOT / "design" / "卡图预览" / "2026-07-21_两张新卡与盾牌冲击"
RUNTIME_DIRECTORIES = (
    ROOT / "packed" / "card_portraits" / "guardian",
    ROOT / "images" / "packed" / "card_portraits" / "guardian",
)
PREVIEW_SIZE = (1000, 760)
RUNTIME_SIZE = (1000, 700)
CARD_NAMES = (
    "final_curtain_halberd",
    "fearless",
    "shield_impact",
)


def crop_to_ratio(image: Image.Image, width: int, height: int) -> Image.Image:
    target_ratio = width / height
    source_ratio = image.width / image.height
    if source_ratio > target_ratio:
        crop_width = round(image.height * target_ratio)
        left = (image.width - crop_width) // 2
        box = (left, 0, left + crop_width, image.height)
    else:
        crop_height = round(image.width / target_ratio)
        top = (image.height - crop_height) // 2
        box = (0, top, image.width, top + crop_height)
    return image.convert("RGB").crop(box).resize((width, height), Image.Resampling.LANCZOS)


def main() -> None:
    for directory in RUNTIME_DIRECTORIES:
        directory.mkdir(parents=True, exist_ok=True)

    for card_name in CARD_NAMES:
        source_path = SOURCE_DIRECTORY / f"{card_name}_raw.png"
        with Image.open(source_path) as source:
            preview = crop_to_ratio(source, *PREVIEW_SIZE)

        preview_path = SOURCE_DIRECTORY / f"{card_name}_1000x760.png"
        preview.save(preview_path, "PNG", optimize=True)
        print(preview_path)

        top = (PREVIEW_SIZE[1] - RUNTIME_SIZE[1]) // 2
        runtime = preview.crop((0, top, RUNTIME_SIZE[0], top + RUNTIME_SIZE[1]))
        for directory in RUNTIME_DIRECTORIES:
            runtime_path = directory / f"{card_name}.png"
            runtime.save(runtime_path, "PNG", optimize=True)
            print(runtime_path)


if __name__ == "__main__":
    main()
