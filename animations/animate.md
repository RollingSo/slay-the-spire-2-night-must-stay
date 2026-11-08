# Animation Assets Layout

This directory is the single source of truth for character animation assets that will be packaged into the mod. All file and folder names under `animations/` must be English. The repository keeps production resources, checklists, manifests, per-character preview folders, and later the built mod files. Preview content is for human review only and is never used by the mod. Extracted frame folders and temporary build caches are still not committed.

## Directory tree

```text
animations/
├── animate.md
├── ironeye/
│   ├── main/
│   │   ├── ironeye_sequences.res
│   │   └── merchant_idle/
│   │       ├── ironeye_merchant_idle.tres
│   │       └── atlas_*.png
│   ├── preview/
│   ├── manifest.json
│   └── checklist.md
├── revenant/
│   ├── main/
│   │   ├── combat_idle.res
│   │   ├── claw_attack.res
│   │   ├── lyre_command.res
│   │   ├── prayer_attack.res
│   │   ├── hurt.res
│   │   ├── death.res
│   │   ├── merchant_idle.res
│   │   └── camp_idle.res
│   ├── derived/
│   │   ├── helen/{idle,attack_a,attack_b,hurt}.res
│   │   ├── frederick/{idle,attack_a,hurt}.res
│   │   └── sebastian/{idle,attack_a,attack_b,hurt}.res
│   ├── preview/
│   │   ├── main/
│   │   └── derived/{helen,frederick,sebastian}/
│   ├── manifest.json
│   └── checklist.md
└── duchess/
    ├── main/
    │   ├── combat_idle.res
    │   ├── dagger_slash.res
    │   ├── dodge_defend.res
    │   ├── cast.res
    │   ├── hurt.res
    │   ├── death.res
    │   ├── merchant_idle.res
    │   ├── camp_idle.res
    │   ├── time_command.res
    │   └── phantom_dial.res
    ├── manifest.json
    └── checklist.md
```

## Roles of each folder

- `main/`: the character's own animation resources that can be used directly by the mod. Prefer `.res`; keep `.tres` only when an existing engine sequence cannot be packed into `.res` without changing behavior.
- `derived/`: animations owned by a character but performed by another actor or layer. For Revenant this is the family set: Helen, Frederick, and Sebastian. Omit this directory for characters that have no derivative animations.
- `checklist.md`: the production checklist for this character. It records action IDs, intended gameplay use, target duration, loop flag, current status, source/adoption notes, and the mapping from action to resource file.
- `preview/`: human-review previews only (GIF, MP4, HTML, keyframes). This folder is **not** mod input and is never packaged. Preview files may be historical and may not match the current `.res` exactly; always use `main/`/`derived/` for production. File names are English.
- `manifest.json`: machine-readable mapping of action → resource → frame count → duration → canvas/anchor → integration scale. Each character has one; keep it in sync with `checklist.md`.
- `mod/`: reserved per character for the built mod files (`.pck`, exported resources, or patch files). It is created when the mod is actually built and is not part of the animation source layout.

## Asset rules

- Resources are authored at the character's real in-game final size. Do not use an arbitrary 1x/2x guess in the file name or metadata.
- For Duchess combat, the texture visible height is 432 and the parent `Visuals` scale is `0.711914088`, giving the final 307.546886. Shop and camp use their scene sizes at scale 1.0.
- For Revenant and IronEye, follow each rig's own parent scale and the per-action canvas/anchor in the checklist; do not copy Duchess scale values to them.
- Each action has one canonical resource. Do not keep duplicate `design/` copies, source videos, preview GIFs, or extracted frames in the repository once the canonical resource is in place.
- When a mod is built, put the generated mod files under `animations/<character>/mod/` and keep the source `.res` files under `main/` or `derived/`.
- The checklist is the human-readable source of truth for action intent; the manifest is the machine-readable asset map. If they disagree, fix both in the same change.

## Runtime copies

Existing runtime resources under `duchess_assets/`, `revenant_assets/`, and `ironeye_assets/` are kept for the current game build until the mod integration switches its load paths. They are not the delivery copies; the canonical mod assets are the files under `animations/<character>/main/` and `animations/revenant/derived/`.
