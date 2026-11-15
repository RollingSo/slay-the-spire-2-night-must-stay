param([string]$ManifestPath = '')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (!$ManifestPath) { $ManifestPath = Join-Path $root 'build/wylder_power_icon_paths.json' }
$ids = @('wylder_endless_will_power','wylder_shriek_of_milos_power','wylder_heroes_assemble_power','wylder_murderous_aura_power','wylder_forget_the_sword_power','wylder_valor_strength_power','wylder_lone_courage_power','wylder_agility_power','wylder_life_from_death_power','wylder_martial_mastery_power','wylder_multiplayer_power','wylder_offensive_instinct_power','wylder_ruinous_ghostflame_power','wylder_boldness_power','wylder_veteran_edge_power','wylder_night_form_power','wylder_remaining_ember_power','wylder_eruption_power','wylder_wildfire_power','wylder_undying_power','wylder_flamelight_on_snow_power','wylder_fates_judgment_power','wylder_pursuit_power','wylder_uppercut_strength_down_power','wylder_break_description_power','freeze_power','phantom_imbalance_power','phantom_imbalance_threshold_power')
$paths = foreach ($id in $ids) {
    foreach ($rel in @("images/atlases/power_atlas.sprites/$id.tres","images/powers/$id.png","powers/$id.png")) {
        if (!(Test-Path -LiteralPath (Join-Path $root $rel) -PathType Leaf)) { throw "Missing power icon: $rel" }
        "res://$rel"
    }
}
Add-Type -AssemblyName System.Drawing
foreach ($id in $ids) {
    foreach ($rel in @("images/powers/$id.png","powers/$id.png")) {
        $path = Join-Path $root $rel
        $bitmap = [System.Drawing.Bitmap]::FromFile($path)
        try {
            if ($bitmap.Width -ne 256 -or $bitmap.Height -ne 256) {
                throw "Power icon must be 256x256: $rel"
            }
            if (![System.Drawing.Image]::IsAlphaPixelFormat($bitmap.PixelFormat)) {
                throw "Power icon must contain a true alpha channel: $rel"
            }
            $cornerAlpha = @(
                $bitmap.GetPixel(0, 0).A,
                $bitmap.GetPixel(255, 0).A,
                $bitmap.GetPixel(0, 255).A,
                $bitmap.GetPixel(255, 255).A
            )
            if ($cornerAlpha | Where-Object { $_ -ne 0 }) {
                throw "Power icon corners must be fully transparent: $rel ($($cornerAlpha -join ','))"
            }
        }
        finally {
            $bitmap.Dispose()
        }
    }
    $compactSource = Join-Path $root "images/powers/$id.png"
    $flashSource = Join-Path $root "powers/$id.png"
    if ((Get-FileHash -LiteralPath $compactSource -Algorithm SHA256).Hash -ne
        (Get-FileHash -LiteralPath $flashSource -Algorithm SHA256).Hash) {
        throw "Compact and flash power icons must use the same artwork: $id"
    }
}
$assetPatchPath = Join-Path $root 'src/Core/Patches/WylderAssetPatch.cs'
$assetPatch = Get-Content -LiteralPath $assetPatchPath -Raw
foreach ($id in @(
    'wylder_valor_strength_power',
    'wylder_lone_courage_power',
    'wylder_agility_power',
    'wylder_uppercut_strength_down_power')) {
    if (!$assetPatch.Contains('"' + $id + '"')) {
        throw "Wylder temporary stat power lacks explicit runtime icon resolution: $id"
    }
}
foreach ($method in @(
    'ResolveWylderTemporaryStatPowerBigIconPath',
    'ResolveWylderTemporaryStatPowerIcon',
    'ResolveWylderTemporaryStatPowerBigIcon')) {
    if (!$assetPatch.Contains($method)) {
        throw "Wylder temporary stat icon runtime patch is incomplete: $method"
    }
}
@{paths=@($paths)} | ConvertTo-Json | Set-Content -LiteralPath $ManifestPath -Encoding utf8
