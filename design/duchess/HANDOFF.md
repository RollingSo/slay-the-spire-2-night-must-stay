# 女爵分支对接说明

## 范围与权威来源

本分支保存女爵目前的开发快照：64 张牌、7 件遗物、3 种药水、角色和战斗资源、三语本地化、生成脚本及验证工具。它尚未通过实机、联机和平衡验收，不应当作已发布版本。

当前项目 `AGENTS.md` 要求以飞书卡表为唯一权威卡牌列表。`cards.json` 和由它生成的完整卡表记录的是本分支已实现的本地快照；继续修订卡牌前，应先核对飞书，并遵循 `design/卡牌文本与机制实现强制规范.md`。不要改动飞书，除非用户明确授权。

## 文件入口

| 用途 | 路径 |
|---|---|
| 当前规格和进度 | `design/duchess/README.md`、`STATUS.md` |
| 完整卡表和生成数据 | `design/duchess/女爵时刻版完整卡表.md`、`cards.json` |
| 人工编写的非卡牌文本源 | `design/duchess/localization.json` |
| 三语游戏文本 | `NightMustStay/localization/{zhs,eng,jpn}/*.json` 中的 `DUCHESS` 键 |
| 卡牌生成器 | `tools/generate_duchess_cards.py` |
| 卡牌实现 | `src/Core/Models/Cards/DuchessCard.cs`、`DuchessCards.Generated.cs` |
| 时刻与 UI | `src/Core/Models/Power/DuchessPowers.cs`、`src/Core/Patches/DuchessMomentPatch.cs`、`DuchessAssetPatch.cs` |
| 角色及奖池 | `src/Core/Models/Characters/Duchess.cs`、女爵 CardPool、RelicPool、PotionPool |
| 战斗与界面资源 | `duchess_assets/`、`images/packed/card_portraits/duchess/`、女爵图集、图标和材质 |
| 验收项目 | `design/duchess/TESTING.md` |

共享注册位于 `ModelDbCharacterPatch.cs`，共享事件和进度接入位于 `Guardian*Patch.cs`、`SteamRichPresencePatch.cs`。`export_presets.cfg` 包含女爵资源导出规则。合并时逐项检查这些共享文件，避免覆盖其他角色的并行修改。

## 机制边界

- 时刻存放在每名玩家自己的 `DuchessMomentPower` 中，经 `PowerCmd` 同步；与储君辉星完全独立。
- 反应仅在自己的出牌阶段从抽牌堆抽到牌时减费，减费持续到本次打出。
- 闪避为 1 费无色衍生技能，具有消耗和反应；基础获得 6 格挡并抽 1，升级为 9 格挡并抽 1。
- 时刻牌只在当前时刻与标记数字完全相等时触发。旧怀表在时刻到达 5 时抽 1。
- `完美预演` 的牌堆和费用回溯是重点风险，详见验收清单。

## 验证和对接顺序

1. 核对飞书卡表与当前本地快照，并阅读适用的强制规范。
2. 运行 `python tools/generate_duchess_cards.py --check`，确认生成文件和卡表一致。
3. 按 `README.md` 的命令运行 Debug 构建、女爵模型测试、卡牌文本格式及三语一致性检查。资源检查脚本为 `tools/validate_duchess_assets.ps1` 和 `tools/validate_duchess_resources.gd`。
4. 依据 `TESTING.md` 在 Stable 和 Public Beta 实机检查 UI、牌堆回溯、保存恢复及多人同步。自动化通过不代表这些项目已完成。

本分支未包含本地游戏程序集、构建产物或测试安装包。所需程序集由开发者本机的《杀戮尖塔 2》安装提供。女爵之外的工作区改动不属于本分支。

### 当前基线的构建前置条件

分支所基于的本地 `main` 提交 `8802cd2` 已在 `RevenantAdvancedCards.cs` 引用 `RevenantAttackEffects`，但该类的实现文件仍是工作区中尚未提交的 Revenant 改动。女爵分支没有收录这些无关改动，因此单独检出时 Debug 编译会在该 Revenant 引用处报 `CS0103`。本次工作区中的 Debug 构建及女爵模型测试通过，但这不能替代分支独立构建。对接时先从负责 Revenant 的分支合并该依赖，再重新运行构建；不要从当前工作区随意复制其他角色文件进女爵分支。
