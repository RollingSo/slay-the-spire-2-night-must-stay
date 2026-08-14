from pathlib import Path
from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
GENERATED = Path(
    r"C:\Users\wenti\.codex\generated_images\019f6b05-d0b0-7af1-a624-760fba7be29d"
)
PREVIEW = ROOT / "design" / "卡图预览" / "铁之眼_42-50_2026-08-02"
PORTRAITS = ROOT / "images" / "packed" / "card_portraits" / "ironeye"

ART = {
    "approaching_venom_fang.png": "exec-ee49de8b-0722-44e3-b2d0-689e37566756.png",
    "all_things_wither.png": "exec-e9f5a230-b4f9-4dcc-bcae-183f17d17e84.png",
    "advance_and_retreat.png": "exec-d73832c9-11d0-43f8-abe3-ed511423facd.png",
    "vigilance.png": "exec-92add4db-0660-45c4-9034-5ce2d8a2010b.png",
    "road_already_traveled.png": "exec-0e3a8f7a-6ffb-46c5-be5d-bc70c028c9a9.png",
    "heavenly_eye_form.png": "exec-6938438b-92be-4978-ab62-1c486feb7489.png",
    "shared_intelligence.png": "exec-d7b29979-48d1-4324-a7b0-6ae96a791a19.png",
    "iron_eye.png": "exec-f292724e-7ae0-4862-8ca3-caf056c986c6.png",
    "observation.png": "exec-5fddfcaa-d733-4831-a3bc-9d4b361cf7d1.png",
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


def save_art() -> None:
    PREVIEW.mkdir(parents=True, exist_ok=True)
    PORTRAITS.mkdir(parents=True, exist_ok=True)
    for filename, generated_name in ART.items():
        image = crop_card(GENERATED / generated_name)
        image.save(PREVIEW / filename)
        image.save(PORTRAITS / filename)


def base_icon(accent=(194, 218, 55, 255)) -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.ellipse(
        (27, 27, 229, 229),
        fill=(17, 25, 31, 240),
        outline=accent,
        width=13,
    )
    return image, draw


def draw_power_icons() -> None:
    images_power = ROOT / "images" / "powers"
    compact_power = ROOT / "powers"
    images_atlas = ROOT / "images" / "atlases" / "power_atlas.sprites"
    compact_atlas = ROOT / "atlases" / "power_atlas.sprites"
    for directory in (images_power, compact_power, images_atlas, compact_atlas):
        directory.mkdir(parents=True, exist_ok=True)

    icons: dict[str, Image.Image] = {}

    image, d = base_icon()
    d.arc((49, 53, 208, 214), 210, 342, fill=(208, 227, 73, 255), width=24)
    d.polygon([(76, 130), (42, 151), (84, 166)], fill=(208, 227, 73, 255))
    d.ellipse((139, 72, 187, 120), fill=(116, 145, 42, 255))
    icons["approaching_venom_fang_power"] = image

    image, d = base_icon()
    for x, y, radius in ((91, 98, 28), (153, 92, 22), (132, 151, 35)):
        d.ellipse((x - radius, y - radius, x + radius, y + radius),
                  fill=(150, 186, 37, 230), outline=(222, 239, 86, 255), width=5)
    d.line((64, 192, 192, 64), fill=(224, 236, 201, 255), width=13)
    icons["all_things_wither_power"] = image

    image, d = base_icon()
    d.line((62, 178, 197, 178), fill=(217, 225, 194, 255), width=11)
    d.line((74, 154, 112, 112, 143, 139, 190, 75), fill=(197, 220, 62, 255), width=15)
    d.ellipse((64, 163, 88, 187), fill=(79, 196, 201, 255))
    icons["road_already_traveled_power"] = image

    image, d = base_icon((79, 196, 201, 255))
    d.ellipse((66, 85, 190, 171), outline=(207, 229, 77, 255), width=13)
    d.ellipse((105, 101, 151, 155), fill=(207, 229, 77, 255))
    d.polygon([(128, 45), (143, 78), (128, 69), (113, 78)], fill=(238, 232, 189, 255))
    icons["heavenly_eye_form_power"] = image

    image, d = base_icon((79, 196, 201, 255))
    d.ellipse((53, 85, 119, 151), fill=(202, 221, 62, 255))
    d.ellipse((137, 85, 203, 151), fill=(202, 221, 62, 255))
    d.line((105, 118, 151, 118), fill=(230, 236, 207, 255), width=12)
    d.polygon([(128, 182), (98, 145), (117, 144), (117, 118), (139, 118), (139, 144), (158, 145)],
              fill=(79, 196, 201, 255))
    icons["shared_intelligence_power"] = image

    image, d = base_icon()
    d.polygon([(51, 128), (87, 84), (128, 70), (169, 84), (205, 128),
               (169, 172), (128, 186), (87, 172)], outline=(215, 229, 79, 255), width=11)
    d.ellipse((94, 94, 162, 162), fill=(60, 93, 73, 255), outline=(222, 237, 103, 255), width=8)
    d.ellipse((116, 106, 140, 150), fill=(186, 228, 255, 255))
    icons["iron_eye_power"] = image

    image, d = base_icon((223, 230, 190, 255))
    d.ellipse((59, 78, 197, 172), outline=(202, 222, 67, 255), width=14)
    d.ellipse((104, 96, 152, 154), fill=(79, 196, 201, 255))
    d.line((92, 193, 164, 193), fill=(202, 222, 67, 255), width=10)
    d.polygon([(128, 166), (107, 190), (149, 190)], fill=(202, 222, 67, 255))
    icons["observation_power"] = image

    for name, icon in icons.items():
        icon.save(images_power / f"{name}.png")
        icon.save(compact_power / f"{name}.png")
        tres = (
            '[gd_resource type="AtlasTexture" load_steps=2 format=3]\n\n'
            f'[ext_resource type="Texture2D" path="res://images/powers/{name}.png" id="1_icon"]\n\n'
            '[resource]\n'
            'atlas = ExtResource("1_icon")\n'
            'region = Rect2(0, 0, 256, 256)\n'
        )
        (images_atlas / f"{name}.tres").write_text(tres, encoding="utf-8")
        (compact_atlas / f"{name}.tres").write_text(tres, encoding="utf-8")


def save_contact_sheet() -> None:
    sheet = Image.new("RGB", (750, 570), (28, 31, 34))
    for index, filename in enumerate(ART):
        image = Image.open(PREVIEW / filename).convert("RGB")
        image.thumbnail((240, 182), Image.Resampling.LANCZOS)
        x = (index % 3) * 250 + 5
        y = (index // 3) * 190 + 4
        sheet.paste(image, (x, y))
    sheet.save(PREVIEW / "contact_sheet.jpg", quality=92)


if __name__ == "__main__":
    save_art()
    draw_power_icons()
    save_contact_sheet()
