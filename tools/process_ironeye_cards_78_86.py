from pathlib import Path

from PIL import Image, ImageOps


ROOT = Path(__file__).resolve().parents[1]
GENERATED = Path(
    r"C:\Users\wenti\.codex\generated_images\019f6b05-d0b0-7af1-a624-760fba7be29d"
)
PREVIEW = ROOT / "design" / "卡图预览"
DESTINATION = ROOT / "images" / "packed" / "card_portraits" / "ironeye"

SOURCES = {
    "offensive": "exec-0ad1718c-d6e1-43fb-9605-ce227649eeb0.png",
    "returning_wind_arrow": "exec-f1c12ef5-03b7-40c1-b6bb-03bd560be334.png",
    "reversal_step": "exec-064735e7-46d8-4946-843e-38974130167d.png",
    "turning_arrow": "exec-be293dfe-fb76-471b-84e0-ef3633341091.png",
    "soul_chasing_volley": "exec-cc3411be-b9f8-41dd-9674-9e874fef636b.png",
    "corrode_all": "exec-ce70f707-30ad-4fac-929c-058e2479806c.png",
    "hundred_schemes": "exec-24d7b88c-42c0-4db6-9514-49a4aa2cae5a.png",
    "cut_through_chaos": "exec-aaded0b3-da9a-4756-8221-a6dac5ae42d8.png",
    "graceful_blade_dance": "exec-5b9923cb-d644-497d-b3e8-425e257b6068.png",
}


def fit_card(source: Path) -> Image.Image:
    with Image.open(source) as image:
        image = image.convert("RGB")
        return ImageOps.fit(
            image,
            (1000, 760),
            method=Image.Resampling.LANCZOS,
            centering=(0.5, 0.48),
        )


def main() -> None:
    PREVIEW.mkdir(parents=True, exist_ok=True)
    DESTINATION.mkdir(parents=True, exist_ok=True)

    approved_images: list[tuple[str, Image.Image]] = []
    for slug, filename in SOURCES.items():
        processed = fit_card(GENERATED / filename)
        preview_path = PREVIEW / f"铁之眼_{slug}_self_approved_2026-08-06.png"
        destination_path = DESTINATION / f"{slug}.png"
        processed.save(preview_path, optimize=True)
        processed.save(destination_path, optimize=True)
        approved_images.append((slug, processed.copy()))

    approved_approach = PREVIEW / "铁之眼_接近_v2_2026-08-06.png"
    approach = fit_card(approved_approach)
    approach.save(DESTINATION / "approach.png", optimize=True)

    sheet = Image.new("RGB", (1000, 760), "#121820")
    for index, (_, approved) in enumerate(approved_images):
        thumb = approved.resize((320, 243), Image.Resampling.LANCZOS)
        x = 10 + (index % 3) * 330
        y = 10 + (index // 3) * 253
        sheet.paste(thumb, (x, y))
    sheet.save(PREVIEW / "铁之眼_78_86_self_approval_sheet.png", optimize=True)


if __name__ == "__main__":
    main()
