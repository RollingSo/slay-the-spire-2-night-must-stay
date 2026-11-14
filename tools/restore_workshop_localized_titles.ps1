param(
    [string]$Updater = (Join-Path $PSScriptRoot 'WorkshopMetadataUpdater/bin/Release/net9.0/WorkshopMetadataUpdater.exe'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../.tmp/workshop-localized-titles')
)

$ErrorActionPreference = 'Stop'
$titles = Get-Content (Join-Path $PSScriptRoot 'workshop_localized_titles.json') -Raw -Encoding UTF8 | ConvertFrom-Json
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

foreach ($language in @('english', 'schinese', 'japanese', 'koreana')) {
    $configPath = Join-Path $OutputDirectory "$language.json"
    [ordered]@{
        Language = $language
        Title = [string]$titles.$language
        ChangeNote = ''
    } | ConvertTo-Json | Set-Content -LiteralPath $configPath -Encoding UTF8

    & $Updater $configPath
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to restore the $language Workshop title (exit $LASTEXITCODE)."
    }
}
