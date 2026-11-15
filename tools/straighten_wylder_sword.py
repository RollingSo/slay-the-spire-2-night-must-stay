"""User-authorized local blade alignment; never overwrites the source sprite."""
from pathlib import Path
import argparse
import math
import json
import hashlib

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
PREVIEW = ROOT / 'design/角色资源预览/wylder'
SOURCE = PREVIEW / 'wylder_pursuit_attack_v9.png'

# Landmarks are measured on the unchanged 1509 x 1042 v9 source.
# The decorative fork tips are NOT the pommel/handle centerline.
HANDLE_REAR = np.array((632.0, 387.0))
GUARD_CENTER = np.array((840.0, 504.0))
BLADE_TIP = np.array((1497.0, 956.0))
BLADE_POLYGON = [
    (861, 480), (902, 506), (1000, 565), (1100, 634),
    (1200, 700), (1300, 770), (1400, 840), (1508, 930),
    (1508, 975), (1480, 965), (1400, 915), (1300, 852),
    (1200, 787), (1100, 719), (1000, 651), (900, 584),
    (850, 551), (816, 551), (823, 541), (838, 518), (850, 498),
]
GUARD_POLYGON = [
    (874, 435), (883, 450), (884, 458), (879, 468), (868, 483),
    (859, 497), (846, 518), (831, 539), (817, 560), (807, 564),
    (791, 560), (784, 553), (786, 542), (802, 521), (819, 496),
    (838, 464), (853, 446), (864, 437),
]


def straighten():
    source_hash = hashlib.sha256(SOURCE.read_bytes()).hexdigest()
    source = Image.open(SOURCE).convert('RGBA')
    assert source.size == (1509, 1042), 'Landmarks apply only to the original v9 source'
    source_data = np.array(source)
    cut = Image.new('L', source.size, 0)
    ImageDraw.Draw(cut).polygon(BLADE_POLYGON, fill=255)
    guard_mask = Image.new('L', source.size, 0)
    ImageDraw.Draw(guard_mask).polygon(GUARD_POLYGON, fill=255)
    cut_array = np.array(cut)
    cut_array[np.array(guard_mask) > 0] = 0
    cut = Image.fromarray(cut_array)
    mask = np.array(cut) > 0
    blade_data = source_data.copy()
    blade_data[~mask] = 0
    base_data = source_data.copy()
    base_data[mask] = 0

    handle_vector = GUARD_CENTER - HANDLE_REAR
    old_vector = BLADE_TIP - GUARD_CENTER
    angle = math.atan2(handle_vector[1], handle_vector[0]) - math.atan2(old_vector[1], old_vector[0])
    c, s = math.cos(angle), math.sin(angle)
    rotation = np.array(((c, -s), (s, c)))
    new_tip = GUARD_CENTER + rotation @ old_vector
    output_size = (max(source.width, math.ceil(new_tip[0]) + 20), source.height)

    # Pillow takes output->input affine coefficients. Transform premultiplied
    # RGBA so interpolation never pulls black RGB out of transparent pixels.
    inverse = rotation.T
    offset = GUARD_CENTER - inverse @ GUARD_CENTER
    affine = (*inverse[0], offset[0], *inverse[1], offset[1])
    blade = Image.fromarray(blade_data).convert('RGBa').transform(
        output_size, Image.Transform.AFFINE, affine,
        resample=Image.Resampling.BICUBIC,
    ).convert('RGBA')
    output = Image.new('RGBA', output_size, (0, 0, 0, 0))
    output.paste(Image.fromarray(base_data), (0, 0))
    # The blade was flattened against a curved guard in the source. Extend a
    # short root-only copy underneath that foreground, rather than leaving a
    # triangular empty seam when the blade pivots. No full-blade warp/scale.
    unit = handle_vector / np.linalg.norm(handle_vector)
    shift = 9.0 * unit
    root = blade.convert('RGBa').transform(
        output_size, Image.Transform.AFFINE,
        (1, 0, shift[0], 0, 1, shift[1]),
        resample=Image.Resampling.BICUBIC,
    ).convert('RGBA')
    root_data = np.array(root)
    yy, xx = np.indices((output.height, output.width))
    t = (xx - GUARD_CENTER[0]) * unit[0] + (yy - GUARD_CENTER[1]) * unit[1]
    root_data[(t < -12) | (t > 35)] = 0
    output = Image.alpha_composite(output, Image.fromarray(root_data))
    output = Image.alpha_composite(output, blade)
    # Protect solid foreground and the traced guard, including its original
    # edge alpha. Do not restore faint old matte over the moved blade.
    output_data = np.array(output)
    occupied_character = ((~mask) & (source_data[:, :, 3] >= 128)) | ((np.array(guard_mask) > 0) & (source_data[:, :, 3] > 0))
    output_data[:source.height, :source.width][occupied_character] = source_data[occupied_character]
    output = Image.fromarray(output_data)
    destination = PREVIEW / 'wylder_pursuit_attack_v12_geometry.png'
    output.save(destination)

    # Separate diagnostic only; the deliverable above has no guide marks.
    qa = output.copy()
    draw = ImageDraw.Draw(qa)
    end = GUARD_CENTER + (new_tip - GUARD_CENTER) * 1.015
    start = HANDLE_REAR - handle_vector * 0.2
    draw.line((*start, *end), fill=(255, 60, 60, 220), width=2)
    for point in (HANDLE_REAR, GUARD_CENTER, new_tip):
        x, y = point
        draw.ellipse((x - 4, y - 4, x + 4, y + 4), outline=(50, 255, 255, 255), width=1)
    qa.save(PREVIEW / 'wylder_pursuit_attack_v12_axis_qa.png')
    output.crop((760, 440, 1000, 650)).resize((960, 840)).save(PREVIEW / 'wylder_v12_joint_qa.png')
    source.crop((760, 440, 1000, 650)).resize((960, 840)).save(PREVIEW / 'wylder_v9_joint_qa.png')
    # Rotation may legitimately cover a few formerly transparent pixels, so
    # test character RGB/alpha separately from the unoccupied canvas.
    unchanged_character = np.array_equal(
        np.array(output)[:source.height, :source.width][occupied_character],
        source_data[occupied_character],
    )
    new_vector = new_tip - GUARD_CENTER
    cross = float(handle_vector[0] * new_vector[1] - handle_vector[1] * new_vector[0])
    assert abs(cross) < 1e-7, 'Measured target landmarks are not collinear'
    assert unchanged_character, 'Protected foreground changed'
    assert math.isclose(np.linalg.norm(old_vector), np.linalg.norm(new_vector), abs_tol=1e-9)
    assert hashlib.sha256(SOURCE.read_bytes()).hexdigest() == source_hash, 'Original changed'
    report = {
        'source_sha256': source_hash,
        'source': str(SOURCE.relative_to(ROOT)),
        'output': str(destination.relative_to(ROOT)),
        'handle_rear': HANDLE_REAR.tolist(), 'guard_center': GUARD_CENTER.tolist(),
        'old_tip': BLADE_TIP.tolist(), 'new_tip': new_tip.tolist(),
        'rotation_degrees_screen_coordinates': math.degrees(angle),
        'blade_length_before': float(np.linalg.norm(old_vector)),
        'blade_length_after': float(np.linalg.norm(new_tip - GUARD_CENTER)),
        'axis_cross_product': cross,
        'source_size': list(source.size), 'output_size': list(output.size),
        'unchanged_protected_foreground_pixels': unchanged_character,
        'landmarks_are_manually_measured': True,
        'root_overlap_extension_pixels': 9,
        'corners_alpha': [output.getpixel(p)[3] for p in ((0, 0), (output.width - 1, 0), (0, output.height - 1), (output.width - 1, output.height - 1))],
        'alpha_cleanup': False, 'generated_image_call': False,
        'status': 'preview_pending_user_approval',
    }
    (PREVIEW / 'wylder_pursuit_attack_v12_geometry.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(report, ensure_ascii=False, indent=2))


def inspect():
    im = Image.open(SOURCE).convert('RGBA')
    draw = ImageDraw.Draw(im)
    font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 14)
    for x in range(500, 1001, 25):
        draw.line((x, 280, x, 650), fill=(255, 255, 0, 90))
        draw.text((x + 1, 280), str(x), font=font, fill='yellow')
    for y in range(300, 651, 25):
        draw.line((500, y, 1000, y), fill=(255, 255, 0, 90))
        draw.text((500, y), str(y), font=font, fill='yellow')
    im.crop((500, 280, 1000, 650)).resize((1500, 1110)).save(PREVIEW / 'wylder_sword_grid_qa.png')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--inspect', action='store_true')
    args = parser.parse_args()
    if args.inspect:
        inspect()
    else:
        straighten()
