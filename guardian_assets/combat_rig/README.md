# 守护者 Skeleton2D 原型

这是独立的纸偶式骨骼动画实验，不会替换当前游戏中稳定使用的铁甲战士战斗视觉。

## 内容

- `guardian_skeleton_prototype.tscn`：`Skeleton2D`、多级 `Bone2D`、切片精灵与动画。
- `guardian_skeleton_prototype.gd`：运行场景时循环播放待机、举盾和戟击。
- `parts/*.png`：从透明纸偶部件图切出的运行时部件。
- `guardian_halberd.svg`：按单侧非对称长刃规范重画的确定性矢量长戟。

完整纸偶图与绿色抠图源文件保存在 `design/骨骼动画预览/源文件`，不会进入运行包。

## 动画

- `idle_loop`：呼吸起伏、轻微抬头、翼与武器的低幅联动。
- `guard`：身体后压、盾臂抬正、翼收拢、持戟臂让位。
- `attack`：短促蓄力、根骨前送、上臂和前臂分段带动长戟重扫。

## 生成提示词摘要

使用 Codex 内置图像生成，以角色选择背景作为平面卡通风格参考，以官方守护者立绘作为身份和装备参考；在纯绿色背景上生成互不重叠的头、躯干、翼、手臂、盾与武器纸偶部件。初版生成武器因偏向大剑枪被拒绝，原型实际使用 `guardian_halberd.svg`。

## 预览

直接在 Godot 中运行本场景即可循环查看三段动画。命令行加用户参数 `capture=idle_loop`、`capture=guard` 或 `capture=attack` 时，会把对应姿态保存到 `design/骨骼动画预览`。

当前原型尚未替换战斗角色：原游戏的 `NCreature` 只会把 `Idle/Attack/Cast/Hit/Dead` 触发发送给 Spine 动画器，Godot `Skeleton2D` 不会自动收到这些触发。正式接入需要新增守护者视觉节点，并在补丁层把战斗触发映射到本场景的 `AnimationPlayer`。
