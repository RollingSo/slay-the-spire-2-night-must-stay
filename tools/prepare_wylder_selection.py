"""Engine-size derivatives of authored selection art; no repaint/alpha cleanup."""
from pathlib import Path
import argparse
import json
import shutil
from PIL import Image, ImageOps, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
PREVIEW = ROOT / 'design/角色资源预览/wylder/selection_20261020'


def prepare(promote=False):
    PREVIEW.mkdir(parents=True, exist_ok=True)
    sources = ROOT / 'wylder_assets/approved_sources'
    portrait_path = PREVIEW / 'portrait_original.png'
    background_path = PREVIEW / 'background_original.png'
    if not portrait_path.exists():
        portrait_path = sources / 'selection_portrait.png'
    if not background_path.exists():
        background_path = sources / 'selection_background.png'
    portrait = Image.open(portrait_path).convert('RGB')
    background = Image.open(background_path).convert('RGB')
    # Both masters were composed at the target aspect ratios. Normalization
    # changes pixel resolution only; no perspective/pose/content edits.
    assert abs(portrait.width / portrait.height - 132 / 195) < .004
    assert abs(background.width / background.height - 2560 / 1200) < .01
    outputs = {
        'char_select_wylder.png': portrait.resize((132, 195), Image.Resampling.LANCZOS),
        'character_select_wylder_bg.png': background.resize((2560, 1200), Image.Resampling.LANCZOS),
    }
    outputs['char_select_wylder_locked.png'] = ImageOps.grayscale(outputs['char_select_wylder.png']).convert('RGB')
    for name, image in outputs.items():
        image.save(PREVIEW / name, optimize=True)

    bg = outputs['character_select_wylder_bg.png']
    bg.crop((320, 0, 2240, 1080)).save(PREVIEW / 'background_16x9_crop.png')
    guide = bg.copy()
    draw = ImageDraw.Draw(guide)
    draw.rectangle((320, 0, 2239, 1199), outline='#FFE27A', width=4)
    draw.line((int(2560 * .43), 0, int(2560 * .43), 1200), fill='#70DAFF', width=3)
    draw.rectangle((384, 4, 2176, 1195), outline='#79E5A3', width=2)
    guide.save(PREVIEW / 'background_safe_area_qa.png')

    # Same native 132x195 size for all five portraits, shown without distortion.
    sheet = Image.new('RGB', (132 * 5, 195), '#111827')
    for index, role in enumerate(('guardian', 'ironeye', 'revenant', 'duchess')):
        reference = Image.open(ROOT / f'{role}_assets/char_select_{role}.png').convert('RGB')
        assert reference.size == (132, 195)
        sheet.paste(reference, (index * 132, 0))
    sheet.paste(outputs['char_select_wylder.png'], (132 * 4, 0))
    sheet.save(PREVIEW / 'five_portraits_native_size.png')

    if promote:
        approval = json.loads((ROOT / 'design/wylder/selection_assets.json').read_text(encoding='utf-8'))
        assert approval['approved'] is True, 'Selection artwork is not approved'
        for source_path, name in ((portrait_path, 'selection_portrait.png'), (background_path, 'selection_background.png')):
            if source_path.resolve() != (sources / name).resolve():
                shutil.copy2(source_path, sources / name)
        report_path = ROOT / 'design/wylder/asset_report.json'
        report = json.loads(report_path.read_text(encoding='utf-8'))
        for name, image in outputs.items():
            destination = ROOT / 'wylder_assets' / name
            image.save(destination, optimize=True)
            report[f'wylder_assets/{name}'] = {
                'size': list(image.size), 'mode': image.mode,
                'bounds': list(image.getbbox()), 'corners_alpha': None,
                'approval': 'approved', 'source': f'design/角色资源预览/wylder/selection_20261020/{name}',
            }
        report_path.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    # The first UI bust was rejected. Do not re-promote it with selection art.
    if promote and (sources / 'ui_head_first_selected.png').exists():
        import subprocess, sys
        subprocess.run([sys.executable, str(ROOT / 'tools/prepare_wylder_ui_icons.py')], check=True)
    print(f'Wylder selection: 132x195 portraits, 2560x1200 background, 16:9 crop; promoted={promote}')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--promote', action='store_true')
    prepare(parser.parse_args().promote)
