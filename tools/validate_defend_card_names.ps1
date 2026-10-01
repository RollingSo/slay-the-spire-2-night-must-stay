$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$catalog = Get-Content (Join-Path $root 'design/card_name_localization.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$chinese = Get-Content (Join-Path $root 'NightMustStay/localization/zhs/cards.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$ids = @($chinese.PSObject.Properties | Where-Object { $_.Name.EndsWith('.title') -and ([string]$_.Value).Contains('防御') } | ForEach-Object { $_.Name.Replace('.title', '') } | Sort-Object)
if (Compare-Object $ids @($catalog.defendCardIds | Sort-Object)) { throw 'Canonical Defend membership differs from Chinese card names.' }
$filter = Get-Content (Join-Path $root 'src/Core/Models/GuardianCardFilters.cs') -Raw -Encoding UTF8
$sourceIds = @([regex]::Matches($filter, '"([A-Z_]+)"') | ForEach-Object { $_.Groups[1].Value } | Sort-Object)
if (Compare-Object $ids $sourceIds) { throw 'Runtime Defend membership differs from Chinese card names.' }
$patterns = @{ eng = 'Defend|Defense'; jpn = '防御'; kor = '수비' }
foreach ($locale in $patterns.Keys) {
    $cards = Get-Content (Join-Path $root "NightMustStay/localization/$locale/cards.json") -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($id in $ids) {
        $title = [string]$cards.PSObject.Properties["$id.title"].Value
        if ($title -notmatch $patterns[$locale]) { throw "$locale/$id loses the Defend-name marker: $title" }
    }
}
foreach ($locale in @('zhs', 'eng', 'jpn', 'kor')) {
    $cards = Get-Content (Join-Path $root "NightMustStay/localization/$locale/cards.json") -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($entry in $catalog.cards.PSObject.Properties) {
        if ($cards.PSObject.Properties["$($entry.Name).title"].Value -cne $entry.Value.$locale) { throw "Name override mismatch: $locale/$($entry.Name)" }
    }
}
Write-Output "Defend card-name validation passed ($($ids.Count) cards, four locales)."
