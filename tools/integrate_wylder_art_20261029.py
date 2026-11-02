import json,re,shutil
from pathlib import Path
root=Path(__file__).resolve().parents[1]
rows=json.loads((root/'design/wylder/art_completion_20261029.json').read_text(encoding='utf-8'))
candidates=root/'design/卡图预览/追踪者_补齐_20261029'
backup=candidates/'before_integration'; backup.mkdir(exist_ok=True)
for row in rows:
    id=row['id']; cls=''.join(p.title() for p in id.split('_'))
    matches=[]
    for file in (root/'src/Core/Models/Cards').glob('Wylder*.cs'):
        text=file.read_text(encoding='utf-8-sig')
        pattern=r'(public sealed class '+cls+r'\b[\s\S]*?public override string PortraitPath => )"[^"]+";'
        updated,n=re.subn(pattern,lambda m:m[1]+'"res://images/packed/card_portraits/wylder/'+id+'.png";',text,count=1)
        if n:
            if not (backup/(file.name+'.bak')).exists(): shutil.copy2(file,backup/(file.name+'.bak'))
            updated=updated.replace('// Native portraits are interim assets, not approved custom artwork.','// Dedicated artwork reviewed and integrated on 2026-10-29.')
            file.write_text(updated,encoding='utf-8'); matches.append(file.name)
    assert len(matches)==1,(cls,matches)
    dest=root/'images/packed/card_portraits/wylder'/f'{id}.png'
    if dest.exists() and not (backup/dest.name).exists(): shutil.copy2(dest,backup/dest.name)
    shutil.copy2(candidates/f'{id}.png',dest)
    print(cls, '->', dest.name)
