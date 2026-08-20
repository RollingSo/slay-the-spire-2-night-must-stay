param(
    [string]$GodotPath = 'D:\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64_console.exe',
    [string]$ModsDirectory = 'D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods',
    [switch]$SkipInstall
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$buildDirectory = Join-Path $root 'build'
$projectPath = Join-Path $root 'sts2mod.csproj'
$manifestPath = Join-Path $root 'manifest.json'
$configPath = Join-Path $root 'config.json'
$releaseDirectory = Join-Path $root '.godot\mono\temp\bin\CodexExport'
$packPath = Join-Path $buildDirectory 'sts2mod.pck'

New-Item -ItemType Directory -Path $buildDirectory -Force | Out-Null
if (-not $SkipInstall) {
    New-Item -ItemType Directory -Path $ModsDirectory -Force | Out-Null
}

# Keep Guardian glossary terms highlighted in every card description. The
# explicit policy bypass also handles freshly-created local validation scripts.
powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'validate_guardian_card_localization.ps1')
if ($LASTEXITCODE -ne 0) {
    throw "Guardian card localization validation failed with exit code $LASTEXITCODE"
}

# Enforce shared card-text rules: one source for canonical keywords, sentence
# line breaks, highlighted mechanics, and upgraded generated-card previews.
powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'validate_card_text_format.ps1')
if ($LASTEXITCODE -ne 0) {
    throw "Card text format validation failed with exit code $LASTEXITCODE"
}

# Keep the compact icon and the large applied/triggered power flash in sync.
& (Join-Path $PSScriptRoot 'sync_guardian_power_icons.ps1')

# Family action powers use the standard PowerModel big-icon lookup under
# images/powers in addition to their compact atlas textures. Keep both copies
# synchronized and reject malformed alpha before Godot import.
& (Join-Path $PSScriptRoot 'sync_revenant_family_power_icons.ps1')

# Import changed images completely before creating the PCK. `--import` waits for
# the import queue to finish; `--editor --quit` may exit before new textures are ready.
& $GodotPath --headless --path $root --import --quit
if ($LASTEXITCODE -ne 0) {
    throw "Godot asset import failed with exit code $LASTEXITCODE"
}

& $GodotPath --headless --path $root --export-pack 'Windows Desktop' $packPath --quit
if ($LASTEXITCODE -ne 0) {
    throw "Godot PCK export failed with exit code $LASTEXITCODE"
}

dotnet build $projectPath -c Release --no-restore `
    -p:IntermediateOutputPath=.godot\mono\temp\obj\CodexExport\ `
    -p:OutputPath=.godot\mono\temp\bin\CodexExport\
if ($LASTEXITCODE -ne 0) {
    throw "Release build failed with exit code $LASTEXITCODE"
}

$runtimeFiles = @(
    'sts2mod.dll',
    'sts2mod.pdb',
    'sts2mod.deps.json',
    'sts2mod.runtimeconfig.json'
)
foreach ($fileName in $runtimeFiles) {
    Copy-Item -LiteralPath (Join-Path $releaseDirectory $fileName) -Destination (Join-Path $buildDirectory $fileName) -Force
}

# Distribution metadata and telemetry configuration must stay beside the DLL.
# Keep them in build as well so build/ and the installed Mods directory contain
# the exact same publishable file set.
Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $buildDirectory 'sts2mod.json') -Force
Copy-Item -LiteralPath $configPath -Destination (Join-Path $buildDirectory 'config.json') -Force

$installFiles = @('sts2mod.pck', 'sts2mod.json', 'sts2mod.dll', 'sts2mod.pdb')

# Keep telemetry configuration outside Mods.  The complete publishable set is
# still preserved in build/, while the game sees only its one manifest JSON.
if (-not $SkipInstall) {
    $telemetryDirectory = Join-Path $env:APPDATA 'SlayTheSpire2\night_must_stay'
    New-Item -ItemType Directory -Path $telemetryDirectory -Force | Out-Null
    Copy-Item -LiteralPath $configPath -Destination (Join-Path $telemetryDirectory 'config.json') -Force
}
foreach ($fileName in $installFiles) {
    $buildPath = Join-Path $buildDirectory $fileName
    $buildHash = (Get-FileHash -LiteralPath $buildPath -Algorithm SHA256).Hash

    if ($SkipInstall) {
        Write-Output "$fileName`t$buildHash`t(build only)"
        continue
    }

    $installedPath = Join-Path $ModsDirectory $fileName
    Copy-Item -LiteralPath $buildPath -Destination $installedPath -Force
    $installedHash = (Get-FileHash -LiteralPath $installedPath -Algorithm SHA256).Hash
    if ($buildHash -ne $installedHash) {
        throw "Installed file hash mismatch: $fileName"
    }

    Write-Output "$fileName`t$buildHash"
}
