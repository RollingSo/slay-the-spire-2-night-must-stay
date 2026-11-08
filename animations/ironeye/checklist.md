# Ironeye Animation Checklist

**Preview: `animations/ironeye/preview/`（仅供预览，不用于 Mod 接入或制作）。**

Layout: `animations/ironeye/main/`. No derivative animation set is present, so `derived/` is intentionally absent. Built mod files should later go under `animations/ironeye/mod/`.

## Main animations

| Action | File | Animation | Frames | FPS | Loop | Status / note |
|---|---|---|---:|---:|---|---|
| idle | `main/ironeye_sequences.res` | `idle` | 64 | 24 | yes | Existing combined combat sequence. |
| attack | `main/ironeye_sequences.res` | `attack` | 24 | 24 | no | Existing combined combat sequence. |
| cast | `main/ironeye_sequences.res` | `cast` | 24 | 24 | no | Existing combined combat sequence. |
| dagger | `main/ironeye_sequences.res` | `dagger` | 18 | 24 | no | Existing combined combat sequence. |
| hurt | `main/ironeye_sequences.res` | `hurt` | 12 | 24 | no | Existing combined combat sequence. |
| merchant_idle | `main/merchant_idle/ironeye_merchant_idle.tres` | `relaxed_loop` | 120 | 60 | yes | Existing merchant sequence; 8 atlas pages in the same folder. |

The combat sequence resource does not carry `canvas`/`anchor` metadata; placement is handled by the Ironeye combat rig. The merchant `.tres` is kept as `.tres` because it is an existing engine sequence with external atlas pages; all atlas paths were rewritten to the new folder.

## Gaps / follow-up

- No separate death or camp idle animation is present in this repository snapshot; confirm whether the combat rig uses a fallback before claiming full action coverage.
- The combined `ironeye_sequences.res` should be the canonical source until the mod integration splits or re-exports it.
- No `derived/` folder because Ironeye has no family/companion animation set.