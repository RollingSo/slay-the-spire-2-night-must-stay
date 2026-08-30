param(
    [Parameter(Mandatory = $true)]
    [string]$BetaAssemblyDir,
    [string]$StableAssemblyDir
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $projectRoot 'NightMustStay.csproj'
if ([string]::IsNullOrWhiteSpace($StableAssemblyDir)) {
    $StableAssemblyDir = Join-Path $projectRoot 'sts2dll'
}

if (-not (Test-Path -LiteralPath (Join-Path $StableAssemblyDir 'sts2.dll'))) {
    throw "Stable sts2.dll was not found in: $StableAssemblyDir"
}
if (-not (Test-Path -LiteralPath (Join-Path $BetaAssemblyDir 'sts2.dll'))) {
    throw "Beta sts2.dll was not found in: $BetaAssemblyDir"
}

function Build-Branch([string]$Name, [string]$AssemblyDir) {
    $obj = ".godot\mono\temp\obj\Compatibility$Name\"
    $out = "build\bin\Compatibility$Name\"
    & dotnet build $project -c Release --no-restore `
        "-p:Sts2AssemblyDir=$AssemblyDir" `
        "-p:IntermediateOutputPath=$obj" `
        "-p:OutputPath=$out" `
        -v:minimal
    if ($LASTEXITCODE -ne 0) {
        throw "$Name compatibility build failed."
    }
}

Build-Branch 'Stable' $StableAssemblyDir
Build-Branch 'PublicBeta' $BetaAssemblyDir
Write-Host 'Night Must Stay compiled successfully against Stable and Public Beta.'
