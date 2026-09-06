# 铁之眼三类攻击 VFX 与音效

## 本次实现（2026-09-06）

遵守 `Ironeye_角色设定与美术强制规范.md` 的克制、精准、短刃标记与直线箭矢语言。与守护者采用同一交付流程：明确分类 → 卡牌/能力实际调用 → 原版音效核验 → 生产几何预览 → 回归与节点回收检查 → 标准导出。没有使用或修改飞书卡表，没有更改卡牌文字、伤害、标记层数、标记阈值、连击次数与目标选择。

| 类别 | 视觉 | 原版音效（线性音量） |
| --- | --- | --- |
| 匕首 | 紧凑骨白斜切、酸黄单边短拖影；0.28 秒，不用守护者的大幅戟刃弧 | `res://debug_audio/slash_attack.mp3`；0.4049 秒；0.60 |
| 弓箭 | 细直箭杆、清晰箭头与双箭羽、短破风尾迹；飞行 0.09 秒，总长 0.36 秒 | `res://debug_audio/dagger_throw.mp3`；0.4281 秒；0.70 |
| 标记触发 | 四角收束到酸黄/青色 X，随后向外裂开；0.44 秒，不用复杂魔法阵 | `res://debug_audio/glass_orb_passive.mp3`；0.7880 秒；0.65 |

射击音使用原版投射物破风声作为替代，不称其为真实弓弦或黑夜君临守护者/铁之眼原声。三种声音已从本机 SlayTheSpire2.pck 加载、解码并读取时长；实际混音与听感尚需游戏内试听。

## 明确卡牌接入

35 个 AttackCommand 调用点全部使用显式路由：11 个匕首，24 个弓箭。

- 匕首：淬毒匕首（`VenomDagger`）、双吻毒蛾（`TwinKissPoisonMoth`）、猎步标记（`HunterStepMark`）、影袭（`IroneyeShadowAssault`）、刀锋滑行（`BladeGlide`）、凋零斩（`WitheringCut`）、恩赐解脱（`Release`）、攻势（`Offensive`）、斩乱麻（`CutThroughChaos`）、风华刃舞（`GracefulBladeDance`）、接近（`Approach`）。
- 弓箭：连续射击（`ContinuousShooting`）、后跃射击（`BackstepShot`）、凋零箭（`PoisonBurst`）、对空射击（`AntiAirShot`）、宿灵射击（`SpiritShot`）、三箭齐射（`TripleVolley`）、爆头（`IroneyeHeadshot`）、箭雨（`IroneyeArrowRain`）、毒箭（`IroneyePoisonArrow`）、贯穿射击（`HeartpiercingArrow`）、散射（`Scatter`）、毒雾箭阵（`PoisonMistArrowArray`）、弓斗术（`BowCombatArt`）、穿杨一箭（`WillowPiercingArrow`）、追踪箭（`TrackingArrow`）、谢幕（`CurtainCall`）、锐不可当（`AirRendingArrow`）、穿云箭（`CloudPiercingArrow`）、回风箭（`ReturningWindArrow`）、回身一箭（`TurningArrow`）、追魂连箭（`SoulChasingVolley`）、蚀尽（`CorrodeAll`）、拉满弓（`FullDraw`）、打击（`StrikeIroneye`）。
- 基础“标记”保留施加状态前的匕首动作；“死亡标记”在每个目标上播放同类匕首划切。它们不会仅因施加标记就播放标记爆裂。
- “闪身箭斩”对应 `EvasiveArrowSlashPower`，其距离变动追击通过兼容入口调用新匕首特效。
- “雷电箭头” `LightningArrowheadPower` 和“乱箭” `DisorderlyArrowsPower` 的追加伤害改用箭矢，停止借用标记消耗特效。
- 原代码 `PoisonBurst` 的当前中文名是“凋零箭”；按当前卡牌身份接弓箭，而不是根据旧类名误判为标记爆裂或匕首。
- “迫近毒牙”等只施加能力、未发生攻击的牌不伪造一次攻击；持续潜毒伤害也不会冒充标记触发。

## 真正的标记结算

唯一的实际标记爆裂入口是 `NightMustStayMarkPower.TriggerOne` 中原有的触发点。自动满足条件、宿灵射击等显式触发、爆头等 `TriggerAll` 均经过该入口；不改变次数、伤害与触发顺序。原有常驻标记状态图案、位置与 Pulse 保持不变。

`NightreignHitVfx` 保留公开兼容方法，转发到角色各自的 Effects 工厂；铁之眼旧绘制已移除。守护者的转发、绘制、声音与卡牌连接不变。

## 技术约束

- `IroneyeAttackVfx.cs` 只依赖 Godot，自行绘制，不需要贴图、着色器或外部音频文件。
- 射击锚定目标中心，以攻击者中心计算起点；左右/高低方位都由实际坐标推导，箭头最终精确落在目标。没有新增伤害流程等待，0.09 秒飞行是表现层动画，不宣称重排了引擎的伤害结算帧。
- 缺少战斗节点、目标死亡、TestMode 均不生成 VFX；结束自动 QueueFree。音效在入树时才播放，非交互模式与战斗结束时不发声。
- 三类声音分别节流：匕首/弓箭 65 ms，标记 160 ms。群攻保留每个目标的视觉，但不按敌人数叠加同一声音；标记声不会被攻击声节流吞掉。
- 使用 NDebugAudioManager 的 SFX 总线与播放器池，遵守游戏主音量/SFX 音量；不改变角色原有起手音。
- 不增加战斗 RNG 调用、屏幕震动、全屏闪白或粒子噪点。

## 验证与复现

1. 构建主项目（传入本机 Sts2AssemblyDir），再运行 `dotnet run --project tools/ironeye_vfx_tests/IroneyeVfxTests.csproj`。
2. 测试自动检查 35 个攻击调用点分类齐全、一个真正的标记爆裂入口、伤害和三连击次数不变、旧 VFX/SFX 清除、单一新工厂、TestMode/兼容入口安全。
3. 同时运行 `tools/guardian_vfx_tests/GuardianVfxTests.csproj`，验证共享兼容入口未破坏守护者。
4. 构建 `tools/ironeye_vfx_preview/IroneyeVfxPreview.csproj`，以 Godot 打开该目录。预览项目直接链接生产 `IroneyeAttackVfx.cs`。默认循环播放；`--fixed-fps 60 -- --verify` 检查三个方向的箭矢端点、121 个生命周期位置以及 33 个动态节点全部释放；`-- --capture` 输出 `design/特效预览/ironeye_attack_vfx_contact.png`。
5. `list_audio.gd` 接收本机原版 PCK 路径，列出可解码的短音频名称与时长；不提取或分发原版音频文件。
6. `tools/export_guardian_mod.ps1 -SkipInstall` 导出候选 DLL/PCK。用户明确要求暂不覆盖，因此不安装、不推送。

本轮验证结果：见实际命令输出与 `.tmp/ironeye-vfx-verify.log`。隔离渲染不是完整战斗验收；多人同屏、不同战斗速度、命中伤害文字与标记先后帧、声音音量均保留实机验收项。独立编辑器导出时存在工程已有的原版 ironclad_energy_counter / card_trail_ironclad 场景引用提示，应与已加载原版资源的游戏环境区别对待。
