# Wylder pursuit energy UI

The test project links the production `WylderPursuitEnergyVisual.cs` directly.
Only game state and the texture cache are stubbed; the Godot controls, lifecycle
and drawing are real. It checks inactive construction, entering and leaving
pursuit, texture replacement and restoration, no external decorations,
complete extracted water above the background and below the native label,
independent counters, mouse pass-through, unchanged bounds, active recreation
and parent cleanup. A graphical run writes `preview.png` using all five actual
energy-counter layers.

Run from the repository root:

```powershell
dotnet build tools/wylder_energy_ui_tests/PursuitUiTests.csproj
& $godot --headless --path "$PWD/tools/wylder_energy_ui_tests"
```

Omit `--headless` to save the rendered comparison preview.
