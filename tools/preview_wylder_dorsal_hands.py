from pathlib import Path
import json, shutil
from PIL import Image, ImageOps, ImageDraw
root=Path(__file__).resolve().parents[1]
out=root/'design/角色资源预览/wylder/dorsal_hands_20261020'
out.mkdir(parents=True,exist_ok=True)
data=json.loads((root/'design/wylder/DORSAL_HANDS_20261020.json').read_text(encoding='utf-8'))
sheet=Image.new('RGB',(1000,550),'#383C45')
draw=ImageDraw.Draw(sheet)
for i,item in enumerate(data['assets']):
    target=out/(item['key']+'.png')
    shutil.copy2(item['path'],target)
    im=Image.open(target).convert('RGBA')
    print(item['key'],im.size,'corners',[im.getpixel(p)[3] for p in [(0,0),(im.width-1,0),(0,im.height-1),(im.width-1,im.height-1)]])
    im=ImageOps.contain(im,(240,510),Image.Resampling.LANCZOS)
    sheet.paste(im,(i*250+(250-im.width)//2,30),im)
    draw.text((i*250+10,8),item['key'],fill='white')
sheet.save(out/'contact_sheet.png')
