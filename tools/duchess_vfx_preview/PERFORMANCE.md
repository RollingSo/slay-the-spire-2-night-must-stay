# Duchess VFX lossless optimization

Run `dotnet build tools/duchess_vfx_preview/DuchessVfxPreview.csproj`, then launch Godot with
`--path tools/duchess_vfx_preview -- --equivalence` using a real rendering backend.

The Baseline classes preserve the pre-optimization implementation (renamed for side-by-side testing).
The test compares RGBA bytes at six fixed progress values for all 15 attack kinds and ReverseTime:
96 samples, exact pixel equality required. It also measures managed allocations inside `_Draw`.
2026-10-16, Godot 4.5.1 / OpenGL compatibility: baseline 1,570,128 bytes; optimized 34,424 bytes.
These are draw-callback allocation totals, not frame-time or whole-game FPS measurements.
Natural-playback preview also reported `DUCHESS_VFX_LIFECYCLE remaining=0`.

Changes: per-instance triangle/quad buffers, reuse adjacent Band vertices, cache Stroke outline
arrays by length, reuse Duchess clock and reverse-time ribbon arrays. No change to sample counts,
draw order, triangles, timing, colors, shaders, texture resolution or audio. Scratch polygon buffers
are safe because Godot copies submitted polygon data into rendering commands synchronously.

## Reverse Time and Midnight Waltz, 2026-10-24

Build this preview project (set `Sts2AssemblyDir` to the installed game's assembly
directory if it differs from the project's default), then launch Godot with
`--path tools/duchess_vfx_preview -- --time-waltz-equivalence`.

`October*` baseline classes preserve the production code immediately before this
change. `TimeWaltzEquivalence` links the actual optimized production classes and
compares exact RGBA bytes for Reverse Time and both Midnight Waltz phases at 12
progress values, including either side of the rewind cue. The test covers
1440×960, 1920×1080 and 960×720, plus five overlapping AOE impacts at three
progress values per resolution: **117 samples per backend, all pixel-identical**
on both OpenGL compatibility and Vulkan Forward Mobile, Godot 4.5.1 / RTX
4070 Ti. The existing 96-sample attack suite
also passes unchanged.

Optimization scope:

- Reverse Time: submit each ribbon's unchanged triangles in one indexed array
  instead of triangulating/submitting each triangle separately. Five arms ×
  three layers drop from up to 1,920 submissions to 15; the two return trails
  drop from up to 112 to two. Preserve triangle rejection, order, alpha overlap,
  all 65/29 samples, sizes, colors and timing. Reuse the return-curve buffers.
- Midnight Waltz: cache fixed arc directions and reuse vertex buffers; cache
  clock ticks, slash and spark directions. Combine eleven disjoint, same-style
  clock ticks into one multiline submission. Share immutable arc tables across
  AOE instances and allocate only the phase's needed vertex buffers.
  Preserve arc sample counts, floating
  point operation order and antialiasing. Native Grand Finale prelude/impact
  nodes, audio, anticipation duration and damage synchronization are untouched.

Isolated `_Draw` benchmark (OpenGL), median of three rounds of 16 warmups then
240 callbacks; queued commands are cleared between callbacks so only the final
geometry reaches the screen:

| Overlay | Before CPU ms | After CPU ms | Before allocated bytes | After allocated bytes |
| --- | ---: | ---: | ---: | ---: |
| Reverse Time (78%) | 1292.838 | 51.473 | 122880 | 0 |
| Waltz prelude (42%) | 8.598 | 7.671 | 0 | 0 |
| Waltz impact (42%) | 16.420 | 15.977 | 0 | 0 |

These are draw-submission CPU totals, **not whole-game frame times or FPS**;
Midnight Waltz's unmodified native effects are not included. The impact-only
timing difference is small and should not be presented as a substantial speedup.
No detail reduction, playback acceleration, texture downscaling or effect removal.

Natural playback additionally verifies Reverse Time and its parent CanvasLayer,
Midnight Waltz prelude and impact all free themselves: `remaining=0`. The full
mod build passes with zero warnings/errors. Real combat (including multi-target
Waltz and the reported card-in-deck stutter) still needs an in-game smoke test;
the isolated renderer cannot establish a whole-game FPS improvement.
