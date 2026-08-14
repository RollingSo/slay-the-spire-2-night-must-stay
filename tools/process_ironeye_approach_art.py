from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(
    r"C:\Users\wenti\.codex\generated_images\019f6b05-d0b0-7af1-a624-760fba7be29d"
    r"\exec-87068433-d47a-4e60-8598-413df2e7165e.png"
)
PREVIEW = ROOT / "design" / "卡图预览" / "铁之眼_接近_v2_2026-08-06.png"


def main() -> None:
    image = Image.open(SOURCE).convert("RGBA")
    target_ratio = 1000 / 760
    source_ratio = image.width / image.height
    if source_ratio > target_ratio:
        width = round(image.height * target_ratio)
        left = (image.width - width) // 2
        image = image.crop((left, 0, left + width, image.height))
    else:
        height = round(image.width / target_ratio)
        top = (image.height - height) // 2
        image = image.crop((0, top, image.width, top + height))

    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    image.resize((1000, 760), Image.Resampling.LANCZOS).save(PREVIEW)


if __name__ == "__main__":
    main()
