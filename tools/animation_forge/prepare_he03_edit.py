#!/usr/bin/env python3
"""HE03-3 edit candidate: same-hand tail, reversed preparation then forward cut.
Run with project animation extraction environment; ffmpeg, OpenCV, Pillow required.
No runtime integration or approval implied.
"""
from pathlib import Path
import argparse,subprocess,json,hashlib,sys
import numpy as np
from PIL import Image
sys.path.insert(0,str(Path(__file__).resolve().parent))
from matte import matte_dark
from imaging import premultiplied_resize
ap=argparse.ArgumentParser();ap.add_argument('--source',required=True);ap.add_argument('--output',required=True);args=ap.parse_args()
source=Path(args.source);out=Path(args.output);out.mkdir(parents=True,exist_ok=True)
# Timestamps refer to 60 FPS source. Avoid hand-transfer frames before 3.30s.
times=np.r_[np.linspace(3.90,3.30,6),np.linspace(3.30,4.45,19)[1:]]
ids=np.rint(times*60).astype(int).tolist()
raw=subprocess.check_output(['ffmpeg','-v','error','-i',str(source),'-f','rawvideo','-pix_fmt','rgb24','-']);rgb=np.frombuffer(raw,np.uint8).reshape(-1,960,960,3)
unique=sorted(set(ids+[0]));cache={};yy,xx=np.indices((960,960));wm=(yy>880)&(xx>720)
for i in unique:
 rgba=matte_dark(rgb[i],wm=wm)
 # Preserve isolated bright sword/bone strokes that GrabCut can discard.
 bg=np.median(np.concatenate([rgb[i,:80,:80].reshape(-1,3),rgb[i,:80,-80:].reshape(-1,3)]),axis=0)
 diff=np.sqrt(np.mean((rgb[i].astype(float)-bg)**2,axis=2));protect=(diff>45)&(~wm)
 rgba[protect,:3]=rgb[i][protect];rgba[protect,3]=255
 cache[i]=Image.fromarray(rgba,'RGBA');print('matte',i,flush=True)
b=cache[0].getchannel('A').getbbox();scale=344/(b[3]-b[1]);small={i:premultiplied_resize(im,scale) for i,im in cache.items()};boxes=[im.getchannel('A').getbbox() for im in small.values()];crop=(min(b[0] for b in boxes)-8,min(b[1] for b in boxes)-8,max(b[2] for b in boxes)+8,max(b[3] for b in boxes)+8);small={i:im.crop(crop) for i,im in small.items()};base=small[0];b=base.getchannel('A').getbbox();anchor=[(b[0]+b[2])/2,b[3]-1];frames=[small[i] for i in ids];w,h=base.size
prefix='HE03_后撤反击_剪辑候选_20261025';base.save(out/(prefix+'_参考.png'))
cols=max(1,2048//w);rows=max(1,2048//h);per=cols*rows;pages=[]
for start in range(0,len(frames),per):
 chunk=frames[start:start+per];atlas=Image.new('RGBA',(cols*w,((len(chunk)+cols-1)//cols)*h))
 for j,im in enumerate(chunk):atlas.paste(im,((j%cols)*w,j//cols*h))
 name=prefix+f'_图集_{len(pages):02}.png';atlas.save(out/name);pages.append(name)
preview=[base]*18+frames+[base]*18;gif=[]
for im in preview:
 bg=Image.new('RGBA',im.size,'#252d3a');bg.alpha_composite(im);gif.append(bg.convert('RGB'))
gif[0].save(out/(prefix+'.gif'),save_all=True,append_images=gif[1:],duration=[round((i+1)*100/24)*10-round(i*100/24)*10 for i in range(len(gif))],loop=0,disposal=2)
data={'task':'HE03','status':'local edit candidate, pending user review','source':str(source),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'source_duration':5.016667,'source_fps':60,'source_indices':ids,'fps':24,'frames':len(frames),'duration':len(frames)/24,'canvas':[w,h],'anchor':anchor,'pages':pages,'cols':cols,'per_page':per,'reference':prefix+'_参考.png','method':'reverse source3.90 to3.30 for preparation; forward3.30 to4.45 for cut and recovery; no generated poses, no root translation','release_marker_candidate_frame':12,'actual_command_wait':None,'game_duration':None,'limits':['Weak retreat; this is a short slash candidate.','Reverse preparation reverses secondary cloth motion.','Context uses source first-frame pose; formal idle integration untested.','No game-speed or runtime integration validation.']}
(out/(prefix+'.json')).write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
# Inspect first / peak / final on dark and bright backgrounds.
sheet=Image.new('RGB',(960,990),'#333')
for row,idx in enumerate([0,6,len(frames)-1]):
 for col,c in enumerate(['#222936','#eee','#f500b4']):
  im=frames[idx].copy();im.thumbnail((310,310));bg=Image.new('RGBA',(320,320),c);bg.alpha_composite(im,((320-im.width)//2,(320-im.height)//2));sheet.paste(bg.convert('RGB'),(col*320,row*330))
sheet.save(out/(prefix+'_审阅.jpg'))
print(json.dumps(data,ensure_ascii=False),flush=True)
# Package the same approved cells; the resource embeds its textures.
import tempfile
from forge import pack
with tempfile.TemporaryDirectory(prefix='he03-pack-') as temp:
 temp=Path(temp);fd=temp/'frames';fd.mkdir()
 for i,im in enumerate(frames):im.save(fd/f'frame_{i:03}.png')
 cfg={'frames_dir':str(fd),'cell':[w,h],'anchor':anchor,'action':'attack_b','fps':24,'loop':False,'output':str((out/'HE03_攻击B_SpriteFrames_20261025.res').resolve())}
 cp=temp/'config.json';cp.write_text(json.dumps(cfg));pack(cp)
