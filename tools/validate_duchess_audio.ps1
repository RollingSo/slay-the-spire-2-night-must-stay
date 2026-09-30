param([string]$NativeRoot = 'D:/STS2')
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$sources = @('src/Core/Nodes/Vfx/DuchessAudio.cs', 'src/Core/Nodes/Vfx/DuchessReverseTimeVfx.cs',
    'src/Core/Nodes/Vfx/DuchessStatusVisual.cs', 'src/Core/Nodes/Vfx/DuchessSlashVfx.cs',
    'src/Core/Patches/DuchessAnimationPatch.cs', 'src/Core/Patches/DuchessTransitionPatch.cs')
$samples = foreach ($source in $sources) {
    $text = Get-Content -Raw -LiteralPath (Join-Path $repo $source)
    foreach ($match in [regex]::Matches($text, '"([^"\r\n]+\.mp3)"')) { $match.Groups[1].Value }
}
foreach ($sample in ($samples | Sort-Object -Unique)) {
    $file = Join-Path $NativeRoot "debug_audio/$sample"
    if (-not (Test-Path -LiteralPath $file)) { throw "Missing native Duchess SFX: $file" }
    if ((Get-Item -LiteralPath $file).Length -eq 0) { throw "Empty native SFX: $file" }
    Write-Output "Native SFX OK: $sample"
}
