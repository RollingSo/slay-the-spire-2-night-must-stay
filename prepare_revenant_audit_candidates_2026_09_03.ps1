param(
    [string]$ProjectRoot = 'D:\slay-the-spire-2-night-must-stay'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$generatedRoot = 'C:\Users\17857\.codex\generated_images\01a05d55-678e-7540-96bc-ab85dcae6456'
$outputDir = Join-Path $ProjectRoot 'design\卡图预览\复仇者_2026-09-03_重绘优化'

$items = @(
    [pscustomobject]@{ Name='ancient_dragon_lightning'; Source='exec-7e653807-a48a-4427-a123-4a87dfff4c0c.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='ancient_dragon_spear'; Source='exec-98f946f4-cb77-45ee-9043-492477a4f81c.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='lansseax_blade'; Source='exec-ec768654-0a0d-4c57-9121-cfa4d7fe28cc.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='strike_revenant'; Source='exec-05a41038-0e56-4f4b-b0da-331920d26c37.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='stun_call'; Source='exec-89273ffe-2a26-4a3c-8824-95c3d1eaa0fe.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='white_shadow_lure'; Source='exec-c150f6aa-0fd9-43f4-9a51-d674c94de122.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='burn_life'; Source='exec-cf373a60-4b55-44e8-9653-a1dce3a789e8.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='ensemble'; Source='exec-7e74a6a7-9e84-4aa4-98b9-64b419a69f54.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='concerto'; Source='exec-dafd3299-0e87-4686-a7e6-a1083a3c7141.png'; Kind='重绘'; Ancient=$true }
    [pscustomobject]@{ Name='fight_for_me'; Source='exec-4100f535-da34-4a7b-940b-89ab48a4ad17.png'; Kind='重绘'; Ancient=$true }
    [pscustomobject]@{ Name='frenzied_three_fingers'; Source='exec-0ee9611e-68de-4efb-8bff-a628b1970db4.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='recover'; Source='exec-37442882-f9e4-4829-a658-3e36a2475067.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='greater_recover'; Source='exec-c1e5fd9a-8077-4f2c-8a20-d5e6aeeed990.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='soul_summon'; Source='exec-fb8817e3-ce93-41fa-921f-5f178efbc1c6.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='space_rending_frenzy'; Source='exec-c5374664-af98-4a3b-a40a-723253a0d203.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='dead_realm_spirit_fire'; Source='exec-25d622ee-ad35-40e4-9836-5e04b38f7e07.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='gaze_beyond'; Source='exec-8faa705e-6f20-4147-b09b-2166c27ae1ee.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='grooming'; Source='exec-6080dc57-86a5-4134-bf94-53d5c92585aa.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='heavy_echo'; Source='exec-83e5ec7c-d1d3-46c5-901d-afab4aae2860.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='resurgence'; Source='exec-3c5babbd-3170-4b85-957d-ac4835132cb5.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='underworld_reflection'; Source='exec-2ed3aaab-ec92-4f03-ab6c-874724cd3efa.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='all_souls_return'; Source='exec-5c0d32db-ba22-4f9b-b3f4-3fdc5107ac63.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='reanimate_dead'; Source='exec-e3547ef8-5298-4f46-ad72-c437250d02a0.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='spirit_gathering'; Source='exec-1d3c2e43-e6f8-466b-98ec-50bdb8aef901.png'; Kind='重绘'; Ancient=$false }
    [pscustomobject]@{ Name='emergency_restore'; Source='exec-11ebb4ed-8da5-4337-9083-442fc10a6ece.png'; Kind='优化'; Ancient=$false }
    [pscustomobject]@{ Name='precise_lightning_strike'; Source='exec-7cd10886-c02d-4b57-ae27-46d90dc8e981.png'; Kind='优化'; Ancient=$false }
    [pscustomobject]@{ Name='blessing_of_grace'; Source='exec-0177ff1a-920f-4a33-ab04-04e663ce19a7.png'; Kind='优化'; Ancient=$false }
    [pscustomobject]@{ Name='gurranqs_rock'; Source='exec-fe6cc0c1-9d3a-4ae9-918c-d81d00ac4c47.png'; Kind='优化'; Ancient=$false }
    [pscustomobject]@{ Name='surge'; Source='exec-cb95e48c-fba5-46cc-b263-07e9abab7861.png'; Kind='优化'; Ancient=$false }
    [pscustomobject]@{ Name='revenant_card'; Source='exec-d2c362dc-dacf-4df1-a5fd-a2c4f3d04399.png'; Kind='优化'; Ancient=$false }
    [pscustomobject]@{ Name='kings_recovery'; Source='exec-6ee1e68c-d29b-4fa7-9dca-6efd5edca5b6.png'; Kind='优化'; Ancient=$false }
    [pscustomobject]@{ Name='soul_cursing_bell'; Source='exec-4364e534-5160-40be-8b8f-bbe1ba9fc0a8.png'; Kind='优化'; Ancient=$false }
    [pscustomobject]@{ Name='light_spirit'; Source='exec-9844bf5d-d7bf-46f9-b620-591d17ae28e2.png'; Kind='优化'; Ancient=$false }
    [pscustomobject]@{ Name='soul_return'; Source='exec-2fc4cd6f-24f1-4a82-bffd-9213adb1d831.png'; Kind='优化'; Ancient=$false }
    [pscustomobject]@{ Name='bone_coin'; Source='exec-b2bd86ec-c748-4734-8518-e47dc92e8a20.png'; Kind='优化'; Ancient=$false }
    [pscustomobject]@{ Name='soulbound'; Source='exec-f52ceee2-7052-487a-9ae5-56e03eee6be8.png'; Kind='优化'; Ancient=$false }
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
        Kind = $item.Kind
        Output = [System.IO.Path]::GetFileName($destinationPath)
        Size = "${targetWidth}x${targetHeight}"
        Source = $item.Source
        SourceSize = "$($crop.SourceWidth)x$($crop.SourceHeight)"
        Crop = "$($crop.CropX),$($crop.CropY),$($crop.CropWidth),$($crop.CropHeight)"
    }
}

$manifest | Export-Csv -LiteralPath (Join-Path $outputDir '_manifest.csv') -NoTypeInformation -Encoding UTF8

$columns = 4
$rows = 3
$cellWidth = 250
$imageHeight = 190
$labelHeight = 28
$perPage = $columns * $rows
$font = [System.Drawing.Font]::new('Arial', 9)
$brush = [System.Drawing.Brushes]::White
$background = [System.Drawing.Color]::FromArgb(24, 26, 31)

try {
    for ($page = 0; $page * $perPage -lt $items.Count; $page++) {
        $sheet = [System.Drawing.Bitmap]::new($columns * $cellWidth, $rows * ($imageHeight + $labelHeight))
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($sheet)
            try {
                $graphics.Clear($background)
                for ($slot = 0; $slot -lt $perPage; $slot++) {
                    $index = $page * $perPage + $slot
                    if ($index -ge $items.Count) { break }
                    $item = $items[$index]
                    $row = [math]::Floor($slot / $columns)
                    $column = $slot % $columns
                    $x = $column * $cellWidth
                    $y = $row * ($imageHeight + $labelHeight)
                    $path = Join-Path $outputDir ($item.Name + '.png')
                    $image = [System.Drawing.Image]::FromFile($path)
                    try {
                        if ($item.Ancient) {
                            $drawHeight = 190
                            $drawWidth = [int][math]::Round($drawHeight * $image.Width / $image.Height)
                            $drawX = $x + [int][math]::Floor(($cellWidth - $drawWidth) / 2)
                            $graphics.DrawImage($image, $drawX, $y, $drawWidth, $drawHeight)
                        } else {
                            $graphics.DrawImage($image, $x, $y, $cellWidth, $imageHeight)
                        }
                    } finally { $image.Dispose() }
                    $graphics.DrawString("$($item.Name) [$($item.Kind)]", $font, $brush, $x + 3, $y + $imageHeight + 4)
                }
            } finally { $graphics.Dispose() }
            $sheet.Save((Join-Path $outputDir ("_contact_sheet_{0:D2}.png" -f ($page + 1))), [System.Drawing.Imaging.ImageFormat]::Png)
        } finally { $sheet.Dispose() }
    }
} finally { $font.Dispose() }

Write-Output "Prepared $($items.Count) Revenant approval candidates in $outputDir"
