# Project instructions

## Card data

- Treat the Feishu card table at `https://my.feishu.cn/wiki/FnXnwkfWUiKQ0SkkvosctRpCnHg` as the only authoritative card list.
- Read the table when needed, but do not modify it unless the user explicitly authorizes the modification.

## Card text and mechanics

- Before adding or revising card rules text, keywords, generated-card previews, or upgrade descriptions, read `design/卡牌文本与机制实现强制规范.md` completely.
- Run `tools/validate_card_text_format.ps1` after card localization changes. Normal exports run this check automatically.

## Card artwork

- Before generating or revising any card artwork, read `design/Slay_the_Spire_2_卡图生成强制规范.md` completely.
- For Guardian identity, equipment, poses, or mechanics, also read `design/Guardian_角色设计与动作强制规范.md` completely.
- Base visual-style judgments on the original portraits under `D:\STS2\images\packed\card_portraits`.
- Do not use the deprecated prompt in `design/Slay_the_Spire_2_卡图美术风格报告.md`.
- Card artwork must use the official-style 1000×760 landscape format, graphic flat color blocks, hard-edged cel shading, bold black shapes, limited palette, exaggerated perspective, and a single readable visual event.
- Reject photorealistic, painterly concept-art, cinematic, highly textured, or realistic-metal results before presenting previews to the user.
- Save approval candidates under `design/卡图预览` until the user approves them.

## Power artwork

- Before generating or revising any power/status/action icon, read `design/能力图标生成强制规范.md` completely.
- Every power icon must use a true-alpha 256×256 PNG. Reject solid-color square backgrounds, color-key transparency, dirty translucent corners, and shared placeholder artwork; verify all four corner pixels have alpha 0 before import.
- Treat `guardian_assets/guardian_power_atlas.png` and the regions in `images/atlases/power_atlas.sprites` as the single source of truth for Guardian power icons.
- Whenever a Guardian power icon or atlas region changes, run `tools/sync_guardian_power_icons.ps1` before Godot import/export.
- The sync must overwrite every matching `images/powers/*.png` and `powers/*.png` file at 256×256 so the compact power icon and the applied/triggered power flash always show the same artwork.
- Use `tools/export_guardian_mod.ps1` for normal exports; it runs the icon sync before Godot import, PCK export, Release build, and installation.
