# 追踪者41张卡本地测试版导出回执

日期：2026-10-23（Asia/Shanghai）。分支：`codex/wylder-development-20261018`，包含当前本地未提交改动，未推送或合并main。

## 导出内容

- 41张真实注册牌：4基础、16普通、15罕见、6稀有；20攻击、21技能，37张非基础掉落。
- 初始生命78，初始卡组4打击、4防御、钩爪移动、冲刺攻击；单边耳环开战进入追击。
- 破势按扣格挡前原生TotalDamage、单敌多段累计、群攻分敌、每CardPlay一次。
- 仍有49张候选尚未实现。新牌测试阶段引用已验证存在的原生卡图，不把未批准的专属图案当成定稿。

## 门禁与修复

使用标准`tools/export_guardian_mod.ps1 -SkipInstall`，本机游戏程序集位于`D:/SteamLibrary/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64`，Godot4.5.1。

首次门禁查出转身防御未同步全局防御名称分类：已补四语登记、稳定ID分类及回归；同步器现在统一保留条件句/手牌术语的高亮并读取四语牌名覆盖。标准导出增加追踪者专项模型检查，避免通用测试入口遗漏本角色。

四语、卡牌文本、女爵模型门禁、追踪者源图/先古对话、521张卡图资源、遗物/药水/能力图标、Godot导入、独立PCK加载及追踪者场景/体型验证全部通过。Release构建0警告0错误。追踪者专项391+1104+836项检查、初始卡组和破势回归通过。模型及资源验证不代替实际游戏测试。

## 文件

导出目录：项目`build/`。安装文件：`NightMustStay.pck`、`NightMustStay.dll`、`NightMustStay.pdb`、`NightMustStay.json`。

- PCK：377999648字节，SHA256 `12DCE936B9F24652B547FBF3FCE3DEA5D7A786C134BD88B0ACFAF9372468CE56`。
- DLL：1205248字节，SHA256 `7558DA6CBFF44887612281027088B0AD4A474758222C1C3B35307042E8280377`。
- PDB：406776字节，SHA256 `1BCFBD4AE1B32C57ADE2B23507D5BE99F06EFDDEE1614A61EA0F23659699B00B`。
- JSON：368字节，SHA256 `EE78B8AACAE4EB80F5300F11EF1365F28D0900E22B48B1ACB1F33DD5AB9EFDC7`。

## 安装状态

安装前在实际权限环境检测到`SlayTheSpire2.exe`运行，PID75580。安全检查在备份/复制之前中止，未覆盖游戏mods。未关闭游戏、未修改存档、用户配置或创意工坊文件。

退出游戏后，将压缩包根目录的四个`NightMustStay.*`文件复制到`D:/SteamLibrary/steamapps/common/Slay the Spire 2/mods/`。先备份同名旧文件；勿与创意工坊同ID版本同时启用。
