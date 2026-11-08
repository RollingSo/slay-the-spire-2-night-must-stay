$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot = Split-Path -Parent $PSScriptRoot
$source = [System.Drawing.Bitmap]::new('C:/Users/wenti/.codex/generated_images/01a0a5d3-926c-7f31-99dd-3aa7d0954550/exec-074cf508-fd12-4d56-a7d1-6810b4f6415b.png')
$output = [System.Drawing.Bitmap]::new(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
try {
    $g = [System.Drawing.Graphics]::FromImage($output)
    try {
        $g.Clear([System.Drawing.Color]::Transparent)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.DrawImage($source, 0, 0, 256, 256)
    } finally { $g.Dispose() }
    foreach ($xy in @(@(0,0), @(255,0), @(0,255), @(255,255))) {
        if ($output.GetPixel($xy[0], $xy[1]).A -ne 0) { throw 'Origin icon must have genuinely transparent corners.' }
    }
    foreach ($relative in @('images/powers/ironeye_origin_power.png', 'powers/ironeye_origin_power.png')) {
        $output.Save((Join-Path $taskRoot $relative), [System.Drawing.Imaging.ImageFormat]::Png)
    }
} finally { $source.Dispose(); $output.Dispose() }
