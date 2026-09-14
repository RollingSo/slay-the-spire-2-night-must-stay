# 花园鳗死灵与守护者合成检查

先编译工作区模组，再执行：

```powershell
$env:NUGET_PACKAGES = 'C:/Users/17857/.nuget/packages'
dotnet build NightMustStay.csproj --no-restore '-p:Sts2AssemblyDir=D:/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64'
dotnet run --project tools/summon_synthesis_tests/summon_synthesis_tests.csproj
```

测试实际安装 Harmony 补丁并执行原怪物 `GenerateAnimator`。仅替换需要 Godot/Spine 的动画构造边界，实际动画分支和条件照常执行。覆盖友方花园鳗缺少/移除胆怯能力、能力存在时的格挡受击，以及敌方原有动画分支。不会给死灵添加敌方能力，不启动游戏或遥测。

合成测试执行进化防御和矛与盾的实际 `OnPlay`、素材过滤和生成逻辑。覆盖基础/升级版、0/1/2/3 张素材、同名素材、生成牌归属和升级；采用原生 `PlayerChoiceResult` 与 `NetCombatCardDb` 完成不同卡牌实例间的 ID 编解码回放。选牌界面和牌堆指令效果被测试替身隔离，因此不是双客户端联机、网络时序或动画残留实测。

## 多人交错执行诊断

```powershell
dotnet run --project tools/summon_synthesis_tests/summon_synthesis_tests.csproj -- --audit-interleaving
```

该模式模拟等待第二次选牌期间，其他行动把第一张素材从弃牌堆移到手牌。

2026-09-15 在正式版 v0.107.1 程序集下的结果：两张牌都请求从 `Hand,Discard` 消耗素材，随后生成合成牌。第一张素材位置未重新校验。这是机制边界问题的受控复现，不等同于已实测某张队友牌必然触发，也不能据此断言网络失步或屏幕中央残留。

原版 `GameActionPlayerChoiceContext` 在选牌时暂停当前玩家行动；`ActionQueueSet.GetReadyAction` 允许其他玩家队列继续。`CardSelectCmd.FromCombatPile` 使用原生选择 ID、战斗卡牌 ID 及恢复行动流程，模组没有绕过该同步机制。正常 ID 回放测试通过，素材在等待期间移动的风险另行保留诊断，未修改两张合成牌。

本机尚无独立 beta 程序集；不能将上述结果视为 beta 已验证。
