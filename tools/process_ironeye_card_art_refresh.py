from pathlib import Path

from PIL import Image, ImageDraw, ImageOps


ROOT = Path(__file__).resolve().parents[1]
GENERATED = Path(
    r"C:\Users\wenti\.codex\generated_images\019f6b05-d0b0-7af1-a624-760fba7be29d"
)
PREVIEW = ROOT / "design" / "卡图预览" / "2026-08-02_铁之眼差异化背景重绘"
RUNTIME = ROOT / "images" / "packed" / "card_portraits" / "ironeye"
TARGET_SIZE = (1000, 760)

ARTWORK = {
    "withering_slash": GENERATED / "exec-015400ad-f024-4b04-9c90-61d41540e33e.png",
    "retreat_step": GENERATED / "exec-e8a8cd7f-0b1c-4317-9173-eac18a178a96.png",
    "wavering_step": GENERATED / "exec-1a1233c2-c69c-4918-b38a-20ae505bb9ea.png",
    "poison_mist_arrow_array": GENERATED / "exec-53babb91-6089-46ac-ab5b-7b651b9b32a1.png",
    "bow_like_full_moon": GENERATED / "exec-eb1a6e7a-3265-4f77-86c5-d203bd3d718b.png",
    "bow_combat_art": GENERATED / "exec-514ef2ed-724e-43ea-b7af-7fd08b9b1f1b.png",
    "anti_air_shot": GENERATED / "exec-bbb67fe7-906c-4e66-a916-0cdfe69a055a.png",
}


def normalize(source_path: Path) -> Image.Image:
    if not source_path.exists():
        raise FileNotFoundError(source_path)
    with Image.open(source_path) as source:
        return ImageOps.fit(
            source.convert("RGB"),
            TARGET_SIZE,
            method=Image.Resampling.LANCZOS,
            centering=(0.5, 0.5),
        )


def make_contact_sheet(images: dict[str, Image.Image]) -> None:
    thumb_size = (250, 190)
    label_height = 24
    columns = 4
    rows = (len(images) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * 250, rows * (190 + label_height)), "#171717")
    draw = ImageDraw.Draw(sheet)
    for index, (name, artwork) in enumerate(images.items()):
        x = (index % columns) * 250
        y = (index // columns) * (190 + label_height)
        sheet.paste(artwork.resize(thumb_size, Image.Resampling.LANCZOS), (x, y))
        draw.text((x + 6, y + 194), name, fill="white")
    sheet.save(PREVIEW / "_contact_sheet_250x190.png", "PNG", optimize=True)


def main() -> None:
    PREVIEW.mkdir(parents=True, exist_ok=True)
    RUNTIME.mkdir(parents=True, exist_ok=True)

    normalized: dict[str, Image.Image] = {}
    for card_name, source_path in ARTWORK.items():
        artwork = normalize(source_path)
        normalized[card_name] = artwork
        preview_path = PREVIEW / f"{card_name}_candidate.png"
        runtime_path = RUNTIME / f"{card_name}.png"
        artwork.save(preview_path, "PNG", optimize=True)
        artwork.save(runtime_path, "PNG", optimize=True)
        print(f"{card_name}: {artwork.size} -> {runtime_path}")

    make_contact_sheet(normalized)
    print(PREVIEW / "_contact_sheet_250x190.png")


if __name__ == "__main__":
    main()
