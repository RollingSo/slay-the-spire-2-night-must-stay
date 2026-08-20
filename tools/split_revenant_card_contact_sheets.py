from pathlib import Path
from PIL import Image

GENERATED = Path(r"C:\Users\wenti\.codex\generated_images\01a00f23-ef57-7810-a22c-4e46ac8529ac")
OUTPUT = Path(r"D:\sts-2-mod\revenant_assets\cards")
PREVIEW = Path(r"D:\sts-2-mod\design\卡图预览")

SHEETS = {
    "exec-56211307-9f33-4cc9-9322-085a25c3f6b6.png": [
        "ensemble", "surge", "underworld_rising", "resurgence"],
    "exec-a02d168a-6305-48ff-b715-2cd0cf19cc7a.png": [
        "answer_the_call", "revenant_card", "soulbound", "frenzied_three_fingers"],
    "exec-f0112a3a-5fd2-40bc-9ec1-00d129bc651a.png": [
        "formation_breaker_hammer", "life_and_death", "giant_skeleton_wrath", "sky_rending_chord"],
    "exec-8910daba-f584-428e-a95b-f88884c18007.png": [
        "substitute_doll", "spirit_gathering", "concerto", "fight_for_me"],
    "exec-0b06032d-8d93-489d-9530-39972e22275b.png": [
        "soul_cursing_bell", "light_spirit", "grooming", "reanimate_dead"],
    "exec-b64baf97-d8bc-40f5-ae73-89cb1015effb.png": [
        "soul_return", "heavy_echo", "chanting_blessing", "underworld_reflection"],
    "exec-6b37efbc-5265-46cc-8f5e-475ded34e1a1.png": [
        "spirit_manipulation", "preparation_ritual", "watchful_waiting", "all_souls_return"],
}


def landscape_center_crop(image: Image.Image) -> Image.Image:
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
    return image.resize((1000, 760), Image.Resampling.LANCZOS)


def save(image: Image.Image, name: str) -> None:
    image = image.convert("RGBA")
    for directory in (OUTPUT, PREVIEW):
        directory.mkdir(parents=True, exist_ok=True)
        image.save(directory / f"{name}.png", optimize=True)


for filename, names in SHEETS.items():
    sheet = Image.open(GENERATED / filename).convert("RGB")
    half_w, half_h = sheet.width // 2, sheet.height // 2
    for index, name in enumerate(names):
        column, row = index % 2, index // 2
        inset = max(4, min(sheet.width, sheet.height) // 256)
        quadrant = sheet.crop((
            column * half_w + inset,
            row * half_h + inset,
            (column + 1) * half_w - inset,
            (row + 1) * half_h - inset,
        ))
        save(landscape_center_crop(quadrant), name)

following_shadow = Image.open(
    GENERATED / "exec-243b22aa-06e2-436b-bb42-012ef73ccf74.png"
).convert("RGB")
save(landscape_center_crop(following_shadow), "following_shadow")

print(f"Created {sum(map(len, SHEETS.values())) + 1} unique 1000x760 Revenant card portraits.")
