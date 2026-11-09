$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

$requiredRoutes = [ordered]@{
    'src/Core/Models/Cards/WylderCards.cs' = @('Kind.Greatsword', 'PlayBlock')
    'src/Core/Models/Cards/WylderBattleArtCards.cs' = @('Kind.VacuumSlice', 'Kind.StormBlade', 'Kind.FlameSlash', 'Kind.FireExplosion')
    'src/Core/Models/Cards/WylderGrappleShot.cs' = @('"ClawShot"', 'Kind.Claw')
    'src/Core/Models/Cards/WylderFlameChaseSlash.cs' = @('Kind.FlameSlash')
    'src/Core/Models/Cards/WylderCompletionCardsA.cs' = @('Kind.CorpseWax')
    'src/Core/Models/Cards/WylderCompletionCardsB.cs' = @('Kind.TakersFlames')
    'src/Core/Models/Cards/WylderPlannerCards.cs' = @('Kind.GhostflameExplosion')
    'src/Core/Models/Cards/WylderVortexBatch.cs' = @('Kind.OrdovisVortex', 'Kind.SiluriaVortex')
    'src/Core/Models/Cards/WylderNovemberBatch.cs' = @('PlayPrelude', 'WithEndTheNightFx')
}

foreach ($entry in $requiredRoutes.GetEnumerator()) {
    $path = Join-Path $root $entry.Key
    $text = Get-Content -LiteralPath $path -Raw
    foreach ($marker in $entry.Value) {
        if (-not $text.Contains($marker)) { throw "Missing Wylder animation/VFX route: $($entry.Key) -> $marker" }
    }
}

$rig = Get-Content -LiteralPath (Join-Path $root 'wylder_assets/combat_rig/wylder_combat_rig.gd') -Raw
foreach ($trigger in @('AttackSingle', 'AttackTwoHand', 'BucklerBlock', 'ClawShot', 'Ultimate')) {
    if (-not $rig.Contains($trigger)) { throw "Missing Wylder rig trigger: $trigger" }
}

Add-Type -AssemblyName System.Drawing
foreach ($relative in @('wylder_assets/combat_rig/wylder_claw_shot.png', 'wylder_assets/combat_rig/wylder_ultimate.png')) {
    $path = Join-Path $root $relative
    $bitmap = [System.Drawing.Bitmap]::FromFile($path)
    try {
        $corners = @(
            $bitmap.GetPixel(0, 0).A,
            $bitmap.GetPixel($bitmap.Width - 1, 0).A,
            $bitmap.GetPixel(0, $bitmap.Height - 1).A,
            $bitmap.GetPixel($bitmap.Width - 1, $bitmap.Height - 1).A
        )
        if (($corners | Where-Object { $_ -ne 0 }).Count -ne 0) { throw "$relative does not have true-alpha corners" }
    }
    finally { $bitmap.Dispose() }
}

Write-Output 'PASS: Wylder five motion triggers, eleven bounded VFX families, End the Night staging, route markers and transparent pose assets.'
