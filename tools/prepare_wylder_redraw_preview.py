from pathlib import Path
import json, shutil
from PIL import Image, ImageOps, ImageDraw
root=Path(__file__).resolve().parents[1]
out=root/'design/角色资源预览/wylder/redraw_20261020'
out.mkdir(parents=True,exist_ok=True)
manifest=json.loads((root/'design/wylder/REDRAW_20261020.json').read_text(encoding='utf-8'))
sheet=Image.new('RGB',(1200,800),'#383C45')
draw=ImageDraw.Draw(sheet)
for i,asset in enumerate(manifest['assets']):
    dest=out/(asset['key']+'_original.png')
    shutil.copy2(asset['path'],dest)
    im=Image.open(dest).convert('RGBA')
    print(asset['key'],im.size,'alpha corners',[im.getpixel(p)[3] for p in [(0,0),(im.width-1,0),(0,im.height-1),(im.width-1,im.height-1)]])
    im=ImageOps.contain(im,(380,355),Image.Resampling.LANCZOS)
    x=(i%3)*400+(400-im.width)//2
    y=(i//3)*400+25
    sheet.paste(im,(x,y),im)
    draw.text(((i%3)*400+12,(i//3)*400+5),asset['key'],fill='white')
sheet.save(out/'contact_sheet.png')
