param([Parameter(Mandatory)][string]$SourcePath)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$projectRoot = Split-Path -Parent $PSScriptRoot
$destination = Join-Path $projectRoot 'revenant_assets/potions/spirit_calling_jar.png'
if (Test-Path -LiteralPath $destination) { throw "Refusing to overwrite existing icon: $destination" }
$sourceImage = [System.Drawing.Bitmap]::FromFile($SourcePath)
try {
    if (($sourceImage.PixelFormat -band [System.Drawing.Imaging.PixelFormat]::Alpha) -eq 0) {
        throw 'Source image must contain true alpha.'
    }
    foreach ($corner in @(@(0,0), @(($sourceImage.Width-1),0), @(0,($sourceImage.Height-1)), @(($sourceImage.Width-1),($sourceImage.Height-1)))) {
        if ($sourceImage.GetPixel($corner[0],$corner[1]).A -ne 0) { throw 'Source image corners must be transparent.' }
    }
    $icon = [System.Drawing.Bitmap]::new(512,512,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($icon)
        try {
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.DrawImage($sourceImage,0,0,512,512)
        } finally { $graphics.Dispose() }
        foreach ($corner in @(@(0,0), @(511,0), @(0,511), @(511,511))) {
            if ($icon.GetPixel($corner[0],$corner[1]).A -ne 0) { throw 'Resized image corners must be transparent.' }
        }
        $icon.Save($destination,[System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $icon.Dispose() }
} finally { $sourceImage.Dispose() }
Write-Output 'Spirit Calling Jar icon: 512x512 RGBA, transparent corners validated.'
