param(
    [string]$ProjectRoot = 'D:\slay-the-spire-2-night-must-stay'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$generatedRoot = 'C:\Users\17857\.codex\generated_images\01a05d55-678e-7540-96bc-ab85dcae6456'
$oldRoot = Join-Path $ProjectRoot 'design\卡图预览\复仇者_2026-09-03_重绘优化'
$outputDir = Join-Path $ProjectRoot 'design\卡图预览\复仇者_2026-09-03_构图保留画风重做18张_v2'

$items = @(
    [pscustomobject]@{ Name='all_souls_return'; Source='exec-4a1a645f-017c-407b-ab57-edb0a01f9116.png'; Invariant='四股魂流从四角汇入中央竖琴' }
    [pscustomobject]@{ Name='ancient_dragon_lightning'; Source='exec-046f55e7-b827-46a3-b236-2caeaeaa9b17.png'; Invariant='四道猩红落雷由近及远连续命中' }
    [pscustomobject]@{ Name='ancient_dragon_spear'; Source='exec-f36d5887-4dd6-46e4-9ef9-dbf9a1640100.png'; Invariant='右下巨大雷枪斜刺左上单一目标' }
    [pscustomobject]@{ Name='bone_coin'; Source='exec-d21c3178-c012-4016-b9f3-9117c546361a.png'; Invariant='中央骨币分隔左侧荆棘与右侧三魂' }
    [pscustomobject]@{ Name='emergency_restore'; Source='exec-33696775-6a56-4ecb-810b-6eaed64fdcac.png'; Invariant='左手与右侧灵体在护盾前发生恢复碰撞' }
    [pscustomobject]@{ Name='frenzied_three_fingers'; Source='exec-4029469a-4aca-4a1c-8c7c-8b9ec9c09c96.png'; Invariant='展开卷轴中央燃烧的三指烙印' }
    [pscustomobject]@{ Name='grooming'; Source='exec-768d0365-4129-4421-b978-ea3d0ce5c316.png'; Invariant='骨梳横压荆棘团并抽离红色根须' }
    [pscustomobject]@{ Name='lansseax_blade'; Source='exec-082f615f-4ebc-48b9-8778-bee6b7dce329.png'; Invariant='红色巨刃弧命中目标并由青色小弧回旋' }
    [pscustomobject]@{ Name='reanimate_dead'; Source='exec-a32822a6-1600-4192-867c-8146be5a4063.png'; Invariant='三条牵引线从倒地尸体中拉起灵魂' }
    [pscustomobject]@{ Name='recover'; Source='exec-51b844f2-3880-453b-b72e-d2e32e65d008.png'; Invariant='中央竖琴以三道连接治疗护罩内三名家人' }
    [pscustomobject]@{ Name='revenant_card'; Source='exec-3e7d8e22-e44f-48da-991e-7f1bde923443.png'; Invariant='左侧拨琴形成巨大浅色卷带挡住右侧攻击' }
    [pscustomobject]@{ Name='soul_cursing_bell'; Source='exec-7ed954bf-7409-4722-85fd-2a1120594018.png'; Invariant='倾斜巨钟与环形诅咒波命中左下目标' }
    [pscustomobject]@{ Name='soulbound'; Source='exec-310f5068-b52a-435b-978f-f44bcb5f0424.png'; Invariant='明暗双手由三枚金环连接并交换魂与荆棘' }
    [pscustomobject]@{ Name='space_rending_frenzy'; Source='exec-15f9172d-5874-4e56-9cab-62bd45cf9e7a.png'; Invariant='左下小型灵体以巨大斜击贯穿右上黑影' }
    [pscustomobject]@{ Name='spirit_gathering'; Source='exec-dc523d0e-d154-45a8-9e49-d948c9b8b1bd.png'; Invariant='裂口三魂向上汇入托住金星的巨手' }
    [pscustomobject]@{ Name='stun_call'; Source='exec-be4b1d4f-f541-4509-89cc-20d5baec0dcd.png'; Invariant='塞巴斯蒂安巨掌砸地震飞三个小敌影' }
    [pscustomobject]@{ Name='surge'; Source='exec-3e184c9a-549e-4aa7-979f-4f0ee86ed389.png'; Invariant='倾斜巨钟被双链牵引并由两道青色涌流环绕' }
    [pscustomobject]@{ Name='white_shadow_lure'; Source='exec-004381d6-f8fa-4f41-9f08-6657672192a4.png'; Invariant='白色诱饵吸收三支箭并掩护左后方家人' }
)

function Save-CroppedImage([string]$sourcePath, [string]$destinationPath) {
    $targetWidth = 1000
    $targetHeight = 760
    $source = [System.Drawing.Image]::FromFile($sourcePath)
    try {
        $targetRatio = $targetWidth / [double]$targetHeight
        $sourceRatio = $source.Width / [double]$source.Height
        if ($sourceRatio -gt $targetRatio) {
            $cropHeight = $source.Height
            $cropWidth = [int][math]::Round($cropHeight * $targetRatio)
            $cropX = [int][math]::Floor(($source.Width - $cropWidth) / 2)
            $cropY = 0
        } else {
            $cropWidth = $source.Width
            $cropHeight = [int][math]::Round($cropWidth / $targetRatio)
            $cropX = 0
            $cropY = [int][math]::Floor(($source.Height - $cropHeight) / 2)
        }

        $bitmap = [System.Drawing.Bitmap]::new($targetWidth, $targetHeight)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            try {
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.DrawImage($source, [System.Drawing.Rectangle]::new(0,0,$targetWidth,$targetHeight), $cropX,$cropY,$cropWidth,$cropHeight, [System.Drawing.GraphicsUnit]::Pixel)
            } finally { $graphics.Dispose() }
            $bitmap.Save($destinationPath, [System.Drawing.Imaging.ImageFormat]::Png)
        } finally { $bitmap.Dispose() }

        [pscustomobject]@{
            SourceSize = "$($source.Width)x$($source.Height)"
            Crop = "$cropX,$cropY,$cropWidth,$cropHeight"
        }
    } finally { $source.Dispose() }
}

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

$manifest = foreach ($item in $items) {
    $sourcePath = Join-Path $generatedRoot $item.Source
    if (-not (Test-Path -LiteralPath $sourcePath)) { throw "Missing generated source: $sourcePath" }
    $destinationPath = Join-Path $outputDir ($item.Name + '.png')
    $crop = Save-CroppedImage $sourcePath $destinationPath
    [pscustomobject]@{
        Name = $item.Name
        Output = [System.IO.Path]::GetFileName($destinationPath)
        Size = '1000x760'
        Source = $item.Source
        SourceSize = $crop.SourceSize
        Crop = $crop.Crop
        CompositionInvariant = $item.Invariant
    }
}
$manifest | Export-Csv -LiteralPath (Join-Path $outputDir '_manifest.csv') -NoTypeInformation -Encoding UTF8

$font = [System.Drawing.Font]::new('Arial', 9)
$brush = [System.Drawing.Brushes]::White
$background = [System.Drawing.Color]::FromArgb(22,24,29)
try {
    $columns = 3
    $rows = 2
    $pairWidth = 400
    $thumbWidth = 200
    $thumbHeight = 152
    $labelHeight = 28
    $perPage = $columns * $rows
    for ($page=0; $page * $perPage -lt $items.Count; $page++) {
        $sheet = [System.Drawing.Bitmap]::new($columns*$pairWidth, $rows*($thumbHeight+$labelHeight))
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($sheet)
            try {
                $graphics.Clear($background)
                for ($slot=0; $slot -lt $perPage; $slot++) {
                    $index = $page*$perPage+$slot
                    if ($index -ge $items.Count) { break }
                    $item = $items[$index]
                    $row = [math]::Floor($slot/$columns)
                    $column = $slot % $columns
                    $x = $column*$pairWidth
                    $y = $row*($thumbHeight+$labelHeight)
                    $oldImage = [System.Drawing.Image]::FromFile((Join-Path $oldRoot ($item.Name+'.png')))
                    $newImage = [System.Drawing.Image]::FromFile((Join-Path $outputDir ($item.Name+'.png')))
                    try {
                        $graphics.DrawImage($oldImage,$x,$y,$thumbWidth,$thumbHeight)
                        $graphics.DrawImage($newImage,$x+$thumbWidth,$y,$thumbWidth,$thumbHeight)
                    } finally { $oldImage.Dispose(); $newImage.Dispose() }
                    $graphics.DrawString("$($item.Name)  OLD | NEW",$font,$brush,$x+4,$y+$thumbHeight+5)
                }
            } finally { $graphics.Dispose() }
            $sheet.Save((Join-Path $outputDir ("_comparison_sheet_{0:D2}.png" -f ($page+1))),[System.Drawing.Imaging.ImageFormat]::Png)
        } finally { $sheet.Dispose() }
    }
} finally { $font.Dispose() }

$readme = @"
# 复仇者卡图：构图保留、画风重做 18 张

- 生成方式：Codex 内置 ImageGen，逐张独立执行风格重绘。
- 左右对照表中每组均为 `OLD | NEW`。
- 保留项：主体位置、视角、运动方向、数量关系与一级视觉事件。
- 重做项：轮廓、色块、硬边阴影、材质概括、特效形状与背景组织。
- 统一规格：18 张均为 1000×760 横图。
- 本目录仅供审批，没有覆盖正式资源。

## 构图不变量

$(($items | ForEach-Object { "- ``$($_.Name)``：$($_.Invariant)" }) -join "`r`n")
"@
Set-Content -LiteralPath (Join-Path $outputDir '重绘说明.md') -Value $readme -Encoding UTF8

Write-Output "Prepared $($items.Count) composition-preserving style redraws in $outputDir"
