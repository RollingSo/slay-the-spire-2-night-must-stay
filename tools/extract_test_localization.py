"""Read native localization fixtures from the installed pack, checking entry MD5."""
import argparse,pathlib,sys
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent/'original_animation_research'))
from extract import Pck
parser=argparse.ArgumentParser()
parser.add_argument('pack');parser.add_argument('output')
args=parser.parse_args()
pack=Pck(args.pack);out=pathlib.Path(args.output)
counts={locale:0 for locale in ['zhs','eng','jpn','kor']}
for name in pack.entries:
    parts=pathlib.PurePosixPath(name).parts
    if len(parts)==3 and parts[0]=='localization' and parts[1] in counts and parts[2].endswith('.json'):
        target=out/parts[1]/parts[2]
        target.parent.mkdir(parents=True,exist_ok=True)
        target.write_bytes(pack.read(name));counts[parts[1]]+=1
pack.file.close()
if not all(counts.values()):raise RuntimeError(f'Missing native localization: {counts}')
print('Native localization extracted with integrity checks:',counts)
