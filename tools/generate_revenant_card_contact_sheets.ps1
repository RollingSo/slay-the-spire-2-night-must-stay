param(
    [string]$ProjectRoot = 'D:\sts-2-mod'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$previewDir = Join-Path $ProjectRoot 'design\卡图预览\复仇者重制'
$files = Get-ChildItem -LiteralPath $previewDir -Filter '*_preview.png' |
    Sort-Object Name

$columns = 4
$rows = 3
$cellWidth = 300
$imageHeight = 228
$labelHeight = 22
$sheetWidth = $columns * $cellWidth
$sheetHeight = $rows * ($imageHeight + $labelHeight)
$font = [System.Drawing.Font]::new('Arial', 10)
$brush = [System.Drawing.Brushes]::White

try {
    for ($page = 0; $page * ($columns * $rows) -lt $files.Count; $page++) {
        $sheet = [System.Drawing.Bitmap]::new($sheetWidth, $sheetHeight)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($sheet)
            try {
                $graphics.Clear([System.Drawing.Color]::FromArgb(28, 29, 33))
                for ($slot = 0; $slot -lt $columns * $rows; $slot++) {
                    $index = $page * ($columns * $rows) + $slot
                    if ($index -ge $files.Count) { break }
                    $row = [math]::Floor($slot / $columns)
                    $column = $slot % $columns
                    $x = $column * $cellWidth
                    $y = $row * ($imageHeight + $labelHeight)
                    $image = [System.Drawing.Image]::FromFile($files[$index].FullName)
                    try {
                        $graphics.DrawImage($image, $x, $y, $cellWidth, $imageHeight)
                    } finally {
                        $image.Dispose()
                    }
                    $label = [System.IO.Path]::GetFileNameWithoutExtension($files[$index].Name)
                    $graphics.DrawString($label, $font, $brush, $x + 3, $y + $imageHeight + 2)
                }
            } finally {
                $graphics.Dispose()
            }
            $destination = Join-Path $previewDir ("qa_contact_{0}.png" -f ($page + 1))
            $sheet.Save($destination, [System.Drawing.Imaging.ImageFormat]::Png)
        } finally {
            $sheet.Dispose()
        }
    }
} finally {
    $font.Dispose()
}

Write-Output "Generated $([math]::Ceiling($files.Count / [double]($columns * $rows))) contact sheets for $($files.Count) card candidates."
