from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
GENERATED = Path(r"C:\Users\wenti\.codex\generated_images\019f6b05-d0b0-7af1-a624-760fba7be29d")
PREVIEW = ROOT / "design" / "卡图预览" / "铁之眼_26-41_2026-07-31"
PORTRAITS = ROOT / "images" / "packed" / "card_portraits" / "ironeye"

ART = {
    "killing_intent_gaze.png": "call_gAlQWgtHxUplKeD53VMMdgSK.png",
    "blade_glide.png": "call_vvmKFJLoIy86AZxX2QigcdCp.png",
    "star_plucker.png": "call_zNn6tbnnzOfKrGT7UcP0uzT8.png",
    "lightning_arrowhead.png": "call_wcBYTPK9VcAZo5Z1SAXQ0GZp.png",
    "bow_like_full_moon.png": "call_qnnKtmxoKg7Im82d5DoEMMMO.png",
    "blade_shadow_unmatched.png": "call_WKXDSlBBcWfS69NJA4Ms1CSX.png",
    "circling_maneuver.png": "call_HLxXC7Xq98PvACtgtuMYES1J.png",
    "wavering_step.png": "call_0BUu0jFic47r5aGyz71BUO0k.png",
    "return_to_zero.png": "call_ANaQUZ1DqE9G2YtT69gCYayJ.png",
    "retreat_step.png": "call_y5n0w1sf1ycUN4GXhZQuvpmt.png",
    "withering_slash.png": "call_Aqhvdlw5ebVHZwQQqeasWsij.png",
    "poison_mist_arrow_array.png": "call_zvGAwi0ARzWtLHaInMtBPgex.png",
    "bow_combat_art.png": "call_Of7HEN9oUuauJ35uAgFegsWH.png",
    "scouting.png": "call_aNNyCi4wyvtyHCXIyBIFfhN6.png",
    "poison_blade.png": "call_YswuuwZ54Pxe1XJzVPE0rOQE.png",
    "aim.png": "call_uA0hiX8ishCx7zFY1ujoWiu9.png",
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

    old = PORTRAITS / "poison_burst.png"
    crop_card(old).save(PREVIEW / "withering_arrow.png")


def normalize_character_icon(
    source: Path,
    destination: Path,
    content_size: tuple[int, int],
) -> None:
    image = Image.open(source).convert("RGBA")
    alpha = image.getchannel("A")
    bbox = alpha.getbbox()
    if bbox is None:
        raise RuntimeError(f"No visible content in {source}")
    subject = image.crop(bbox)
    subject.thumbnail(content_size, Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    x = (64 - subject.width) // 2
    y = (64 - subject.height) // 2
    canvas.alpha_composite(subject, (x, y))
    canvas.save(destination)


def normalize_avatars() -> None:
    pairs = [
        (
            ROOT / "ironeye_assets" / "character_icon_ironeye.png",
            ROOT / "ironeye_assets" / "character_icon_ironeye.png",
            (56, 58),
        ),
        (
            ROOT / "ironeye_assets" / "character_icon_ironeye_outline.png",
            ROOT / "ironeye_assets" / "character_icon_ironeye_outline.png",
            (56, 58),
        ),
        (
            ROOT / "guardian_assets" / "character_icon_guardian.png",
            ROOT / "guardian_assets" / "character_icon_guardian.png",
            (49, 55),
        ),
        (
            ROOT / "guardian_assets" / "character_icon_guardian_outline.png",
            ROOT / "guardian_assets" / "character_icon_guardian_outline.png",
            (49, 55),
        ),
    ]
    for source, destination, size in pairs:
        normalize_character_icon(source, destination, size)


def normalize_map_marker(
    source: Path,
    destination: Path,
    content_size: tuple[int, int],
) -> None:
    image = Image.open(source).convert("RGBA")
    alpha = image.getchannel("A")
    bbox = alpha.getbbox()
    if bbox is None:
        raise RuntimeError(f"No visible content in {source}")
    subject = image.crop(bbox)
    subject.thumbnail(content_size, Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (49, 64), (0, 0, 0, 0))
    x = (49 - subject.width) // 2
    y = (64 - subject.height) // 2
    canvas.alpha_composite(subject, (x, y))
    canvas.save(destination)


def normalize_map_markers() -> None:
    # The original map markers use a 49x64 transparent canvas. IronEye's old
    # marker was a 1254px opaque portrait, while Guardian's subject occupied
    # only a small corner of an otherwise-correct canvas.
    normalize_map_marker(
        ROOT / "ironeye_assets" / "character_icon_ironeye.png",
        ROOT / "ironeye_assets" / "map_marker_ironeye.png",
        (43, 54),
    )
    normalize_map_marker(
        ROOT / "guardian_assets" / "map_marker_guardian.png",
        ROOT / "guardian_assets" / "map_marker_guardian.png",
        (42, 44),
    )


def draw_power_icons() -> None:
    power_dir = ROOT / "images" / "powers"
    compact_dir = ROOT / "powers"
    atlas_dir = ROOT / "images" / "atlases" / "power_atlas.sprites"
    for directory in (power_dir, compact_dir, atlas_dir):
        directory.mkdir(parents=True, exist_ok=True)

    icons = {}

    image = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    d = ImageDraw.Draw(image)
    d.ellipse((28, 28, 228, 228), fill=(17, 25, 31, 235), outline=(200, 217, 74, 255), width=14)
    d.polygon([(128, 38), (174, 112), (143, 106), (202, 210), (105, 132), (132, 138), (70, 45)],
              fill=(209, 232, 67, 255))
    icons["lightning_arrowhead_power"] = image

    image = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    d = ImageDraw.Draw(image)
    d.ellipse((42, 42, 214, 214), fill=(230, 226, 189, 255), outline=(19, 28, 35, 255), width=12)
    d.arc((30, 30, 226, 226), 200, 340, fill=(117, 129, 75, 255), width=22)
    d.line((54, 163, 202, 163), fill=(200, 217, 74, 255), width=9)
    icons["bow_like_full_moon_power"] = image

    image = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    d = ImageDraw.Draw(image)
    d.ellipse((30, 30, 226, 226), fill=(18, 26, 33, 235), outline=(79, 196, 201, 255), width=12)
    d.polygon([(54, 182), (110, 70), (132, 94), (94, 198)], fill=(119, 129, 80, 255))
    d.polygon([(202, 182), (146, 70), (124, 94), (162, 198)], fill=(200, 217, 74, 255))
    icons["blade_shadow_unmatched_power"] = image

    image = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    d = ImageDraw.Draw(image)
    d.ellipse((26, 26, 230, 230), fill=(20, 28, 35, 235), outline=(205, 225, 63, 255), width=12)
    d.polygon([(128, 45), (175, 110), (148, 108), (193, 190), (128, 154), (63, 190), (108, 108), (81, 110)],
              fill=(190, 220, 48, 255))
    d.ellipse((101, 101, 155, 155), fill=(28, 40, 35, 255))
    icons["poison_burst_power"] = image

    image = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    d = ImageDraw.Draw(image)
    d.ellipse((28, 28, 228, 228), fill=(18, 27, 33, 235), outline=(79, 196, 201, 255), width=12)
    d.polygon([(54, 128), (112, 74), (112, 109), (202, 109), (202, 147), (112, 147), (112, 182)],
              fill=(200, 217, 74, 255))
    icons["next_turn_distance_power"] = image

    image = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    d = ImageDraw.Draw(image)
    d.ellipse((28, 28, 228, 228), fill=(18, 27, 33, 235), outline=(200, 217, 74, 255), width=12)
    d.polygon([(50, 162), (164, 66), (200, 88), (131, 127), (93, 198)], fill=(190, 203, 184, 255))
    d.polygon([(84, 181), (114, 138), (142, 150), (110, 205)], fill=(198, 222, 55, 255))
    icons["poison_blade_power"] = image

    for name, icon in icons.items():
        for directory in (power_dir, compact_dir):
            icon.save(directory / f"{name}.png")
        (atlas_dir / f"{name}.tres").write_text(
            "[gd_resource type=\"AtlasTexture\" load_steps=2 format=3]\n\n"
            f"[ext_resource type=\"Texture2D\" path=\"res://images/powers/{name}.png\" id=\"1_icon\"]\n\n"
            "[resource]\n"
            "atlas = ExtResource(\"1_icon\")\n"
            "region = Rect2(0, 0, 256, 256)\n",
            encoding="utf-8",
        )


if __name__ == "__main__":
    save_art()
    normalize_avatars()
    normalize_map_markers()
    draw_power_icons()
