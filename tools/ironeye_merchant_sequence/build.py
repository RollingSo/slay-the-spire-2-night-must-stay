"""Rebuild portable SpriteFrames atlases from the approved 60 FPS merchant loop.
Requires Python 3, Pillow and FFmpeg; no Godot, game DLLs or local source paths.
"""
import hashlib,json,pathlib,subprocess,tempfile
from PIL import Image
ROOT=pathlib.Path(__file__).resolve().parents[2]
HERE=pathlib.Path(__file__).resolve().parent
OUT=ROOT/'ironeye_assets/merchant/idle_sequence'
FPS=60

def run(args):
    subprocess.run(args,check=True)
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='nms-merchant-alpha-') as temp:
        temp=pathlib.Path(temp)
        # Protect pale feather/arrow details using fixed scene-space regions.
        protect='between(X,235,310)*between(Y,40,145)+between(X,335,540)*between(Y,185,245)'
        distance='sqrt(pow(r(X,Y)-247,2)+pow(g(X,Y)-246,2)+pow(b(X,Y)-248,2))/441.67295593'
        alpha=f'if(gte(Y,800),0,if(gt({protect},0),alpha(X,Y),255*clip(({distance}-0.12)/0.08,0,1)))'
        vf="format=rgba,colorkey=0xf7f6f8:0.065:0.025,geq=r='r(X,Y)':g='g(X,Y)':b='b(X,Y)':a='"+alpha+"'"
        channels=[]
        for c,bg in [('r',247),('g',246),('b',248)]:
            channels.append(f"{c}='if(gt(alpha(X,Y),0),clip(({c}(X,Y)-{bg}*(1-alpha(X,Y)/255))*255/alpha(X,Y),0,255),0)'")
        vf+=',geq='+':'.join(channels)+":a='alpha(X,Y)'"
        run(['ffmpeg','-v','error','-y','-i',str(HERE/'approved_loop.mp4'),'-vf',vf,str(temp/'%04d.png')])
        frames=sorted(temp.glob('*.png'))
        if len(frames)!=120:raise RuntimeError(f'Expected 120 frames, got {len(frames)}')
        bounds=[]
        for f in frames:
            with Image.open(f) as im:bounds.append(im.getchannel('A').getbbox())
        left=max(0,min(b[0] for b in bounds)-4);top=max(0,min(b[1] for b in bounds)-4)
        right=min(624,max(b[2] for b in bounds)+4);bottom=min(800,max(b[3] for b in bounds)+4)
        # Shared crop only; add transparent margin, never normalize individual frames.
        width=right-left+16;height=bottom-top+16
        vf=f'crop={right-left}:{bottom-top}:{left}:{top},pad={width}:{height}:8:8:color=black@0,tile=4x4:nb_frames=16:padding=0:margin=0:color=black@0'
        run(['ffmpeg','-v','error','-y','-framerate',str(FPS),'-i',str(temp/'%04d.png'),'-vf',vf,'-fps_mode','vfr',str(OUT/'atlas_%02d.png')])
        pages=sorted(OUT.glob('atlas_*.png'))
        text=['[gd_resource type="SpriteFrames" load_steps=129 format=3]','']
        for i,page in enumerate(pages):text.append(f'[ext_resource type="Texture2D" path="res://ironeye_assets/merchant/idle_sequence/{page.name}" id="{i+1}"]')
        for i in range(120):
            page=i//16;cell=i%16
            text.extend(['',f'[sub_resource type="AtlasTexture" id="Frame_{i:03d}"]',f'atlas = ExtResource("{page+1}")',f'region = Rect2({cell%4*width}, {cell//4*height}, {width}, {height})','filter_clip = true'])
        text+=['','[resource]','resource_name = "Ironeye merchant approved idle"',f'metadata/canvas_origin = Vector2({left-8}, {top-8})','animations = [{','"frames": [']
        text += [f'{{"duration": 1.0, "texture": SubResource("Frame_{i:03d}")}}'+(',' if i<119 else '') for i in range(120)]
        text+= ['],','"loop": true,','"name": &"relaxed_loop",','"speed": 60.0','}]','']
        (OUT/'ironeye_merchant_idle.tres').write_text('\n'.join(text),encoding='utf-8')
        metadata={'action':'merchant relaxed_loop','fps':FPS,'frame_count':120,'duration_seconds':2,'source_canvas':[624,840],'shared_crop':[left,top,right,bottom],'transparent_padding':8,'frame_size':[width,height],'canvas_origin':[left-8,top-8],'alpha_bounds_source':bounds,'decoded_atlas_rgba_mib':len(pages)*4*width*4*height*4/1048576,'approved_loop_sha256':sha(HERE/'approved_loop.mp4'),'reference_image_sha256':sha(ROOT/'ironeye_assets/merchant/ironeye_merchant.png'),'alpha_method':{'background_rgb':[247,246,248],'base_similarity':.065,'base_blend':.025,'body_similarity':.12,'body_blend':.08,'protected_regions_xyxy':[[235,40,310,145],[335,185,540,245]],'clear_rows_from':800},'status':'material handoff; Windows import, packaging and game validation pending','known_limits':['Source bow is clipped at left edge; padding does not reconstruct missing pixels.','Fixed protection regions may retain pale fringe around feather and arrow; review against actual shop background.','No shop scene replacement or DLL changes performed.'],'sha256':{p.name:sha(p) for p in pages}}
        (OUT/'manifest.json').write_text(json.dumps(metadata,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        print(json.dumps({k:metadata[k] for k in ['frame_count','frame_size','shared_crop','decoded_atlas_rgba_mib']},ensure_ascii=False))
if __name__=='__main__':main()
