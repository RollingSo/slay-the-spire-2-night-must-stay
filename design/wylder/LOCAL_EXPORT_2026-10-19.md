# 追踪者本地导出安装回执

日期：2026-10-19（Asia/Shanghai）。用户要求“导出一个版本到我本地”。

- 分支：`codex/wylder-development-20261018`，包含本地未提交改动；未推送、未合并main。
- 使用标准 `tools/export_guardian_mod.ps1`，Godot 4.5.1，程序集来自本机 `D:/SteamLibrary/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64`。先以`-SkipInstall`完成全套门禁，再复制四个Mod文件安装，保留现有用户配置。
- 导出目录：`build/`；安装目录：`D:/SteamLibrary/steamapps/common/Slay the Spire 2/mods/`。
- 已安装`NightMustStay.pck`、`NightMustStay.dll`、`NightMustStay.pdb`、`NightMustStay.json`。四文件SHA256均与导出结果一致。
- PCK：373983124字节，SHA256 `AC5FB122882449438F999575FEB9623097B833F16A21063464E2D6F367FC1E40`。
- DLL：1152512字节，SHA256 `E0D65263BB8DF5514BD6A61A3B7F56674F24705CA2FC69C84040F9DDC56F3935`。

## 验证

标准导出的卡名、关键词高亮、卡牌文本格式、四语键一致性、韩文检查、模型回归、角色/卡图/遗物/药水/能力图标门禁通过。Godot导入、独立PCK加载、追踪者场景与状态切换、其他角色图标和多人手势检查通过，Release构建成功。

包内追踪者待机可见高度299.200019px，追击待机人体高度299.168590px，追击攻击299.199998px；脚底基线均近0。追击攻击采用用户批准的v12本地几何校直版本。详见`build/wylder_resources.log`。

导出检查中修复了追踪者卡牌缺少的格挡/力量/易伤高亮；未改变卡牌效果或数值。单边耳环详情页缺少的风味字段已补齐为中性物品外观描述，四语保持一致（日韩仍沿用明确英文回退），同步生成器也保留此字段。没有降低或跳过检查规则。

## 边界

安装前确认游戏未运行。未改存档、用户现有配置或创意工坊文件。创意工坊中也有同ID的NightMustStay，测试时应避免同时启用两个来源。未启动游戏游玩，实际开局由用户测试。
