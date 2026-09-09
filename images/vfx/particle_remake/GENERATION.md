# 特效贴图生成记录

2026-09-10，使用内置 image_gen，未使用外部付费生成服务或 CLI/API。四张原始生成图直接复制进本目录，没有通过脚本绘制或替代图片内容。每张是 2×2 图集，共 16 个独立图块，Shader 按象限采样。

最初两次透明背景请求返回了带棋盘格的 RGB 图片，均已废弃。最终资产是黑底灰度**强度贴图**，不是具有原生透明通道的 PNG。生产 Shader 将亮度连续映射到 Alpha，黑色对应零覆盖，并对图块边界做平滑衰减，避免矩形边缘；不是显示黑底图片。不要将这些素材当作能力图标或角色精灵使用。

## elemental.png

来源：exec-5999708a-d4b2-4d96-ad93-efb42891288a.png。

最终提示词：

> A production grayscale EMISSION MASK TEXTURE for a video game. Perfectly solid pure BLACK (#000000) background, never checkerboard. Single square sheet, exact 2x2 equal quadrants. Each quadrant has one centered isolated WHITE/GRAY effect with generous black space around it, no overlap between quadrants. TOP LEFT: beautiful stylized billowing smoke puff, rich sculpted round lobes and wispy outer curls. TOP RIGHT: flowing white flame tongue pointing upward with torn split tips. BOTTOM LEFT: cluster of three angular white crystal shards with one tall dominant spear. BOTTOM RIGHT: low sweeping dust curl with wisps and tiny motes. Effects consist exclusively of luminosity fading smoothly all the way into pure BLACK at the edges; this will be read by a GPU shader as a light-intensity mask. All four effects must fit inside 80 percent of the width and height of their respective quadrants. Game sprite material, appealing sculpted hand-painted shapes, restrained detail, not photographic, no hard outlines. NO text, NO gridlines, NO gray background, NO checkerboard, NO transparency preview pattern. Black padding at image edges and along horizontal and vertical midlines. Square image.

## energy.png

来源：exec-a9e850dd-5169-4e12-9a19-370f90fe6657.png。

最终提示词：

> Create ONE square 2x2 game VFX emission-mask texture atlas on perfectly uniform pure BLACK (#000000). Four separate WHITE and GRAY luminous sprites, each centered in its quadrant with at least 12% black padding on all four sides within its own cell. TOP LEFT: one powerful sweeping CRESCENT SLASH, diagonal lower-left to upper-right, thick sharp leading edge and multiple beautifully flowing tapered wispy filaments trailing behind; a single open crescent, not a ring. TOP RIGHT: one circular HOLY ENERGY RING viewed straight-on, richly layered narrow luminous rim with fine ornamental light filaments, a large clean empty BLACK center; no religious symbols, text or runes. BOTTOM LEFT: a tall FORKED LIGHTNING BOLT, thick dominant central zigzag and fine branching electricity, hot-white core with smoothly fading outer light. BOTTOM RIGHT: a sharp explosive IMPACT STAR, broad central burst with 7 asymmetric tapered rays and a few small flying spark streaks, readable silhouette. Stylized high-quality real-time game textures, strong designed shapes and delicate internal flow detail, moderate glow, graceful edges fading smoothly to BLACK. Monochrome only, all sprites neutral white to tint in shaders. NO checkerboard, NO gray background, NO text, NO gridlines, NO scene, no cropped sprites. Four equal square cells in exact 2x2 arrangement.

## physical.png

来源：exec-16b3ea65-4d2a-4bbf-93c2-e588745d5112.png。参考图为旧版家人效果实际渲染 `.tmp/vfx-scale/movie/page4/frame_05.png`（在本次录像覆盖该临时文件之前）。仅参考武器和骨掌的功能与主体含义。

最终提示词：

> The reference is ONLY a functional comparison of game attack subjects; do NOT copy its grid labels, flat geometry or layout. Create ONE new square 2x2 production game VFX emission-mask ATLAS on uniform pure BLACK. EXACT four equal square cells, isolated WHITE/GRAY spectral objects centered inside each cell, generous black padding. TOP LEFT: a slender elegant medieval RAPIER pointing horizontally RIGHT, complete long thin blade, small curved guard, visible hilt; spectral icy-white weapon apparition, no person. TOP RIGHT: a massive PUMPKIN-SHAPED WAR HAMMER, ribbed rounded metal pumpkin head, distinct short pumpkin stem, attached long handle slanting down-left; NOT barrel, NOT ordinary mallet, NO jack-o-lantern face. BOTTOM LEFT: large anatomically clear SKELETAL HAND, palm facing viewer, exactly FIVE jointed bony digits including a thumb, short wrist bones trailing into wispy pale mist, NO glove, NO sleeve, NO clothing. BOTTOM RIGHT: a large faceted angular ROCK with a few small stone splinters, sculpted broad facets and subtle dust at base. All objects monochrome WHITE and GRAY with luminous spectral rims, suitable to recolor using a shader. Stylized dark-fantasy game illustration, clear strong silhouette, tasteful hand-painted interior modeling, no photographic surfaces. Black background is a GPU intensity-mask zero value, no checkerboard, no text, no UI, no ground or cast shadows. Do not depict any character bodies. Do not overlap quadrant boundaries.

## motion.png

来源：exec-543917e1-9dc5-4d10-ae37-fa13829c3ed4.png。

最终提示词：

> Create ONE square production 2D game VFX emission-mask atlas, precise 2x2 equal quadrants, perfectly uniform pure BLACK background. Four separate WHITE and GRAY sprites, no color. TOP LEFT: a single long thin ARROW pointing horizontally RIGHT, clear sharp triangular metal arrowhead at right, straight shaft, small fletching at left, restrained fine wake. TOP RIGHT: one elegant broad bird FEATHER, curved shaft and layered barbs, pointed tip, pale feather with subtle luminous wisps; not an angel wing. BOTTOM LEFT: an irregular X-shaped SHATTERED GLASS FRACTURE, four jagged thick main cracks radiating from one central impact with a few angular detached shards; must look like fractured glass, NOT lightning, NO stars, NO ring. BOTTOM RIGHT: a soft tall vertical HEALING LIGHT SHAFT, three serene straight parallel light ribbons tapering at the top, delicate drifting diamond motes, smooth softly feathered lower edge; NO flames or fire, NO cross or religious symbol. Each sprite centered in its own quadrant, all endpoints fit inside its cell, black padding at least 10% around all four edges of each cell, do not cross central dividing lines. Elegant high quality game VFX mask artwork, readable silhouettes and fine luminous detail. Pure black zero-intensity background, no checkerboard, no gray background, no text, no lines separating cells, no UI.

透明度与性能最终以游戏 Shader 的实际 GPU 渲染为准。每个效果使用独立的动画参数、材质实例及粒子发射器，底层四张图集和着色器资源缓存共享。
