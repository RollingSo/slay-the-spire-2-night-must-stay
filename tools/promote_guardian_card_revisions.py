from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
PREVIEW = ROOT / "design" / "卡图预览"
NORMALIZED_PREVIEW = PREVIEW / "1000x700_无黑边"
RUNTIME_DIRECTORIES = (
    ROOT / "packed" / "card_portraits" / "guardian",
    ROOT / "images" / "packed" / "card_portraits" / "guardian",
)
TARGET_SIZE = (1000, 700)

REVISIONS = {
    "strike_guardian": PREVIEW / "strike_guardian_revision.png",
    "defend_guardian": PREVIEW / "defend_guardian_revision.png",
    "wandering_spell_soul": PREVIEW / "wandering_spell_soul_revision.png",
    "guardian_charge": PREVIEW / "guardian_charge_revision.png",
}


def center_crop(image: Image.Image) -> Image.Image:
    if image.width < TARGET_SIZE[0] or image.height < TARGET_SIZE[1]:
        raise ValueError(
            f"Revision is smaller than {TARGET_SIZE[0]}x{TARGET_SIZE[1]}: "
            f"{image.width}x{image.height}"
        )

    left = (image.width - TARGET_SIZE[0]) // 2
    top = (image.height - TARGET_SIZE[1]) // 2
    return image.convert("RGB").crop(
        (left, top, left + TARGET_SIZE[0], top + TARGET_SIZE[1])
    )


def main() -> None:
    NORMALIZED_PREVIEW.mkdir(parents=True, exist_ok=True)
    for directory in RUNTIME_DIRECTORIES:
        directory.mkdir(parents=True, exist_ok=True)

    for card_name, source_path in REVISIONS.items():
        if not source_path.exists():
            raise FileNotFoundError(source_path)

        with Image.open(source_path) as source:
            artwork = center_crop(source)

        destinations = [
            NORMALIZED_PREVIEW / f"{card_name}.png",
            *(directory / f"{card_name}.png" for directory in RUNTIME_DIRECTORIES),
        ]
        for destination in destinations:
            artwork.save(destination, "PNG", optimize=True)
            print(destination)


if __name__ == "__main__":
    main()
