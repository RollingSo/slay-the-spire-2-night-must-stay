param([string]$ManifestPath = '')

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    $ManifestPath = Join-Path $root 'build/ironeye_power_icon_paths.json'
}

$powerSourceFiles = Get-ChildItem -LiteralPath (Join-Path $root 'src/Core/Models/Power') -File -Filter 'IroneyePowers*.cs'
$classNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($file in $powerSourceFiles) {
    $source = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($match in [regex]::Matches($source, 'class\s+(\w+Power)\s*:\s*(?:PowerModel|TemporaryStrengthPower)')) {
        [void]$classNames.Add($match.Groups[1].Value)
    }
}
if ($classNames.Count -eq 0) {
    throw 'No Ironeye power models were discovered; refusing an empty validation.'
}

$localeTables = @{}
foreach ($locale in @('eng', 'jpn', 'zhs')) {
    $localeTables[$locale] = Get-Content -LiteralPath (Join-Path $root "NightMustStay/localization/$locale/powers.json") -Raw -Encoding UTF8 | ConvertFrom-Json -AsHashtable
}

Add-Type -AssemblyName System.Drawing
$paths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$iconHashes = @{}
foreach ($className in $classNames | Sort-Object) {
    $snakeName = [regex]::Replace($className, '([A-Z]+)([A-Z][a-z])', '$1_$2')
    $snakeName = [regex]::Replace($snakeName, '([a-z0-9])([A-Z])', '$1_$2').ToLowerInvariant()
    $localizationId = $snakeName.ToUpperInvariant()
    $atlasPath = "res://images/atlases/power_atlas.sprites/$snakeName.tres"
    $atlasSourcePath = Join-Path $root $atlasPath.Substring(6)
    if (-not (Test-Path -LiteralPath $atlasSourcePath -PathType Leaf)) {
        throw "Ironeye power $className is missing its AtlasTexture: $atlasPath"
    }

    $atlasText = Get-Content -LiteralPath $atlasSourcePath -Raw
    $sourceMatch = [regex]::Match($atlasText, 'path="(res://[^"{}]+\.png)"')
    if (-not $sourceMatch.Success) {
        throw "Ironeye power $className has no PNG source in $atlasPath"
    }
    $iconPath = $sourceMatch.Groups[1].Value
    $iconSourcePath = Join-Path $root $iconPath.Substring(6)
    if (-not (Test-Path -LiteralPath $iconSourcePath -PathType Leaf)) {
        throw "Ironeye power $className is missing its PNG source: $iconPath"
    }

    $bitmap = [System.Drawing.Bitmap]::FromFile($iconSourcePath)
    try {
        if ($bitmap.Width -ne 256 -or $bitmap.Height -ne 256) {
            throw "Ironeye power icon must be 256x256: $iconPath"
        }
        foreach ($corner in @($bitmap.GetPixel(0, 0), $bitmap.GetPixel(255, 0), $bitmap.GetPixel(0, 255), $bitmap.GetPixel(255, 255))) {
            if ($corner.A -ne 0) {
                throw "Ironeye power icon must have transparent corners: $iconPath"
            }
        }
    }
    finally {
        $bitmap.Dispose()
    }

    $hash = (Get-FileHash -LiteralPath $iconSourcePath -Algorithm SHA256).Hash
    if ($iconHashes.ContainsKey($hash)) {
        throw "Ironeye powers share the same icon: $($iconHashes[$hash]) and $className"
    }
    $iconHashes[$hash] = $className

    foreach ($locale in $localeTables.Keys) {
        foreach ($suffix in @('.title', '.description')) {
            $key = "$localizationId$suffix"
            if (-not $localeTables[$locale].ContainsKey($key)) {
                throw "Ironeye power $className is missing localization key $locale/$key"
            }
        }
    }

    [void]$paths.Add($atlasPath)
    [void]$paths.Add($iconPath)
}

New-Item -ItemType Directory -Path (Split-Path -Parent $ManifestPath) -Force | Out-Null
@{ paths = @($paths | Sort-Object) } | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $ManifestPath -Encoding utf8
Write-Output "Ironeye power icon source validation passed: $($classNames.Count) powers, $($paths.Count) packaged resources."
