# 打包发布规范（Night Must Stay）

本文档说明如何正确编译、打包、发布本 mod。
**不遵守本文档会导致：玩家装了 mod 但功能缺失——尤其是数据上报会静默不工作，且没有任何报错。**

> 建议：每次发布新版本前，按第 5 节"验证清单"逐项检查。

---

## 1. 编译

```bash
dotnet build sts2mod.csproj -c Release --ignore-failed-sources
```

前提：
- 需要 .NET SDK 9
- 需要本地 `sts2dll/` 目录（被 .gitignore 排除，不随仓库分发），内含：
  - `sts2.dll`（游戏主程序集）
  - `0Harmony.dll`
  - `GodotSharp.dll`
  - 来源：游戏安装目录 `data_sts2_windows_x86_64/` 下复制

编译产物：`.godot/mono/temp/bin/Release/sts2mod.dll`

## 2. 发布文件清单（缺一不可）

发布 zip / 创意工坊 / 任何分发形式时，以下文件**必须全部包含**，并放置在玩家游戏目录的 `mods/` 下（与 mods/ 下其他文件平铺）：

```
mods/
├── sts2mod.dll               # 编译产物（必须 Release 版）
├── sts2mod.pck               # 资源包（如有资源）
├── sts2mod.json              # mod 清单（id/name/version...）
├── sts2mod.runtimeconfig.json
├── sts2mod.deps.json
├── sts2mod.pdb               # 可选（调试符号）
└── config.json               # ⚠️ 数据上报配置（与 dll 同目录！见第 3 节）
```

> 原则：**config.json 与 sts2mod.dll 放在同一个目录**（mod 装到哪，配置就跟到哪，层级自然正确）。
> 旧版结构 `mods/sts2mod/config.json`（子目录）仍然兼容，但新发布请使用与 dll 同目录的平铺方式。

## 3. config.json（最容易漏，漏了 = 数据上报静默禁用）

- **位置**：与 `sts2mod.dll` **同一目录**（如 `mods/config.json`）——采集器优先读取 dll 所在目录的 `config.json`
- **内容**（模板见仓库根目录 `config.json`）：

```json
{
  "serverUrl": "http://8.138.196.248:8000/",
  "token": "NMS-ingest-7f3a9c2e5b8d4a61",
  "enabled": true
}
```

- 打包时把仓库根目录的 `config.json` 与 dll 放在一起即可
- **缺少此文件时**：采集器检测不到配置会**静默禁用**，游戏内无任何报错，但玩家的战局数据不会上报

### 3.1 安全说明（重要：不要删除仓库中的 config.json 模板）

- `config.json` 是 mod 的**客户端运行时配置**，不是服务器密钥
- 其中的 `token` 只是"防路人乱传数据"的接入标识——它**随 mod 分发给所有玩家**，玩家打开配置文件就能看到，本身就不是机密（服务器端真正的防滥用靠限流、请求大小限制等，与 token 无关）
- **删除仓库中的 `config.json` 会导致下次打包没有模板可用 → 发布遗漏此文件 → 全部玩家数据上报静默失效**（没有任何报错，极难排查）
- 如果担心 token 暴露：正确做法是**轮换 token**（服务器端和客户端模板同步更新），而不是删除模板文件

## 4. 版本号（发布新版本时必须更新）

- `manifest.json` 的 `version` 字段是 mod 版本号的**唯一来源**
- 数据上报 payload 中的 `modVersion` 字段**自动从 manifest.json 读取**（`TelemetryService.GetModVersion`），不要写死
- **每次发版：先更新 `manifest.json` 的 `version`，再编译、再发布**

## 5. 发布前验证清单

1. ✅ `dotnet build sts2mod.csproj -c Release --ignore-failed-sources` 0 错误
2. ✅ 文件清单完整（含 `sts2mod/config.json`）
3. ✅ `manifest.json` 的 version 已更新
4. ✅ 本机实测：游戏内打完一局（**胜利或死亡**；注意：从主菜单"放弃"的局不触发上报，属正常）后，检查游戏日志：
   - 日志路径：`%APPDATA%\SlayTheSpire2\logs\godot.log`
   - 应包含：`[NMS] 数据上报已启用: <serverUrl>` 和 `[NMS] 战局数据上传成功`
   - 服务器端确认：接收端日志出现 `POST ok`

## 6. 常见问题速查

| 现象 | 原因 |
|---|---|
| godot.log 中完全没有 `[NMS]` 日志 | mod 未加载，或 dll 不是含采集代码的版本 |
| godot.log 显示"数据上报未启用" | `config.json` 缺失 / 不在 dll 同目录 / `enabled: false` |
| 打了局但服务器没收到 | 战局是从主菜单**放弃**的（放弃局不触发上报，官方同源） |
| `modVersion` 显示 unknown | `manifest.json` 的 version 为空，或 mod id 不是 `sts2mod` |
| 玩家改了配置导致不上报 | 正常：`config.json` 的 `enabled: false` 或游戏设置关闭数据上传时，采集器尊重开关 |

## 7. 数据上报工作原理（简要）

- 战局正常结束（胜利/死亡）→ 游戏触发官方 `ModManager.OnMetricsUpload` 事件
- 采集器 `TelemetryService` 订阅该事件 → 组装 payload（含角色/版本/选牌/事件/胜负等）→ 匿名化（`run.Anonymized()`，清除 Steam ID）→ 写入本地队列 → 异步上传到 `serverUrl`
- 上传失败自动本地保留，下次启动/下局自动补传
- 玩家在游戏设置中关闭"数据上传"或 `config.json` 的 `enabled: false` 时，采集器不工作
