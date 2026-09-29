param([switch]$RemoveObsolete)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$preview = Get-ChildItem -LiteralPath (Join-Path $root 'design') -Directory |
    Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'duchess_reverse_pocketwatch.png') } |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $preview) { throw 'Approved item icon preview directory not found' }
$relics = @('reverse_pocketwatch','crown_badge','golden_dewdrop','primal_glintstone_blade','blessed_iron_coin','night_of_wisdom','blue_stained_blade','carian_badge')
$potions = @('smoke_bottle','radiant_blade_crystal','regret_potion')

function Install-Icon([string]$name, [string]$kind) {
    $source = Join-Path $preview "duchess_$name.png"
    if (-not (Test-Path -LiteralPath $source)) { throw "Missing approved icon: $source" }
    $destination = Join-Path $root "duchess_assets/$kind/duchess_$name.png"
    $inputImage = [System.Drawing.Image]::FromFile($source)
    $canvas = [System.Drawing.Bitmap]::new(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $scale = [Math]::Min(240.0 / $inputImage.Width, 240.0 / $inputImage.Height)
        $width = [int][Math]::Round($inputImage.Width * $scale)
        $height = [int][Math]::Round($inputImage.Height * $scale)
        $graphics.DrawImage($inputImage, [System.Drawing.Rectangle]::new([int]((256-$width)/2), [int]((256-$height)/2), $width, $height))
        $canvas.Save($destination, [System.Drawing.Imaging.ImageFormat]::Png)
    } finally {
        $graphics.Dispose()
        $canvas.Dispose()
        $inputImage.Dispose()
    }
    if ($kind -eq 'potions') {
        $atlas = Join-Path $root "images/atlases/potion_atlas.sprites/duchess_$name.tres"
        $text = "[gd_resource type=`"AtlasTexture`" load_steps=2 format=3]`n`n[ext_resource type=`"Texture2D`" path=`"res://duchess_assets/potions/duchess_$name.png`" id=`"1`"]`n`n[resource]`natlas = ExtResource(`"1`")`nregion = Rect2(0, 0, 256, 256)`n"
        [System.IO.File]::WriteAllText($atlas, $text, [System.Text.UTF8Encoding]::new($false))
    }
}

foreach ($name in $relics) { Install-Icon $name 'relics' }
foreach ($name in $potions) { Install-Icon $name 'potions' }

if ($RemoveObsolete) {
    foreach ($name in @('mended_pocketwatch','lace_cuff','silver_thimble','dance_shoes','unsent_letter','blue_ribbon')) {
        $target = Join-Path $root "duchess_assets/relics/duchess_$name.png"
        if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target }
    }
    foreach ($name in @('silver_perfume','veil_vial','memory_draught')) {
        foreach ($relative in @("duchess_assets/potions/duchess_$name.png", "images/atlases/potion_atlas.sprites/duchess_$name.tres")) {
            $target = Join-Path $root $relative
            if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target }
        }
    }
}
