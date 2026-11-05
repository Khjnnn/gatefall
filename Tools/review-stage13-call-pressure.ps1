param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskOut='TestResults/stage13-call-v0207-raw.json'
$taskBaseRows=@((Get-Content TestResults/stage13-marker-v0204-models.json -Raw|ConvertFrom-Json).rows|Where-Object MarkerSite -eq 14)
if($taskBaseRows.Count -ne 3){throw 'Three original baseline seeds required'}
if(-not $UseRecorded){
    if(Test-Path $taskOut){throw 'Preserve prior run; use -UseRecorded'}
    foreach($taskR in (Get-Content TestResults/source-hashes-v0207.json -Raw|ConvertFrom-Json)){if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Current product differs'}}
    $taskOriginal=[IO.File]::ReadAllText('Tests/Stage13MarkerPositionReview.cs')
    $taskEdits=@(
        @('Stage13MarkerPositionReview','Stage13CallPressureReview'),
        @('        public sealed class Result {',@'
        public static string Calling="none";
        public sealed class CallMoment {public int Wave,Enemies,CreditsBefore,CreditsAfter;public double Time,Advance;public float Core;}
        public sealed class Result {
            public int WaveCalls,Supplies,Offers,CatalystsGranted,CatalystsRemaining;
            public double Advance;
            public string[] CallLog;
            public CallMoment[] CallMoments;
'@),
        @('            var audit=new StageThreatObservation(g.BoardSockets.Length);var chosen=new List<string>();',('            g.WaveCallsEnabled=true;var moments=new List<CallMoment>();'+"`n"+'            var audit=new StageThreatObservation(g.BoardSockets.Length);var chosen=new List<string>();')),
        @('                if(g.AwaitingCard) {',@'
                if(Calling!="none" && (Calling=="earliest" || g.Enemies.Count==0) && g.CanCallNextWave()=="") {
                    var moment=new CallMoment{Wave=g.NextCallWave,Time=g.Elapsed,Enemies=g.Enemies.Count,Core=g.CoreHealth,CreditsBefore=g.Credits,Advance=g.CallAdvanceSeconds};
                    if(!g.TryCallNextWave(g.NextCallWave,null,out why))throw new Exception(why);
                    moment.CreditsAfter=g.Credits;moments.Add(moment);
                }
                if(g.AwaitingCard) {
'@),
        @('MarkBonus=g.Telemetry.MarkBonusDamage,Observation=audit};','MarkBonus=g.Telemetry.MarkBonusDamage,Observation=audit,WaveCalls=g.Telemetry.WaveCalls,Advance=g.Telemetry.WaveAdvanceSeconds,Supplies=g.SuppliesGranted,Offers=g.Telemetry.CardOffers,CatalystsGranted=g.Telemetry.CoresGranted,CatalystsRemaining=g.Cores,CallLog=g.Telemetry.WaveCallLog.ToArray(),CallMoments=moments.ToArray()};')
    )
    $taskSource=$taskOriginal
    foreach($taskEdit in $taskEdits){if($taskEdit.Count -ne 2){throw 'Harness edit must be a pair'};if(-not $taskSource.Contains($taskEdit[0])){throw 'Harness anchor missing'};$taskSource=$taskSource.Replace($taskEdit[0],$taskEdit[1])}
    $taskInverse=$taskSource
    for($taskI=$taskEdits.Count-1;$taskI -ge 0;$taskI--){$taskInverse=$taskInverse.Replace($taskEdits[$taskI][1],$taskEdits[$taskI][0])}
    if($taskInverse -cne $taskOriginal){throw 'Unexplained harness change'}
    $taskHarness='TestResults/Stage13CallPressureReview-v0207.cs';[IO.File]::WriteAllText((Join-Path (Get-Location) $taskHarness),$taskSource)
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+(Join-Path (Get-Location) 'Tests/StageThreatObservation.cs')+(Join-Path (Get-Location) $taskHarness))
    $taskActions=@(foreach($taskR in (Get-Content TestResults/v0204-stage13-native-purchase-ledger.json -Raw|ConvertFrom-Json)){
        [Gatefall.Tests.Stage13CallPressureReview+Action]@{Time=$taskR.time;Command=$taskR.action;Kind=[Enum]::Parse([Gatefall.Core.TowerKind],$taskR.kind);Site=$taskR.site}
    })
    [Gatefall.Tests.Stage13CallPressureReview]::Calling='none'
    $taskControl=[Gatefall.Tests.Stage13CallPressureReview]::Run(19,14,$taskActions)
    $taskPrior=$taskBaseRows|Where-Object Seed -eq 19
    foreach($taskF in @('State','Core','Hunter','Time','Spent','Credits','Towers','Kills','Spawned','Remaining','CompletedActions','MarkedHits','MarkBonus','MaximumPurchaseDelay')){
        if($taskF -in @('Time','MarkBonus','MaximumPurchaseDelay')){if([Math]::Abs([double]$taskControl.$taskF-[double]$taskPrior.$taskF) -gt .0001){throw "Call-enabled control differs: $taskF"}}
        elseif($taskControl.$taskF -cne $taskPrior.$taskF){throw "Call-enabled control differs: $taskF"}
    }
    foreach($taskF in @('Cards','Purchases','KindDamage')){if(($taskControl.$taskF|ConvertTo-Json -Compress) -cne ($taskPrior.$taskF|ConvertTo-Json -Compress)){throw "Call-enabled control array differs: $taskF"}}
    if(($taskControl.Observation.SiteShots|ConvertTo-Json -Compress) -cne ($taskPrior.Observation.SiteShots|ConvertTo-Json -Compress)){throw 'Call-enabled control shots differ'}
    $taskRows=@(foreach($taskPolicy in @('earliest','clear')){foreach($taskSeed in @(7,19,41)){
        [Gatefall.Tests.Stage13CallPressureReview]::Calling=$taskPolicy
        $taskBattle=[Gatefall.Tests.Stage13CallPressureReview]::Run($taskSeed,14,$taskActions)
        if($taskBattle.Spent -gt 1050 -or $taskBattle.Spawned -gt 928 -or $taskBattle.Supplies -gt 5 -or $taskBattle.Offers -gt 3 -or $taskBattle.CatalystsGranted -gt 2 -or $taskBattle.WaveCalls -gt 5 -or $taskBattle.Advance -gt 30.001){throw 'Fixed caps exceeded'}
        [ordered]@{seed=$taskSeed;policy=$taskPolicy;battle=$taskBattle}
    }})
    [ordered]@{created=(Get-Date -Format o);task=215;version='0.20.7';newModels=7;baselineReused=3;controlSeed=19;controlMatchesOutcomeLedgerCardsDamageShots=$true;inverseHarnessMatchesOriginal=$true;control=$taskControl;rows=$taskRows;productChanged=$false;newAndroidRun=$false}|ConvertTo-Json -Depth 12|Set-Content $taskOut
}
$taskData=Get-Content $taskOut -Raw|ConvertFrom-Json
$taskRows=@(foreach($taskR in $taskData.rows){$taskB=$taskBaseRows|Where-Object Seed -eq $taskR.seed;$taskA=$taskR.battle
    [ordered]@{seed=$taskR.seed;policy=$taskR.policy;state=$taskA.State;core=$taskA.Core;baselineCore=$taskB.Core;time=$taskA.Time;baselineTime=$taskB.Time;spent=$taskA.Spent;baselineSpent=$taskB.Spent;towers=$taskA.Towers;baselineTowers=$taskB.Towers;completedActions=$taskA.CompletedActions;credits=$taskA.Credits;calls=$taskA.WaveCalls;advanced=$taskA.Advance;supplies=$taskA.Supplies;cards=$taskA.Offers;catalysts=$taskA.CatalystsGranted;peakAlive=$taskA.Observation.PeakAlive;baselinePeak=$taskB.Observation.PeakAlive;fired=$taskA.Observation.FiredSites;baselineFired=$taskB.Observation.FiredSites;purchaseDelay=$taskA.MaximumPurchaseDelay;sameCards=(($taskA.Cards|ConvertTo-Json -Compress) -ceq ($taskB.Cards|ConvertTo-Json -Compress));callMoments=$taskA.CallMoments}
})
[ordered]@{created=(Get-Date -Format o);task=215;source='stage13-call-v0207-raw.json';newModelsRecorded=7;modelsRerunForSummary=0;baselineReused=3;controlParity=$taskData.controlMatchesOutcomeLedgerCardsDamageShots;rows=$taskRows;limits='Fixed target purchase policy, not guaranteed same final purchases/time/budget after earlier victory; no human fun or native replay claim';productChanged=$false;newApk=$false;newAvd=$false}|ConvertTo-Json -Depth 9|Set-Content TestResults/stage13-call-v0207-comparison.json
$taskRows|ForEach-Object {[pscustomobject]$_}|Select-Object seed,policy,state,core,time,spent,towers,completedActions,calls,advanced,peakAlive,sameCards|Format-Table -AutoSize
