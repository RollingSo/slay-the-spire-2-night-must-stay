# 韩语本地化（kor）

## 范围

以远端 main 的 `c9b4901`（0.2.15）为基线，覆盖发布版守护者、铁之眼、复仇者。原工作区尚未发布的女爵开发内容不在这次发布版本地化范围内。

新增 `NightMustStay/localization/kor/` 下的 8 张表，共 1,717 个条目：

| 表 | 条目数 |
| --- | ---: |
| cards | 1,020 |
| powers | 236 |
| relics | 85 |
| potions | 19 |
| characters | 42 |
| ancients | 312 |
| card_library | 3 |
| events | 0（与源表一致） |

由 Codex 直接翻译现有中英文文本，未调用机器翻译服务。没有修改卡牌清单、伤害、费用、掉落率或飞书表。

## 术语

原版术语与语言目录以本机 `SlayTheSpire2.pck` 的 `localization/kor` 为准，包括 수비、방어도、약화、취약、민첩、보존、소멸、휘발성、선천성、잉크투성이、탄스、아키텍트。

| 模组术语 | 韩语 |
| --- | --- |
| 守护者 / 铁之眼 / 复仇者 | 수호자 / 철의 눈 / 복수자 |
| 固守 / 防御反击 / 藏锋 / 合成 | 철벽 / 가드 카운터 / 숨긴 칼날 / 합성 |
| 距离 / 远射 / 标记 / 潜毒 / 毒爆 | 거리 / 장거리 사격 / 표식 / 잠복독 / 독폭발 |
| 蓄力 / 回收 / 呼唤 / 共鸣 | 충전 / 회수 / 부름 / 공명 |
| 家人 / 死灵 / 冻伤 | 가족 / 사령 / 동상 |
| 海伦 / 弗雷德利克 / 塞巴斯蒂安 | 헬렌 / 프레드릭 / 세바스찬 |

夜渡者名称、家人名称与祷告名称参考韩语资料交叉核对：[角色资料](https://eldenring.inven.co.kr/dataninfo/class/)、[复仇者与家人](https://eldenring.inven.co.kr/dataninfo/class/?boardidx=286&comeidx=5871)、[古龙信仰祷告](https://eldenring.inven.co.kr/dataninfo/spell/?bidx=306482)、[守护者与救世之翼](https://www.inven.co.kr/board/eldenring/5871/280)。新增的模组机制采用上表固定译名，不宣称为原版官方术语。

角色对话遵循项目文本规范：守护者礼貌且完整，铁之眼简短专业，复仇者克制而强硬。保留原有旁白、对话选项和文本特效标签。

## 配套修正

- 守护者的名字匹配加入韩语 `수비`。所有韩语卡名的匹配集合与中文设计一致；验证脚本覆盖 24 个相关名称。没有借本次本地化调整现有其他语言的匹配行为。
- 卡牌与家人能力的补充悬浮说明支持韩语机制名。
- 距离达到上限、下限的气泡提示改用本地化键，四种语言均补齐。
- 韩语升级栏使用完整规则和动态变量，不复用旧表的“伤害 5→7”摘要；保留蓄力状态文本和生成牌升级分支。
- 韩语先天、保留等原生关键词由引擎显示，不在升级正文重复添加。
- 正常导出已接入韩语校验。原有导出过滤器已覆盖 `NightMustStay/**/*.json`，无需新增字体或改动资源 ID。

## 验证

从此工作树根目录运行：

```powershell
& tools/validate_localization_parity.ps1
& tools/validate_card_text_format.ps1
& tools/validate_korean_localization.ps1
dotnet run --project tools/korean_localization_tests -- .
dotnet build NightMustStay.csproj '-p:Sts2AssemblyDir=D:\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64'
dotnet run --project tools/revenant_damage_tests
git diff --check
```

本次以上检查均通过。编译为 0 警告、0 错误；原生格式化测试验证 1,717 个条目在基础、升级、升级预览和战斗内外的 10,302 种组合，检查变量解析及可见文本中的中日英文残留。测试使用游戏自身的格式化器及变量类型，不启动游戏、不访问存档。

尚未进行完整游戏内排版、角色选择及多人实机测试，也未安装或覆盖本地 mods。导出测试包时使用项目正常的 `tools/export_guardian_mod.ps1`；进入游戏后将语言设为 한국어。

本次开发在独立工作树 `.tmp/korean-localization`、分支 `codex/korean-localization-20260913` 完成；原目录的未提交开发内容保留不动。韩语本地化及其配套验证文件单独提交到远端 main。
