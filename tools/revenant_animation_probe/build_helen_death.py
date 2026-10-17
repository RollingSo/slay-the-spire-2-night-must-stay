from pathlib import Path
import sys,json,subprocess,hashlib
import numpy as np
from PIL import Image,ImageDraw,ImageFilter
r=Path.cwd();sys.path.insert(0,str(r/'.tmp/video_review_deps'));import cv2,imageio_ffmpeg
cv2.setNumThreads(2);out=r/'design/动画预览/revenant';ff=imageio_ffmpeg.get_ffmpeg_exe()
params=json.loads((out/'HE01_待机_校色参数_v01.json').read_text(encoding='utf-8'))
def matte(rgb,he):
 h,w=rgb.shape[:2];y,x=np.indices((h,w));left=np.median(rgb[:,:50],axis=1).astype(float);right=np.median(rgb[:,-45:],axis=1).astype(float);bg=left[:,None]+(right-left)[:,None]*np.linspace(0,1,w)[None,:,None];dist=np.linalg.norm(rgb.astype(float)-bg,axis=2)
 mask=np.where(dist>32,3,2).astype('uint8');mask[dist>85]=1;mask[:12]=0;mask[:,:10]=0;mask[:,-2:]=0;mask[-10:]=0
 if he:
  mask[(y>820)&(rgb.mean(axis=2)<90)]=0;mask[y>890]=0;mask[(y>870)&(x>750)]=0
 else:mask[(y>1000)&(rgb.max(axis=2)<140)]=0;mask[(y>1060)&(x>685)]=0
 # Lower-resolution segmentation guides the full-resolution color-distance boundary.
 small=cv2.resize(rgb,None,fx=.5,fy=.5);m=cv2.resize(mask,(small.shape[1],small.shape[0]),interpolation=cv2.INTER_NEAREST);cv2.setRNGSeed(137);cv2.grabCut(cv2.cvtColor(small,cv2.COLOR_RGB2BGR),m,None,np.zeros((1,65)),np.zeros((1,65)),3,cv2.GC_INIT_WITH_MASK)
 b=cv2.resize(np.isin(m,[1,3]).astype('uint8'),(w,h),interpolation=cv2.INTER_NEAREST);b[dist<18]=0
 if he:b[(y>825)&(rgb[:,:,2].astype(float)>rgb[:,:,0]*1.18)&(rgb[:,:,2].astype(float)-rgb[:,:,0]>17)]=0
 else:
  f=rgb.astype(float);skin=(f[:,:,0]>f[:,:,1]+10)&(f[:,:,1]>f[:,:,2]+1)&(f[:,:,0]>150);feet=((x>295)&(x<395))|((x>550)&(x<687));b[(y>1005)&~(skin&feet)]=0
 n,labels,stats,_=cv2.connectedComponentsWithStats(b,8);keep=np.zeros(n,bool);keep[1:]=stats[1:,cv2.CC_STAT_AREA]>12;b=keep[labels].astype('uint8');inner=cv2.erode(b,np.ones((3,3),np.uint8));a=b.astype(float);edge=(b>0)&(inner==0);a[edge]=np.clip((dist[edge]-18)/30,0,1)
 return np.dstack([rgb,np.rint(a*255).astype('uint8')])
def corrected(a):
 lab=cv2.cvtColor(a[:,:,:3].astype(np.float32)/255,cv2.COLOR_RGB2LAB)
 for c in [1,2]:lab[:,:,c]+=np.interp(lab[:,:,0],params['L_knots'],np.array(params['ab_offsets'])[:,c-1])
 b=a.copy();b[:,:,:3]=np.rint(np.clip(cv2.cvtColor(lab,cv2.COLOR_LAB2RGB),0,1)*255).astype('uint8');return b
report={}
for prefix,he in [('HE01_待机',True),('RV07_死亡',False)]:
 w,h=(960,960) if he else (832,1120);src=out/(prefix+'_源视频.mp4');raw=subprocess.check_output([ff,'-v','error','-i',str(src),'-vf','fps=24','-f','rawvideo','-pix_fmt','rgb24','-']);video=np.frombuffer(raw,np.uint8).reshape(-1,h,w,3)
 if he:
  # Four-second eased loop within the stable-colored section, avoiding the generated opening transition.
  ids=np.rint(24+(1-np.cos(np.arange(96)*2*np.pi/96))/2*48).astype(int).tolist()
 else:ids=np.rint(np.linspace(12,96,60)).astype(int).tolist()+[96]*18
 cache={};frames=[];normalizer=None
 for i,k in enumerate(ids):
  if k not in cache:
   a=matte(video[k],he)
   if he:a=corrected(a)
   else:
    # Separate the instrument only below the hand; body/sleeve stay fully unaffected.
    t=k/24;yy,xx=np.indices((h,w));hand_y=float(np.interp(t,[.5,1,1.5,2,2.5,3],[380,390,440,535,600,650]));threshold_x=float(np.interp(t,[.5,1,1.5,2,2.5,3],[585,620,690,730,780,820]));mask=(xx>threshold_x)&(yy>hand_y+35)
    fade=float(np.clip((t-1.35)/.9,0,1));layer=Image.fromarray(np.where(mask[:,:,None],a,0).astype('uint8'));body=a.copy();body[mask]=0
    if fade>0:
     layer=layer.filter(ImageFilter.GaussianBlur(fade*5));la=np.array(layer);la[:,:,3]=np.rint(la[:,:,3]*(1-fade)).astype('uint8');base=Image.fromarray(body);base.alpha_composite(Image.fromarray(la));a=np.array(base)
   a[a[:,:,3]==0,:3]=0
   if normalizer is None:
    bbox=Image.fromarray(a[:,:,3]).getbbox();scale=(350 if he else 420)/(bbox[3]-bbox[1]);normalizer=np.array([[scale,0,256-w/2*scale],[0,scale,472-bbox[3]*scale]],np.float32)
   f=a.astype(np.float32)/255;f[:,:,:3]*=f[:,:,3:];f=cv2.warpAffine(f,normalizer,(512,512),flags=cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT);f[:,:,:3]=np.divide(f[:,:,:3],f[:,:,3:],out=np.zeros_like(f[:,:,:3]),where=f[:,:,3:]>0);a=np.rint(np.clip(f,0,1)*255).astype('uint8');a[a[:,:,3]<5]=0;cache[k]=a
  a=cache[k].copy();frames.append(Image.fromarray(a));Image.fromarray(a).save(out/f'{prefix}_透明动画帧_v01_{i:03d}.png')
  if i%24==0:print(prefix,i,'/',len(ids),flush=True)
 gifs=[]
 for im in frames:
  bg=Image.new('RGBA',(512,512),'#222832');bg.alpha_composite(im);gifs.append(bg.convert('RGB'))
 durations=[round((i+1)*100/24)*10-round(i*100/24)*10 for i in range(len(gifs))]
 path=out/f'{prefix}_透明动画预览_v01.gif';gifs[0].save(path,save_all=True,append_images=gifs[1:],duration=durations,loop=0,disposal=2,optimize=False)
 atlas=Image.new('RGBA',(512*8,512*((len(frames)+7)//8)));regions=[]
 for i,im in enumerate(frames):x=i%8*512;y=i//8*512;atlas.paste(im,(x,y));regions.append([x,y,512,512])
 atlas.save(out/f'{prefix}_透明动画图集_v01.png')
 report[prefix]={'source_sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'source_duration_seconds':len(video)/24,'source_indices':ids,'fps':24,'frame_count':len(frames),'duration_seconds':len(frames)/24,'loop':he,'regions':regions,'canvas':[512,512],'anchor':[256,472],'normalizer':normalizer.tolist(),'all_corners_clear':all(all(im.getpixel(xy)[3]==0 for xy in [(0,0),(511,0),(0,511),(511,511)]) for im in frames),'status':'GIF candidate; not game validated'}
 print(prefix,'done',flush=True)
(out/'RV07_HE01_动画制作参数_v01.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
