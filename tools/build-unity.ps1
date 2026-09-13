[CmdletBinding()]
param(
    [ValidateSet('Check', 'Prepare', 'Build')][string]$Action = 'Check',
    [string]$EditorPath = '',
    [switch]$Package
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskProject = Join-Path $taskRoot 'UnityProject'
$taskVersionText = Get-Content -LiteralPath (Join-Path $taskProject 'ProjectSettings\ProjectVersion.txt') -Raw
if ($taskVersionText -notmatch 'm_EditorVersion:\s*(\S+)') { throw 'ProjectVersion.txt has no Editor version.' }
$taskVersion = $Matches[1]
$taskHub = Join-Path $env:ProgramFiles 'Unity Hub\Unity Hub.exe'

if (-not $EditorPath) {
    $taskCandidates = @(
        (Join-Path $env:ProgramFiles ('Unity\Hub\Editor\' + $taskVersion + '\Editor\Unity.exe')),
        (Join-Path $taskRoot ('artifacts\Unity\' + $taskVersion + '\Editor\Unity.exe'))
    )
    foreach ($taskCandidate in $taskCandidates) {
        if (Test-Path -LiteralPath $taskCandidate -PathType Leaf) { $EditorPath = $taskCandidate; break }
    }
}

$taskEditorFound = $false
$taskEditorVersion = ''
if ($EditorPath) {
    if (-not (Test-Path -LiteralPath $EditorPath -PathType Leaf)) { throw ('Editor not found: ' + $EditorPath) }
    $EditorPath = (Resolve-Path -LiteralPath $EditorPath).Path
    if ([System.IO.Path]::GetFileName($EditorPath) -ine 'Unity.exe') { throw 'EditorPath must point to Unity.exe.' }
    $taskEditorFound = $true
    $taskEditorVersion = (Get-Item -LiteralPath $EditorPath).VersionInfo.ProductVersion
}
$taskVersionMatches = $taskEditorFound -and ($taskEditorVersion -match ('^' + [regex]::Escape($taskVersion) + '(?:\s|_|\(|$)'))
if ($Action -eq 'Check') {
    [pscustomobject]@{
        Project = $taskProject
        RequiredVersion = $taskVersion
        HubInstalled = (Test-Path -LiteralPath $taskHub -PathType Leaf)
        EditorFound = $taskEditorFound
        EditorPath = $EditorPath
        DetectedVersion = $taskEditorVersion
        VersionMatches = $taskVersionMatches
        LicenseStatus = 'Not checked; sign in and activate through Unity Hub yourself.'
        UnityBuildVerified = $false
    }
    return
}
if (-not $taskEditorFound) { throw ('Install Unity ' + $taskVersion + ' and activate your license in Hub, then retry. For a custom location pass -EditorPath <full-path-to-Unity.exe>.') }
if (-not $taskVersionMatches) { throw ('Editor version mismatch: expected ' + $taskVersion + ', found ' + $taskEditorVersion + '. No automatic project upgrade or downgrade was performed.') }

$taskLogDir = Join-Path $taskRoot 'artifacts\UnityLogs'
New-Item -ItemType Directory -Path $taskLogDir -Force | Out-Null
$taskLog = Join-Path $taskLogDir ($Action.ToLowerInvariant() + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '.log')
$taskMethod = if ($Action -eq 'Build') { 'Hearthhold.Editor.ProjectSetup.BuildWindows' } else { 'Hearthhold.Editor.ProjectSetup.Prepare' }
$taskArguments = @('-batchmode', '-quit', '-buildTarget', 'StandaloneWindows64', '-projectPath', ('"' + $taskProject + '"'), '-executeMethod', $taskMethod, '-logFile', ('"' + $taskLog + '"'))
Write-Output ('Unity ' + $Action + ' started. Close any Editor already using this project.')
Write-Output ('Log: ' + $taskLog)
$taskStarted = [DateTime]::UtcNow
$taskProcess = Start-Process -FilePath $EditorPath -ArgumentList $taskArguments -WindowStyle Hidden -PassThru
while (-not $taskProcess.WaitForExit(15000)) {
    Write-Output ('Unity is still running; elapsed ' + [int]([DateTime]::UtcNow - $taskStarted).TotalSeconds + ' seconds. See the log above for import/build progress.')
}
if ($taskProcess.ExitCode -ne 0) {
    throw ('Unity exited with code ' + $taskProcess.ExitCode + '. Inspect ' + $taskLog + '. If licensing is required, finish it in Hub; this script never submits account credentials or activates a license.')
}
if ($Action -eq 'Build') {
    $taskPlayer = Join-Path $taskRoot 'artifacts\WindowsUnity\Hearthhold.exe'
    $taskPlayerData = Join-Path $taskRoot 'artifacts\WindowsUnity\Hearthhold_Data'
    $taskEngine = Join-Path $taskRoot 'artifacts\WindowsUnity\UnityPlayer.dll'
    if (-not (Test-Path -LiteralPath $taskPlayer -PathType Leaf) -or -not (Test-Path -LiteralPath $taskPlayerData -PathType Container) -or -not (Test-Path -LiteralPath $taskEngine -PathType Leaf)) {
        throw ('Unity returned success but the Windows player output is incomplete. Inspect ' + $taskLog)
    }
    if (-not (Select-String -LiteralPath $taskLog -Pattern 'Windows build ready:' -SimpleMatch -Quiet)) { throw ('The current log has no build success marker. Old build files are not proof of success: ' + $taskLog) }
    Write-Output ('Windows Unity build produced: ' + $taskPlayer)
    Write-Output 'Build completed; runtime gameplay and visual verification are still required.'
    if ($Package) {
        $taskReleaseReadme = Join-Path $taskRoot 'release\UNITY-README.txt'
        if (-not (Test-Path -LiteralPath $taskReleaseReadme -PathType Leaf)) { throw ('Unity release guide is missing: ' + $taskReleaseReadme) }
        $taskPackage = Join-Path $taskRoot 'artifacts\Hearthhold-0.6.2-unity-win-x64.zip'
        $taskPackageInputs = @(Get-ChildItem -LiteralPath (Split-Path -Parent $taskPlayer) | Where-Object Name -ne 'Hearthhold_BackUpThisFolder_ButDontShipItWithYourGame' | ForEach-Object FullName)
        $taskPackageInputs += $taskReleaseReadme
        Compress-Archive -LiteralPath $taskPackageInputs -DestinationPath $taskPackage -Force
        Write-Output ('Packaged: ' + $taskPackage)
        Get-FileHash -LiteralPath $taskPackage -Algorithm SHA256 | Select-Object Algorithm, Hash, Path
    }
} else {
    Write-Output 'Unity project preparation completed. This is not a Windows player build or a gameplay test.'
}
