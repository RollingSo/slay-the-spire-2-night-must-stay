# Slay the Spire 2 : Night Must Stay — 2026-08-17 交接总结

## 1. 项目与工作方式

- 工程目录：`D:\sts-2-mod`
- 正式名称：`Slay the Spire 2 : Night Must Stay`
- 游戏 Mods 目录：`D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods`
- 当前分支：`main`，相对 `origin/main` 为 ahead 5；工作区存在大量尚未提交的代码、美术与本地化变更。
- **不得重置、回滚或覆盖无关改动。** 开始任何工作前先查看 `git status --short --branch`。
- 用户通常要求直接完成修改；不要反复确认显而易见的实现细节。
- 除非用户明确要求，**不要启动游戏**。游戏内验证由用户完成。
- `config.json` 是远程开发者加入的数据分析配置，打包和同步时必须保留。

## 2. 强制规范

每次继续开发前先完整读取根目录 `AGENTS.md`。其中最重要的约束如下：

- 卡牌数据唯一权威来源为飞书卡表：`https://my.feishu.cn/wiki/FnXnwkfWUiKQ0SkkvosctRpCnHg`。只读，未经授权不得修改。
- 卡图工作前必须完整读取：
  - `design/Slay_the_Spire_2_卡图生成强制规范.md`
  - 涉及守护者时还要读取 `design/Guardian_角色设计与动作强制规范.md`
  - 复仇者身份、美术与动作必须读取 `design/Revenant_角色设计与动作强制规范.md`
- 能力/状态/行动图标工作前必须完整读取：
  - `design/能力图标生成强制规范.md`
- 能力图标必须为真正透明的 256×256 PNG，四角 alpha 必须为 0；禁止色键透明、脏半透明角、纯色方块背景、共用占位图。
- 守护者能力图标修改后必须运行 `tools/sync_guardian_power_icons.ps1`；正常导出统一使用 `tools/export_guardian_mod.ps1`。
- 卡图必须是原版《杀戮尖塔 2》式 1000×760 横图、平涂色块、硬边赛璐璐阴影、强黑形、有限色板、夸张透视、单一清晰事件。不得提交写实、厚涂、电影概念图或高纹理金属风格。

## 3. 当前角色与系统概览

工程已有三个可玩角色：

- 守护者 Guardian：防御反击、固守、藏锋、盾戳等完整体系。
- 铁之眼 Ironeye：距离、标记、远射、隐毒、毒爆等完整体系。
- 复仇者 Revenant：家人召唤、呼唤、共鸣与大量祷告/雷电/兽爪牌。

守护者与铁之眼已经经历大量平衡、文本、动态预览、事件、遗物、药水与专属资产迭代。除非收到新反馈，不要根据旧对话重新改动它们。

## 4. 复仇者当前机制口径

这是最近反复调整最多的区域，继续开发时以此为最新口径，并同时核对当前代码：

### 呼唤与共鸣

- `呼唤`：从 3 个家人中选择 1 个召唤。
- 若已有家人在场，仍然执行三选一；选择后提升所选家人的最大生命值，提升量等同于当前家人的剩余生命值。
- 已死亡家人仍可选择；重新选择时应以其**初始生命值**复活并召唤，不使用“已保存最大生命值”阻止选择。
- 小化妆刷：战斗开始时，呼唤。
- 奏琴：2 费，呼唤；升级为 1 费。
- 共鸣：使你的召唤物立刻执行行动。
- 怨气：1 费，共鸣；升级添加保留。

### 家人

- 家人在复仇者回合开始时行动。
- 每个家人头顶显示行动意图，并以独立能力说明即将执行的行动；能力死亡后必须清理。
- 海伦初始生命 6：
  - 踏步刺击：对随机敌人造成 4 点伤害，抽 1 张牌。
  - 后撤：获得 1 点能量。
- 弗雷德利克初始生命 8：
  - 重锤：对随机敌人造成 8 点伤害并施加 1 层易伤。
  - 头槌：对随机敌人造成 8 点伤害 2 次。
- 塞巴斯蒂安初始生命 10：
  - 吼叫：给予所有敌人 1 层虚弱。
  - 拍击：对所有敌人造成 7 点伤害。
- 之前存在“强力行动”版本，但共鸣已简化为“召唤物立刻执行行动”；若代码仍残留强力行动分支，应按当前机制审慎清理。

## 5. 复仇者近期代码与美术状态

### 已建立的人物规范与官方参考

- `design/Revenant_角色设计与动作强制规范.md`
- `design/Revenant_角色选择界面强制规范.md` 已改为引用上面的身份规范。
- 官方参考：
  - `design/references/revenant_official_wide.png`
  - `design/references/revenant_official_icon.png`
- 官方页面：
  - `https://en.bandainamcoent.eu/elden-ring/elden-ring-nightreign/characters/revenant`
  - `https://en.bandainamcoent.eu/elden-ring/news/elden-ring-nightreign-the-official-starter-guide`

### 最近重置并已导出的复仇者人物资产

- `revenant_assets/character_select_revenant_bg.png`
- `revenant_assets/char_select_revenant.png`
- `revenant_assets/char_select_revenant_locked.png`
- `revenant_assets/character_icon_revenant.png`
- `revenant_assets/character_icon_revenant_outline.png`
- `revenant_assets/map_marker_revenant.png`
- `revenant_assets/combat/revenant_idle.png`
- `revenant_assets/combat/revenant_attack.png`
- `revenant_assets/combat/revenant_hit.png`
- `revenant_assets/merchant/revenant_merchant.png`
- `revenant_assets/rest_site/revenant_rest_site.png`
- QA 拼图：`revenant_assets/revenant_art_qa_contact_sheet.png`
- 处理脚本：`tools/process_revenant_character_art.py`
- 战斗 rig：`revenant_assets/combat/revenant_combat_rig.tscn`，已去除上下漂浮，只保留极轻微旋转。
- 上述透明资产已检查四角 alpha 为 0。

### 仍需警惕的复仇者区域

- 家人立绘、行动意图、行动能力图标、死亡清理、层级与复活逻辑此前多次出现错误；后续收到反馈时应优先检查：
  - `src/Core/Models/Revenant/RevenantSummonManager.cs`
  - `src/Core/Models/Power/RevenantSummonControllerPower.cs`
  - `src/Core/Models/Power/RevenantFamilyActionPowers.cs`
  - `src/Core/Models/Cards/RevenantStarterCards.cs`
  - `src/Core/Models/Relics/TempRevenantStarterRelic.cs`
- 复仇者卡牌与能力新增主要位于：
  - `src/Core/Models/Cards/RevenantCards.cs`
  - `src/Core/Models/Cards/RevenantAdvancedCards.cs`
  - `src/Core/Models/Power/RevenantAdvancedPowers.cs`
  - `src/Core/Models/Power/RevenantFreezePower.cs`
- 这些文件和大量 `revenant_assets/cards/*.png` 仍是未提交变更。用户此前明确否决过一批卡图，后续重做卡图必须先按规范自审，不能把现有生成图默认视为已通过。
- 除黄金律法、野兽爪痕、古龙信仰三张牌以卷轴为主题外，其他卡图不得因为参考图中有卷轴就画出卷轴。

## 6. 导出、同步与发布

正常导出并替换 Mods：

```powershell
powershell -ExecutionPolicy Bypass -File tools/export_guardian_mod.ps1
```

该脚本负责图标同步、本地化检查、Godot 导入与 PCK 导出、Release 编译、复制到游戏 Mods 目录并校验哈希。不要手工跳过同步步骤。

最近一次完整导出成功，导出物哈希为：

- `sts2mod.pck`：`1E95F25B695D4249448265A9260BA40E7649110AB38DB76C5847E9DE166DD61C`
- `sts2mod.json`：`6F6CA3742D4096843A65C44F22ACDFFD85EE61EBA2F2B43B978075C4D1A8244C`
- `sts2mod.dll`：`9AC8F282DB7721F989B37959B0E7CEF655A4B1CC0538D7B9C80BF6C04D49E781`
- `sts2mod.pdb`：`A2D385F81E519A53B4416642DA1359946626C25A7095573418DADC17A7D08FCF`

注意：曾在 `build/verify_obj`、`build/verify_bin` 做临时编译，导致项目通配编译重复引入 Assembly 属性。临时目录已删除。不要在 `build/` 下创建会被 C# glob 扫描的中间源码目录。

发布包规则：只有用户明确说“发包/发布版本”时才运行发布流程。包内保留 `sts2mod.deps.json`、DLL、JSON、PCK、PDB、runtimeconfig 以及 `config.json`。版本号按用户指示；此前多次要求沿用 `0.1`。

## 7. 当前 Git 状态与安全边界

- 当前存在大量 modified/untracked 文件，包含用户认可与尚未认可的混合成果。
- 不要使用 `git reset --hard`、`git checkout --` 或批量删除来“清理”。
- 项目已配置在线 Git 仓库；当前本地 `main` 比远端 ahead 5。
- 在提交或推送前必须先列出准确范围，避免把未审批美术误当正式资产提交。

## 8. 新任务启动检查单

1. 完整读取 `AGENTS.md` 和本交接文档。
2. 运行 `git status --short --branch`，保留所有现有修改。
3. 以用户在新任务中的最新指令为准，不要重新执行旧需求。
4. 涉及卡图/能力图标/角色身份时，先完整读取对应强制规范。
5. 修改后做与风险相称的编译、脚本检查和资源透明度检查。
6. 只有用户要求时才导出、替换、发包；不要启动游戏。

