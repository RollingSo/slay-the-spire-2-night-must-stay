import pathlib,json,math
from PIL import Image
root=pathlib.Path(__file__).resolve().parents[2]
data=json.loads((root/'.tmp/revenant_sequences_inputs.json').read_text(encoding='utf8'))
out=root/'design/动画预览/revenant'
all_images=[Image.open(p).convert('RGBA') for paths in data['inputs'].values() for p in paths]
boxes=[im.getchannel('A').getbbox() for im in all_images]
crop=[max(0,min(b[0] for b in boxes)-2),max(0,min(b[1] for b in boxes)-2),min(512,max(b[2] for b in boxes)+2),min(512,max(b[3] for b in boxes)+2)]
w,h=crop[2]-crop[0],crop[3]-crop[1]
cols=10;rows=math.ceil(len(all_images)/cols)
atlas=Image.new('RGBA',(cols*w,rows*h));actions={};k=0
for name,paths in data['inputs'].items():
 actions[name]=[]
 for p in paths:
  rect=[k%cols*w,k//cols*h,w,h];atlas.paste(all_images[k].crop(crop),rect[:2]);actions[name].append(rect);k+=1
atlas_path=out/'复仇者_拨弦与祷告_透明图集_v01.png';atlas.save(atlas_path)
result={'atlas_path':str(atlas_path.resolve()).replace('\\','/'),'output_resource':data['output_resource'],'canvas_origin':[crop[0]-256,crop[1]-472],'original_canvas':[512,512],'uniform_crop':crop,'frame_size':[w,h],'atlas_size':list(atlas.size),'actions':actions,'fps':24,'decoded_rgba_mib':atlas.width*atlas.height*4/1048576}
(out/'复仇者_拨弦与祷告_图集索引_v01.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
(root/'.tmp/revenant_atlas_inputs.json').write_text(json.dumps(result,ensure_ascii=False),encoding='utf8')
print({k:v for k,v in result.items() if k not in ['actions','atlas_path','output_resource']})
