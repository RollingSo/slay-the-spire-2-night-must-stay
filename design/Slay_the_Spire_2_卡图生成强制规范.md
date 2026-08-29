# 《Slay the Spire 2》卡图生成强制规范

> 本规范来自对 `D:\STS2\images\packed\card_portraits` 中游戏原始卡图的直接审阅。
> 后续守护者 Mod 的所有卡图草案、生成提示词和成图审核均以本文件为准。

## 1. 结论先行

《Slay the Spire 2》的卡图核心不是写实黑暗奇幻厚涂，而是：

- 图形化、扁平化的 2D 奇幻插画；
- 粗壮、尖锐、略带不规则感的轮廓；
- 大面积纯色块和硬边分面明暗；
- 黑色阴影作为独立图形参与构图；
- 荧光色轮廓、撞击星芒和箭头等漫画化视觉符号；
- 单一、夸张、缩略后仍可立刻识别的视觉事件。

禁止把“卡图质量高”理解为材质更真实、纹理更多、光影更电影化。官方卡图的完成度来自形状设计、色彩组织和动作可读性。

## 2. 原图依据

本次重点审阅了以下原始资源：

- 基础攻击与防御：
  - `ironclad/strike_ironclad.png`
  - `ironclad/defend_ironclad.png`
  - `silent/strike_silent.png`
  - `silent/defend_silent.png`
  - `defect/strike_defect.png`
  - `defect/defend_defect.png`
  - `necrobinder/strike_necrobinder.png`
  - `regent/strike_regent.png`
- 技能、能力与复杂叙事：
  - `ironclad/barricade.png`
  - `ironclad/inflame.png`
  - `ironclad/demon_form.png`
  - `ironclad/fiend_fire.png`
  - `silent/footwork.png`
  - `silent/grand_finale.png`
  - `silent/nightmare.png`
  - `silent/phantom_blades.png`
  - `defect/meteor_strike.png`
  - `defect/echo_form.png`
  - `defect/creative_ai.png`
  - `necrobinder/reaper_form.png`
  - `necrobinder/grave_warden.png`
  - `regent/big_bang.png`
  - `regent/the_smith.png`
  - `regent/void_form.png`
  - `regent/tyranny.png`

上述普通、稀有及常规卡池正式卡图的统一尺寸为 **1000×760 px**，横宽比约为 **1.316:1**。

### 2.1 先古卡图尺寸例外

先古牌不是横向卡图。对原游戏全部 `CardRarity.Ancient` 卡牌及其资源逐项核对后，正式先古卡图统一使用：

- **606×852 px**；
- 纵向比例约为 **0.711:1**；
- 画面必须从上到下完整填满，不得把横图居中后用纯色、模糊背景或延展色带补齐；
- 不得先生成 1000×760 横图再粗暴裁成竖图，必须从构图阶段就按 606×852 纵向画布设计；
- 先古牌仍遵守本规范的图形化平涂、硬边赛璐璐、大黑色形状、有限色盘和单一视觉事件要求。

核对样本包括 `apotheosis`、`apparition`、`biased_cognition`、`break`、`corruption`、`forbidden_grimoire`、`meteor_shower`、`protector`、`quadcast`、`the_sealed_throne`、`wraith_form` 等，资源尺寸均为 606×852。

## 3. 造型语言

### 3.1 轮廓优先

- 主体应占画面约 60%–90%，允许武器、肢体和特效被画面边缘裁切。
- 先确保纯黑剪影可读，再添加内部颜色。
- 轮廓以大弧线、大折线和尖角构成，避免写实的小起伏。
- 动作必须有明确方向：斜冲、下砸、横扫、收缩防御或向外爆发。
- 透视可以夸张；近处的脚、盾、拳、武器可显著放大。

### 3.2 结构简化

- 每个主要物体只拆成少数大块结构。
- 单个材质通常只使用基色、暗面、亮面和极少量高光。
- 盔甲不画密集划痕、铆钉、链甲和真实反射。
- 羽毛不逐根刻画，而是归纳成成组的锯齿形大块。
- 石头、木头、金属均用棱角分面表现，不追求照片级表面。

### 3.3 黑色的用法

- 黑色不是自然阴影的结果，而是主动设计的形状。
- 可用大块纯黑切开肢体、披风、盾牌和背景，强化轮廓。
- 主体背光侧允许直接压成近黑色，不必保留全部细节。

## 4. 色彩与光影

每张卡优先采用以下有限调色结构：

1. 一个深色背景基调；
2. 一个主体主色；
3. 一个高饱和强调色；
4. 少量近白色撞击光或高光。

具体要求：

- 大色块边界清晰，明暗转折以硬边为主。
- 渐变只用于背景或能量光晕，不能支配主体材质。
- 使用高饱和轮廓光把主体从背景中切出，例如青、红、黄绿或冰蓝。
- 高光可以不符合真实光学，但必须服务于形状识别。
- 避免全画面同一冷暖、同一亮度的“电影截图感”。

## 5. 构图与叙事

### 5.1 一张卡只讲一个瞬间

卡图应能用一句短语概括，例如：

- 刀刃横贯画面；
- 三道攻击撞上盾牌；
- 脚步落地，地面爆开；
- 能量在双手之间聚合；
- 幽灵手臂从斗篷后展开。

如果一句话中出现多个“然后”，画面通常过于复杂。

### 5.2 背景必须退后

- 基础卡和普通卡多使用抽象色块、速度线、烟雾形或极简空间。
- 背景通常不承担地点写实，不画完整战场全景。
- 复杂场景也应保持舞台化，只保留帮助理解机制的道具或角色。
- 不使用雨滴、瓦砾、远景军队、云层和材质噪点同时堆叠的概念图式背景。
- 卡图中不得直接出现卡牌实体、卡框、卡背或可被识别为卡牌的矩形轮廓；抽牌、保留、弃牌等机制必须改用动作、轨迹、光点、羽片或其他抽象符号表达。

### 5.3 机制视觉化

- 攻击：斜线、尖角、撞击星芒、武器轨迹、断裂形状。
- 防御：正面阻挡、进入画面的攻击箭头、盾或屏障占据大面积。
- 蓄势/姿态：重心、脚步、身体扭转、环形力场或明确的准备动作。
- 能力：对称、光环、重复幻影、持续性领域或符号化空间。
- 多段攻击：重复轮廓或平行轨迹，不画成写实残影摄影。

## 6. 守护者角色专用规范

守护者必须保留：

- 鸟类头部和明显鸟喙；
- 厚重银灰铠甲；
- 巨盾、长戟和羽翼三类身份符号中的至少一类；
- 老兵式沉重、可靠的力量感。

同时必须做风格化简化：

- 鸟头采用 3–5 个主要色块，不刻画真实羽毛纹理。
- 盔甲由大块银灰、蓝灰、近黑色分面组成。
- 盾牌轮廓要比内部纹章重要。
- 身体比例可夸张：宽肩、大手、大脚、短而有力的躯干。
- 普通卡不要每张都同时出现盾、长戟、双翼和完整全身。
- 角色不得变成写实鹰人、电影级重甲骑士或神圣天使。

建议角色色彩锚点：

- 铠甲基色：低饱和银灰、蓝灰；
- 深部阴影：近黑海军蓝；
- 身份强调：冷青色轮廓；
- 攻击/冲击强调：橙红或亮黄；
- 稀有神圣效果：少量金色，但避免柔和圣光。

## 7. 不再使用的提示词

以下词语容易把生成结果推向错误方向，后续提示词中默认禁用：

- `painterly fantasy concept art`
- `cinematic realism`
- `photorealistic`
- `highly detailed armor`
- `intricate metal texture`
- `realistic lighting`
- `epic battlefield`
- `volumetric rain and fog`
- `dark souls style`
- `Elden Ring style`
- `8k highly detailed`

`dark fantasy` 只能描述题材气质，不能作为主要视觉风格词单独使用。

## 8. 标准生成提示词骨架

后续每张卡先根据卡表机制写“视觉事件”，再套用以下骨架：

```text
Asset: 1000x760 landscape card illustration, artwork only.

Visual event: [只写一个清晰瞬间].
Subject: [守护者或关键物体，说明必须出现的身份特征].

Graphic 2D fantasy card illustration with chunky angular silhouettes,
flat color blocks, hard-edged cel shading, bold irregular black shadow shapes,
limited palette, exaggerated perspective, stylized impact symbols,
and one bright colored rim light separating the subject from the background.

Composition: one dominant focal shape filling most of the frame;
strong diagonal or centered graphic arrangement; readable as a small thumbnail;
intentional cropping at the frame edges; abstract minimal background.

Materials are simplified into 2–4 hard-edged color planes.
No realistic texture rendering. No fine feather detail.

Do not include: card frame, UI, text, numbers, logo, watermark,
photorealism, painterly brushwork, cinematic concept-art rendering,
realistic metal reflections, dense environmental detail, tiny decorative details.
```

先古牌不得使用上述横图尺寸行，必须替换为：

```text
Asset: 606x852 portrait Ancient card illustration, artwork only.
Composition: native portrait composition filling the entire canvas from top to bottom;
do not adapt, letterbox, pad, or crop a landscape illustration.
```

## 9. “踏地架势”正确方向示例

上一版错误点：完整角色、真实铠甲、雨夜战场、金属划痕、尘土粒子和电影光照共同抢占注意力，缩略后只剩“写实鹰人骑士”，没有《Slay the Spire 2》的图形语言。

根据守护者 Steel Guard 的实际触发动作，本卡不是“猛踩地面”，而是从普通格挡通过一次短促垫步进入强力防御。

正确的视觉事件应压缩为：

> 守护者横向垫步的同时把高而窄的巨盾抬到身体正前方；双脚仍前后错开，鸟头从盾沿后警觉观察，身体正向盾后收拢。

建议构图：

- 高盾占据画面右侧或中央的大部分面积，盾面正从侧向转为正面；
- 前后脚明显错开，用一条横向擦地色块表达短促垫步，不画地裂或冲击波；
- 鸟头从盾上缘后露出，持盾肩向前压，收拢羽翼形成后方深色大形；
- 可用 2–3 根即将接触盾面的白色攻击箭头强化动作目的，但不表现撞击结果；
- 背景使用深蓝、蓝黑的大弧形色块，主体以冷青描边分离；
- 不出现雨、月亮、完整战场、写实金属或精细羽毛；
- 卡图只表达“垫步举盾进入架势”，不同时表演格挡反击。

## 10. 审核门槛

每张预览提交审批前必须通过以下检查：

- [ ] 常规卡图尺寸或最终裁切比例为 1000×760；先古卡图必须为原生纵向 606×852。
- [ ] 缩小到约 250×190 时，仍能一眼说出视觉事件。
- [ ] 主体由大色块和硬边分面组成，而非厚涂纹理。
- [ ] 主体轮廓明确，并有黑色形状或高饱和描边参与分离。
- [ ] 背景不比主体复杂。
- [ ] 只有一个主要动作或机制焦点。
- [ ] 没有写实金属、逐根羽毛、雨雾颗粒或电影概念图光影。
- [ ] 没有卡框、文字、数字、Logo 或水印。
- [ ] 画面内部没有卡牌实体、卡背或卡牌轮廓。
- [ ] 守护者身份在缩略图中仍可辨认。
- [ ] 与相邻卡图的主要轮廓和强调色有明显区别。

任一关键项不通过，都不得作为正式预览提交。

## 11. 后续工作流程

1. 从飞书唯一卡表读取卡名、类型、稀有度和机制。
2. 把机制压缩成一句“视觉事件”。
3. 从原图中选择 2–4 张同机制/同构图的参考，只学习共同语言，不复制具体内容。
4. 按本规范生成首版，禁止使用旧版厚涂提示词。
5. 生成后同时检查原尺寸和 25% 缩略图。
6. 不合格时只针对一个问题迭代，例如轮廓、背景复杂度或色块数量。
7. 用户审批通过后再进入正式资源处理。
