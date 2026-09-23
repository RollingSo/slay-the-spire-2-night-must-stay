"""Deterministic code/text export from the current local Duchess snapshot.

Translations below are authored text templates, not an external translation service.
Check the authoritative Feishu table before editing design/duchess/cards.json.
Run after editing that file; --check never modifies files.
"""
import argparse
import difflib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ROWS = json.loads((ROOT / 'design/duchess/cards.json').read_text(encoding='utf-8'))

def validate_upgrade_contracts():
    keyword_flags = {'upgradeRetain', 'upgradeInnate', 'upgradeRemoveExhaust'}
    keyword_only_rows = []
    for row in ROWS:
        opt = row[8] if len(row) > 8 else {}
        value_change = any(effect[1] != effect[2] for effect in row[7])
        text_rule_change = bool(opt.get('upgradeTokens')) or bool(opt.get('upgradeX')) or opt.get('upgradedMoment', -1) >= 0 or opt.get('upgradeCost', -1) >= 0 or opt.get('upgradeHits', -1) >= 0
        keyword_change = any(opt.get(key) for key in keyword_flags)
        # The game reads only .description, not .upgradeDescription. Any textual
        # upgrade must therefore be encoded with its native IfUpgraded formatter.
        if text_rule_change and effects_text(row, 0, False) == effects_text(row, 0, True):
            raise ValueError(f'{row[0]} upgrades its mechanics but has no visible upgraded rules-text difference')
        if not (value_change or text_rule_change or keyword_change):
            if any(effects_text(row, language, False) != effects_text(row, language, True) for language in range(3)):
                raise ValueError(f'{row[0]} has unexplained upgraded rules text')
        for language in range(3):
            base = effects_text(row, language, False)
            upgraded = effects_text(row, language, True)
            combined = runtime_description(row, language)
            if expand_upgrade_conditionals(combined, False) != base or expand_upgrade_conditionals(combined, True) != upgraded:
                raise ValueError(f'{row[0]} {language}: runtime description does not render both upgrade states')
        if opt.get('upgradeTokens'):
            base, upgraded = effects_text(row, 0, False), effects_text(row, 0, True)
            token_name = '辉剑' if any(effect[0] in ('RadiantBladeToDraw', 'RadiantBladeToHand', 'InstinctRadiantBladesToDraw') for effect in row[7]) else '闪避'
            if f'[gold]{token_name}+[/gold]' in base or f'[gold]{token_name}+[/gold]' not in upgraded:
                raise ValueError(f'{row[0]} must generate {token_name} before upgrade and {token_name}+ only after upgrade')
        if keyword_change and not (value_change or text_rule_change):
            keyword_only_rows.append(row[0])

    source = (ROOT / 'src/Core/Models/Cards/DuchessCard.cs').read_text(encoding='utf-8')
    required_mutations = ('AddKeyword(CardKeyword.Retain)', 'AddKeyword(CardKeyword.Innate)',
                          'RemoveKeyword(CardKeyword.Exhaust)')
    if any(flag in str(ROWS) for flag in keyword_flags) and any(call not in source for call in required_mutations):
        raise ValueError('Duchess keyword upgrades must be explicit OnUpgrade mutations for upgrade previews')
    if any(effect[0] == 'Replay' for row in ROWS for effect in row[7]) and 'case "Replay":' not in source:
        raise ValueError('Native Replay must be accepted as a declarative marker by DuchessCard.OnPlay')
    if ('NextTurnEnergy' in str(ROWS) or 'FutureMomentEnergy' in str(ROWS)) \
            and '"Energy" or "NextTurnEnergy" or "FutureMomentEnergy"' not in source:
        raise ValueError('Duchess energyIcons variables must be backed by EnergyVar')

def model_id(name):
    return re.sub(r'(?<!^)(?=[A-Z])', '_', 'Duchess' + name).upper()

def runtime_description(row, language):
    """Embed nonnumeric upgrades in the one description the game actually reads."""
    base = effects_text(row, language, False)
    upgraded = effects_text(row, language, True)
    parts = []
    for op, a0, a1, b0, b1 in difflib.SequenceMatcher(None, base, upgraded, autojunk=False).get_opcodes():
        before, after = base[a0:a1], upgraded[b0:b1]
        if op == 'equal':
            parts.append(before)
            continue
        if any(char in before + after for char in '{}|'):
            raise ValueError(f'{row[0]}: upgrade changes a formatter expression; author the conditional explicitly')
        parts.append('{IfUpgraded:show:' + after + '|' + before + '}')
    return ''.join(parts)

def expand_upgrade_conditionals(text, upgraded):
    return re.sub(r'\{IfUpgraded:show:([^{}|]*)\|([^{}]*)\}',
                  lambda match: match.group(1 if upgraded else 2), text)

def effects_text(row, language, upgraded=False):
    opt = row[8] if len(row) > 8 else {}
    out = []
    # Native keywords are rendered from CanonicalKeywords. Do not duplicate
    # Retain, Exhaust or Innate in authored rules text.
    if opt.get('reaction'):
        out.append(['[gold]反应[/gold]。', '[gold]Reaction[/gold].', '[gold]リアクション[/gold]。'][language])
    if opt.get('unplayable'):
        out.append(['无法打出。', 'Cannot be played.', 'プレイできない。'][language])
    for effect in row[7]:
        kind = effect[0]
        var_key = 'Block' if kind == 'AllyBlock' else 'ExtraDamage' if kind in ('ConcealedBonusDamage', 'MomentBonusDamage') else kind
        var = '{' + var_key + ':diff()}'
        dodge = ['闪避', 'Dodge', '回避'][language] + ('+' if upgraded and opt.get('upgradeTokens') else '')
        if kind == 'Damage':
            hits = opt.get('upgradeHits', opt.get('hits', 1)) if upgraded else opt.get('hits', 1)
            target = ['对所有敌人', 'to ALL enemies', '敵全体に'][language] if opt.get('all') else ['', '', '敵1体に'][language]
            damage_var = '{CalculatedDamage:diff()}' if opt.get('restageDivisor') or opt.get('concealedTripleDamage') or any(e[0] in ('ConcealedBonusDamage', 'MomentBonusDamage') for e in row[7]) else var
            text = [f'{target}造成{var}点伤害' + (f'，共{hits}次' if hits > 1 else '') + '。',
                    f'Deal {damage_var} damage' + (' ' + target if target else '') + (f' {hits} times' if hits > 1 else '') + '.',
                    f'{target}{damage_var}ダメージを' + (f'{hits}回' if hits > 1 else '') + '与える。'][language]
            if language == 0:
                text = f'{target}造成{damage_var}点伤害' + (f'，共{hits}次' if hits > 1 else '') + '。'
        else:
            templates = {
                'Block': [f'获得{var}点[gold]格挡[/gold]。', f'Gain {var} [gold]Block[/gold].', f'[gold]ブロック[/gold]{var}を得る。'],
                'TurnStartSwap': ['每回合开始时，选择1张[gold]手牌[/gold]洗入[gold]抽牌堆[/gold]，抽1张牌。', 'At the start of each turn, choose 1 card in your [gold]hand[/gold], shuffle it into your [gold]draw pile[/gold], then draw 1 card.', '毎ターン開始時、[gold]手札[/gold]から1枚選んで[gold]山札[/gold]に加えてシャッフルし、カードを1枚引く。'],
                'HandToDrawTopBlock': [f'将[gold]手牌[/gold]中任意张牌放到[gold]抽牌堆[/gold]顶部。\n每放回1张，获得{var}点[gold]格挡[/gold]。', f'Put any number of cards from your [gold]hand[/gold] on top of your [gold]draw pile[/gold].\nGain {var} [gold]Block[/gold] for each card returned.', f'[gold]手札[/gold]から好きな枚数を[gold]山札[/gold]の一番上に置く。\n戻したカード1枚につき[gold]ブロック[/gold]{var}を得る。'],
                'AllyIntangible': [f'所有玩家获得{var}层[gold]无实体[/gold]。', f'ALL players gain {var} [gold]Intangible[/gold].', f'プレイヤー全員が[gold]無形[/gold]{var}を得る。'],
                'DrawUntilReaction': ['抽牌，直到抽到1张[gold]反应[/gold]牌。', 'Draw cards until you draw a [gold]Reaction[/gold] card.', '[gold]リアクション[/gold]のカードを引くまでカードを引く。'],
                'AoeDamage': [f'对所有敌人额外造成{var}点伤害。', f'Deal {var} additional damage to ALL enemies.', f'敵全体に追加で{var}ダメージを与える。'],
                'ExtraDamage': [f'额外造成{var}点伤害。', f'Deal {var} additional damage.', f'追加で{var}ダメージを与える。'],
                'AllyBlock': [f'所有玩家获得{var}点[gold]格挡[/gold]。', f'ALL players gain {var} [gold]Block[/gold].', f'プレイヤー全員が[gold]ブロック[/gold]{var}を得る。'],
                'Draw': [f'抽{var}张牌。', f'Draw {var} cards.', f'カードを{var}枚引く。'],
                'ReactionDraw': [f'抽到[gold]反应[/gold]牌时，额外抽{var}张牌。', f'Whenever you draw a [gold]Reaction[/gold] card, draw {var} additional card.', f'[gold]リアクション[/gold]のカードを引くたび、追加で{var}枚引く。'],
                'ReactionDrawBlock': [f'回合中抽到[gold]反应[/gold]牌时，获得{var}点[gold]格挡[/gold]。', f'Whenever you draw a [gold]Reaction[/gold] card during your turn, gain {var} [gold]Block[/gold].', f'自分のターン中に[gold]リアクション[/gold]のカードを引くたび、[gold]ブロック[/gold]{var}を得る。'],
                'Concealment': [f'获得{var}层[gold]隐匿[/gold]。', f'Gain {var} [gold]Concealment[/gold].', f'[gold]隠密[/gold]{var}を得る。'],
                'ConcealedBonusDamage': [f'[gold]隐匿[/gold]时打出，伤害额外加{var}。', f'If played while [gold]Concealed[/gold], deal {var} additional damage.', f'[gold]隠密[/gold]中にプレイすると、追加で{var}ダメージを与える。'],
                'MomentBonusDamage': [f'伤害+{var}。', f'Damage +{var}.', f'ダメージ+{var}。'],
                'DrawReactionDamageBoost': [f'抽到这张牌时，使其在下次打出前伤害+{var}。', f'When drawn, this card deals {var} more damage until it is next played.', f'このカードを引いたとき、次にプレイするまでダメージが{var}増加する。'],
                'DrawReactionFromPile': ['从[gold]抽牌堆[/gold]中随机抽取1张[gold]反应[/gold]牌。', 'Draw 1 random [gold]Reaction[/gold] card from your [gold]draw pile[/gold].', '[gold]山札[/gold]からランダムな[gold]リアクション[/gold]のカードを1枚引く。'],
                'RadiantBladeGrowth': [f'每打出1张[gold]辉剑[/gold]，本场战斗中所有[gold]辉剑[/gold]的伤害+{var}。', f'Whenever you play a [gold]Radiant Blade[/gold], all [gold]Radiant Blades[/gold] gain {var} damage for this combat.', f'[gold]輝剣[/gold]をプレイするたび、この戦闘中すべての[gold]輝剣[/gold]のダメージが{var}増加する。'],
                'FullBlockRadiantBlade': [f'本回合完全[gold]格挡[/gold]住攻击时，将{var}张[gold]辉剑[/gold]加入[gold]手牌[/gold]。', f'Whenever you fully [gold]block[/gold] an attack this turn, add {var} [gold]Radiant Blades[/gold] to your [gold]hand[/gold].', f'このターン攻撃を完全に防いだとき、[gold]輝剣[/gold]を{var}枚[gold]手札[/gold]に加える。'],
                'EndTurnDodge': [f'回合结束时，将{var}张[gold]{dodge}[/gold]洗入[gold]抽牌堆[/gold]。', f'At the end of your turn, shuffle {var} [gold]{dodge}[/gold] into your [gold]draw pile[/gold].', f'ターン終了時、[gold]{dodge}[/gold]を{var}枚[gold]山札[/gold]に加えてシャッフルする。'],
                'EndTurnRetain': [f'本回合结束时，[gold]保留[/gold]最多{var}张[gold]手牌[/gold]。', f'At the end of this turn, [gold]retain[/gold] up to {var} cards from your [gold]hand[/gold].', f'このターン終了時、[gold]手札[/gold]から最大{var}枚保留する。'],
                'MomentFiveFirstEnergy': [f'回合中[gold]时刻[/gold]首次到达5时，获得{var}能量。', f'The first time [gold]Moment[/gold] reaches 5 each turn, gain {var} Energy.', f'各ターン初めて[gold]時刻[/gold]が5に到達したとき、エナジーを{var}得る。'],
                'MomentFiveBlock': [f'到达[gold]时刻5[/gold]时，获得{var}点[gold]格挡[/gold]。', f'When you reach [gold]Moment 5[/gold], gain {var} [gold]Block[/gold].', f'[gold]時刻5[/gold]に到達したとき、[gold]ブロック[/gold]{var}を得る。'],
                'MomentTwelveEndTurn': ['到达[gold]时刻12[/gold]时，结束你的回合。', 'When you reach [gold]Moment 12[/gold], end your turn.', '[gold]時刻12[/gold]に到達したとき、ターンを終了する。'],
                'RestageEndTurnAoe': [f'本回合每造成过{var}点伤害，回合结束时对所有敌人造成1点伤害。', f'At end of turn, deal 1 damage to ALL enemies for every {var} damage you dealt this turn.', f'このターン与えたダメージ{var}につき、ターン終了時に敵全体へ1ダメージを与える。'],
                'ReplayMomentThree': ['本回合内，重放你在[gold]时刻3[/gold]打出的卡牌。', 'This turn, replay cards you play at [gold]Moment 3[/gold].', 'このターン、[gold]時刻3[/gold]でプレイしたカードをリプレイする。'],
                'AllyDodgeDrawX': [f'将X{ "+1" if upgraded and opt.get("upgradeX") else ""}张[gold]闪避[/gold]洗入其他玩家的牌堆，其他玩家抽取相同数量的牌。', f'Shuffle X{ "+1" if upgraded and opt.get("upgradeX") else ""} [gold]Dodge[/gold] into each other player’s deck. They draw that many cards.', f'ほかのプレイヤーの山札に[gold]回避[/gold]をX{ "+1" if upgraded and opt.get("upgradeX") else ""}枚加えてシャッフルし、同じ枚数引く。'],
                'TransformStrike': [f'选择[gold]抽牌堆[/gold]中的{var}张[gold]打击[/gold]，将其永久变化为[gold]卡利亚迅剑[/gold]。\n升级过的[gold]打击[/gold]会变化为[gold]卡利亚迅剑+[/gold]。', f'Choose {var} [gold]Strike[/gold] in your [gold]draw pile[/gold] and permanently transform it into [gold]Carian Slicer[/gold].\nAn upgraded [gold]Strike[/gold] becomes [gold]Carian Slicer+[/gold].', f'[gold]山札[/gold]の[gold]ストライク[/gold]を{var}枚選び、恒久的に[gold]カーリアの速剣[/gold]に変化させる。\n強化済みの[gold]ストライク[/gold]は[gold]カーリアの速剣+[/gold]になる。'],
                'ChooseDrawToTop': ['选择[gold]抽牌堆[/gold]中的1张牌放到[gold]抽牌堆[/gold]顶部。', 'Choose 1 card in your [gold]draw pile[/gold] and put it on top of your [gold]draw pile[/gold].', '[gold]山札[/gold]からカードを1枚選び、[gold]山札[/gold]の一番上に置く。'],
                'EndTurnMomentBlock': ['回合结束时，获得等同于当前[gold]时刻[/gold]的[gold]格挡[/gold]。', 'At the end of your turn, gain [gold]Block[/gold] equal to your current [gold]Moment[/gold].', 'ターン終了時、現在の[gold]時刻[/gold]に等しい[gold]ブロック[/gold]を得る。'],
                'ReturnHandDamageBoost': [f'将这张牌放回[gold]手牌[/gold]，在下次打出前伤害+{var}。', f'Return this card to your [gold]hand[/gold]. Until it is next played, its damage increases by {var}.', f'このカードを[gold]手札[/gold]に戻す。次にプレイするまでダメージが{var}増加する。'],
                'Energy': [('获得' + '[E]' * int(effect[2] if upgraded else effect[1]) + '。'), ('Gain ' + '[E]' * int(effect[2] if upgraded else effect[1]) + '.'), ('[E]' * int(effect[2] if upgraded else effect[1]) + 'を得る。')],
                'Weak': [(f'对所有敌人施加' if opt.get('all') else '施加') + f'{var}层[gold]虚弱[/gold]。', f'Apply {var} [gold]Weak[/gold]' + (' to ALL enemies' if opt.get('all') else '') + '.', ('敵全体に' if opt.get('all') else '敵1体に') + f'[gold]脱力[/gold]{var}を付与する。'],
                'WeakAll': [f'给予所有敌人{var}层[gold]虚弱[/gold]。', f'Apply {var} [gold]Weak[/gold] to ALL enemies.', f'敵全体に[gold]脱力[/gold]{var}を付与する。'],
                'Vulnerable': [(f'对所有敌人施加' if opt.get('all') else '施加') + f'{var}层[gold]易伤[/gold]。', f'Apply {var} [gold]Vulnerable[/gold]' + (' to ALL enemies' if opt.get('all') else '') + '.', ('敵全体に' if opt.get('all') else '敵1体に') + f'[gold]弱体[/gold]{var}を付与する。'],
                'Strength': [f'获得{var}点[gold]力量[/gold]。', f'Gain {var} [gold]Strength[/gold].', f'[gold]筋力[/gold]{var}を得る。'],
                'TemporaryStrength': [f'本回合内获得{var}点[gold]力量[/gold]。', f'Gain {var} [gold]Strength[/gold] this turn.', f'このターン、[gold]筋力[/gold]{var}を得る。'],
                'Dexterity': [f'获得{var}点[gold]敏捷[/gold]。', f'Gain {var} [gold]Dexterity[/gold].', f'[gold]敏捷性[/gold]{var}を得る。'],
                'DodgeToDraw': [f'将{var}张[gold]{dodge}[/gold]洗入[gold]抽牌堆[/gold]。', f'Shuffle {var} [gold]{dodge}[/gold] cards into your [gold]draw pile[/gold].', f'[gold]{dodge}[/gold]を{var}枚[gold]山札[/gold]に加えてシャッフルする。'],
                'DodgeToHand': [f'将{var}张[gold]{dodge}[/gold]加入[gold]手牌[/gold]。', f'Add {var} [gold]{dodge}[/gold] cards to your [gold]hand[/gold].', f'[gold]{dodge}[/gold]を{var}枚[gold]手札[/gold]に加える。'],
                'AllyDodge': [f'所有玩家将{var}张[gold]{dodge}[/gold]加入[gold]手牌[/gold]。', f'ALL players add {var} [gold]{dodge}[/gold] cards to their [gold]hands[/gold].', f'プレイヤー全員が[gold]{dodge}[/gold]を{var}枚[gold]手札[/gold]に加える。'],
                'AdvanceMoment': [f'[gold]时刻[/gold]额外增加{var}。', f'Advance [gold]Moment[/gold] by an additional {var}.', f'[gold]時刻[/gold]を追加で{var}進める。'],
                'SetMoment': [f'使[gold]时刻[/gold]变为{var}。', f'Set [gold]Moment[/gold] to {var}.', f'[gold]時刻[/gold]を{var}にする。'],
                'ReturnMoment': [f'使[gold]时刻[/gold]回到{var}。', f'Return [gold]Moment[/gold] to {var}.', f'[gold]時刻[/gold]を{var}に戻す。'],
                'ShuffleHand': [f'选择{var}张[gold]手牌[/gold]洗入[gold]抽牌堆[/gold]。', f'Choose {var} cards in your [gold]hand[/gold] and shuffle them into your [gold]draw pile[/gold].', f'[gold]手札[/gold]から{var}枚選び、[gold]山札[/gold]に加えてシャッフルする。'],
                'ShuffleDiscard': [f'选择{var}张[gold]弃牌堆[/gold]中的牌洗入[gold]抽牌堆[/gold]。', f'Choose {var} cards in your [gold]discard pile[/gold] and shuffle them into your [gold]draw pile[/gold].', f'[gold]捨て札[/gold]から{var}枚選び、[gold]山札[/gold]に加えてシャッフルする。'],
                'ShuffleDiscardAll': ['将[gold]弃牌堆[/gold]中的所有牌洗入[gold]抽牌堆[/gold]。', 'Shuffle all cards in your [gold]discard pile[/gold] into your [gold]draw pile[/gold].', '[gold]捨て札[/gold]のすべてのカードを[gold]山札[/gold]に加えてシャッフルする。'],
                'RewindTurn': ['使你的牌堆和能量回到回合开始时的状态。', 'Restore your piles and Energy to their state at the start of this turn.', 'カードの山とエナジーをこのターン開始時の状態に戻す。'],
                'RestageAoe': [f'本回合每造成过{var}点伤害，对所有敌人造成1点伤害。', f'For every {var} damage dealt this turn, deal 1 damage to ALL enemies.', f'このターンに与えたダメージ{var}につき、敵全体に1ダメージを与える。'],
                'Replay': [f'[gold]重放[/gold]{var}。', f'[gold]Replay[/gold] {var}.', f'[gold]リプレイ[/gold]{var}。'],
                'NextTurnEnergy': ['下回合开始时，获得{NextTurnEnergy:energyIcons()}。', 'At the start of next turn, gain {NextTurnEnergy:energyIcons()}.', '次のターン開始時、{NextTurnEnergy:energyIcons()}を得る。'],
                'NextTurnDraw': [f'下回合开始时，抽{var}张牌。', f'At the start of next turn, draw {var} cards.', f'次のターン開始時、カードを{var}枚引く。'],
                'NextTurnEnergyAndDraw': [f'下回合开始时，获得1能量并抽{var}张牌。', f'At the start of next turn, gain 1 Energy and draw {var} card(s).', f'次のターン開始時、エナジーを1得て、カードを{var}枚引く。'],
                'RadiantBladeToDraw': [f'将{var}张[gold]{["辉剑", "Radiant Blade", "輝剣"][language] + ("+" if upgraded and opt.get("upgradeTokens") else "")}[/gold]洗入[gold]抽牌堆[/gold]。', f'Shuffle {var} [gold]{"Radiant Blade+" if upgraded and opt.get("upgradeTokens") else "Radiant Blade"}[/gold] cards into your [gold]draw pile[/gold].', f'[gold]{"輝剣+" if upgraded and opt.get("upgradeTokens") else "輝剣"}[/gold]を{var}枚[gold]山札[/gold]に加えてシャッフルする。'],
                'InstinctRadiantBladesToDraw': [f'将{var}张带有[gold]本能[/gold]的[gold]{"辉剑+" if upgraded and opt.get("upgradeTokens") else "辉剑"}[/gold]洗入[gold]抽牌堆[/gold]。', f'Shuffle {var} [gold]{"Radiant Blade+" if upgraded and opt.get("upgradeTokens") else "Radiant Blade"}[/gold] cards with [gold]Instinct[/gold] into your [gold]draw pile[/gold].', f'[gold]本能[/gold]を付与した[gold]{"輝剣+" if upgraded and opt.get("upgradeTokens") else "輝剣"}[/gold]を{var}枚[gold]山札[/gold]に加えてシャッフルする。'],
                'MomentDamage': ['造成等同于当前[gold]时刻[/gold]的伤害({CalculatedDamage:diff()}点)。', 'Deal damage equal to your current [gold]Moment[/gold] ({CalculatedDamage:diff()}).', '現在の[gold]時刻[/gold]に等しいダメージ({CalculatedDamage:diff()})を与える。'],
                'ReturnSelfToDrawTop': ['将这张牌放到[gold]抽牌堆[/gold]顶部。', 'Put this card on top of your [gold]draw pile[/gold].', 'このカードを[gold]山札[/gold]の一番上に置く。'],
                'RadiantBladeToHand': [f'将{var}张[gold]{["辉剑", "Radiant Blade", "輝剣"][language] + ("+" if upgraded and opt.get("upgradeTokens") else "")}[/gold]加入[gold]手牌[/gold]。', f'Add {var} [gold]{"Radiant Blade+" if upgraded and opt.get("upgradeTokens") else "Radiant Blade"}[/gold] cards to your [gold]hand[/gold].', f'[gold]{"輝剣+" if upgraded and opt.get("upgradeTokens") else "輝剣"}[/gold]を{var}枚[gold]手札[/gold]に加える。'],
                'RadiantBladeTurns': [f'接下来X{"+1" if upgraded and opt.get("upgradeX") else ""}个回合，在回合开始时将1张[gold]辉剑[/gold]加入[gold]手牌[/gold]。', f'For the next X{"+1" if upgraded and opt.get("upgradeX") else ""} turns, add 1 [gold]Radiant Blade[/gold] to your [gold]hand[/gold] at the start of your turn.', f'次のX{"+1" if upgraded and opt.get("upgradeX") else ""}ターンの間、ターン開始時に[gold]輝剣[/gold]1枚を[gold]手札[/gold]に加える。'],
                'ShuffleGrowth': [f'每次被洗入[gold]抽牌堆[/gold]后，伤害+{var}。', f'Whenever this is shuffled into your [gold]draw pile[/gold], increase its damage by {var}.', f'[gold]山札[/gold]に加えてシャッフルされるたび、ダメージが{var}増加する。'],
                'Intangible': [f'获得{var}层[gold]无实体[/gold]。', f'Gain {var} [gold]Intangible[/gold].', f'[gold]無形[/gold]{var}を得る。'],
                'FutureMomentEnergy': ['本回合到达[gold]时刻6[/gold]时，获得{FutureMomentEnergy:energyIcons()}。', 'When you reach [gold]Moment 6[/gold] this turn, gain {FutureMomentEnergy:energyIcons()}.', 'このターン[gold]時刻6[/gold]に到達した時、{FutureMomentEnergy:energyIcons()}を得る。'],
                'DodgeCurrentMoment': [f'将等同于当前[gold]时刻[/gold]数量的[gold]{dodge}[/gold]洗入[gold]抽牌堆[/gold]。', f'Shuffle [gold]{dodge}[/gold] cards equal to your current [gold]Moment[/gold] into your [gold]draw pile[/gold].', f'現在の[gold]時刻[/gold]に等しい枚数の[gold]{dodge}[/gold]を[gold]山札[/gold]に加えてシャッフルする。'],
                'DrawCurrentMoment': ['抽等同于当前[gold]时刻[/gold]数量的牌。', 'Draw cards equal to your current [gold]Moment[/gold].', '現在の[gold]時刻[/gold]に等しい枚数のカードを引く。'],
                'ReactionBlock': [f'每打出一张[gold]反应[/gold]牌，获得{var}点[gold]格挡[/gold]。', f'Whenever you play a [gold]Reaction[/gold] card, gain {var} [gold]Block[/gold].', f'[gold]リアクション[/gold]のカードをプレイするたび、[gold]ブロック[/gold]{var}を得る。'],
                'ShuffleRandomDamage': [f'这张牌每次被洗入[gold]抽牌堆[/gold]，对随机一名敌人造成{var}点伤害。', f'Whenever this card is shuffled into your [gold]draw pile[/gold], deal {var} damage to a random enemy.', f'このカードが[gold]山札[/gold]に加えてシャッフルされるたび、ランダムな敵1体に{var}ダメージを与える。'],
                'MomentFiveDraw': [f'每当[gold]时刻[/gold]到达5，抽{var}张牌。', f'Whenever [gold]Moment[/gold] reaches 5, draw {var} cards.', f'[gold]時刻[/gold]が5に到達するたび、カードを{var}枚引く。'],
                'DodgeMoment': [f'每打出一张[gold]闪避[/gold]，[gold]时刻[/gold]额外增加{var}。', f'Whenever you play [gold]Dodge[/gold], advance [gold]Moment[/gold] by an additional {var}.', f'[gold]回避[/gold]をプレイするたび、[gold]時刻[/gold]を追加で{var}進める。'],
                'ShuffleBlock': [f'每将一张[gold]手牌[/gold]或[gold]弃牌堆[/gold]中的牌洗入[gold]抽牌堆[/gold]，获得{var}点[gold]格挡[/gold]。', f'Whenever you shuffle a card from your [gold]hand[/gold] or [gold]discard pile[/gold] into your [gold]draw pile[/gold], gain {var} [gold]Block[/gold].', f'[gold]手札[/gold]か[gold]捨て札[/gold]からカードを[gold]山札[/gold]に加えてシャッフルするたび、[gold]ブロック[/gold]{var}を得る。'],
            }
            text = templates[kind][language]
        if len(effect) > 3:
            prefix = {
                'first': ['若此牌是你本回合打出的第一张牌，', 'If this is your first card this turn, ', 'このターン最初にプレイしたカードなら、'],
                'afterSkill': ['若你本回合上一张打出的是技能牌，', 'If your previous card this turn was a Skill, ', 'このターン直前にプレイしたカードがスキルなら、'],
                'afterAttack': ['若你本回合上一张打出的是攻击牌，', 'If your previous card this turn was an Attack, ', 'このターン直前にプレイしたカードがアタックなら、'],
                'moment': [f'[gold]时刻{opt.get("upgradedMoment") if upgraded and opt.get("upgradedMoment") is not None else opt.get("moment")}[/gold]：', f'[gold]Moment {opt.get("upgradedMoment") if upgraded and opt.get("upgradedMoment") is not None else opt.get("moment")}[/gold]: ', f'[gold]時刻{opt.get("upgradedMoment") if upgraded and opt.get("upgradedMoment") is not None else opt.get("moment")}[/gold]：'],
                'moment2': ['[gold]时刻2[/gold]：', '[gold]Moment 2[/gold]: ', '[gold]時刻2[/gold]：'],
                'moment4': ['[gold]时刻4[/gold]：', '[gold]Moment 4[/gold]: ', '[gold]時刻4[/gold]：'],
                'concealed': ['[gold]隐匿[/gold]时：', 'While [gold]Concealed[/gold]: ', '[gold]隠密[/gold]中：'],
            }[effect[3]][language]
            text = prefix + (text[0].lower() + text[1:] if language == 1 else text)
        out.append(text)
    if opt.get('shuffleSelf'):
        out.append(['将这张牌洗入你的[gold]抽牌堆[/gold]。',
                    'Shuffle this card into your [gold]draw pile[/gold].',
                    'このカードを[gold]山札[/gold]に加えてシャッフルする。'][language])
    if opt.get('momentCostReduction'):
        moment = opt.get('moment')
        reduction = opt['momentCostReduction']
        compact = opt.get('compactMomentCostText')
        out.append(([f'[gold]时刻{moment}[/gold]：耗能-{reduction}。',
                     f'[gold]Moment {moment}[/gold]: Cost -{reduction}.',
                     f'[gold]時刻{moment}[/gold]：コスト-{reduction}。'] if compact else
                    [f'[gold]时刻{moment}[/gold]：此牌耗能-{reduction}。',
                     f'[gold]Moment {moment}[/gold]: this card costs {reduction} less [E].',
                     f'[gold]時刻{moment}[/gold]：このカードのコストを{reduction}減らす。'])[language])
    if opt.get('restageDivisor'):
        divisor = opt['restageDivisor']
        moment = opt.get('moment', 5)
        out.append([
            f'[gold]时刻{moment}[/gold]：本回合每造成过{divisor}点伤害，此牌造成的伤害增加1点。',
            f'[gold]Moment {moment}[/gold]: For every {divisor} damage you have dealt this turn, this card deals 1 more damage.',
            f'[gold]時刻{moment}[/gold]：このターンに与えたダメージ{divisor}につき、このカードのダメージが1増加する。',
        ][language])
    if opt.get('concealedTripleDamage'):
        out.append(['[gold]隐匿[/gold]时打出，造成3倍伤害。', 'If played while [gold]Concealed[/gold], deal triple damage.', '[gold]隠密[/gold]中にプレイすると、ダメージが3倍になる。'][language])
    if opt.get('concealedCostReduction'):
        reduction = opt['concealedCostReduction']
        out.append([f'[gold]隐匿[/gold]时耗能-{reduction}。', f'Costs {reduction} less Energy while [gold]Concealed[/gold].', f'[gold]隠密[/gold]中はコスト-{reduction}。'][language])
    if opt.get('momentCostReductionDynamic'):
        out.append(['这张牌的耗能减少等同于当前[gold]时刻[/gold]的数量。', 'Costs less Energy equal to your current [gold]Moment[/gold].', '現在の[gold]時刻[/gold]に等しいだけコストが減少する。'][language])
    if upgraded and opt.get('upgradeCost', -1) >= 0 and opt['upgradeCost'] != row[4]:
        out.append([f'升级后耗能变为{opt["upgradeCost"]}。',
                    f'Upgraded cost: {opt["upgradeCost"]}.',
                    f'強化後のコストは{opt["upgradeCost"]}。'][language])
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
                key = 'Block' if effect[0] == 'AllyBlock' else 'ExtraDamage' if effect[0] in ('ConcealedBonusDamage', 'MomentBonusDamage') else effect[0]
                if effect[0] == 'Damage' and (opt.get('restageDivisor') or opt.get('concealedTripleDamage') or any(e[0] in ('ConcealedBonusDamage', 'MomentBonusDamage') for e in row[7])):
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
        flags = ', '.join(f'{key}: {str(opt.get(value, False)).lower()}' for key, value in [('All','all'),('Exhaust','exhaust'),('Retain','retain'),('UpgradeTokens','upgradeTokens'),('Reaction','reaction'),('ShuffleSelf','shuffleSelf'),('UpgradeRetain','upgradeRetain'),('UpgradeInnate','upgradeInnate'),('UpgradeRemoveExhaust','upgradeRemoveExhaust'),('TargetSelf','targetSelf'),('XCost','xCost'),('UpgradeX','upgradeX'),('MultiplayerOnly','multiplayerOnly'),('ConcealedTripleDamage','concealedTripleDamage'),('MomentCostReductionDynamic','momentCostReductionDynamic')])
        code.append(f'["Duchess{name}"] = new({cost}, CardType.{kind}, CardRarity.{rarity}, new DuchessEffect[] {{ {effect_code} }}, Hits: {opt.get("hits",1)}, UpgradeHits: {opt.get("upgradeHits",-1)}, ConcealedCostReduction: {opt.get("concealedCostReduction",0)}, {flags}, Moment: {opt.get("moment",-1)}, UpgradedMoment: {opt.get("upgradedMoment",-1)}, UpgradeCost: {opt.get("upgradeCost",-1)}, MomentCostReduction: {opt.get("momentCostReduction",0)}, RestageDivisor: {opt.get("restageDivisor",0)}, SecondaryMoment: {opt.get("secondaryMoment",-1)}),')
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
            data[key + '.description'] = runtime_description(row, lang)
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
    validate_upgrade_contracts()
    mismatches = []
    for path, content in outputs():
        if args.check:
            matches = path.exists() and path.read_text(encoding='utf-8-sig') == content
            if not matches and path.suffix == '.json' and path.exists():
                # Formatting-only differences in shared localization tables are
                # unrelated to Duchess data and should not fail the export check.
                matches = json.loads(path.read_text(encoding='utf-8-sig')) == json.loads(content)
            if not matches:
                mismatches.append(str(path.relative_to(ROOT)))
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(content, encoding='utf-8', newline='\n')
    if mismatches:
        raise SystemExit('Out-of-date Duchess exports: ' + ', '.join(mismatches))
    print(f'Duchess table: {len(ROWS)} cards; generated code and three authored locales agree.')

if __name__ == '__main__':
    main()
