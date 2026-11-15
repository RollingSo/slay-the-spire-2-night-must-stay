"""Build reproducible research sheets of shipped character artwork, without editing assets."""
from pathlib import Path
import hashlib, json, math
from PIL import Image, ImageDraw, ImageFont, ImageOps

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'design/卡图研究/20261026'
SOURCES = {'guardian': 'packed/card_portraits/guardian',
           'ironeye': 'images/packed/card_portraits/ironeye',
           'revenant': 'revenant_assets/cards',
           'duchess': 'images/packed/card_portraits/duchess'}

def sheet(files, path, columns, width):
    height = round(width * .76)
    cell = height + 28
    canvas = Image.new('RGB', (columns * width, math.ceil(len(files)/columns)*cell), '#20232d')
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 12)
    for i, file in enumerate(files):
        x, y = (i % columns)*width, (i//columns)*cell
        with Image.open(file) as im:
            thumb = ImageOps.contain(im.convert('RGB'), (width, height))
        canvas.paste(thumb, (x+(width-thumb.width)//2, y))
        draw.text((x+2, y+height+2), file.stem[:30], fill='white', font=font)
    canvas.save(path)

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    report = {}
    for role, source in SOURCES.items():
        files = sorted((ROOT/source).glob('*.png'))
        entries = []
        for file in files:
            with Image.open(file) as im:
                entries.append({'path': file.relative_to(ROOT).as_posix(), 'size': list(im.size),
                                'sha256': hashlib.sha256(file.read_bytes()).hexdigest()})
        chosen = [files[round(i*(len(files)-1)/15)] for i in range(min(16,len(files)))]
        sheet(files, OUT/f'{role}_all.png', 8, 160)
        sheet(chosen, OUT/f'{role}_sample.png', 4, 250)
        hashes = {}
        for entry in entries:
            hashes.setdefault(entry['sha256'], []).append(entry['path'])
        report[role] = {'source': source, 'file_count': len(files), 'assets': entries,
                        'sample': [f.relative_to(ROOT).as_posix() for f in chosen],
                        'exact_duplicate_groups': [v for v in hashes.values() if len(v)>1]}
        print(role, len(files), 'source images; research only, not registered card count or visual approval')
    (OUT/'inventory.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')

if __name__ == '__main__': main()
