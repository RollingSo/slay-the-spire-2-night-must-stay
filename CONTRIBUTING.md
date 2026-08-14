# Contributing to Night Must Stay

## Before you begin

Read `AGENTS.md`, then the relevant character and art specifications in `design/`. Keep changes focused: gameplay code, localization, art resources, and export tooling should be independently reviewable.

## Adding a character

1. Add the `CharacterModel` and register it in `ModelDbCharacterPatch`.
2. Add the starter deck, card pool, relic pool, potion pool, energy color and character-specific assets.
3. Add character localization in `sts2mod/localization/zhs/characters.json` and `eng/characters.json`.
4. Add `SEA_GLASS.<MODEL_ID>.title` in both `relics.json` files. Use the exact model ID; this is required for Orobas/Sea Glass events.
5. Add the character to any relevant card-library filters and multiplayer/ancient-event hooks.
6. Add preload entries for every scene, texture, atlas resource and icon used by the character.
7. Build, validate JSON, run the export script, and check the installed hashes before asking for in-game verification.

## Adding a card

- Add one `CardModel` with a stable ID and the correct pool/rarity/type.
- Keep behavior, upgrade behavior, dynamic variables, hover tips and localization in sync.
- Use existing base-game implementations as references for targeting, card transformation, retain/exhaust, dynamic previews and event timing.
- Add the portrait atlas resource and the official 1000×760 landscape artwork. Do not put card frames or text inside the portrait.
- Run the localization validator and check that every glossary keyword is highlighted consistently.

## Localization

Card and power text must use the existing keyword colors and hover-tip conventions. Dynamic values belong in the card model's calculated variables, not hard-coded strings. When a model ID changes, update every locale key and any event/relic key derived from that ID.

## Art and assets

Use hard-edged cel shading, flat graphic shapes, limited palettes and a single readable visual event. Keep generated candidates in `design/卡图预览` until approved. Avoid committing Godot import products, build output, logs or local caches; `.gitignore` covers those paths.

## Review checklist

- [ ] Code compiles with zero errors and warnings.
- [ ] Save/load and act-transition state is safe; `[SavedProperty]` types are injected when needed.
- [ ] Chinese and English localization JSON parses successfully.
- [ ] New character has a Sea Glass title in both locales.
- [ ] New assets are preloaded and use independent character visuals.
- [ ] Guardian atlas icons were synchronized if touched.
- [ ] Export/install hashes match.
- [ ] No game launch or unrelated generated files are included in the change.
