$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$zhsPath = Join-Path $root 'sts2mod\localization\zhs\cards.json'
$engPath = Join-Path $root 'sts2mod\localization\eng\cards.json'
$zhs = Get-Content -LiteralPath $zhsPath -Raw -Encoding UTF8 | ConvertFrom-Json
$eng = Get-Content -LiteralPath $engPath -Raw -Encoding UTF8 | ConvertFrom-Json

function ConvertFrom-CodePoints([int[]]$CodePoints) {
    return -join ($CodePoints | ForEach-Object { [char]$_ })
}

function Get-CardText($Table, [string]$Key) {
    return [string]$Table.PSObject.Properties[$Key].Value
}

$zhFullStop = ConvertFrom-CodePoints @(0x3002)
$zhCharge = ConvertFrom-CodePoints @(0x84C4, 0x529B)
$zhRecover = ConvertFrom-CodePoints @(0x56DE, 0x6536)
$zhEthereal = ConvertFrom-CodePoints @(0x865A, 0x65E0)
$zhExhaust = ConvertFrom-CodePoints @(0x6D88, 0x8017)
$zhRetain = ConvertFrom-CodePoints @(0x4FDD, 0x7559)
$zhApproachPlus = (ConvertFrom-CodePoints @(0x63A5, 0x8FD1)) + '+'
$zhRetreatPlus = (ConvertFrom-CodePoints @(0x8FDC, 0x79BB)) + '+'
$zhSentencePattern = [regex]::Escape($zhFullStop) + '(?!\r?\n|$)'
$zhChargePattern = '(?<!\[gold\])' + [regex]::Escape($zhCharge)
$zhRecoverPattern = '(?<!\[gold\])' + [regex]::Escape($zhRecover)

$revenantIds = @(
    'STRIKE_REVENANT', 'DEFEND_REVENANT', 'REVENANT_CALL', 'REVENANT_RESONANCE',
    'ICE_LIGHTNING_SPEAR', 'CURSED_CLAW_COMBO', 'HALO', 'EMERGENCY_RESTORE',
    'PRECISE_LIGHTNING_STRIKE', 'THREEFOLD_HALO', 'ANCIENT_DRAGON_LIGHTNING',
    'LANSSEAX_BLADE', 'LIGHTNING_STRIKE', 'ANCIENT_DRAGON_SPEAR', 'RECOVER',
    'FLANN_SAX_LIGHTNING_SPEAR', 'BEAST_CLAW', 'DEATH_LIGHTNING',
    'SPACE_RENDING_FRENZY', 'WHITE_SHADOW_LURE', 'SOULGUARD', 'LIGHTNING_SPEAR',
    'SPIRIT_FORM', 'UNBEARABLE_FRENZY', 'BEASTSTONE', 'RADAGON_HALO',
    'SOUL_SUMMON', 'GRAVE_ROB', 'GREATER_RECOVER', 'ANCIENT_DRAGON_FAITH',
    'BEAST_CLAW_MARK', 'GOLDEN_ORDER', 'SPIRIT_LINK', 'BLESSING_OF_GRACE',
    'GURRANQ_BEAST_CLAW'
)

$errors = [System.Collections.Generic.List[string]]::new()

# Keep the localization tables aligned with the ModelId entries generated from
# every card class explicitly registered in RevenantCardPool. A missing title
# can abort the card-library sorter and leave the entire Revenant page empty.
$revenantPoolPath = Join-Path $root 'src\Core\Models\CardPools\RevenantCardPool.cs'
$revenantPoolSource = Get-Content -LiteralPath $revenantPoolPath -Raw -Encoding UTF8
$registeredCardClassNames = [regex]::Matches(
    $revenantPoolSource,
    'ModelDb\.Card<([A-Za-z0-9_]+)>'
) | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique

foreach ($className in $registeredCardClassNames) {
    $cardId = (($className -creplace '([a-z0-9])([A-Z])', '$1_$2') `
        -creplace '([A-Z]+)([A-Z][a-z])', '$1_$2').ToUpperInvariant()
    foreach ($suffix in @('title', 'description', 'upgradeDescription')) {
        $key = "$cardId.$suffix"
        if (-not $zhs.PSObject.Properties[$key]) {
            $errors.Add("${key}: missing from Chinese card localization.")
        }
        if (-not $eng.PSObject.Properties[$key]) {
            $errors.Add("${key}: missing from English card localization.")
        }
    }
}

foreach ($id in $revenantIds) {
    foreach ($suffix in @('description', 'upgradeDescription')) {
        $key = "$id.$suffix"
        $zhsText = Get-CardText $zhs $key
        $engText = Get-CardText $eng $key
        if ($zhsText -and $zhsText -match $zhSentencePattern) {
            $errors.Add("${key}: Chinese full stop must be followed by a newline.")
        }
        if ($engText -and $engText -match '\.(?!\r?\n|$)') {
            $errors.Add("${key}: English sentence period must be followed by a newline.")
        }
        if ($zhsText -match $zhChargePattern) {
            $errors.Add("${key}: Charge must be highlighted with [gold] in Chinese.")
        }
        if ($engText -match '(?<!\[gold\])Charge(?![A-Za-z])') {
            $errors.Add("${key}: Charge must be highlighted with [gold] in English.")
        }
        if ($zhsText -match $zhRecoverPattern) {
            $errors.Add("${key}: Recover must be highlighted with [gold] in Chinese.")
        }
        if ($engText -match '(?<!\[gold\])Recover(?:ed)?') {
            $errors.Add("${key}: Recover must be highlighted with [gold] in English.")
        }
    }
}

$canonicalKeywordCards = @{
    'HALO' = @($zhEthereal, 'Ethereal')
    'THREEFOLD_HALO' = @($zhEthereal, 'Ethereal')
    'RADAGON_HALO' = @($zhEthereal, 'Ethereal')
    'SOUL_SUMMON' = @($zhExhaust, 'Exhaust')
    'SPIRIT_FORM' = @($zhRetain, 'Retain')
    'REVENANT_RESONANCE' = @($zhRetain, 'Retain')
}
foreach ($id in $canonicalKeywordCards.Keys) {
    foreach ($suffix in @('description', 'upgradeDescription')) {
        $key = "$id.$suffix"
        $zhsKeywordPattern = '\[gold\]' + [regex]::Escape($canonicalKeywordCards[$id][0]) + '\[/gold\]'
        $engKeywordPattern = '\[gold\]' + [regex]::Escape($canonicalKeywordCards[$id][1]) + '\[/gold\]'
        if ((Get-CardText $zhs $key) -match $zhsKeywordPattern) {
            $errors.Add("${key}: duplicates an automatically rendered canonical keyword in Chinese.")
        }
        if ((Get-CardText $eng $key) -match $engKeywordPattern) {
            $errors.Add("${key}: duplicates an automatically rendered canonical keyword in English.")
        }
    }
}

$ghostStepChecks = @(
    @($zhs, $zhApproachPlus, $zhRetreatPlus, $zhRetain, 'Chinese'),
    @($eng, 'Approach+', 'Retreat+', 'Retain', 'English')
)
foreach ($check in $ghostStepChecks) {
    $table = $check[0]
    foreach ($key in @('HEAVENLY_EYE_FORM.description', 'HEAVENLY_EYE_FORM.upgradeDescription')) {
        $cardText = Get-CardText $table $key
        if (-not $cardText.Contains($check[1]) -or -not $cardText.Contains($check[2])) {
            $errors.Add("${key}: Ghost Step must name the upgraded Approach+ and Retreat+ cards in $($check[4]).")
        }
        $retainPattern = '\[gold\]' + [regex]::Escape($check[3]) + '\[/gold\]'
        if ($cardText -match $retainPattern) {
            $errors.Add("${key}: duplicates the automatically rendered Retain keyword in $($check[4]).")
        }
    }
}

$nowhereChecks = @(
    @($zhs, $zhRetain, $zhSentencePattern, 'Chinese'),
    @($eng, 'Retain', '\.(?!\r?\n|$)', 'English')
)
foreach ($check in $nowhereChecks) {
    $table = $check[0]
    foreach ($key in @('NOWHERE_TO_HIDE.description', 'NOWHERE_TO_HIDE.upgradeDescription')) {
        $cardText = Get-CardText $table $key
        $retainPattern = '\[gold\]' + [regex]::Escape($check[1]) + '\[/gold\]'
        if ($cardText -match $retainPattern) {
            $errors.Add("${key}: duplicates the automatically rendered Retain keyword in $($check[3]).")
        }
        if ($cardText -match $check[2]) {
            $errors.Add("${key}: each complete sentence must start on a new line in $($check[3]).")
        }
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Output 'Card text format validation passed.'
