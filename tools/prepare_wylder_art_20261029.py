import json, shutil
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
root=Path(__file__).resolve().parents[1]
rows=json.loads((root/'design/wylder/art_completion_20261029.json').read_text(encoding='utf-8'))
out=root/'design/卡图预览/追踪者_补齐_20261029'
out.mkdir(parents=True,exist_ok=True)
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',18)
sheet=Image.new('RGB',(1000,5*225),'#20202b')
for i,r in enumerate(rows):
    if r.get('rejected_source'):
        shutil.copy2(r['rejected_source'],out/(r['id']+'_rejected_v1.png'))
    src=Path(r['source']); shutil.copy2(src,out/(r['id']+'_source.png'))
    im=Image.open(src).convert('RGB').resize((1000,760),Image.Resampling.LANCZOS)
    im.save(out/(r['id']+'.png'))
    thumb=im.resize((250,190),Image.Resampling.LANCZOS)
    x=(i%4)*250; y=(i//4)*225
    sheet.paste(thumb,(x,y)); ImageDraw.Draw(sheet).text((x+5,y+194),r['name'],font=font,fill='white')
sheet.save(out/'contact_sheet.png')
print(out)
