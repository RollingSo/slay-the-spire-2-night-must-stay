"""Copy generated originals and normalize preview sizes; no runtime asset writes."""
from pathlib import Path
import argparse
import json
import shutil

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MANIFEST = 'design/wylder/SKILL_SEVEN_ART_20261022.json'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest', default=DEFAULT_MANIFEST)
    args = parser.parse_args()
    manifest = json.loads((ROOT / args.manifest).read_text(encoding='utf-8'))
    output = ROOT / manifest['output_dir']
    output.mkdir(parents=True, exist_ok=True)
    label_font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 17)
    rows = max(1, (len(manifest['assets']) + 3) // 4)
    sheet = Image.new('RGB', (1000, rows * 225), '#252E4A')
    draw = ImageDraw.Draw(sheet)
    for index, item in enumerate(manifest['assets']):
        source = Path(item['source'])
        key = item['key']
        shutil.copy2(source, output / f'{key}_original.png')
        # Built-in imagegen can reference a conversation attachment whose old
        # clipboard file has expired. Preserve its provenance in the manifest
        # instead of fabricating a replacement local pose reference.
        if item.get('ref'):
            shutil.copy2(item['ref'], output / f'{key}_reference.png')
        with Image.open(source) as original:
            assert abs(original.width / original.height - 1000 / 760) < .02, key
            normalized = original.convert('RGB').resize((1000, 760), Image.Resampling.LANCZOS)
        normalized.save(output / f'{key}.png', optimize=True)
        thumbnail = normalized.resize((250, 190), Image.Resampling.LANCZOS)
        thumbnail.save(output / f'{key}_thumb.png')
        x, y = (index % 4) * 250, (index // 4) * 225
        draw.text((x + 8, y + 3), f"{index + 1}. {item['name']}", font=label_font, fill='white')
        sheet.paste(thumbnail, (x, y + 28))
        with Image.open(output / f'{key}.png') as check:
            assert check.size == (1000, 760) and check.mode == 'RGB'
        print(f"{key}: 1000x760 artwork / 250x190 thumbnail PASS")
    sheet.save(output / 'contact_sheet.png')
    print('Preview files only. Runtime art, card pool and localization not modified.')


if __name__ == '__main__':
    main()
