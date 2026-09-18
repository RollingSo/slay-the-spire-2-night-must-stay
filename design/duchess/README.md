# 女爵：本地开发设计基准

`cards.json` 记录当前已实现的女爵卡牌快照。后续卡牌修订以项目 `AGENTS.md` 指定的飞书卡表为权威来源；对接步骤见 `HANDOFF.md`。不修改飞书，除非用户明确授权。

## 定位

银色短发、遮眼面具、深蓝燕尾外衣、白色裤装与短刃。温和而坚定的组织者，不是轻佻盗贼或穿长裙的法师。

女爵以独立的“时刻”资源、抽牌时触发的“反应”和衍生牌“闪避”为核心。原型版的舞步、隐蔽与伤害重演账本均已删除；“重演”现在只是一张初始攻击牌的牌名，不再代表一套独立资源。

- **时刻**：每回合开始时为1。每打出1张牌，时刻增加1；在牌面标注的准确时刻打出卡牌，触发额外效果。时刻由女爵自己的隐藏能力保存，不读取或修改储君的辉星。
- **反应**：在自己的回合进行中从抽牌堆抽到此牌时，本次打出耗能减少1。
- **闪避**：1费无色衍生技能牌；消耗、反应；获得6点格挡并抽1张牌。升级后获得9点格挡。

## 卡池与构筑

`cards.json` 为有序卡表：[类名后缀、中文名、英文名、日文名、耗能、类型、稀有度、效果列表、可选参数]。每个效果为 `[类型、基础数值、升级数值、可选条件]`。

75个独立模型：4基础、68常规奖励、2先古、1衍生。基础、先古、衍生牌不进入常规奖励。

主要运转方式：

1. 把手牌或弃牌堆的牌洗回抽牌堆，再继续抽牌，反复触发反应。
2. 通过闪避建立防御，并用抽牌把闪避转化为节奏推进。
3. 使用额外推进、设为3或回溯回合开始牌堆与费用状态的卡牌校准时刻。
4. 时刻3至7均有对应牌；要求更高时刻的奖励整体更强。

初始数值为66生命、99金币。初始牌组为4打击、4防御、优雅身段、重演：

- **优雅身段**：1费技能；将3张闪避洗入抽牌堆，抽2张牌；升级后洗入闪避+。
- **重演**：0费攻击；保留；造成4点伤害；时刻5时，本回合每造成过3点生命伤害，本牌伤害增加1。升级后基础伤害为6。
- **旧怀表**：时刻每次从5以下到达5时，抽1张牌。

## 技术约束

- 时刻保存在每名玩家各自的 `DuchessMomentPower.Amount`，所有变更通过 `PowerCmd` 执行，以保持多人同步。
- 时刻不复用 `PlayerCombatState.Stars`，获得、消耗或显示辉星均不影响时刻。
- 女爵复制 `NStarCounter` 的界面外壳生成第二个独立控件，并把复制件改为怀表和时刻提示；原辉星控件、辉星刷新与辉星悬停说明保持不变。
- 时刻牌只在数值完全相等时触发，并复用原版金色发光接口提示。
- 反应只在自己的出牌阶段、卡牌从抽牌堆进入手牌时触发，费用减免持续到打出。
- 完美预演记录回合开始后的牌堆归属与已结算费用；回溯时移除本回合生成牌、恢复原牌堆归属与费用，并将时刻设为3。
- 牌名、动态变量、升级数值和三语完整描述通过 `tools/generate_duchess_cards.py` 从本表导出。
- `localization.json` 包含人工撰写的三语角色、能力、遗物和药水文本，未使用机器翻译服务。
- 先古牙齿的两条转换为重演→永不落幕的重演、优雅身段→初光；怀表精炼为修复的怀表。

## 验证

```powershell
python tools/generate_duchess_cards.py --check
dotnet build NightMustStay.csproj -c Debug --no-restore -p:Sts2AssemblyDir='D:\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64'
dotnet build tools/duchess_model_tests/duchess_model_tests.csproj -c Debug --no-restore
dotnet tools/duchess_model_tests/bin/Debug/net9.0/duchess_model_tests.dll
pwsh -NoProfile -File tools/validate_card_text_format.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/validate_localization_parity.ps1
```

成功编译和隔离模型测试不能替代实机验收。角色选择、怀表位置、战斗、多人、保存恢复与不同游戏分支仍须按 `TESTING.md` 检查。
