# Development reference

## Runtime integration

`ModInitializer.Initialize` injects custom saved-property types, then calls Harmony `PatchAll`. Patches are grouped by concern under `src/Core/Patches`:

- asset and scene selection (`GuardianAssetPatch`, `IroneyeAssetPatch`, `RevenantAssetPatch`),
- model database and pools (`ModelDbCharacterPatch`),
- ancient/event integrations (`GuardianAncientDialoguePatch`, `GuardianTouchOfOrobasPatch`, `GuardianArchaicToothPatch`),
- combat/UI overlays (distance, mark, hidden poison, Guard Counter forecast),
- independent combat rigs, animations, card trails and multiplayer hands.

Prefer a narrow Harmony patch with a type guard over replacing a base-game scene. Character-specific visuals must fail closed rather than silently falling back to another vanilla character.

## State and save compatibility

Permanent card growth and relic counters must use `[SavedProperty]` and be registered with `SavedPropertiesTypeCache.InjectTypeIntoCache` before model/save caches initialize. Act transitions recreate rooms and visuals; never retain stale Godot nodes or room-local references across that boundary. Rehydrate custom powers and dynamic indicators from the authoritative combat state.

## Dynamic card text

Calculated variables should be shared by execution and preview. If a value depends on distance, movement, Mark, Hidden Poison, Guard Counter or a card count, expose one calculated variable and use it in both the card model and localization. Avoid custom preview logic that diverges from actual damage/block resolution.

## Export pipeline

`tools/export_guardian_mod.ps1` is the canonical pipeline. It runs:

1. `validate_guardian_card_localization.ps1`;
2. `sync_guardian_power_icons.ps1`;
3. Godot headless import;
4. Godot PCK export;
5. Release .NET build;
6. runtime-file copy and SHA-256 verification.

The project intentionally leaves game launch and in-game verification to the user.
