# 追踪者选择头像、背景与深蓝主题（2026-10-20）

用户批准本轮新头像和背景，授权覆盖并本地导出。内置imagegen生成两张新母图，后续仅进行引擎分辨率归一化和原生锁定头像灰度派生；没有用脚本重画人物。

## 规格与检查

- 对照已实际打开的守护者、铁之眼、复仇者、女爵四套选择资源。头像原四角均为132×195，追踪者旧288×400不一致；新正式普通/锁定头像均为132×195。
- 新背景2560×1200，姿势参考本地`10_角色选择立绘_Nightfarers/01_追踪者_wylder.png`：向左侧俯身低头、钩爪前臂横于胸前、剑背负于肩后。不是战斗待机扛剑姿势。
- 头像采用同套图形化平涂、硬边阴影；背景远景用大色块，保留银盔和钩爪的辨识细节。检查了完整封闭面甲、无橙眼、手部连接、单一背剑及分叉柄尾。
- 已检查源图和独立横向安全框，头部、手部、前臂钩爪及剑柄在x=12.5%–87.5%范围内；左侧43%留低对比环境。母图没有提前截断头、手和背剑柄。
- 场景根维持视口居中的-1280/+1280，待机Shader中心同步调整至body=(0.64,0.53)、secondary=(0.66,0.78)。
- `background_16x9_crop.png`是1920×1080离线检查图，`background_safe_area_qa.png`是横向安全框。它们不是游戏截图。真实16:9界面的UI遮挡与裁切验收仍待用户截图，不以离线图冒充已完成。

## 主题色

从已批准裤裙布料和本地官方立绘的蓝色像素交叉取样：战斗待机约#232941，追击攻击约#242C4F，官方立绘受光面约#373B4F；取居中的深海军蓝#252E4A作为统一主色，#111827为阴影，#8694B4为高光。卡框、地图绘线、牌组颜色、对话底色、目标线、拖牌轨迹及钩爪转场同步改为深蓝/银蓝系；姓名文字使用较浅的蓝灰保证可读性，不给实物装备强行染色。

## 文件

正式文件：`wylder_assets/char_select_wylder.png`、`char_select_wylder_locked.png`、`character_select_wylder_bg.png`。

母图保存在`wylder_assets/approved_sources/selection_portrait.png`与`selection_background.png`，原始生成文件和衍生预览保存在`design/角色资源预览/wylder/selection_20261020/`。审批与来源映射：`selection_assets.json`。`tools/prepare_wylder_selection.py --promote`可复现；历史批处理最后重新应用已批准选择图，避免老缓存覆盖新资源。

## 头像精确提示词

```text
Use case: stylized-concept. Create a NEW Wylder character-selection portrait for a Slay the Spire 2 mod. Images 1–4 are the four existing character-selection portraits and are STYLE/FRAMING REFERENCES ONLY. Image 5 is Wylder's official local character illustration and is the IDENTITY REFERENCE ONLY.
画面应与前四张头像是一套：竖幅132:195构图的头肩近景，头盔与上胸甲占据画面，平涂色块、清晰黑色轮廓、硬边赛璐璐阴影、少量明暗层次、低纹理，不是精细金属插画，不是照片或3D渲染。没有外框、字、UI和徽标。
追踪者完整封闭的银色翼盔，金属下半面甲与暗色视缝，拱形盔顶有简化流线雕纹，两侧为向上后方延展的金属羽翼；深蓝花纹只在头盔侧后，不能画成蓝布蒙脸，不露眼、不露肤、不画橙色发光眼睛。黑围巾包裹颈部，简化银色肩甲和深蓝衣料。三分之四侧脸朝屏幕左方，轮廓安静而警觉。近景中头部足够大，金属翼尖完整在画幅内；不要出现手、武器或全身。
背景用单纯深海军蓝 #29344E，接近追踪者裤裙的深蓝色。盔甲银灰与黑围巾在深蓝背景上保持清楚的明暗轮廓。沿用参考头像的图形化勾线和面块，而非母图写实纹理。无透明背景，画面填满。
```

## 背景精确提示词

```text
Use case: stylized-concept. Generate a NEW 2560×1200 landscape character-selection background for Wylder (追踪者), matching the four existing Nightfarer character-selection backgrounds.
Image 1 is the LOCAL OFFICIAL WY LDER ILLUSTRATION: use its exact character pose and equipment silhouette. Images 2 and 3 are STYLE + COMPOSITION references only, do not copy their characters. Image 4 is the newly drawn Wylder portrait: keep the same closed metal wing helmet and simplified cel-shaded visual identity.
姿势必须参考图1：身体三分之四侧向屏幕左，略俯身低头，左前臂的钩爪甲具横过胸前向画面右侧伸展，另一只手在胸口附近自然张开；大剑背负在肩后，长剑柄竖向延伸到头顶后方，标志性分叉柄尾可辨，不能改成扛剑站姿或攻击挥砍。不改变角色为正面摆拍。保留全封闭银色翼盔、暗色视缝与金属面甲、侧后蓝色纹饰、黑围巾、V形胸甲、不对称银肩甲、鳞甲袖、棕色皮手套、圆银腰扣，以及深蓝色银纹分片裤裙。不要露出眼睛、皮肤或蓝布面罩，不要橙色发光眼。前臂钩爪必须是一套连贯的银色机械装置，不是第二把弯剑；背后只有一把直大剑，不增加手持武器或前景巨盾。
画风要与图2和图3已有角色选择背景统一：图形化色块，硬边赛璐璐阴影，轮廓清晰，有限深蓝/银灰/黑色调，远景以大块形状概括。不是写实照片、油画、电影概念设计，也不要过密金属微纹理。银色与暗蓝布料在暗色环境中可辨。
构图：2560x1200横幅。角色主要身份核心放在画面x=58%–80%，头部约x=64%、y=28%，胸甲约x=66%、y=53%，主要伸手动作最远不超过x=82%；头盔顶翼和背剑柄都完整入画，顶部留出余量。人物取膝部以上，裙摆底部可自然出画，人物不能占满整幅画面，也不能贴右边。右侧x=87.5%–100%保留纯环境，不放脸、手、主武器或甲具。中央x=12.5%–87.5%裁切后仍能看到完整头部、双手和前臂钩爪轮廓。左侧0–43%保持低对比、安静、留给角色资料UI，不能放额外主体或明亮焦点。
场景：黑夜中的风吹石砌堡垒遗迹与远处沉暗的山丘，深蓝灰天空与破损城墙形成层次。角色脚下或低处有少量岩石与深色草形，不添加其他人物、怪物、火焰、发光法阵。背景和裤裙以深海军蓝#252E4A为主调，金属用克制的冷银亮面。参考图1姿势的垂落黑背布，不凭空加巨大飞舞羽翼。
只有一名追踪者。无UI、字、边框、水印。实色不透明完整背景。
```

