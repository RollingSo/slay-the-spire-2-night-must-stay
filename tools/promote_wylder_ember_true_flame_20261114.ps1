$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$projectRoot = Split-Path $PSScriptRoot -Parent
$candidateDir = Join-Path $projectRoot 'design/卡图预览/追踪者_20261113'
$targetDir = Join-Path $projectRoot 'images/packed/card_portraits/wylder'
$backupDir = Join-Path $candidateDir '正式替换前备份_20261114'
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
$items = @(
    @{ Source='余火_v3_五指修正.png'; Target='wylder_remaining_ember.png' },
    @{ Source='见真火_v2.png'; Target='wylder_true_flame.png' }
)
foreach ($item in $items) {
    $sourcePath = Join-Path $candidateDir $item.Source
    $targetPath = Join-Path $targetDir $item.Target
    $backupPath = Join-Path $backupDir $item.Target
    if (-not (Test-Path -LiteralPath $backupPath)) { Copy-Item -LiteralPath $targetPath -Destination $backupPath }
    $sourceImage = [System.Drawing.Image]::FromFile($sourcePath)
    $bitmap = [System.Drawing.Bitmap]::new(1000,760)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        # Preserve aspect ratio; only a subpixel edge crop for generated dimensions.
        $ratio = [Math]::Max(1000.0/$sourceImage.Width,760.0/$sourceImage.Height)
        $width = [single]($sourceImage.Width*$ratio)
        $height = [single]($sourceImage.Height*$ratio)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.DrawImage($sourceImage,[single]((1000-$width)/2),[single]((760-$height)/2),$width,$height)
        $bitmap.Save($targetPath,[System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $graphics.Dispose(); $bitmap.Dispose(); $sourceImage.Dispose() }
    $check = [System.Drawing.Image]::FromFile($targetPath)
    try { if ($check.Width -ne 1000 -or $check.Height -ne 760) { throw 'Invalid portrait dimensions' } }
    finally { $check.Dispose() }
    Write-Output "$($item.Target): 1000x760 approved artwork installed"
}
