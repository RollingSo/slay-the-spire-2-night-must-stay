from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
GENERATED = Path(
    r"C:\Users\wenti\.codex\generated_images\019f6b05-d0b0-7af1-a624-760fba7be29d"
)
PREVIEW = ROOT / "design" / "卡图预览" / "铁之眼_71-77_2026-08-05"
PORTRAITS = ROOT / "images" / "packed" / "card_portraits" / "ironeye"

ART = {
    "irresistible_force.png": "exec-cfc5c1d9-886b-4ab8-ae81-1ab320855cc0.png",
    "fatal_blade_edge.png": "exec-1690240d-1c2d-4e18-9fc6-f571462fc2cc.png",
    "release.png": "exec-0a58ea02-fa24-4fe4-ae21-339d1a9dc4e2.png",
    "emergency_nocking.png": "exec-a0882902-7a66-4852-92c4-30370ad30038.png",
    "adaptation.png": "exec-02fd65e2-eb96-46d4-a1e5-ef95c9c39a8b.png",
    "cloud_piercing_arrow.png": "exec-f676e54c-bcb9-45a8-91ee-9fdf8c814c1e.png",
    "calibration.png": "exec-207a1601-1488-44fe-822b-1154c88e9718.png",
}


def crop_card(source: Path) -> Image.Image:
    image = Image.open(source).convert("RGBA")
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


def prepare_previews() -> None:
    PREVIEW.mkdir(parents=True, exist_ok=True)
    for filename, generated_name in ART.items():
        crop_card(GENERATED / generated_name).save(PREVIEW / filename)

    crop_card(PORTRAITS / "air_rending_arrow.png").save(PREVIEW / "skybreaker.png")

    labels = [
        ("skybreaker.png", "破空（复用现有画面）"),
        ("irresistible_force.png", "锐不可当"),
        ("fatal_blade_edge.png", "致命刃芒"),
        ("release.png", "解脱"),
        ("emergency_nocking.png", "紧急上弦"),
        ("calibration.png", "校准"),
        ("cloud_piercing_arrow.png", "穿云箭"),
        ("adaptation.png", "应变"),
    ]
    thumb_size = (500, 380)
    label_height = 48
    sheet = Image.new("RGB", (1000, (380 + label_height) * 4), (20, 24, 28))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.truetype(r"C:\Windows\Fonts\msyh.ttc", 26)
    for index, (filename, label) in enumerate(labels):
        x = (index % 2) * thumb_size[0]
        y = (index // 2) * (thumb_size[1] + label_height)
        thumb = Image.open(PREVIEW / filename).convert("RGB").resize(
            thumb_size, Image.Resampling.LANCZOS)
        sheet.paste(thumb, (x, y))
        draw.rectangle((x, y + 380, x + 500, y + 428), fill=(20, 24, 28))
        draw.text((x + 14, y + 387), label, font=font, fill=(235, 228, 205))
    sheet.save(PREVIEW / "preview_sheet.jpg", quality=92)


def base_icon(accent: tuple[int, int, int, int]) -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.ellipse(
        (27, 27, 229, 229),
        fill=(17, 25, 31, 240),
        outline=accent,
        width=13,
    )
    return image, draw


def draw_power_icons() -> dict[str, Image.Image]:
    icons: dict[str, Image.Image] = {}

    image, draw = base_icon((202, 222, 67, 255))
    draw.polygon(
        [(42, 128), (142, 82), (125, 112), (213, 112),
         (213, 144), (125, 144), (142, 174)],
        fill=(202, 222, 67, 255),
    )
    draw.polygon([(103, 57), (126, 91), (111, 91), (132, 126),
                  (96, 102), (112, 102)], fill=(79, 196, 201, 255))
    icons["skybreaker_power"] = image

    image, draw = base_icon((79, 196, 201, 255))
    draw.arc((45, 42, 211, 220), 205, 344, fill=(214, 220, 190, 255), width=24)
    draw.line((80, 181, 176, 79), fill=(202, 222, 67, 255), width=15)
    draw.line((80, 79, 176, 181), fill=(202, 222, 67, 255), width=15)
    draw.ellipse((116, 116, 140, 140), fill=(244, 239, 212, 255))
    icons["fatal_blade_edge_power"] = image

    return icons


def install() -> None:
    prepare_previews()
    PORTRAITS.mkdir(parents=True, exist_ok=True)
    for filename in ART:
        Image.open(PREVIEW / filename).save(PORTRAITS / filename)

    power_dirs = (ROOT / "images" / "powers", ROOT / "powers")
    atlas_dirs = (
        ROOT / "images" / "atlases" / "power_atlas.sprites",
        ROOT / "atlases" / "power_atlas.sprites",
    )
    for directory in (*power_dirs, *atlas_dirs):
        directory.mkdir(parents=True, exist_ok=True)

    for name, icon in draw_power_icons().items():
        for directory in power_dirs:
            icon.save(directory / f"{name}.png")
        tres = (
            '[gd_resource type="AtlasTexture" load_steps=2 format=3]\n\n'
            f'[ext_resource type="Texture2D" path="res://images/powers/{name}.png" id="1_icon"]\n\n'
            '[resource]\n'
            'atlas = ExtResource("1_icon")\n'
            'region = Rect2(0, 0, 256, 256)\n'
        )
        for directory in atlas_dirs:
            (directory / f"{name}.tres").write_text(tres, encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--install", action="store_true")
    args = parser.parse_args()
    if args.install:
        install()
    else:
        prepare_previews()


if __name__ == "__main__":
    main()
