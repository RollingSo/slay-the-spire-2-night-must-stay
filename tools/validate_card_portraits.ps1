param([string]$ManifestPath = '')

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    $ManifestPath = Join-Path $root 'build/card_portrait_paths.json'
}
$paths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$characters = 'guardian|ironeye|revenant|duchess'
$portraitPattern = '(?:packed/card_portraits/(?:' + $characters + ')/|images/packed/card_portraits/(?:' + $characters + ')/|(?:' + $characters + ')_assets/cards/|images/atlases/card_atlas\.sprites/(?:' + $characters + ')/)'

# Check literal runtime overrides, not just whether an image with the same basename exists.
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $root 'src/Core/Models/Cards') -File -Filter '*.cs') {
    $source = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($match in [regex]::Matches($source, '"(res://' + $portraitPattern + '[^"{}]+\.(?:png|tres))"')) {
        [void]$paths.Add($match.Groups[1].Value)
    }
    foreach ($match in [regex]::Matches($source, 'ImageHelper\.GetImagePath\("((?:packed/card_portraits/|atlases/card_atlas\.sprites/)(?:' + $characters + ')/[^"{}]+\.(?:png|tres))"\)')) {
        [void]$paths.Add('res://images/' + $match.Groups[1].Value)
    }
}

# Include base-class atlas portraits and dynamic character paths, not only explicit overrides.
foreach ($relative in @('packed/card_portraits/guardian', 'guardian_assets/cards', 'ironeye_assets/cards', 'revenant_assets/cards',
    'images/packed/card_portraits/guardian', 'images/packed/card_portraits/ironeye', 'images/packed/card_portraits/revenant', 'images/packed/card_portraits/duchess',
    'images/atlases/card_atlas.sprites/guardian', 'images/atlases/card_atlas.sprites/ironeye', 'images/atlases/card_atlas.sprites/revenant', 'images/atlases/card_atlas.sprites/duchess')) {
    $directory = Join-Path $root $relative
    if (-not (Test-Path -LiteralPath $directory)) { continue }
    foreach ($file in Get-ChildItem -LiteralPath $directory -File -Recurse) {
        if ($file.Extension -notin @('.png', '.tres')) { continue }
        [void]$paths.Add('res://' + $file.FullName.Substring($root.TrimEnd('\').Length + 1).Replace('\', '/'))
        if ($file.Extension -eq '.tres') {
            foreach ($match in [regex]::Matches((Get-Content -LiteralPath $file.FullName -Raw), 'path="(res://[^"{}]+\.(?:png|tres))"')) {
                [void]$paths.Add($match.Groups[1].Value)
            }
        }
    }
}

$missing = @($paths | Where-Object { -not (Test-Path -LiteralPath (Join-Path $root $_.Substring(6)) -PathType Leaf) })
if ($missing.Count -gt 0) {
    throw ("Card portrait paths do not resolve to source assets:`n" + ($missing -join "`n"))
}
if ($paths.Count -eq 0) { throw 'No card portrait resources found; refusing empty validation.' }
New-Item -ItemType Directory -Path (Split-Path -Parent $ManifestPath) -Force | Out-Null
@{ paths = @($paths | Sort-Object) } | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $ManifestPath -Encoding utf8
Write-Output ("Card portrait source validation passed: {0} resources. Manifest: {1}" -f $paths.Count, $ManifestPath)
