param(
    [string]$Sts2AssemblyDir = 'D:\SteamLibrary\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$bundledPython = Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
$python = if (Test-Path -LiteralPath $bundledPython) { $bundledPython } else {
    $command = Get-Command python -ErrorAction SilentlyContinue
    if ($command -and $command.Source -notlike '*\WindowsApps\*') { $command.Source } else { $null }
}
if (-not $python) { throw 'Python is required to verify the generated Duchess card text.' }

Push-Location $root
try {
    & $python -X utf8 (Join-Path $PSScriptRoot 'generate_duchess_cards.py') --check
    if ($LASTEXITCODE -ne 0) { throw 'Duchess generated card text is out of date or invalid.' }

    $project = Join-Path $root 'NightMustStay.csproj'
    dotnet build $project -c Debug --no-restore "-p:Sts2AssemblyDir=$Sts2AssemblyDir" -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Duchess Debug build failed.' }

    $tests = Join-Path $PSScriptRoot 'duchess_model_tests\duchess_model_tests.csproj'
    $debugDirectory = Join-Path $root '.godot\mono\temp\bin\Debug'
    dotnet build $tests --no-restore -t:Rebuild "-p:Sts2AssemblyDir=$Sts2AssemblyDir" "-p:NightMustStayAssemblyDir=$debugDirectory" -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Duchess upgrade model test build failed.' }
    dotnet run --project $tests --no-build --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Duchess upgrade model tests failed.' }
}
finally {
    Pop-Location
}
