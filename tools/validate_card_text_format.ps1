$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$zhsPath = Join-Path $root 'NightMustStay\localization\zhs\cards.json'
$engPath = Join-Path $root 'NightMustStay\localization\eng\cards.json'
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
$zhColon = ConvertFrom-CodePoints @(0xFF1A)
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

# Starter Lyre has repeatedly regressed by changing only its upgrade behavior
# or only its displayed text. Keep the implementation and both localization
# tables locked together: the upgrade adds Recover 1 and never reduces cost.
$revenantStarterPath = Join-Path $root 'src\Core\Models\Cards\RevenantStarterCards.cs'
$revenantStarterSource = Get-Content -LiteralPath $revenantStarterPath -Raw -Encoding UTF8
$zhCall = ConvertFrom-CodePoints @(0x547C, 0x5524)
$zhCardCounter = ConvertFrom-CodePoints @(0x5F20, 0x724C)
$zhLyreBase = '[gold]' + $zhCall + '[/gold]' + $zhFullStop
$zhLyreUpgrade = $zhLyreBase + "`n[gold]" + $zhRecover + '[/gold]1' + $zhCardCounter + $zhFullStop
$lyreChecks = @(
    @($zhs, 'REVENANT_CALL.description', $zhLyreBase, 'Chinese base text'),
    @($zhs, 'REVENANT_CALL.upgradeDescription', $zhLyreUpgrade, 'Chinese upgraded text'),
    @($eng, 'REVENANT_CALL.description', '[gold]Call[/gold].', 'English base text'),
    @($eng, 'REVENANT_CALL.upgradeDescription', "[gold]Call[/gold].`n[gold]Recover[/gold] 1 card.", 'English upgraded text')
)
foreach ($check in $lyreChecks) {
    $actual = (Get-CardText $check[0] $check[1]) -replace "`r`n", "`n"
    if ($actual -cne $check[2]) {
        $errors.Add("$($check[1]): $($check[3]) must stay synchronized with the Recover 1 upgrade.")
    }
}
$lyreClassMatch = [regex]::Match(
    $revenantStarterSource,
    'public sealed class RevenantCall\s*:[\s\S]*?(?=public sealed class RevenantResonance)'
)
$lyreClassSource = if ($lyreClassMatch.Success) { $lyreClassMatch.Value } else { '' }
if (-not $lyreClassMatch.Success) {
    $errors.Add('REVENANT_CALL: implementation class could not be found.')
}
if ($lyreClassSource -notmatch 'if\s*\(IsUpgraded\)[\s\S]*?AddFromDiscard\(this,\s*context,\s*1,\s*false\)') {
    $errors.Add('REVENANT_CALL: upgraded implementation must Recover exactly 1 card.')
}
if ($lyreClassSource -match 'OnUpgrade\s*\(\)[\s\S]*?EnergyCost') {
    $errors.Add('REVENANT_CALL: upgrade must not reduce Energy cost.')
}
$lyreDescriptionPatchPath = Join-Path $root 'src\Core\Patches\RevenantCallDescriptionPatch.cs'
$lyreDescriptionPatchSource = if (Test-Path -LiteralPath $lyreDescriptionPatchPath) {
    Get-Content -LiteralPath $lyreDescriptionPatchPath -Raw -Encoding UTF8
} else {
    ''
}
if ($lyreDescriptionPatchSource -notmatch 'HarmonyPatch\(typeof\(CardModel\),\s*nameof\(CardModel\.Description\),\s*MethodType\.Getter\)' -or
    $lyreDescriptionPatchSource -notmatch 'RevenantCall\s*\{\s*IsUpgraded:\s*true\s*\}' -or
    $lyreDescriptionPatchSource -notmatch 'REVENANT_CALL\.upgradeDescription') {
    $errors.Add('REVENANT_CALL: upgraded cards must explicitly select the Recover 1 upgrade description at runtime.')
}
if ($lyreDescriptionPatchSource -notmatch 'RevenantResonance\s*\{\s*IsUpgraded:\s*true\s*\}' -or
    $lyreDescriptionPatchSource -notmatch 'REVENANT_RESONANCE\.upgradeDescription') {
    $errors.Add('REVENANT_RESONANCE: upgraded cards must explicitly select the draw-pile discard upgrade description at runtime.')
}

# Concerto has no numeric or keyword delta on upgrade: the upgrade is entirely
# implemented by an IsUpgraded branch. Without an explicit Description getter
# override, the game keeps rendering the base three-line text even though the
# upgraded card recovers a card when played.
$revenantTextTablePath = Join-Path $root 'src\Core\Models\Cards\RevenantTextTableCards.cs'
$revenantTextTableSource = Get-Content -LiteralPath $revenantTextTablePath -Raw -Encoding UTF8
$concertoClassMatch = [regex]::Match(
    $revenantTextTableSource,
    'public sealed class Concerto\s*:[\s\S]*?(?=public sealed class FightForMe)'
)
$concertoClassSource = if ($concertoClassMatch.Success) { $concertoClassMatch.Value } else { '' }
$zhResonance = ConvertFrom-CodePoints @(0x5171, 0x9E23)
$zhConcertoBase = '[gold]' + $zhCall + '[/gold]' + $zhFullStop + "`n[gold]" + $zhResonance + '[/gold]' + $zhFullStop + "`n[gold]" + $zhCall + '[/gold]' + $zhFullStop
$zhConcertoUpgrade = $zhConcertoBase + "`n[gold]" + $zhRecover + '[/gold]1' + $zhCardCounter + $zhFullStop
$concertoChecks = @(
    @($zhs, 'CONCERTO.description', $zhConcertoBase, 'Chinese base text'),
    @($zhs, 'CONCERTO.upgradeDescription', $zhConcertoUpgrade, 'Chinese upgraded text'),
    @($eng, 'CONCERTO.description', "[gold]Call[/gold].`n[gold]Resonance[/gold].`n[gold]Call[/gold].", 'English base text'),
    @($eng, 'CONCERTO.upgradeDescription', "[gold]Call[/gold].`n[gold]Resonance[/gold].`n[gold]Call[/gold].`n[gold]Recover[/gold] 1 card.", 'English upgraded text')
)
foreach ($check in $concertoChecks) {
    $actual = (Get-CardText $check[0] $check[1]) -replace "`r`n", "`n"
    if ($actual -cne $check[2]) {
        $errors.Add("$($check[1]): $($check[3]) must stay synchronized with the Recover 1 upgrade.")
    }
}
if (-not $concertoClassMatch.Success) {
    $errors.Add('CONCERTO: implementation class could not be found.')
}
if ($concertoClassSource -notmatch 'if\s*\(IsUpgraded\)[\s\S]*?AddFromDiscard\(this,\s*context,\s*1,\s*false\)') {
    $errors.Add('CONCERTO: upgraded implementation must Recover exactly 1 card.')
}
if ($concertoClassSource -match 'OnUpgrade\s*\(\)[\s\S]*?EnergyCost') {
    $errors.Add('CONCERTO: upgrade must not reduce Energy cost.')
}
if ($lyreDescriptionPatchSource -notmatch 'Concerto\s*\{\s*IsUpgraded:\s*true\s*\}' -or
    $lyreDescriptionPatchSource -notmatch 'CONCERTO\.upgradeDescription') {
    $errors.Add('CONCERTO: upgraded cards must explicitly select the Recover 1 upgrade description at runtime.')
}

# Gurranq's Beast Claw only gains Resonance after Charge is complete. Directly
# playing the uncharged card deals its base AoE damage without Resonance.
$revenantAdvancedPath = Join-Path $root 'src\Core\Models\Cards\RevenantAdvancedCards.cs'
$revenantAdvancedSource = Get-Content -LiteralPath $revenantAdvancedPath -Raw -Encoding UTF8
$gurranqClassMatch = [regex]::Match(
    $revenantAdvancedSource,
    'public sealed class GurranqBeastClaw\s*:[\s\S]*\z'
)
$gurranqClassSource = if ($gurranqClassMatch.Success) { $gurranqClassMatch.Value } else { '' }
if (-not $gurranqClassMatch.Success) {
    $errors.Add('GURRANQ_BEAST_CLAW: implementation class could not be found.')
}
if ($gurranqClassSource -notmatch 'new DamageVar\(13m' -or
    $gurranqClassSource -notmatch 'new DynamicVar\("ChargeDamage",\s*10m\)') {
    $errors.Add('GURRANQ_BEAST_CLAW: base AoE damage must be 13 and Charge bonus must be 10.')
}
$gurranqSelfBranch = [regex]::Match(
    $gurranqClassSource,
    'if\s*\(cardPlay\.Target\s*==\s*Owner\.Creature\)\s*\{[\s\S]*?\}'
).Value
if ($gurranqSelfBranch -match 'TriggerResonance|ChargeResonance') {
    $errors.Add('GURRANQ_BEAST_CLAW: charging the card must not trigger Resonance immediately.')
}
if ($gurranqClassSource -notmatch 'if\s*\(wasCharged\)\s*\{[\s\S]*?TriggerResonance\(context\)') {
    $errors.Add('GURRANQ_BEAST_CLAW: Resonance must trigger only when the charged card is played.')
}
$gurranqZhText = Get-CardText $zhs 'GURRANQ_BEAST_CLAW.unchargedDescription'
$gurranqEnText = Get-CardText $eng 'GURRANQ_BEAST_CLAW.unchargedDescription'
if ($gurranqZhText -notmatch ('\[gold\]' + [regex]::Escape($zhCharge) + '\[/gold\]' + [regex]::Escape($zhColon) + '\[gold\]' + [regex]::Escape($zhResonance) + '\[/gold\]')) {
    $errors.Add('GURRANQ_BEAST_CLAW: Chinese text must place Resonance inside the Charge effect.')
}
if ($gurranqEnText -notmatch '\[gold\]Charge\[/gold\]: \[gold\]Resonance\[/gold\]') {
    $errors.Add('GURRANQ_BEAST_CLAW: English text must place Resonance inside the Charge effect.')
}

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
