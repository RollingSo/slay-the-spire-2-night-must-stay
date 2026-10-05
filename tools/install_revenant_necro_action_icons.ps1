param(
    [Parameter(Mandatory)][string]$SwordPath,
    [Parameter(Mandatory)][string]$ShieldPath
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$projectRoot = Split-Path -Parent $PSScriptRoot
$sources = @{
    'necro_attack_power.png' = $SwordPath
    'necro_protect_power.png' = $ShieldPath
}
foreach ($entry in $sources.GetEnumerator()) {
    $sourceImage = [System.Drawing.Bitmap]::FromFile($entry.Value)
    try {
        foreach ($corner in @(@(0,0), @(($sourceImage.Width-1),0), @(0,($sourceImage.Height-1)), @(($sourceImage.Width-1),($sourceImage.Height-1)))) {
            if ($sourceImage.GetPixel($corner[0],$corner[1]).A -ne 0) {
                throw "Source icon must have true transparent corners: $($entry.Value)"
            }
        }
        $icon = [System.Drawing.Bitmap]::new(256,256,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($icon)
            try {
                $graphics.Clear([System.Drawing.Color]::Transparent)
                $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.DrawImage($sourceImage,0,0,256,256)
            } finally { $graphics.Dispose() }
            $destination = Join-Path $projectRoot "revenant_assets/powers/$($entry.Key)"
            if (Test-Path -LiteralPath $destination) { throw "Refusing to overwrite existing icon: $destination" }
            $icon.Save($destination,[System.Drawing.Imaging.ImageFormat]::Png)
        } finally { $icon.Dispose() }
    } finally { $sourceImage.Dispose() }
}
& (Join-Path $PSScriptRoot 'sync_revenant_family_power_icons.ps1')
