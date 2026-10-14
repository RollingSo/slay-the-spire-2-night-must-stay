"""Prepare lossless, uniformly cropped frames for the production SpriteFrames."""
import json,pathlib
import numpy as np
from PIL import Image
ROOT=pathlib.Path(__file__).resolve().parents[2]
source=ROOT/'design/动画预览/ironeye/polished_bundle_v02_20261014'
out=ROOT/'.tmp/ironeye_runtime_sequences';out.mkdir(exist_ok=True)
actions={}
removed=[]
for name in ['idle','attack','dagger','cast','hurt']:
    frames=[]
    for p in sorted((source/name/'frames').glob('*.png')):
        frame=np.asarray(Image.open(p).convert('RGBA')).copy()
        index=len(frames)
        if name=='attack' and index>=11:
            removed.append({'frame':index,'pixels':int((frame[:,450:,3]>0).sum())})
            frame[:,450:]=0 # Detached baked arrow; body/bow stay entirely left of x450.
        frames.append(frame)
    actions[name]=frames
bounds=[Image.fromarray(f[:,:,3]).getbbox() for frames in actions.values() for f in frames]
box=(max(0,min(b[0] for b in bounds)-2),max(0,min(b[1] for b in bounds)-2),min(640,max(b[2] for b in bounds)+2),min(480,max(b[3] for b in bounds)+2))
inputs={}
for name,frames in actions.items():
    folder=out/name;folder.mkdir(exist_ok=True);inputs[name]=[]
    for i,frame in enumerate(frames):
        path=folder/f'{name}_{i:04d}.png'
        Image.fromarray(frame).crop(box).save(path)
        inputs[name].append(str(path.resolve()).replace('\\','/'))
report={'inputs':inputs,'output_resource':str(ROOT/'ironeye_assets/combat_rig/ironeye_sequences.res').replace('\\','/'),'uniform_crop':box,'sprite_offset':[box[0]-260,box[1]-400],'canvas':[box[2]-box[0],box[3]-box[1]],'frame_count':sum(map(len,actions.values())),'decoded_rgba_mib':sum(map(len,actions.values()))*(box[2]-box[0])*(box[3]-box[1])*4/1048576,'baked_arrow_removal':removed}
(out/'inputs.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k!='inputs'},ensure_ascii=False))
