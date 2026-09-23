"""Normalize Duchess ImageGen candidates without changing their originals.

Local alpha cleanup was explicitly authorized by the user. Background removal is
connectivity based: enclosed ivory costume panels are never globally color-keyed.
Run with the bundled Python (Pillow + numpy); no network or model downloads.
"""
from pathlib import Path
import json
import re
import argparse
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageOps

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "design/卡图预览/duchess"
REVIEW = SOURCE / "processed_review"
REPORT = {}
CARD_ART_REUSE = {}
POWER_OUTPUTS = {
    'duchess_echo_description_power': ['duchess_reaction_description_power'],
    'duchess_measured_breath_power': ['duchess_reaction_block_power'],
    'duchess_memory_power': ['duchess_moment_description_power', 'duchess_moment_power'],
    'duchess_opening_dance_power': ['duchess_dodge_at_turn_start_power'],
    'duchess_reprise_guard_power': ['duchess_moment_five_block_power'],
    'duchess_reprise_step_power': ['duchess_moment_five_draw_power'],
    'duchess_step_power': ['duchess_shuffle_block_power'],
    'duchess_veil_power': [],
    'duchess_veil_reward_power': ['duchess_dodge_moment_power'],
}
# Reviewed enclosed background regions, in original pixel coordinates. Keeping
# these seeds explicit prevents an automatic white-removal pass eating costumes.
HOLES = {
    'hit': [(600,345)], 'rest': [(585,583)],
    'duchess_veil_power': [(285,710),(690,680)],
    'duchess_veil_vial': [(625,215)],
    'duchess_memory_power': [(545,170)],
    'duchess_mended_pocketwatch': [(615,140)],
    'duchess_old_pocketwatch': [(754,151)],
}


def save(image, relative):
    path = ROOT / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, optimize=True)
    REPORT[relative] = {"size": list(image.size), "mode": image.mode,
                        "bounds": list(image.getbbox()) if image.getbbox() else None}


def cutout(path):
    rgba = Image.open(path).convert("RGBA")
    pixels = np.array(rgba)
    rgb = pixels[:, :, :3].astype(np.int16)
    candidate = ((rgb.mean(axis=2) >= 224) & (np.ptp(rgb, axis=2) <= 20)) | (pixels[:, :, 3] == 0)
    mask = Image.fromarray((candidate * 255).astype('uint8'))
    # A padded border connects every exterior component, including cut-off hands.
    flood = ImageOps.expand(mask, border=1, fill=255)
    ImageDraw.floodfill(flood, (0, 0), 128)
    for x,y in HOLES.get(path.stem,[]):
        if flood.getpixel((x+1,y+1)) != 255:
            raise ValueError(f'Background seed is not bright neutral: {path.name} {(x,y)}')
        ImageDraw.floodfill(flood, (x+1,y+1), 128)
    background = np.array(flood)[1:-1, 1:-1] == 128
    alpha = Image.fromarray(np.where(background, 0, pixels[:, :, 3]).astype('uint8'))
    # Remove one source pixel of checkerboard/white antialias fringe, then preserve
    # antialiasing when downsampling the finished RGBA asset.
    alpha = alpha.filter(ImageFilter.MinFilter(3))
    rgba.putalpha(alpha)
    return rgba


def fit(image, size, padding=8):
    image = image.crop(image.getbbox())
    image = ImageOps.contain(image, (size[0]-padding*2, size[1]-padding*2), Image.Resampling.LANCZOS)
    canvas = Image.new('RGBA', size)
    canvas.alpha_composite(image, ((size[0]-image.width)//2, (size[1]-image.height)//2))
    return canvas


def sheet(entries, name, cell=(240, 220), columns=5):
    canvas = Image.new('RGB', (cell[0]*columns, cell[1]*((len(entries)+columns-1)//columns)), '#292638')
    draw = ImageDraw.Draw(canvas)
    for i, (label, image) in enumerate(entries):
        thumb = fit(image.convert('RGBA'), (cell[0],cell[1]-24), 8)
        x,y = (i%columns)*cell[0], (i//columns)*cell[1]
        canvas.paste(thumb, (x,y), thumb)
        draw.text((x+8,y+cell[1]-22), label, fill='white')
    REVIEW.mkdir(parents=True, exist_ok=True)
    canvas.save(REVIEW / name)


def atlas(folder, name, texture, size):
    target = ROOT / f'images/atlases/{folder}.sprites/{name}.tres'
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text('[gd_resource type="AtlasTexture" load_steps=2 format=3]\n\n'
        f'[ext_resource type="Texture2D" path="res://{texture}" id="1"]\n\n'
        '[resource]\natlas = ExtResource("1")\n'
        f'region = Rect2(0, 0, {size[0]}, {size[1]})\n', encoding='utf-8')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--review-only', action='store_true')
    args = parser.parse_args()
    rows = json.loads((ROOT / 'design/duchess/cards.json').read_text(encoding='utf-8'))
    cards = []
    for row in rows:
        art_key = CARD_ART_REUSE.get(row[0], row[0])
        image = Image.open(SOURCE / 'cards' / (art_key+'.png')).convert('RGB')
        size = (606,852) if row[6]=='Ancient' else (1000,760)
        image = ImageOps.fit(image, size, Image.Resampling.LANCZOS)
        cards.append((row[0],image))
        if not args.review_only:
            slug = re.sub(r'(?<!^)(?=[A-Z])','_','Duchess'+art_key).lower()
            save(image, f'images/packed/card_portraits/duchess/{slug}.png')
    for start in range(0,len(cards),25):
        sheet(cards[start:start+25], f'cards_{start//25+1}.jpg')
    icons = {p.stem:fit(cutout(p),(256,256),12) for p in sorted((SOURCE/'icons').glob('*.png'))}
    sheet(list(icons.items()),'icons.png',cell=(260,280),columns=5)
    sheet(list(icons.items()),'icons_small.png',cell=(180,64),columns=4)
    poses = {p.stem:cutout(p) for p in sorted((SOURCE/'scenes').glob('*.png')) if p.stem!='background'}
    poses['idle'] = cutout(SOURCE / 'sprites/idle.png')
    sheet(list(poses.items()),'poses.png',cell=(380,460),columns=4)
    if args.review_only:
        return
    for name, icon in icons.items():
        if name.endswith('_power'):
            for output_name in POWER_OUTPUTS.get(name, [name]):
                for folder in ('images/powers','powers'):
                    save(icon,f'{folder}/{output_name}.png')
                atlas('power_atlas',output_name,f'images/powers/{output_name}.png',(256,256))
        elif name in ('duchess_silver_perfume','duchess_veil_vial','duchess_memory_draught'):
            save(icon,f'duchess_assets/potions/{name}.png')
            atlas('potion_atlas',name,f'duchess_assets/potions/{name}.png',(256,256))
        elif name!='duchess_energy':
            save(icon,f'duchess_assets/relics/{name}.png')
    for key, name in [('idle','duchess_combat_character'),('attack','duchess_attack'),('hit','duchess_hit')]:
        # Crouching attack and recoiling hit retain approximately idle head size;
        # equal silhouette heights would enlarge a crouching character.
        heights = {'idle':1200,'attack':930,'hit':1100}
        height = heights[key]
        img = poses[key].crop(poses[key].getbbox())
        img = img.resize((round(img.width*height/img.height),height),Image.Resampling.LANCZOS)
        canvas = Image.new('RGBA',(1600,1280))
        if img.width > 1580:
            raise ValueError(f'Pose does not fit canvas: {key}')
        canvas.alpha_composite(img,((1600-img.width)//2,1240-height))
        save(canvas,f'duchess_assets/combat_rig/{name}.png')
    for key in ('rest','merchant'):
        save(fit(poses[key],(1400,1280),40),f'duchess_assets/{"rest_site" if key=="rest" else key}/duchess_{"rest_site" if key=="rest" else key}.png')
    for key in ('rock','paper','scissors'):
        save(fit(poses[key],(627,627),12),f'duchess_assets/multiplayer_hands/multiplayer_hand_duchess_{key}.png')
    point = cutout(ROOT/'duchess_assets/multiplayer_hands/duchess_point.png')
    save(fit(point,(422,1200),8),'duchess_assets/multiplayer_hands/multiplayer_hand_duchess_point.png')
    idle = poses['idle']
    # Deliberate head-and-shoulders portrait crop (silver bob + eye mask).
    head = idle.crop((480,25,755,320))
    for filename,size in [('character_icon_duchess',(64,64)),('map_marker_duchess',(49,64)),('char_select_duchess',(132,195))]:
        portrait = fit(head if size[0]<100 else idle.crop((430,25,820,600)),size,3)
        save(portrait,f'duchess_assets/{filename}.png')
        if filename=='character_icon_duchess':
            outline = Image.new('RGBA',size,(224,232,247,0))
            outline.putalpha(portrait.getchannel('A').filter(ImageFilter.MaxFilter(5)))
            outline.alpha_composite(portrait)
            # A fixed clear margin avoids alpha expansion into corners.
            save(fit(outline,size,2),'duchess_assets/character_icon_duchess_outline.png')
        if filename=='char_select_duchess':
            locked = ImageOps.grayscale(portrait).convert('RGBA')
            locked.putalpha(portrait.getchannel('A'))
            save(locked,'duchess_assets/char_select_duchess_locked.png')
    energy = icons['duchess_energy']
    save(energy.resize((74,74),Image.Resampling.LANCZOS),'duchess_assets/energy_icon/duchess_energy_card_icon.png')
    save(energy.resize((24,24),Image.Resampling.LANCZOS),'duchess_assets/energy_icon/duchess_energy_font_icon.png')
    save(energy.resize((24,24),Image.Resampling.LANCZOS),'images/packed/sprite_fonts/duchess_energy_icon.png')
    atlas('ui_atlas','card/energy_duchess','duchess_assets/energy_icon/duchess_energy_card_icon.png',(74,74))
    # Partition the existing generated emblem into disjoint radial layers. These
    # are crops/masks, not independently drawn substitute icon artwork.
    yy,xx=np.ogrid[:256,:256]
    radius=np.sqrt((xx-127.5)**2+(yy-127.5)**2)
    for i,(low,high) in enumerate([(0,60),(60,80),(80,96),(96,112),(112,182)],1):
        layer=energy.copy()
        layer.putalpha(Image.fromarray((np.array(energy.getchannel('A'))*((radius>=low)&(radius<high))).astype('uint8')))
        save(layer,f'duchess_assets/energy_counter/duchess_orb_layer_{i}.png')
    background=ImageOps.fit(Image.open(SOURCE/'scenes/background.png').convert('RGB'),(2560,1200),Image.Resampling.LANCZOS)
    save(background,'duchess_assets/character_select_duchess_bg.png')
    # The production select control uses this central crop in a 16:9 viewport.
    background.crop((320,60,2240,1140)).resize((1280,720)).save(REVIEW/'background_safe_crop.jpg')
    (ROOT/'design/duchess/asset_report.json').write_text(json.dumps(REPORT,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(f'Wrote {len(REPORT)} PNG assets; originals retained; review sheets: {REVIEW}')


if __name__=='__main__':
    main()
