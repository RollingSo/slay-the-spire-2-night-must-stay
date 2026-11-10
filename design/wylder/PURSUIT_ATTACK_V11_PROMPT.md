# 追击攻击v11：以握柄轴线修正剑身

内置imagegen局部编辑。v10仍有折角，不通过。此次以握柄为固定基准，仅要求剑身改变角度，剑尖上移。保留角色动作与透明通道，不覆盖运行资源。

结果：实际输出未充分执行剑尖上移要求，握柄与剑身仍有角度差，内部复核不通过。保存为 `design/角色资源预览/wylder/wylder_pursuit_attack_v11_rejected.png`。没有修改正式资源。

## 精确提示词

```text
Use case: precise-object-edit. The input is an existing transparent combat sprite. Make ONE surgical geometric correction to the SWORD BLADE ONLY. Preserve the character and the TWO HANDS, handle, forked pommel, crossguard, pose, armor, cape, feet, canvas, rendering style unchanged.
The straight handle currently descends diagonally right at about 31 degrees, but the blade drops more steeply about 36 degrees. This is an incorrectly bent sword at the crossguard. FIX THE BLADE TO MATCH THE HANDLE, do not preserve the blade's old angle.
Extend the handle's central straight axis FORWARD THROUGH THE CROSSGUARD TO THE TIP. Pivot/redraw the long rigid engraved BLADE around the crossguard's center so its tip moves UP about 80 pixels, with almost unchanged horizontal endpoint and unchanged length. In the approximately 1509x1042 input, pommel main-axis center roughly(602,360), hand-held grip around(708,424), crossguard center roughly(826,496), and NEW TIP roughly(1480,893). These lie on ONE straight projected line. The OLD tip near(1480,973) MUST NOT remain in place.
Crucial: blade center ridge must be exactly the forward extension of the narrow HANDLE beneath the two fists. No kink at guard; no bowed blade. Preserve the long engraved silver steel blade design, taper and point; rotate the whole blade as a single rigid object, not warp it. Remove the old lower blade entirely, leaving transparent alpha in vacated region. Only one blade and one tip. This is still a diagonal downward right cutting attack. Keep both fists and natural connected arms as drawn; do not add new hands, redraw torso, shorten sword, change forked pommel, or add buckler.
Final: same single full-body character, genuinely transparent background, no guides, red lines, text or effects.
```
