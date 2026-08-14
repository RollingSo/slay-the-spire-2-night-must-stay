from pathlib import Path
from shutil import copy2

from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
GENERATED = Path(
    r"C:\Users\wenti\.codex\generated_images"
    r"\019f6b05-d0b0-7af1-a624-760fba7be29d"
)

CARD_SOURCES = {
    "pierce_the_willow": GENERATED / "call_YbCkaSk4xTNRVNoAL7Q0ovJd.png",
    "heartpiercing_arrow": GENERATED / "call_VIbvIDlBJXPMTokef0Cq4KzM.png",
    "disorderly_arrows": GENERATED / "call_EocgAloWrQItYZGQUJCu0v8w.png",
    "startled_bird": GENERATED / "call_3cALF60Eil7IRXANwMVr0aZa.png",
    "eagle_eye": GENERATED / "call_NhJIfbkSaY8ew8RbwrUvWZjw.png",
}


def crop_to_ratio(image: Image.Image, ratio: float) -> Image.Image:
    width, height = image.size
    current = width / height
    if current > ratio:
        crop_width = round(height * ratio)
        left = (width - crop_width) // 2
        return image.crop((left, 0, left + crop_width, height))
    crop_height = round(width / ratio)
    top = (height - crop_height) // 2
    return image.crop((0, top, width, top + crop_height))


def install_card_portraits() -> None:
    output = ROOT / "images/packed/card_portraits/ironeye"
    preview = ROOT / "design/卡图预览/铁之眼_21-25_2026-07-30"
    output.mkdir(parents=True, exist_ok=True)
    preview.mkdir(parents=True, exist_ok=True)

    for name, source in CARD_SOURCES.items():
        if not source.exists():
            raise FileNotFoundError(source)
        image = Image.open(source).convert("RGB")
        image = crop_to_ratio(image, 1000 / 760)
        image = image.resize((1000, 760), Image.Resampling.LANCZOS)
        image.save(output / f"{name}.png", optimize=True)
        copy2(output / f"{name}.png", preview / f"{name}.png")


def scale_character_select_icon(path: Path, scale: float, shrink: bool) -> None:
    source_dir = ROOT / "tools/source_assets/character_select"
    source_dir.mkdir(parents=True, exist_ok=True)
    source_path = source_dir / path.name
    if source_path.exists():
        image = Image.open(source_path).convert("RGBA")
    else:
        image = Image.open(path).convert("RGBA")
        if shrink:
            # Recover the original full-frame art from the first inset-scale pass.
            image = image.crop((8, 11, 124, 183)).resize(
                (132, 195), Image.Resampling.LANCZOS
            )
        image.save(source_path, optimize=True)

    width, height = image.size
    if shrink:
        background = image.filter(ImageFilter.GaussianBlur(11))
        background = ImageEnhance.Brightness(background).enhance(0.82)
        new_size = (round(width * scale), round(height * scale))
        subject = image.resize(new_size, Image.Resampling.LANCZOS)
        x = (width - new_size[0]) // 2
        y = (height - new_size[1]) // 2
        edge_mask = Image.new("L", new_size, 0)
        ImageDraw.Draw(edge_mask).rectangle(
            (5, 5, new_size[0] - 6, new_size[1] - 6),
            fill=255,
        )
        edge_mask = edge_mask.filter(ImageFilter.GaussianBlur(4))
        edge_mask = ImageChops.multiply(edge_mask, subject.getchannel("A"))
        background.paste(subject, (x, y), edge_mask)
        result = background
    else:
        new_size = (round(width * scale), round(height * scale))
        subject = image.resize(new_size, Image.Resampling.LANCZOS)
        x = (new_size[0] - width) // 2
        y = max(0, (new_size[1] - height) // 2 - 2)
        result = subject.crop((x, y, x + width, y + height))
    result.save(path, optimize=True)


def fix_character_select_scale() -> None:
    for name in ("char_select_guardian.png", "char_select_guardian_locked.png"):
        scale_character_select_icon(ROOT / "guardian_assets" / name, 0.88, True)
    for name in ("char_select_ironeye.png", "char_select_ironeye_locked.png"):
        scale_character_select_icon(ROOT / "ironeye_assets" / name, 1.0, False)


def icon_canvas() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def draw_pierce_the_willow() -> Image.Image:
    image, draw = icon_canvas()
    draw.ellipse((20, 20, 236, 236), fill=(15, 23, 26, 238), outline=(158, 185, 36, 255), width=12)
    draw.polygon(((39, 128), (105, 91), (105, 113), (215, 113), (215, 143), (105, 143), (105, 165)),
                 fill=(231, 225, 181, 255), outline=(17, 23, 23, 255))
    draw.line((92, 63, 166, 193), fill=(205, 231, 37, 255), width=24)
    draw.line((166, 63, 92, 193), fill=(205, 231, 37, 255), width=24)
    draw.line((92, 63, 166, 193), fill=(251, 244, 93, 255), width=7)
    draw.line((166, 63, 92, 193), fill=(251, 244, 93, 255), width=7)
    return image


def draw_disorderly_arrows() -> Image.Image:
    image, draw = icon_canvas()
    draw.ellipse((19, 19, 237, 237), fill=(17, 21, 31, 238), outline=(112, 139, 40, 255), width=11)
    center = (132, 132)
    for start in ((35, 55), (221, 48), (41, 212), (222, 208)):
        draw.line((*start, *center), fill=(224, 220, 184, 255), width=13)
        sx, sy = start
        dx, dy = center[0] - sx, center[1] - sy
        length = max((dx * dx + dy * dy) ** 0.5, 1)
        ux, uy = dx / length, dy / length
        px, py = -uy, ux
        tip = (center[0] - ux * 12, center[1] - uy * 12)
        base = (center[0] - ux * 43, center[1] - uy * 43)
        draw.polygon(
            (
                tip,
                (base[0] + px * 17, base[1] + py * 17),
                (base[0] - px * 17, base[1] - py * 17),
            ),
            fill=(247, 240, 196, 255),
        )
    draw.line((101, 92, 165, 172), fill=(206, 232, 35, 255), width=22)
    draw.line((164, 91, 101, 173), fill=(206, 232, 35, 255), width=22)
    return image


def draw_eagle_eye() -> Image.Image:
    image, draw = icon_canvas()
    draw.ellipse((18, 18, 238, 238), fill=(13, 21, 27, 240), outline=(145, 170, 42, 255), width=12)
    draw.polygon(((43, 132), (78, 91), (128, 72), (180, 91), (216, 132),
                  (178, 164), (128, 178), (78, 164)),
                 fill=(225, 225, 198, 255), outline=(5, 11, 15, 255))
    draw.ellipse((92, 94, 164, 166), fill=(33, 119, 165, 255), outline=(10, 24, 33, 255), width=8)
    draw.ellipse((116, 116, 140, 140), fill=(4, 10, 14, 255))
    draw.arc((55, 55, 201, 201), 205, 335, fill=(210, 234, 41, 255), width=12)
    draw.arc((55, 55, 201, 201), 25, 155, fill=(210, 234, 41, 255), width=12)
    draw.line((128, 31, 128, 79), fill=(210, 234, 41, 255), width=9)
    draw.line((128, 177, 128, 225), fill=(210, 234, 41, 255), width=9)
    draw.line((31, 128, 79, 128), fill=(210, 234, 41, 255), width=9)
    draw.line((177, 128, 225, 128), fill=(210, 234, 41, 255), width=9)
    return image


def install_power_icons() -> None:
    icons = {
        "pierce_the_willow_power": draw_pierce_the_willow(),
        "disorderly_arrows_power": draw_disorderly_arrows(),
        "eagle_eye_power": draw_eagle_eye(),
    }
    for name, image in icons.items():
        for folder in (ROOT / "images/powers", ROOT / "powers"):
            folder.mkdir(parents=True, exist_ok=True)
            image.save(folder / f"{name}.png", optimize=True)


if __name__ == "__main__":
    install_card_portraits()
    fix_character_select_scale()
    install_power_icons()
