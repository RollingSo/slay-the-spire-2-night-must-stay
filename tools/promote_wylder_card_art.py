"""Install the seven user-approved-for-testing card portraits."""
from pathlib import Path
import hashlib, json, shutil
from PIL import Image
root=Path(__file__).resolve().parents[1]
data=json.loads((root/'design/wylder/CARD_ART_20261020.json').read_text(encoding='utf-8'))
assert data.get('approved_for_testing') is True
preview=root/'design/卡图预览/wylder/complete_20261020'
dest=root/'images/packed/card_portraits/wylder'
dest.mkdir(parents=True,exist_ok=True)
for item in data['assets']:
    name=item['key']+'.png'
    source=root/item['approved_runtime_source'] if item.get('approved_runtime_source') else preview/name
    if item.get('approved_runtime_source'):
        if not source.resolve().is_relative_to(root.resolve()):
            raise ValueError('Approved artwork source must remain inside the project')
        if not source.is_file():
            raise FileNotFoundError(source)
        if item.get('approved_sha256') and hashlib.sha256(source.read_bytes()).hexdigest().upper() != item['approved_sha256'].upper():
            raise ValueError('Approved artwork checksum mismatch: '+item['key'])
    if source.exists():
        with Image.open(source) as image: assert image.size==(1000,760)
        shutil.copy2(source,dest/name)
    assert (dest/name).exists(), name
print('Seven Wylder card portraits promoted for local testing')
