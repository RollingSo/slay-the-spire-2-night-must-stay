param(
    [Parameter(Mandatory = $true)][string]$HelenCombatSource,
    [Parameter(Mandatory = $true)][string]$FrederickCombatSource,
    [Parameter(Mandatory = $true)][string]$SebastianCombatSource,
    [Parameter(Mandatory = $true)][string]$HelenChoiceSource,
    [Parameter(Mandatory = $true)][string]$FrederickChoiceSource,
    [Parameter(Mandatory = $true)][string]$SebastianChoiceSource
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$repoRoot = Split-Path -Parent $PSScriptRoot

function Get-AlphaBounds {
    param([System.Drawing.Bitmap]$Bitmap)

    $minX = $Bitmap.Width
    $minY = $Bitmap.Height
    $maxX = -1
    $maxY = -1

    for ($y = 0; $y -lt $Bitmap.Height; $y++) {
        for ($x = 0; $x -lt $Bitmap.Width; $x++) {
            if ($Bitmap.GetPixel($x, $y).A -le 4) { continue }
            if ($x -lt $minX) { $minX = $x }
            if ($y -lt $minY) { $minY = $y }
            if ($x -gt $maxX) { $maxX = $x }
            if ($y -gt $maxY) { $maxY = $y }
        }
    }

    if ($maxX -lt $minX -or $maxY -lt $minY) {
        throw 'Source image has no visible pixels.'
    }

    return [System.Drawing.Rectangle]::new($minX, $minY, $maxX - $minX + 1, $maxY - $minY + 1)
}

function Save-Png {
    param([System.Drawing.Bitmap]$Bitmap, [string]$Destination)

    $parent = Split-Path -Parent $Destination
    [System.IO.Directory]::CreateDirectory($parent) | Out-Null
    $Bitmap.Save($Destination, [System.Drawing.Imaging.ImageFormat]::Png)
}

function Convert-CombatSprite {
    param([string]$Source, [string]$Destination)

    $sourceBitmap = [System.Drawing.Bitmap]::new($Source)
    try {
        $bounds = Get-AlphaBounds $sourceBitmap
        $canvasSize = 512
        $margin = 12
        $usable = $canvasSize - (2 * $margin)
        $scale = [Math]::Min($usable / $bounds.Width, $usable / $bounds.Height)
        $width = [Math]::Max(1, [int][Math]::Round($bounds.Width * $scale))
        $height = [Math]::Max(1, [int][Math]::Round($bounds.Height * $scale))
        $left = [int][Math]::Round(($canvasSize - $width) / 2)
        $top = $canvasSize - $margin - $height

        $output = [System.Drawing.Bitmap]::new($canvasSize, $canvasSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($output)
            try {
                $graphics.Clear([System.Drawing.Color]::Transparent)
                $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
                $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $destinationRect = [System.Drawing.Rectangle]::new($left, $top, $width, $height)
                $graphics.DrawImage($sourceBitmap, $destinationRect, $bounds, [System.Drawing.GraphicsUnit]::Pixel)
            }
            finally {
                $graphics.Dispose()
            }
            Save-Png $output $Destination
        }
        finally {
            $output.Dispose()
        }
    }
    finally {
        $sourceBitmap.Dispose()
    }
}

function Convert-ChoiceArt {
    param([string]$Source, [string]$Destination)

    $sourceBitmap = [System.Drawing.Bitmap]::new($Source)
    try {
        $output = [System.Drawing.Bitmap]::new(1000, 760, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($output)
            try {
                $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $graphics.DrawImage($sourceBitmap, 0, 0, 1000, 760)
            }
            finally {
                $graphics.Dispose()
            }
            Save-Png $output $Destination
        }
        finally {
            $output.Dispose()
        }
    }
    finally {
        $sourceBitmap.Dispose()
    }
}

$combatJobs = @(
    @($HelenCombatSource, (Join-Path $repoRoot 'revenant_assets\families\helen.png')),
    @($FrederickCombatSource, (Join-Path $repoRoot 'revenant_assets\families\frederick.png')),
    @($SebastianCombatSource, (Join-Path $repoRoot 'revenant_assets\families\sebastian.png'))
)

$choiceJobs = @(
    @($HelenChoiceSource, (Join-Path $repoRoot 'revenant_assets\cards\helen_family.png')),
    @($FrederickChoiceSource, (Join-Path $repoRoot 'revenant_assets\cards\pumpkin_head_family.png')),
    @($SebastianChoiceSource, (Join-Path $repoRoot 'revenant_assets\cards\skeleton_family.png'))
)

foreach ($job in $combatJobs) {
    Convert-CombatSprite -Source $job[0] -Destination $job[1]
}

foreach ($job in $choiceJobs) {
    Convert-ChoiceArt -Source $job[0] -Destination $job[1]
}

foreach ($job in $combatJobs) {
    $bitmap = [System.Drawing.Bitmap]::new($job[1])
    try {
        $bounds = Get-AlphaBounds $bitmap
        $corners = @(
            $bitmap.GetPixel(0, 0).A,
            $bitmap.GetPixel($bitmap.Width - 1, 0).A,
            $bitmap.GetPixel(0, $bitmap.Height - 1).A,
            $bitmap.GetPixel($bitmap.Width - 1, $bitmap.Height - 1).A
        ) -join ','
        Write-Output ('{0}|{1}x{2}|visible={3}x{4}|alpha={5}' -f $job[1], $bitmap.Width, $bitmap.Height, $bounds.Width, $bounds.Height, $corners)
    }
    finally {
        $bitmap.Dispose()
    }
}

foreach ($job in $choiceJobs) {
    $bitmap = [System.Drawing.Bitmap]::new($job[1])
    try {
        Write-Output ('{0}|{1}x{2}' -f $job[1], $bitmap.Width, $bitmap.Height)
    }
    finally {
        $bitmap.Dispose()
    }
}
