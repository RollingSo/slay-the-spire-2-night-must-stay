# animation_forge

家人/复仇者立绘动画的打包与闸门。**不要再用 .tmp 里的一次性脚本。**

## 规则（v03 起）

- **画布随动作**：每个动作按自己所有帧的并集包围盒 + 8px 边距开画布，不固定 512。
- **锚点入库**：SpriteFrames 的 meta 里有 `canvas`（Vector2i）和 `anchor`（Vector2），锚点是该动作中立帧的「地面接触点」。
- **接入**：`sprite.Offset = Vector2(canvas.x/2 - anchor.x, canvas.y/2 - anchor.y)`，节点本身只站在地面线。
- **尺寸按角色**：海伦 344 / 弗雷德利克 325 / 塞巴斯蒂安 331 / 复仇者 216，各自来自游戏内实际渲染，不要统一。

## 用法

```bash
# 闸门：复载一组 .res，检查裁剪 / 锚点 / 中立高 / 帧率
python tools/animation_forge/forge.py verify --dir design/动画预览/revenant/基准_v03

# 也可以检查运行时目录
python tools/animation_forge/forge.py verify --dir revenant_assets/families/animations/helen

# 打包：帧序列 → SpriteFrames（config 里给 frames_dir / cell / anchor / action / loop）
python tools/animation_forge/forge.py pack --config .tmp/canvas_v03/SE02_咆哮/pack_config.json
```

`pack` 需要 Pillow；用项目里的 `.tmp/revenant-extraction-venv/bin/python` 跑。

## 闸门检查什么

| 检查 | 判据 | 为什么 |
|---|---|---|
| 可加载 | `loadable` | 资源损坏或版本不兼容 |
| 裁剪 | 并集包围盒不贴任何一条边 | 这是 v02 栽过的坑：HE02 剑尖被切 |
| 锚点 | 中立帧底边 == anchor.y（±2） | 锚点算错，角色会悬空或陷地 |
| 中立高 | 与角色基准差 ≤3（例外登记在 `specs.json`） | 防御整体缩放算错 |
| 帧率 | 24 fps | 与源素材一致 |

失败时退出码非 0，可以直接挂到提交前检查。

## 新增一个动作的流程

1. 生成视频 → 取帧 → 抠图 → 归一化到该角色的基准高
2. 用很大的临时画布摆位，取并集包围盒 + 8px 得 `cell`，锚点 = 中立帧包围盒底边中点
3. `forge.py pack` 出 `.res`
4. `forge.py verify` 过闸门
5. 家人动作复制到 `revenant_assets/families/animations/<角色>/<clip>.res`（clip = idle / attack_a / attack_b / hurt）
6. 更新 `design/复仇者与家人_动画视频制作清单.md`
