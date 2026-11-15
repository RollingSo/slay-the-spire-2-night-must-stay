# idle视觉校准提示词

生成方式：内置imagegen。用户已选用v2第一版造型和细节风格，v3平涂版不采用。后续另补小圆盾及追击姿态预览，不覆盖运行资产。生成原件保留，不运行本地Alpha清理。

## v2：用用户四张截图重新固定身份和姿态

原图：`design/角色资源预览/wylder/wylder_idle_v2_identity.png`

```text
Use case: stylized-concept.
Asset: one full-body transparent-background 2D battle idle sprite for Wylder (追踪者), not a card illustration. Generate a single character only.
Inputs: image 1 is the EXACT idle-pose reference; images 2 and 3 fix armor construction; image 4 fixes helmet side construction. These screenshots are authoritative identity references, NOT rendering-style references. Ignore screenshot text and backgrounds.
Pose: reproduce image 1's relaxed planted stance: anatomical RIGHT hand raised beside his right shoulder, gripping the greatsword handle; the greatsword rests across the right shoulder, blade extending back behind the head to the opposite side. Anatomical LEFT arm hangs naturally, gloved hand loosely closed, integrated grappling mechanism on outer left forearm. Both full boots firmly planted, legs slightly apart, no crouch, no attack motion. Rotate the SAME pose into a front three-quarter game view looking toward screen RIGHT (about 35 degrees from front), retaining visible chest and armor front. It is a shoulder-rested sword, not a sword held diagonally forward or upright.
Identity and helmet: a FULLY ENCLOSED silver knight helmet, domed skull with bold curved/spiral engraving, paired tall swept-up METAL feather wings attached to helmet sides, silver horizontal brow band, dark narrow visor slit, angular silver lower face plates and projecting chin. No visible face, skin, human eyes, orange eye glow, fabric mask or hood. A blue patterned panel is at the SIDE/BACK of helmet underneath the wing, not on the face. Wing profiles must resemble image4's tall stacked metal feather blades, not horns or antlers. No huge plume.
Armor: thick folded BLACK scarf tightly wrapped around neck, NOT a huge cape; broad V-bottom silver breastplate with a few bold flowing engraved channels; asymmetric layered silver shoulder armor (anatomical left more elaborate). Navy short oversleeves over grouped silver scale armor at both upper arms. Dark brown leather gloves and bracers. Wide brown wrapped waist belt, single round silver ornamental buckle centered, small sheathed dagger at anatomical LEFT hip (screen right in front view); split knee-length NAVY front coat panels with simplified fine silver arcs and embroidered border, central silver scales visible between panels. Dark trousers, brown cord-wrapped calves, plain brown boots. Match all major costume shapes in supplied references.
Weapon: one long straight STEEL GREATSWORD resting on shoulder exactly as image1, dark gray blade with a few graphic engraved marks, simple silver crossguard, dark wrapped grip and modest metal pommel. No shield, curved sword, extra sword, or fantasy glowing weapon.
Style: graphic 2D fantasy game sprite, bold slightly irregular dark outlines, strong black shadow shapes, flat navy / black / cool silver-gray / brown color planes, hard-edged cel shading, 2-3 tones per material. Clear silhouette at a 300px display height. Simplify ornament into a few confident line motifs and scales into grouped pointed blocks; identity accuracy through shape, not realistic texture. NOT photorealistic, NOT a 3D render, NOT painterly, no gritty metal texture, no airbrush highlights, no cinematic lighting.
Composition: entire helmet wings, greatsword tip, both hands and both boot soles fully inside canvas with clean transparent margins. Landscape canvas wide enough for horizontal shoulder sword. Single consistent view, no turnaround sheet, no duplicate limbs, no text, no labels, no ground pedestal, no cast shadow, no glow, no mist, no colored backdrop. Genuine alpha transparency right to crisp antialiased silhouette.
```

## v3：只简化渲染

输入为v2；保持身份和姿态，仅减少纹理和细碎装饰。

候选输出：`design/角色资源预览/wylder/wylder_idle_v3_flat.png`。v2与v3原件均保留，运行资源未改变。

```text
Use case: style-transfer. Edit this single Wylder full-body transparent idle sprite. KEEP the exact identity, helmet geometry, two metal wing fins, enclosed silver face plates and black visor, side/back blue helmet panel, costume construction, asymmetrical shoulders, right hand grip, shoulder-rested greatsword, hanging left arm, grappling bracer, leg stance, all proportions, viewing direction, and complete silhouette unchanged.
Change ONLY the rendering language to a much simpler graphic 2D Slay the Spire-like battle sprite. Large flat color blocks, bold dark irregular outer contour, hard-edged cel shading, strong solid-black shadow shapes. Each silver armor component has just THREE flat planes: light silver, medium blue-gray, nearly black. Navy garment has two flat colors, brown leather has two flat colors. Remove ALL surface grain, scratches, mottling, fine feather/scale lines, crosshatching, realistic metal shine, small engraved flourishes and embroidery clutter. Retain only 2-3 broad flowing engraved channels on the breastplate, a few bold curved helmet channels, and ONE simplified silver arc motif on each navy skirt panel. Group sleeve scales into 3-4 large jagged blocks instead of individually rendering every scale. Simplify greatsword surface to one dark blade plane and one silver edge, preserving shape. Keep skin and eyes invisible. No orange eye.
This is a clean flat-color production game cutout, not a detailed character concept illustration. No painterly gradients, no brushed texture, no realistic metal material, no additional glow or rim haze. True-alpha transparent background, no floor or shadow, no text. Do not crop sword tip, helmet wings or boots. Single figure only.
```
