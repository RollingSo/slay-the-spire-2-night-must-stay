"""Native-size comparison of UI head candidates; never promotes runtime art."""
from pathlib import Path
import argparse
from PIL import Image, ImageOps, ImageFilter
ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--version',default='ui_heads_v2')
args=parser.parse_args()
assert args.version in ('ui_heads_v2','ui_heads_v3_profile','ui_heads_v5_lateral')
OUT=ROOT/'design/角色资源预览/wylder'/args.version
source=OUT/'original.png'
image=Image.open(source).convert('RGBA')
image=image.crop(image.getbbox())
def fit(size,pad):
    thumb=ImageOps.contain(image,(size[0]-pad*2,size[1]-pad*2),Image.Resampling.LANCZOS)
    canvas=Image.new('RGBA',size)
    canvas.alpha_composite(thumb,((size[0]-thumb.width)//2,(size[1]-thumb.height)//2))
    return canvas
icon=fit((88,88),3)
marker=fit((49,64),2)
base=fit((64,64),3)
outline=Image.new('RGBA',base.size,'#EEE9D7')
outline.putalpha(base.getchannel('A').filter(ImageFilter.MaxFilter(3)))
outline.alpha_composite(base)
for name,im in [('character_icon_wylder.png',icon),('character_icon_wylder_outline.png',outline),('map_marker_wylder.png',marker)]:
    assert all(im.getpixel(p)[3]==0 for p in [(0,0),(im.width-1,0),(0,im.height-1),(im.width-1,im.height-1)])
    im.save(OUT/name)
sheet=Image.new('RGB',(88*5,168),'#34383F')
for i,role in enumerate(['guardian','ironeye','revenant','duchess','wylder']):
    for prefix,y in [('character_icon_',0),('map_marker_',96)]:
        im=Image.open((OUT if role=='wylder' else ROOT/f'{role}_assets')/f'{prefix}{role}.png').convert('RGBA')
        sheet.paste(im,(i*88+(88-im.width)//2,y),im)
sheet.save(OUT/'comparison_native.png')
sheet.resize((880,336),Image.Resampling.NEAREST).save(OUT/'comparison_2x.png')
print('Preview only: 88x88 / 64x64 / 49x64; alpha corners PASS')
