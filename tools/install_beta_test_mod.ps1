param(
    [string]$ModsDirectory = 'D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods'
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$buildDirectory = Join-Path $root 'build'
$manifestPath = Join-Path $root 'manifest.json'
$stableId = 'NightMustStay'
$betaId = 'NightMustStayBetaTest'
$resolvedModsDirectory = [System.IO.Path]::GetFullPath($ModsDirectory).TrimEnd('\')

New-Item -ItemType Directory -Path $resolvedModsDirectory -Force | Out-Null

$requiredBuildFiles = @("$stableId.pck", "$stableId.dll", "$stableId.pdb")
foreach ($fileName in $requiredBuildFiles) {
    $path = Join-Path $buildDirectory $fileName
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing export artifact: $path. Run export_guardian_mod.ps1 -SkipInstall first."
    }
}

$stableManifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$betaManifest = [ordered]@{
    id = $betaId
    name = "$($stableManifest.name) [Beta Test]"
    author = $stableManifest.author
    description = "Local Beta Test build. Do not enable together with the Steam Workshop release. $($stableManifest.description)"
    version = "$($stableManifest.version)-beta-test"
    has_dll = $true
    has_pck = $true
    affects_gameplay = $true
}
$betaManifestPath = Join-Path $buildDirectory "$betaId.json"
$betaManifest | ConvertTo-Json | Set-Content -LiteralPath $betaManifestPath -Encoding utf8

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

$copies = [ordered]@{
    "$betaId.pck" = Join-Path $buildDirectory "$stableId.pck"
    "$betaId.dll" = Join-Path $buildDirectory "$stableId.dll"
    "$betaId.pdb" = Join-Path $buildDirectory "$stableId.pdb"
    "$betaId.json" = $betaManifestPath
}
foreach ($entry in $copies.GetEnumerator()) {
    $destination = Join-Path $resolvedModsDirectory $entry.Key
    Copy-Item -LiteralPath $entry.Value -Destination $destination -Force
    $sourceHash = (Get-FileHash -LiteralPath $entry.Value -Algorithm SHA256).Hash
    $destinationHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
    if ($sourceHash -ne $destinationHash) {
        throw "Installed file hash mismatch: $($entry.Key)"
    }
    Write-Output "$($entry.Key)`t$destinationHash"
}
