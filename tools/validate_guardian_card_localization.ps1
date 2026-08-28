param()

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$localizationRoot = Join-Path $root 'NightMustStay\localization'
$failures = [System.Collections.Generic.List[string]]::new()

function ConvertFrom-CodePoints([int[]]$CodePoints) {
    return -join ($CodePoints | ForEach-Object { [char]$_ })
}

$zhsTerms = @(
    (ConvertFrom-CodePoints @(0x5408, 0x6210)),
    (ConvertFrom-CodePoints @(0x56FA, 0x5B88)),
    (ConvertFrom-CodePoints @(0x9632, 0x5FA1, 0x53CD, 0x51FB)),
    (ConvertFrom-CodePoints @(0x76FE, 0x6233)),
    (ConvertFrom-CodePoints @(0x5931, 0x8861)),
    (ConvertFrom-CodePoints @(0x85CF, 0x950B)),
    (ConvertFrom-CodePoints @(0x865A, 0x5F31)),
    (ConvertFrom-CodePoints @(0x683C, 0x6321)),
    (ConvertFrom-CodePoints @(0x624B, 0x724C)),
    (ConvertFrom-CodePoints @(0x5F03, 0x724C, 0x5806)),
    (ConvertFrom-CodePoints @(0x62BD, 0x724C, 0x5806)),
    (ConvertFrom-CodePoints @(0x4FDD, 0x7559)),
    (ConvertFrom-CodePoints @(0x6D88, 0x8017)),
    (ConvertFrom-CodePoints @(0x529B, 0x91CF)),
    (ConvertFrom-CodePoints @(0x654F, 0x6377)),
    (ConvertFrom-CodePoints @(0x7729, 0x6655))
)
$turnSucceeded = ConvertFrom-CodePoints @(0x56DE, 0x5408, 0x6210, 0x529F)

$rules = @(
    @{
        Locale = 'zhs'
        Terms = $zhsTerms
        WordBoundaries = $false
    },
    @{
        Locale = 'eng'
        Terms = @('Synthesize', 'Synthesis', 'Concealed Edge', 'Fortify', 'Guard Counter', 'Shield Poke', 'Imbalance', 'Weak', 'Block', 'Blocking', 'hand', 'discard pile', 'draw pile', 'Retain', 'Retained', 'Exhaust', 'Exhausted', 'Strength', 'Dexterity', 'Stun')
        WordBoundaries = $true
    }
)

foreach ($rule in $rules) {
    $path = Join-Path $localizationRoot "$($rule.Locale)\cards.json"
    $table = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json

    foreach ($entry in $table.PSObject.Properties) {
        if ($entry.Name -notmatch '\.(description|upgradeDescription)$') {
            continue
        }

        $raw = [string]$entry.Value
        $openTags = ([regex]::Matches($raw, '\[gold\]')).Count
        $closeTags = ([regex]::Matches($raw, '\[/gold\]')).Count
        if ($openTags -ne $closeTags) {
            $failures.Add("$($rule.Locale)/$($entry.Name): unbalanced gold tags")
        }

        if ($raw.Contains('{GeneratedCard}') -and $raw -notmatch '\[gold\]\{GeneratedCard\}\[/gold\]') {
            $failures.Add("$($rule.Locale)/$($entry.Name): GeneratedCard is not highlighted")
        }

        $plain = [regex]::Replace($raw, '\[gold\].*?\[/gold\]', '', [System.Text.RegularExpressions.RegexOptions]::Singleline)
        $plain = [regex]::Replace($plain, '\{[A-Za-z][A-Za-z0-9]*(?::[^{}]*)?\}', '')
        if ($rule.Locale -eq 'zhs') {
            # Avoid reading adjacent characters in "turn succeeded" as "synthesize".
            $plain = $plain.Replace($turnSucceeded, '')
        }

        foreach ($term in $rule.Terms) {
            $found = if ($rule.WordBoundaries) {
                $plain -match "(?i)(?<![A-Za-z])$([regex]::Escape($term))(?![A-Za-z])"
            }
            else {
                $plain.Contains($term)
            }

            if ($found) {
                $failures.Add("$($rule.Locale)/$($entry.Name): '$term' is not highlighted")
            }
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    throw "Guardian card localization validation failed with $($failures.Count) issue(s)."
}

Write-Output 'Guardian card localization highlighting validated.'
