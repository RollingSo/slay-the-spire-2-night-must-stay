"""Import approved combat originals without repainting or alpha cleanup."""
from pathlib import Path
import json
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
manifest = json.loads((ROOT / 'design/wylder/approved_combat_assets.json').read_text(encoding='utf-8'))
report_path = ROOT / 'design/wylder/asset_report.json'
report = json.loads(report_path.read_text(encoding='utf-8'))
for entry in manifest['assets']:
    image = Image.open(ROOT / entry['source']).convert('RGBA')
    bounds = image.getchannel('A').getbbox()
    body_top = entry.get('body_top', bounds[1])
    body_height = bounds[3] - body_top
    target_height = 900 if entry['key'] == 'death' else 1360
    factor = target_height / body_height
    size = (round((bounds[2] - bounds[0]) * factor), round((bounds[3] - bounds[1]) * factor))
    if size[0] > 2500 or size[1] > 1550:
        raise ValueError(f"Approved pose does not fit without changing body scale: {entry['key']}")
    # Taller canvas accommodates the raised sword without reducing body height.
    # (1550 - 1700/2) * .22 = 154: existing PoseRoot -154 stays grounded.
    canvas = Image.new('RGBA', (2600, 1700))
    anchor_x = entry['anchor_x']
    left = round(1300 - (anchor_x - bounds[0]) * factor)
    if left < 0 or left + size[0] > 2600:
        raise ValueError(f"Approved pose anchor clips the sprite: {entry['key']}")
    canvas.alpha_composite(image.crop(bounds).resize(size, Image.Resampling.LANCZOS),
                           (left, 1550 - size[1]))
    destination = ROOT / entry['runtime']
    canvas.save(destination, optimize=True)
    alpha = canvas.getchannel('A')
    report[entry['runtime']] = {
        'size': list(canvas.size), 'mode': 'RGBA', 'bounds': list(alpha.getbbox()),
        'corners_alpha': [alpha.getpixel(p) for p in [(0, 0), (2599, 0), (0, 1699), (2599, 1699)]],
        'approval': 'approved', 'source': entry['source'],
        'body_height_px': body_height * factor,
        'body_top_px': 1550 - size[1] + (body_top - bounds[1]) * factor,
    }
report_path.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(f"Promoted {len(manifest['assets'])} approved combat assets; original alpha preserved.")
