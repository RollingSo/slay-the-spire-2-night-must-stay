# 铁之眼五动作正式接入与本地安装

用户已授权在 `animate-0.1` 修改正式 Mod 并应用到铁之眼，随后授权提交和推送关键内容。本轮已完成代码、资源、完整导出和本机安装；没有发布工作坊或合并 main。main 基准为98bad9e。

## 当前效果与实现

- 正式战斗rig使用142帧 SpriteFrames：普通待机、射箭、小刀、施法／强化、受击。旧PNG与旧死亡AnimationPlayer保留作为死亡表现。
- 低血量、放松继续使用普通待机；没有新低血量或死亡素材。
- `Cast` 与 `PowerUp` 都播放施法／强化序列，结束回到待机。目前没有独立防御动画，也没有按卡牌类型强制播放的新增规则；特殊防御／能力卡仍取决于既有命令是否发出这两个触发。
- AttackCommand 的刀／箭分类由既有 WithIroneyeKnifeFx / WithIroneyeShotFx 记录，使用每个命令的弱引用元数据与AsyncLocal不可变作用域传给实际触发。保留原Attack触发、音效入口、伤害、目标、次数、RNG和任务等待。
- 同名攻击立即从首帧重播，受击即时打断。死亡隐藏新序列、播放旧死亡；死亡期间忽略迟到攻击和待机，复活重置旧透明度并恢复新待机。
- 起手按 CreatureCmd.TriggerAnim 的真实 waitTime 对齐：普通、快速和瞬时模式分别采用原命令的等待规则，修改的是表现时间线，不是游戏伤害等待。直接触发没有命令参数时使用角色当前默认0.15秒攻击、0.25秒施法作为回退。标记不是新伤害事件。
- 去掉射箭源序列中已经画出的离弦飞箭，保留现有游戏箭VFX；箭的起点使用当前rig的放箭姿态挂点并经过全部父变换，不再始终从角色中心出生。挂点是人工标定，未声称逐帧骨骼跟踪。
- 142帧统一裁去透明画布留白，单份解码RGBA约58.44 MiB，原全画布约166.4 MiB。该数字不含旧死亡贴图、驱动缓存和其他Mod资源，不能当作实测总显存。

## 验证结果

正式工具 `tools/export_guardian_mod.ps1` 完整通过；最终 Release 生成0错误、0警告。原有卡名、文本格式、多语言、女爵模型、家人／死灵、图标和卡图隔离PCK检查均执行。原有卡牌hover-tip测试2880项通过，资源工厂按原测试方案替身。

新增导出PCK的23项动画检查同时在官方编辑器和游戏自带 MegaDot 4.5.1.m.14 引擎通过：正式场景和脚本加载、五动作、经过正式嵌套缩放后的可见高度323.733564与脚底0、Bounds宽250、保留意图／中心标记、普通与快速起手、瞬时跳过、命令自定义等待、重复攻击、受击归位、能力复用、死亡、复活与去除重复箭。原始结果位于 `build/ironeye_animation_checks.json` 和 `build/ironeye_game_engine_checks.json`。

最终DLL的11项Harmony绑定和异步路由检查通过，覆盖同步调用方恢复、await后分类、同一角色并发命令、嵌套未分类命令、异常传播、不同角色隔离和重复构建元数据，见 `build/ironeye_animation_context_checks.json`。

初次检查中发现重复箭起点函数，已移除后重新全量导出；失败版本未安装。验证器现在检查脚本接口并有20秒超时，避免解析失败后无限等待。独立引擎的脚本缓存／系统证书日志保留，不当作真实游戏运行日志。

构建环境原有女爵测试依赖D:/STS2文本目录。本轮增加 `NMS_NATIVE_LOCALIZATION` 路径与原版PCK提取：184个原版JSON按PCK条目MD5校验后放到.tmp，本地化检查照常执行，没有跳过测试、改原文或改卡牌规则。

## 已安装的本地测试包

目录：`F:/SteamLibrary/steamapps/common/Slay the Spire 2/mods`。

Mod显示名称：`Slay the Spire 2 : Night Must Stay [animate-0.1]`，ID仍为NightMustStay，版本仍为0.3.15；源发布manifest不作版本升级。外部本地描述文件仅增加分支名。

安装4个文件：NightMustStay.pck、NightMustStay.dll、NightMustStay.pdb、NightMustStay.json，安装后哈希与构建／测试描述文件一致。本机此前没有这4个同名本地文件，因此本次没有覆盖旧本地包；保留安装记录于 `build/ironeye_animation_install.json` 和 `.tmp/ironeye_install_backup_20261015_004941/install_record.json`。

最终PCK SHA256：4067CF9D66E90A36962BD26495575FEB9952148DD38A3F38ADFC33A7E49BB794。

最终DLL SHA256：9642B10D3235B151BAF9359440E2CB87BD1BDDF475EF9E0CE0EFB89C32CAC7FC。

未改游戏程序、存档、用户设置或遥测配置。安装脚本会检查分支、验证记录、游戏未运行以及目标目录，先保存已有同名文件，再安装并校验哈希。

## 实际战斗仍待用户确认

本轮验证包括导出资源和正式脚本，但没有打开用户游戏进行菜单操作、开启实际战斗或录制实战画面。需要用户从Steam启动游戏，在Mod列表启用上述本地版本，再选铁之眼检查：普通射箭、刀类卡、施法、受击、连续出牌、快速模式，以及血条和特效对齐。

受击仍是短促整体后倾的补救素材，小刀仍用倒放收招，没有真实入鞘；默认待机循环接缝也仍存在。正式接入没有把这些美术限制抹掉。实际刀类命令路由、箭起点与同步已实现，但游戏中的最终观感、全部特殊卡触发与GPU性能不能仅凭离线测试宣布完成。

## 关键文件

- `ironeye_assets/combat_rig/ironeye_combat_rig.gd`、同名tscn、`ironeye_sequences.res`。
- `src/Core/Nodes/IroneyeAnimationContext.cs`、`src/Core/Patches/IroneyeAnimationPatch.cs`、`src/Core/Nodes/Vfx/IroneyeAttackEffects.cs`。
- `tools/ironeye_animation_validation/`、`tools/ironeye_animation_tests/`、`tools/install_ironeye_animation_test.ps1`。
- SpriteFrames生成器在 `tools/ironeye_animation_probe/prepare_runtime_sequences.py` 和 `build_runtime_sequences.gd`；原候选与透明帧仍在design预览目录，不额外打包供用户解压。

## Git交付范围

本次提交包含正式代码、自包含的 `ironeye_sequences.res`、验证与安装工具、构建所需的原版本地化读取器，以及接入记录。原始视频、历次预览与透明帧、原版资源提取结果、下载的编辑器和构建安装包保留在本地。正常构建直接使用已提交的 `.res`，无需重新生成视频帧；重新运行生成器则需要本地预览目录中的已打磨源帧。
