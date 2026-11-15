$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$source = Get-Content (Join-Path $root 'design/wylder/localization_jpn_kor.json') -Raw -Encoding UTF8 | ConvertFrom-Json -AsHashtable

function Merge-Entries([string]$path, [hashtable]$entries) {
    $text = [IO.File]::ReadAllText($path)
    foreach ($item in $entries.GetEnumerator()) {
        $key = [regex]::Escape([string]$item.Key)
        $encoded = $item.Value | ConvertTo-Json -Compress
        $pattern = '(?m)^(\s*"' + $key + '"\s*:\s*)("(?:\\.|[^"\\])*")'
        if ([regex]::IsMatch($text, $pattern)) {
            $text = [regex]::Replace($text, $pattern, { param($m) $m.Groups[1].Value + $encoded })
        } else {
            $trimmed = $text.TrimEnd()
            $text = $trimmed.Substring(0, $trimmed.Length - 1).TrimEnd() + ",`n  " + (($item.Key | ConvertTo-Json -Compress)) + ': ' + $encoded + "`n}`n"
        }
    }
    [IO.File]::WriteAllText($path, $text, [Text.UTF8Encoding]::new($false))
}

foreach ($locale in @('jpn','kor')) {
    foreach ($table in @('cards','powers','relics','potions','characters','card_library')) {
        $entries = $source[$locale][$table]
        if ($table -eq 'cards') {
            $expanded = [ordered]@{}
            foreach ($item in $entries.GetEnumerator()) {
                $expanded[$item.Key] = $item.Value
                if ($item.Key.EndsWith('.description') -and $item.Key -notmatch '^WYLDER_(PURSUIT|BREAK|HAND_CONDITION)\.description$') {
                    $expanded[$item.Key.Replace('.description','.upgradeDescription')] = $item.Value
                }
            }
            $entries = $expanded
        }
        Merge-Entries (Join-Path $root "NightMustStay/localization/$locale/$table.json") $entries
        Write-Host "${locale}/${table}: $($entries.Count) entries"
    }
}

$dialogues = Get-Content (Join-Path $root 'design/wylder/ancient_dialogues.json') -Raw -Encoding UTF8 | ConvertFrom-Json -AsHashtable
$standard = @(@('ancient','char'), @('ancient','char'), @('ancient','char','ancient'))
$architect = @(@('ancient','char'), @('ancient','ancient','char'), @('ancient','char','ancient'), @('ancient','char'))
foreach ($locale in @('jpn','kor')) {
    $entries = [ordered]@{}
    foreach ($ancient in $dialogues[$locale].Keys) {
        $sets = $dialogues[$locale][$ancient]
        $roles = if ($ancient -eq 'THE_ARCHITECT') { $architect } else { $standard }
        for ($i = 0; $i -lt $sets.Count; $i++) {
            $repeat = if (($ancient -eq 'THE_ARCHITECT' -and $i -eq 3) -or ($ancient -ne 'THE_ARCHITECT' -and $i -gt 0)) { 'r' } else { '' }
            for ($j = 0; $j -lt $sets[$i].Count; $j++) {
                $stem = "$ancient.talk.WYLDER.$i-$j$repeat"
                $entries["$stem.$($roles[$i][$j])"] = $sets[$i][$j]
                if ($j -lt $sets[$i].Count - 1) { $entries["$stem.next"] = if ($locale -eq 'jpn') { '続ける' } else { '계속' } }
            }
        }
    }
    if ($entries.Count -ne 104) { throw "$locale ancient entry count was $($entries.Count), expected 104" }
    Merge-Entries (Join-Path $root "NightMustStay/localization/$locale/ancients.json") $entries
    Write-Host "${locale}/ancients: 104 entries"
}
