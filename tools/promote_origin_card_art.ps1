$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot = Split-Path -Parent $PSScriptRoot
$preview = Get-ChildItem -LiteralPath (Join-Path $taskRoot 'design') -Recurse -File -Filter origin_v2.png | Select-Object -First 1
if ($null -eq $preview) { throw 'Approved Origin v2 preview not found.' }
$source = [System.Drawing.Bitmap]::new($preview.FullName)
$output = [System.Drawing.Bitmap]::new(1000, 760)
try {
    $graphics = [System.Drawing.Graphics]::FromImage($output)
    try {
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.DrawImage($source, 0, 0, 1000, 760)
    } finally { $graphics.Dispose() }
    foreach ($relative in @('packed/card_portraits/ironeye', 'images/packed/card_portraits/ironeye')) {
        New-Item -ItemType Directory -Path (Join-Path $taskRoot $relative) -Force | Out-Null
        $output.Save((Join-Path $taskRoot "$relative/ironeye_origin.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    }
} finally { $output.Dispose(); $source.Dispose() }
