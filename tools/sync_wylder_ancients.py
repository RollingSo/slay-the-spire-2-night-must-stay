"""Author/check Wylder Ancient keys without changing other characters."""
from pathlib import Path
import argparse
import json
import re

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'design/wylder/ancient_dialogues.json'
STANDARD = [('ancient', 'char'), ('ancient', 'char'), ('ancient', 'char', 'ancient')]
ARCHITECT = [('ancient', 'char'), ('ancient', 'ancient', 'char'), ('ancient', 'char', 'ancient'), ('ancient', 'char')]
ANCIENTS = {'NEOW', 'DARV', 'NONUPEIPE', 'OROBAS', 'PAEL', 'TANX', 'TEZCATARA', 'VAKUU', 'THE_ARCHITECT'}


def expected(locale):
    source = json.loads(SOURCE.read_text(encoding='utf-8'))['zhs' if locale == 'zhs' else 'eng']
    assert set(source) == ANCIENTS
    entries = {}
    for ancient, dialogues in source.items():
        speakers = ARCHITECT if ancient == 'THE_ARCHITECT' else STANDARD
        assert len(dialogues) == len(speakers), ancient
        for index, (lines, roles) in enumerate(zip(dialogues, speakers)):
            assert len(lines) == len(roles), (ancient, index)
            repeat = index == 3 if ancient == 'THE_ARCHITECT' else index > 0
            for line, (text, speaker) in enumerate(zip(lines, roles)):
                assert text.strip(), (ancient, index, line)
                stem = f'{ancient}.talk.WYLDER.{index}-{line}{"r" if repeat else ""}'
                entries[stem + '.' + speaker] = text
                if line < len(lines) - 1:
                    entries[stem + '.next'] = '继续' if locale == 'zhs' else 'Continue'
    assert len(entries) == 104
    return entries


def merge(text, entries):
    for key, value in entries.items():
        pattern = r'(?m)^(\s*"' + re.escape(key) + r'"\s*:\s*)("(?:\\.|[^"\\])*")'
        encoded = json.dumps(value, ensure_ascii=False)
        if re.search(pattern, text):
            text = re.sub(pattern, lambda match: match[1] + encoded, text)
        else:
            text = text.rstrip()[:-1].rstrip() + ',\n  ' + json.dumps(key) + ': ' + encoded + '\n}\n'
    return text


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    for locale in ('zhs', 'eng', 'jpn', 'kor'):
        path = ROOT / f'NightMustStay/localization/{locale}/ancients.json'
        text = path.read_text(encoding='utf-8-sig')
        entries = expected(locale)
        if not args.check:
            text = merge(text, entries)
            path.write_text(text, encoding='utf-8')
        table = json.loads(text)
        actual = {key: value for key, value in table.items() if '.talk.WYLDER.' in key}
        if actual != entries:
            raise SystemExit(f'{locale}: missing, stale or extra Wylder Ancient dialogue entries')
        print(f'{locale}: 104 Wylder Ancient keys including all next buttons and repeating lines: PASS')
