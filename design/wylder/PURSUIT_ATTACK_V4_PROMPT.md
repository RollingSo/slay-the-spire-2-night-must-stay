# 追击攻击 v4：重建横斩

> 用户已否决动作：看起来是横持摆架势，不是有发力过程的挥砍。v4不作为通过候选；后续重新设计动作见 `PURSUIT_ATTACK_V5_PROMPT.md`。

工具：内置imagegen。重新以通过的普通待机固定身份，不沿用v1–v3的折角剑。

候选：`design/角色资源预览/wylder/wylder_pursuit_attack_v4.png`。
原件：`C:/Users/wenti/.codex/generated_images/01a0f3d5-9b03-7243-a90e-88371d6c286a/exec-9c0cbc8b-a33c-4af3-a58a-a95e8e7e3f72.png`。

将动作调整为双手横斩，使柄尾中央、握柄、护手中央、剑身中轴、剑尖可按同一条近水平轴线检查。保留分叉柄尾，不显示圆盾。

## 本次检查及限制

- 手工目视定位原图1536×1024上的轴线点（约数）：柄尾中央(140,421)、露出的握柄中部(204,423)、护手中央(421,425)、近端剑脊(480,427)、中段剑脊(950,428)、剑尖(1507,432)。
- 端点连线的手工点最大垂距约3.3px，轴线倾角约0.46度。标注有像素定位误差，仅作可复核的人工辅助，不冒充自动轮廓证明或绝对共线证书。
- 目视未见v3在护手处的明显折角。柄尾保留双侧分叉与外端小孔形投影；没有通用圆头柄尾。
- 外围仍有低透明光雾，左上角alpha为1，其余三角为0。因此不是可直接发布的清洁透明资产。依用户此前“保留原图”决定，不运行本地Alpha清理。
- 待用户审图，未覆盖运行资源，未接入追击攻击，未安装、导出或推送。

## 精确提示词

```text
Create a NEW Wylder PURSUIT ATTACK sprite from the approved identity reference, NOT an edit of the flawed diagonal attack.
Image1 fixes exact character identity, detailed inked illustration style, costume and LONG GREATsword. Image2 is the mandatory distinctive forked POMMEL / hilt-end silhouette. Ignore its checker pattern.
Pose: full-body powerful TWO-HANDED HORIZONTAL SLASH to SCREEN RIGHT, a decisive follow-through, knees bent in a wide grounded lunge, chest twisted into cut, navy coat flaring modestly behind. This is an actual slash, not a relaxed weapon-ready pose. Both gloved hands together in front of abdomen at waist height. No shield visible anywhere.
ABSOLUTE WEAPON GEOMETRY: the ENTIRE ONE sword is drawn EXACTLY HORIZONTAL across the picture. Imagine a ruler at constant y through the center of the pommel, both gloved fists, grip, crossguard center, blade center and sword tip. ALL those centers lie on that SAME HORIZONTAL LINE. The long handle is horizontal, the blade is horizontal, with ZERO change of angle at the crossguard. Do not angle either part down, no curved grip, no bent ricasso, no curved blade, no perspective bend. No zigzag. No wavy edges. Both blade edges are straight and parallel until their short final taper converges at the centered point. Show the sword nearly parallel to the picture plane, no foreshortening. Crossguard is perpendicular to the sword, rising vertically above and below the horizontal blade axis.
The character is in LEFT HALF, sword blade extends far into RIGHT HALF. Blade length about helmet-to-boot character height, genuinely long, broad straight steel greatsword with dark engraved center. Expand landscape canvas to fit full length. Entire blade tip and whole hilt-end visible with clean transparent margins.
HANDS: anatomical RIGHT hand grips the handle near crossguard; anatomical LEFT hand grips immediately behind near pommel. Exactly two adjacent coherent gloved fists around ONE continuous horizontal leather-wrapped grip. Both forearms connect naturally to hands; no fingers through blade or crossguard. Position arms to fit straight sword, never bend sword to fit arms. Left forearm retains the silver grappling bracer.
HILT: exactly ONE forked branching silver POMMEL at far-left BUTT END behind the rear hand, matching image2: central faceted metal junction, small outer eyelet projection, two long swept angular winglike prongs to opposite sides. Do NOT replace with a ball or coin. Exactly ONE crossguard at blade base, distinct from the forked butt pommel.
Identity: enclosed silver winged helmet with curved engraving, dark visor and metal face plates, blue patterned side/back panel not face cloth, no eyes or skin. Same black scarf with short drape, engraved V-bottom silver chest plate, asymmetric silver shoulder armor, scale sleeves, leather gloves, round belt buckle, sheathed hip dagger, split embroidered navy knee-length coat, dark trousers and cord-bound brown boots. Preserve detailed material shading and linework of image1; do not simplify into flat-vector art or restyle as a photo.
Transparent full-body game sprite, no floor, shadow, haze, aura, glow, slash effect, background, enemy, labels or text. Full boots and helmet wings visible. One complete sword only.
```
