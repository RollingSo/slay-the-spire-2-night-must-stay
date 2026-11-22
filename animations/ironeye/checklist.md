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
| camp_idle | planned `main/camp_idle.res` | `camp_idle` | 96 | 24 | yes | 首帧+提示词已备；待生成视频，当前仍是静态场景。 |

The combat sequence resource does not carry `canvas`/`anchor` metadata; placement is handled by the Ironeye combat rig. The merchant `.tres` is kept as `.tres` because it is an existing engine sequence with external atlas pages; all atlas paths were rewritten to the new folder.


## Ironeye 篝火待机 提示词

首帧：`preview/ironeye_camp_idle_start_green.png`（2600×1700 绿幕，人物保持原像素尺寸）。

```text
【图生视频｜首帧循环】只使用我提供的这一张绿幕首帧，不需要尾帧参考。图中角色保持坐姿：尖顶苔绿色兜帽、近黑面部和冷色眼光、后方灰白短羽、背部高箭袋/箭架、长弓横放在腿上，右手拿着一支箭，左手搭在弓/膝上。只做篝火旁的坐姿待机，不站起、不走动、不射击。

【画面】保持 2600×1700 绿幕画布、人物像素尺寸、脚底/坐姿接触点和画面中心不变；不要放大、缩小或整体缩放角色。固定机位，2D 平面游戏动画；全身、兜帽、箭袋、箭、长弓和披挂完整入画，四周保留充足绿边，任何部件都不能触碰/越过画面边缘。无地面、无影子、无文字、无水印、无景深、无镜头运动、无 3D 透视。

【动作】5 秒循环、24fps；正式资源再截 2–4 秒连续循环：
0.0–0.8 秒稳定首帧。
0.8–2.2 秒缓慢吸气：胸口和肩线轻微起伏，头仍朝画面右前方，只做很小的视线变化；右手箭矢和左手长弓保持稳定。
2.2–3.4 秒缓慢呼气：肩背放松，斗篷和兜帽边缘低幅跟随；手指只做极小的握持调整。
3.4–4.6 秒轻微换重心：髋部和膝盖极小幅调整，坐姿不改变；长弓、箭和箭袋不主动摆动。
4.6–5.0 秒回到首帧，循环衔接自然。
动作幅度要小，像在篝火旁安静休息；不要大幅呼吸或整体缩放。

【约束】长弓始终是一把完整的弧形长弓；箭矢细、直、方向明确；箭袋和背部箭架保持原结构。右手始终持同一支箭，左手搭弓，不换手、不搭箭、不拉弦、不射击。兜帽/披挂不改变轮廓，不增加部件。人物大小、坐姿接触点和中心位置全程不变。

【负向】不要：站起、走动、转身、跑动、跳跃、射击、拉弓、搭箭、挥动武器、大幅呼吸、整体缩放、换手、多出一把弓/一支箭、箭矢弯曲/断折、箭袋变形、兜帽或披挂形变、角色出画、裁切四肢/弓/箭、镜头运动、3D 透视、地面、影子、文字、水印。
```

生成后从视频中截取首尾连续的 2–4 秒循环，再进入 `.res` 打包；当前静态场景暂留。

## Gaps / follow-up

- No separate death or camp idle animation is present in this repository snapshot; confirm whether the combat rig uses a fallback before claiming full action coverage.
- The combined `ironeye_sequences.res` should be the canonical source until the mod integration splits or re-exports it.
- No `derived/` folder because Ironeye has no family/companion animation set.