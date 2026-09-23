param([switch]$AllowMissing)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$errors = [System.Collections.Generic.List[string]]::new()
$checks = 0
function Test-DuchessImage([string]$RelativePath, [int]$Width, [int]$Height, [bool]$Alpha) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        if (-not $AllowMissing) { $errors.Add("Missing: $RelativePath") }
        return
    }
    $script:checks++
    $bitmap = [System.Drawing.Bitmap]::FromFile($path)
    try {
        if ($Width -gt 0 -and ($bitmap.Width -ne $Width -or $bitmap.Height -ne $Height)) {
            $errors.Add("Wrong dimensions: $RelativePath ($($bitmap.Width)x$($bitmap.Height), expected ${Width}x${Height})")
        }
        if ($Alpha) {
            if (($bitmap.PixelFormat -band [System.Drawing.Imaging.PixelFormat]::Alpha) -eq 0) {
                $errors.Add("Missing real alpha: $RelativePath")
            }
            foreach ($point in @(@(0,0), @(($bitmap.Width-1),0), @(0,($bitmap.Height-1)), @(($bitmap.Width-1),($bitmap.Height-1)))) {
                if ($bitmap.GetPixel($point[0],$point[1]).A -ne 0) {
                    $errors.Add("Nontransparent corner: $RelativePath")
                    break
                }
            }
        }
    } finally { $bitmap.Dispose() }
}
$rows = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'design/duchess/cards.json') | ConvertFrom-Json
foreach ($row in $rows) {
    $id = [regex]::Replace(('Duchess' + $row[0]), '(?<!^)(?=[A-Z])', '_').ToLowerInvariant()
    if ($row[0] -eq 'ElegantBearing') { $id = 'duchess_elegant_bearing' }
    if ($row[0] -eq 'Dodge') { $id = 'duchess_dodge' }
    if ($row[6] -eq 'Ancient') { Test-DuchessImage "images/packed/card_portraits/duchess/$id.png" 606 852 $false }
    else { Test-DuchessImage "images/packed/card_portraits/duchess/$id.png" 1000 760 $false }
}
$powers = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'NightMustStay/localization/zhs/powers.json') | ConvertFrom-Json
foreach ($key in $powers.PSObject.Properties.Name | Where-Object { $_ -like 'DUCHESS_*.title' }) {
    $id = $key.Replace('.title','').ToLowerInvariant()
    Test-DuchessImage "images/powers/$id.png" 256 256 $true
    Test-DuchessImage "powers/$id.png" 256 256 $true
    if (-not (Test-Path -LiteralPath (Join-Path $root "images/atlases/power_atlas.sprites/$id.tres")) -and -not $AllowMissing) {
        $errors.Add("Missing power atlas mapping: $id")
    }
}
Test-DuchessImage 'duchess_assets/character_select_duchess_bg.png' 2560 1200 $false
foreach ($name in @('duchess_combat_character','duchess_attack','duchess_hit')) {
    Test-DuchessImage "duchess_assets/combat_rig/$name.png" 0 0 $true
}
foreach ($name in @('character_icon_duchess','character_icon_duchess_outline','char_select_duchess','char_select_duchess_locked','map_marker_duchess')) {
    Test-DuchessImage "duchess_assets/$name.png" 0 0 $true
}
foreach ($name in @('old_pocketwatch','mended_pocketwatch','lace_cuff','silver_thimble','dance_shoes','unsent_letter','blue_ribbon')) {
    Test-DuchessImage "duchess_assets/relics/duchess_$name.png" 256 256 $true
}
foreach ($name in @('silver_perfume','veil_vial','memory_draught')) {
    Test-DuchessImage "duchess_assets/potions/duchess_$name.png" 256 256 $true
    if (-not (Test-Path (Join-Path $root "images/atlases/potion_atlas.sprites/duchess_$name.tres")) -and -not $AllowMissing) {
        $errors.Add("Missing potion atlas mapping: $name")
    }
}
foreach ($i in 1..5) { Test-DuchessImage "duchess_assets/energy_counter/duchess_orb_layer_$i.png" 256 256 $true }
Test-DuchessImage 'duchess_assets/energy_icon/duchess_energy_card_icon.png' 74 74 $true
Test-DuchessImage 'duchess_assets/energy_icon/duchess_energy_font_icon.png' 24 24 $true
Test-DuchessImage 'images/packed/sprite_fonts/duchess_energy_icon.png' 24 24 $true
Test-DuchessImage 'duchess_assets/rest_site/duchess_rest_site.png' 1400 1280 $true
Test-DuchessImage 'duchess_assets/merchant/duchess_merchant.png' 1400 1280 $true
Test-DuchessImage 'duchess_assets/multiplayer_hands/multiplayer_hand_duchess_point.png' 422 1200 $true
foreach ($name in @('rock','paper','scissors')) {
    Test-DuchessImage "duchess_assets/multiplayer_hands/multiplayer_hand_duchess_$name.png" 627 627 $true
}
# Check literal resource paths in the character patch and every Duchess scene.
$sources = @((Join-Path $root 'src/Core/Patches/DuchessAssetPatch.cs')) + @(Get-ChildItem (Join-Path $root 'duchess_assets') -Recurse -File | Where-Object { $_.Extension -in '.tscn','.tres','.gd' } | ForEach-Object FullName)
$nativePaths = @('src/Core/Nodes/Vfx/NCardTrailVfx.cs','src/Core/Nodes/Vfx/NCardTrail.cs','images/packed/vfx/trail.png','images/packed/vfx/trail2.png','themes/canvas_item_material_additive_shared.tres')
foreach ($source in $sources) {
    foreach ($match in [regex]::Matches((Get-Content -Raw $source), 'res://([^"\r\n]+)')) {
        $relative = $match.Groups[1].Value
        if ($relative -in $nativePaths) { continue } # provided by the base game PCK
        if (-not (Test-Path -LiteralPath (Join-Path $root $relative)) -and -not $AllowMissing) {
            $errors.Add("Missing scene/code resource: $relative")
        }
    }
}
if ($errors.Count) { $errors | ForEach-Object { Write-Host $_ }; throw "Duchess asset validation failed: $($errors.Count) errors." }
if ($AllowMissing) { Write-Host "Checked $checks existing Duchess images; missing assets were NOT validated." }
else { Write-Host "Duchess asset validation passed ($checks PNGs)." }
