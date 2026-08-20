from pathlib import Path

from PIL import Image


GENERATED = Path(
    r"C:\Users\wenti\.codex\generated_images\01a00f23-ef57-7810-a22c-4e46ac8529ac"
)
OUTPUT = Path(r"D:\sts-2-mod\revenant_assets\cards")
PREVIEW = Path(r"D:\sts-2-mod\design\卡图预览")

SHEETS = {
    "exec-28fa349f-f1b5-4344-bb7e-32303bf4aa3d.png": [
        "ensemble",
        "underworld_rising",
        "answer_the_call",
        "frenzied_three_fingers",
    ],
    "exec-410af695-c748-4989-9de2-6d4339f2dca1.png": [
        "formation_breaker_hammer",
        "life_and_death",
        "giant_skeleton_wrath",
        "concerto",
    ],
    "exec-83c94dbf-0b7a-475c-98f5-eeb6e2c7cb59.png": [
        "fight_for_me",
        "following_shadow",
        "watchful_waiting",
        "chanting_blessing",
    ],
    "exec-93bab013-1516-45be-80bc-466e65fe7e7c.png": [
        None,
        "surge",
        None,
        "resurgence",
    ],
    "exec-909d6a10-e2bc-4336-8a11-b4781168524d.png": [
        None,
        "revenant_card",
        "soulbound",
        None,
    ],
    "exec-ffdd31d6-abc0-48c8-ad15-d655d917ba80.png": [
        None,
        None,
        None,
        "sky_rending_chord",
    ],
    "exec-880a7385-a070-4f88-9328-1e1ed0ad5dbf.png": [
        "substitute_doll",
        "spirit_gathering",
        None,
        None,
    ],
    "exec-163ab445-83b3-482c-a2d9-9d4c015e676d.png": [
        "soul_cursing_bell",
        "light_spirit",
        "grooming",
        "reanimate_dead",
    ],
    "exec-2729d310-2cf5-477a-91b5-28588bc4d3c2.png": [
        "soul_return",
        "heavy_echo",
        "underworld_reflection",
        "spirit_manipulation",
    ],
    "exec-688b5059-b1e0-47ac-b09b-3c5d23fa3923.png": [
        "preparation_ritual",
        "all_souls_return",
        None,
        None,
    ],
}


def fit_portrait(image: Image.Image) -> Image.Image:
    target_ratio = 1000 / 760
    width, height = image.size
    if width / height > target_ratio:
        crop_width = round(height * target_ratio)
        left = (width - crop_width) // 2
        image = image.crop((left, 0, left + crop_width, height))
    else:
        crop_height = round(width / target_ratio)
        top = (height - crop_height) // 2
        image = image.crop((0, top, width, top + crop_height))
    return image.resize((1000, 760), Image.Resampling.LANCZOS).convert("RGBA")


for filename, names in SHEETS.items():
    sheet = Image.open(GENERATED / filename).convert("RGB")
    half_w, half_h = sheet.width // 2, sheet.height // 2
    gutter = max(4, min(sheet.width, sheet.height) // 256)
    for index, name in enumerate(names):
        if name is None:
            continue
        column, row = index % 2, index // 2
        panel = sheet.crop(
            (
                column * half_w + gutter,
                row * half_h + gutter,
                (column + 1) * half_w - gutter,
                (row + 1) * half_h - gutter,
            )
        )
        portrait = fit_portrait(panel)
        for directory in (OUTPUT, PREVIEW):
            directory.mkdir(parents=True, exist_ok=True)
            portrait.save(directory / f"{name}.png", optimize=True)

print("Created 12 official-family-aligned Revenant card portraits.")
