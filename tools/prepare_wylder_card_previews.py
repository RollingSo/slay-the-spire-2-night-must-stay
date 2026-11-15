"""Resolution-only normalization and thumbnail QA; never promotes artwork."""
from pathlib import Path
import json, shutil
from PIL import Image, ImageDraw
root=Path(__file__).resolve().parents[1]
out=root/'design/卡图预览/wylder/complete_20261020'
out.mkdir(parents=True,exist_ok=True)
manifest=json.loads((root/'design/wylder/CARD_ART_20261020.json').read_text(encoding='utf-8'))
sheet=Image.new('RGB',(1000,650),'#252E4A')
draw=ImageDraw.Draw(sheet)
for i,item in enumerate(manifest['assets']):
    source=Path(item['path'])
    shutil.copy2(source,out/(item['key']+'_original.png'))
    image=Image.open(source).convert('RGB')
    assert abs(image.width/image.height-1000/760)<.02
    image=image.resize((1000,760),Image.Resampling.LANCZOS)
    image.save(out/(item['key']+'.png'),optimize=True)
    image.resize((250,190),Image.Resampling.LANCZOS).save(out/(item['key']+'_thumb.png'))
    x=(i%4)*250;y=(i//4)*220
    sheet.paste(image.resize((250,190),Image.Resampling.LANCZOS),(x,y+20))
    draw.text((x+5,y+3),str(i+1),fill='white')
    print(item['key'],'1000x760 / 250x190 PASS')
sheet.crop((0,0,1000,440)).save(out/'contact_sheet.png')
