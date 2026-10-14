param([string]$GameDirectory = 'F:\SteamLibrary\steamapps\common\Slay the Spire 2')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
if ((git -C $taskRoot branch --show-current) -ne 'animate-0.1') { throw 'Animation test install requires animate-0.1.' }
if (Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue) { throw 'Close Slay the Spire 2 before replacing the local Mod.' }
$taskBuild = Join-Path $taskRoot 'build'
$taskChecks = Get-Content -LiteralPath (Join-Path $taskBuild 'ironeye_animation_checks.json') -Raw | ConvertFrom-Json
if ($taskChecks.failed -ne 0 -or $taskChecks.passed -lt 23) { throw 'Final animation checks have not passed.' }
$taskGame = (Resolve-Path -LiteralPath $GameDirectory).Path
$taskMods = [System.IO.Path]::GetFullPath((Join-Path $taskGame 'mods'))
if (-not $taskMods.StartsWith($taskGame.TrimEnd('\') + '\', [System.StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid installation directory.' }
$taskBackup = Join-Path $taskRoot ('.tmp/ironeye_install_backup_' + (Get-Date -Format 'yyyyMMdd_HHmmss'))
New-Item -ItemType Directory -Path $taskBackup -Force | Out-Null
New-Item -ItemType Directory -Path $taskMods -Force | Out-Null
$taskManifest = Get-Content -LiteralPath (Join-Path $taskBuild 'NightMustStay.json') -Raw | ConvertFrom-Json
$taskManifest.name = $taskManifest.name + ' [animate-0.1]'
$taskDescriptor = Join-Path $taskBuild 'NightMustStay.animate.json'
$taskManifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $taskDescriptor -Encoding utf8
$taskRecord = @()
foreach ($taskName in @('NightMustStay.pck','NightMustStay.dll','NightMustStay.pdb','NightMustStay.json')) {
    $taskSource = if ($taskName -eq 'NightMustStay.json') { $taskDescriptor } else { Join-Path $taskBuild $taskName }
    $taskTarget = Join-Path $taskMods $taskName
    $taskExisted = Test-Path -LiteralPath $taskTarget
    if ($taskExisted) { Copy-Item -LiteralPath $taskTarget -Destination (Join-Path $taskBackup $taskName) }
    Copy-Item -LiteralPath $taskSource -Destination $taskTarget -Force
    $taskHash = (Get-FileHash -LiteralPath $taskSource -Algorithm SHA256).Hash
    if ($taskHash -ne (Get-FileHash -LiteralPath $taskTarget -Algorithm SHA256).Hash) { throw "Installed hash mismatch: $taskName" }
    $taskRecord += [ordered]@{ name=$taskName; target=$taskTarget; sha256=$taskHash; previous_file_existed=$taskExisted }
}
$taskResult = [ordered]@{ branch='animate-0.1'; mods_directory=$taskMods; backup_directory=$taskBackup; installed=$taskRecord }
$taskResult | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $taskBuild 'ironeye_animation_install.json') -Encoding utf8
$taskResult | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $taskBackup 'install_record.json') -Encoding utf8
Write-Output "Installed animation test Mod: $taskMods"
Write-Output "Backup record: $taskBackup"
