"""Validate portable candidate atlas resources without local caches or game files."""
from pathlib import Path
import hashlib,json,re,sys
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'revenant_assets/combat/sequence_candidates'
HERE=Path(__file__).resolve().parent
meta=json.loads((OUT/'动画提取记录.json').read_text())
report={}
for name,action in meta['actions'].items():
 assert hashlib.sha256((HERE/f'{name}_源视频.mp4').read_bytes()).hexdigest()==meta['source'][name]['sha256']
 text=(OUT/f'{name}_动画.tres').read_text()
 references=re.findall(r'path="res://([^"\n]+)"',text)
 assert len(references)==len(action['atlas_pages'])
 for ref in references:assert (ROOT/ref).is_file(),ref
 pages=[]
 for page in action['atlas_pages']:
  im=Image.open(OUT/page);assert im.mode=='RGBA' and im.size==(1664,2240)
  pages.append(im)
 regions=re.findall(r'region = Rect2\((\d+), (\d+), (\d+), (\d+)\)',text)
 ids=re.findall(r'atlas = ExtResource\("(\d+)"\)',text)
 assert len(regions)==len(ids)==action['frame_count']==len(action['atlas_indices'])
 for i,(rect,pid) in enumerate(zip(regions,ids)):
  x,y,w,h=map(int,rect);assert (w,h)==(416,560)
  assert x+w<=1664 and y+h<=2240
  k=action['atlas_indices'][i]
  assert int(pid)-1==k//16 and (x,y)==(k%4*416,k%16//4*560)
  a=np.asarray(pages[int(pid)-1].crop((x,y,x+w,y+h)))[:,:,3]
  assert a[0,0]==a[0,-1]==a[-1,0]==a[-1,-1]==0
  assert a.max()==255 and (a>0).sum()>1000
 report[name]={'frame_count':len(regions),'duration_seconds':action['resource_duration'],'atlas_pages':len(pages),'source_sha256_matches':True,'resource_regions_valid':True,'alpha_corners_zero':True}
# Regression: manually chosen solid arm/hand/sleeve interiors must be opaque,
# while nearby finger/lyre openings and exterior background remain transparent.
probes=json.loads((HERE/'opacity_probes.json').read_text())
for name,groups in probes.items():
 if name=='description':continue
 action=meta['actions'][name]
 pages=[Image.open(OUT/p) for p in action['atlas_pages']]
 count=0
 for group in groups:
  i=group['frame'];k=action['atlas_indices'][i]
  frame=np.asarray(pages[k//16].crop((k%4*416,k%16//4*560,k%4*416+416,k%16//4*560+560)))
  for x,y in group['opaque']:
   assert frame[y//2,x//2,3]>=250,(name,i,x,y,'arm interior translucent',int(frame[y//2,x//2,3]))
   count+=1
  for x,y in group['background']:
   assert frame[y//2,x//2,3]<=40,(name,i,x,y,'background filled',int(frame[y//2,x//2,3]))
 report[name]['manual_opacity_probes_passed']=count
assert hashlib.sha256((HERE/'reference.png').read_bytes()).hexdigest()==hashlib.sha256((ROOT/'revenant_assets/combat/revenant_idle.png').read_bytes()).hexdigest()
if '--write-report' in sys.argv:(OUT/'动画提取检查.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(report,ensure_ascii=False,indent=2))
