$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$localization = Join-Path $root 'NightMustStay\localization'
$cards = Get-Content (Join-Path $localization 'kor\cards.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$chinese = Get-Content (Join-Path $localization 'zhs\cards.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$failures = [System.Collections.Generic.List[string]]::new()
$defendCount = 0
$filterSource = Get-Content (Join-Path $root 'src\Core\Models\GuardianCardFilters.cs') -Raw -Encoding UTF8
if (-not $filterSource.Contains('title.Contains("수비", StringComparison.OrdinalIgnoreCase)')) {
    $failures.Add('GuardianCardFilters must recognize Korean Defend names.')
}

foreach ($entry in $cards.PSObject.Properties) {
    $key = $entry.Name
    $text = [string]$entry.Value
    if ($key.EndsWith('.title')) {
        # Preserve the Chinese design's Defend membership; translating a
        # non-Defend title as "수비" would silently change its interactions.
        $isDefend = ([string]$chinese.PSObject.Properties[$key].Value).Contains('防御')
        if ($isDefend -ne $text.Contains('수비')) {
            $failures.Add("$key changes Defend-name membership.")
        }
        if ($isDefend) { $defendCount++ }
    }
    if ($key.EndsWith('.upgradeDescription') -and
        ($text.Contains([string][char]0x2192) -or $text -match '^\[gold\]보존\[/gold\] 추가$')) {
        $failures.Add("$key must describe complete upgraded rules.")
    }
    if ($key -match '\.(?:description|upgradeDescription|unchargedDescription|chargedDescription)$' -and
        $text -match '\. (?=[가-힣\[])') {
        $failures.Add("$key must put independent sentences on separate lines.")
    }
}

$canonicalKeywordCards = @{
    HALO = '휘발성'
    THREEFOLD_HALO = '휘발성'
    RADAGON_HALO = '휘발성'
    SOUL_SUMMON = '소멸'
    SPIRIT_FORM = '보존'
    FORMATION_BREAKER_HAMMER = '보존'
    ALL_SOULS_RETURN = '보존'
    TRAVELING_SATCHEL = '보존'
    STALWART_SHIELD = '선천성'
    REVERSAL_STEP = '보존'
}
foreach ($id in $canonicalKeywordCards.Keys) {
    foreach ($suffix in @('description', 'upgradeDescription')) {
        $text = [string]$cards.PSObject.Properties["$id.$suffix"].Value
        if ($text.Contains('[gold]' + $canonicalKeywordCards[$id] + '[/gold]')) {
            $failures.Add("$id.$suffix duplicates an engine-rendered keyword.")
        }
    }
}

foreach ($file in Get-ChildItem (Join-Path $localization 'kor') -Filter '*.json') {
    $table = Get-Content $file.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($entry in $table.PSObject.Properties) {
        if ([string]::IsNullOrWhiteSpace([string]$entry.Value)) {
            $failures.Add("$($file.Name)/$($entry.Name) is empty.")
        }
        if ([string]$entry.Value -match '[\u3400-\u9fff\u3040-\u30ff]') {
            $failures.Add("$($file.Name)/$($entry.Name) contains Chinese or Japanese text.")
        }
    }
}
if ($failures.Count) {
    $failures | ForEach-Object { Write-Host "ERROR: $_" -ForegroundColor Red }
    exit 1
}
Write-Host "Korean localization validation passed ($defendCount Defend-name cards)."
