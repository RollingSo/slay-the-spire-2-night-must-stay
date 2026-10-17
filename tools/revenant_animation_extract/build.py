"""Extract user-approved Revenant clips; preview assets only, no production replacement.
Python dependencies: rembg[cpu], opencv-python-headless, numpy, Pillow, scipy; FFmpeg.
Run with --samples for the three-frame alpha review before the full extraction.
"""
from pathlib import Path
import os,json,subprocess,hashlib,argparse
os.environ.setdefault('OMP_NUM_THREADS','4')
import cv2
import numpy as np
from PIL import Image,ImageDraw
from rembg import new_session,remove
from scipy import ndimage as ndi
from clothing_color import correct as correct_clothing_color
ROOT=Path(__file__).resolve().parents[2]
SOURCE=Path(__file__).resolve().parent
OUT=ROOT/'revenant_assets/combat/sequence_candidates'
OUT.mkdir(parents=True,exist_ok=True)
CACHE=ROOT/'.tmp/revenant-extracted';CACHE.mkdir(parents=True,exist_ok=True)
FPS=24; W=832;H=1120

def decode(path):
 raw=subprocess.check_output(['ffmpeg','-v','error','-i',str(path),'-vf','fps=24','-f','rawvideo','-pix_fmt','rgb24','-'])
 return np.frombuffer(raw,np.uint8).reshape(-1,H,W,3)

def opaque_interior(rgb,a):
 # Restore solid material interiors using both the existing subject mask and
 # source color evidence. Background openings must satisfy neither condition.
 # Erode the support to keep silhouette antialiasing and finger gaps untouched.
 bg=np.median(rgb[:25,:25].reshape(-1,3),axis=0)
 distance=np.linalg.norm(rgb.astype(np.float32)-bg,axis=2)
 support=(a>.025)&(distance>18)
 yy,xx=np.indices(a.shape)
 closed=ndi.binary_closing(support,iterations=2)
 # Do not bridge the thin strings or their background gaps in the lyre window.
 support=np.where((xx<590)|(yy>400),closed,support)
 core=ndi.binary_erosion(support,iterations=1)
 # Central opaque costume/forearm protection includes near-white wraps which
 # cannot be distinguished from the background by color distance alone.
 central=(xx>350)&(xx<590)&(yy>250)&(yy<650)&(a>.025)
 core |= ndi.binary_erosion(central,iterations=1)
 # Recover actual holes (alpha == 0) erased inside the opaque garment. The
 # protected region follows the torso/sleeve and excludes the lyre aperture.
 garment=((xx>350)&(xx<590)&(yy>=300)&(yy<900))|((xx>350)&(xx<530)&(yy>220)&(yy<300))
 enclosed=ndi.binary_fill_holes(a>.05)
 core |= ndi.binary_erosion(enclosed & garment,iterations=1)
 fixed=np.maximum(a,core.astype(np.float32))
 return fixed,core


def matte(rgb,session):
 # Model segmentation protects opaque pale clothing. Remove near-background only
 # inside uncertain model edges, never globally key pale subject pixels.
 a=np.array(remove(Image.fromarray(rgb),session=session,only_mask=True)).astype(np.float32)/255
 a=np.clip((a-.02)/.96,0,1)
 # The pale dress receives low model confidence in early frames. Make the
 # segmented interior opaque, and fill enclosed dress holes only below the
 # instrument window. Never fill the lyre aperture as a silhouette hole.
 core=a>.06
 filled=ndi.binary_fill_holes(core)
 yy,xx=np.indices(core.shape)
 core |= filled & (yy>460) & (xx<700)
 interior=ndi.binary_erosion(core,iterations=1)
 a=np.maximum(a,ndi.gaussian_filter((interior & (yy>350) & (xx>330) & (xx<680)).astype(float),.5))
 a,_=opaque_interior(rgb,a)
 # White exterior gaps in lyre need separate review; retain model results here.
 a[:3]=0;a[-3:]=0;a[:,:3]=0;a[:,-3:]=0
 # Watermark is outside the feet at far lower right, not a subject feature.
 a[1070:,730:]=0
 # Unmix white spill only where alpha is partial; preserve opaque source colors.
 bg=np.median(rgb[:25,:25].reshape(-1,3),axis=0)
 color=np.clip((rgb.astype(float)-bg*(1-a[...,None]))/np.maximum(a[...,None],.08),0,255)
 rgba=np.dstack([color,a*255]).astype(np.uint8);rgba[a<.01]=0
 return Image.fromarray(rgba)

def review(samples,path):
 thumb=(312,420);sheet=Image.new('RGB',(936,450*len(samples)),'#444');d=ImageDraw.Draw(sheet)
 for row,(label,im) in enumerate(samples):
  im=im.copy();im.thumbnail(thumb)
  for col,c in enumerate([(245,245,245),(28,32,42),None]):
   bg=Image.new('RGBA',thumb,c or (130,130,130))
   if c is None:
    dr=ImageDraw.Draw(bg)
    for y in range(0,420,16):
     for x in range(0,312,16):
      if (x//16+y//16)%2:dr.rectangle((x,y,x+15,y+15),fill=(190,190,190))
   bg.alpha_composite(im,((312-im.width)//2,0));sheet.paste(bg.convert('RGB'),(col*312,row*450+25))
  d.text((8,row*450+5),label,fill='white')
 sheet.save(path)

def atlas(frames,name):
 # A fixed half-resolution full canvas for all actions; no per-frame cropping.
 # Visual height remains >2x the current in-game approved 216px.
 fw,fh=416,560; tiles=[];bounds=[]
 for im in frames:
  im=im.resize((fw,fh),Image.Resampling.LANCZOS);a=np.asarray(im).copy();a[a[:,:,3]==0,:3]=0
  im=Image.fromarray(a);tiles.append(im);bounds.append(im.getchannel('A').getbbox())
 unique=[];lookup={};indices=[]
 for im in tiles:
  key=hashlib.sha256(im.tobytes()).digest()
  if key not in lookup:lookup[key]=len(unique);unique.append(im)
  indices.append(lookup[key])
 pages=[]
 for start in range(0,len(unique),16):
  page=Image.new('RGBA',(fw*4,fh*4))
  for k,im in enumerate(unique[start:start+16]):page.paste(im,((k%4)*fw,(k//4)*fh))
  path=OUT/f'{name}_图集_{start//16+1:02}.png';page.save(path);pages.append(path)
 return tiles,bounds,pages,indices

def resource(name,frames,pages,loop,indices):
 count=len(frames);lines=[f'[gd_resource type="SpriteFrames" load_steps={1+len(pages)+count} format=3]','']
 for i,p in enumerate(pages):lines.append(f'[ext_resource type="Texture2D" path="res://revenant_assets/combat/sequence_candidates/{p.name}" id="{i+1}"]')
 for i in range(count):
  k=indices[i]
  lines.extend(['',f'[sub_resource type="AtlasTexture" id="F{i}"]',f'atlas = ExtResource("{k//16+1}")',f'region = Rect2({k%4*416}, {k%16//4*560}, 416, 560)','filter_clip = true'])
 lines+=['','[resource]','animations = [{','"frames": [']
 lines += [f'{{"duration": 1.0, "texture": SubResource("F{i}")}}'+(',' if i<count-1 else '') for i in range(count)]
 lines+= ['],',f'"loop": {str(loop).lower()},',f'"name": &"{name}",','"speed": 24.0','}]']
 (OUT/f'{name}_动画.tres').write_text('\n'.join(lines)+'\n')

def main():
 ap=argparse.ArgumentParser();ap.add_argument('--samples',action='store_true');ap.add_argument('--preview-only',action='store_true');args=ap.parse_args()
 print('Loading segmentation model',flush=True);session=new_session('isnet-general-use',providers=['CPUExecutionProvider'])
 clips={n:decode(SOURCE/f'{n}_源视频.mp4') for n in ['战斗待机','爪击攻击']}
 if args.samples:
  samples=[]
  for n,fs in clips.items():
   for i in [0,48,119]:
    im=matte(fs[i],session);im.save(CACHE/f'{n}_{i:03}.png');samples.append((f'{n.encode("ascii","ignore").decode()} frame {i} / {i/24:.3f}s',im))
  review(samples,SOURCE/'透明小样审查.jpg');print('Samples ready',flush=True);return
 allframes={}
 for n,fs in clips.items():
  folder=CACHE/n;folder.mkdir(exist_ok=True);arr=[]
  keys=sorted(set(list(range(0,len(fs),3))+[len(fs)-1]))
  masks={}
  for i in keys:
   p=folder/f'key_v2_{i:03}.png'
   if not p.exists():matte(fs[i],session).save(p)
   masks[i]=np.asarray(Image.open(p).convert('RGBA'))[:,:,3].astype(np.float32)/255
   print(n,'alpha key',i,'/',len(fs),flush=True)
  gray=[cv2.cvtColor(cv2.resize(x,(416,560)),cv2.COLOR_RGB2GRAY) for x in fs]
  gridx,gridy=np.meshgrid(np.arange(W,dtype=np.float32),np.arange(H,dtype=np.float32))
  def warp(i,k):
   flow=cv2.calcOpticalFlowFarneback(gray[i],gray[k],None,.5,4,21,4,7,1.5,0)
   flow=cv2.resize(flow,(W,H))*2
   return cv2.remap(masks[k],gridx+flow[:,:,0],gridy+flow[:,:,1],cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT)
  for i,rgb in enumerate(fs):
   if i in masks:a=masks[i]
   else:
    lo=max(k for k in keys if k<i);hi=min(k for k in keys if k>i);t=(i-lo)/(hi-lo)
    a=warp(i,lo)*(1-t)+warp(i,hi)*t
   a,solid=opaque_interior(rgb,a)
   if n=='爪击攻击':rgb,_=correct_clothing_color(rgb)
   bg=np.median(rgb[:25,:25].reshape(-1,3),axis=0)
   color=np.clip((rgb.astype(float)-bg*(1-a[...,None]))/np.maximum(a[...,None],.08),0,255)
   rgba=np.dstack([color,a*255]).astype(np.uint8);rgba[a<.01]=0
   arr.append(Image.fromarray(rgba))
  allframes[n]=arr
 # User-approved slow gesture: preserve full source path, remap later for game.
 # Idle: a continuous 2.04s interval with similar endpoint pose; endpoint
 # velocities still need loop review. Ping-pong fallback is not silently used.
 segment=allframes['战斗待机'][54:104]
 # Low-amplitude idle only: cosine-eased forward/reverse playback avoids
 # crossfaded duplicate hands/strings, with zero speed at turning points.
 idle_indices=[int(round((1-np.cos(2*np.pi*i/96))*.5*(len(segment)-1))) for i in range(96)]
 idle=[segment[k] for k in idle_indices]
 attack=allframes['爪击攻击'][:110] # 0..4.5417: return to lyre, omit final hold
 metadata={'status':'user-approved material v4: enclosed sleeve-hole repair; engine and combat validation pending','color_correction':'claw skirt warm patch only; source-luminance-preserving cool-grey chroma; no alpha/geometry changes by color stage','fps':24,'canvas':[416,560],'source_canvas':[832,1120],'alpha_model':'isnet-general-use keyframes every 3 frames plus bidirectional optical-flow mask propagation; source-color opaque interior restoration v2','opaque_interior':{'version':2,'mask_threshold':.025,'source_bg_rgb_distance_min':18,'erosion_pixels':1,'applied_after_optical_flow':True},'source':{},'actions':{},'known_limits':['Model alpha may lose hair/lyre strings and retain pale background gaps; inspect supplied light/dark checker review.','Idle uses cosine-eased ping-pong of low-amplitude motion; reverse cloth motion remains a stylized compromise.','No production scene or command delay changes.']}
 for n,frames,loop in [('战斗待机',idle,True),('爪击攻击',attack,False)]:
  tiles,bounds,pages,indices=atlas(frames,n);resource(n,tiles,pages,loop,indices)
  # Animated lossless WebP preserves alpha; ZIP retains per-frame RGBA master.
  if not args.preview_only:
   tiles[0].save(CACHE/f'{n}_透明预览.webp',save_all=True,append_images=tiles[1:],duration=1000/24,loop=0 if loop else 1,lossless=True)
   import zipfile,io
   with zipfile.ZipFile(CACHE/f'{n}_透明帧.zip','w',zipfile.ZIP_DEFLATED) as z:
    for i,im in enumerate(frames):
     buf=io.BytesIO();im.save(buf,format='PNG');z.writestr(f'{n}_{i:03}.png',buf.getvalue())
  # First, middle and last against all backgrounds.
  review([(f'{n} {k}',frames[k]) for k in [0,len(frames)//2,len(frames)-1]],SOURCE/f'{n}_透明边缘审查.jpg')
  metadata['actions'][n]={'frame_count':len(frames),'atlas_indices':indices,'resource_duration':len(frames)/24,'loop':loop,'bounds':bounds,'atlas_pages':[p.name for p in pages],'decoded_rgba_mib':len(pages)*1664*2240*4/1048576}
  metadata['source'][n]={'sha256':hashlib.sha256((SOURCE/f'{n}_源视频.mp4').read_bytes()).hexdigest(),'duration':5.016667,'frame_count':121}
 metadata['timing']={'idle_source_interval':[2.25,4.333333],'idle_blend_frames':0,'idle_loop_method':'96-frame cosine-eased ping-pong, duplicate atlas images reused','attack_source_interval':[0,110/24],'attack_release_marker':None,'proposed_source_marker_seconds':.625,'proposed_source_marker_frame':15,'normal_target_duration':1.0,'fast_target_duration':.5,'default_attack_wait':.15,'actual_game_duration':None,'note':'Spell-gesture approved by user; gesture peak and game timing must be chosen in preview, not assumed from this source.'}
 (OUT/'动画提取记录.json').write_text(json.dumps(metadata,ensure_ascii=False,indent=2)+'\n')
 print('Complete',flush=True)
if __name__=='__main__':main()
