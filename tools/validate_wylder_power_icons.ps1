param([string]$ManifestPath = '')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (!$ManifestPath) { $ManifestPath = Join-Path $root 'build/wylder_power_icon_paths.json' }
$ids = @('wylder_endless_will_power','wylder_shriek_of_milos_power','wylder_heroes_assemble_power','wylder_murderous_aura_power','wylder_forget_the_sword_power','wylder_lone_courage_power','wylder_agility_power','wylder_life_from_death_power','wylder_martial_mastery_power','wylder_multiplayer_power','wylder_offensive_instinct_power','wylder_ruinous_ghostflame_power','wylder_boldness_power','wylder_veteran_edge_power','wylder_night_form_power','wylder_remaining_ember_power','wylder_eruption_power','wylder_wildfire_power','wylder_undying_power','wylder_pursuit_power','wylder_uppercut_strength_down_power','wylder_break_description_power','freeze_power','phantom_imbalance_power')
$paths = foreach ($id in $ids) {
    foreach ($rel in @("images/atlases/power_atlas.sprites/$id.tres","images/powers/$id.png","powers/$id.png")) {
        if (!(Test-Path -LiteralPath (Join-Path $root $rel) -PathType Leaf)) { throw "Missing power icon: $rel" }
        "res://$rel"
    }
}
@{paths=@($paths)} | ConvertTo-Json | Set-Content -LiteralPath $ManifestPath -Encoding utf8
