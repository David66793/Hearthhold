param([switch]$Test, [switch]$Package)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $taskCompiler)) { throw 'Windows .NET Framework 4.x compiler is required for the standalone preview.' }
$taskOut = Join-Path $taskRoot 'artifacts\WindowsPreview'
New-Item -ItemType Directory -Path $taskOut -Force | Out-Null
$taskCore = @(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'UnityProject\Assets\Hearthhold\Core') -Filter '*.cs' | ForEach-Object FullName)
$taskUi = @(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'NativePreview') -Filter '*.cs' | ForEach-Object FullName)
$taskArgs = @('/nologo', '/target:winexe', '/optimize+', '/platform:x64', '/utf8output', '/codepage:65001', '/r:System.dll', '/r:System.Core.dll', '/r:System.Drawing.dll', '/r:System.Windows.Forms.dll', '/r:System.Xml.dll', ('/out:' + (Join-Path $taskOut 'Hearthhold.exe')))
& $taskCompiler @taskArgs @taskCore @taskUi
if ($LASTEXITCODE -ne 0) { throw 'Preview compilation failed.' }
Write-Output ('Built: ' + (Join-Path $taskOut 'Hearthhold.exe'))
if ($Test) {
    $taskTestOut = Join-Path $taskRoot 'artifacts\CoreTests.exe'
    $taskTestSource = Join-Path $taskRoot 'Tests\CoreTests.cs'
    & $taskCompiler /nologo /target:exe /optimize+ /platform:x64 /codepage:65001 /r:System.dll /r:System.Core.dll /r:System.Xml.dll ('/out:' + $taskTestOut) @taskCore $taskTestSource
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
    & $taskTestOut
    if ($LASTEXITCODE -ne 0) { throw 'Core checks failed.' }
    $taskUiTestOut = Join-Path $taskOut 'UiSmoke.exe'
    & $taskCompiler /nologo /target:exe /optimize+ /platform:x64 /codepage:65001 /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ('/r:' + (Join-Path $taskOut 'Hearthhold.exe')) ('/out:' + $taskUiTestOut) (Join-Path $taskRoot 'Tests\UiSmoke.cs')
    if ($LASTEXITCODE -ne 0) { throw 'UI smoke compilation failed.' }
    & $taskUiTestOut
    if ($LASTEXITCODE -ne 0) { throw 'UI smoke checks failed.' }
}
if ($Package) {
    $taskGuide = Join-Path $taskRoot 'release\README.txt'
    $taskGuideCopy = Join-Path $taskOut 'README.txt'
    Copy-Item -LiteralPath $taskGuide -Destination $taskGuideCopy -Force
    $taskZip = Join-Path $taskRoot 'artifacts\Hearthhold-0.6.2-native-win-x64.zip'
    Compress-Archive -LiteralPath @((Join-Path $taskOut 'Hearthhold.exe'), $taskGuideCopy) -DestinationPath $taskZip -Force
    Write-Output ('Packaged: ' + $taskZip)
    Get-FileHash -LiteralPath $taskZip -Algorithm SHA256 | Select-Object Algorithm,Hash,Path
}
