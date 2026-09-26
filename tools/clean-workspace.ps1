[CmdletBinding(SupportsShouldProcess)]
param(
    [switch]$IncludeTransientArtifacts,
    [switch]$IncludeTestArtifacts,
    [switch]$PruneObsoleteScreenshots,
    [switch]$IncludeCurrentBuild
)

$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
if (-not $WhatIfPreference -and @(Get-Process -Name Unity -ErrorAction SilentlyContinue).Count -gt 0) {
    throw 'Close Unity Editor before cleaning its generated project cache.'
}

function Remove-SafePath([string]$RelativePath) {
    $taskTarget = [System.IO.Path]::GetFullPath((Join-Path $taskRoot $RelativePath))
    $taskPrefix = $taskRoot.TrimEnd('\') + '\'
    if (-not $taskTarget.StartsWith($taskPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw ('Refusing to remove a path outside the repository: ' + $taskTarget)
    }
    if (Test-Path -LiteralPath $taskTarget) {
        # A junction/symlink anywhere in the candidate path could escape the lexical repository check.
        $taskPart = $taskRoot
        foreach ($taskSegment in ($RelativePath -split '[\\/]')) {
            $taskPart = Join-Path $taskPart $taskSegment
            if (Test-Path -LiteralPath $taskPart) {
                $taskItem = Get-Item -LiteralPath $taskPart -Force
                if (($taskItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                    throw ('Refusing to remove a reparse-point path: ' + $taskPart)
                }
            }
        }
        if ((Get-Item -LiteralPath $taskTarget -Force).PSIsContainer) {
            $taskNestedLinks = @(Get-ChildItem -LiteralPath $taskTarget -Recurse -Force -Attributes ReparsePoint -ErrorAction Stop)
            if ($taskNestedLinks.Count -gt 0) {
                throw ('Refusing to remove a directory containing reparse points: ' + $taskTarget)
            }
        }
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

# Transient test files are optional; screenshots may still be useful for review.
if ($IncludeTransientArtifacts -or $IncludeTestArtifacts) {
    Remove-SafePath 'artifacts\UnityLogs'
    Remove-SafePath 'artifacts\TestData'
    Remove-SafePath 'artifacts\CoreTests.exe'
}
if ($IncludeTestArtifacts) { Remove-SafePath 'artifacts\screenshots' }

if ($PruneObsoleteScreenshots -and -not $IncludeTestArtifacts) {
    $taskScreenshotDir = Join-Path $taskRoot 'artifacts\screenshots'
    if (Test-Path -LiteralPath $taskScreenshotDir -PathType Container) {
        $taskSmokeScript = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'test-unity-player.ps1') -Raw
        $taskCurrentNames = @{}
        foreach ($taskMatch in [regex]::Matches($taskSmokeScript, "Invoke-HearthholdSmoke\s+'[^']+'\s+'([^']+\.png)'")) {
            $taskCurrentNames[$taskMatch.Groups[1].Value] = $true
        }
        if ($taskCurrentNames.Count -lt 20) { throw 'Current screenshot allowlist could not be read from test-unity-player.ps1.' }
        foreach ($taskFile in Get-ChildItem -LiteralPath $taskScreenshotDir -File) {
            if ($taskFile.Extension -eq '.png' -and $taskCurrentNames.ContainsKey($taskFile.Name)) { continue }
            if ($taskFile.Extension -eq '.png' -or $taskFile.Name -match '\.village\.xml(\.bak)?$' -or $taskFile.Name -match '^smoke-village\.xml(\.bak)?$') {
                Remove-SafePath ('artifacts\screenshots\' + $taskFile.Name)
            }
        }
    }
}

if ($IncludeCurrentBuild) {
    Remove-SafePath 'artifacts\WindowsUnity'
    Remove-SafePath 'artifacts\Hearthhold-current-unity-win-x64.zip'
}

Write-Output 'Requested generated content cleaned. Source assets were preserved.'
