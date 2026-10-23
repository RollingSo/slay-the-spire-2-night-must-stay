#!/usr/bin/env python3
"""animation_forge —— 家人/复仇者立绘动画的打包与闸门。

子命令
  verify  复载一组 .res，按 specs.json 做闸门检查（裁剪、锚点、尺寸、帧率）
  pack    把某个动作的帧序列打成 SpriteFrames，画布按并集包围盒 + 边距

设计约束（v03 起）
  * 画布随动作，不固定尺寸
  * 画布尺寸与锚点写进 SpriteFrames 的 meta：canvas / anchor
  * 接入时 offset = (canvas/2 - anchor)，节点只负责站在地面线
"""
from __future__ import annotations

import argparse
import json
import pathlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
FORGE = pathlib.Path(__file__).resolve().parent
SPECS = json.loads((FORGE / "specs.json").read_text(encoding="utf8"))
PREFIX = {"RV": "revenant", "HE": "helen", "FR": "frederick", "SE": "sebastian"}


def run_godot(script: pathlib.Path, *args: str) -> str:
    cmd = ["godot", "--headless", "--path", str(ROOT / "tools/revenant_animation_probe"),
           "--script", str(script), "--", *map(str, args)]
    proc = subprocess.run(cmd, capture_output=True, text=True)
    if proc.returncode != 0:
        sys.stderr.write(proc.stdout[-2000:] + proc.stderr[-2000:])
        raise SystemExit(f"godot 退出码 {proc.returncode}: {script.name}")
    return proc.stdout


def character_of(item_id: str) -> str | None:
    return PREFIX.get(item_id[:2])


def verify(directory: pathlib.Path, strict: bool = True, family: str | None = None) -> int:
    tmp = ROOT / ".tmp"
    tmp.mkdir(exist_ok=True)
    out = tmp / "forge_verify.json"
    run_godot(FORGE / "verify.gd", directory.resolve(), out.resolve())
    data = json.loads(out.read_text(encoding="utf8"))
    exceptions = SPECS.get("exceptions", {})
    tol_h = SPECS["neutral_height_tolerance"]
    problems = 0

    head = f"{'文件':30s} {'画布':>10s} {'锚点':>12s} {'帧':>4s} {'中立高':>6s} {'基准':>5s} {'裁剪':>4s} {'锚点':>4s}"
    print(head)
    print("-" * len(head))
    for entry in sorted(data, key=lambda e: e["file"]):
        name = entry["file"]
        item = name.split("_")[0]
        char = family if family else character_of(item)
        base = SPECS["characters"][char]["baseline_height"] if char else 0
        exp_neutral = exceptions.get(item, base)
        if not entry["loadable"]:
            print(f"{name:30s} {'加载失败':>10s}")
            problems += 1
            continue
        canvas = entry["canvas"]
        anchor = entry["anchor"]
        for anim, d in entry["animations"].items():
            u = d["union"]
            clip_x = u[0] <= 0 or u[2] >= canvas[0] - 1
            clip_y = u[1] <= 0 or u[3] >= canvas[1] - 1
            anchor_ok = abs(d["neutral_bottom"] - anchor[1]) <= 2
            if strict:
                problems += int(clip_x or clip_y) + int(not anchor_ok)
            neutral = d["neutral_height"]
            flag_n = "" if abs(neutral - exp_neutral) <= tol_h else "!"
            print(f"{name:30s} {canvas[0]:4d}x{canvas[1]:<5d} "
                  f"({anchor[0]:.0f},{anchor[1]:.0f}){'':3s} {d['frames']:4d} "
                  f"{neutral:5d}{flag_n:1s} {exp_neutral:5d} "
                  f"{'⚠' if (clip_x or clip_y) else 'ok':>4s} {'ok' if anchor_ok else '⚠':>4s}")
    print()
    if problems:
        print(f"闸门失败：{problems} 项")
        return 1
    print(f"闸门通过：{len(data)} 个资源")
    return 0


def pack(config: pathlib.Path) -> int:
    cfg = json.loads(config.read_text(encoding="utf8"))
    frames = sorted(pathlib.Path(cfg["frames_dir"]).glob("frame_*.png"))
    if not frames:
        raise SystemExit(f"没有帧: {cfg['frames_dir']}")
    from PIL import Image
    cell = cfg.get("cell")
    if cell is None:
        raise SystemExit("config 需要 cell=[w,h]（并集包围盒 + 边距）")
    W, H = cell
    cols = max(1, cfg.get("max_page", 2048) // W)
    rows = max(1, cfg.get("max_page", 2048) // H)
    per = cols * rows
    work = config.parent
    pages, rects = [], []
    for i in range(0, len(frames), per):
        chunk = frames[i:i + per]
        r = (len(chunk) + cols - 1) // cols
        img = Image.new("RGBA", (cols * W, r * H), (0, 0, 0, 0))
        for k, p in enumerate(chunk):
            img.paste(Image.open(p).convert("RGBA"), ((k % cols) * W, (k // cols) * H))
            rects.append([len(pages), (k % cols) * W, (k // cols) * H])
        page = work / f"atlas_{len(pages)}.png"
        img.save(page)
        pages.append(str(page.resolve()))
    data = {"pages": pages, "cell": [W, H], "anchor": cfg["anchor"], "canvas_scale": 1.0,
            "animations": {cfg["action"]: {"speed": cfg.get("fps", 24),
                                           "loop": cfg.get("loop", False),
                                           "frames": rects}},
            "output": str(pathlib.Path(cfg["output"]).resolve())}
    gen = work / "_pack.json"
    gen.write_text(json.dumps(data), encoding="utf8")
    run_godot(FORGE / "pack.gd", gen.resolve())
    print(f"packed {len(frames)} 帧 → {cfg['output']}（画布 {W}x{H}，{len(pages)} 页）")
    return 0


def main() -> int:
    ap = argparse.ArgumentParser(description="animation_forge")
    sub = ap.add_subparsers(dest="cmd", required=True)
    v = sub.add_parser("verify", help="闸门检查一组 .res")
    v.add_argument("--dir", required=True)
    v.add_argument("--family", default=None, help="运行时目录用：按角色基准校验（helen/frederick/sebastian/revenant）")
    v.add_argument("--no-strict", action="store_true")
    p = sub.add_parser("pack", help="把帧序列打成 SpriteFrames")
    p.add_argument("--config", required=True)
    args = ap.parse_args()
    if args.cmd == "verify":
        return verify(pathlib.Path(args.dir), strict=not args.no_strict, family=args.family)
    return pack(pathlib.Path(args.config))


if __name__ == "__main__":
    raise SystemExit(main())
