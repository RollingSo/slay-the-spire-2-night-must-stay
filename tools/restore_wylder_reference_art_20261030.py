import json,shutil,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[1]
out=root/'design/卡图预览/追踪者_原版复审_20261030'
ids=['claw_movement','stamp_sweep','vacuum_slice','storm_blade','flaming_strike','flame_of_redmanes','takers_flames','eochaids_dancing_blade']
excluded={'wylder_'+n for n in ids}
manifest=json.loads((out/'integration_manifest.json').read_text(encoding='utf-8'))
for r in manifest:
    if r['id'] not in excluded:continue
    name=r['id']+'.png'
    src=out/'before_integration'/name
    dst=root/r['formal_path']
    assert src.exists()
    shutil.copy2(src,dst)
    r['status']='withdrawn_reference_pose_priority'
    r['reason']='用户提供战技图标优先；撤回擅自改变动作景别的近景替换，恢复本轮之前正式资源。不是新一轮用户审批。'
    r['restored_sha256']=hashlib.sha256(dst.read_bytes()).hexdigest()
(out/'integration_manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
data=root/'design/wylder/art_reaudit_20261030_final.json'
rows=json.loads(data.read_text(encoding='utf-8'))
for r in rows:
    if r['id'] in excluded:r['status']='withdrawn_reference_pose_priority'
data.write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
print('Restored 8 reference-directed artworks; 8 non-reference close-ups retained.')
