param(
    [Parameter(Mandatory = $true)]
    [string]$SourceDir,

    [Parameter(Mandatory = $true)]
    [string]$OutputDir,

    [string[]]$AncientCardNames = @('concerto', 'fight_for_me')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$source = (Resolve-Path -LiteralPath $SourceDir).Path
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$output = (Resolve-Path -LiteralPath $OutputDir).Path
$files = Get-ChildItem -LiteralPath $source -Filter '*.png' | Sort-Object Name

if ($files.Count -eq 0) {
    throw "No PNG files found in $source"
}

$rows = foreach ($file in $files) {
    $image = [System.Drawing.Image]::FromFile($file.FullName)
    try {
        $expected = if ($AncientCardNames -contains $file.BaseName) { '606x852' } else { '1000x760' }
        $actual = "{0}x{1}" -f $image.Width, $image.Height
        [pscustomobject]@{
            File = $file.Name
            Width = $image.Width
            Height = $image.Height
            Expected = $expected
            SizePass = ($actual -eq $expected)
            Sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $file.FullName).Hash
        }
    } finally {
        $image.Dispose()
    }
}

$rows | Export-Csv -LiteralPath (Join-Path $output 'asset_report.csv') -NoTypeInformation -Encoding utf8
$duplicateGroups = $rows | Group-Object Sha256 | Where-Object Count -gt 1
$duplicateLines = @('# Exact duplicate artwork', '')
if ($duplicateGroups.Count -eq 0) {
    $duplicateLines += 'No exact file duplicates found.'
} else {
    foreach ($group in $duplicateGroups) {
        $duplicateLines += ('- ' + (($group.Group | ForEach-Object File) -join ', '))
    }
}
$duplicateLines | Set-Content -LiteralPath (Join-Path $output 'exact_duplicates.md') -Encoding utf8

$columns = 5
$rowsPerPage = 4
$imageWidth = 250
$imageHeight = 190
$labelHeight = 34
$cellWidth = $imageWidth
$cellHeight = $imageHeight + $labelHeight
$font = [System.Drawing.Font]::new('Arial', 10, [System.Drawing.FontStyle]::Regular)
$labelBrush = [System.Drawing.Brushes]::White
$background = [System.Drawing.Color]::FromArgb(24, 26, 31)
$letterbox = [System.Drawing.Color]::FromArgb(8, 9, 12)

try {
    $pageSize = $columns * $rowsPerPage
    $pageCount = [math]::Ceiling($files.Count / [double]$pageSize)
    for ($page = 0; $page -lt $pageCount; $page++) {
        $sheet = [System.Drawing.Bitmap]::new($columns * $cellWidth, $rowsPerPage * $cellHeight)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($sheet)
            try {
                $graphics.Clear($background)
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                for ($slot = 0; $slot -lt $pageSize; $slot++) {
                    $index = $page * $pageSize + $slot
                    if ($index -ge $files.Count) { break }
                    $column = $slot % $columns
                    $row = [math]::Floor($slot / $columns)
                    $x = $column * $cellWidth
                    $y = $row * $cellHeight
                    $graphics.FillRectangle([System.Drawing.SolidBrush]::new($letterbox), $x, $y, $imageWidth, $imageHeight)
                    $image = [System.Drawing.Image]::FromFile($files[$index].FullName)
                    try {
                        $scale = [math]::Min($imageWidth / [double]$image.Width, $imageHeight / [double]$image.Height)
                        $drawWidth = [int][math]::Round($image.Width * $scale)
                        $drawHeight = [int][math]::Round($image.Height * $scale)
                        $drawX = $x + [int](($imageWidth - $drawWidth) / 2)
                        $drawY = $y + [int](($imageHeight - $drawHeight) / 2)
                        $graphics.DrawImage($image, $drawX, $drawY, $drawWidth, $drawHeight)
                    } finally {
                        $image.Dispose()
                    }
                    $graphics.DrawString($files[$index].BaseName, $font, $labelBrush, $x + 4, $y + $imageHeight + 4)
                }
            } finally {
                $graphics.Dispose()
            }
            $path = Join-Path $output ("contact_sheet_{0:D2}.png" -f ($page + 1))
            $sheet.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        } finally {
            $sheet.Dispose()
        }
    }
} finally {
    $font.Dispose()
}

Write-Output "Audited $($files.Count) images and generated $pageCount contact sheets in $output"
