$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskRoot=Join-Path $taskProject 'TestResults/qa-transfer-229-fixtures'
New-Item -ItemType Directory -Path $taskRoot -Force|Out-Null
$taskRows=@(foreach($taskMode in @('success','fails-once','no-partial','all-fail','capture-fail','protected','missing-serial')){
    # A copied tool tree isolates mock taps/screens/actions from real Native QA evidence.
    $taskCase=Join-Path $taskRoot ($taskMode+'-'+[Guid]::NewGuid().ToString('N'))
    $taskTools=Join-Path $taskCase Tools
    New-Item -ItemType Directory -Path $taskTools -Force|Out-Null
    foreach($taskScript in @('qa-android.ps1','qa-device-ready.ps1','qa-screenshot-transfer.ps1')){
        Copy-Item -LiteralPath (Join-Path $taskProject ('Tools/'+$taskScript)) -Destination $taskTools
    }
    $taskMock=Join-Path $taskCase mock-adb.ps1
    $taskCalls=Join-Path $taskCase calls.txt
    $taskBody=@'
$taskMode='MODE'
$taskCalls='CALLS'
Add-Content -LiteralPath $taskCalls -Value ($args -join ' ')
if($args[-1] -eq 'get-state'){$global:LASTEXITCODE=0;'device';return}
if(($args -join ' ') -match 'shell screencap'){
    $global:LASTEXITCODE=if($taskMode -eq 'capture-fail'){1}else{0};return
}
if($args[2] -eq 'pull'){
    $taskPull=@(Get-Content $taskCalls|Where-Object {$_ -match ' pull '}).Count
    if($taskMode -eq 'all-fail' -or ($taskMode -in @('fails-once','no-partial') -and $taskPull -eq 1)){
        if($taskMode -ne 'no-partial'){[IO.File]::WriteAllText($args[-1],('partial-'+$taskPull))}
        $global:LASTEXITCODE=1;return
    }
    [IO.File]::WriteAllText($args[-1],'same-original-capture');$global:LASTEXITCODE=0;return
}
if(($args -join ' ') -match 'shell input (tap 100 200|keyevent 3)$'){$global:LASTEXITCODE=0;return}
throw 'Unexpected mock command'
'@
    Set-Content -LiteralPath $taskMock -Value $taskBody.Replace('MODE',$taskMode).Replace('CALLS',$taskCalls.Replace("'","''"))
    $taskSerial=if($taskMode -eq 'protected'){'emulator-5558'}elseif($taskMode -eq 'missing-serial'){''}else{'emulator-5994'}
    $taskOk=$false;$taskError=''
    try {& (Join-Path $taskTools qa-android.ps1) -Serial $taskSerial -Adb $taskMock -Taps '100,200' -Shot once -BackgroundAfterShot;$taskOk=$true}catch{$taskError=$_.Exception.Message}
    $taskExpected=$taskMode -in @('success','fails-once','no-partial')
    if($taskOk -ne $taskExpected){throw "Unexpected outcome: $taskMode"}
    $taskLines=if(Test-Path $taskCalls){@(Get-Content $taskCalls)}else{@()}
    $taskPulls=@($taskLines|Where-Object {$_ -match ' pull '}).Count
    $taskCapture=@($taskLines|Where-Object {$_ -match 'shell screencap'}).Count
    $taskTaps=@($taskLines|Where-Object {$_ -match 'shell input tap'}).Count
    $taskHome=@($taskLines|Where-Object {$_ -match 'shell input keyevent 3$'}).Count
    $taskExpectedPulls=switch($taskMode){'success'{1};'fails-once'{2};'no-partial'{2};'all-fail'{3};default{0}}
    $taskNoInput=$taskMode -in @('protected','missing-serial')
    if($taskPulls -ne $taskExpectedPulls -or $taskCapture -ne [int](-not $taskNoInput) -or $taskTaps -ne [int](-not $taskNoInput) -or $taskHome -ne [int]$taskExpected){throw "Input/capture/HOME replayed or reordered: $taskMode"}
    if(@($taskLines|Where-Object {$_ -notmatch '^-s emulator-5994 (get-state|shell input tap 100 200|shell input keyevent 3|shell screencap -p /sdcard/gatefall-qa.png|pull /sdcard/gatefall-qa.png .+)$'}).Count){throw 'Unsafe/fallback command'}
    $taskEvidence=Join-Path $taskCase Docs/QA/v050
    $taskPartials=@(Get-ChildItem -LiteralPath $taskEvidence -Filter 'once-pull-failed-*.png' -ErrorAction SilentlyContinue)
    $taskExpectedPartials=switch($taskMode){'fails-once'{1};'all-fail'{3};default{0}}
    if($taskPartials.Count -ne $taskExpectedPartials){throw 'Partial screenshot evidence lost'}
    if($taskExpected -and [IO.File]::ReadAllText((Join-Path $taskEvidence once.png)) -ne 'same-original-capture'){throw 'Final capture changed'}
    if($taskHome -eq 1 -and $taskLines[-1] -notmatch 'keyevent 3$'){throw 'HOME order changed'}
    [pscustomobject]@{mode=$taskMode;success=$taskOk;pulls=$taskPulls;captures=$taskCapture;taps=$taskTaps;homes=$taskHome;partialsPreserved=$taskPartials.Count;error=$taskError;fixture=$taskCase}
})
[ordered]@{created=(Get-Date -Format o);task=229;mockCases=7;rows=$taskRows;realAdbInvoked=$false;productChanged=$false;newApk=$false;passed=$true}|ConvertTo-Json -Depth 6|Set-Content TestResults/qa-screenshot-transfer-v0210.json
'PASS: seven mock transfer cases; same captured file only; no tap/capture replay; partials retained; all-fail propagates; explicit serial/protected guard and HOME order preserved'
