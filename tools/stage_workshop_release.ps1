param(
    [string]$BuildDirectory = 'D:\sts-2-mod\build',
    [string]$StageDirectory = 'D:\sts-2-mod\releases\workshop\NightMustStay'
)

$ErrorActionPreference = 'Stop'

$modId = 'NightMustStay'
$manifestName = "$modId.json"
$contentFiles = @(
    "$modId.dll",
    $manifestName,
    "$modId.pck",
    "$modId.pdb"
)

foreach ($name in $contentFiles) {
    $source = Join-Path $BuildDirectory $name
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Required Workshop source file is missing: $source"
    }
}

$manifest = Get-Content -LiteralPath (Join-Path $BuildDirectory $manifestName) -Raw -Encoding UTF8 |
    ConvertFrom-Json
if ([string]$manifest.id -ne $modId) {
    throw "Workshop manifest ID must be '$modId', got '$($manifest.id)'"
}

New-Item -ItemType Directory -Path $StageDirectory -Force | Out-Null
$unexpectedFiles = @(
    Get-ChildItem -LiteralPath $StageDirectory -File |
        Where-Object { $_.Name -notin $contentFiles }
)
if ($unexpectedFiles.Count -gt 0) {
    throw "Workshop stage contains unexpected files: $($unexpectedFiles.Name -join ', ')"
}

foreach ($name in $contentFiles) {
    $source = Join-Path $BuildDirectory $name
    $target = Join-Path $StageDirectory $name
    Copy-Item -LiteralPath $source -Destination $target -Force

    $sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
    $targetHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
    if ($sourceHash -ne $targetHash) {
        throw "Workshop staging hash mismatch: $name"
    }

    Write-Output "$name`t$targetHash"
}

Write-Output "Workshop content: $StageDirectory"
