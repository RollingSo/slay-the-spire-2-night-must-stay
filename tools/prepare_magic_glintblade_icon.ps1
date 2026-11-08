$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot = Split-Path -Parent $PSScriptRoot
$source = [System.Drawing.Bitmap]::new('C:/Users/wenti/.codex/generated_images/01a0a5d3-926c-7f31-99dd-3aa7d0954550/exec-e96bbe91-3b9a-4710-aa38-6ae0c713d6cf.png')
$output = [System.Drawing.Bitmap]::new(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
try {
    $g = [System.Drawing.Graphics]::FromImage($output)
    try {
        $g.Clear([System.Drawing.Color]::Transparent)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.DrawImage($source, 0, 0, 256, 256)
    } finally { $g.Dispose() }
    foreach ($xy in @(@(0,0), @(255,0), @(0,255), @(255,255))) {
        if ($output.GetPixel($xy[0], $xy[1]).A -ne 0) { throw 'Magic Glintblade icon must have genuinely transparent corners.' }
    }
    foreach ($relative in @('images/powers/duchess_radiant_blade_turns_power.png', 'powers/duchess_radiant_blade_turns_power.png')) {
        $output.Save((Join-Path $taskRoot $relative), [System.Drawing.Imaging.ImageFormat]::Png)
    }
} finally { $source.Dispose(); $output.Dispose() }
