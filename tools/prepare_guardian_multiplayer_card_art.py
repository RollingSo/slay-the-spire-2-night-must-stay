from pathlib import Path

from PIL import Image, ImageOps


ROOT = Path(__file__).resolve().parents[1]
GENERATED = Path(r"C:\Users\wenti\.codex\generated_images\019f6b05-d0b0-7af1-a624-760fba7be29d")

CARD_ART = {
    "step_forward_for_all.png": GENERATED / "exec-b8a2a185-d9e5-44e0-b60a-db01da1e249b.png",
    "guardian_multiplayer_card.png": GENERATED / "exec-bc1495cf-233e-4dc3-b1e5-e8369c29a09c.png",
}

POWER_ICON_SOURCE = GENERATED / "exec-f8f965a0-4b48-4de0-a64f-c98d26d4d8d4.png"
POWER_ATLAS = ROOT / "guardian_assets" / "guardian_power_atlas.png"
POWER_REGION = (768, 640)


def prepare_card_art() -> None:
    destinations = (
        ROOT / "design" / "卡图预览",
        ROOT / "images" / "packed" / "card_portraits" / "guardian",
        ROOT / "packed" / "card_portraits" / "guardian",
    )
    for filename, source in CARD_ART.items():
        with Image.open(source) as image:
            prepared = ImageOps.fit(
                image.convert("RGB"),
                (1000, 760),
                method=Image.Resampling.LANCZOS,
                centering=(0.5, 0.5),
            )
        for destination in destinations:
            destination.mkdir(parents=True, exist_ok=True)
            prepared.save(destination / filename, optimize=True)


def remove_checkerboard(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    pixels = []
    for red, green, blue, _ in rgba.getdata():
        # Image generation returned a baked light checkerboard. Its squares are
        # nearly neutral and bright; the colored emblem is saturated or dark.
        neutral_bright = max(red, green, blue) - min(red, green, blue) <= 12 and min(red, green, blue) >= 205
        pixels.append((red, green, blue, 0 if neutral_bright else 255))
    rgba.putdata(pixels)
    return rgba


def prepare_power_icon() -> None:
    with Image.open(POWER_ICON_SOURCE) as image:
        transparent = remove_checkerboard(image)

    alpha_box = transparent.getchannel("A").getbbox()
    if alpha_box is None:
        raise RuntimeError("Guardian multiplayer power icon has no foreground pixels")

    foreground = transparent.crop(alpha_box)
    foreground.thumbnail((116, 116), Image.Resampling.LANCZOS)
    icon = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    icon.alpha_composite(
        foreground,
        ((128 - foreground.width) // 2, (128 - foreground.height) // 2),
    )

    preview = ROOT / "design" / "卡图预览" / "守护者_多人能力图标.png"
    icon.save(preview, optimize=True)

    with Image.open(POWER_ATLAS) as atlas_image:
        atlas = atlas_image.convert("RGBA")
    atlas.alpha_composite(icon, POWER_REGION)
    atlas.save(POWER_ATLAS, optimize=True)


if __name__ == "__main__":
    prepare_card_art()
    prepare_power_icon()

