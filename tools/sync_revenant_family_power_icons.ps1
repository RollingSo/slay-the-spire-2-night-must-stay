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
)

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

    foreach ($targetDirectory in $targets) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
        Copy-Item -LiteralPath $sourcePath -Destination (Join-Path $targetDirectory $fileName) -Force
    }
}

Write-Output "Synced and validated $($revenantPowerIcons.Count) Revenant power icons."
