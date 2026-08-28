param(
    [string]$ModsDirectory = 'D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods',
    [string]$ReleaseDirectory = 'D:\sts-2-mod\releases',
    [string]$Version,
    [switch]$ReplaceExisting
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$statePath = Join-Path $PSScriptRoot 'release_state.json'
# Keep the script ASCII-safe for Windows PowerShell 5.1 while retaining the
# requested colon-like separator, which is legal in Windows filenames.
$productName = "Slay the Spire 2 $([char]0xFF1A) Night Must Stay"
$modId = 'NightMustStay'
$requiredFiles = @(
    "$modId.dll",
    "$modId.json",
    "$modId.pck",
    "$modId.pdb"
)

function Get-NextVersion([string]$CurrentVersion) {
    $parts = $CurrentVersion.Split('.')
    if ($parts.Count -lt 2) {
        throw "Unsupported release version: $CurrentVersion"
    }

    $major = [int]$parts[0]
    $minor = [int]$parts[1] + 1
    return "$major.$minor"
}

$existingState = $null
if (Test-Path -LiteralPath $statePath) {
    $existingState = Get-Content -LiteralPath $statePath -Raw -Encoding UTF8 |
        ConvertFrom-Json
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    if ($null -ne $existingState) {
        $Version = [string]$existingState.next_version
    }
    else {
        $Version = '0.1'
    }
}

foreach ($fileName in $requiredFiles) {
    $sourcePath = Join-Path $ModsDirectory $fileName
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        throw "Required Mods file is missing: $sourcePath"
    }
}

New-Item -ItemType Directory -Path $ReleaseDirectory -Force | Out-Null
$packageName = "$productName ${Version}.zip"
$packagePath = Join-Path $ReleaseDirectory $packageName
if (Test-Path -LiteralPath $packagePath) {
    if (-not $ReplaceExisting) {
        throw "Release package already exists: $packagePath"
    }

    Remove-Item -LiteralPath $packagePath -Force
}

Add-Type -AssemblyName System.IO.Compression
$fileStream = [System.IO.File]::Open(
    $packagePath,
    [System.IO.FileMode]::CreateNew,
    [System.IO.FileAccess]::ReadWrite,
    [System.IO.FileShare]::None)

try {
    $archive = [System.IO.Compression.ZipArchive]::new(
        $fileStream,
        [System.IO.Compression.ZipArchiveMode]::Create,
        $false)
    try {
        foreach ($fileName in $requiredFiles) {
            $sourcePath = Join-Path $ModsDirectory $fileName
            $entry = $archive.CreateEntry(
                $fileName,
                [System.IO.Compression.CompressionLevel]::Optimal)
            $entryStream = $entry.Open()
            try {
                $input = [System.IO.File]::OpenRead($sourcePath)
                try {
                    $input.CopyTo($entryStream)
                }
                finally {
                    $input.Dispose()
                }
            }
            finally {
                $entryStream.Dispose()
            }
        }
    }
    finally {
        $archive.Dispose()
    }
}
finally {
    $fileStream.Dispose()
}

$verifyStream = [System.IO.File]::OpenRead($packagePath)
try {
    $verifyArchive = [System.IO.Compression.ZipArchive]::new(
        $verifyStream,
        [System.IO.Compression.ZipArchiveMode]::Read,
        $false)
    try {
        $actualEntries = @($verifyArchive.Entries | ForEach-Object { $_.FullName })
        $missing = @($requiredFiles | Where-Object { $_ -notin $actualEntries })
        $extra = @($actualEntries | Where-Object { $_ -notin $requiredFiles })
        if ($missing.Count -gt 0 -or $extra.Count -gt 0) {
            throw "Release archive contents are invalid. Missing: $($missing -join ', '); Extra: $($extra -join ', ')"
        }
    }
    finally {
        $verifyArchive.Dispose()
    }
}
finally {
    $verifyStream.Dispose()
}

$packageHash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash
$nextVersion = Get-NextVersion $Version
if (($null -ne $existingState) -and (([version]([string]$existingState.next_version)) -gt ([version]$nextVersion))) {
    $nextVersion = [string]$existingState.next_version
}

$newState = [ordered]@{
    last_published = $Version
    next_version = $nextVersion
    last_package = $packageName
    last_sha256 = $packageHash
    published_at = (Get-Date).ToString('o')
}
$newState | ConvertTo-Json | Set-Content -LiteralPath $statePath -Encoding UTF8

Write-Output "Package: $packagePath"
Write-Output "SHA256: $packageHash"
Write-Output "Next version: $($newState.next_version)"
