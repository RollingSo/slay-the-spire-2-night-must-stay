import sys,pathlib,subprocess,json,hashlib,base64
import numpy as np
from PIL import Image
root=pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0,str(root/'.tmp/video_review_deps'));sys.path.insert(0,str(pathlib.Path(__file__).parent))
import cv2,imageio_ffmpeg
from matte_video import matte
cv2.setNumThreads(2)
out=root/'design/动画预览/revenant';ff=imageio_ffmpeg.get_ffmpeg_exe()
def decode(path,seconds):
 raw=subprocess.run([ff,'-v','error','-i',str(path),'-vf','fps=24','-t',str(seconds),'-f','rawvideo','-pix_fmt','rgb24','pipe:1'],stdout=subprocess.PIPE,check=True).stdout
 return np.frombuffer(raw,np.uint8).reshape(-1,1120,832,3)
def bounds(f):return Image.fromarray(f[:,:,3]).getbbox()
def mix(a,b,t):
 a=a.astype(float)/255;b=b.astype(float)/255
 alpha=(1-t)*a[:,:,3:]+t*b[:,:,3:]
 rgb=np.divide((1-t)*a[:,:,:3]*a[:,:,3:]+t*b[:,:,:3]*b[:,:,3:],alpha,out=np.zeros_like(a[:,:,:3]),where=alpha>0)
 return np.rint(np.concatenate([rgb,alpha],axis=2)*255).astype('uint8')
actions={};inputs={};details={};normalizer=None;neutral=None
for name,prefix,filename,ids in [
 ('lyre_command','RV03_拨弦','RV03_拨弦_源视频.mp4',np.rint(np.linspace(0,55,32)).astype(int).tolist()),
 ('prayer_attack','RV04_祷告','RV04_祷告_源视频.mp4',json.loads((out/'RV04_祷告_倒放收招参数_v02.json').read_text(encoding='utf8'))['source_indices'])]:
 src=out/filename
 raw=decode(src,2.4)
 frames=[];cache={}
 for k,i in enumerate(ids):
  if i not in cache:
   cv2.setRNGSeed(137)
   rgba=matte(raw[i])
   if normalizer is None:
    box=bounds(rgba);scale=420/(box[3]-box[1]);normalizer=np.array([[scale,0,256-416*scale],[0,scale,472-box[3]*scale]],np.float32)
   f=rgba.astype(np.float32)/255;f[:,:,:3]*=f[:,:,3:]
   f=cv2.warpAffine(f,normalizer,(512,512),flags=cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT,borderValue=0)
   f[:,:,:3]=np.divide(f[:,:,:3],f[:,:,3:],out=np.zeros_like(f[:,:,:3]),where=f[:,:,3:]>0)
   f=np.rint(np.clip(f,0,1)*255).astype('uint8');f[f[:,:,3]<8]=0
   cache[i]=f
  frames.append(cache[i].copy())
  if k%8==0:print(name,k,'/',len(ids),flush=True)
 if neutral is None:neutral=frames[0].copy()
 # A short, alpha-correct endpoint transition matches the common approved starting image.
 if name=='lyre_command':
  frames[-3]=mix(frames[-3],neutral,.20)
  frames[-2]=mix(frames[-2],neutral,.55)
  frames[-1]=neutral.copy()
 else:
  # Both source videos share the uploaded initial pose. Use one common boundary frame.
  frames[0]=neutral.copy();frames[-1]=neutral.copy()
 inputs[name]=[]
 for k,f in enumerate(frames):
  path=out/f'{prefix}_透明帧_v01_{k:03d}.png';Image.fromarray(f).save(path);inputs[name].append(str(path.resolve()).replace('\\','/'))
 for theme,color in [('深底',(23,27,36)),('浅底',(238,238,238))]:
  ims=[]
  for f in frames:
   im=Image.new('RGBA',(512,512),color+(255,));im.alpha_composite(Image.fromarray(f));im=im.convert('RGB')
   ims.append(im)
  ims[0].save(out/f'{prefix}_透明动画_{theme}_v01.gif',save_all=True,append_images=ims[1:],duration=[40 if k%6!=5 else 50 for k in range(len(ims))],loop=0)
 boxes=[bounds(f) for f in frames]
 details[name]={'frames':len(frames),'fps':24,'duration_seconds':len(frames)/24,'source_indices':ids,'source_sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'canvas':[512,512],'alpha_corners_zero':all(not any(int(f[y,x,3]) for x,y in [(0,0),(511,0),(0,511),(511,511)]) for f in frames),'endpoints_equal':bool(np.array_equal(frames[0],frames[-1])),'foot_y_range':[min(b[3] for b in boxes),max(b[3] for b in boxes)],'alpha_area':[round(float(f[:,:,3].sum()/255),2) for f in frames]}
 actions[name]=[base64.b64encode(pathlib.Path(p).read_bytes()).decode() for p in inputs[name]]
 print(name,details[name]['foot_y_range'],'done',flush=True)
 Image.fromarray(frames[len(frames)//2]).save(out/f'{prefix}_透明动画关键帧_v01.png')
 report={'stage':'transparent_sequence_preview','fps':24,'sprite_offset':[-256,-472],'normalizer':normalizer.tolist(),'actions':details,'decoded_rgba_mib':sum(d['frames'] for d in details.values())*512*512*4/1048576,'game_timing_adapted':False,'mod_integrated':False}
 (out/'RV03_RV04_透明动画检查_v01.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
 data={'inputs':inputs,'output_resource':str((out/'复仇者_拨弦与祷告_SpriteFrames_v01.res').resolve()).replace('\\','/'),'sprite_offset':[-256,-472]}
 (root/'.tmp/revenant_sequences_inputs.json').write_text(json.dumps(data,ensure_ascii=False),encoding='utf8')
 html='''<!doctype html><meta charset="utf-8"><title>复仇者：拨弦与祷告透明动画</title><style>body{background:#20232b;color:#eee;font:16px sans-serif;margin:24px}canvas{display:block;border:1px solid #666;margin-top:16px;background:#171b24}button,select,input{margin:8px;padding:8px}small{display:block}</style><h2>复仇者：拨弦与祷告</h2><select id="action"><option value="lyre_command">拨弦／指挥</option><option value="prayer_attack">攻击祷告（缓动收招）</option></select><button id="restart">重播</button><button id="pause">暂停／继续</button><button id="prev">上一帧</button><button id="next">下一帧</button><label>速度<select id="speed"><option value="1">1×</option><option value="0.5">0.5×</option><option value="2">2×</option></select></label><label>背景<input type="color" id="bg" value="#171b24"></label><canvas id="view" width="512" height="512"></canvas><p id="status"></p><small>透明序列审阅；动作完整播放时长。正式游戏触发时点另行适配。</small><script>const raw=__DATA__;const frames={};for(const [key,list] of Object.entries(raw))frames[key]=list.map(b=>{let i=new Image;i.src='data:image/png;base64,'+b;return i});let k='lyre_command',clock=0,play=true,last=performance.now();const ctx=view.getContext('2d');const statusEl=document.getElementById('status');action.onchange=()=>{k=action.value;clock=0};restart.onclick=()=>{clock=0;play=true};pause.onclick=()=>play=!play;prev.onclick=()=>{play=false;clock=Math.max(0,clock-1/24)};next.onclick=()=>{play=false;clock=Math.min((frames[k].length-1)/24,clock+1/24)};bg.oninput=()=>view.style.background=bg.value;function tick(t){if(play)clock+=(t-last)/1000*Number(speed.value);last=t;let n=Math.min(frames[k].length-1,Math.floor(clock*24+0.00001));if(clock>=frames[k].length/24){clock=0;n=0}ctx.clearRect(0,0,512,512);let im=frames[k][n];if(im.complete)ctx.drawImage(im,0,0);statusEl.textContent=`${n+1}/${frames[k].length}帧 · ${(frames[k].length/24).toFixed(3)}秒`;requestAnimationFrame(tick)}requestAnimationFrame(tick)</script>'''
 (out/'复仇者_拨弦与祷告_透明动画预览_v01.html').write_text(html.replace('__DATA__',json.dumps(actions)),encoding='utf8')
 print('All outputs ready')
