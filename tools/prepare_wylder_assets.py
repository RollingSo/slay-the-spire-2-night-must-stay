"""Prepare engine-size derivatives of ImageGen sprites; preserve source candidates.

This is import/atlas/thumbnail preparation, not painting. --clean-alpha is an
optional local alpha-only cleanup and must only be used with user authorization.
"""
from pathlib import Path
import json, shutil, argparse, math
from PIL import Image, ImageOps, ImageFilter, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[1]
PREVIEW=ROOT/'design/角色资源预览/wylder'
REPORT={}
args=argparse.ArgumentParser()
args.add_argument('--clean-alpha',action='store_true')
opts=args.parse_args()
sources=json.loads((ROOT/'design/wylder/generated_sources.json').read_text(encoding='utf-8'))
def source(key):
    path=Path(sources[key]); local=PREVIEW/'source'/f'{key}.png'
    local.parent.mkdir(parents=True,exist_ok=True)
    if path.exists(): shutil.copy2(path,local)
    image=Image.open(local).convert('RGBA')
    if opts.clean_alpha and key not in ['background','card_upward_cut','card_sweep']:
        # Generated foreground is near-opaque, diffuse exterior glow is not.
        # Preserve enclosed foreground holes and thin antialiased outer edges.
        alpha=image.getchannel('A')
        core=alpha.point(lambda a:255 if a>=235 else 0)
        vicinity=core.filter(ImageFilter.MaxFilter(3))
        from PIL import ImageChops
        image.putalpha(ImageChops.multiply(alpha,vicinity))
    return image
def save(im,path):
    p=ROOT/path;p.parent.mkdir(parents=True,exist_ok=True);im.save(p,optimize=True)
    REPORT[path]={'size':list(im.size),'mode':im.mode,'bounds':list(im.getbbox()) if im.getbbox() else None,
        'corners_alpha':[im.getpixel(xy)[3] for xy in [(0,0),(im.width-1,0),(0,im.height-1),(im.width-1,im.height-1)]] if im.mode=='RGBA' else None,
        'approval':'draft'}
def fit(im,size,pad=8):
    im=im.crop(im.getbbox()); im=ImageOps.contain(im,(size[0]-2*pad,size[1]-2*pad),Image.Resampling.LANCZOS)
    out=Image.new('RGBA',size);out.alpha_composite(im,((size[0]-im.width)//2,(size[1]-im.height)//2));return out
def outline(im):
    out=Image.new('RGBA',im.size,'#EEE9D7');out.putalpha(im.getchannel('A').filter(ImageFilter.MaxFilter(7)));out.alpha_composite(im);return out
def atlas(category,name,path,w,h):
    p=ROOT/f'images/atlases/{category}.sprites/{name}.tres';p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(f'[gd_resource type="AtlasTexture" load_steps=2 format=3]\n\n[ext_resource type="Texture2D" path="res://{path}" id="1"]\n\n[resource]\natlas = ExtResource("1")\nregion = Rect2(0, 0, {w}, {h})\n',encoding='utf-8')

for key,out in [('idle','wylder_combat_character'),('attack','wylder_attack'),('hit','wylder_hit'),('block','wylder_block'),('death','wylder_death')]:
    if key not in sources: continue
    im=source(key); im=im.crop(im.getbbox())
    # Uniform rendered visible height (sword does not exceed head in these poses).
    im=ImageOps.contain(im,(2500,900 if key=='death' else 1360),Image.Resampling.LANCZOS)
    canvas=Image.new('RGBA',(2600,1500));canvas.alpha_composite(im,((2600-im.width)//2,1450-im.height))
    save(canvas,f'wylder_assets/combat_rig/{out}.png')
for key in ['rest','merchant']:
    save(fit(source(key),(1200,1250),12),f'wylder_assets/{"rest_site" if key=="rest" else key}/wylder_{"rest_site" if key=="rest" else key}.png')
portrait=source('portrait')
icon=fit(portrait,(128,128),6)
save(icon,'wylder_assets/character_icon_wylder.png');save(outline(icon),'wylder_assets/character_icon_wylder_outline.png')
save(fit(portrait,(80,80),4),'wylder_assets/map_marker_wylder.png')
button=Image.new('RGBA',(288,400),'#A96738')
head=portrait.crop(portrait.getbbox());head=head.crop((int(head.width*.3),0,head.width,head.height))
head=ImageOps.contain(head,(264,376),Image.Resampling.LANCZOS);button.alpha_composite(head,((288-head.width)//2,(400-head.height)//2))
save(button,'wylder_assets/char_select_wylder.png')
locked=ImageOps.grayscale(button).convert('RGBA');locked.putalpha(button.getchannel('A'));save(locked,'wylder_assets/char_select_wylder_locked.png')
save(source('background').convert('RGB').resize((2560,1200),Image.Resampling.LANCZOS),'wylder_assets/character_select_wylder_bg.png')
energy=source('energy')
for size,path in [(74,'wylder_assets/energy_icon/wylder_energy_card_icon.png'),(48,'wylder_assets/energy_icon/wylder_energy_font_icon.png'),(48,'images/packed/sprite_fonts/wylder_energy_icon.png')]:save(fit(energy,(size,size),3),path)
atlas('ui_atlas','card/energy_wylder','wylder_assets/energy_icon/wylder_energy_card_icon.png',74,74)
# The authored emblem is split into radial engine layers, not five overlaid copies.
full=fit(energy,(256,256),8)
bands=[(0,0.52),(0.52,0.71),(0.71,0.85),(0.85,0.94),(0.94,2)]
for i,(lo,hi) in enumerate(bands,1):
    mask=Image.new('L',(256,256));pix=mask.load()
    for y in range(256):
        for x in range(256):
            r=math.hypot(x-127.5,y-127.5)/120
            pix[x,y]=255 if lo<=r<hi else 0
    from PIL import ImageChops
    layer=full.copy();layer.putalpha(ImageChops.multiply(full.getchannel('A'),mask))
    save(layer,f'wylder_assets/energy_counter/wylder_orb_layer_{i}.png')
for key in ['point','rock','paper','scissors']:
    save(fit(source('hand_'+key),(512,768),8),f'wylder_assets/multiplayer_hands/multiplayer_hand_wylder_{key}.png')
for key,name in [('pursuit','wylder_pursuit_power'),('break','wylder_break_description_power'),('suppression','wylder_uppercut_strength_down_power')]:
    if key not in sources:continue
    icon=fit(source(key),(256,256),12)
    for folder in ['images/powers','powers']:save(icon,f'{folder}/{name}.png')
    atlas('power_atlas',name,f'images/powers/{name}.png',256,256)
relic=fit(source('earring'),(256,256),8)
save(relic,'wylder_assets/relics/wylder_single_earring.png');save(outline(relic),'wylder_assets/relics/wylder_single_earring_outline.png')
for key,name in [('card_upward_cut','wylder_stamp_upward_cut'),('card_sweep','wylder_stamp_sweep')]:
    image=source(key).convert('RGB').resize((1000,760),Image.Resampling.LANCZOS)
    save(image,f'design/卡图预览/wylder/{name}.png')
    # Isolated branch/test resources: not promoted to main or approved.
    save(image,f'images/packed/card_portraits/wylder/{name}.png')
entries=[(p,Image.open(ROOT/p).convert('RGBA')) for p in REPORT if p.startswith('wylder_assets/') and not p.endswith('_outline.png')]
cols=5;cell=(280,245);sheet=Image.new('RGB',(cols*cell[0],((len(entries)+cols-1)//cols)*cell[1]),'#30353F');d=ImageDraw.Draw(sheet)
for i,(name,im) in enumerate(entries):
    im=ImageOps.contain(im,(264,210));x=(i%cols)*280;y=(i//cols)*245
    sheet.paste(im,(x+(280-im.width)//2,y),im);d.text((x+4,y+212),Path(name).name[:36],fill='white')
PREVIEW.mkdir(parents=True,exist_ok=True);sheet.save(PREVIEW/'review.jpg')
(ROOT/'design/wylder/asset_report.json').write_text(json.dumps(REPORT,indent=2),encoding='utf-8')
print(f'Prepared {len(REPORT)} draft assets. Local alpha cleanup: {opts.clean_alpha}')
