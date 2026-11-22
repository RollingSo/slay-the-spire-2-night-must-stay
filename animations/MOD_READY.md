# 动画 Pack-Ready 状态

更新：2026-11-22。**没有实际导出或打包**；这里只记录打包前的接入状态、资源路径和已知缺口。

## 已接入

| 角色 | 战斗视觉入口 | 动画来源 | 状态 |
|---|---|---|---|
| Guardian | `guardian_assets/combat_rig/guardian_combat_visuals.tscn` | `animations/guardian/main/*.res` | 7 个战斗动作 + 篝火；Prototype 已替换为序列播放器 |
| Duchess | `duchess_assets/combat_rig/duchess_combat_visuals.tscn` | `animations/duchess/main/*.res` | 战斗/篝火/商店；Block 映射 `dodge_defend` |
| Revenant | `revenant_assets/combat/revenant_combat_visuals.tscn` | `animations/revenant/main/*.res` | 本体战斗 + 篝火；家人序列仍走各自旧路径 |
| Ironeye | `ironeye_assets/combat_rig/ironeye_combat_visuals.tscn` | `ironeye_assets/combat_rig/ironeye_sequences.res` | 原序列 rig；已替换为 canonical 压缩资源；篝火/死亡缺素材 |

通用播放器：`animations/runtime/sequence_prototype.tscn` / `.gd`。C# 仍从 `Visuals/Prototype` 调用 `play_trigger`，接口不变。

Headless 冒烟测试已通过：Guardian/Duchess/Revenant 最终可见高分别精确到 308.3310 / 307.5469 / 215.7527；`Block`、`Prayer` 和原生 `Attack` 确认路径正常。测试脚本：`tools/animation_forge/test_sequence_scenes.gd`。

## 未完成 / 不允许假装完成

- **Wylder**：版权原因暂停，只有 WY07 追击待机；不能打包成完整战斗动画。
- **Ironeye**：没有 `death.res` 和 `camp_idle.res`；篝火仍是静态 `Sprite2D` + 程序呼吸。
- **Revenant**：FR03（Frederick 攻击）缺失；家人动画未统一切到通用播放器。
- **Duchess**：DU09 `time_command` / `phantom_dial` 尚未映射到真实触发。
- **Guardian**：已知盾牌正面瑕疵沿用；尚未实机验收。
- 所有角色都还没做目标版本实机/导出 PCK 验证；`pack-ready` 只表示资源与场景引用已接好。

## 打包要求

1. 使用正常 `all_resources` 导出；不要只靠 include 白名单，因为通用播放器用动态路径 `load()` 动作。
2. 排除 `animations/*/preview/**`、`.tmp/**`、`tools/**`、`design/**`。
3. 保留：
   - `animations/runtime/sequence_prototype.gd/.tscn`
   - `animations/<character>/main/*.res`
   - 对应 `*_assets` 场景/脚本
4. 不要删除旧静态 Prototype 场景作为回退，除非先确认新序列 rig 在目标版本通过。
5. 正式打包前仍需：资源加载、四人同框、快速/瞬时模式、受击打断、死亡/复活、PCK 新进程测试。

清单机读版：`animations/mod-ready.json`。
