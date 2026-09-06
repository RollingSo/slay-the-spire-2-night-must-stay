# 守护者三类攻击 VFX 与音效

## 本次实现（2026-09-06）

依据 `Guardian_角色设计与动作强制规范.md`：用戟刃扫击而非盾牌撞击表达防御反击；旋风采用横向宽风带与少量羽片，不画写实龙卷风。纯 Godot 几何绘制，无外部贴图、着色器、音频文件依赖；不改卡表、伤害、升级、目标、攻击次数或反击判定。

| 类型 | 视觉 / 时长 | 原版声音 / 音量 |
| --- | --- | --- |
| 武器直击 | 银白斜向戟刃、深色轮廓、单一命中点；0.36 秒 | `res://debug_audio/slash_attack.mp3`；0.4049 秒；0.75 |
| 呼唤旋风 | 青灰横向双宽风带、下层短风弧、3 枚羽片；0.54 秒 | `event:/sfx/characters/ironclad/ironclad_whirlwind`；0.65 |
| 防御反击 | 短促银白格挡提示转赭金重戟斩、交叉冲击点；0.50 秒 | `res://debug_audio/heavy_attack.mp3`；0.8345 秒；0.85 |

声音沿用游戏 NDebugAudioManager / SfxCmd，遵循游戏主音量和 SFX 音量，不另开绕过静音的播放器。保留既有角色出牌起手音，本次替换的是命中特效和命中声音。音量为线性倍率，尚需实机听感验收，不宣称是黑夜君临守护者原声。

本机实际可用目录是 `D:\Nightreign_Audio_Work`，而不是用户给出的嵌套路径。目录中的 WEM/WAV 主要为数字 ID，本轮未能可靠建立守护者招式对应关系，因此没有拷贝这些音频到发布包。普通斩击和重击的存在、可解码性与时长已经从本机 SlayTheSpire2.pck 核验；旋风事件取自已安装原版 Whirlwind 卡的调用。

## 接入清单

- 武器攻击：`AllOutCounter`、`GuardianCharge`、`GuardianAssault`、`PhantomSpear`、`PhantomCoStrike`、`ShieldImpact`、`FinalCurtainHalberd`、`SkySweepingGod`、`HeavyHalberd`、`DustReturnSlash`、`HideAndSeekStab`、`HalberdWingCombo`、`ProbingStab`、`SaviorSpreadWings`、`ShieldedThrust`、`ShieldPoke`、`StepForwardPursuit`、`StrikeGuardian`、`Topple`、`WingStrike`。
- 旋风攻击：`Cyclone`、`CycloneHalberd`、`GreatTornadoPower`、`StormAssault`、`InvokeStorm`、`CloudRendingSweep`、`CirclingGust`、`WorldEndingWings`、`WhirlingStrike`。
- 延迟戟击：`PhantomCoStrikePower` 的次回合追击同样播放武器特效。
- 非 AttackCommand 风击：`GuardianWhirlwind`、`WardingGalePower`、`GreatTornadoPower` 无卡源分支、`StormBirthPower` 沿用旧入口，转发到新旋风实现。
- 所有防御反击共用 `GuardCounterPower.TriggerGuardCounter` → `NightreignHitVfx.PlayGuardianCounter` → 新 Counter 特效。
- `AllOutCounter`、`StepForwardPursuit` 等只是附带或受益于反击状态的主动攻击，不能因为牌名就播放“反击成功”特效。
- 仅施加状态、格挡、姿态失衡眩晕及独立咒魂伤害的规则保持原样，不伪装成一次武器命中。

## 生命周期与音效叠加

- 特效挂到目标的 VFX 容器，以 VfxSpawnPosition 为中心；死亡目标、缺少战斗节点、TestMode 均不生成节点。
- _Ready 播放声音，避免未入树或被跳过的目标发声。每个目标都有独立视觉，音效按类型节流：武器/反击 75 ms，长尾旋风 400 ms，避免 AOE 和 X 次攻击堆叠音量。
- 所有节点在 0.54 秒内 QueueFree；不增加伤害流程等待、不用战斗随机数、不加全屏闪白和屏幕震动。
- 旧 NightreignHitVfx 的守护者入口保留为兼容转发，旧守护者绘制代码删除；铁之眼实现不变。

## 验证与复现

1. `dotnet build NightMustStay.sln --no-restore -p:Sts2AssemblyDir=<本机游戏程序集目录>`。
2. `dotnet run --project tools/guardian_vfx_tests/GuardianVfxTests.csproj`：检查无 Godot 窗口的 TestMode 工厂、兼容入口、同一 AttackCommand、伤害与攻击次数不变、只有一个新 VFX 工厂、旧 VFX/SFX 清空。
3. `dotnet build tools/guardian_vfx_preview/GuardianVfxPreview.csproj`；Godot 启动 `tools/guardian_vfx_preview`，底部可循环预览。该项目链接生产 GuardianAttackVfx.cs，不复制一套假预览几何。
4. 启动预览时添加 `--fixed-fps 60 -- --verify`：逐类采样 121 个生命周期位置，同时验证 33 个动态节点全部释放。添加 `-- --capture` 保存 `design/特效预览/guardian_attack_vfx_contact.png`。
5. `check_audio.gd` 接收原版 PCK 路径，检查两种 MP3 的加载和时长。
6. 标准打包：`tools/export_guardian_mod.ps1 -SkipInstall`。不覆盖已安装模组。

本轮已通过：Debug 编译（0 警告/0 错误）、模型回归、实际 GPU 绘制与节点回收检查、原版短音频加载检查。尚未进行完整战斗实机听感、多人同屏、不同战斗速度验收。导出生成 DLL/PCK，但独立编辑器无法解析现有工程引用的两处原版场景（ironclad_energy_counter、card_trail_ironclad）；需在加载原版资源包的游戏中确认，这不是本轮新增的 VFX 资源。
