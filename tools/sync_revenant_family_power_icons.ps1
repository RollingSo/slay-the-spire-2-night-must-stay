$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$sourceDirectory = Join-Path $root 'revenant_assets\powers'
$targets = @(
    Join-Path $root 'images\powers'
    Join-Path $root 'powers'
)
$revenantPowerIcons = @(
    'helen_step_strike_power.png'
    'helen_retreat_power.png'
    'frederick_heavy_hammer_power.png'
    'frederick_headbutt_power.png'
    'sebastian_roar_power.png'
    'sebastian_slam_power.png'
    'freeze_power.png'
    'white_shadow_lure_power.png'
    'soulguard_power.png'
    'spirit_form_power.png'
    'ancient_dragon_faith_power.png'
    'beast_claw_mark_power.png'
    'golden_order_power.png'
    'blessing_of_grace_power.png'
    'spirit_link_power.png'
    'undying_march_power.png'
    'frenzied_three_fingers_power.png'
    'fight_for_me_power.png'
    'light_spirit_power.png'
    'heavy_echo_power.png'
    'chanting_blessing_power.png'
    'following_shadow_power.png'
    'necromancy_power.png'
    'mutual_understanding_power.png'
    'change_hands_power.png'
    'relay_power.png'
    'pack_up_power.png'
)
$updatedCount = 0
$unchangedCount = 0

Add-Type -AssemblyName System.Drawing

foreach ($fileName in $revenantPowerIcons) {
    $sourcePath = Join-Path $sourceDirectory $fileName
    if (-not (Test-Path -LiteralPath $sourcePath)) {
        throw "Missing Revenant power icon: $sourcePath"
    }

    $bitmap = [System.Drawing.Bitmap]::FromFile($sourcePath)
    try {
        if ($bitmap.Width -ne 256 -or $bitmap.Height -ne 256) {
            throw "Revenant power icon must be 256x256: $sourcePath"
        }

        $corners = @(
            $bitmap.GetPixel(0, 0).A
            $bitmap.GetPixel(255, 0).A
            $bitmap.GetPixel(0, 255).A
            $bitmap.GetPixel(255, 255).A
        )
        if ($corners | Where-Object { $_ -ne 0 }) {
            throw "Revenant power icon has a non-transparent corner: $sourcePath"
        }
    }
    finally {
        $bitmap.Dispose()
    }

    $sourceHash = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
    foreach ($targetDirectory in $targets) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
        $targetPath = Join-Path $targetDirectory $fileName
        if ((Test-Path -LiteralPath $targetPath) -and
            $sourceHash -eq (Get-FileHash -LiteralPath $targetPath -Algorithm SHA256).Hash) {
            $unchangedCount++
            continue
        }

        Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force
        $updatedCount++
    }
}

Write-Output "Revenant power icons: validated $($revenantPowerIcons.Count), updated $updatedCount, unchanged $unchangedCount."
