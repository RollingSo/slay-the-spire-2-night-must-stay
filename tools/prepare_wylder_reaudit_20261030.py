import json,shutil
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[1]
out=root/'design/卡图预览/追踪者_原版复审_20261030'
out.mkdir(parents=True,exist_ok=True)
rows=json.loads((root/'design/wylder/art_reaudit_20261030_final.json').read_text(encoding='utf-8'))
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',18)
for r in rows:
    if r.get('status')=='withdrawn_reference_pose_priority':continue
    shutil.copy2(r['source'],out/(r['id']+'_source.png'))
    if r.get('rejected_source'):
        shutil.copy2(r['rejected_source'],out/(r['id']+'_rejected.png'))
    Image.open(r['source']).convert('RGB').resize((1000,760),Image.Resampling.LANCZOS).save(out/(r['id']+'.png'))
manifest=json.loads((root/'build/wylder_card_art_manifest.json').read_text(encoding='utf-8-sig'))
for batch in range(2):
    sheet=Image.new('RGB',(1250,900),'#24242a')
    for i,r in enumerate(manifest[batch*20:(batch+1)*20]):
        p=root/r['path']
        im=Image.open(p).convert('RGB').resize((250,190))
        x=i%5*250;y=i//5*225
        sheet.paste(im,(x,y));ImageDraw.Draw(sheet).text((x+4,y+192),r['name'],font=font,fill='white')
    sheet.save(out/f'all_{batch+1}.png')
print(out)
