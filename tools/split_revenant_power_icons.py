from pathlib import Path

from PIL import Image


SOURCE = Path(
    r"C:\Users\wenti\.codex\generated_images\01a00f23-ef57-7810-a22c-4e46ac8529ac"
    r"\exec-d8a589bf-c92c-4195-a62b-353a975318a8.png"
)
OUTPUT = Path(r"D:\sts-2-mod\revenant_assets\powers")
NAMES = (
    "frenzied_three_fingers_power",
    "fight_for_me_power",
    "light_spirit_power",
    "heavy_echo_power",
    "chanting_blessing_power",
    "following_shadow_power",
    "necromancy_power",
)


sheet = Image.open(SOURCE).convert("RGB")
cell_width = sheet.width / 4
cell_height = sheet.height / 2
OUTPUT.mkdir(parents=True, exist_ok=True)

for index, name in enumerate(NAMES):
    column, row = index % 4, index // 4
    left = round(column * cell_width)
    top = round(row * cell_height)
    right = round((column + 1) * cell_width)
    bottom = round((row + 1) * cell_height)
    cell = sheet.crop((left, top, right, bottom)).convert("RGBA")

    pixels = []
    for red, green, blue, _ in cell.getdata():
        distance = 255 - min(red, green, blue)
        alpha = max(0, min(255, (distance - 2) * 8))
        pixels.append((red, green, blue, alpha))
    cell.putdata(pixels)

    bounds = cell.getchannel("A").getbbox()
    if bounds is None:
        raise RuntimeError(f"No icon pixels found for {name}")
    cell = cell.crop(bounds)
    scale = min(220 / cell.width, 220 / cell.height)
    cell = cell.resize(
        (max(1, round(cell.width * scale)), max(1, round(cell.height * scale))),
        Image.Resampling.LANCZOS,
    )
    icon = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    icon.alpha_composite(cell, ((256 - cell.width) // 2, (256 - cell.height) // 2))
    icon.save(OUTPUT / f"{name}.png", optimize=True)

print(f"Created {len(NAMES)} distinct 256x256 Revenant power icons.")
