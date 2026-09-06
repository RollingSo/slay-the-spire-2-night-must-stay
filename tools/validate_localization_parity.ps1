$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$localizationRoot = Join-Path $root 'NightMustStay\localization'
$referenceLocale = 'eng'
$locales = @('zhs', 'jpn')
$files = @('ancients.json', 'card_library.json', 'cards.json', 'characters.json', 'events.json', 'potions.json', 'powers.json', 'relics.json')
$errors = [System.Collections.Generic.List[string]]::new()

function Read-Table([string]$Locale, [string]$FileName) {
    $path = Join-Path $localizationRoot "$Locale\$FileName"
    if (-not (Test-Path -LiteralPath $path)) {
        $errors.Add("Missing localization file: $Locale/$FileName")
        return $null
    }
    try {
        return Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        $errors.Add("Invalid JSON in $Locale/${FileName}: $($_.Exception.Message)")
        return $null
    }
}

function Get-TokenNames([string]$Text) {
    return @([regex]::Matches($Text, '\{([A-Za-z][A-Za-z0-9_]*)') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
}

foreach ($file in $files) {
    $reference = Read-Table $referenceLocale $file
    $chinese = Read-Table 'zhs' $file
    if ($null -eq $reference) { continue }
    $referenceKeys = @($reference.PSObject.Properties | ForEach-Object { $_.Name } | Where-Object { $_ })

    foreach ($locale in $locales) {
        $table = Read-Table $locale $file
        if ($null -eq $table) { continue }
        $keys = @($table.PSObject.Properties | ForEach-Object { $_.Name } | Where-Object { $_ })
        foreach ($missing in @($referenceKeys | Where-Object { $_ -notin $keys })) {
            $errors.Add("$locale/${file}: missing key $missing")
        }
        foreach ($extra in @($keys | Where-Object { $_ -notin $referenceKeys })) {
            $errors.Add("$locale/${file}: unexpected key $extra")
        }

        # English contains plural selectors that Chinese does not, while a few
        # newer Chinese previews contain dynamic selectors not yet present in
        # English. Japanese must preserve the union of their runtime token names.
        if ($locale -ne 'jpn') { continue }
        foreach ($key in @($referenceKeys | Where-Object { $_ -in $keys })) {
            $referenceText = [string]$reference.PSObject.Properties[$key].Value
            $chineseText = [string]$chinese.PSObject.Properties[$key].Value
            $localizedText = [string]$table.PSObject.Properties[$key].Value
            $referenceTokens = @(@(Get-TokenNames $referenceText) + @(Get-TokenNames $chineseText) | Sort-Object -Unique) -join "`n"
            $localizedTokens = @(Get-TokenNames $localizedText) -join "`n"
            if ($referenceTokens -cne $localizedTokens) {
                $errors.Add("$locale/${file}: placeholder mismatch at $key")
            }
            foreach ($tag in @('gold', 'purple', 'blue', 'sine', 'thinky_dots', 'b', 'i')) {
                $openCount = [regex]::Matches($localizedText, "\[$tag(?:=[^\]]+)?\]").Count
                $closeCount = [regex]::Matches($localizedText, "\[/$tag\]").Count
                if ($openCount -ne $closeCount) {
                    $errors.Add("$locale/${file}: unbalanced [$tag] tags at $key")
                }
            }
        }
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Host "ERROR: $_" -ForegroundColor Red }
    exit 1
}

Write-Host 'Localization parity validation passed.'
