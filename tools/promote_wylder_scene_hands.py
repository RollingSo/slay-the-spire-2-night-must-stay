"""Promote user-approved scene and dorsal-hand sources; no alpha cleanup."""
from pathlib import Path
import json, shutil
from PIL import Image, ImageOps
root=Path(__file__).resolve().parents[1]
sources=root/'wylder_assets/approved_sources'
report_path=root/'design/wylder/asset_report.json'
report=json.loads(report_path.read_text(encoding='utf-8'))
entries=[('rest','rest_site/wylder_rest_site.png',(1200,1250)),('merchant','merchant/wylder_merchant.png',(1200,1250))]+[(k,f'multiplayer_hands/multiplayer_hand_wylder_{k}.png',(512,768)) for k in ['point','rock','paper','scissors']]
for key,target,size in entries:
    candidate=root/('design/角色资源预览/wylder/redraw_20261020/'+key+'_original.png' if key in ['rest','merchant'] else 'design/角色资源预览/wylder/dorsal_hands_20261020/'+key+'.png')
    original=sources/('approved_scene_hand_'+key+'.png')
    if candidate.exists(): shutil.copy2(candidate,original)
    im=Image.open(original).convert('RGBA')
    im=ImageOps.contain(im.crop(im.getbbox()),(size[0]-24,size[1]-24),Image.Resampling.LANCZOS)
    canvas=Image.new('RGBA',size)
    canvas.alpha_composite(im,((size[0]-im.width)//2,(size[1]-im.height)//2))
    corners=[canvas.getpixel(p)[3] for p in [(0,0),(size[0]-1,0),(0,size[1]-1),(size[0]-1,size[1]-1)]]
    assert corners==[0,0,0,0]
    path='wylder_assets/'+target
    canvas.save(root/path,optimize=True)
    report[path]=dict(size=list(size),mode='RGBA',bounds=list(canvas.getbbox()),corners_alpha=corners,approval='approved',source=str(original.relative_to(root)))
report_path.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('Promoted 2 approved scene sprites and 4 approved dorsal hand sprites')
