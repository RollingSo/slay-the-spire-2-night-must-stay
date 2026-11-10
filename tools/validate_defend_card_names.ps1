$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$catalog = Get-Content (Join-Path $root 'design/card_name_localization.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$chinese = Get-Content (Join-Path $root 'NightMustStay/localization/zhs/cards.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$defendNameMarker = ([char]0x9632) + ([char]0x5FA1)
$ids = @($chinese.PSObject.Properties | Where-Object { $_.Name.EndsWith('.title') -and ([string]$_.Value).Contains($defendNameMarker) } | ForEach-Object { [string]$_.Name.Replace('.title', '') } | Sort-Object)
# Windows PowerShell 5.1 can preserve ConvertFrom-Json values as wrapped
# PSObjects. Normalize both sides to plain strings before comparing them.
$catalogDefendIds = @($catalog.defendCardIds | ForEach-Object { [string]$_ } | Sort-Object)
$canonicalDifference = @(Compare-Object -ReferenceObject $ids -DifferenceObject $catalogDefendIds)
if ($canonicalDifference.Count -gt 0) {
    $details = ($canonicalDifference | ForEach-Object { "$($_.SideIndicator)$($_.InputObject)" }) -join ', '
    throw "Canonical Defend membership differs from Chinese card names: $details"
}
$filter = Get-Content (Join-Path $root 'src/Core/Models/GuardianCardFilters.cs') -Raw -Encoding UTF8
$sourceIds = @([regex]::Matches($filter, '"([A-Z_]+)"') | ForEach-Object { $_.Groups[1].Value } | Sort-Object)
$runtimeDifference = @(Compare-Object -ReferenceObject $ids -DifferenceObject $sourceIds)
if ($runtimeDifference.Count -gt 0) {
    $details = ($runtimeDifference | ForEach-Object { "$($_.SideIndicator)$($_.InputObject)" }) -join ', '
    throw "Runtime Defend membership differs from Chinese card names: $details"
}
$patterns = @{ eng = 'Defend|Defense'; jpn = $defendNameMarker; kor = ([char]0xC218) + ([char]0xBE44) }
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
