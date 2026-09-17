[CmdletBinding()]
param(
    [ValidateSet('All', 'Home', 'Campaign', 'Training', 'Research', 'Battle', 'Deploy', 'Rotated', 'Detail', 'ArmyDetail', 'MainTroops', 'RemainingTroops', 'Casters', 'SavedVillage')][string]$Case = 'All',
    [string]$PlayerPath = '',
    [string]$SaveSource = ''
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
if (-not $PlayerPath) { $PlayerPath = Join-Path $taskRoot 'artifacts\WindowsUnity\Hearthhold.exe' }
if (-not (Test-Path -LiteralPath $PlayerPath -PathType Leaf)) { throw ('Unity player not found: ' + $PlayerPath + '. Build it first.') }
$PlayerPath = (Resolve-Path -LiteralPath $PlayerPath).Path
if ($Case -eq 'SavedVillage') {
    if (-not $SaveSource -or -not (Test-Path -LiteralPath $SaveSource -PathType Leaf)) { throw 'SavedVillage requires -SaveSource with an existing village.xml path.' }
    $SaveSource = (Resolve-Path -LiteralPath $SaveSource).Path
}
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
    if ($Mode -eq 'Research') { $taskArguments += '-hearthhold-smoke-research' }
    if ($Mode -eq 'Rotated') { $taskArguments += '-hearthhold-smoke-rotated' }
    if ($Mode -eq 'Detail') { $taskArguments += '-hearthhold-smoke-detail' }
    if ($Mode -eq 'ArmyDetail') { $taskArguments += '-hearthhold-smoke-army-detail' }
    if ($Mode -eq 'SavedVillage') { $taskArguments += @('-hearthhold-smoke-save-source', ('"' + $SaveSource + '"')) }
    if ($Mode -eq 'VanguardDetail') { $taskArguments += @('-hearthhold-smoke-army-detail', '-hearthhold-smoke-army-kind', '0') }
    if ($Mode -eq 'RangerDetail') { $taskArguments += @('-hearthhold-smoke-army-detail', '-hearthhold-smoke-army-kind', '1') }
    if ($Mode -eq 'SapperDetail') { $taskArguments += @('-hearthhold-smoke-army-detail', '-hearthhold-smoke-army-kind', '3') }
    if ($Mode -eq 'GuardianDetail') { $taskArguments += @('-hearthhold-smoke-army-detail', '-hearthhold-smoke-army-kind', '2') }
    if ($Mode -eq 'SkyRiderDetail') { $taskArguments += @('-hearthhold-smoke-army-detail', '-hearthhold-smoke-army-kind', '4') }
    if ($Mode -eq 'AlchemistDetail') { $taskArguments += @('-hearthhold-smoke-army-detail', '-hearthhold-smoke-army-kind', '5') }
    if ($Mode -eq 'MedicDetail') { $taskArguments += @('-hearthhold-smoke-army-detail', '-hearthhold-smoke-army-kind', '6') }
    if ($Mode -eq 'SummonerDetail') { $taskArguments += @('-hearthhold-smoke-army-detail', '-hearthhold-smoke-army-kind', '7') }
    $taskArguments += @('-logFile', ('"' + $taskLog + '"'))
    $taskStarted = [DateTime]::UtcNow
    Write-Output ('Starting ' + $Name + ' smoke test. The game window closes automatically.')
    & $PlayerPath @taskArguments
    if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw ($Name + ' smoke test exited with code ' + $LASTEXITCODE + '. Inspect ' + $taskLog) }
    # PowerShell may return immediately for a GUI executable, without setting LASTEXITCODE.
    $taskDeadline = [DateTime]::UtcNow.AddSeconds(45)
    while ([DateTime]::UtcNow -lt $taskDeadline) {
        $taskFreshLog = (Test-Path -LiteralPath $taskLog -PathType Leaf) -and (Get-Item -LiteralPath $taskLog).LastWriteTimeUtc -ge $taskStarted.AddSeconds(-1)
        $taskFreshShot = (Test-Path -LiteralPath $taskShot -PathType Leaf) -and (Get-Item -LiteralPath $taskShot).LastWriteTimeUtc -ge $taskStarted.AddSeconds(-1)
        if ($taskFreshLog -and $taskFreshShot -and (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_SMOKE_READY:' -SimpleMatch -Quiet)) { break }
        Start-Sleep -Milliseconds 200
    }
    if (-not (Test-Path -LiteralPath $taskShot -PathType Leaf)) { throw ($Name + ' smoke test did not create ' + $taskShot) }
    $taskImage = Get-Item -LiteralPath $taskShot
    if ($taskImage.Length -le 1024 -or $taskImage.LastWriteTimeUtc -lt $taskStarted.AddSeconds(-1)) { throw ($Name + ' smoke screenshot is empty or stale: ' + $taskShot) }
    if (-not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_SMOKE_READY:' -SimpleMatch -Quiet)) { throw ($Name + ' log has no completion marker: ' + $taskLog) }
    if (($Mode -eq 'Battle' -or $Mode -eq 'Deploy') -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_DEPLOY_SMOKE_READY:' -SimpleMatch -Quiet)) { throw ($Name + ' log has no successful central-click deployment marker: ' + $taskLog) }
    if ($Mode -eq 'Battle' -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_ACTION_SMOKE_READY:' -SimpleMatch -Quiet)) { throw ($Name + ' log has no troop and defense action marker: ' + $taskLog) }
    if ($Mode -eq 'Battle' -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_FOCUS_SMOKE_READY:' -SimpleMatch -Quiet)) { throw ($Name + ' log has no tactical focus marker: ' + $taskLog) }
    if ($Mode -eq 'Battle' -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_SPELL_SMOKE_READY:' -SimpleMatch -Quiet)) { throw ($Name + ' log has no four-spell validation marker: ' + $taskLog) }
    if ($Mode -eq 'Home' -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_EXTERNAL_MODEL_READY: ThirdParty/KayKitMedieval/Keep' -SimpleMatch -Quiet)) { throw ($Name + ' log has no external settlement-model marker: ' + $taskLog) }
    if ($Mode -eq 'SavedVillage' -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_SAVED_VILLAGE_SMOKE_READY:' -SimpleMatch -Quiet)) { throw ($Name + ' log has no loaded-village marker: ' + $taskLog) }
    if ($Mode -eq 'Battle' -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_EXTERNAL_MODEL_READY: ThirdParty/QuaterniusRPG/Warrior' -SimpleMatch -Quiet)) { throw ($Name + ' log has no external troop-model marker: ' + $taskLog) }
    if ($Mode -eq 'Battle' -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_STORM_RIG_READY' -SimpleMatch -Quiet)) { throw ($Name + ' log has no articulated storm-tower marker: ' + $taskLog) }
    if ($Mode -eq 'Rotated' -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_ROTATED_SMOKE_READY:' -SimpleMatch -Quiet)) { throw ($Name + ' log has no rotated 3D view marker: ' + $taskLog) }
    if ($Mode -eq 'Deploy' -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_DEPLOY_VISUAL_READY:' -SimpleMatch -Quiet)) { throw ($Name + ' log has no synchronized deployed-unit visual marker: ' + $taskLog) }
    if (($Mode -eq 'Detail' -or $Mode -like '*Detail') -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_DETAIL_PREVIEW_READY:' -SimpleMatch -Quiet)) { throw ($Name + ' log has no animated model-preview marker: ' + $taskLog) }
    if ($Mode -in @('VanguardDetail', 'RangerDetail', 'SapperDetail')) {
        $taskKind = $Mode -replace 'Detail$', ''
        if (-not (Select-String -LiteralPath $taskLog -Pattern ('HEARTHHOLD_IMPORTED_RIG_READY: ' + $taskKind) -SimpleMatch -Quiet)) { throw ($Name + ' log has no ' + $taskKind + ' imported-animation marker: ' + $taskLog) }
        if (-not (Select-String -LiteralPath $taskLog -Pattern ('HEARTHHOLD_IMPORTED_CAST_READY: ' + $taskKind) -SimpleMatch -Quiet)) { throw ($Name + ' log has no ' + $taskKind + ' combat-action marker: ' + $taskLog) }
        if ($Mode -eq 'RangerDetail' -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_IMPORTED_CAST_READY: Ranger clip=CharacterArmature|Bow_Attack_Draw' -SimpleMatch -Quiet)) { throw ($Name + ' attack did not use the bow-draw clip: ' + $taskLog) }
        if ($Mode -eq 'RangerDetail' -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_IMPORTED_FOLLOWUP_READY: Ranger clip=CharacterArmature|Bow_Attack_Shoot' -SimpleMatch -Quiet)) { throw ($Name + ' attack did not release the bow: ' + $taskLog) }
    }
    if ($Mode -eq 'MedicDetail' -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_IMPORTED_RIG_READY: Medic' -SimpleMatch -Quiet)) { throw ($Name + ' log has no medic imported-animation marker: ' + $taskLog) }
    if ($Mode -eq 'SummonerDetail' -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_IMPORTED_RIG_READY: Summoner' -SimpleMatch -Quiet)) { throw ($Name + ' log has no summoner imported-animation marker: ' + $taskLog) }
    if ($Mode -eq 'MedicDetail' -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_IMPORTED_CAST_READY: Medic' -SimpleMatch -Quiet)) { throw ($Name + ' log has no medic spell-action marker: ' + $taskLog) }
    if ($Mode -eq 'SummonerDetail' -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_IMPORTED_CAST_READY: Summoner' -SimpleMatch -Quiet)) { throw ($Name + ' log has no summoner spell-action marker: ' + $taskLog) }
    if ($Mode -in @('GuardianDetail', 'SkyRiderDetail', 'AlchemistDetail')) {
        $taskKind = $Mode -replace 'Detail$', ''
        if (-not (Select-String -LiteralPath $taskLog -Pattern ('HEARTHHOLD_ARTICULATED_RIG_READY: ' + $taskKind) -SimpleMatch -Quiet)) { throw ($Name + ' log has no ' + $taskKind + ' bone-rig marker: ' + $taskLog) }
        if (-not (Select-String -LiteralPath $taskLog -Pattern ('HEARTHHOLD_ARTICULATED_ATTACK_READY: ' + $taskKind) -SimpleMatch -Quiet)) { throw ($Name + ' log has no ' + $taskKind + ' attack-action marker: ' + $taskLog) }
    }
    if ($Mode -eq 'SkyRiderDetail' -and -not (Select-String -LiteralPath $taskLog -Pattern 'HEARTHHOLD_ARTICULATED_FLIGHT_READY: SkyRider' -SimpleMatch -Quiet)) { throw ($Name + ' log has no articulated flight marker: ' + $taskLog) }
    if (Select-String -LiteralPath $taskLog -Pattern 'NullReferenceException|MissingReferenceException|Shader error|HEARTHHOLD_SMOKE_TIMEOUT' -Quiet) { throw ($Name + ' log contains a runtime or rendering error: ' + $taskLog) }
    Write-Output ($Name + ' smoke test passed: ' + $taskShot + ' (' + $taskImage.Length + ' bytes)')
}

if ($Case -eq 'All' -or $Case -eq 'Home') { Invoke-HearthholdSmoke 'home-v090' '62-unity-home-v090.png' 'Home' }
if ($Case -eq 'SavedVillage') { Invoke-HearthholdSmoke 'saved-village-v111' '77-unity-saved-village-v111.png' 'SavedVillage' }
if ($Case -eq 'All' -or $Case -eq 'Campaign') { Invoke-HearthholdSmoke 'campaign-v090' '63-unity-campaign-v090.png' 'Campaign' }
if ($Case -eq 'All' -or $Case -eq 'Training') { Invoke-HearthholdSmoke 'training-v090' '64-unity-training-v090.png' 'Training' }
if ($Case -eq 'All' -or $Case -eq 'Research') { Invoke-HearthholdSmoke 'research-v090' '65-unity-research-v090.png' 'Research' }
if ($Case -eq 'All' -or $Case -eq 'Battle') { Invoke-HearthholdSmoke 'battle-v090' '66-unity-battle-v090.png' 'Battle' }
if ($Case -eq 'All' -or $Case -eq 'Deploy') { Invoke-HearthholdSmoke 'deploy-v090' '67-unity-deploy-v090.png' 'Deploy' }
if ($Case -eq 'All' -or $Case -eq 'Rotated') { Invoke-HearthholdSmoke 'rotated-v090' '68-unity-rotated-v090.png' 'Rotated' }
if ($Case -eq 'All' -or $Case -eq 'Detail') { Invoke-HearthholdSmoke 'detail-v091' '69-unity-building-detail-v091.png' 'Detail' }
if ($Case -eq 'All' -or $Case -eq 'ArmyDetail') { Invoke-HearthholdSmoke 'army-detail-v091' '70-unity-army-detail-v091.png' 'ArmyDetail' }
if ($Case -eq 'All' -or $Case -eq 'MainTroops') {
    Invoke-HearthholdSmoke 'vanguard-detail-v110' '74-unity-vanguard-v110.png' 'VanguardDetail'
    Invoke-HearthholdSmoke 'ranger-detail-v110' '75-unity-ranger-v110.png' 'RangerDetail'
    Invoke-HearthholdSmoke 'sapper-detail-v110' '76-unity-sapper-v110.png' 'SapperDetail'
}
if ($Case -eq 'All' -or $Case -eq 'RemainingTroops') {
    Invoke-HearthholdSmoke 'guardian-detail-v112' '78-unity-guardian-v112.png' 'GuardianDetail'
    Invoke-HearthholdSmoke 'sky-rider-detail-v112' '79-unity-sky-rider-v112.png' 'SkyRiderDetail'
}
if ($Case -eq 'RemainingTroops') { Invoke-HearthholdSmoke 'alchemist-detail-v100' '71-unity-alchemist-v100.png' 'AlchemistDetail' }
if ($Case -eq 'All' -or $Case -eq 'Casters') {
    Invoke-HearthholdSmoke 'alchemist-detail-v100' '71-unity-alchemist-v100.png' 'AlchemistDetail'
    Invoke-HearthholdSmoke 'medic-detail-v100' '72-unity-medic-v100.png' 'MedicDetail'
    Invoke-HearthholdSmoke 'summoner-detail-v100' '73-unity-summoner-v100.png' 'SummonerDetail'
}
