[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
$taskAssemblies = Join-Path $taskRoot 'UnityProject\Assets\Hearthhold'
$taskCore = Get-Content -LiteralPath (Join-Path $taskAssemblies 'Core\Hearthhold.Core.asmdef') -Raw | ConvertFrom-Json
$taskRuntime = Get-Content -LiteralPath (Join-Path $taskAssemblies 'Runtime\Hearthhold.Runtime.asmdef') -Raw | ConvertFrom-Json
$taskEditor = Get-Content -LiteralPath (Join-Path $taskAssemblies 'Editor\Hearthhold.Editor.asmdef') -Raw | ConvertFrom-Json

if ($taskCore.name -ne 'Hearthhold.Core' -or $taskCore.noEngineReferences -ne $true -or @($taskCore.references).Count -ne 0) {
    throw 'Core must remain an engine-independent assembly with no project references.'
}
if ($taskRuntime.name -ne 'Hearthhold.Runtime' -or 'Hearthhold.Core' -notin @($taskRuntime.references) -or 'Hearthhold.Editor' -in @($taskRuntime.references)) {
    throw 'Runtime may depend on Core, but must not depend on Editor.'
}
if ($taskEditor.name -ne 'Hearthhold.Editor' -or 'Editor' -notin @($taskEditor.includePlatforms)) {
    throw 'Editor tools must remain in an Editor-only assembly.'
}

$taskCoreFiles = @(Get-ChildItem -LiteralPath (Join-Path $taskAssemblies 'Core') -Filter '*.cs' -File)
foreach ($taskFile in $taskCoreFiles) {
    $taskSource = Get-Content -LiteralPath $taskFile.FullName -Raw
    if ($taskSource -match '(?m)^\s*using\s+Unity(Engine|Editor)\b|\bUnity(Engine|Editor)\.') {
        throw ('Unity API leaked into Core: ' + $taskFile.Name)
    }
}

$taskRuntimeDir = Join-Path $taskAssemblies 'Runtime'
$taskLegacyModalPatterns = 'DrawExtraModals\s*\(', 'DrawResearchModal\s*\(', 'DrawHeroManagement\s*\('
foreach ($taskFile in Get-ChildItem -LiteralPath $taskRuntimeDir -Filter '*.cs' -File) {
    $taskSource = Get-Content -LiteralPath $taskFile.FullName -Raw
    foreach ($taskPattern in $taskLegacyModalPatterns) {
        if ($taskSource -match $taskPattern) { throw ('Duplicate legacy modal implementation found in ' + $taskFile.Name) }
    }
}

Write-Output ('Architecture checks passed: Core engine-free (' + $taskCoreFiles.Count + ' files), Runtime -> Core, Editor-only tooling, no duplicate IMGUI village modals.')
