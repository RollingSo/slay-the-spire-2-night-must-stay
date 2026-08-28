using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace NightMustStay.Core.Telemetry;

/// <summary>
/// Night Must Stay 战局数据上报服务。
/// 订阅官方 ModManager.OnMetricsUpload 事件（战局结束自动触发，数据与官方埋点同源），
/// 匿名化后异步上传到配置的服务器。
///
/// 健壮性设计（面向大众安装）：
///  - 完全异步，不阻塞游戏主线程；
///  - 所有路径 try-catch，绝不向游戏抛出异常；
///  - 本地队列 + 启动补传：先落盘再发送，成功才删，断网/闪退不丢数据；
///  - 尊重游戏设置的数据上传开关（PrefsSave.UploadData），玩家关闭则不上传；
///  - payload 大小上限保护，防止异常数据打到服务器。
/// </summary>
public static class TelemetryService
{
    private const string ConfigFileName = "config.json";
    private const string EmbeddedConfigName = "NightMustStay.telemetry.config.json";
    private const string PendingPrefix = "pending_";
    private const int MaxPayloadBytes = 512 * 1024;   // 单局 payload 上限（正常 ~30-120KB）
    private const int MaxPendingFiles = 50;           // 本地补传队列上限（防无限积压）

    private static readonly HttpClient _http = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(10)
    };
    private static readonly object _pendingLock = new object();
    private static readonly HashSet<string> _queuedRuns = new HashSet<string>();

    private static string _dataDir = "";
    private static string _serverUrl = "";
    private static string _token = "";
    private static bool _enabled;
    private static bool _initialized;

    /// <summary>由 ModInitializer 调用一次。</summary>
    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }
        _initialized = true;
        try
        {
            // STS2 recursively scans every JSON file below Mods as a manifest.
            // Keep ordinary configuration and retry queues in user data.
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _dataDir = Path.Combine(appData, "SlayTheSpire2", "night_must_stay");
            LoadConfig();

            if (!_enabled || string.IsNullOrEmpty(_serverUrl))
            {
                Log.Info("[NMS] 数据上报未启用（未配置 serverUrl 或已关闭）", 2);
                return;
            }

            ModManager.OnMetricsUpload += OnRunFinished;
            Log.Info("[NMS] 数据上报已启用: " + _serverUrl, 2);

            // 启动时补传上次未发送成功的队列
            _ = Task.Run(FlushPendingAsync);
        }
        catch (Exception ex)
        {
            Log.Warn("[NMS] 数据上报初始化失败: " + ex.Message, 2);
        }
    }

    private static void LoadConfig()
    {
        string cfgPath = Path.Combine(_dataDir, ConfigFileName);
        try
        {
            string configJson;
            string configSource;
            if (File.Exists(cfgPath))
            {
                configJson = File.ReadAllText(cfgPath);
                configSource = cfgPath;
            }
            else
            {
                using Stream stream = typeof(TelemetryService).Assembly.GetManifestResourceStream(EmbeddedConfigName);
                if (stream == null)
                {
                    Log.Warn("[NMS] 未找到内置数据上报配置，数据上报已禁用", 2);
                    return;
                }
                using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                configJson = reader.ReadToEnd();
                configSource = "embedded";
            }

            using JsonDocument doc = JsonDocument.Parse(configJson);
            JsonElement root = doc.RootElement;
            _serverUrl = root.TryGetProperty("serverUrl", out JsonElement u) ? u.GetString() ?? "" : "";
            _token = root.TryGetProperty("token", out JsonElement t) ? t.GetString() ?? "" : "";
            // enabled 缺省为 true（写了配置文件就默认开）
            _enabled = !root.TryGetProperty("enabled", out JsonElement e) || e.GetBoolean();
            Log.Info("[NMS] 数据上报配置来源: " + configSource, 2);
        }
        catch (Exception ex)
        {
            Log.Warn("[NMS] config.json 解析失败，数据上报已禁用: " + ex.Message, 2);
            _enabled = false;
        }
    }

    /// <summary>游戏主线程回调（战局结束）。只做组装与落盘，发送放后台。</summary>
    private static void OnRunFinished(SerializableRun run, bool isVictory, ulong localPlayerId)
    {
        CaptureRun(run, isVictory, isAbandoned: false, localPlayerId);
    }

    /// <summary>由放弃战局历史补丁调用。</summary>
    public static void CaptureAbandonedRun(SerializableRun run, PlatformType platformType)
    {
        ulong localPlayerId = 0;
        try
        {
            localPlayerId = PlatformUtil.GetLocalPlayerId(platformType);
        }
        catch (Exception ex)
        {
            Log.Warn("[NMS] 获取放弃战局的本地玩家 ID 失败，将从存档回退: " + ex.Message, 2);
        }

        if (run.Players != null && run.Players.Count > 0 && !run.Players.Any(p => p.NetId == localPlayerId))
        {
            localPlayerId = run.Players[0].NetId;
        }

        CaptureRun(run, isVictory: false, isAbandoned: true, localPlayerId);
    }

    private static void CaptureRun(SerializableRun run, bool isVictory, bool isAbandoned, ulong localPlayerId)
    {
        try
        {
            if (!_enabled || string.IsNullOrEmpty(_serverUrl))
            {
                return;
            }

            // 隐私与合规：玩家在游戏设置里关闭了官方数据上传，我们也不传
            if (!SaveManager.Instance.PrefsSave.UploadData)
            {
                return;
            }

            string runKey = string.Join("|", run.StartTime, run.SerializableRng?.Seed ?? "", localPlayerId, isAbandoned);
            lock (_pendingLock)
            {
                if (!_queuedRuns.Add(runKey))
                {
                    Log.Info("[NMS] 跳过重复战局数据: " + runKey, 2);
                    return;
                }
            }

            string json = BuildPayload(run, isVictory, isAbandoned, localPlayerId);
            if (json == null)
            {
                return;
            }

            // 先落盘（防丢），再异步发送
            string pendingFile = WritePending(json);
            _ = Task.Run(() => SendAsync(pendingFile, json));
        }
        catch (Exception ex)
        {
            Log.Warn("[NMS] 战局数据采集失败: " + ex.Message, 2);
        }
    }

    private static string BuildPayload(SerializableRun run, bool isVictory, bool isAbandoned, ulong localPlayerId)
    {
        try
        {
            // 角色信息：本地玩家角色 + 全队角色（多人局）
            string character = "";
            List<string> team = new List<string>();
            if (run.Players != null)
            {
                foreach (SerializablePlayer p in run.Players)
                {
                    string id = p.CharacterId?.Entry ?? "unknown";
                    if (p.NetId == localPlayerId)
                    {
                        character = id;
                    }
                    team.Add(id);
                }
            }

            var payload = new
            {
                source = "night-must-stay",
                modVersion = GetModVersion(),   // 从 mod manifest 自动读取，不写死
                gameVersion = ReleaseInfoManager.Instance.ReleaseInfo?.Version ?? "unknown",
                schemaVersion = run.SchemaVersion,   // 数据结构版本（如 9），随版本记录
                isVictory,
                isAbandoned,
                localPlayerId,
                character,            // 本地玩家角色，如 CARD.GUARDIAN / CHARACTER.GUARDIAN
                team,                 // 全队角色（多人局）
                floor = run.FloorReached,
                ascension = run.Ascension,
                runTime = run.RunTime,
                gameMode = run.GameMode.ToString(),
                // Anonymized(): 官方匿名化，去掉玩家名等个人信息
                mapPointHistory = run.Anonymized().MapPointHistory,
                acts = run.Acts,
                modifiers = run.Modifiers
            };
            string json = JsonSerializer.Serialize(payload);
            if (Encoding.UTF8.GetByteCount(json) > MaxPayloadBytes)
            {
                Log.Warn("[NMS] 本局数据超限，已丢弃（保护服务器）", 2);
                return null;
            }
            return json;
        }
        catch (Exception ex)
        {
            Log.Warn("[NMS] payload 组装失败: " + ex.Message, 2);
            return null;
        }
    }

    /// <summary>从 ModManager 读取本 mod 的 manifest 版本号（不写死，随发布自动更新）。</summary>
    private static string GetModVersion()
    {
        try
        {
            foreach (Mod m in ModManager.Mods)
            {
                if (m.manifest != null && m.manifest.id == "NightMustStay" && !string.IsNullOrEmpty(m.manifest.version))
                {
                    return m.manifest.version;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn("[NMS] 读取 mod 版本失败: " + ex.Message, 2);
        }
        return "unknown";
    }

    private static string WritePending(string json)
    {
        lock (_pendingLock)
        {
            try
            {
                Directory.CreateDirectory(_dataDir);
                // 清理过期的排队文件（防无限积压）
                string[] existing = Directory.GetFiles(_dataDir, PendingPrefix + "*.json");
                if (existing.Length >= MaxPendingFiles)
                {
                    Array.Sort(existing);
                    for (int i = 0; i < existing.Length - MaxPendingFiles + 1; i++)
                    {
                        TryDelete(existing[i]);
                    }
                }
                string file = Path.Combine(_dataDir, PendingPrefix + Guid.NewGuid().ToString("N") + ".json");
                File.WriteAllText(file, json, Encoding.UTF8);
                return file;
            }
            catch (Exception ex)
            {
                Log.Warn("[NMS] 本地排队失败: " + ex.Message, 2);
                return null;
            }
        }
    }

    private static async Task SendAsync(string pendingFile, string json)
    {
        try
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            if (!string.IsNullOrEmpty(_token))
            {
                content.Headers.Add("X-Token", _token);
            }
            HttpResponseMessage resp = await _http.PostAsync(_serverUrl, content);
            if (resp.IsSuccessStatusCode)
            {
                TryDelete(pendingFile);
                Log.Info("[NMS] 战局数据上传成功", 2);
            }
            else
            {
                Log.Warn("[NMS] 上传失败 HTTP " + (int)resp.StatusCode + "，已留在本地队列，下次启动补传", 2);
            }
        }
        catch (Exception ex)
        {
            Log.Warn("[NMS] 上传异常: " + ex.Message + "，已留在本地队列，下次启动补传", 2);
        }
    }

    private static void FlushPendingAsync()
    {
        try
        {
            string[] files = Directory.GetFiles(_dataDir, PendingPrefix + "*.json");
            foreach (string file in files)
            {
                try
                {
                    string json = File.ReadAllText(file, Encoding.UTF8);
                    SendAsync(file, json).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Log.Warn("[NMS] 补传失败: " + ex.Message, 2);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn("[NMS] 补传扫描失败: " + ex.Message, 2);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // 删除失败不影响主流程
        }
    }
}

[HarmonyPatch(typeof(RunHistoryUtilities), nameof(RunHistoryUtilities.CreateRunHistoryEntry))]
internal static class AbandonedRunTelemetryPatch
{
    private static void Prefix(SerializableRun run, bool isAbandoned, PlatformType platformType)
    {
        if (isAbandoned)
        {
            TelemetryService.CaptureAbandonedRun(run, platformType);
        }
    }
}
