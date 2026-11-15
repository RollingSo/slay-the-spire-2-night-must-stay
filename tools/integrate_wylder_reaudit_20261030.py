import json,shutil,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[1]
out=root/'design/卡图预览/追踪者_原版复审_20261030'
backup=out/'before_integration'
backup.mkdir(exist_ok=True)
rows=json.loads((root/'design/wylder/art_reaudit_20261030_final.json').read_text(encoding='utf-8'))
for r in rows:
    if r.get('status')=='withdrawn_reference_pose_priority':continue
    src=out/(r['id']+'.png')
    dst=root/'images/packed/card_portraits/wylder'/src.name
    assert dst.exists(),dst
    old=backup/dst.name
    if not old.exists():shutil.copy2(dst,old)
    shutil.copy2(src,dst)
    r['status']='self_reviewed_approved_integrated'
    r['sha256']=hashlib.sha256(dst.read_bytes()).hexdigest()
    r['formal_path']=dst.relative_to(root).as_posix()
(out/'integration_manifest.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'Integrated {len(rows)}; backups retained: {backup}')
