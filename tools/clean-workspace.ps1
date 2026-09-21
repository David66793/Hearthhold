[CmdletBinding(SupportsShouldProcess)]
param(
    [switch]$IncludeTestArtifacts,
    [switch]$IncludeCurrentBuild
)

$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path

function Remove-SafePath([string]$RelativePath) {
    $taskTarget = [System.IO.Path]::GetFullPath((Join-Path $taskRoot $RelativePath))
    $taskPrefix = $taskRoot.TrimEnd('\') + '\'
    if (-not $taskTarget.StartsWith($taskPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw ('Refusing to remove a path outside the repository: ' + $taskTarget)
    }
    if (Test-Path -LiteralPath $taskTarget) {
        if ($PSCmdlet.ShouldProcess($taskTarget, 'Remove generated content')) {
            Remove-Item -LiteralPath $taskTarget -Recurse -Force
        }
    }
}

# Unity recreates these folders from Assets, Packages and ProjectSettings.
Remove-SafePath 'UnityProject\Library'
Remove-SafePath 'UnityProject\Temp'
Remove-SafePath 'UnityProject\Logs'
Remove-SafePath 'UnityProject\Obj'
Remove-SafePath 'UnityProject\UserSettings'

# Test outputs are optional because screenshots may still be useful for review.
if ($IncludeTestArtifacts) {
    Remove-SafePath 'artifacts\UnityLogs'
    Remove-SafePath 'artifacts\TestData'
    Remove-SafePath 'artifacts\screenshots'
    Remove-SafePath 'artifacts\CoreTests.exe'
}

if ($IncludeCurrentBuild) {
    Remove-SafePath 'artifacts\WindowsUnity'
    Remove-SafePath 'artifacts\Hearthhold-current-unity-win-x64.zip'
}

Write-Output 'Requested generated content cleaned. Source assets were preserved.'
