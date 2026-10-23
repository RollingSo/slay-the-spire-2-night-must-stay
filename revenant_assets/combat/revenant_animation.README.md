# revenant_animation.gd（未接入，评估用）

本体在战斗里目前仍由 `revenant_combat_rig.gd` 驱动：一组**静态姿势贴图**
（idle / attack / hit / claw_attack / prayer_attack / death）+ AnimationPlayer 做位移、
缩放和淡出。没有逐帧动画。

这里放的是按家人那套工作流做的逐帧版本，用于对比评估，**尚未接入**
`revenant_combat_visuals.tscn`。公开接口与 rig 保持一致（`play_trigger` /
`queue_attack_motion` / `play_card_motion` / `clear_card_motion`），要换的话
只需把 `Visuals/Prototype` 换成 Sprite2D + 这个脚本。

## 对照表

| rig 动画 | 触发 | 本播放器 clip | 资源 |
|---|---|---|---|
| idle_loop | Idle / Relaxed | idle | RV01 combat_idle，96 帧循环 |
| claw_attack | Attack / Cast（默认） | claw_attack | RV02，110 帧 |
| attack | Call / Resonance | lyre_command | RV03，32 帧 |
| prayer_attack | Prayer | prayer_attack | RV04，44 帧 |
| hit | Hit | hurt | RV06，20 帧 |
| death | Dead | death | RV07，78 帧 |

RV05 复用 RV03；RV08 是商店待机，不在战斗里。

## 关键差异（评估结论）

1. **分辨率**：rig 姿势贴图角色高 1328px，逐帧版本只有 215px（1:1 游戏尺寸）。
   游戏一旦放大或高 DPI，逐帧版本会明显糊。
2. **自带特效**：`revenant_claw_attack.png` 里已经把紫色爪痕画进贴图；逐帧版本没有，
   需要游戏侧补 VFX，否则打击感更弱。
3. **动作细腻度**：逐帧版本有真实的手臂、长发、衣料运动；rig 只有姿势切换。
4. 画布/锚点规则与家人一致：offset = canvas/2 - anchor，画布随动作。

对比 GIF 见 `design/动画预览/revenant/基准_v03/GIF/本体对比/`。
