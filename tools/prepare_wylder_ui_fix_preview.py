"""Prepare preview derivatives only; never overwrite approved assets."""
from pathlib import Path
import argparse
from PIL import Image, ImageOps, ImageFilter
root=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--folder',default='ui_fix_20261021')
args=parser.parse_args()
assert args.folder in ('ui_fix_20261021','ui_side_reference_20261021')
out=root/'design/角色资源预览/wylder'/args.folder
def fit(path,size,pad):
    image=Image.open(path).convert('RGBA')
    image=ImageOps.contain(image.crop(image.getbbox()),(size[0]-pad*2,size[1]-pad*2),Image.Resampling.LANCZOS)
    canvas=Image.new('RGBA',size)
    canvas.alpha_composite(image,((size[0]-image.width)//2,(size[1]-image.height)//2))
    assert all(canvas.getpixel(p)[3]==0 for p in [(0,0),(size[0]-1,0),(0,size[1]-1),(size[0]-1,size[1]-1)])
    return canvas
for name,size in [('character_icon_wylder.png',(88,88)),('map_marker_wylder.png',(49,64))]:
    fit(out/'head_original.png',size,3).save(out/name)
base=fit(out/'head_original.png',(64,64),3)
border=Image.new('RGBA',(64,64),'#EEE9D7')
border.putalpha(base.getchannel('A').filter(ImageFilter.MaxFilter(3)))
border.alpha_composite(base)
border.save(out/'character_icon_wylder_outline.png')
for size in [256,74,48]: fit(out/'energy_original.png',(size,size),3).save(out/f'energy_{size}.png')
sheet=Image.new('RGB',(500,120),'#383C45')
for i,(folder,role) in enumerate([('guardian_assets','guardian'),('ironeye_assets','ironeye'),('revenant_assets','revenant'),('duchess_assets','duchess'),(None,'wylder')]):
    icon=Image.open((root/folder if folder else out)/f'character_icon_{role}.png').convert('RGBA')
    sheet.paste(icon,(i*100,10),icon)
sheet.resize((1000,240),Image.Resampling.NEAREST).save(out/'comparison.png')
print('Preview dimensions and transparent corners PASS')
