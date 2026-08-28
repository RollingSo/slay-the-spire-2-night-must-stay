# Slay the Spire 2 : Night Must Stay

`Night Must Stay` is a Godot/.NET mod for *Slay the Spire 2*. It adds three custom characters—Guardian, Ironeye, and Revenant—with independent card pools, relics, potions, powers, combat visuals, character-select scenes, localization, and multiplayer assets.

The project is a mod source tree, not a copy of the base game. The game assemblies and base assets are supplied by a local Slay the Spire 2 installation and are intentionally not committed here.

## Quick start

Requirements:

- Slay the Spire 2 installed locally, with `sts2.dll`, `GodotSharp.dll`, and `0Harmony.dll` available under `sts2dll/`.
- Godot 4.5.1 Mono (the project is pinned to `Godot.NET.Sdk/4.5.1`).
- .NET 9 SDK.
- PowerShell on Windows.

`NuGet.Config` intentionally contains only the public NuGet source. The Godot
Mono package source is machine-specific; configure it locally (or restore the
Godot package source from your installed Godot SDK) rather than committing an
absolute Windows path or a user profile path.

Build and install the complete mod from the project root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\export_guardian_mod.ps1
```

The export script validates localization, synchronizes Guardian power icons, imports Godot assets, exports `build/NightMustStay.pck`, builds the Release DLL/PDB/runtime files, copies them to the configured game Mods directory, and verifies SHA-256 hashes. It does not launch the game.

For a build without installing to Mods:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\export_guardian_mod.ps1 -SkipInstall
```

The default Godot and Mods paths are declared at the top of `tools/export_guardian_mod.ps1`; pass `-GodotPath` and `-ModsDirectory` for another installation.

## Project map

| Path | Responsibility |
| --- | --- |
| `src/Core/Models/Characters` | Character models, starter decks, pools and character identity. |
| `src/Core/Models/Cards` | Card behavior and card-specific dynamic variables. |
| `src/Core/Models/Power` | Persistent and combat powers/keywords. |
| `src/Core/Models/Relics`, `Potions` | Relic and potion behavior. |
| `src/Core/Models/CardPools`, `RelicPools`, `PotionPools` | Character-specific acquisition pools. |
| `src/Core/Patches` | Harmony integration with base-game scenes, events, UI and animation hooks. |
| `src/Core/Nodes` | Godot UI/VFX nodes such as distance, mark and forecast overlays. |
| `guardian_assets`, `ironeye_assets`, `revenant_assets` | Character-owned art, rigs, icons, scenes and transition assets. |
| `NightMustStay/localization/{zhs,eng}` | Mod localization tables. |
| `images/atlases`, `materials`, `atlases`, `powers`, `packed` | Godot atlas/material resources and packaged card/power assets. |
| `tools` | Export, icon-sync and localization validation scripts. |
| `design` | Art specifications, character bibles, card table snapshot and approved previews. |

## Development rules

The authoritative card list is the Feishu card table referenced by `AGENTS.md`. Do not invent a parallel card list or modify that table unless explicitly authorized. Before creating or revising card art, read the mandatory art specifications in `design/` and use the original portraits under `D:\STS2\images\packed\card_portraits` for style comparison.

Guardian power icons are atlas-backed. `guardian_assets/guardian_power_atlas.png` and `images/atlases/power_atlas.sprites` are the source of truth; run `tools/sync_guardian_power_icons.ps1` whenever a Guardian power region changes. The normal export script already runs this step.

Every new character must register a Sea Glass title in both localization tables before packaging:

```text
SEA_GLASS.<CHARACTER_MODEL_ID>.title
```

The key must match the character's actual model ID. This prevents Orobas/Sea Glass event options from throwing a missing-key `LocException` when a run enters a later act. Validate both JSON files before export.

## Testing and diagnostics

Do not start the game as part of unattended build verification. Use `dotnet build NightMustStay.csproj -c Release --no-restore` for a fast compile check and inspect the latest game log under `%APPDATA%\\SlayTheSpire2\\logs` when a user reports an in-game failure. Distinguish base-game asset-cache warnings from mod exceptions; include the first exception and its stack trace in bug reports.

## Versioning

The runtime mod ID and assembly name are `NightMustStay`; the public project name and metadata are `Slay the Spire 2 : Night Must Stay`. Release archives should contain the runtime files produced by the export script and use an incremented version directory under `releases/` (which is ignored locally so generated packages do not pollute source history).

## License and assets

The project code is maintained for collaborative mod development. Slay the Spire 2 and Elden Ring: Nightreign names, base-game assemblies, and any source/reference artwork remain the property of their respective owners. Contributors must not add redistributed base-game files or unlicensed third-party assets.
