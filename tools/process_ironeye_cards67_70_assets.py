from pathlib import Path
import argparse

from PIL import Image, ImageOps


ROOT = Path(__file__).resolve().parents[1]
GENERATED = Path(
    r"C:\Users\wenti\.codex\generated_images\019f6b05-d0b0-7af1-a624-760fba7be29d"
)
PREVIEW = ROOT / "design" / "卡图预览" / "铁之眼_67-70_2026-08-05"

SOURCES = {
    "67_谢幕_curtain_call.png": GENERATED / "exec-54a5b02b-f628-4b0d-89d6-c7caa44456b4.png",
    "68_破空箭_air_rending_arrow.png": GENERATED / "exec-45076481-9602-4ae5-a8e1-f9b26f2a1ad2.png",
    "69_不怒自威_imposing_presence.png": GENERATED / "exec-4aed2b66-27c5-4ebe-b12e-aef74d717db9.png",
    "70_看破_see_through.png": GENERATED / "exec-dedf6c96-c239-4659-ac40-926917f2ea42.png",
}


FORMAL_NAMES = {
    "67_谢幕_curtain_call.png": "curtain_call.png",
    "68_破空箭_air_rending_arrow.png": "air_rending_arrow.png",
    "69_不怒自威_imposing_presence.png": "imposing_presence.png",
    "70_看破_see_through.png": "see_through.png",
}


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--install", action="store_true")
    args = parser.parse_args()
    PREVIEW.mkdir(parents=True, exist_ok=True)
    portrait_dir = ROOT / "images" / "packed" / "card_portraits" / "ironeye"
    if args.install:
        portrait_dir.mkdir(parents=True, exist_ok=True)
    for filename, source in SOURCES.items():
        with Image.open(source) as image:
            image = ImageOps.fit(
                image.convert("RGB"),
                (1000, 760),
                method=Image.Resampling.LANCZOS,
                centering=(0.5, 0.5),
            )
            image.save(PREVIEW / filename, format="PNG", optimize=True)
            if args.install:
                image.save(
                    portrait_dir / FORMAL_NAMES[filename],
                    format="PNG",
                    optimize=True,
                )
        print(PREVIEW / filename)


if __name__ == "__main__":
    main()
