param(
    [string]$Path = (Join-Path $PSScriptRoot '../design/wylder/PLANNER_ROSTER_20261022.csv')
)

$ErrorActionPreference = 'Stop'
$cards = @(Import-Csv -LiteralPath $Path -Encoding UTF8)
if ($cards.Count -ne 76) { throw "Expected 76 planned additions, found $($cards.Count)." }

foreach ($field in @('PlanId', 'Name', 'Rarity', 'Type', 'Cost', 'Route', 'Batch', 'Status')) {
    if ($cards | Where-Object { [string]::IsNullOrWhiteSpace($_.$field) }) {
        throw "Missing required planner field: $field."
    }
}
foreach ($field in @('PlanId', 'Name')) {
    $duplicates = @($cards | Group-Object -Property $field | Where-Object Count -gt 1)
    if ($duplicates.Count) { throw "Duplicate $field`: $($duplicates.Name -join ', ')." }
}
for ($i = 1; $i -le 76; $i++) {
    $id = 'P{0:D3}' -f $i
    if (@($cards | Where-Object PlanId -eq $id).Count -ne 1) { throw "Missing plan ID $id." }
}

$rarities = @{ Common = 15; Uncommon = 33; Rare = 26; Ancient = 2 }
$types = @{ Attack = 21; Skill = 38; Power = 17 }
$batches = @{ B1 = 12; B2 = 12; B3 = 13; B4 = 13; B5 = 13; B6 = 13 }
foreach ($definition in @(
    @{ Field = 'Rarity'; Counts = $rarities },
    @{ Field = 'Type'; Counts = $types },
    @{ Field = 'Batch'; Counts = $batches }
)) {
    $field = $definition.Field
    $counts = $definition.Counts
    foreach ($group in ($cards | Group-Object -Property $field)) {
        if (-not $counts.ContainsKey($group.Name) -or $counts[$group.Name] -ne $group.Count) {
            throw "Incorrect $field group $($group.Name): $($group.Count)."
        }
    }
    foreach ($key in $counts.Keys) {
        if (@($cards | Where-Object { $_.$field -eq $key }).Count -ne $counts[$key]) {
            throw "Incorrect or absent $field group: $key."
        }
    }
}
foreach ($card in $cards) {
    $cost = 0
    if (-not [int]::TryParse($card.Cost, [ref]$cost) -or $cost -lt 0 -or $cost -gt 3) {
        throw "Unexpected cost for $($card.PlanId): $($card.Cost). Update the reviewed schema if intentional."
    }
    if ($card.Status -notin @('proposal', 'detailed', 'reviewed', 'implemented')) {
        throw "Unexpected status for $($card.PlanId): $($card.Status)."
    }
}

$reviewPath = Join-Path (Split-Path -Parent $Path) 'PLANNER_REVIEWED_CATALOG_20261022.md'
if (Test-Path -LiteralPath $reviewPath) {
    $reviewRows = @(Get-Content -LiteralPath $reviewPath -Encoding UTF8 | Where-Object { $_ -match '^\| P\d{3} \|' })
    if ($reviewRows.Count -ne 76) { throw "Reviewed catalog must contain 76 full rows; found $($reviewRows.Count)." }
    $reviewIds = @()
    $rarityNames = @{ Common = '普通'; Uncommon = '罕见'; Rare = '稀有'; Ancient = '先古' }
    $typeNames = @{ Attack = '攻击'; Skill = '技能'; Power = '能力' }
    foreach ($line in $reviewRows) {
        $cells = @($line.Trim('|').Split('|') | ForEach-Object { $_.Trim() })
        if ($cells.Count -ne 8 -or ($cells | Where-Object { [string]::IsNullOrWhiteSpace($_) })) {
            throw "Incomplete reviewed rule/upgrade/art row: $line"
        }
        $reviewIds += $cells[0]
        $planned = @($cards | Where-Object PlanId -eq $cells[0])
        if ($planned.Count -ne 1 -or $planned[0].Name -ne $cells[1] -or $rarityNames[$planned[0].Rarity] -ne $cells[2] -or
            $typeNames[$planned[0].Type] -ne $cells[3] -or $planned[0].Cost -ne $cells[4]) {
            throw "Reviewed catalog metadata differs from roster: $($cells[0])."
        }
    }
    if (@($reviewIds | Select-Object -Unique).Count -ne 76) { throw 'Duplicate reviewed plan IDs.' }
    Write-Output 'PASS: 76 full reviewed base/upgrade/art-event rows match the local planner roster.'
}
Write-Output 'PASS: 76 unique planner additions: 15 Common / 33 Uncommon / 26 Rare / 2 Ancient; six batches 12/12/13/13/13/13.'
Write-Output 'Planning validation only. This does NOT assert runtime registration, artwork approval, installation, or gameplay balance.'
