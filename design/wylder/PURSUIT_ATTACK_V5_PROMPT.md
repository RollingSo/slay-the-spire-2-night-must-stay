# 追击攻击v5：重做发力动作

> v5内部审查未通过：模型仍把分叉柄尾画成普通圆头，不能作为合格候选。过程图保存为 `wylder_pursuit_attack_v5_rejected.png`，不覆盖运行资源。

v4被用户否决：横持大剑的姿势僵硬，没有挥砍动作。不得继续当作通过动作审查的版本。

工具：内置imagegen。以已批准普通待机固定身份和细节画风，不沿用v4横持姿势。新稿仍为待审候选，未覆盖运行资源。

## 新参考

- 教材出版社动作分解：[Fighting with the German Longsword — Special Edition](https://www.freelanceacademypress.com/fightingwiththegermanlongsword-special.aspx)，出版社公开示例图片 `https://www.freelanceacademypress.com/images/products/detail/02.11.jpg`。
- 本地研究参考：`design/角色资源预览/wylder/pose_references/oberhau_sequence_reference.jpg`。只学习步法、肩髋配合和双手动作，不复制页面、现代人物或服装到游戏素材。
- 另查阅[完整挥砍轨迹示意](https://www.hema101.com/post/meyer-s-longsword-101-chapters-5-6-parrying-handworks-and-the-withdrawal)，本地图仅为研究记录，不进入运行资源，不替代实际人物动作参考。
- 用户分叉柄尾近照仍是武器造型依据。

## 本轮要求

向右迈步斜劈；前腿承重，后脚跟随推进，肩髋转动、躯干随动作前倾。手臂配合直剑，不为了水平轴线僵化人物。小圆盾不显示。剑柄至剑尖仍须整条同轴，保持分叉柄尾。

候选目标：`design/角色资源预览/wylder/wylder_pursuit_attack_v5.png`。

## 精确提示词

```text
Asset: one new full-body Wylder pursuit attack sprite, true transparent background.
Image1 is ONLY character/style/equipment identity reference (not pose): keep this exact enclosed engraved silver wing helmet, dark visor, blue SIDE/BACK panel, black scarf, V-shaped silver chest plate, asymmetric shoulders, silver scale sleeves, left grappling bracer, navy embroidered split coat, cord-bound brown boots. Keep detailed inked rendering; no simplified flat-vector restyle.
Image2 is a historical swordsmanship book's movement reference, ONLY for coordinated stepping, shoulder turn and two-handed cutting mechanics. Do not reproduce page, panels, lettering, modern man or clothes. Image3 fixes the characteristic forked silver pommel.

ACTION: a forceful TWO-HANDED descending DIAGONAL CUT at the actual striking moment. Not an idle, guard, demonstration, holding sword for display, or thrust. Wylder advances a natural passing step toward SCREEN RIGHT: right foot has landed forward beneath the bent knee bearing the weight; left leg trails with the heel lifting as hips drive through. Athletic but NOT an absurdly wide sideways squat. Torso rotates into the cut and inclines forward about 20 degrees over forward leg. Right shoulder drives forward; trailing shoulder turns back. Head faces target to right. Both arms have swung forward and DOWN from the right shoulder, elbows naturally flexed with wrists aligned; hands now moving past the front of torso toward leading hip. Show whole-body momentum, not a sword pasted onto a rigid upright mannequin. Coat tails whip backward, not static triangular spread.
One greatsword makes a strong long diagonal DOWN-RIGHT. Both hands grip the same hilt naturally, right hand near guard, left toward pommel; left hand pulls the hilt as right guides the cut. The hands and elbows must adapt to a RIGID STRAIGHT weapon. Entire sword axis from forked pommel center through grip, crossguard center, blade center to point is one straight diagonal, approximately 30 degrees down-right. No kink at guard, no curve in grip or blade. Nearly parallel straight edges until final short taper. Whole blade about character's body height, match identity reference proportions. Blade visibly sweeps across the opponent's space in front of him; not behind his head and not through his legs.
One distinctive forked branching silver butt pommel matching image3, not a round ball or disk; separate ordinary crossguard at blade base. Exactly one handle, one blade, two coherent gloved hands, no duplicate grip stub.
NO shield visible anywhere in this attack sprite. No extra weapon, eyes, skin, background, ground, platform, particles, glow, aura, haze, arrows, motion trails or text.
View: front three-quarter facing screen-right; full figure and long sword tip fit inside a wide canvas with transparent margins. Single dynamic animation key pose, not a sheet.
```
