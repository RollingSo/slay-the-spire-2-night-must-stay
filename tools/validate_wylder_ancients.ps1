$ErrorActionPreference = 'Stop'
$wylderPython = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
if (-not (Test-Path -LiteralPath $wylderPython)) { throw 'Bundled Python required for Wylder Ancient dialogue validation.' }
& $wylderPython -X utf8 (Join-Path $PSScriptRoot 'sync_wylder_ancients.py') --check
if ($LASTEXITCODE -ne 0) { throw 'Wylder Ancient dialogue keys are missing or out of sync.' }
