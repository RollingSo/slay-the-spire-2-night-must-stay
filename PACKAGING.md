# 打包发布规范（Night Must Stay）

本文档说明如何正确编译、打包、发布本 mod。
**不遵守本文档会导致：玩家装了 mod 但功能缺失——尤其是数据上报会静默不工作，且没有任何报错。**

> 建议：每次发布新版本前，按第 5 节"验证清单"逐项检查。

---

## 1. 编译

```bash
dotnet build NightMustStay.csproj -c Release --ignore-failed-sources
```

前提：
- 需要 .NET SDK 9
- 需要本地 `sts2dll/` 目录（被 .gitignore 排除，不随仓库分发），内含：
  - `sts2.dll`（游戏主程序集）
  - `0Harmony.dll`
  - `GodotSharp.dll`
  - 来源：游戏安装目录 `data_sts2_windows_x86_64/` 下复制

编译产物：`.godot/mono/temp/bin/Release/NightMustStay.dll`

## 2. 发布文件清单（缺一不可）

发布 zip / 创意工坊 / 任何分发形式时，以下文件**必须全部包含**，并放置在玩家游戏目录的 `mods/` 下（与 mods/ 下其他文件平铺）：

```
mods/
├── NightMustStay.dll               # 编译产物（必须 Release 版）
├── NightMustStay.pck               # 资源包（如有资源）
├── NightMustStay.json              # mod 清单（id/name/version...）
└── NightMustStay.pdb               # 可选（调试符号）
```

> 创意工坊目录不要放置 `config.json`、`.deps.json` 或 `.runtimeconfig.json`。游戏会递归扫描其中的 JSON 并将其当作 Mod 清单。

## 3. 遥测配置

- 发布默认配置由构建系统从仓库根目录 `config.json` 嵌入 `NightMustStay.dll`，Workshop 安装无需额外配置文件。
- 玩家覆盖配置位于 `%APPDATA%\SlayTheSpire2\night_must_stay\config.json`；存在时优先于内置配置。
- 玩家可把 `enabled` 改为 `false` 关闭本 Mod 的遥测。游戏设置中的“数据上传”总开关关闭时也不会上传。
- 配置内容：

```json
{
  "serverUrl": "http://8.138.196.248:8000/",
  "token": "NMS-ingest-7f3a9c2e5b8d4a61",
  "enabled": true
}
```

- `serverUrl` 或 `enabled` 改动后必须重新编译，才会更新 DLL 内置的发布默认值。

### 3.1 安全说明（重要：不要删除仓库中的 config.json 模板）

- `config.json` 是 mod 的**客户端运行时配置**，不是服务器密钥
- 其中的 `token` 只是"防路人乱传数据"的接入标识——它**随 mod 分发给所有玩家**，玩家打开配置文件就能看到，本身就不是机密（服务器端真正的防滥用靠限流、请求大小限制等，与 token 无关）
- 不要删除仓库中的 `config.json`；它是 DLL 内置默认配置的构建输入。
- 如果担心 token 暴露：正确做法是**轮换 token**（服务器端和客户端模板同步更新），而不是删除模板文件

## 4. 版本号（发布新版本时必须更新）

- `manifest.json` 的 `version` 字段是 mod 版本号的**唯一来源**
- 数据上报 payload 中的 `modVersion` 字段**自动从 manifest.json 读取**（`TelemetryService.GetModVersion`），不要写死
- **每次发版：先更新 `manifest.json` 的 `version`，再编译、再发布**

## 5. 发布前验证清单

1. ✅ `dotnet build NightMustStay.csproj -c Release --ignore-failed-sources` 0 错误
2. ✅ Workshop 文件清单仅包含 `NightMustStay.dll`、`NightMustStay.pck`、`NightMustStay.json` 与可选的 `NightMustStay.pdb`
3. ✅ `manifest.json` 的 version 已更新
4. ✅ 本机实测：分别完成胜利、死亡、战斗中主动放弃和主菜单放弃存档，检查游戏日志：
   - 日志路径：`%APPDATA%\SlayTheSpire2\logs\godot.log`
   - 应包含：`[NMS] 数据上报已启用: <serverUrl>` 和 `[NMS] 战局数据上传成功`
   - 服务器端确认：接收端日志出现 `POST ok`

## 6. 常见问题速查

| 现象 | 原因 |
|---|---|
| godot.log 中完全没有 `[NMS]` 日志 | mod 未加载，或 dll 不是含采集代码的版本 |
| godot.log 显示“数据上报未启用” | AppData 覆盖配置或 DLL 内置配置的 `enabled: false` / `serverUrl` 为空 |
| 放弃战局但服务器没收到 | 检查游戏“数据上传”开关和 `[NMS]` 日志；放弃战局由 RunHistory 补丁采集，不依赖官方跳过放弃局的指标上传 |
| `modVersion` 显示 unknown | `manifest.json` 的 version 为空，或 mod id 不是 `NightMustStay` |
| 玩家改了配置导致不上报 | 正常：`config.json` 的 `enabled: false` 或游戏设置关闭数据上传时，采集器尊重开关 |

## 7. 数据上报工作原理（简要）

- 战局正常结束（胜利/死亡）→ 游戏触发官方 `ModManager.OnMetricsUpload` 事件
- 主动放弃（战斗中、单人主菜单或多人主菜单）→ 创建 `isAbandoned=true` 的战局历史时由补丁采集
- 采集器 `TelemetryService` 订阅该事件 → 组装 payload（含角色/版本/选牌/事件/胜负等）→ 匿名化（`run.Anonymized()`，清除 Steam ID）→ 写入本地队列 → 异步上传到 `serverUrl`
- 上传失败自动本地保留，下次启动/下局自动补传
- 玩家在游戏设置中关闭"数据上传"或 `config.json` 的 `enabled: false` 时，采集器不工作
