param([string]$RepoRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$titles = Get-Content -Raw -LiteralPath (Join-Path $RepoRoot 'NightMustStay/localization/zhs/cards.json') | ConvertFrom-Json
$entries = Get-Content -Raw -LiteralPath (Join-Path $RepoRoot 'design/卡图预览/三角色形态_20261001/角色卡图索引.json') | ConvertFrom-Json
foreach ($entry in $entries) {
    if ($titles.($entry.localizationKey) -ne $entry.title) { throw "Title drift: $($entry.localizationKey)" }
    $definition = Get-Content -Raw -LiteralPath (Join-Path $RepoRoot $entry.definition)
    $pattern = '(?s)public sealed class ' + [regex]::Escape($entry.model) + '\b.*?(?=public sealed class |\z)'
    $modelBlock = [regex]::Match($definition, $pattern).Value
    $runtimePath = $entry.portrait -replace '^images/', ''
    if (-not $modelBlock.Contains($runtimePath)) { throw "Portrait drift: $($entry.model) -> $runtimePath" }
    if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot $entry.portrait))) { throw "Missing portrait: $($entry.portrait)" }
    Write-Output "$($entry.title): $($entry.model) -> $($entry.portrait) OK"
}
