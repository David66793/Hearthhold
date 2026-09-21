[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $taskCompiler -PathType Leaf)) {
    throw 'Windows .NET Framework 4.x compiler is required for the standalone core checks.'
}

$taskArtifacts = Join-Path $taskRoot 'artifacts'
New-Item -ItemType Directory -Path $taskArtifacts -Force | Out-Null
$taskCore = @(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'UnityProject\Assets\Hearthhold\Core') -Filter '*.cs' | ForEach-Object FullName)
$taskTestSource = Join-Path $taskRoot 'Tests\CoreTests.cs'
$taskTestOut = Join-Path $taskArtifacts 'CoreTests.exe'

& $taskCompiler /nologo /target:exe /optimize+ /platform:x64 /codepage:65001 /r:System.dll /r:System.Core.dll /r:System.Xml.dll ('/out:' + $taskTestOut) @taskCore $taskTestSource
if ($LASTEXITCODE -ne 0) { throw 'Core test compilation failed.' }
& $taskTestOut
if ($LASTEXITCODE -ne 0) { throw 'Core checks failed.' }
