$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$preview = Get-ChildItem -LiteralPath (Join-Path $root 'design') -Directory -Recurse |
    Where-Object { $_.Name -eq 'Duchess_2026-09-30_NewCards' } |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $preview) { throw 'New Duchess card preview directory not found' }
foreach ($name in @('zero_cost_attack_power','graceful_sword_dance_power','phantom_killer_power')) {
    $source = Join-Path $preview "$name.png"
    if (-not (Test-Path -LiteralPath $source)) { throw "Missing preview: $source" }
    $inputImage = [System.Drawing.Image]::FromFile($source)
    $canvas = [System.Drawing.Bitmap]::new(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $scale = [Math]::Min(240.0 / $inputImage.Width, 240.0 / $inputImage.Height)
        $width = [int][Math]::Round($inputImage.Width * $scale)
        $height = [int][Math]::Round($inputImage.Height * $scale)
        $graphics.DrawImage($inputImage, [System.Drawing.Rectangle]::new([int]((256-$width)/2), [int]((256-$height)/2), $width, $height))
        foreach ($folder in @('images/powers','powers')) {
            $canvas.Save((Join-Path $root "$folder/duchess_$name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        }
    } finally {
        $graphics.Dispose()
        $canvas.Dispose()
        $inputImage.Dispose()
    }
    $atlas = Join-Path $root "images/atlases/power_atlas.sprites/duchess_$name.tres"
    $text = "[gd_resource type=`"AtlasTexture`" load_steps=2 format=3]`n`n[ext_resource type=`"Texture2D`" path=`"res://images/powers/duchess_$name.png`" id=`"1`"]`n`n[resource]`natlas = ExtResource(`"1`")`nregion = Rect2(0, 0, 256, 256)`n"
    [System.IO.File]::WriteAllText($atlas, $text, [System.Text.UTF8Encoding]::new($false))
}
