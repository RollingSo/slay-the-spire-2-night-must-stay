param(
    [string]$ProjectRoot = 'D:\slay-the-spire-2-night-must-stay'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$generatedRoot = 'C:\Users\17857\.codex\generated_images\01a05d55-678e-7540-96bc-ab85dcae6456'
$outputDir = Join-Path $ProjectRoot 'design\卡图预览\复仇者_2026-09-03_完全重绘12张_v2'

$items = @(
    [pscustomobject]@{ Name='blessing_of_grace'; Source='exec-995c5f5f-28f8-49d7-8314-26439f58e03d.png'; Ancient=$false; Event='赫伦在宽阔金色赐福光柱下恢复斗篷与生命' }
    [pscustomobject]@{ Name='burn_life'; Source='exec-353f5850-7aa8-4ff8-96dc-405283d18d4c.png'; Ancient=$false; Event='弗雷德里克燃尽生命并转化为青色护盾与能量' }
    [pscustomobject]@{ Name='concerto'; Source='exec-1d4b10cb-b740-421e-b979-5c8611257b53.png'; Ancient=$true; Event='巨型竖琴在两道回响间召来弗雷德里克' }
    [pscustomobject]@{ Name='dead_realm_spirit_fire'; Source='exec-7f5399e4-0963-4bd1-9edb-fc300f5591a5.png'; Ancient=$false; Event='一股死境寒焰横扫并冻结三个敌影' }
    [pscustomobject]@{ Name='ensemble'; Source='exec-cf064456-f778-4f04-a5fe-2f01019988ee.png'; Ancient=$false; Event='竖琴回响凝成象牙与青色盾弧挡住攻击' }
    [pscustomobject]@{ Name='fight_for_me'; Source='exec-6fea111c-8a51-4863-aebe-578c499294b3.png'; Ancient=$true; Event='塞巴斯蒂安的巨大骨手撕断束缚并释放竖琴回响' }
    [pscustomobject]@{ Name='gaze_beyond'; Source='exec-343ef8b8-6740-443f-9a46-8c770dfac4ba.png'; Ancient=$false; Event='一道宽阔幽紫凝视同时削弱三个敌影' }
    [pscustomobject]@{ Name='greater_recover'; Source='exec-9588a23c-7252-47d7-a700-79643238e048.png'; Ancient=$false; Event='一轮象牙盾弧同时庇护并治疗三名家人' }
    [pscustomobject]@{ Name='heavy_echo'; Source='exec-7b040b62-5c7a-4915-9f7e-31e2ec68dc0a.png'; Ancient=$false; Event='弗雷德里克用单一南瓜锤击断琴弦并触发呼唤' }
    [pscustomobject]@{ Name='kings_recovery'; Source='exec-5c594733-1794-451a-8467-91c318e7f6c0.png'; Ancient=$false; Event='王冠形金色恢复波扫过三名盟友的伤臂' }
    [pscustomobject]@{ Name='precise_lightning_strike'; Source='exec-9e64379d-12cc-42b5-9b22-f4637872a235.png'; Ancient=$false; Event='同一雷击通道中的双段古龙雷精准命中单一目标' }
    [pscustomobject]@{ Name='soul_return'; Source='exec-a597c07c-f054-4c47-b993-abab6361c371.png'; Ancient=$false; Event='灵魂从被冻住的敌影回流进竖琴琴弦' }
)

function Save-CroppedImage([string]$sourcePath, [string]$destinationPath, [int]$targetWidth, [int]$targetHeight) {
    $source = [System.Drawing.Image]::FromFile($sourcePath)
    try {
        $sourceRatio = $source.Width / [double]$source.Height
        $targetRatio = $targetWidth / [double]$targetHeight
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
                $graphics.DrawImage(
                    $source,
                    [System.Drawing.Rectangle]::new(0, 0, $targetWidth, $targetHeight),
                    $cropX, $cropY, $cropWidth, $cropHeight,
                    [System.Drawing.GraphicsUnit]::Pixel
                )
            } finally { $graphics.Dispose() }
            $bitmap.Save($destinationPath, [System.Drawing.Imaging.ImageFormat]::Png)
        } finally { $bitmap.Dispose() }

        return [pscustomobject]@{
            SourceWidth = $source.Width
            SourceHeight = $source.Height
            CropX = $cropX
            CropY = $cropY
            CropWidth = $cropWidth
            CropHeight = $cropHeight
        }
    } finally { $source.Dispose() }
}

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

$manifest = foreach ($item in $items) {
    $sourcePath = Join-Path $generatedRoot $item.Source
    if (-not (Test-Path -LiteralPath $sourcePath)) { throw "Missing generated source: $sourcePath" }
    $targetWidth = if ($item.Ancient) { 606 } else { 1000 }
    $targetHeight = if ($item.Ancient) { 852 } else { 760 }
    $destinationPath = Join-Path $outputDir ($item.Name + '.png')
    $crop = Save-CroppedImage $sourcePath $destinationPath $targetWidth $targetHeight
    [pscustomobject]@{
        Name = $item.Name
        Output = [System.IO.Path]::GetFileName($destinationPath)
        Size = "${targetWidth}x${targetHeight}"
        Source = $item.Source
        SourceSize = "$($crop.SourceWidth)x$($crop.SourceHeight)"
        Crop = "$($crop.CropX),$($crop.CropY),$($crop.CropWidth),$($crop.CropHeight)"
        VisualEvent = $item.Event
    }
}

$manifest | Export-Csv -LiteralPath (Join-Path $outputDir '_manifest.csv') -NoTypeInformation -Encoding UTF8

$columns = 4
$rows = 3
$cellWidth = 300
$imageHeight = 228
$labelHeight = 30
$font = [System.Drawing.Font]::new('Arial', 10)
$brush = [System.Drawing.Brushes]::White
$background = [System.Drawing.Color]::FromArgb(24, 26, 31)

try {
    $sheet = [System.Drawing.Bitmap]::new($columns * $cellWidth, $rows * ($imageHeight + $labelHeight))
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($sheet)
        try {
            $graphics.Clear($background)
            for ($index = 0; $index -lt $items.Count; $index++) {
                $item = $items[$index]
                $row = [math]::Floor($index / $columns)
                $column = $index % $columns
                $x = $column * $cellWidth
                $y = $row * ($imageHeight + $labelHeight)
                $path = Join-Path $outputDir ($item.Name + '.png')
                $image = [System.Drawing.Image]::FromFile($path)
                try {
                    if ($item.Ancient) {
                        $drawHeight = $imageHeight
                        $drawWidth = [int][math]::Round($drawHeight * $image.Width / $image.Height)
                        $drawX = $x + [int][math]::Floor(($cellWidth - $drawWidth) / 2)
                        $graphics.DrawImage($image, $drawX, $y, $drawWidth, $drawHeight)
                    } else {
                        $graphics.DrawImage($image, $x, $y, $cellWidth, $imageHeight)
                    }
                } finally { $image.Dispose() }
                $graphics.DrawString($item.Name, $font, $brush, $x + 4, $y + $imageHeight + 5)
            }
        } finally { $graphics.Dispose() }
        $sheet.Save((Join-Path $outputDir '_contact_sheet_01.png'), [System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $sheet.Dispose() }
} finally { $font.Dispose() }

$readme = @"
# 复仇者卡图完全重绘 12 张（审批候选）

- 生成方式：Codex 内置 ImageGen；以《杀戮尖塔 2》本地原版卡图作为风格参照。
- 画面规格：普通卡图 1000×760；远古牌 `concerto` 与 `fight_for_me` 为 606×852。
- 处理原则：粗黑轮廓、少量大色块、硬边赛璐璐阴影、有限色板、夸张透视、单一可读事件。
- 祷告参考：`blessing_of_grace` 参考 Blessing's Boon / Blessing of the Erdtree 的赐福光形；`precise_lightning_strike` 参考 Honed Bolt 的雷击形态，仅提取母题和色彩，不复制图标边框。
- 本目录仅供审查；尚未覆盖正式卡图资源。

## 逐张视觉事件

$(($items | ForEach-Object { "- ``$($_.Name)``：$($_.Event)" }) -join "`r`n")
"@
Set-Content -LiteralPath (Join-Path $outputDir '重绘说明.md') -Value $readme -Encoding UTF8

Write-Output "Prepared $($items.Count) full-redraw Revenant candidates in $outputDir"
