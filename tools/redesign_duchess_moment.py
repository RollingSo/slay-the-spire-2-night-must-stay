"""One-time deterministic migration from the prototype Duchess to Moment/Reaction."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / "design/duchess/cards.json"


def row(names, cost, kind, rarity, effects, options=None):
    return [*names, cost, kind, rarity, effects, options or {}]


updates = {
    "Restage": row(["Restage", "重演", "Restage", "リステージ"], 0, "Attack", "Basic",
                   [["Damage", 4, 6]], {"retain": True, "moment": 5, "restageDivisor": 3}),
    "Quickstep": row(["ElegantBearing", "优雅身段", "Elegant Bearing", "優雅な身ごなし"], 1, "Skill", "Basic",
                     [["DodgeToDraw", 1, 1], ["Draw", 2, 2]], {"upgradeTokens": True}),
    "PassingCut": row(["PassingCut", "擦身斩", "Passing Cut", "すれ違い斬り"], 2, "Attack", "Common",
                      [["Damage", 7, 9], ["Block", 7, 9]], {"reaction": True}),
    "BlindSpot": row(["BlindSpot", "视线之外", "Blind Spot", "死角"], 1, "Attack", "Common",
                     [["Damage", 8, 8], ["Vulnerable", 2, 3, "moment"]], {"moment": 0}),
    "PoisedExit": row(["PoisedExit", "从容退场", "Poised Exit", "優雅な退場"], 1, "Skill", "Common",
                      [["Damage", 8, 8], ["Weak", 2, 3, "moment"]], {"moment": 3}),
    "GlintstoneKnife": row(["GlintstoneKnife", "辉石小刀", "Glintstone Knife", "輝石の小刀"], 1, "Attack", "Common",
                          [["Damage", 7, 9], ["DodgeToDraw", 1, 1]]),
    "Pivot": row(["Pivot", "回身", "Pivot", "身返し"], 0, "Skill", "Common",
                 [["Block", 3, 5], ["AdvanceMoment", 1, 1]]),
    "VeiledStep": row(["VeiledStep", "隐步", "Veiled Step", "隠れ歩き"], 1, "Skill", "Common",
                      [["DodgeToDraw", 1, 2], ["Draw", 1, 1]]),
    "Feint": row(["Feint", "佯攻", "Feint", "陽動"], 0, "Skill", "Common",
                 [["AdvanceMoment", 2, 3]]),
    "SwayingStep": row(["SwayingStep", "摇曳步", "Swaying Step", "揺らめく歩み"], 1, "Skill", "Common",
                       [["DodgeToDraw", 3, 3]]),
    "RadiantBladeArray": row(["RadiantBladeArray", "辉剑圆阵", "Radiant Blade Array", "輝剣円陣"], 1, "Attack", "Common",
                              [["RadiantBladeToDraw", 3, 3]], {"upgradeTokens": True, "targetSelf": True}),
    "HiddenPocket": row(["HiddenPocket", "暗袋", "Hidden Pocket", "隠しポケット"], 1, "Skill", "Common",
                        [["DodgeToDraw", 2, 3]]),
    "SoftLanding": row(["SoftLanding", "无声落地", "Soft Landing", "静かな着地"], 1, "Skill", "Common",
                       [["Block", 7, 10], ["DodgeToDraw", 1, 1, "moment"]], {"moment": 4}),
    "Reverberation": row(["Reverberation", "余响", "Reverberation", "残響"], 1, "Skill", "Common",
                         [["ShuffleDiscard", 1, 2], ["Draw", 1, 2], ["Block", 4, 6]]),
    "Poised": row(["Poised", "整顿衣襟", "Poised", "襟を正す"], 1, "Skill", "Common",
                  [["Block", 5, 8], ["SetMoment", 3, 3]]),
    "CutAndFade": row(["CutAndFade", "斩后隐去", "Cut and Fade", "斬って消える"], 2, "Attack", "Uncommon",
                      [["Damage", 13, 17], ["DodgeToDraw", 1, 2]]),
    "SilverFlash": row(["SilverFlash", "银光一闪", "Silver Flash", "銀の閃き"], 2, "Attack", "Uncommon",
                       [["Damage", 12, 16], ["DodgeToDraw", 2, 2]], {"reaction": True}),
    "GapMoonshadow": row(["GapMoonshadow", "隙间月影", "Gap Moonshadow", "間隙の月影"], 1, "Attack", "Uncommon",
                         [["Damage", 7, 7], ["AoeDamage", 7, 9, "moment2"],
                          ["ExtraDamage", 14, 18, "moment4"]], {"moment": 2, "secondaryMoment": 4}),
    "PerfectAngle": row(["PerfectAngle", "绝佳角度", "Perfect Angle", "完璧な角度"], 2, "Attack", "Uncommon",
                        [["Damage", 19, 25], ["Vulnerable", 2, 3, "moment"]], {"moment": 6}),
    "Understudy": row(["Understudy", "替身", "Understudy", "身代わり"], 1, "Skill", "Uncommon",
                      [["DodgeToHand", 2, 3]], {"exhaust": True}),
    "HeldBreath": row(["HeldBreath", "屏息", "Held Breath", "息を潜める"], 1, "Skill", "Uncommon",
                      [["DodgeToHand", 1, 2]], {"exhaust": True, "reaction": True}),
    "Invitation": row(["Invitation", "邀舞", "Invitation", "舞への誘い"], 1, "Skill", "Uncommon",
                      [["AdvanceMoment", 2, 3], ["Draw", 1, 1]]),
    "TurnAside": row(["TurnAside", "侧身避让", "Turn Aside", "身をかわす"], 1, "Skill", "Uncommon",
                     [["Block", 9, 12], ["DodgeToDraw", 1, 2, "moment"]], {"moment": 4}),
    "QuietSignal": row(["QuietSignal", "无声暗号", "Quiet Signal", "無言の合図"], 1, "Skill", "Uncommon",
                      [["AllyBlock", 6, 9], ["AdvanceMoment", 1, 2]]),
    "MoonlitVeil": row(["MoonlitVeil", "月下帷幕", "Moonlit Veil", "月下の帳"], 2, "Skill", "Uncommon",
                       [["AllyDodge", 1, 2], ["Block", 7, 11]]),
    "DressRehearsal": row(["DressRehearsal", "预演", "Dress Rehearsal", "予行"], 1, "Skill", "Uncommon",
                          [["ShuffleHand", 1, 2], ["Draw", 2, 3]]),
    "QuickHands": row(["QuickHands", "巧手", "Quick Hands", "早業"], 0, "Skill", "Uncommon",
                      [["DodgeToHand", 1, 2]]),
    "StolenMoment": row(["StolenMoment", "偷来的片刻", "Stolen Moment", "盗んだひととき"], 1, "Skill", "Uncommon",
                       [["Block", 10, 14], ["SetMoment", 3, 3]]),
    "Sway": row(["Sway", "摇曳", "Sway", "ゆらめき"], 1, "Skill", "Uncommon",
                [["DodgeToDraw", 1, 2], ["AdvanceMoment", 2, 3]]),
    "Elegance": row(["Elegance", "沉着身段", "Measured Poise", "静かな身ごなし"], 1, "Power", "Uncommon",
                    [["DodgeAtTurnStart", 1, 2]]),
    "Composure": row(["Composure", "镇定", "Composure", "平静"], 1, "Power", "Uncommon",
                     [["ReactionBlock", 3, 4]]),
    "Afterglow": row(["Afterglow", "余晖护身", "Afterglow", "残光の護り"], 1, "Power", "Uncommon",
                    [["MomentFiveBlock", 5, 8]]),
    "HiddenRhythm": row(["HiddenRhythm", "暗中节拍", "Hidden Rhythm", "秘めた拍子"], 1, "Power", "Uncommon",
                       [["MomentFiveDraw", 1, 2]]),
    "UnseenResolve": row(["UnseenResolve", "不露声色", "Unseen Resolve", "密かな決意"], 1, "Power", "Uncommon",
                         [["DodgeMoment", 1, 2]]),
    "Finale": row(["Finale", "落幕", "Finale", "終幕"], 2, "Skill", "Rare",
                  [["AllyDodge", 2, 3], ["Draw", 2, 3]], {"exhaust": True}),
    "MidnightWaltz": row(["MidnightWaltz", "午夜圆舞曲", "Midnight Waltz", "真夜中のワルツ"], 2, "Attack", "Rare",
                         [["Damage", 5, 6], ["Draw", 2, 3, "moment"]], {"hits": 4, "moment": 6}),
    "LastWord": row(["LastWord", "最后一语", "Last Word", "最後の一言"], 3, "Attack", "Rare",
                    [["Damage", 32, 40], ["Draw", 3, 4, "moment"]], {"moment": 7}),
    "GrandReprise": row(["GrandReprise", "盛大返场", "Grand Reprise", "盛大な再登場"], 2, "Skill", "Rare",
                        [["ShuffleDiscardAll", 0, 0], ["Draw", 3, 4]], {"exhaust": True}),
    "BothPaths": row(["BothPaths", "兼顾两路", "Both Paths", "二つの道"], 2, "Skill", "Rare",
                    [["ShuffleHand", 2, 3], ["AllyBlock", 8, 11], ["Draw", 2, 3]], {"exhaust": True}),
    "DawnPromise": row(["DawnPromise", "破晓之约", "Dawn Promise", "暁の約束"], 2, "Power", "Rare",
                       [["DodgeAtTurnStart", 2, 3]]),
    "MagicRadiantBlade": row(["MagicRadiantBlade", "魔法辉剑", "Magic Radiant Blade", "魔法の輝剣"], 0, "Attack", "Rare",
                              [["RadiantBladeTurns", 0, 0]], {"xCost": True, "upgradeX": True, "targetSelf": True}),
    "ThiefsArsenal": row(["ThiefsArsenal", "盗贼的武库", "Thief's Arsenal", "盗賊の武器庫"], 2, "Skill", "Rare",
                         [["DodgeToHand", 3, 3]], {"exhaust": True, "upgradeTokens": True}),
    "PerfectRehearsal": row(["PerfectRehearsal", "完美预演", "Perfect Rehearsal", "完璧な予行"], 3, "Skill", "Rare",
                            [["RewindTurn", 0, 0], ["SetMoment", 3, 3]], {"exhaust": True}),
    "SharedStage": row(["SharedStage", "同台", "Shared Stage", "同じ舞台"], 2, "Skill", "Rare",
                       [["AllyBlock", 12, 16], ["AllyDodge", 1, 2]], {"exhaust": True}),
    "UnbrokenPoise": row(["UnbrokenPoise", "不坠的从容", "Unbroken Poise", "崩れぬ余裕"], 2, "Power", "Rare",
                         [["ShuffleBlock", 3, 4]]),
    "EternalRestage": row(["EternalRestage", "永不落幕的重演", "Eternal Restage", "終わらぬリステージ"], 0, "Attack", "Ancient",
                          [["Damage", 6, 8]], {"retain": True, "moment": 5, "restageDivisor": 2}),
    "FirstLight": row(["FirstLight", "初光", "First Light", "初光"], 1, "Skill", "Ancient",
                      [["AllyDodge", 2, 3], ["AllyBlock", 8, 12]], {"exhaust": True}),
    "FleetingBlade": row(["Dodge", "闪避", "Dodge", "回避"], 1, "Skill", "Token",
                         [["Block", 6, 9], ["Draw", 1, 1]], {"exhaust": True, "reaction": True}),
    "RadiantBlade": row(["RadiantBlade", "辉剑", "Radiant Blade", "輝剣"], 0, "Attack", "Token",
                         [["Damage", 4, 6]], {"exhaust": True}),
}

cards = json.loads(PATH.read_text(encoding="utf-8"))
seen = set()
renamed = {"ElegantBearing": "Quickstep", "Dodge": "FleetingBlade"}
for index, existing in enumerate(cards):
    key = renamed.get(existing[0], existing[0])
    if key in updates:
        cards[index] = updates[key]
        seen.add(key)

missing = set(updates) - seen
if missing:
    raise SystemExit(f"Missing Duchess rows: {sorted(missing)}")

PATH.write_text(json.dumps(cards, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

support_path = ROOT / "design/duchess/localization.json"
support = json.loads(support_path.read_text(encoding="utf-8"))
support["powers"] = {
    "DUCHESS_MOMENT_DESCRIPTION_POWER.title": ["时刻", "Moment", "時刻"],
    "DUCHESS_MOMENT_DESCRIPTION_POWER.description": [
        "每回合开始时时刻为1。\n每打出一张牌，时刻增加1。\n在对应时刻打出卡牌会触发其时刻效果。",
        "Moment is 1 at the start of each turn.\nWhenever you play a card, Moment increases by 1.\nPlay cards at their listed Moment to trigger their Moment effects.",
        "毎ターン開始時、時刻は1になる。\nカードをプレイするたび、時刻が1増加する。\n指定された時刻にカードをプレイすると、時刻効果が発動する。",
    ],
    "DUCHESS_REACTION_DESCRIPTION_POWER.title": ["反应", "Reaction", "リアクション"],
    "DUCHESS_REACTION_DESCRIPTION_POWER.description": [
        "在回合进行中抽到此牌时，其耗能减少1直到打出。",
        "When drawn during your turn, this card costs 1 less until played.",
        "自分のターン中に引いた時、プレイするまでコストが1減少する。",
    ],
    "DUCHESS_MOMENT_POWER.title": ["时刻", "Moment", "時刻"],
    "DUCHESS_MOMENT_POWER.description": ["记录当前时刻。", "Tracks the current Moment.", "現在の時刻を記録する。"],
    "DUCHESS_DODGE_AT_TURN_START_POWER.title": ["从容准备", "Poised Preparation", "優雅な備え"],
    "DUCHESS_DODGE_AT_TURN_START_POWER.description": [
        "每回合开始时，将{Amount}张[gold]闪避[/gold]洗入抽牌堆。",
        "At the start of each turn, shuffle {Amount} [gold]Dodge[/gold] cards into your draw pile.",
        "毎ターン開始時、[gold]回避[/gold]を{Amount}枚山札に加えてシャッフルする。",
    ],
    "DUCHESS_REACTION_BLOCK_POWER.title": ["镇定", "Composure", "平静"],
    "DUCHESS_REACTION_BLOCK_POWER.description": [
        "每触发一次[gold]反应[/gold]，获得{Amount}点[gold]格挡[/gold]。",
        "Whenever [gold]Reaction[/gold] triggers, gain {Amount} [gold]Block[/gold].",
        "[gold]リアクション[/gold]が発動するたび、[gold]ブロック[/gold]{Amount}を得る。",
    ],
    "DUCHESS_MOMENT_FIVE_BLOCK_POWER.title": ["余晖护身", "Afterglow", "残光の護り"],
    "DUCHESS_MOMENT_FIVE_BLOCK_POWER.description": [
        "每当[gold]时刻[/gold]到达5，获得{Amount}点[gold]格挡[/gold]。",
        "Whenever [gold]Moment[/gold] reaches 5, gain {Amount} [gold]Block[/gold].",
        "[gold]時刻[/gold]が5に到達するたび、[gold]ブロック[/gold]{Amount}を得る。",
    ],
    "DUCHESS_MOMENT_FIVE_DRAW_POWER.title": ["暗中节拍", "Hidden Rhythm", "秘めた拍子"],
    "DUCHESS_MOMENT_FIVE_DRAW_POWER.description": [
        "每当[gold]时刻[/gold]到达5，抽{Amount}张牌。",
        "Whenever [gold]Moment[/gold] reaches 5, draw {Amount} cards.",
        "[gold]時刻[/gold]が5に到達するたび、カードを{Amount}枚引く。",
    ],
    "DUCHESS_DODGE_MOMENT_POWER.title": ["不露声色", "Unseen Resolve", "密かな決意"],
    "DUCHESS_DODGE_MOMENT_POWER.description": [
        "每打出一张[gold]闪避[/gold]，[gold]时刻[/gold]额外增加{Amount}。",
        "Whenever you play [gold]Dodge[/gold], advance [gold]Moment[/gold] by an additional {Amount}.",
        "[gold]回避[/gold]をプレイするたび、[gold]時刻[/gold]を追加で{Amount}進める。",
    ],
    "DUCHESS_SHUFFLE_BLOCK_POWER.title": ["不坠的从容", "Unbroken Poise", "崩れぬ余裕"],
    "DUCHESS_SHUFFLE_BLOCK_POWER.description": [
        "每将一张手牌或弃牌洗入抽牌堆，获得{Amount}点[gold]格挡[/gold]。",
        "Whenever you shuffle a card from your hand or discard pile into your draw pile, gain {Amount} [gold]Block[/gold].",
        "手札か捨て札からカードを山札に加えてシャッフルするたび、[gold]ブロック[/gold]{Amount}を得る。",
    ],
}

relics = support["relics"]
relics["DUCHESS_OLD_POCKETWATCH.description"] = [
    "每当[gold]时刻[/gold]到达5，抽1张牌。",
    "Whenever [gold]Moment[/gold] reaches 5, draw 1 card.",
    "[gold]時刻[/gold]が5に到達するたび、カードを1枚引く。",
]
relics["DUCHESS_MENDED_POCKETWATCH.description"] = [
    "每当[gold]时刻[/gold]到达5，抽2张牌。",
    "Whenever [gold]Moment[/gold] reaches 5, draw 2 cards.",
    "[gold]時刻[/gold]が5に到達するたび、カードを2枚引く。",
]
relics["DUCHESS_LACE_CUFF.description"] = [
    "战斗开始时，将1张[gold]闪避[/gold]洗入抽牌堆。",
    "At the start of combat, shuffle 1 [gold]Dodge[/gold] into your draw pile.",
    "戦闘開始時、[gold]回避[/gold]1枚を山札に加えてシャッフルする。",
]
relics["DUCHESS_SILVER_THIMBLE.description"] = [
    "每回合第一次触发[gold]反应[/gold]时，抽1张牌。",
    "The first time [gold]Reaction[/gold] triggers each turn, draw 1 card.",
    "毎ターン最初に[gold]リアクション[/gold]が発動した時、カードを1枚引く。",
]
relics["DUCHESS_DANCE_SHOES.description"] = [
    "每回合开始时，[gold]时刻[/gold]额外增加1。",
    "At the start of each turn, advance [gold]Moment[/gold] by an additional 1.",
    "毎ターン開始時、[gold]時刻[/gold]を追加で1進める。",
]

potions = support["potions"]
potions["DUCHESS_SILVER_PERFUME.description"] = [
    "[gold]时刻[/gold]增加3。", "Advance [gold]Moment[/gold] by 3.", "[gold]時刻[/gold]を3進める。"
]
potions["DUCHESS_VEIL_VIAL.title"] = ["闪避小瓶", "Dodge Vial", "回避の小瓶"]
potions["DUCHESS_VEIL_VIAL.description"] = [
    "将2张[gold]闪避[/gold]加入手牌。",
    "Add 2 [gold]Dodge[/gold] cards to your hand.",
    "[gold]回避[/gold]2枚を手札に加える。",
]
potions["DUCHESS_MEMORY_DRAUGHT.title"] = ["定时饮剂", "Timing Draught", "時刻の霊薬"]
potions["DUCHESS_MEMORY_DRAUGHT.description"] = [
    "使[gold]时刻[/gold]变为5。\n抽1张牌。",
    "Set [gold]Moment[/gold] to 5.\nDraw 1 card.",
    "[gold]時刻[/gold]を5にする。\nカードを1枚引く。",
]

support["characters"]["DUCHESS.description"] = [
    "拨动怀表的时刻，以闪避与反应穿行于牌堆，在精准的节拍中给予致命一击。",
    "She turns the hands of her pocketwatch, weaving Dodge and Reaction through her deck before striking at the perfect Moment.",
    "懐中時計の時刻を操り、回避とリアクションで山札を巡り、完璧な瞬間に致命の一撃を放つ。",
]
support.setdefault("cards", {})["DUCHESS_SELECT_TO_SHUFFLE"] = [
    "选择要洗入抽牌堆的牌。", "Choose cards to shuffle into your draw pile.", "山札に加えてシャッフルするカードを選択。"
]

support_path.write_text(json.dumps(support, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

asset_report_path = ROOT / "design/duchess/asset_report.json"
if asset_report_path.exists():
    asset_report = json.loads(asset_report_path.read_text(encoding="utf-8"))
    power_sources = {
        "duchess_moment_description_power": "duchess_memory_power",
        "duchess_moment_power": "duchess_memory_power",
        "duchess_reaction_description_power": "duchess_echo_description_power",
        "duchess_dodge_at_turn_start_power": "duchess_opening_dance_power",
        "duchess_reaction_block_power": "duchess_measured_breath_power",
        "duchess_moment_five_block_power": "duchess_reprise_guard_power",
        "duchess_moment_five_draw_power": "duchess_reprise_step_power",
        "duchess_dodge_moment_power": "duchess_veil_reward_power",
        "duchess_shuffle_block_power": "duchess_step_power",
    }
    old_power_names = set(power_sources.values()) | {"duchess_veil_power"}
    for new_name, old_name in power_sources.items():
        for folder in ("images/powers", "powers"):
            source_key = f"{folder}/{old_name}.png"
            if source_key in asset_report:
                asset_report[f"{folder}/{new_name}.png"] = dict(asset_report[source_key])
    for old_name in old_power_names:
        for folder in ("images/powers", "powers"):
            asset_report.pop(f"{folder}/{old_name}.png", None)
    asset_report_path.write_text(
        json.dumps(asset_report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(f"Migrated {len(updates)} Duchess cards to Moment/Reaction.")
