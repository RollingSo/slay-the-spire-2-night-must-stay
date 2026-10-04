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

# Compilation alone misses return-type changes in referenced game methods.
# JIT the same Production DLL against both runtimes, especially the shared
# Duchess OnPlay async body (an invalid call here prevents every card play).
$apiInspector = Join-Path $projectRoot 'tools\inspect_sts2_api\inspect_sts2_api.csproj'
$stableMod = Join-Path $projectRoot 'build\bin\CompatibilityStable\NightMustStay.dll'
foreach ($runtimeDir in @($StableAssemblyDir, $BetaAssemblyDir)) {
    & dotnet run --project $apiInspector -- `
        (Join-Path $runtimeDir 'sts2.dll') unused prepare-duchess $BetaAssemblyDir $stableMod
    if ($LASTEXITCODE -ne 0) {
        throw "Duchess runtime API binding failed against: $runtimeDir"
    }
}

function Assert-NoModelIdCollisions(
    [string]$Name,
    [string]$AssemblyDir,
    [string]$ModAssemblyPath,
    [string]$DependencyDirectory) {
    $validatorProject = Join-Path $projectRoot `
        'tools\ModelIdCollisionValidator\ModelIdCollisionValidator.csproj'
    dotnet run --project $validatorProject --configuration Release -- `
        (Join-Path $AssemblyDir 'sts2.dll') $ModAssemblyPath $DependencyDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "$Name model ID collision scan failed with exit code $LASTEXITCODE"
    }
}

Assert-NoModelIdCollisions `
    'Stable' `
    $StableAssemblyDir `
    (Join-Path $projectRoot 'build\bin\CompatibilityStable\NightMustStay.dll') `
    $BetaAssemblyDir
Assert-NoModelIdCollisions `
    'Public Beta' `
    $BetaAssemblyDir `
    (Join-Path $projectRoot 'build\bin\CompatibilityPublicBeta\NightMustStay.dll') `
    $BetaAssemblyDir
Write-Host 'Night Must Stay compiled successfully against Stable and Public Beta.'
