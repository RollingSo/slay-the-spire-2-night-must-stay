"""Merge authored Wylder localization without rewriting other roles' entries."""
from pathlib import Path
import json, re
ROOT=Path(__file__).resolve().parents[1]
source=json.loads((ROOT/'design/wylder/localization.json').read_text(encoding='utf-8'))
def merge(path,entries):
    # Preserve existing order, formatting and even legacy duplicate keys outside
    # this character. A JSON round-trip silently drops such legacy entries.
    text=path.read_text(encoding='utf-8-sig')
    for key,value in entries.items():
        pattern=r'(?m)^(\s*"'+re.escape(key)+r'"\s*:\s*)("(?:\\.|[^"\\])*")'
        encoded=json.dumps(value,ensure_ascii=False)
        if re.search(pattern,text):text=re.sub(pattern,lambda m:m[1]+encoded,text)
        else:
            text=text.rstrip()[:-1].rstrip()+',\n  '+json.dumps(key)+': '+encoded+'\n}\n'
    path.write_text(text,encoding='utf-8')
for locale in ['zhs','eng','jpn','kor']:
    offset=0 if locale=='zhs' else 2
    for table,entries in source.items():
        p=ROOT/f'NightMustStay/localization/{locale}/{table}.json'
        data={}
        for key,values in entries.items():
            data[key+'.title']=values[offset]
            data[key+'.description']=values[offset+1]
            if table=='cards' and key not in ['WYLDER_PURSUIT','WYLDER_BREAK']:
                data[key+'.upgradeDescription']=values[offset+1]
        merge(p,data)
        if table=='cards' and locale in ['jpn','kor']:
            merge(p,{'WYLDER_DEFEND.title':'防御' if locale=='jpn' else '수비'})
    p=ROOT/f'NightMustStay/localization/{locale}/characters.json'
    data={}
    name='追踪者' if locale=='zhs' else 'Wylder'
    entries={'title':name,'titleObject':name,'pronounSubject':'他' if locale=='zhs' else 'he',
        'pronounObject':'他' if locale=='zhs' else 'him','pronounPossessive':'他的' if locale=='zhs' else 'his',
        'possessiveAdjective':'他的' if locale=='zhs' else 'his','cardsModifierTitle':name,
        'cardsModifierDescription':name+' cards' if locale!='zhs' else '追踪者的卡牌。',
        'description':'以钩爪进入追击，用攻击打破敌人的架势。' if locale=='zhs' else 'Enter Pursuit with a claw and break the enemy stance with attacks.'}
    for key,value in entries.items(): data['WYLDER.'+key]=value
    merge(p,data)
    p=ROOT/f'NightMustStay/localization/{locale}/card_library.json';data={}
    data['POOL_WYLDER_TIP']=name
    merge(p,data)
print('Wylder localization synchronized; Japanese/Korean use explicit English fallbacks.')
