$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Add-Type -AssemblyName System.Drawing
$report = Get-Content (Join-Path $root 'design/wylder/asset_report.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$checked = 0
$uiSizes = @{'wylder_assets/character_icon_wylder.png' = @(88,88); 'wylder_assets/character_icon_wylder_outline.png' = @(64,64); 'wylder_assets/map_marker_wylder.png' = @(49,64)}
foreach ($entry in $report.PSObject.Properties) {
    $path = Join-Path $root $entry.Name
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing Wylder asset: $path" }
    $image = [Drawing.Bitmap]::FromFile($path)
    try {
        if ($uiSizes.ContainsKey($entry.Name) -and ($image.Width -ne $uiSizes[$entry.Name][0] -or $image.Height -ne $uiSizes[$entry.Name][1])) {
            throw "Wylder UI head must match the four-role standard: $path"
        }
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
$pointingHandPath = Join-Path $root 'wylder_assets/multiplayer_hands/multiplayer_hand_wylder_point.png'
$pointingHand = [Drawing.Bitmap]::FromFile($pointingHandPath)
try {
    $tip = $null
    for ($y = 0; $y -lt 100 -and $null -eq $tip; $y++) {
        for ($x = 0; $x -lt $pointingHand.Width; $x++) {
            if ($pointingHand.GetPixel($x, $y).A -gt 32) {
                $tip = @($x, $y)
                break
            }
        }
    }
    if ($null -eq $tip -or $tip[0] -lt 140 -or $tip[0] -gt 160 -or $tip[1] -lt 35 -or $tip[1] -gt 50) {
        throw "Wylder multiplayer pointing-hand cursor hotspot is misaligned: $($tip -join ',')"
    }
} finally { $pointingHand.Dispose() }
Write-Output "Wylder source asset validation passed ($checked assets)."
