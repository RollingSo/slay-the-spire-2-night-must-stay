from pathlib import Path
import sys,json,hashlib
import numpy as np
from PIL import Image,ImageDraw,ImageFont
r=Path.cwd();sys.path.insert(0,str(r/'.tmp/video_review_deps'));import cv2
out=r/'design/动画预览/revenant';ref=np.array(Image.open(r/'revenant_assets/families/animations/helen/idle.png').convert('RGBA'))
# Learn one smooth tonal palette from opaque subject interiors, excluding backdrop and antialiased edges.
refmask=cv2.erode((ref[:,:,3]>230).astype('uint8'),np.ones((3,3),np.uint8))>0
refLab=cv2.cvtColor(ref[:,:,:3].astype(np.float32)/255,cv2.COLOR_RGB2LAB)
samples=[np.array(Image.open(out/f'HE01_待机_透明小样_v01_{k}秒.png').convert('RGBA')) for k in [1,2,4]]
source=[]
for a in samples:
 mask=cv2.erode((a[:,:,3]>245).astype('uint8'),np.ones((3,3),np.uint8))>0
 lab=cv2.cvtColor(a[:,:,:3].astype(np.float32)/255,cv2.COLOR_RGB2LAB);source.append(lab[mask])
src=np.concatenate(source);target=refLab[refmask]
# Chromatic correction follows luminance, preserving light/shadow structure and dark outlines.
knots=np.array([0,15,30,45,60,75,90,100.]);delta=[]
for value in knots:
 sm=src[np.abs(src[:,0]-value)<12];tm=target[np.abs(target[:,0]-value)<12]
 delta.append((np.median(tm[:,1:],axis=0)-np.median(sm[:,1:],axis=0)).tolist() if len(sm)>20 and len(tm)>20 else [0,0])
delta=np.array(delta);delta=np.clip(delta,[-8,-22],[8,8]);delta[0]=delta[1]*.4
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',19)
sheet=Image.new('RGB',(1050,1030),'#222832');draw=ImageDraw.Draw(sheet)
for col,label in enumerate(['正式原图','视频提取原色','校色后']):draw.text((col*350+25,12),label,font=font,fill='white')
rows=[]
for j,(k,a) in enumerate(zip([1,2,4],samples)):
 lab=cv2.cvtColor(a[:,:,:3].astype(np.float32)/255,cv2.COLOR_RGB2LAB)
 for c in [1,2]:lab[:,:,c]+=np.interp(lab[:,:,0],knots,delta[:,c-1])
 rgb=cv2.cvtColor(lab,cv2.COLOR_LAB2RGB);fixed=a.copy();fixed[:,:,:3]=np.rint(np.clip(rgb,0,1)*255).astype('uint8');fixed[fixed[:,:,3]==0,:3]=0
 assert np.array_equal(fixed[:,:,3],a[:,:,3]);Image.fromarray(fixed).save(out/f'HE01_待机_校色透明小样_v01_{k}秒.png')
 for col,arr in enumerate([ref,a,fixed]):
  im=Image.fromarray(arr);bbox=im.getchannel('A').getbbox();im=im.crop(bbox);im.thumbnail((315,290));bg=Image.new('RGBA',(350,315),'#222832' if j!=1 else '#dedede');bg.alpha_composite(im,((350-im.width)//2,310-im.height));sheet.paste(bg.convert('RGB'),(col*350,55+j*320))
 draw.text((355,58+j*320),str(k)+'s',font=font,fill='white' if j!=1 else 'black')
 rows.append({'second':k,'alpha_unchanged':True})
sheet.save(out/'HE01_待机_正式图-原色-校色对比_v01.jpg',quality=96)
params={'reference':'revenant_assets/families/animations/helen/idle.png','reference_sha256':hashlib.sha256((r/'revenant_assets/families/animations/helen/idle.png').read_bytes()).hexdigest(),'method':'fixed luminance-conditioned CIELAB chroma offsets; luminance and alpha unchanged','L_knots':knots.tolist(),'ab_offsets':delta.tolist(),'samples':rows,'status':'color review candidate; not production integration'}
(out/'HE01_待机_校色参数_v01.json').write_text(json.dumps(params,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(params,ensure_ascii=False))
