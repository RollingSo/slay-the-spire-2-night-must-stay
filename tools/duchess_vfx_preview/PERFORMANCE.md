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
