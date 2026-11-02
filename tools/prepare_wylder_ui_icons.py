"""Mechanical thumbnail/outline preparation of the updated authored UI bust."""
from pathlib import Path
import json
from PIL import Image, ImageOps, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / 'wylder_assets/approved_sources/ui_head_first_selected.png'
image = Image.open(source).convert('RGBA')
image = image.crop(image.getbbox())
report_path = ROOT / 'design/wylder/asset_report.json'
report = json.loads(report_path.read_text(encoding='utf-8'))
outputs = {}
for name, size, pad in [('character_icon_wylder.png',(88,88),3),('map_marker_wylder.png',(49,64),2),('_outline_base',(64,64),3)]:
    thumb = ImageOps.contain(image,(size[0]-pad*2,size[1]-pad*2),Image.Resampling.LANCZOS)
    canvas = Image.new('RGBA',size)
    canvas.alpha_composite(thumb,((size[0]-thumb.width)//2,(size[1]-thumb.height)//2))
    outputs[name] = canvas
icon = outputs.pop('_outline_base')
border = Image.new('RGBA',icon.size,'#EEE9D7')
border.putalpha(icon.getchannel('A').filter(ImageFilter.MaxFilter(3)))
border.alpha_composite(icon)
outputs['character_icon_wylder_outline.png'] = border
for name, output in outputs.items():
    corners = [output.getpixel(p)[3] for p in [(0,0),(output.width-1,0),(0,output.height-1),(output.width-1,output.height-1)]]
    assert corners == [0,0,0,0]
    output.save(ROOT/'wylder_assets'/name,optimize=True)
    report['wylder_assets/'+name] = dict(size=list(output.size),mode='RGBA',bounds=list(output.getbbox()),corners_alpha=corners,approval='updated per user request',source=str(source.relative_to(ROOT)))
report_path.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('Updated Wylder UI icons: 88x88 / 64x64 / 49x64; true-alpha corners PASS')
