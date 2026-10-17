from pathlib import Path
import json
from PIL import Image,ImageDraw,ImageFont
root=Path.cwd(); out=root/'design/动画预览/revenant'; assets=root/'revenant_assets/combat/sequence_candidates'
m=json.loads((assets/'动画提取记录.json').read_text(encoding='utf-8'));font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',20)
clips=[]
for name,label in [('战斗待机','待机'),('爪击攻击','爪击')]:
 a=m['actions'][name];pages=[Image.open(assets/p).convert('RGBA') for p in a['atlas_pages']];frames=[]
 for k in a['atlas_indices']:
  x=k%4*416;y=k%16//4*560;frames.append(pages[k//16].crop((x,y,x+416,y+560)))
 clips.append((label,frames))
for prefix,label in [('RV03_拨弦','拨弦'),('RV04_祷告','祷告')]:
 frames=[Image.open(p).convert('RGBA') for p in sorted(out.glob(prefix+'_透明帧_v01_*.png'))];assert frames;clips.append((label,frames))
allframes=[];durations=[]
for label,frames in clips:
 b=frames[0].getchannel('A').getbbox();scale=420/(b[3]-b[1]);cx=(b[0]+b[2])/2;foot=b[3]
 for i,im in enumerate(frames):
  bg=Image.new('RGBA',(512,544),(30,34,43,255));dr=ImageDraw.Draw(bg);dr.text((20,12),f'{label}  ·  素材节奏预览',font=font,fill=(232,235,241));dr.line((32,506,480,506),fill=(70,76,88),width=1)
  resized=im.resize((round(im.width*scale),round(im.height*scale)),Image.Resampling.LANCZOS);bg.alpha_composite(resized,(round(256-cx*scale),round(506-foot*scale)));allframes.append(bg.convert('RGB'));durations.append(round((i+1)*1000/24/10)*10-round(i*1000/24/10)*10)
path=out/'复仇者_待机-爪击-拨弦-祷告_连续预览_v02.gif'
allframes[0].save(path,save_all=True,append_images=allframes[1:],duration=durations,loop=0,optimize=False,disposal=2)
with Image.open(path) as gif:
 total=0
 for i in range(gif.n_frames):gif.seek(i);total+=gif.info.get('duration',0)
 assert total==sum(durations)
 print(json.dumps({'path':str(path),'duration_ms':total,'bytes':path.stat().st_size},ensure_ascii=False))
allframes[135].save(root/'.tmp/revenant_gif_v02_check.png')
