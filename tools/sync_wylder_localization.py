"""Merge authored Wylder localization without rewriting other roles' entries."""
from pathlib import Path
import json, re
ROOT=Path(__file__).resolve().parents[1]
source=json.loads((ROOT/'design/wylder/localization.json').read_text(encoding='utf-8'))
for fragment_name in ['completion_a_localization.json', 'completion_b_localization.json', 'power_expansion_localization.json', 'sheet_20261029_localization.json']:
    fragment_path = ROOT/'design/wylder'/fragment_name
    if fragment_path.exists():
        for table, entries in json.loads(fragment_path.read_text(encoding='utf-8')).items():
            for key, value in entries.items():
                if key in source.setdefault(table, {}):
                    raise ValueError(f'Duplicate Wylder localization source: {table}.{key}')
                source[table][key] = value
name_overrides=json.loads((ROOT/'design/card_name_localization.json').read_text(encoding='utf-8'))['cards']
authored_locale_path = ROOT/'design/wylder/localization_jpn_kor.json'
authored_locales = json.loads(authored_locale_path.read_text(encoding='utf-8')) if authored_locale_path.exists() else {}

def highlight_card_terms(text, chinese):
    # Preserve authored gold spans and runtime placeholders. Conditional clauses
    # and glossary text need the same highlighting as the primary effect line.
    terms = ['合成', '固守', '防御反击', '盾戳', '失衡', '藏锋', '虚弱', '格挡',
             '手牌', '弃牌堆', '抽牌堆', '保留', '消耗', '力量', '敏捷', '眩晕'] if chinese else [
             'Synthesize', 'Synthesis', 'Concealed Edge', 'Fortify', 'Guard Counter',
             'Shield Poke', 'Imbalance', 'Weak', 'Block', 'Blocking', 'hand',
             'discard pile', 'draw pile', 'Retain', 'Retained', 'Exhaust',
             'Exhausted', 'Strength', 'Dexterity', 'Stun']
    pattern = '|'.join(re.escape(term) for term in terms)
    if not chinese:
        pattern = r'(?<![A-Za-z])(?:'+pattern+r')(?![A-Za-z])'
    parts = re.split(r'(\[gold\].*?\[/gold\]|\{[^{}]*\})', text, flags=re.S)
    return ''.join(part if part.startswith(('[gold]', '{')) else
        re.sub(pattern, lambda match: '[gold]'+match[0]+'[/gold]', part,
               flags=0 if chinese else re.I) for part in parts)
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
            authored = authored_locales.get(locale, {}).get(table, {})
            title_key = key+'.title'
            description_key = key+'.description'
            data[title_key]=authored.get(title_key, name_overrides.get(key, {}).get(locale, values[offset]))
            description=authored.get(description_key, highlight_card_terms(values[offset+1], locale=='zhs') if table=='cards' else values[offset+1])
            data[key+'.description']=description
            if table=='relics':
                # Native inspection and all shipping locale gates need flavor.
                flavor_key = key+'.flavor'
                data[flavor_key]=authored.get(flavor_key, values[4 if locale=='zhs' else 5])
            if table=='cards' and key not in ['WYLDER_PURSUIT','WYLDER_BREAK','WYLDER_HAND_CONDITION']:
                data[key+'.upgradeDescription']=description
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
    authored_character = authored_locales.get(locale, {}).get('characters', {})
    entries = {key: authored_character.get('WYLDER.'+key, value) for key, value in entries.items()}
    for key,value in entries.items(): data['WYLDER.'+key]=value
    merge(p,data)
    p=ROOT/f'NightMustStay/localization/{locale}/card_library.json';data={}
    data['POOL_WYLDER_TIP']=authored_locales.get(locale, {}).get('card_library', {}).get('POOL_WYLDER_TIP', name)
    merge(p,data)
print('Wylder localization synchronized; Japanese/Korean use explicit English fallbacks.')
import subprocess, sys
subprocess.run([sys.executable, str(ROOT/'tools/sync_wylder_ancients.py')], check=True)
