from pathlib import Path

from PIL import Image, ImageOps


ROOT = Path(__file__).resolve().parents[1]
GENERATED = Path(
    r"C:\Users\wenti\.codex\generated_images"
    r"\019f6b05-d0b0-7af1-a624-760fba7be29d"
)

CARD_ART = {
    "heavy_halberd.png": "call_FN3n6e02SZxBafnrFvxJ9cp3.png",
    "featherstep.png": "call_6vEPeoNsXRMSO3C9AD4BNwc4.png",
    "dust_return_slash.png": "call_0z9zbreLxQEhHx9huo9ZrIuS.png",
    "eve_of_counterattack.png": "call_t82kXpAvI3WwhHKwPLs7oDof.png",
    "hide_and_seek_stab.png": "call_mlrNxIbecBbSSU5h9YkVrDos.png",
    "cloud_rending_sweep.png": "call_LlC41q1Z2LJFqmXqbyD9I11a.png",
    "circling_gust.png": "call_hBarYtbZRdLjq3lwVFiIWGHW.png",
    "world_ending_wings.png": "call_ZDEASxRmrCvsKjVzKOzytqzv.png",
}


def save_card_art() -> None:
    destinations = (
        ROOT / "design" / "卡图预览",
        ROOT / "images" / "packed" / "card_portraits" / "guardian",
        ROOT / "packed" / "card_portraits" / "guardian",
    )
    for destination in destinations:
        destination.mkdir(parents=True, exist_ok=True)

    for output_name, generated_name in CARD_ART.items():
        source = GENERATED / generated_name
        with Image.open(source) as image:
            card = ImageOps.fit(
                image.convert("RGB"),
                (1000, 760),
                method=Image.Resampling.LANCZOS,
                centering=(0.5, 0.5),
            )
            for destination in destinations:
                card.save(destination / output_name, format="PNG", optimize=True)


def save_featherstep_power_icon() -> None:
    source = ROOT / "tmp" / "featherstep_power_transparent.png"
    with Image.open(source) as image:
        icon = ImageOps.contain(
            image.convert("RGBA"),
            (118, 118),
            method=Image.Resampling.LANCZOS,
        )

    tile = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    tile.alpha_composite(icon, ((128 - icon.width) // 2, (128 - icon.height) // 2))

    preview = ROOT / "design" / "卡图预览" / "飞羽步_能力图标.png"
    preview.parent.mkdir(parents=True, exist_ok=True)
    tile.save(preview, format="PNG", optimize=True)

    atlas_path = ROOT / "guardian_assets" / "guardian_power_atlas.png"
    with Image.open(atlas_path) as atlas_source:
        atlas = atlas_source.convert("RGBA")
    atlas.alpha_composite(tile, (896, 640))
    atlas.save(atlas_path, format="PNG", optimize=True)


if __name__ == "__main__":
    save_card_art()
    save_featherstep_power_icon()
