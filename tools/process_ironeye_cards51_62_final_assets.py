from pathlib import Path
from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
GENERATED = Path(
    r"C:\Users\wenti\.codex\generated_images\019f6b05-d0b0-7af1-a624-760fba7be29d"
)
PREVIEW_51_57 = ROOT / "design" / "卡图预览" / "铁之眼_51-57_2026-08-03"
PREVIEW_58_62 = ROOT / "design" / "卡图预览" / "铁之眼_58-62_2026-08-03"
PORTRAITS = ROOT / "images" / "packed" / "card_portraits" / "ironeye"

ART_51_57 = (
    "death_mark.png",
    "final_battle.png",
    "hunting_prelude.png",
    "hunt.png",
    "wave_walking.png",
    "arrow_on_string.png",
    "wither_and_flourish.png",
)

ART_58_62 = {
    "throat_seal.png": "exec-2297f216-ef56-407b-886e-6174e8978ff4.png",
    "nowhere_to_hide.png": "exec-25197643-a4ad-422d-b321-448135ca50e8.png",
    "willow_piercing_arrow.png": "exec-ec948269-48e7-496e-9162-788424a4d57b.png",
    "volatile_poison.png": "exec-55e3a94e-fa9c-4e4b-8b56-d49be3b77177.png",
    "tracking_arrow.png": "exec-061eb77d-4f53-4545-8abd-ab709b05df47.png",
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


def save_card_art() -> None:
    PREVIEW_58_62.mkdir(parents=True, exist_ok=True)
    PORTRAITS.mkdir(parents=True, exist_ok=True)

    for filename in ART_51_57:
        image = Image.open(PREVIEW_51_57 / filename).convert("RGBA")
        if image.size != (1000, 760):
            image = crop_card(PREVIEW_51_57 / filename)
        image.save(PORTRAITS / filename)

    for filename, generated_name in ART_58_62.items():
        image = crop_card(GENERATED / generated_name)
        image.save(PREVIEW_58_62 / filename)
        image.save(PORTRAITS / filename)

    sheet = Image.new("RGB", (750, 380), (24, 28, 34))
    for index, filename in enumerate(ART_58_62):
        image = Image.open(PREVIEW_58_62 / filename).convert("RGB")
        image.thumbnail((240, 182), Image.Resampling.LANCZOS)
        x = (index % 3) * 250 + 5
        y = (index // 3) * 190 + 4
        sheet.paste(image, (x, y))
    sheet.save(PREVIEW_58_62 / "铁之眼_58-62_候选总览.png")


def base_icon(accent=(205, 231, 63, 255)) -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.ellipse((25, 25, 231, 231), fill=(16, 24, 30, 240), outline=accent, width=13)
    return image, draw


def eye(draw: ImageDraw.ImageDraw, box=(63, 83, 193, 169), accent=(205, 231, 63, 255)) -> None:
    draw.ellipse(box, outline=accent, width=13)
    cx = (box[0] + box[2]) // 2
    cy = (box[1] + box[3]) // 2
    draw.polygon([(cx, box[1] + 12), (cx + 20, cy), (cx, box[3] - 12), (cx - 20, cy)], fill=accent)


def draw_power_icons() -> None:
    images_power = ROOT / "images" / "powers"
    compact_power = ROOT / "powers"
    images_atlas = ROOT / "images" / "atlases" / "power_atlas.sprites"
    compact_atlas = ROOT / "atlases" / "power_atlas.sprites"
    for directory in (images_power, compact_power, images_atlas, compact_atlas):
        directory.mkdir(parents=True, exist_ok=True)

    icons: dict[str, Image.Image] = {}

    image, d = base_icon((215, 228, 201, 255))
    d.rounded_rectangle((66, 66, 190, 190), radius=15, outline=(215, 228, 201, 255), width=14)
    d.line((52, 204, 204, 52), fill=(205, 231, 63, 255), width=19)
    icons["final_battle_no_block_power"] = image

    image, d = base_icon()
    eye(d)
    d.polygon([(164, 173), (211, 173), (211, 202), (233, 181), (211, 160), (211, 188), (164, 188)], fill=(78, 196, 202, 255))
    icons["hunt_power"] = image

    image, d = base_icon((78, 196, 202, 255))
    for y in (84, 121, 158):
        d.arc((47, y - 25, 209, y + 25), 195, 345, fill=(205, 231, 63, 255), width=13)
    d.polygon([(127, 52), (96, 131), (127, 116), (108, 204), (169, 106), (137, 119)], fill=(238, 231, 184, 255))
    icons["wave_walking_power"] = image

    image, d = base_icon((78, 196, 202, 255))
    d.arc((55, 43, 174, 213), 275, 85, fill=(205, 231, 63, 255), width=17)
    d.line((88, 72, 88, 184), fill=(230, 236, 207, 255), width=9)
    d.line((70, 128, 201, 128), fill=(230, 236, 207, 255), width=11)
    d.polygon([(215, 128), (177, 105), (177, 151)], fill=(205, 231, 63, 255))
    icons["arrow_on_string_power"] = image

    image, d = base_icon((232, 109, 171, 255))
    eye(d, (61, 75, 195, 161))
    for x in (75, 128, 181):
        d.line((128, 151, x, 205), fill=(205, 231, 63, 255), width=11)
        d.ellipse((x - 13, 192, x + 13, 218), fill=(205, 231, 63, 255))
    icons["nowhere_to_hide_power"] = image

    image, d = base_icon((232, 109, 171, 255))
    d.polygon([(85, 49), (165, 49), (152, 100), (183, 170), (153, 204), (91, 204), (67, 170), (102, 100)],
              fill=(31, 36, 42, 255), outline=(230, 234, 212, 255), width=10)
    d.polygon([(79, 166), (111, 116), (122, 148), (146, 103), (148, 153), (184, 134), (161, 187), (94, 193)],
              fill=(205, 231, 63, 255))
    icons["volatile_poison_power"] = image

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


if __name__ == "__main__":
    save_card_art()
    draw_power_icons()
