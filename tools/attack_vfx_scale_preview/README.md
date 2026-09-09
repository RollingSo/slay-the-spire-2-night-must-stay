# 全量粒子特效渲染与审查

该隔离 Godot 项目直接编译生产环境的绘图类，覆盖全部 22 类战斗特效、防御反击和光环的四档伤害，以及标记出现/常驻两种状态。不会启动游戏或改写安装目录。

先设置 `NUGET_PACKAGES` 为本机已有的 NuGet 缓存，再执行：

```powershell
dotnet build tools/attack_vfx_scale_preview/AttackVfxScalePreview.csproj --no-restore
& $GodotPath --path tools/attack_vfx_scale_preview --log-file "$PWD/.tmp/vfx-scale/verify.log" --fixed-fps 60 -- --verify
& $GodotPath --path tools/attack_vfx_scale_preview --log-file "$PWD/.tmp/vfx-scale/movie.log" --fixed-fps 24 -- --movie
python tools/attack_vfx_scale_preview/assemble_preview_gifs.py
```

`$GodotPath` 为本机 Godot 4.5.1 Mono 可执行文件。提前创建 `.tmp/vfx-scale`。同一个项目的验证与录像应依次运行，等待前一个 Godot 进程退出；同时检查日志中没有 `triangulation failed`、`C# backtrace` 或对象泄漏，不能只看进程退出码。

`--verify` 每种状态绘制 121 帧，在生命周期的 10% 至 90% 取九帧测量，选择 Alpha > 0.10 的像素数量最大帧。攻击最低覆盖为 330×245、有效像素 30,000，九帧中至少四帧有效像素 ≥12,000。该门槛是依据原版施毒量级拟定的本项目审查标准，并非原版常量。标记为长期读数，按独立的收束与可辨认性标准检查。还有箭尖/投射物双向起终点、极端伤害数值、伤害成长与 22 个实际节点释放检查。

当前预览使用生成贴图、生产 Shader 与真实 `GpuParticles2D`。`Seek` 根据目标时间步长调整原生粒子速度，使用固定 seed，不会以静态图片代替粒子。验证时额外隐藏主体 Sprite，在三个时间点单独渲染原生粒子，检查至少两个有效帧的可见范围随时间改变，兼容投石等延迟爆发。本机 GLES 驱动不支持 `CaptureRect` 的 GPU 缓冲读取，因此采用实际像素检查。

可在 `--verify` 或 `--movie` 后追加 `--pack=C:/absolute/path/NightMustStay.pck`，检查四张图集从导出包加载；不带该参数时读取工作区原图。每次应等待 Godot 进程完成再启动下一次录像/审查。

输出 `design/特效预览/particle_remake_20260910`：全量对照 PNG、测量 JSON、逐项审查 JSON 和四段 GIF。PNG 每格严格同为 0.65 倍战斗画布尺寸；伤害对比增加格子宽高以容纳高伤害效果。灰色矩形仅为统一尺度标尺，不代表某个真实角色。GIF 为生产代码的实际渲染，飞行距离在预览中统一为 210 画布单位以适配分格。

原版施毒参照可使用 `measure_native_poison.gd`，在 `--` 后传本机游戏 PCK。脚本读取原版粒子，仅为隔离运行移除场景控制器，临时文件写入忽略目录 `.tmp`。原版测量使用固定粒子 seed；不要把原版资源加入 Mod。

旧版几何特效审查：`design/三角色全量特效重制审查_20260910.md`。当前粒子与贴图版：`design/三角色贴图粒子特效重制_20260910.md`。图像生成方式及完整提示词：`images/vfx/particle_remake/GENERATION.md`。
