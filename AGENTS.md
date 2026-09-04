# Project instructions

## Card data

- Treat the Feishu card table at `https://my.feishu.cn/wiki/FnXnwkfWUiKQ0SkkvosctRpCnHg` as the only authoritative card list.
- Read the table when needed, but do not modify it unless the user explicitly authorizes the modification.

## Card text and mechanics

- Before adding or revising card rules text, keywords, generated-card previews, or upgrade descriptions, read `design/卡牌文本与机制实现强制规范.md` completely.
- Run `tools/validate_card_text_format.ps1` after card localization changes. Normal exports run this check automatically.

## Character writing

- Before adding or revising dialogue, event speech, remembrance narration, character flavor text, or character-voiced localization for any Nightfarer, read `design/Nightreign_十角色文本强制规范.md` completely.
- Keep normal dialogue, combat barks, remembrance journals, quoted speech, alternate personas, and corrupted states distinct; do not infer a character's baseline voice from an exceptional state.
- Use the source index in `design/Nightreign_十角色文本语料索引.md` to recheck character relationships, chapter timing, and official Simplified Chinese naming before finalizing text.

## Card artwork

- Before generating or revising any card artwork, read `design/Slay_the_Spire_2_卡图生成强制规范.md` completely.
- Before auditing or batch-redrawing Guardian, Ironeye, or Revenant card artwork, also read `design/NightMustStay_三角色卡图审查与重绘强制规范.md` completely and apply its six-stage audit in order.
- For Guardian identity, equipment, poses, or mechanics, also read `design/Guardian_角色设计与动作强制规范.md` completely.
- Base visual-style judgments on the original portraits under `D:\STS2\images\packed\card_portraits`.
- Do not use the deprecated prompt in `design/Slay_the_Spire_2_卡图美术风格报告.md`.
- Card artwork must use the official-style 1000×760 landscape format, graphic flat color blocks, hard-edged cel shading, bold black shapes, limited palette, exaggerated perspective, and a single readable visual event.
- Reject photorealistic, painterly concept-art, cinematic, highly textured, or realistic-metal results before presenting previews to the user.
- Save approval candidates under `design/卡图预览` until the user approves them.

## Character combat visuals

- Before revising Guardian, Ironeye, or Revenant combat model scale, ground alignment, creature bounds, health-bar width, or combat marker positions, read `design/角色战斗体型与血条强制规范.md` completely.
- Character size must be compared by the final rendered non-transparent visual bounds after every nested scale, not by source-canvas dimensions or the `Bounds` control.

## Character-select backgrounds

- Before replacing or recomposing a character-select background, read `design/角色选择背景构图与裁切强制规范.md` completely.
- Validate the final in-game 16:9 crop, not only the source PNG. A centered `2560×1200` background loses about `12.5%` from each horizontal edge in the standard viewport; essential character features must remain inside the central safe region.

## Revenant necros

- Before revising Revenant Necro health, damage scaling, actions, summon limits, or battlefield layout, read `design/Revenant_死灵机制规范.md` completely.
- Keep the meanings of the Necro fixed HP value, HP ratio, and damage ratio distinct; changing one must not silently change the others.

## Revenant family artwork

- Before generating or revising Helen, Frederick, or Sebastian combat sprites, summon-choice artwork, character-select silhouettes, or family card artwork, read `design/Revenant_家人视觉规范.md` completely.
- Scale family combat visuals by their visible non-transparent bounds relative to Osty: Helen `0.8×`, Frederick `1.0×`, Sebastian `1.2×`; do not infer size from canvas dimensions alone.
- Helen's hood must completely hide her face, Frederick must wield a pumpkin-shaped hammer, and Sebastian must be an unclothed half-body skeleton with no full legs.

## Power artwork

- Before generating or revising any power/status/action icon, read `design/能力图标生成强制规范.md` completely.
- Every power icon must use a true-alpha 256×256 PNG. Reject solid-color square backgrounds, color-key transparency, dirty translucent corners, and shared placeholder artwork; verify all four corner pixels have alpha 0 before import.
- Treat `guardian_assets/guardian_power_atlas.png` and the regions in `images/atlases/power_atlas.sprites` as the single source of truth for Guardian power icons.
- Whenever a Guardian power icon or atlas region changes, run `tools/sync_guardian_power_icons.ps1` before Godot import/export.
- The sync must overwrite every matching `images/powers/*.png` and `powers/*.png` file at 256×256 so the compact power icon and the applied/triggered power flash always show the same artwork.
- Use `tools/export_guardian_mod.ps1` for normal exports; it runs the icon sync before Godot import, PCK export, Release build, and installation.
