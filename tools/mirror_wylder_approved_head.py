"""User-requested deterministic horizontal flip; preserve authored geometry."""
from pathlib import Path
from PIL import Image, ImageOps, ImageFilter
root=Path(__file__).resolve().parents[1]
out=root/'design/角色资源预览/wylder/front_back_corrected_20261021'
source=Image.open(out/'head_original.png').convert('RGBA')
mirrored=ImageOps.mirror(source)
mirrored.save(out/'head_left.png',optimize=True)
def fit(size,pad):
    image=ImageOps.contain(mirrored.crop(mirrored.getbbox()),(size[0]-2*pad,size[1]-2*pad),Image.Resampling.LANCZOS)
    canvas=Image.new('RGBA',size)
    canvas.alpha_composite(image,((size[0]-image.width)//2,(size[1]-image.height)//2))
    return canvas
icon=fit((88,88),3)
marker=fit((49,64),2)
base=fit((64,64),3)
outline=Image.new('RGBA',(64,64),'#EEE9D7')
outline.putalpha(base.getchannel('A').filter(ImageFilter.MaxFilter(3)))
outline.alpha_composite(base)
for name,im in [('character_icon_wylder.png',icon),('map_marker_wylder.png',marker),('character_icon_wylder_outline.png',outline)]:
    assert all(im.getpixel(p)[3]==0 for p in [(0,0),(im.width-1,0),(0,im.height-1),(im.width-1,im.height-1)])
    im.save(out/name,optimize=True)
sheet=Image.new('RGB',(500,100),'#383C45')
for i,role in enumerate(['guardian','ironeye','revenant','duchess','wylder']):
    image=icon if role=='wylder' else Image.open(root/f'{role}_assets/character_icon_{role}.png').convert('RGBA')
    sheet.paste(image,(i*100,6),image)
sheet.resize((1000,200),Image.Resampling.NEAREST).save(out/'comparison_left.png')
print('Horizontal mirror only; 88x88 / 64x64 / 49x64; alpha corners PASS')
