[CmdletBinding()]
param(
    [ValidateSet('All', 'Home', 'Campaign', 'Training', 'Battle', 'Deploy')][string]$Case = 'All',
    [string]$PlayerPath = ''
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
if (-not $PlayerPath) { $PlayerPath = Join-Path $taskRoot 'artifacts\WindowsUnity\Hearthhold.exe' }
if (-not (Test-Path -LiteralPath $PlayerPath -PathType Leaf)) { throw ('Unity player not found: ' + $PlayerPath + '. Build it first.') }
$PlayerPath = (Resolve-Path -LiteralPath $PlayerPath).Path
$taskScreenshots = Join-Path $taskRoot 'artifacts\screenshots'
$taskLogs = Join-Path $taskRoot 'artifacts\UnityLogs'
New-Item -ItemType Directory -Path $taskScreenshots, $taskLogs -Force | Out-Null

function Invoke-HearthholdSmoke([string]$Name, [string]$FileName, [string]$Mode) {
    $taskShot = Join-Path $taskScreenshots $FileName
    $taskLog = Join-Path $taskLogs ('player-smoke-' + $Name + '.log')
    $taskArguments = @(
        '-force-d3d11', '-screen-width', '1440', '-screen-height', '900', '-popupwindow',
        '-hearthhold-smoke', ('"' + $taskShot + '"')
    )
    if ($Mode -eq 'Battle') { $taskArguments += '-hearthhold-smoke-battle' }
    if ($Mode -eq 'Deploy') { $taskArguments += '-hearthhold-smoke-deploy' }
    if ($Mode -eq 'Campaign') { $taskArguments += '-hearthhold-smoke-campaign' }
    if ($Mode -eq 'Training') { $taskArguments += '-hearthhold-smoke-training' }
    $taskArguments += @('-logFile', ('"' + $taskLog + '"'))
    $taskStarted = [DateTime]::UtcNow
    Write-Output ('Starting ' + $Name + ' smoke test. The game window closes automatically.')
    $taskProcess = Start-Process -FilePath $PlayerPath -ArgumentList $taskArguments -Wait -PassThru
    if ($taskProcess.ExitCode -ne 0) { throw ($Name + ' smoke test exited with code ' + $taskProcess.ExitCode + '. Inspect ' + $taskLog) }
    if (-not (Test-Path -LiteralPath $taskShot -PathType Leaf)) { throw ($Name + ' smoke test did not create ' + $taskShot) }
    $taskImage = Get-Item -LiteralPath $taskShot
    if ($taskImage.Length -le 1024 -or $taskImage.LastWriteTimeUtc -lt $taskStarted.AddSeconds(-1)) { throw ($Name + ' smoke screenshot is empty or stale: ' + $taskShot) }
    if (-not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_SMOKE_READY:' -SimpleMatch -Quiet)) { throw ($Name + ' log has no completion marker: ' + $taskLog) }
    if (($Mode -eq 'Battle' -or $Mode -eq 'Deploy') -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_DEPLOY_SMOKE_READY:' -SimpleMatch -Quiet)) { throw ($Name + ' log has no successful central-click deployment marker: ' + $taskLog) }
    if ($Mode -eq 'Battle' -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_ACTION_SMOKE_READY:' -SimpleMatch -Quiet)) { throw ($Name + ' log has no troop and defense action marker: ' + $taskLog) }
    if ($Mode -eq 'Deploy' -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_DEPLOY_VISUAL_READY:' -SimpleMatch -Quiet)) { throw ($Name + ' log has no synchronized deployed-unit visual marker: ' + $taskLog) }
    if (Select-String -LiteralPath $taskLog -Pattern 'NullReferenceException|MissingReferenceException|Shader error|HEARTHHOLD_SMOKE_TIMEOUT' -Quiet) { throw ($Name + ' log contains a runtime or rendering error: ' + $taskLog) }
    Write-Output ($Name + ' smoke test passed: ' + $taskShot + ' (' + $taskImage.Length + ' bytes)')
}

if ($Case -eq 'All' -or $Case -eq 'Home') { Invoke-HearthholdSmoke 'home-v062' '36-unity-home-v062.png' 'Home' }
if ($Case -eq 'All' -or $Case -eq 'Campaign') { Invoke-HearthholdSmoke 'campaign-v062' '37-unity-campaign-v062.png' 'Campaign' }
if ($Case -eq 'All' -or $Case -eq 'Training') { Invoke-HearthholdSmoke 'training-v062' '38-unity-training-v062.png' 'Training' }
if ($Case -eq 'All' -or $Case -eq 'Battle') { Invoke-HearthholdSmoke 'battle-v062' '39-unity-battle-v062.png' 'Battle' }
if ($Case -eq 'All' -or $Case -eq 'Deploy') { Invoke-HearthholdSmoke 'deploy-v062' '40-unity-deploy-v062.png' 'Deploy' }
