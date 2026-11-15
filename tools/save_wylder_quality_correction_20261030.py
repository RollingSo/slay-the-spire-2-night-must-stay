import json,shutil,hashlib
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[1]
out=root/'design/卡图预览/追踪者_动作纠偏_20261030'
out.mkdir(parents=True,exist_ok=True)
backup=out/'before_integration';backup.mkdir(exist_ok=True)
rows=json.loads((root/'design/wylder/quality_correction_20261030.json').read_text(encoding='utf-8'))
targets={'correction_sweep':'wylder_stamp_sweep','correction_upward':'wylder_stamp_upward_cut','correction_parry':'wylder_parry'}
sheet=Image.new('RGB',(1000,3*410),'#25252b')
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',22)
i=0
for r in rows:
    shutil.copy2(r['source'],out/(r['key']+'_source.png'))
    if r['key'] not in targets:continue
    name=targets[r['key']]+'.png'
    dst=root/'images/packed/card_portraits/wylder'/name
    if not (backup/name).exists():shutil.copy2(dst,backup/name)
    im=Image.open(r['source']).convert('RGB').resize((1000,760),Image.Resampling.LANCZOS)
    im.save(out/name)
    sheet.paste(Image.open(backup/name).convert('RGB').resize((500,380)),(0,i*410))
    sheet.paste(im.resize((500,380)),(500,i*410))
    ImageDraw.Draw(sheet).text((8,i*410+382),targets[r['key']]+'  旧版 / 本次重绘',font=font,fill='white')
    shutil.copy2(out/name,dst)
    r['formal_path']=dst.relative_to(root).as_posix()
    r['sha256']=hashlib.sha256(dst.read_bytes()).hexdigest()
    i+=1
sheet.save(out/'comparison.png')
(out/'review_manifest.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'{i} integrated, rejected drafts archived separately.')
