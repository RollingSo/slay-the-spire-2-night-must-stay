# 复仇者攻击特效与音效

2026-09-06：制作、接入与隔离验证候选。沿用用户「先不用覆盖」要求：不安装本机模组，不提交/推送 Git，不修改飞书。恢复卡图另存待审，不替换运行时 PNG。

## 分类与实际连接

| 类别 | 视觉 | 实际卡牌/入口 |
| --- | --- | --- |
| 光环飞出、下回合回收 | 金环从琴弦手部飞出，越过敌人；真实回手后从敌方飞回角色手部 | `Halo`、`ThreefoldHalo`、`RadagonHalo`；返程接 `HaloReturnPower.AfterSideTurnStart` 中成功移入 Hand 之后 |
| 红色古龙雷击 | 红色粗折线、浅色核心、侧支雷 | `AncientDragonLightning`、`LansseaxBlade`、`AncientDragonSpear`、`FlannSaxLightningSpear` |
| 黄色雷击 | 金黄色折线与落点放射 | `PreciseLightningStrike`、`LightningStrike`、`LightningSpear`、`DeathLightning` |
| 蓝色雷击 | 冰蓝色折线，落点附冰晶 | `IceLightningSpear` |
| 投石 | 棱角岩石抛物线飞行，撞击后少量石块 | `Beaststone`、`GurranqsRock` |
| 野兽爪形地裂气浪 | 三道土金色弧形爪浪，底部地裂线 | `BeastClaw`、`GurranqBeastClaw` |
| 癫火 | 五条弯曲散射的黄橙色火舌 | `SpaceRendingFrenzy`、`UnbearableFrenzy`、`FrenziedFlame`；`FrenziedThreeFingersPower` 真正对敌追加伤害 |
| 咒爪 | 三道弯曲骨白/紫色爪痕 | `StrikeRevenant`、`CursedClawCombo`、`SoulChargingClaw` |
| 海伦 | 细长冷色刺剑突刺、单点星芒 | `PerformFamilyAction` 海伦两分支，以及以当前海伦执行的 `DamageAsFamily` |
| 弗雷德里克 | 有分瓣南瓜锤头的下砸、土色冲击 | `PerformFamilyAction` 南瓜头两分支，以及当前弗雷德里克的卡牌追加攻击 |
| 塞巴斯蒂安 | 无衣物的五指骨掌与宽冷色冲击弧 | `PerformFamilyAction` 骷髅两分支，以及当前塞巴斯蒂安的卡牌追加攻击 |
| 恢复祷告 | 实际受治疗者身上的金色上升光柱、弧带、少量菱形光点 | `EmergencyRestore`、`Recover`、`GreaterRecover`（经 `HealFamily`）、`KingsRecovery`、`BlessingOfGracePower` |

22 张攻击牌映射由测试固定。普通光环与三重光环的 AttackCommand 使用施法者 VFX 工厂，只发一条穿透路线，不因群攻目标数复制圆环；拉达冈光环的直接伤害包装使用同样的路线创建器。返程不是飞出动画自动倒放：必须发生下一回合的实际回手，已在手中的牌不再假回收。

家人自动行动、共鸣和双行动经过 `PerformFamilyAction`；「破阵之锤」「巨骨狂怒」等家人追加攻击经过 `RevenantTextTableHelpers.DamageAsFamily` 的单体/群体两个分支。这些入口全都已连接，不只是独立预览。死灵的普通攻击没有冒充三位家人的特效。

只在真正攻击时绘制：蓄力选本体的分支仍提前返回；癫火牺牲家人的友伤保持原生 Damage，不附对敌火舌；纯能力牌不伪造攻击。未列入本轮分类的其他复仇者攻击保留原效果。

## 金色治疗的替换边界

`RevenantAttackEffects.Heal` 仍调用原生 `CreatureCmd.Heal` 处理 HP、记录、钩子和原等待。只有战斗中有可用目标节点、且原调用允许动画时，关闭该次原生蓝色 Osty/绿色十字动画，改为金色治疗；补回治疗数字和必要复活动画。没有全局修改其他角色的治疗。

血量实际增加才创建金色效果；满血不假造治疗。给复仇者本体增加格挡的恢复牌分支仍是格挡，不画生命恢复。原 `playAnim:false` 的免死处理仍不画动画。家人/死灵/多人玩家分别以实际被治疗 Creature 的位置为锚点，不固定画在复仇者身上。

当前代码 `GreaterRecover` 的治疗 helper 只治疗当前家人，本轮没有擅自扩展其目标或修正卡表/描述；任何机制差异留给专门的机制任务。

## 原版音效配置

全部引用本机 STS2 PCK 内的已有资源，不提取/分发 Nightreign 或 STS2 音频。本轮已逐一加载、解码并验证时长，尚未进行完整战斗混音试听。

| 用途 | 原版资源（`res://debug_audio/`） | 时长约 |
| --- | --- | --- |
| 光环飞出 / 回收 | `glass_orb_passive.mp3` / `glass_orb_evoke.mp3` | 0.79 / 1.04 s |
| 红 / 黄 / 蓝雷 | `lightning_orb_evoke.mp3` / `lightning_orb_passive.mp3` / `lightning_orb_channel.mp3` | 2.25 / 2.01 / 1.75 s |
| 岩石撞击 | `blunt_attack.mp3` | 0.73 s |
| 野兽气浪 / 南瓜重锤 | `heavy_attack.mp3` | 0.83 s |
| 癫火 | `STS_SFX_BurnCard_v1.mp3` | 1.65 s |
| 咒爪 | `slash_attack.mp3` | 0.40 s |
| 海伦突刺 | `dagger_throw.mp3` | 0.43 s |
| 塞巴斯蒂安 | `dark_orb_evoke.mp3` | 1.41 s |
| 治疗 | 原生 `event:/sfx/heal` | FMOD 事件 |

治疗音对普通对象由原生 Heal 播放，不叠加第二份。家人采用 Osty 实体，原生 Heal 对它不直接发此声，因此替换旧家人治疗 VFX 后补一次原生治疗音。其余声音走 `NDebugAudioManager` 原版 SFX 总线/播放器池，遵守游戏音量；雷电 0.5，其余 0.65。雷电/癫火按类别 240 ms 节流，其他 85 ms，避免群攻瞬间按敌人数叠音。此配置是原版素材适配，不宣称复刻黑夜君临原声。

## 实现与验证

- `src/Core/Nodes/Vfx/RevenantAttackVfx.cs`：13 种纯 Godot 绘制（光环往返各一种），无新增纹理、粒子噪点、屏幕震动或全屏闪白。
- `RevenantAttackEffects.cs`：卡牌分类、家人类型、金色治疗、光环路径和音频。路径按实际全局坐标计算；返程终点由角色当前琴弦手部经所有嵌套变换取得，不修改战斗体型或标记。路线使用弱引用表，不进入存档，不持有旧卡牌；加载战斗没有历史路径时从敌方区域回手。
- 伤害值、攻击次数、选敌 RNG、蓄力、共鸣、伤害归属和召唤物数值均不改动。飞行属于表现层；不添加战斗等待，也不声称每次落点都与引擎扣血帧精确同步。
- `tools/revenant_vfx_tests`：22 个分类、攻击工厂、家人 6 行动 + 2 追加入口、回收顺序、治疗入口、TestMode 与伤害/次数不变检查。
- `tools/revenant_damage_tests`：现有随机多段攻击、冰冻伤害过滤、蓄力右键取消回归。
- 守护者与铁之眼现有 VFX 测试同样通过，没有覆盖它们的实现。
- `tools/revenant_vfx_preview`：独立 Godot 工程直接链接生产绘制。默认循环；`--fixed-fps 60 -- --verify` 验证 13 类各 121 个生命周期采样、65 个节点全部释放及 3 类飞行正/反/零长度端点；`-- --capture` 生成 `design/特效预览/revenant_attack_vfx_contact.png`。GPU 检查包含曲线接缝修订。
- `check_audio.gd` 接收原版 PCK 路径，验证所有 11 个独立音频资源；不写出音频。
- 使用 `tools/export_guardian_mod.ps1 -SkipInstall` 生成 `build/NightMustStay.dll` 与 `.pck` 候选。独立工程已有 ironclad energy counter / card trail 原版场景引用提示，不等同于加载原版 PCK 后的完整实机验证。

尚需用户同意覆盖后再进行：实际多人场景/不同战斗速度、目标死亡与移动时的表现、光环入手视觉位置、治疗数字遮挡与最终音量试听。本轮隔离渲染和编译不冒称完整实机验收。

恢复卡图：`design/卡图预览/revenant_recover_gold_20260906/recover_gold_candidate.png`，1000×760，使用 imagegen 两轮审查改稿后保存。未修改正式卡图；同目录记录六项审查及飞书读取失败限制。

## 本轮最终验证记录

- Debug 与候选导出构建：0 警告、0 错误；正常导出脚本退出码 0，明确显示 `build only`。
- 卡牌文本格式、本地化语言键一致性、既有公爵夫人资源检查及两角色能力图标同步检查通过，图标同步没有产生更新。
- 复仇者 VFX / 伤害回归、守护者 VFX、铁之眼 VFX 测试均通过。
- 最终 GPU 日志：`.tmp/revenant-vfx-verify.log`，`remaining=0`；最终接触表已目视复核，弧形带使用共享法线闭合多边形，消除分段拼接孔隙。
- DLL SHA-256：`67079533DFEF2C67ED0960AA237EE8A90AEE82130169BD57F7D4A5891348AD96`。
- PCK SHA-256：`9A0F7D280FD19226A0F400105C63603EC94CF1FC6744DD3CA1544FD0CE4ABB61`。
- 待审 Recover PNG SHA-256：`713778BA57F9C4D4A41D7A2A2B7061661A9A6CA811DA90E599472A50A3C55A56`；正式 `revenant_assets/cards/recover.png` 与 Git 版本无差异。
