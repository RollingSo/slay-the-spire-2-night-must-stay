"""Promote approved mirrored head and white-water energy emblem."""
from pathlib import Path
import json, shutil, math
from PIL import Image, ImageOps, ImageChops
root=Path(__file__).resolve().parents[1]
sources=root/'wylder_assets/approved_sources'
head=root/'design/角色资源预览/wylder/front_back_corrected_20261021/head_left.png'
energy=root/'design/角色资源预览/wylder/ui_fix_20261021/energy_original.png'
if head.exists(): shutil.copy2(head,sources/'ui_head_first_selected.png')
if energy.exists(): shutil.copy2(energy,sources/'energy_white_water.png')
image=Image.open(sources/'energy_white_water.png').convert('RGBA')
def fit(size,pad):
    thumb=ImageOps.contain(image.crop(image.getbbox()),(size-pad*2,size-pad*2),Image.Resampling.LANCZOS)
    out=Image.new('RGBA',(size,size));out.alpha_composite(thumb,((size-thumb.width)//2,(size-thumb.height)//2));return out
report_path=root/'design/wylder/asset_report.json'
report=json.loads(report_path.read_text(encoding='utf-8'))
def save(im,path):
    im.save(root/path,optimize=True)
    corners=[im.getpixel(p)[3] for p in [(0,0),(im.width-1,0),(0,im.height-1),(im.width-1,im.height-1)]]
    assert corners==[0,0,0,0]
    report[path]=dict(size=list(im.size),mode='RGBA',bounds=list(im.getbbox()),corners_alpha=corners,approval='approved',source='wylder_assets/approved_sources/energy_white_water.png')
for size,path in [(74,'wylder_assets/energy_icon/wylder_energy_card_icon.png'),(48,'wylder_assets/energy_icon/wylder_energy_font_icon.png'),(48,'images/packed/sprite_fonts/wylder_energy_icon.png')]:save(fit(size,3),path)
full=fit(256,8)
for i,(lo,hi) in enumerate([(0,.52),(.52,.71),(.71,.85),(.85,.94),(.94,2)],1):
    mask=Image.new('L',(256,256));pixels=mask.load()
    for y in range(256):
        for x in range(256): pixels[x,y]=255 if lo<=math.hypot(x-127.5,y-127.5)/120<hi else 0
    layer=full.copy();layer.putalpha(ImageChops.multiply(full.getchannel('A'),mask))
    save(layer,f'wylder_assets/energy_counter/wylder_orb_layer_{i}.png')
report_path.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
import subprocess,sys
subprocess.run([sys.executable,str(root/'tools/prepare_wylder_ui_icons.py')],check=True)
print('Approved left head and white-water energy promoted')
