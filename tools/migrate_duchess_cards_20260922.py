"""One-time, deterministic Duchess card-table revision for 2026-09-22."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
path = root / 'design/duchess/cards.json'
rows = json.loads(path.read_text(encoding='utf-8'))

removed = {
    'SerratedEdge', 'TwinFangs', 'PerfectAngle', 'PaleMoonblade',
    'CutAndFade', 'TurnAside', 'HeldBreath', 'Understudy',
    'StolenMoment', 'QuietSignal', 'Sway', 'UnseenResolve',
    'BorrowedTime', 'BothPaths', 'SteadyHand', 'LightAsSilk',
    'UnbrokenPoise', 'FirstLight', 'DawnPromise', 'Nightfall',
    'FollowThrough', 'NoWitness', 'Encore', 'Backstage', 'VanishingAct',
}
rows = [row for row in rows if row[0] not in removed]
by_name = {row[0]: row for row in rows}

by_name['PoisedExit'][7] = [['Block', 7, 7], ['WeakAll', 2, 3, 'moment']]
by_name['PoisedExit'][8]['targetSelf'] = True
by_name['Pivot'][7] = [['Block', 4, 6], ['AdvanceMoment', 1, 1]]
by_name['Composure'][7] = [['ReactionBlock', 3, 3]]
by_name['Reverberation'][6] = 'Uncommon'
by_name['Reverberation'][7] = [['ShuffleDiscard', 2, 2], ['Draw', 2, 3]]

if 'LightningNerves' not in by_name:
    rows.insert(rows.index(by_name['Composure']) + 1, [
        'LightningNerves', '闪电神经', 'Lightning Nerves', '稲妻の神経',
        2, 'Power', 'Rare', [['ReactionDraw', 1, 1]], {'upgradeCost': 1},
    ])
if 'CarianSwordsmanship' not in by_name:
    rows.insert(rows.index(by_name['CarianSlicer']) + 1, [
        'CarianSwordsmanship', '卡利亚剑术', 'Carian Swordsmanship', 'カーリアの剣術',
        2, 'Power', 'Rare', [['TransformStrike', 1, 1]], {'upgradeCost': 1},
    ])
if 'GreatswordPhalanx' not in by_name:
    rows.insert(rows.index(by_name['CarianSlicer']) + 1, [
        'GreatswordPhalanx', '巨剑阵', 'Greatsword Phalanx', '大剣の円陣',
        2, 'Attack', 'Rare', [['InstinctRadiantBladesToDraw', 3, 3]],
        {'upgradeTokens': True, 'targetSelf': True},
    ])
if 'MiquellasHalo' not in by_name:
    rows.insert(next(i for i, row in enumerate(rows) if row[0] == 'GreatswordPhalanx') + 1, [
        'MiquellasHalo', '米凯拉的光环', "Miquella's Halo", 'ミケラの光輪',
        1, 'Attack', 'Rare', [['MomentDamage', 0, 0], ['ReturnSelfToDrawTop', 0, 0]],
        {'upgradeCost': 0},
    ])

if 'FallingMagic' not in by_name:
    rows.insert(rows.index(by_name['AngelWings']) + 1, [
        'FallingMagic', '天降魔力', 'Falling Magic', '降り注ぐ魔力',
        0, 'Skill', 'Rare', [['ShuffleRandomDamage', 6, 9]],
        {'retain': True, 'unplayable': True},
    ])
path.write_text(json.dumps(rows, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

manifest_path = root / 'design/duchess/art_manifest.json'
manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
manifest['cards'] = [entry for entry in manifest['cards'] if entry['key'] not in removed]
manifest['prompts']['cards'] = [entry for entry in manifest['prompts']['cards'] if entry['key'] not in removed]
manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

report_path = root / 'design/duchess/asset_report.json'
report = json.loads(report_path.read_text(encoding='utf-8'))
portrait_dir = root / 'images/packed/card_portraits/duchess'
for relative_path in list(report):
    if relative_path.startswith('images/packed/card_portraits/duchess/') and not (root / relative_path).exists():
        report.pop(relative_path)
old_dodge_key = 'images/packed/card_portraits/duchess/duchess_turn_aside.png'
new_dodge_key = 'images/packed/card_portraits/duchess/duchess_dodge.png'
if new_dodge_key not in report and (root / new_dodge_key).exists():
    from PIL import Image
    with Image.open(root / new_dodge_key) as image:
        report[new_dodge_key] = {'size': list(image.size), 'mode': image.mode, 'bounds': list(image.getbbox())}
report.pop(old_dodge_key, None)
report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
