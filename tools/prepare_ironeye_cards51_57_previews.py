from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
GENERATED = Path(
    r"C:\Users\wenti\.codex\generated_images\019f6b05-d0b0-7af1-a624-760fba7be29d"
)
PREVIEW = ROOT / "design" / "卡图预览" / "铁之眼_51-57_2026-08-03"

ART = {
    "death_mark.png": "exec-ef7e707a-e966-4b30-844f-cfd5eae00245.png",
    "final_battle.png": "exec-3dad2bf8-5cb5-489f-8758-127665b1100a.png",
    "hunting_prelude.png": "exec-420a9c0d-bb89-4fd5-8193-121e7cc8b057.png",
    "hunt.png": "exec-03e94275-d743-47c6-8be9-9b8dc4bed0dc.png",
    "wave_walking.png": "exec-401ecd50-ee43-46a4-a400-8a40452a205a.png",
    "arrow_on_string.png": "exec-a1299e15-c33b-4cc7-99fe-2da50cbed480.png",
    "wither_and_flourish.png": "exec-bdf02632-a955-483a-b888-3db5a5d79367.png",
}

LABELS = {
    "death_mark.png": "51  死亡标记",
    "final_battle.png": "52  终局一战",
    "hunting_prelude.png": "53  狩猎序幕",
    "hunt.png": "54  猎获",
    "wave_walking.png": "55  凌波微步",
    "arrow_on_string.png": "56  弦上箭",
    "wither_and_flourish.png": "57  枯荣相生",
}


def crop_card(source: Path) -> Image.Image:
    image = Image.open(source).convert("RGB")
    target_ratio = 1000 / 760
    ratio = image.width / image.height
    if ratio > target_ratio:
        width = round(image.height * target_ratio)
        left = (image.width - width) // 2
        image = image.crop((left, 0, left + width, image.height))
    else:
        height = round(image.width / target_ratio)
        top = (image.height - height) // 2
        image = image.crop((0, top, image.width, top + height))
    return image.resize((1000, 760), Image.Resampling.LANCZOS)


def main() -> None:
    PREVIEW.mkdir(parents=True, exist_ok=True)
    images: list[tuple[str, Image.Image]] = []
    for filename, generated_name in ART.items():
        artwork = crop_card(GENERATED / generated_name)
        artwork.save(PREVIEW / filename, quality=95)
        images.append((filename, artwork))

    thumb_w, thumb_h = 500, 380
    label_h = 54
    columns = 2
    rows = (len(images) + columns - 1) // columns
    sheet = Image.new(
        "RGB",
        (columns * thumb_w, rows * (thumb_h + label_h)),
        (20, 24, 29),
    )
    draw = ImageDraw.Draw(sheet)
    for index, (filename, artwork) in enumerate(images):
        x = (index % columns) * thumb_w
        y = (index // columns) * (thumb_h + label_h)
        sheet.paste(
            artwork.resize((thumb_w, thumb_h), Image.Resampling.LANCZOS),
            (x, y),
        )
        draw.text(
            (x + 18, y + thumb_h + 12),
            LABELS[filename],
            fill=(224, 231, 213),
        )
    sheet.save(PREVIEW / "铁之眼_51-57_候选总览.png", quality=95)


if __name__ == "__main__":
    main()
