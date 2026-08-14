param(
    [string]$GodotPath = 'D:\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64_console.exe',
    [string]$ModsDirectory = 'D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods',
    [switch]$SkipInstall
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$buildDirectory = Join-Path $root 'build'
$projectPath = Join-Path $root 'sts2mod.csproj'
$releaseDirectory = Join-Path $root '.godot\mono\temp\bin\Release'
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

# Keep the compact icon and the large applied/triggered power flash in sync.
& (Join-Path $PSScriptRoot 'sync_guardian_power_icons.ps1')

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

dotnet build $projectPath -c Release --no-restore
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

$installFiles = @('sts2mod.pck') + $runtimeFiles
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
