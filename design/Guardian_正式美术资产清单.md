# 守护者正式美术资产清单

## 已替换并接入

- 角色选择背景：`guardian_assets/character_select_guardian_bg.png`
- 角色选择头像：`guardian_assets/char_select_guardian.png`
- 锁定状态头像：`guardian_assets/char_select_guardian_locked.png`
- 顶部栏头像与轮廓：`guardian_assets/character_icon_guardian.png`、`character_icon_guardian_outline.png`
- 地图标记：`guardian_assets/map_marker_guardian.png`
- 战斗能量计数器：`guardian_assets/energy_counter` 中的五层蓝银能量球纹理；运行时换肤本体计数器，保留成熟动画逻辑
- 36 个 Power 图标：`guardian_assets/guardian_power_atlas.svg`，通过 `atlases/power_atlas.sprites` 中的独立区域资源接入

## 暂时保留的原游戏资源

以下项目不是静态占位图，不能仅靠一张图片安全替换：

- 战斗角色骨骼、动作与受击动画
- 商店角色动画
- 营火角色动画
- 卡牌拖尾粒子
- 角色选择转场材质
- 攻击、施法、死亡与选择音效

这些项目需要各自的骨骼动画、粒子材质或音频制作流程。当前继续复用原游戏铁甲战士资源，确保功能完整，不把未完成的静态图冒充正式动画资产。
