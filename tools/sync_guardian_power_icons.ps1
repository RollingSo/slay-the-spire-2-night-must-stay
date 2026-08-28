param(
    [switch]$MissingOnly,
    [int]$OutputSize = 256
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$atlasPath = Join-Path $root 'guardian_assets\guardian_power_atlas.png'
$spriteDirectory = Join-Path $root 'images\atlases\power_atlas.sprites'
$guardianAtlasResourcePath = 'res://guardian_assets/guardian_power_atlas.png'
$outputDirectories = @(
    (Join-Path $root 'images\powers'),
    (Join-Path $root 'powers')
)
$updatedCount = 0
$unchangedCount = 0

if (-not (Test-Path -LiteralPath $atlasPath)) {
    throw "Guardian power atlas was not found: $atlasPath"
}

foreach ($outputDirectory in $outputDirectories) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$atlas = [System.Drawing.Bitmap]::FromFile($atlasPath)
try {
    foreach ($spriteFile in Get-ChildItem -LiteralPath $spriteDirectory -Filter '*.tres' | Sort-Object Name) {
        $contents = Get-Content -LiteralPath $spriteFile.FullName -Raw

        # Iron Eye and future characters can use standalone power icons in the
        # same sprite directory. Only crop entries whose AtlasTexture explicitly
        # points at the Guardian atlas; otherwise an independent 256x256 icon
        # would be overwritten with the Guardian atlas' top-left region.
        $atlasSourceMatch = [regex]::Match(
            $contents,
            'ext_resource\s+type="Texture2D"\s+path="([^"]+)"'
        )
        if (-not $atlasSourceMatch.Success) {
            throw "No AtlasTexture source was found in $($spriteFile.FullName)"
        }
        if ($atlasSourceMatch.Groups[1].Value -ne $guardianAtlasResourcePath) {
            continue
        }

        $outputPaths = @(
            $outputDirectories | ForEach-Object {
                Join-Path $_ ($spriteFile.BaseName + '.png')
            }
        )
        if ($MissingOnly -and ($outputPaths | Where-Object { -not (Test-Path -LiteralPath $_) }).Count -eq 0) {
            continue
        }

        $match = [regex]::Match(
            $contents,
            'region\s*=\s*Rect2\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)'
        )
        if (-not $match.Success) {
            throw "No AtlasTexture region was found in $($spriteFile.FullName)"
        }

        $x = [int]$match.Groups[1].Value
        $y = [int]$match.Groups[2].Value
        $width = [int]$match.Groups[3].Value
        $height = [int]$match.Groups[4].Value

        if ($x -lt 0 -or $y -lt 0 -or $x + $width -gt $atlas.Width -or $y + $height -gt $atlas.Height) {
            throw "AtlasTexture region is outside the atlas in $($spriteFile.FullName)"
        }

        $icon = New-Object System.Drawing.Bitmap(
            $OutputSize,
            $OutputSize,
            [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
        )
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($icon)
            try {
                $graphics.Clear([System.Drawing.Color]::Transparent)
                $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $source = New-Object System.Drawing.Rectangle($x, $y, $width, $height)
                $destination = New-Object System.Drawing.Rectangle(0, 0, $OutputSize, $OutputSize)
                $graphics.DrawImage($atlas, $destination, $source, [System.Drawing.GraphicsUnit]::Pixel)
            }
            finally {
                $graphics.Dispose()
            }

            $pngStream = New-Object System.IO.MemoryStream
            try {
                $icon.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
                $pngBytes = $pngStream.ToArray()
            }
            finally {
                $pngStream.Dispose()
            }

            $sha256 = [System.Security.Cryptography.SHA256]::Create()
            try {
                $generatedHash = [System.BitConverter]::ToString($sha256.ComputeHash($pngBytes)).Replace('-', '')
            }
            finally {
                $sha256.Dispose()
            }

            foreach ($outputPath in $outputPaths) {
                if (-not $MissingOnly -or -not (Test-Path -LiteralPath $outputPath)) {
                    if ((Test-Path -LiteralPath $outputPath) -and
                        (Get-FileHash -LiteralPath $outputPath -Algorithm SHA256).Hash -eq $generatedHash) {
                        $unchangedCount++
                        continue
                    }

                    # Saving directly over an imported PNG can make GDI+ keep
                    # the destination handle open (especially after Godot has
                    # imported the atlas). Write beside it first, then replace
                    # the destination atomically so repeated exports are safe.
                    $temporaryPath = "$outputPath.tmp.$PID.png"
                    try {
                        [System.IO.File]::WriteAllBytes($temporaryPath, $pngBytes)
                        Move-Item -LiteralPath $temporaryPath -Destination $outputPath -Force
                    }
                    finally {
                        if (Test-Path -LiteralPath $temporaryPath) {
                            Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction SilentlyContinue
                        }
                    }
                    $updatedCount++
                    Write-Output $outputPath
                }
            }
        }
        finally {
            $icon.Dispose()
        }
    }
}
finally {
    $atlas.Dispose()
}

Write-Output "Guardian power icons: updated $updatedCount, unchanged $unchangedCount."
