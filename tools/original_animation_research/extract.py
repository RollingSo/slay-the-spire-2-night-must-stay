"""Read the installed game's PCK without changing it. Research assets stay in .tmp."""
import argparse
import hashlib
import json
import pathlib
import re
import struct


class Pck:
    def __init__(self, path):
        self.path = pathlib.Path(path)
        self.file = self.path.open('rb')
        magic, version, major, minor, patch, flags = struct.unpack('<6I', self.file.read(24))
        if magic != 0x43504447 or version != 3 or flags & 1:
            raise ValueError('Expected unencrypted Godot PCK v3')
        base, directory = struct.unpack('<2Q', self.file.read(16))
        self.file.seek(directory)
        self.entries = {}
        for _ in range(self.u32()):
            name = self.file.read(self.u32()).rstrip(b'\0').decode('utf8')
            offset, size = struct.unpack('<2Q', self.file.read(16))
            md5 = self.file.read(16)
            entry_flags = self.u32()
            self.entries[name] = (base + offset, size, entry_flags, md5)

    def u32(self):
        return struct.unpack('<I', self.file.read(4))[0]

    def read(self, name):
        offset, size, flags, md5 = self.entries[name]
        if flags:
            raise ValueError(f'Unsupported entry flags: {name}: {flags}')
        self.file.seek(offset)
        data = self.file.read(size)
        if len(data) != size or hashlib.md5(data).digest() != md5:
            raise ValueError(f'Integrity check failed: {name}')
        return data


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('pck')
    ap.add_argument('output')
    args = ap.parse_args()
    pack = Pck(args.pck)
    out = pathlib.Path(args.output)
    out.mkdir(parents=True, exist_ok=True)
    provenance = []
    for character in ['ironclad', 'silent', 'regent', 'necrobinder', 'defect', 'osty', 'regent_weapon']:
        skels = [n for n in pack.entries if n.startswith('.godot/imported/' + character + '.skel-')]
        if not skels:
            continue
        target = out / character
        target.mkdir(exist_ok=True)
        for ext in ['skel', 'atlas']:
            name = next(n for n in pack.entries if n.startswith('.godot/imported/' + character + '.' + ext + '-'))
            data = pack.read(name)
            provenance.append({'character': character, 'source': name, 'size': len(data), 'sha256': hashlib.sha256(data).hexdigest()})
            if ext == 'atlas':
                atlas_wrapper = json.loads(data)
                (target / 'atlas_wrapper.json').write_text(json.dumps(atlas_wrapper, ensure_ascii=False, indent=2), encoding='utf8')
                data = atlas_wrapper['atlas_data'].encode('utf8')
            (target / (character + '.' + ext)).write_bytes(data)
        for name in pack.entries:
            if name.startswith('animations/characters/') and (name.endswith('/' + character + '_skel_data.tres') or '/' + character + '/' in name and name.endswith('.png.import')) or name == 'scenes/creature_visuals/' + character + '.tscn':
                (target / pathlib.PurePosixPath(name).name).write_bytes(pack.read(name))
                if name.endswith('.png.import'):
                    text = pack.read(name).decode('utf8')
                    imported = re.search(r'path="res://([^\"]+\.ctex)"', text)
                    if imported:
                        data = pack.read(imported.group(1))
                        (target / (pathlib.PurePosixPath(name).name[:-7] + '.ctex')).write_bytes(data)
                        print(character, pathlib.PurePosixPath(name).name, 'texture header', data[:60].hex())
    (out / 'provenance.json').write_text(json.dumps(provenance, ensure_ascii=False, indent=2), encoding='utf8')
    print('Verified and extracted', len(provenance), 'skeleton/atlas resources')


if __name__ == '__main__':
    main()
