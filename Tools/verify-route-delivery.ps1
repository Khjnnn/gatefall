$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskApk=Get-Content TestResults/android-apk-v0170.json -Raw|ConvertFrom-Json
foreach($taskFile in @('Builds/Android/Gatefall-v0.17.0.apk','Builds/Android/Gatefall.apk','TestResults/v0170-installed.apk')) {
    if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskApk.sha256){throw "APK differs: $taskFile"}
}
foreach($taskRow in (Get-Content TestResults/source-hashes-v0170.json -Raw|ConvertFrom-Json)) {
    if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw "Source changed: $($taskRow.path)"}
}
$taskLines=Get-Content TestResults/android-v0170-final-runtime.log
$taskRows=@(foreach($taskLine in $taskLines){
    if($taskLine -match 'GATEFALL PERF.*enemies=(\d+).*fxPeak=(\d+).*swarm=(\d+).*detailed=(\d+).*recycled=(\d+)') {
        [pscustomobject]@{enemies=[int]$Matches[1];peak=[int]$Matches[2];swarm=[int]$Matches[3];detailed=[int]$Matches[4];recycled=[int]$Matches[5]}
    }
})
if($taskRows.Count -eq 0 -or @($taskRows|Where-Object {$_.enemies -ne $_.swarm+$_.detailed}).Count){throw 'View count mismatch'}
$taskEffects=[pscustomobject]@{windows=$taskRows.Count;fxPeak=($taskRows|Measure-Object peak -Maximum).Maximum;cap=220;recycled=($taskRows|Measure-Object recycled -Maximum).Maximum;renderCountsMatch=$true;scope='Window end counts; not true enemy peak/GPU draw calls'}
$taskEffects|ConvertTo-Json|Set-Content TestResults/android-v0170-effects.json
$taskErrors=@($taskLines|Where-Object {$_ -match '\s7344\s+\d+\s+[EF]\s' -or $_ -match 'ANR in com.gatefall.prototype'})
$taskFirstPerf=($taskLines|Where-Object {$_ -match 'GATEFALL PERF'}|Select-Object -First 1).Substring(0,18)
$taskStartup=@($taskErrors|Where-Object { $_.Substring(0,18) -lt $taskFirstPerf -and ($_ -match 'ClassNotFoundException:.*com.google.android.play.core.assetpacks.AssetPackManager' -or $_ -match 'E EGL_emulation: (eglCreateContext: EGL_BAD_CONFIG: no ES 3.2 support|tid 7362: eglCreateContext\(1830\): error 0x3005|\[getAttribValue\] Bad attribute idx|tid 7362: eglGetConfigAttrib\(1249\): error 0x3004)') })
$taskUnexpected=@($taskErrors|Where-Object {$_ -notin $taskStartup})
if(-not @($taskLines|Where-Object {$_ -match 'Creating OpenGL ES 3.1 graphics device'}).Count){throw 'Graphics fallback not confirmed'}
[pscustomobject]@{pid=7344;rawErrorLines=$taskErrors.Count;startupDiagnostics=$taskStartup.Count;unexpectedErrorLines=$taskUnexpected.Count;lines=$taskErrors;graphicsFallback='ES3.2 context negotiation failed; ES3.1 created successfully';assetPackClassLookup='One startup ClassNotFoundException; run completed with local APK assets, cause and physical-device impact unverified';scope='All final app PID E/F and package ANR lines; startup diagnostics explicitly retained, no blanket exclusion'}|ConvertTo-Json -Depth 3|Set-Content TestResults/android-v0170-errors.json
if($taskUnexpected.Count -gt 0){throw 'Unexpected scoped Android errors present'}
$taskProfile=Get-Content TestResults/v0170-stage10-profile-summary.json -Raw|ConvertFrom-Json
if(-not $taskProfile.valid -or $taskProfile.reward -ne 28 -or $taskProfile.stageStars -ne 3 -or $taskProfile.pending -or -not $taskProfile.markerRecipeUnlocked -or -not $taskProfile.nextStageOpen){throw 'Saved outcome differs'}
$taskRun=Get-Content TestResults/v0170-stage10-last-run.txt -Raw
if($taskRun -notmatch 'Won elapsed=185.50 core=300 hunter=100 kills=864' -or $taskRun -notmatch 'allSpawned=True spawned=864 remainingEnemies=0' -or $taskRun -notmatch 'Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2'){throw 'Run outcome differs'}
$taskBuilds=[regex]::Matches($taskRun,'(?m)^[\d.]+ build ')
$taskFusions=[regex]::Matches($taskRun,'(?m)^[\d.]+ fusion ')
if($taskBuilds.Count-$taskFusions.Count -ne 16){throw 'Tower count differs'}
foreach($taskSave in @('v0170-update-save.json','v0170-final-update-save.json')){if(-not (Get-Content "TestResults/$taskSave" -Raw|ConvertFrom-Json).equal){throw 'Update save mismatch'}}
$taskCleanup=Get-Content TestResults/qa-cleanup-v0170.json -Raw|ConvertFrom-Json
if(-not $taskCleanup.directoryAbsent -or $taskCleanup.priorityRestored -ne 'Normal' -or -not $taskCleanup.protectedDevicesReceivedNoInput){throw 'Cleanup incomplete'}
[pscustomobject]@{created=(Get-Date).ToString('o');version=$taskApk.version;sha256=$taskApk.sha256;apkAndSourceConsistency=$true;updateSaveEqual=$true;stage10Towers=16;stage10Kills=864;stage10Elapsed=185.5;core=300;stars=3;rewardSaved=28;effects=$taskEffects;unexpectedErrors=$taskUnexpected.Count;retainedStartupDiagnostics=$taskStartup.Count;ownedAvdCleaned=$true;humanPlaytested=$false;physicalDeviceTested=$false;iosTested=$false}|ConvertTo-Json -Depth 5|Set-Content TestResults/delivery-consistency-v0170.json
Write-Output 'PASS final APK/source/installed copy, both update saves, 16 native towers/864 clear/reward unlock, effects/counts/errors and owned AVD cleanup.'
