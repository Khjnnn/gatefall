$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
. .\Tools\qa-device-ready.ps1
$taskRoot=Join-Path $taskProject 'TestResults/qa-boot-readiness-223'
New-Item -ItemType Directory -Path $taskRoot -Force|Out-Null
$taskRows=@(foreach($taskMode in @('ready','booting','recovers','error-one','connection-only','offline','flaky')){
    $taskMock=Join-Path $taskRoot ($taskMode+'-adb.ps1')
    $taskCalls=Join-Path $taskRoot ($taskMode+'-calls.txt')
    if(Test-Path $taskCalls){Remove-Item -LiteralPath $taskCalls}
    $taskBody=@'
$taskMode='MODE'
$taskCalls='CALLS'
Add-Content -LiteralPath $taskCalls -Value ($args -join ' ')
if($args[-1] -eq 'get-state'){
    $taskStateCount=@(Get-Content $taskCalls|Where-Object {$_ -like '*get-state'}).Count
    if($taskMode -eq 'offline' -or ($taskMode -eq 'flaky' -and $taskStateCount -eq 1)){$global:LASTEXITCODE=1;'offline';return}
    $global:LASTEXITCODE=0;'device';return
}
if(($args -join ' ') -notmatch '^-s emulator-5994 shell getprop sys.boot_completed$'){throw 'Unexpected or unsafe probe'}
$taskBootCount=@(Get-Content $taskCalls|Where-Object {$_ -like '*sys.boot_completed'}).Count
if($taskMode -eq 'ready' -or ($taskMode -eq 'recovers' -and $taskBootCount -ge 2)){$global:LASTEXITCODE=0;'1';return}
if($taskMode -eq 'error-one'){$global:LASTEXITCODE=1;'1';return}
$global:LASTEXITCODE=0;''
'@
    $taskBody=$taskBody.Replace('MODE',$taskMode).Replace('CALLS',$taskCalls.Replace("'","''"))
    Set-Content -LiteralPath $taskMock -Value $taskBody
    $taskSucceeded=$false;$taskError=''
    try {Assert-QaAndroidReady -Serial emulator-5994 -Adb $taskMock -BootCompleted:($taskMode -notin @('connection-only','offline','flaky'));$taskSucceeded=$true}catch{$taskError=$_.Exception.Message}
    $taskExpected=$taskMode -in @('ready','recovers','connection-only','flaky')
    $taskLines=@(Get-Content $taskCalls)
    if($taskSucceeded -ne $taskExpected){throw "Unexpected readiness outcome: $taskMode"}
    if(@($taskLines|Where-Object {$_ -notmatch '^-s emulator-5994 (get-state|shell getprop sys.boot_completed)$'}).Count){throw 'Non-read-only/fallback command'}
    $taskBootCalls=@($taskLines|Where-Object {$_ -like '*sys.boot_completed'}).Count
    $taskExpectedBootCalls=switch($taskMode){'ready'{1};'booting'{3};'recovers'{2};'error-one'{3};default{0}}
    if($taskBootCalls -ne $taskExpectedBootCalls){throw 'Unexpected retry count'}
    [pscustomobject]@{mode=$taskMode;allowed=$taskSucceeded;bootProbes=$taskBootCalls;onlyReadOnlyExplicitSerial=$true;error=$taskError}
})
[ordered]@{created=(Get-Date -Format o);task=223;mockCases=7;rows=$taskRows;actualBootFailureObservedInTask222=$true;actualRecoveryTestedByMocks=$false;productChanged=$false;newApk=$false;passed=$true}|ConvertTo-Json -Depth 6|Set-Content TestResults/qa-boot-readiness-v0208.json
'PASS: seven mocked boot/connection checks; explicit serial/read-only probes; real emulator recovery not simulated proof'
