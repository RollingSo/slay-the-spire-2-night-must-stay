from pathlib import Path
from collections import deque

from PIL import Image
from PIL import ImageDraw


ROOT = Path(__file__).resolve().parents[1]
TARGET_SIZE = (422, 1200)
TARGET_ART_BOX = (330, 1100)
ROTATION_DEGREES = 48


def remove_detached_alpha(image: Image.Image) -> Image.Image:
    """Remove neighboring contact-sheet fragments from generated hand cells."""
    image = image.copy()
    alpha = image.getchannel("A")
    width, height = alpha.size
    opaque = bytearray(1 if value > 8 else 0 for value in alpha.getdata())
    seen = bytearray(width * height)
    largest: list[int] = []

    for start, is_opaque in enumerate(opaque):
        if not is_opaque or seen[start]:
            continue
        component: list[int] = []
        queue = deque([start])
        seen[start] = 1
        while queue:
            index = queue.popleft()
            component.append(index)
            x = index % width
            y = index // width
            for neighbor in (
                index - 1 if x > 0 else -1,
                index + 1 if x + 1 < width else -1,
                index - width if y > 0 else -1,
                index + width if y + 1 < height else -1,
            ):
                if neighbor >= 0 and opaque[neighbor] and not seen[neighbor]:
                    seen[neighbor] = 1
                    queue.append(neighbor)
        if len(component) > len(largest):
            largest = component

    if not largest:
        return image
    keep = bytearray(width * height)
    for index in largest:
        keep[index] = 1
    alpha_values = list(alpha.getdata())
    alpha.putdata([
        value if keep[index] else 0
        for index, value in enumerate(alpha_values)
    ])
    image.putalpha(alpha)
    return image


def cleanup_detached(path: Path) -> None:
    image = remove_detached_alpha(Image.open(path).convert("RGBA"))
    image.save(path)


def clear_revenant_sheet_edge(image: Image.Image) -> Image.Image:
    """Clear two fingers leaked in from the adjacent generated sheet cell."""
    image = image.copy()
    ImageDraw.Draw(image).rectangle((300, 0, image.width, image.height), fill=(0, 0, 0, 0))
    return image


def normalize(path: Path) -> None:
    image = Image.open(path).convert("RGBA")
    if image.size == TARGET_SIZE:
        if "revenant_assets" in path.parts:
            image = clear_revenant_sheet_edge(image)
        image.save(path)
        return
    image = remove_detached_alpha(image)
    bbox = image.getchannel("A").getbbox()
    if bbox is None:
        raise ValueError(f"Hand image has no visible pixels: {path}")
    image = image.crop(bbox).rotate(
        ROTATION_DEGREES,
        resample=Image.Resampling.BICUBIC,
        expand=True,
    )
    bbox = image.getchannel("A").getbbox()
    if bbox is None:
        raise ValueError(f"Rotated hand image has no visible pixels: {path}")
    image = image.crop(bbox)
    # Native multiplayer arms use a tall 422x1200 canvas and their hotspot is
    # derived from that full-height silhouette. Stretch the generated forearm
    # into the same art box instead of leaving a short square sprite floating
    # near the cursor.
    image = image.resize(TARGET_ART_BOX, Image.Resampling.LANCZOS)

    output = Image.new("RGBA", TARGET_SIZE, (0, 0, 0, 0))
    x = (TARGET_SIZE[0] - image.width) // 2
    y = 22
    output.alpha_composite(image, (x, y))
    if "revenant_assets" in path.parts:
        output = clear_revenant_sheet_edge(output)
    output.save(path)


def main() -> None:
    directories = (
        ROOT / "ironeye_assets" / "multiplayer_hands",
        ROOT / "revenant_assets" / "multiplayer_hands",
    )
    for directory in directories:
        # Treasure-room cursor placement uses the pointing arm. Other gesture
        # sprites belong to the separate multiplayer emote UI and keep their
        # authored framing.
        for path in sorted(directory.glob("*point.png")):
            normalize(path)
            print(f"normalized {path.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
