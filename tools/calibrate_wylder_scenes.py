"""Mechanical runtime calibration for the normalized Wylder asset canvases."""
from pathlib import Path
import re
ROOT=Path(__file__).resolve().parents[1]
p=ROOT/'wylder_assets/combat_rig/wylder_combat_visuals.tscn'
t=p.read_text().replace('position = Vector2(0, -7.831054963)','position = Vector2(0, 0)').replace('Vector2(0.711914088, 0.711914088)','Vector2(1, 1)').replace('offset_top = -260.0','offset_top = -300.0')
p.write_text(t,encoding='utf-8')
p=ROOT/'wylder_assets/combat_rig/wylder_combat_rig.tscn';t=p.read_text()
t=t.replace('Vector2(0.36, 0.36)','Vector2(0.22, 0.22)')
# Original y=-205 -> normalized foot anchor y=-154. Apply the same translation
# to the authored animation keys; the .calibrated marker makes this idempotent.
if '; Wylder normalized foot anchor' not in t:
    t=re.sub(r'Vector2\(([-\d.]+), ([-\d.]+)\)',lambda m:f'Vector2({m[1]}, {float(m[2])+51:g})' if float(m[2]) < -100 else m[0],t)
    t='; Wylder normalized foot anchor\n'+t
p.write_text(t,encoding='utf-8')
for name in ['rest_site','merchant']:
    p=ROOT/f'wylder_assets/{name}/wylder_{name}.tscn';t=p.read_text()
    t=t.replace('-263.307531375','-269.008').replace('-267.307531375','-273.008')
    t=t.replace('-263.320249500','-269.021').replace('-267.320249500','-273.021')
    p.write_text(t,encoding='utf-8')
p=ROOT/'wylder_assets/char_select_bg_wylder.tscn';t=p.read_text().replace('Vector2(0.595, 0.56)','Vector2(0.65, 0.52)').replace('Vector2(0.77, 0.68)','Vector2(0.72, 0.7)');p.write_text(t,encoding='utf-8')
print('Calibrated normalized combat height 299.2 px and foot baseline 0.')
