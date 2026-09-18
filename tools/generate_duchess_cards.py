"""Deterministic code/text export from the current local Duchess snapshot.

Translations below are authored text templates, not an external translation service.
Check the authoritative Feishu table before editing design/duchess/cards.json.
Run after editing that file; --check never modifies files.
"""
import argparse
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ROWS = json.loads((ROOT / 'design/duchess/cards.json').read_text(encoding='utf-8'))

def model_id(name):
    return re.sub(r'(?<!^)(?=[A-Z])', '_', 'Duchess' + name).upper()

def effects_text(row, language, upgraded=False):
    opt = row[8] if len(row) > 8 else {}
    out = []
    if opt.get('retain'):
        out.append(['[gold]保留[/gold]。', '[gold]Retain[/gold].', '[gold]保留[/gold]。'][language])
    if opt.get('exhaust'):
        out.append(['[gold]消耗[/gold]。', '[gold]Exhaust[/gold].', '[gold]廃棄[/gold]。'][language])
    if opt.get('reaction'):
        out.append(['[gold]反应[/gold]。', '[gold]Reaction[/gold].', '[gold]リアクション[/gold]。'][language])
    for effect in row[7]:
        kind = effect[0]
        var = '{' + ('Block' if kind == 'AllyBlock' else kind) + ':diff()}'
        dodge = ['闪避', 'Dodge', '回避'][language] + ('+' if upgraded and opt.get('upgradeTokens') else '')
        if kind == 'Damage':
            hits = opt.get('hits', 1)
            target = ['对所有敌人', 'to ALL enemies', '敵全体に'][language] if opt.get('all') else ['', '', '敵1体に'][language]
            damage_var = '{CalculatedDamage:diff()}' if opt.get('restageDivisor') else var
            text = [f'{target}造成{var}点伤害' + (f'，共{hits}次' if hits > 1 else '') + '。',
                    f'Deal {damage_var} damage' + (' ' + target if target else '') + (f' {hits} times' if hits > 1 else '') + '.',
                    f'{target}{damage_var}ダメージを' + (f'{hits}回' if hits > 1 else '') + '与える。'][language]
            if language == 0:
                text = f'{target}造成{damage_var}点伤害' + (f'，共{hits}次' if hits > 1 else '') + '。'
        else:
            templates = {
                'Block': [f'获得{var}点[gold]格挡[/gold]。', f'Gain {var} [gold]Block[/gold].', f'[gold]ブロック[/gold]{var}を得る。'],
                'AllyBlock': [f'所有玩家获得{var}点[gold]格挡[/gold]。', f'ALL players gain {var} [gold]Block[/gold].', f'プレイヤー全員が[gold]ブロック[/gold]{var}を得る。'],
                'Draw': [f'抽{var}张牌。', f'Draw {var} cards.', f'カードを{var}枚引く。'],
                'Energy': [('获得' + '[E]' * int(effect[2] if upgraded else effect[1]) + '。'), ('Gain ' + '[E]' * int(effect[2] if upgraded else effect[1]) + '.'), ('[E]' * int(effect[2] if upgraded else effect[1]) + 'を得る。')],
                'Weak': [(f'对所有敌人施加' if opt.get('all') else '施加') + f'{var}层[gold]虚弱[/gold]。', f'Apply {var} [gold]Weak[/gold]' + (' to ALL enemies' if opt.get('all') else '') + '.', ('敵全体に' if opt.get('all') else '敵1体に') + f'[gold]脱力[/gold]{var}を付与する。'],
                'Vulnerable': [(f'对所有敌人施加' if opt.get('all') else '施加') + f'{var}层[gold]易伤[/gold]。', f'Apply {var} [gold]Vulnerable[/gold]' + (' to ALL enemies' if opt.get('all') else '') + '.', ('敵全体に' if opt.get('all') else '敵1体に') + f'[gold]弱体[/gold]{var}を付与する。'],
                'Strength': [f'获得{var}点[gold]力量[/gold]。', f'Gain {var} [gold]Strength[/gold].', f'[gold]筋力[/gold]{var}を得る。'],
                'Dexterity': [f'获得{var}点[gold]敏捷[/gold]。', f'Gain {var} [gold]Dexterity[/gold].', f'[gold]敏捷性[/gold]{var}を得る。'],
                'DodgeToDraw': [f'将{var}张[gold]{dodge}[/gold]洗入[gold]抽牌堆[/gold]。', f'Shuffle {var} [gold]{dodge}[/gold] cards into your [gold]draw pile[/gold].', f'[gold]{dodge}[/gold]を{var}枚[gold]山札[/gold]に加えてシャッフルする。'],
                'DodgeToHand': [f'将{var}张[gold]{dodge}[/gold]加入[gold]手牌[/gold]。', f'Add {var} [gold]{dodge}[/gold] cards to your [gold]hand[/gold].', f'[gold]{dodge}[/gold]を{var}枚[gold]手札[/gold]に加える。'],
                'AllyDodge': [f'所有玩家将{var}张[gold]{dodge}[/gold]加入[gold]手牌[/gold]。', f'ALL players add {var} [gold]{dodge}[/gold] cards to their [gold]hands[/gold].', f'プレイヤー全員が[gold]{dodge}[/gold]を{var}枚[gold]手札[/gold]に加える。'],
                'AdvanceMoment': [f'[gold]时刻[/gold]额外增加{var}。', f'Advance [gold]Moment[/gold] by an additional {var}.', f'[gold]時刻[/gold]を追加で{var}進める。'],
                'SetMoment': [f'使[gold]时刻[/gold]变为{var}。', f'Set [gold]Moment[/gold] to {var}.', f'[gold]時刻[/gold]を{var}にする。'],
                'ShuffleHand': [f'选择{var}张手牌洗入[gold]抽牌堆[/gold]。', f'Choose {var} cards in your hand and shuffle them into your [gold]draw pile[/gold].', f'手札から{var}枚選び、[gold]山札[/gold]に加えてシャッフルする。'],
                'ShuffleDiscard': [f'选择{var}张弃牌堆中的牌洗入[gold]抽牌堆[/gold]。', f'Choose {var} cards in your discard pile and shuffle them into your [gold]draw pile[/gold].', f'捨て札から{var}枚選び、[gold]山札[/gold]に加えてシャッフルする。'],
                'ShuffleDiscardAll': ['将弃牌堆洗入[gold]抽牌堆[/gold]。', 'Shuffle your discard pile into your [gold]draw pile[/gold].', '捨て札を[gold]山札[/gold]に加えてシャッフルする。'],
                'RewindTurn': ['使牌堆和卡牌费用状态回到本回合开始时。', 'Restore your piles and card costs to their state at the start of this turn.', 'カードの山とコストをこのターン開始時の状態に戻す。'],
                'DodgeAtTurnStart': [f'每回合开始时，将{var}张[gold]{dodge}[/gold]洗入[gold]抽牌堆[/gold]。', f'At the start of each turn, shuffle {var} [gold]{dodge}[/gold] cards into your [gold]draw pile[/gold].', f'毎ターン開始時、[gold]{dodge}[/gold]を{var}枚[gold]山札[/gold]に加えてシャッフルする。'],
                'ReactionBlock': [f'每触发一次[gold]反应[/gold]，获得{var}点[gold]格挡[/gold]。', f'Whenever [gold]Reaction[/gold] triggers, gain {var} [gold]Block[/gold].', f'[gold]リアクション[/gold]が発動するたび、[gold]ブロック[/gold]{var}を得る。'],
                'MomentFiveBlock': [f'每当[gold]时刻[/gold]到达5，获得{var}点[gold]格挡[/gold]。', f'Whenever [gold]Moment[/gold] reaches 5, gain {var} [gold]Block[/gold].', f'[gold]時刻[/gold]が5に到達するたび、[gold]ブロック[/gold]{var}を得る。'],
                'MomentFiveDraw': [f'每当[gold]时刻[/gold]到达5，抽{var}张牌。', f'Whenever [gold]Moment[/gold] reaches 5, draw {var} cards.', f'[gold]時刻[/gold]が5に到達するたび、カードを{var}枚引く。'],
                'DodgeMoment': [f'每打出一张[gold]闪避[/gold]，[gold]时刻[/gold]额外增加{var}。', f'Whenever you play [gold]Dodge[/gold], advance [gold]Moment[/gold] by an additional {var}.', f'[gold]回避[/gold]をプレイするたび、[gold]時刻[/gold]を追加で{var}進める。'],
                'ShuffleBlock': [f'每将一张手牌或弃牌洗入抽牌堆，获得{var}点[gold]格挡[/gold]。', f'Whenever you shuffle a card from your hand or discard pile into your draw pile, gain {var} [gold]Block[/gold].', f'手札か捨て札からカードを山札に加えてシャッフルするたび、[gold]ブロック[/gold]{var}を得る。'],
            }
            text = templates[kind][language]
        if len(effect) > 3:
            prefix = {
                'first': ['若此牌是你本回合打出的第一张牌，', 'If this is your first card this turn, ', 'このターン最初にプレイしたカードなら、'],
                'afterSkill': ['若你本回合上一张打出的是技能牌，', 'If your previous card this turn was a Skill, ', 'このターン直前にプレイしたカードがスキルなら、'],
                'afterAttack': ['若你本回合上一张打出的是攻击牌，', 'If your previous card this turn was an Attack, ', 'このターン直前にプレイしたカードがアタックなら、'],
                'moment': [f'[gold]时刻{opt.get("moment")}[/gold]：', f'[gold]Moment {opt.get("moment")}[/gold]: ', f'[gold]時刻{opt.get("moment")}[/gold]：'],
            }[effect[3]][language]
            text = prefix + (text[0].lower() + text[1:] if language == 1 else text)
        out.append(text)
    if opt.get('restageDivisor'):
        divisor = opt['restageDivisor']
        moment = opt.get('moment', 5)
        out.append([
            f'[gold]时刻{moment}[/gold]：本回合每造成过{divisor}点伤害，此牌造成的伤害增加1点。',
            f'[gold]Moment {moment}[/gold]: For every {divisor} damage you have dealt this turn, this card deals 1 more damage.',
            f'[gold]時刻{moment}[/gold]：このターンに与えたダメージ{divisor}につき、このカードのダメージが1増加する。',
        ][language])
    return '\n'.join(out)

def chinese_card_table():
    rarity_names = {'Basic': '初始', 'Common': '普通', 'Uncommon': '罕见',
                    'Rare': '稀有', 'Ancient': '先古', 'Token': '衍生'}
    type_names = {'Attack': '攻击', 'Skill': '技能', 'Power': '能力'}
    lines = [
        '# 女爵时刻版完整卡表',
        '',
        '本表由 `design/duchess/cards.json` 自动生成；数值格式为“基础 / 升级”。',
        '',
        '| 卡牌 | 分类 | 耗能 | 基础效果 | 升级后效果 |',
        '|---|---|---:|---|---|',
    ]
    for row in ROWS:
        opt = row[8] if len(row) > 8 else {}
        rendered = []
        for upgraded in (False, True):
            text = effects_text(row, 0, upgraded)
            for effect in row[7]:
                key = 'Block' if effect[0] == 'AllyBlock' else effect[0]
                if effect[0] == 'Damage' and opt.get('restageDivisor'):
                    key = 'CalculatedDamage'
                value = effect[2] if upgraded else effect[1]
                value_text = str(int(value)) if int(value) == value else str(value)
                text = text.replace('{' + key + ':diff()}', value_text)
            rendered.append(text.replace('[gold]', '').replace('[/gold]', '')
                            .replace('\n', '<br>').replace('|', '\\|'))
        category = rarity_names[row[6]] + type_names[row[5]]
        lines.append(f'| {row[1]} | {category} | {row[4]} | {rendered[0]} | {rendered[1]} |')
    return '\n'.join(lines) + '\n'

def outputs():
    code = ['// Generated by tools/generate_duchess_cards.py. Edit design/duchess/cards.json.',
            'using System.Collections.Generic;', 'using MegaCrit.Sts2.Core.Entities.Cards;',
            'namespace NightMustStay.Core.Models.Cards;',
            'public static class DuchessCardCatalog { public static readonly Dictionary<string, DuchessCardSpec> All = new() {']
    for row in ROWS:
        name, _, _, _, cost, kind, rarity, effects, *options = row
        opt = options[0] if options else {}
        effect_code = ', '.join(f'new("{e[0]}", {e[1]}m, {e[2]}m, "{e[3] if len(e)>3 else ""}")' for e in effects)
        flags = ', '.join(f'{key}: {str(opt.get(value, False)).lower()}' for key, value in [('All','all'),('Exhaust','exhaust'),('Retain','retain'),('UpgradeTokens','upgradeTokens'),('Reaction','reaction')])
        code.append(f'["Duchess{name}"] = new({cost}, CardType.{kind}, CardRarity.{rarity}, new DuchessEffect[] {{ {effect_code} }}, Hits: {opt.get("hits",1)}, {flags}, Moment: {opt.get("moment",0)}, RestageDivisor: {opt.get("restageDivisor",0)}),')
    code.append('}; }')
    for row in ROWS:
        name = 'Duchess' + row[0]
        code.append(f'public sealed class {name} : DuchessCard {{ public {name}() : base(nameof({name})) {{ }} }}')
    yield ROOT / 'src/Core/Models/Cards/DuchessCards.Generated.cs', '\n'.join(code) + '\n'
    pool = '''using Godot;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;
namespace NightMustStay.Core.Models.CardPools;
public sealed class DuchessCardPool : CardPoolModel
{
    public override string Title => "duchess";
    public override string EnergyColorName => "duchess";
    public override string CardFrameMaterialPath => "card_frame_duchess";
    public override Color DeckEntryCardColor => new("809CC8");
    public override Color EnergyOutlineColor => new("17243D");
    public override bool IsColorless => false;
    protected override CardModel[] GenerateAllCards() => new CardModel[] {
'''
    pool += '\n'.join('        ModelDb.Card<Duchess' + row[0] + '>(),' for row in ROWS if row[6] != 'Token')
    yield ROOT / 'src/Core/Models/CardPools/DuchessCardPool.cs', pool + '\n    };\n}\n'
    yield ROOT / 'design/duchess/女爵时刻版完整卡表.md', chinese_card_table()
    support_path = ROOT / 'design/duchess/localization.json'
    support = json.loads(support_path.read_text(encoding='utf-8')) if support_path.exists() else {}
    for lang, locale in enumerate(['zhs', 'eng', 'jpn']):
        path = ROOT / f'NightMustStay/localization/{locale}/cards.json'
        data = json.loads(path.read_text(encoding='utf-8-sig'))
        live_keys = {model_id(row[0]) for row in ROWS}
        for key in list(data):
            if key.startswith('DUCHESS_') and key.rsplit('.', 1)[0] not in live_keys:
                del data[key]
        for row in ROWS:
            key = model_id(row[0])
            data[key + '.title'] = row[lang+1]
            data[key + '.description'] = effects_text(row, lang, False)
            data[key + '.upgradeDescription'] = effects_text(row, lang, True)
        data.update({key: values[lang] for key, values in support.get('cards', {}).items()})
        yield path, json.dumps(data, ensure_ascii=False, indent=2) + '\n'
    if support:
        for table, entries in support.items():
            if table == 'cards' or not entries:
                continue
            for language, locale in enumerate(['zhs', 'eng', 'jpn']):
                path = ROOT / f'NightMustStay/localization/{locale}/{table}.json'
                data = json.loads(path.read_text(encoding='utf-8-sig'))
                if table == 'powers':
                    for key in list(data):
                        if key.startswith('DUCHESS_') and key not in entries:
                            del data[key]
                data.update({key: values[language] for key, values in entries.items()})
                yield path, json.dumps(data, ensure_ascii=False, indent=2) + '\n'

def main():
    check = argparse.ArgumentParser()
    check.add_argument('--check', action='store_true')
    args = check.parse_args()
    mismatches = []
    for path, content in outputs():
        if args.check:
            if not path.exists() or path.read_text(encoding='utf-8-sig') != content:
                mismatches.append(str(path.relative_to(ROOT)))
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(content, encoding='utf-8', newline='\n')
    if mismatches:
        raise SystemExit('Out-of-date Duchess exports: ' + ', '.join(mismatches))
    print(f'Duchess table: {len(ROWS)} cards; generated code and three authored locales agree.')

if __name__ == '__main__':
    main()
