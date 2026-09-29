$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$preview = Get-ChildItem -LiteralPath (Join-Path $root 'design') -Directory -Recurse |
    Where-Object { $_.Name -eq 'Duchess_2026-09-30_NewCards' } |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $preview) { throw 'New Duchess card preview directory not found' }
$output = Join-Path $preview 'processed'
New-Item -ItemType Directory -Path $output -Force | Out-Null
foreach ($name in @('memory','inch_victory','loretta_slash','sacred_halo','graceful_sword_dance','memory_fragment','phantom_killer','great_caria','fate')) {
    $source = Join-Path $preview "$name.png"
    if (-not (Test-Path -LiteralPath $source)) { throw "Missing card preview: $source" }
    $image = [System.Drawing.Image]::FromFile($source)
    $canvas = [System.Drawing.Bitmap]::new(1000, 760, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    try {
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.DrawImage($image, [System.Drawing.Rectangle]::new(0, 0, 1000, 760))
        $canvas.Save((Join-Path $output "$name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    } finally {
        $graphics.Dispose()
        $canvas.Dispose()
        $image.Dispose()
    }
}
$names = @('memory','inch_victory','loretta_slash','sacred_halo','graceful_sword_dance','memory_fragment','phantom_killer','great_caria','fate')
$sheet = [System.Drawing.Bitmap]::new(750, 630, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$draw = [System.Drawing.Graphics]::FromImage($sheet)
$font = [System.Drawing.Font]::new('Arial', 12)
try {
    $draw.Clear([System.Drawing.Color]::FromArgb(24, 25, 38))
    for ($i = 0; $i -lt $names.Count; $i++) {
        $image = [System.Drawing.Image]::FromFile((Join-Path $output "$($names[$i]).png"))
        try {
            $x = ($i % 3) * 250
            $y = [int][Math]::Floor($i / 3) * 210
            $draw.DrawImage($image, [System.Drawing.Rectangle]::new($x, $y, 250, 190))
            $draw.DrawString($names[$i], $font, [System.Drawing.Brushes]::White, $x + 4, $y + 191)
        } finally { $image.Dispose() }
    }
    $sheet.Save((Join-Path $output 'thumbnail_review.png'), [System.Drawing.Imaging.ImageFormat]::Png)
} finally {
    $font.Dispose()
    $draw.Dispose()
    $sheet.Dispose()
}
