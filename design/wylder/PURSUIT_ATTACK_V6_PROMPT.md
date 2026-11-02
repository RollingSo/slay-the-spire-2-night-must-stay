# 追击攻击v6：转体挥砍

> v6/v7均未通过人体—武器连接审查。用户明确标出柄尾从腋下冒出、长柄穿过手臂和躯干。撤回，不作为合格动作预览。v8仅重建双手与短柄，见 `PURSUIT_ATTACK_V8_PROMPT.md`。

## v7定向修订

v6改变为双臂挥出、肩髋转动的斜向切斩，不再使用v4横持动作。内部复核仍发现握柄与刃身延长线有角度差，因此另作v7，只校正武器主干及紧贴握柄的手指/手腕，不改变该转体动作。

新候选：`design/角色资源预览/wylder/wylder_pursuit_attack_v7.png`。仍待用户确认，不覆盖现有资源。

v7原件：`C:/Users/wenti/.codex/generated_images/01a0f3d5-9b03-7243-a90e-88371d6c286a/exec-497cef20-c813-4479-90fc-6d03910b118b.png`。采用双臂抬至胸肩前的转体斜向挥剑，弃用v4水平端剑；分叉柄尾可见，无圆盾。该记录不等于用户认可动作，也不把单次生成结果自行标为正式通过。

```text
Precise edit of this Wylder two-handed cutting sprite. KEEP the exact dynamic body pose, stepping legs, torso twist, arms' overall position, costume, helmet, coat motion and detailed illustration.
Correct ONLY the sword's axial alignment and the immediately gripping fingers/wrists as necessary. Keep the long blade at its existing straight diagonal angle (about 22 degrees upward toward screen right). Rebuild the leather grip and forked pommel on the EXACT EXTENSION of that blade centerline through the crossguard. The current long grip behind the arms is at a slightly different angle. Move its far-left forked pommel slightly DOWN so the pommel central junction, grip center, crossguard center and sword point are all collinear. Treat the sword as one indivisible straight rigid object, no bend hidden under hands, no kink at guard. Both fists must hold that ONE corrected handle, wrists adapt to the weapon. Preserve the characteristic three-branched sculpted silver pommel, including two swept winglike prongs and outward small eyelet; do not simplify it to a knob. No second handle or weapon.
No shield. Keep full blade length and complete tip. Keep front foot loaded, trailing foot extended, upper-body swing and cloth motion. Do NOT turn the action into an idle or static holding pose. Preserve true alpha and all clean margins; no fog, halo, backdrop or text.
```

## v6动作重建记录

内置imagegen生成，采用已批准角色细节风格、用户分叉柄尾近照，以及实际游戏挥砍帧的身体转动关系。

动作研究来源：[公开游戏动作截图页面](https://note.com/pyon_pyoko/n/n6e9163c24d6c)，仅用于转体、迈步、双臂带动长剑的动作关系；不复制截图人物、武器、场景或特效。

本地参考：`design/角色资源预览/wylder/pose_references/game_slash_pose_reference.jpg`。研究图不进入运行资源。

v4被用户否决僵硬动作；v5生成仍为圆头柄尾，内部排除。新稿不据提示词自行宣称通过，必须目视核对后再记录。

候选目标：`design/角色资源预览/wylder/wylder_pursuit_attack_v6.png`。待用户审图，未覆盖、导出或推送，原图保留。

```text
Create ONE dynamic two-handed greatsword ATTACK sprite of Wylder, transparent full body.
INPUT ROLES: Image1 = exact Wylder identity, outfit, detailed illustration style, sword design. Image2 = exact FORKED POMMEL shape, indispensable. Image3 = ONLY actual game SWING ACTION reference: body twisting with a passing step, both arms swept around with sword, clearly attacking rather than holding still. Do not copy image3 character, weapon, clothing, setting, slash effect or blur.
Draw the striking instant of a heavy, turning TWO-HANDED CUT toward screen RIGHT. Strong torso and hip rotation into the swing, near shoulder thrust forward, far shoulder drawn back, weight driving onto a planted bent front leg, rear foot pivoting on toes. Arms have whipped out ACROSS THE BODY near upper-chest/shoulder level with natural elbow bends; the chest turns with them. Long sword sweeps in a rising diagonal toward upper-right; do not lower both hands into a static shovel-holding position. The whole silhouette must show rotational momentum, not a symmetric horse stance or a straight-on posed mannequin. Navy skirt panels and scarf trail left behind body. The single frame must look like a sword slash even without any motion trail.
Both hands grip adjacent sections of ONE long continuous leather handle: one hand near crossguard, the other near BUTT END. Hands do NOT grip each other's wrists. At butt end draw the distinctive three-branch silver fitting from image2: central faceted junction, small outward hole-like tip, two long swept hooked prongs. This is NOT a round knob or coin. Make it visible and unmistakable behind rear fist.
ONE rigid straight greatsword. Pommel central axis, long grip, crossguard center, long engraved blade and point all lie on the SAME straight diagonal up-right. No angle change at guard. Align arms and wrists to the straight sword, not vice versa. Blade roughly as long as character's body height. Full sword and tip visible.
Preserve image1's fully enclosed engraved silver winged helmet, black visor, blue SIDE helmet panel, black scarf, silver V-bottom breastplate, asymmetrical engraved shoulders, scale sleeves, left grappling bracer, brown gloves, round belt buckle, navy embroidered knee-length coat, dark trousers, cord-wrapped calves and boots. Detailed inked shading, not simplified flat vector.
NO shield visible, no background, ground, shadow, aura, mist, glow, dust, slash trail, text, panels or extra weapons. Single full-body front/side three-quarter view facing RIGHT; enough transparent canvas for sword, cloak and feet.
```
