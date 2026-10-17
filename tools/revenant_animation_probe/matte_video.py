import sys,pathlib,subprocess
import numpy as np
from PIL import Image,ImageDraw
root=pathlib.Path(__file__).resolve().parents[2];sys.path.insert(0,str(root/'.tmp/video_review_deps'))
import cv2,imageio_ffmpeg
ff=imageio_ffmpeg.get_ffmpeg_exe();out=root/'design/动画预览/revenant'

def matte(rgb):
 h,w=rgb.shape[:2]
 # Estimate the white backdrop independently per row using clear outer margins.
 bg=np.median(np.concatenate([rgb[:,:35],rgb[:,-20:]],axis=1),axis=1).astype(np.float32)
 diff=np.sqrt(np.mean((rgb.astype(np.float32)-bg[:,None])**2,axis=2))
 mask=np.where(diff>18,cv2.GC_PR_FGD,cv2.GC_PR_BGD).astype('uint8')
 mask[(diff>45)]=cv2.GC_FGD
 mask[:12]=cv2.GC_BGD;mask[:,:25]=cv2.GC_BGD;mask[:,-12:]=cv2.GC_BGD
 mask[-16:]=cv2.GC_BGD
 mask[1060:,690:]=cv2.GC_BGD
 # Foreground inference keeps spatially connected pale cloth instead of white-keying it.
 cv2.grabCut(cv2.cvtColor(rgb,cv2.COLOR_RGB2BGR),mask,None,np.zeros((1,65),np.float64),np.zeros((1,65),np.float64),3,cv2.GC_INIT_WITH_MASK)
 binary=np.isin(mask,[cv2.GC_FGD,cv2.GC_PR_FGD]).astype('uint8')
 n,labels,stats,_=cv2.connectedComponentsWithStats(binary,8)
 keep=np.zeros(n,bool);keep[1:]=stats[1:,cv2.CC_STAT_AREA]>=9
 binary=keep[labels].astype('uint8')
 interior=cv2.erode(binary,np.ones((3,3),np.uint8))
 exterior=cv2.dilate(binary,np.ones((3,3),np.uint8))
 edge=(exterior>0)&(interior==0)
 # Use local opaque foreground colors to unmix edge pixels from the white backdrop.
 seed=(interior>0)&(diff>22)
 nearest=cv2.inpaint(rgb,(~seed).astype('uint8')*255,3,cv2.INPAINT_TELEA) if False else None
 best=np.zeros_like(rgb,dtype=np.float32);best_dist=np.full((h,w),np.inf)
 for dy in range(-2,3):
  for dx in range(-2,3):
   valid=np.roll(seed,(dy,dx),(0,1));cost=dx*dx+dy*dy
   take=valid&(cost<best_dist);best[take]=np.roll(rgb,(dy,dx),(0,1))[take];best_dist[take]=cost
 b=bg[:,None];vec=best-b
 a=np.clip(np.sum((rgb-b)*vec,axis=2)/np.maximum(np.sum(vec*vec,axis=2),1),0,1)
 alpha=binary.astype(np.float32)
 recover=edge&np.isfinite(best_dist)&(diff>3)
 alpha[recover]=a[recover]
 alpha[diff<3]=0
 # Preserve pale enclosed cloth; edge recovery is restricted to the silhouette boundary.
 alpha[interior>0]=1
 cleaned=rgb.astype(np.float32)
 valid=(alpha>.04)&(alpha<.995)
 cleaned[valid]=np.clip((rgb[valid]-(1-alpha[valid,None])*np.broadcast_to(b,rgb.shape)[valid])/alpha[valid,None],0,255)
 alpha[alpha<.04]=0;cleaned[alpha==0]=0
 return np.dstack([np.rint(cleaned).astype('uint8'),np.rint(alpha*255).astype('uint8')])

if __name__=='__main__':
 for label,file,times in [('RV03_拨弦','拨弦.mp4',[0,.65,1.85]),('RV04_祷告','祷告.mp4',[0,1.8,2.25])]:
  sheet=Image.new('RGB',(960,1000),'#171b24');d=ImageDraw.Draw(sheet)
  for k,t in enumerate(times):
   raw=subprocess.run([ff,'-v','error','-ss',str(t),'-i',str(root/'design/动画预览/ironeye/复仇者'/file),'-frames:v','1','-f','rawvideo','-pix_fmt','rgb24','pipe:1'],stdout=subprocess.PIPE,check=True).stdout
   rgb=np.frombuffer(raw,np.uint8).reshape(1120,832,3);rgba=matte(rgb)
   im=Image.fromarray(rgba);im.save(out/f'{label}_透明小样_{k}.png')
   im.thumbnail((310,440))
   for row,color in enumerate(['#171b24','#dedede']):
    tile=Image.new('RGBA',im.size,color);tile.alpha_composite(im);sheet.paste(tile.convert('RGB'),(k*320+(320-im.width)//2,row*500+35))
    d.text((k*320+8,row*500+9),f'{t:.2f}s',fill='white' if row==0 else 'black')
  sheet.save(out/f'{label}_透明边缘审阅_v01.jpg',quality=95)
  print(label,'samples ready',flush=True)
