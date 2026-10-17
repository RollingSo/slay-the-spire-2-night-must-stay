# Revenant animation material handoff

User-approved source clips for RV01 combat idle and RV02 claw attack (a spell gesture). Candidates only: production scenes and gameplay are unchanged.

## Material locations

- This directory: both original source videos, their prompts, the shared approved `reference.png`, light/dark/checker alpha reviews, `preview.html`, build and validation tools.
- `revenant_assets/combat/sequence_candidates/`: portable transparent PNG atlases, Godot SpriteFrames `.tres` resources, extraction manifest and the original master-frame QA results.
- Local `.tmp/revenant-extracted/`: model masks, full-resolution RGBA frame ZIPs and animated WebP previews, recreated by `build.py`. These large intermediates are intentionally excluded from Git. Previously delivered local previews also remain under `design/动画预览/revenant/`.

## Use

Open the candidate `.tres` resources in Godot or use their 416×560 atlas regions. Both actions share the same canvas and scale; do not normalize individual frames. The resources do not replace the current rig. Calibrate the final visible height and ground contact before integration.

From repository root, serve the repository with `python3 -m http.server 8767 --bind 127.0.0.1`, then open `http://127.0.0.1:8767/tools/revenant_animation_extract/preview.html`.

To regenerate: install FFmpeg and the Python dependencies in `requirements.txt`, then run `python tools/revenant_animation_extract/build.py --samples` for alpha samples, followed by `python tools/revenant_animation_extract/build.py`. The first run downloads the rembg isnet-general-use model. Validate the delivered package with `python tools/revenant_animation_extract/check.py`. Segmentation output can vary across dependency/platform versions; committed atlases are the handoff baseline.

## Timing and limits

- Idle: 96 frames at 24 FPS, 4 seconds. Uses cosine-eased forward/reverse sampling of source 2.25–4.333 seconds. No crossfade double silhouettes; cloth/hair reversal is a deliberate preview compromise.
- Claw/spell gesture: 110 frames at 24 FPS, 4.583 seconds, source 0–4.583 seconds. Full approved gesture retained. The preview proposes 1-second normal / 0.5-second fast playback, with source frame 15 (0.625s downward hand motion) mapped to the default 0.15s normal attack wait. This cue is proposed, not a user-confirmed damage event.
- Model masks every three frames, with bidirectional optical-flow propagation between them; pale dress interior protected. Fine hair, strings and residual pale edges require game-background review.
- Original master QA checked 206 RGBA frames, fixed canvas and zero-alpha corners. Portable package validation additionally checks source hashes, all atlas frame regions and references. Browser checks covered stepping, playback modes, replay and return to idle.
- No Godot import/export, production installation or real combat validation performed. No claim of final VFX/damage synchronization. Atlas uncompressed RGBA total is about 142 MiB before runtime import/compression; final memory optimization remains.

## Opacity repair v2

The initial delivery left arm/hand/sleeve interiors translucent. The extractor now restores source-color-supported solid interiors after every frame's flow propagation, keeps a one-pixel border for antialiasing, and recomputes colors from the original RGB at the repaired alpha. Versioned v2 key-mask caches avoid silently retaining stale mattes. `opacity_probes.json` contains independently selected solid arm/hand/sleeve points and real background openings across idle, extension and raised-arm poses; `check.py` requires opaque interiors and transparent openings. The corrected atlas images supersede the original commit's extraction.

## Approved skirt color repair v3

User approved the local preview on 2026-10-17. `clothing_color.py` recolors only the generated warm skirt patch, preserving source luminance and matching the surrounding cool grey-white fabric. Component selection excludes detached hands and stops above bare lower legs. Original videos are unchanged. See `爪击攻击_衣料修色对比.jpg` (left original, right corrected). The atlas images include both opacity v2 and color v3 repairs; engine/game validation remains pending.

## Approved enclosed garment-hole repair v4

User approved the updated local preview on 2026-10-17. Fully transparent holes erased inside the sleeve/shoulder are now recovered inside the protected garment region; the lyre aperture remains excluded. A dense 165-pixel sleeve patch regression supplements the earlier arm/hand point probes, which missed these holes. See `手臂遮罩修正对比.jpg` (pink indicates background; left previous, right repaired).
