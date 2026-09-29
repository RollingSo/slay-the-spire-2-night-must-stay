param(
    [Parameter(Mandatory = $true)][string]$PreviewDirectory
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$sourceDirectory = Join-Path $root $PreviewDirectory
$spriteDirectory = Join-Path $root 'images/atlases/power_atlas.sprites'
$destinations = @((Join-Path $root 'images/powers'), (Join-Path $root 'powers'))
$files = @(Get-ChildItem -LiteralPath $sourceDirectory -Filter '*.png' -File | Sort-Object Name)
if ($files.Count -eq 0) { throw "No approved Duchess power icons found: $sourceDirectory" }

foreach ($file in $files) {
    $id = $file.BaseName
    if (-not $id.StartsWith('duchess_') -or -not $id.EndsWith('_power')) {
        throw "Unexpected Duchess power icon name: $($file.Name)"
    }
    $mapping = Join-Path $spriteDirectory "$id.tres"
    if (-not (Test-Path -LiteralPath $mapping) -or
        -not (Select-String -LiteralPath $mapping -SimpleMatch "res://images/powers/$($file.Name)" -Quiet)) {
        throw "Missing or mismatched power atlas mapping: $id"
    }

    $source = [System.Drawing.Bitmap]::FromFile($file.FullName)
    try {
        if ($source.Width -ne $source.Height -or
            ($source.PixelFormat -band [System.Drawing.Imaging.PixelFormat]::Alpha) -eq 0) {
            throw "Approved icon is not a square alpha PNG: $($file.Name)"
        }
        $corners = @(
            $source.GetPixel(0, 0).A,
            $source.GetPixel($source.Width - 1, 0).A,
            $source.GetPixel(0, $source.Height - 1).A,
            $source.GetPixel($source.Width - 1, $source.Height - 1).A
        )
        if (@($corners | Where-Object { $_ -ne 0 }).Count -gt 0) {
            throw "Approved icon has an opaque corner: $($file.Name)"
        }

        $output = [System.Drawing.Bitmap]::new(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($output)
            try {
                $graphics.Clear([System.Drawing.Color]::Transparent)
                $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.DrawImage($source, [System.Drawing.Rectangle]::new(0, 0, 256, 256))
            }
            finally { $graphics.Dispose() }

            $outputCorners = @(
                $output.GetPixel(0, 0).A, $output.GetPixel(255, 0).A,
                $output.GetPixel(0, 255).A, $output.GetPixel(255, 255).A
            )
            if (@($outputCorners | Where-Object { $_ -ne 0 }).Count -gt 0) {
                throw "Resized icon has a nontransparent corner: $($file.Name)"
            }
            foreach ($directory in $destinations) {
                $target = Join-Path $directory $file.Name
                $output.Save($target, [System.Drawing.Imaging.ImageFormat]::Png)
            }
        }
        finally { $output.Dispose() }
    }
    finally { $source.Dispose() }
    Write-Output "Approved $($file.Name)"
}

Write-Output "Imported $($files.Count) approved Duchess power icons at 256x256."
