"""Copy approved-for-review sources and resize only; never promote runtime art."""
from pathlib import Path
import json
import shutil

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / 'design/wylder/POSE_CARD_REDRAW_20261022.json'


def main():
    manifest = json.loads(MANIFEST.read_text(encoding='utf-8'))
    output = ROOT / manifest['output_dir']
    output.mkdir(parents=True, exist_ok=True)
    sheet = Image.new('RGB', (500, 190), '#252E4A')
    for index, item in enumerate(manifest['assets']):
        source = Path(item['source'])
        key = item['key']
        shutil.copy2(source, output / f'{key}_original.png')
        with Image.open(source) as original:
            assert abs(original.width / original.height - 1000 / 760) < 0.02
            normalized = original.convert('RGB').resize((1000, 760), Image.Resampling.LANCZOS)
        normalized.save(output / f'{key}.png', optimize=True)
        thumbnail = normalized.resize((250, 190), Image.Resampling.LANCZOS)
        thumbnail.save(output / f'{key}_thumb.png')
        sheet.paste(thumbnail, (index * 250, 0))
        print(f'{key}: 1000x760 artwork, 250x190 thumbnail; no runtime write')
    sheet.save(output / 'contact_sheet.png')
    for name in ('pose_upward', 'pose_sweep'):
        shutil.copy2(manifest['references'][name], output / f'{name}_reference.png')


if __name__ == '__main__':
    main()
