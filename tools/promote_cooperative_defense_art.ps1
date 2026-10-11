$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot = Split-Path -Parent $PSScriptRoot
$source = [System.Drawing.Bitmap]::new((Join-Path $taskRoot 'design/卡图预览/cooperative_defense_v2.png'))
$output = [System.Drawing.Bitmap]::new(1000, 760)
try {
    $graphics = [System.Drawing.Graphics]::FromImage($output)
    try {
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.DrawImage($source, 0, 0, 1000, 760)
    } finally { $graphics.Dispose() }
    foreach ($relative in @('packed/card_portraits/guardian', 'images/packed/card_portraits/guardian')) {
        $output.Save((Join-Path $taskRoot "$relative/cooperative_defense.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    }
} finally { $output.Dispose(); $source.Dispose() }
