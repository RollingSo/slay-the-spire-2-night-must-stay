$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Add-Type -AssemblyName System.Drawing
$report = Get-Content (Join-Path $root 'design/wylder/asset_report.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$checked = 0
foreach ($entry in $report.PSObject.Properties) {
    $path = Join-Path $root $entry.Name
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing Wylder asset: $path" }
    $image = [Drawing.Bitmap]::FromFile($path)
    try {
        if ($image.Width -ne $entry.Value.size[0] -or $image.Height -ne $entry.Value.size[1]) {
            throw "Wrong Wylder asset size: $path"
        }
        if ($entry.Value.mode -eq 'RGBA') {
            foreach ($corner in @(@(0,0),@(($image.Width-1),0),@(0,($image.Height-1)),@(($image.Width-1),($image.Height-1)))) {
                if ($image.GetPixel($corner[0],$corner[1]).A -ne 0 -and $entry.Name -notlike '*char_select_wylder*') {
                    throw "Nontransparent Wylder corner: $path"
                }
            }
        }
        if ($entry.Name -like 'images/powers/*' -and ($image.Width -ne 256 -or $image.Height -ne 256)) {
            throw "Wylder power icon must be 256x256: $path"
        }
    } finally { $image.Dispose() }
    $checked++
}
foreach ($power in @('wylder_pursuit_power','wylder_break_description_power','wylder_uppercut_strength_down_power')) {
    if ((Get-FileHash (Join-Path $root "images/powers/$power.png")).Hash -ne (Get-FileHash (Join-Path $root "powers/$power.png")).Hash) {
        throw "Wylder compact/flash power icon mismatch: $power"
    }
}
Write-Output "Wylder source asset validation passed ($checked draft assets)."
