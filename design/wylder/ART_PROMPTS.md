# 追踪者首批美术提示词记录

生成方式：内置 imagegen。身份主参考为用户指定的官方追踪者立绘；首张待机图作为后续一致性参考。两张箭步卡图分别追加用户图2、图3作为姿势参考，原生打击卡图仅作平面风格参考。单边耳环使用用户图1。

## 角色通用约束

Graphic 2D hard cel shading, bold black outlines and large flat color planes.
Preserve Wylder's asymmetrical silver wing-shaped closed helmet, blue patterned face covering,
small orange eye, black scarf/mantle, navy long surcoat, silver shoulder plates,
leather gloves and boots, mechanical claw on the left forearm, straight steel greatsword
with simple crossguard and round pommel. No realistic metal, painterly texture or generic knight redesign.
Sprites use true-alpha transparent backgrounds with clean margins. No text, labels or sheets.

## 分资产视觉事件

- idle：Full-body 3/4 profile facing right, grounded ready stance, greatsword diagonally forward.
- attack：Lunging while swinging the greatsword horizontally forward to the right.
- hit：Recoil backwards slightly, shoulders turned, sword held low without losing grip.
- block：Brace on bent legs and hold the greatsword across the chest to parry; no shield.
- death：Collapse onto one knee, helmet bowed, glove braced on ground, greatsword beside him; no blood.
- rest：Seated on a low stone, leaning forward with the greatsword; helmet remains on.
- merchant：Stand calmly with the greatsword sheathed behind the shoulder and empty right hand relaxed.
- portrait：Single head-and-shoulders profile facing right; preserve the wing helmet and covered face.
- hands：One isolated vertically rising leather-gloved forearm, silver vambrace and navy sleeve; point, fist, open five-finger palm, and two-finger V generated separately. Explicit five-finger anatomy.
- energy：Orange-gold hooked claw curling around a silver sword tip, navy inner disk and angular silver rim, no text or numerals, readable at 24 pixels.
- pursuit：Silver grappling claw swooping toward one orange impact wedge, single diagonal symbol, readable at 32 pixels.
- break：Silver greatsword edge splitting a navy guard into two angular pieces at an amber impact star.
- suppression：Down-pointing kinked grey sword with two red downward chevrons, flat grey/red/black, no glow.
- single earring：Reference oval silver hanging pendant, thin upper loop, silver curling waves and muted blue inlay. Simplify to hard color planes, no realistic tarnish.

## 选择背景

2560x1200 landscape. Wylder at right-center, x=65%; preserve helmet, face covering, scarf, surcoat and sword.
Ruined stone arch silhouettes under a dark blue night sky with a narrow amber dawn.
Left 40% dark and quiet for UI. Essential head, hands and torso inside the central safe region.
Leave outer-right breathing room. Flat graphic 2D, hard cel shading, no UI/text/logos.

## 两张卡图

1000x760 landscape artwork only. Inputs: Wylder identity, supplied pose, original game graphic style.
Bold irregular black shadow shapes, flat color blocks, hard cel shading, limited palette,
exaggerated perspective, one event readable at thumbnail size. No frame, text, realistic texture or busy scenery.

- 上砍：Explosive rising greatsword uppercut in the supplied orientation, bent legs push up and torso twists; one broad white-amber rising slash against burgundy.
- 回旋斩：Low planted stamp and wide spinning greatsword sweep, bent front knee and coiling torso; one bold horizontal silver/lime-white arc against dark teal.

## 透明修订提示

Precise background extraction edit. Preserve foreground subject, pose, anatomy, shapes, colors and scale.
Remove all surrounding diffuse glow, fog, haze, cloud and cast shadow outside the hard contour.
Leave true alpha transparency to the clean antialiased outline and alpha-zero corners.
Do not redraw or recolor the foreground or add any new backdrop.

原生成结果保留，透明修订没有完全消除光雾的版本不作为已通过资产。源文件对应关系见 generated_sources.json，最终尺寸/透明验证见 asset_report.json。
