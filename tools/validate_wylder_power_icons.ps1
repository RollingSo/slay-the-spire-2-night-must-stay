param([string]$ManifestPath = '')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (!$ManifestPath) { $ManifestPath = Join-Path $root 'build/wylder_power_icon_paths.json' }
$ids = @('wylder_ruinous_ghostflame_power','wylder_boldness_power','wylder_veteran_edge_power','wylder_night_form_power','wylder_remaining_ember_power','wylder_undying_power','wylder_pursuit_power','wylder_uppercut_strength_down_power','wylder_break_description_power','freeze_power','phantom_imbalance_power')
$paths = foreach ($id in $ids) {
    foreach ($rel in @("images/atlases/power_atlas.sprites/$id.tres","images/powers/$id.png","powers/$id.png")) {
        if (!(Test-Path -LiteralPath (Join-Path $root $rel) -PathType Leaf)) { throw "Missing power icon: $rel" }
        "res://$rel"
    }
}
@{paths=@($paths)} | ConvertTo-Json | Set-Content -LiteralPath $ManifestPath -Encoding utf8
