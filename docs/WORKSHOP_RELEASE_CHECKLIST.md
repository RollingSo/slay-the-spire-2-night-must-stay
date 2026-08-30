# NightMustStay 创意工坊发布清单

## 1. Workshop 内容目录

《杀戮尖塔 2》的 Mod 加载器会递归读取内容目录中的每个 `.json`，并把它们当作 Mod manifest 尝试解析。因此创意工坊内容目录应保持精简：

```text
NightMustStay.json
NightMustStay.dll
NightMustStay.pck
NightMustStay.pdb   # 可选，便于用户提交可定位的崩溃日志
```

- `NightMustStay.json` 内的 `id` 必须是 `NightMustStay`。
- DLL 与 PCK 必须严格使用 `<id>.dll`、`<id>.pck` 命名。
- 不要把 `config.json`、`NightMustStay.deps.json` 或 `NightMustStay.runtimeconfig.json` 放入 Workshop 内容目录；这些额外 JSON 会被加载器扫描为 manifest。
- 发布 zip 仍可按 `PACKAGING.md` 保留完整运行时文件集，但不能直接把完整 zip 解压结果当作 Workshop 内容目录。
- 遥测发布默认配置已嵌入 `NightMustStay.dll`。玩家可以用 `%APPDATA%\SlayTheSpire2\night_must_stay\config.json` 覆盖它；游戏设置中的“数据上传”关闭时，本 Mod 同样不会上传。

## 2. 版本与兼容性

- 每次更新前修改 `manifest.json` 的语义化版本号。
- 确认是否添加 `min_game_version`；本地当前用于开发的游戏版本是 `0.107.1`。
- 当前公开版本同时支持 Production/正式版与 Public Beta 分支；每次发布前必须分别编译验证。
- 创意工坊项目的最低/最高游戏分支均留空，表示支持所有分支；页面说明明确列出已验证的 Production 与 Public Beta。
- Mod ID 从旧的 `sts2mod` 改为 `NightMustStay` 后，Steam 会把它视为不同的 Mod 身份。发布前需要测试旧存档能否加载；不要再同时安装旧 ID 与新 ID。
- 开发机本地测试包使用独立 ID `NightMustStayBetaTest` 与显示后缀 `[Beta Test]`。它只用于区分本地测试和线上稳定版，禁止与 Workshop 版本同时启用。

## 3. 页面素材

- 准备一张清晰的 Workshop 主预览图，避免直接使用游戏截图裁出的临时 UI。
- 准备至少 4–6 张实机截图：三名角色选择页、战斗界面、卡牌/遗物图鉴和多人模式。
- 中英文标题与简介需明确说明：新增角色、主要机制、当前版本、支持语言、是否影响游戏玩法。
- 写明依赖项、冲突项、已知问题、更新日志、反馈方式和卸载方式。
- 初次上传设为仅自己可见或好友可见，完成订阅安装测试后再公开。

## 4. 权利与合规

- 发布者必须确认对代码、卡图、立绘、音频、字体、Logo 和第三方素材拥有足够的发布权利。
- 本项目包含《ELDEN RING NIGHTREIGN》角色与设定衍生内容，上架前必须逐项确认万代南梦宫/FromSoftware 素材的使用边界；直接复用官方资产的风险高于原创同人绘制。
- 页面应注明这是非官方、非商业的粉丝 Mod，并列出相应作品及权利人的归属说明。该声明不能替代素材授权。
- 不要在 Workshop 包里包含基础游戏程序集、解包出的原游戏资源、Steam 凭据或服务器私钥。

## 5. 数据收集与隐私

- 当前 Mod 可上传匿名化战局数据；Workshop 版本从 DLL 内置配置读取发布默认值，并尊重游戏设置的数据上传总开关。
- 公开发布前必须提供明确的用户告知、开关、隐私说明、数据字段、接收地址、保存期限和删除/退出方式。
- 不应通过把 `config.json` 直接放进 Workshop 内容目录来启用采集；这会与游戏的 manifest 扫描规则冲突。

## 6. 发布前测试

1. 运行 `tools/export_guardian_mod.ps1`，确认校验、Godot 导入、PCK、Release 编译和哈希检查通过。
2. 确认 Mods 目录只存在一套活动文件：`NightMustStay.json/.dll/.pck`。
3. 在仅安装 Workshop 测试副本的干净环境中启动游戏，确认 Mod 菜单能识别 `NightMustStay`。
4. 分别验证中英文、本地单人、多人、保存并继续、胜利、死亡、切换章节和一局正常结束。
5. 检查 `%APPDATA%\SlayTheSpire2\logs\godot.log`，确保没有缺失本地化、重复 Mod ID、资源路径或程序集加载错误。
6. 从 Steam 订阅、更新、取消订阅各测试一次，确认旧文件不会残留。

## 7. 上传资料

- 运行 `powershell -NoProfile -ExecutionPolicy Bypass -File tools/stage_workshop_release.ps1` 生成不含额外 JSON 的 Workshop 内容目录。
- App ID：`2868840`。
- SteamCMD/上传工具需要内容目录、主预览图、标题、描述、可见度、变更说明和已发布项目 ID。
- 首次创建后保存 `PublishedFileId`，后续更新必须复用该 ID，避免重复创建多个 Workshop 项目。
- 首次公开前接受 Steam 创意工坊法律协议。
