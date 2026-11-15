# 追击攻击修订：武器与圆盾

> v1–v3未通过用户的整条剑轴复核，不得继续称为“已拉直”或用于正式资源。新v4从身份参考重新构图，见 `PURSUIT_ATTACK_V4_PROMPT.md`。

## 第二次定向调整（v3）

v2已隐藏盾牌并更换分叉柄尾，但剑身仍偏短。v3保持人物和剑柄不变，沿原直线延长剑身，扩大画布。

候选：`design/角色资源预览/wylder/wylder_pursuit_attack_v3.png`。v1、v2均保留为过程原件，不覆盖运行资源。

v3原件：`C:/Users/wenti/.codex/generated_images/01a0f3d5-9b03-7243-a90e-88371d6c286a/exec-14c6f1d3-c559-4210-a1ad-fa901191ca7b.png`。工具仍生成了外围低透明度光雾；按此前要求保留原件，不进行本地Alpha清理。该候选尚未获批或用于正式资源。

```text
Precise object edit of this transparent Wylder attack sprite. The blade is STILL TOO SHORT. Change ONLY the blade length and canvas extent: extend the same dead-straight greatsword blade by FIFTY PERCENT (1.5 times current crossguard-to-tip length), along its existing straight down-right axis. Keep its base width, engraved design and straight parallel edges, tapering only near the new tip. Do not bend, kink or curve the sword. Keep the crossguard, BOTH hands, the distinctive BRANCHING FORKED POMMEL, handle, every character body part, proportions, stance, boots, helmet, costume details, palette and rendering IDENTICAL. The character must NOT grow or shrink relative to the sword hilt; extending canvas is mandatory. Put full character toward LEFT and the newly long sword across the RIGHT half of a wider, taller TRANSPARENT canvas. The resulting blade should look unmistakably like an exceptionally LONG straight greatsword, crossguard-to-tip length roughly matching or exceeding the character's helmet-to-boot height. Full new tip must be visible, ample margin, do not shorten sword to fit. NO visible buckler/shield anywhere. Genuine alpha transparency outside the character and sword with crisp antialiased edges, not black/gray smoke or a glowing backdrop. No floor, cast shadow, text, effects or extra objects. Preserve the detailed illustration; no flat-vector restyle.
```

## 首次定向修订（v2）

用户要求：本张攻击贴图不显示小圆盾；剑身加长并保持笔直，恢复附件中的标志性剑柄外形。保持已有双手斩击姿势与细节插画风格。

工具：内置imagegen。仅生成新候选，原图保留，不覆盖运行资源，不导出、不安装、不推送，不进行本地Alpha清理。

输入：v1追击攻击为编辑目标；用户剑柄局部图为柄尾外形参考；已通过普通idle仅作长直大剑及柄尾结构参考。

目标候选：`design/角色资源预览/wylder/wylder_pursuit_attack_v2.png`。

```text
Use case: precise-object-edit.
Asset: revised transparent full-body pursuit attack sprite for Wylder.
Image1: EDIT TARGET, the current two-handed slash. Keep character identity, head, helmet, torso, armor engravings, navy embroidered coat, boots, lunge, body proportions, lighting and detailed inked illustration style unchanged.
Image2: AUTHORITATIVE close-up of the distinctive weapon HILT END / POMMEL shape. It is not a generic round knob; replicate its recognisable branching silhouette. Ignore its checkerboard background.
Image3: Supporting reference of the same character's LONG STRAIGHT GREATSWORD and its distinctive hilt end. Do NOT copy image3's shield or idle pose.
Make only these corrections:
1. Remove the visible round buckler COMPLETELY from this attack texture, including the disk peeking behind shoulder. Reconstruct the short black scarf drape / back silhouette behind it. No shield visible anywhere, either in hands, on back or on ground. Do not remove left-forearm grappling bracer.
2. Replace the short bent blade with a MUCH LONGER, DEAD-STRAIGHT rigid steel GREATSWORD. Blade length from crossguard to tip should be about 1.1 times the character's helmet-to-boot body height, distinctly longer than image1. Broad parallel straight cutting edges for most of blade, clean pointed tip; a geometrically straight centerline. Hilt, ricasso, blade and tip must all share ONE continuous perfectly straight axis. No kink at crossguard, bow, curve, wavy edge, saber geometry, lens distortion or exaggerated foreshortening. Keep the same down-right slashing direction, but EXPAND THE TRANSPARENT CANVAS rightward and downward as needed so full sword length fits. Do not shorten or bend sword to fit existing canvas. Do not shrink only the sword.
3. Replace the generic round POMMEL with image2's distinctive sculpted branching end: a silver faceted central junction, a small ring/eyelet-like projection on the outer end, and two long swept hooked/winglike prongs extending to opposite sides, with their characteristic angular bends. This distinctive branching fitting belongs at the BUTT END of the long dark leather-wrapped handle, behind the rear hand. It is NOT an extra weapon and NOT a second crossguard near blade. Match the outer silhouette of image2, not a new invented disk, sphere or simple cross.
Grip: TWO gloved hands gripping adjacent parts of the SAME ONE long straight handle. Preserve the two-handed down-right cutting pose; fingers closed plausibly around grip, no hands on blade. Exactly one crossguard at blade base, exactly one distinctive forked pommel at opposite handle end, exactly one continuous greatsword. Do not add a second sword, free-ended handle stub, extra arm or detached fist.
Composition: generous transparent margins around helmet wings, coat, both boots, the long blade tip AND branching pommel. Entire figure and entire weapon visible, wide landscape transparent canvas. Same detailed art style and colors as image1, not flat vector or simplified cel shading.
Genuine alpha transparency, no checkers painted into background, no ground, shadow, mist, glow, slash trail, enemies, text or labels.
```
