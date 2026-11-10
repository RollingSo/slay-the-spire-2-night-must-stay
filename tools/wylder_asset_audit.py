"""Read-only inventory/contact sheets of the four existing character asset sets."""
from pathlib import Path
import json
from PIL import Image, ImageOps, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'design/wylder/audit'
OUT.mkdir(parents=True, exist_ok=True)
report = {}
for role in ['guardian', 'ironeye', 'revenant', 'duchess']:
    paths = sorted((ROOT / f'{role}_assets').rglob('*.png'))
    paths = [p for p in paths if not any(s in p.parts for s in ['cards', 'relics', 'potions'])]
    paths += sorted((ROOT/'images/powers').glob(f'{role}_*.png'))
    report[role] = []
    # All non-card/item character assets, paginated for visual inspection.
    for start in range(0, len(paths), 30):
        group = paths[start:start+30]
        canvas = Image.new('RGB', (1200, 220*((len(group)+4)//5)), '#343841')
        d = ImageDraw.Draw(canvas)
        for i,p in enumerate(group):
            im = Image.open(p).convert('RGBA')
            report[role].append({'path':p.relative_to(ROOT).as_posix(), 'size':im.size, 'alpha_bounds':im.getbbox()})
            thumb = ImageOps.contain(im, (224, 180))
            x,y = (i%5)*240, (i//5)*220
            canvas.paste(thumb, (x+(240-thumb.width)//2, y+(180-thumb.height)//2),thumb)
            d.text((x+3,y+185),p.name[:33],fill='white')
        canvas.save(OUT/f'{role}_{start//30}.jpg')
(OUT/'inventory.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print({role:len(items) for role,items in report.items()})

# Older models use unprefixed power IDs. Review their shared source assets too,
# rather than treating the role-prefixed subset as a complete status-icon audit.
paths=sorted((ROOT/'images/powers').glob('*.png'))
paths=[p for p in paths if not p.name.startswith('wylder_')]
for start in range(0,len(paths),64):
    group=paths[start:start+64]
    canvas=Image.new('RGB',(1280,160*((len(group)+7)//8)),'#343841');d=ImageDraw.Draw(canvas)
    for i,p in enumerate(group):
        im=ImageOps.contain(Image.open(p).convert('RGBA'),(140,125));x=(i%8)*160;y=(i//8)*160
        canvas.paste(im,(x+(160-im.width)//2,y),im);d.text((x+2,y+130),p.stem[:24],fill='white')
    canvas.save(OUT/f'power_shared_{start//64}.jpg')
print(f'Additional shared power icon audit: {len(paths)} images.')
