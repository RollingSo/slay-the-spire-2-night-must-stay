param(
    [string]$GodotPath = 'D:\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64_console.exe',
    [string]$ModsDirectory = 'D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods',
    [switch]$SkipInstall,
    [switch]$BetaTestInstall
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$buildDirectory = Join-Path $root 'build'
$modId = 'NightMustStay'
$projectPath = Join-Path $root "$modId.csproj"
$manifestPath = Join-Path $root 'manifest.json'
$configPath = Join-Path $root 'config.json'
$releaseDirectory = Join-Path $root '.godot\mono\temp\bin\CodexExport'
$packPath = Join-Path $buildDirectory "$modId.pck"
$installModId = if ($BetaTestInstall) { 'NightMustStayBetaTest' } else { $modId }

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

& $GodotPath --headless --path $root --export-pack 'Windows Desktop' $packPath
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
    "$modId.dll",
    "$modId.pdb",
    "$modId.deps.json",
    "$modId.runtimeconfig.json"
)
foreach ($fileName in $runtimeFiles) {
    Copy-Item -LiteralPath (Join-Path $releaseDirectory $fileName) -Destination (Join-Path $buildDirectory $fileName) -Force
}

# Keep the manifest in build for staging. The telemetry default is embedded in
# the DLL; config.json remains here only as the editable build input/template.
Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $buildDirectory "$modId.json") -Force
Copy-Item -LiteralPath $configPath -Destination (Join-Path $buildDirectory 'config.json') -Force

$installSources = [ordered]@{
    "$installModId.pck" = Join-Path $buildDirectory "$modId.pck"
    "$installModId.dll" = Join-Path $buildDirectory "$modId.dll"
    "$installModId.pdb" = Join-Path $buildDirectory "$modId.pdb"
}

if ($BetaTestInstall) {
    $stableManifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $betaManifest = [ordered]@{
        id = $installModId
        name = "$($stableManifest.name) [Beta Test]"
        author = $stableManifest.author
        description = "Local Beta Test build. Do not enable together with the Steam Workshop release. $($stableManifest.description)"
        version = "$($stableManifest.version)-beta-test"
        has_dll = $true
        has_pck = $true
        affects_gameplay = $true
    }
    $betaManifestPath = Join-Path $buildDirectory "$installModId.json"
    $betaManifest | ConvertTo-Json | Set-Content -LiteralPath $betaManifestPath -Encoding utf8
    $installSources["$installModId.json"] = $betaManifestPath
}
else {
    $installSources["$installModId.json"] = Join-Path $buildDirectory "$modId.json"
}

# Keep the local developer override outside Mods, while the game sees only its
# one manifest JSON. Workshop users fall back to the DLL-embedded default.
if (-not $SkipInstall) {
    $telemetryDirectory = Join-Path $env:APPDATA 'SlayTheSpire2\night_must_stay'
    New-Item -ItemType Directory -Path $telemetryDirectory -Force | Out-Null
    Copy-Item -LiteralPath $configPath -Destination (Join-Path $telemetryDirectory 'config.json') -Force
}
if (-not $SkipInstall -and $BetaTestInstall) {
    $resolvedModsDirectory = [System.IO.Path]::GetFullPath($ModsDirectory).TrimEnd('\')
    $legacyNames = @(
        'NightMustStay.pck', 'NightMustStay.json', 'NightMustStay.dll', 'NightMustStay.pdb',
        'NightMustStayBetaTest.pck', 'NightMustStayBetaTest.json', 'NightMustStayBetaTest.dll', 'NightMustStayBetaTest.pdb',
        'sts2mod.pck', 'sts2mod.json', 'sts2mod.dll', 'sts2mod.pdb'
    )
    foreach ($legacyName in $legacyNames) {
        $legacyPath = [System.IO.Path]::GetFullPath((Join-Path $resolvedModsDirectory $legacyName))
        if (-not $legacyPath.StartsWith($resolvedModsDirectory + '\', [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove a file outside the Mods directory: $legacyPath"
        }
        if (Test-Path -LiteralPath $legacyPath) {
            Remove-Item -LiteralPath $legacyPath -Force
        }
    }
}

foreach ($entry in $installSources.GetEnumerator()) {
    $fileName = $entry.Key
    $buildPath = $entry.Value
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
