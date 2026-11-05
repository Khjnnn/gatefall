param([string]$CoreDirectory='',[switch]$ExpectRejected)
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
if(-not $CoreDirectory){$CoreDirectory=Join-Path $taskProject 'Assets/Scripts/Core'}
Add-Type -Path @((Get-ChildItem -LiteralPath $CoreDirectory -Filter '*.cs').FullName+(Join-Path $taskProject 'Tests/AlleyReadabilityChecks.cs'))
try {$taskResult=[Gatefall.Tests.AlleyReadabilityChecks]::Verify()} catch {
    if($ExpectRejected -and $_.Exception.Message -like '*disconnected north/east roads look joined*'){'PASS negative control rejected close north/east road silhouettes';exit 0}
    throw
}
if($ExpectRejected){throw 'Negative control unexpectedly passed'}
$taskResult
