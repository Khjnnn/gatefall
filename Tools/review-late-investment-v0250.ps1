param([ValidateSet('baseline','front','escort','asymmetric','pressure','selected')][string]$Variant='baseline',[int[]]$Seeds=@(7,41),[string[]]$Policies=@('spears'),[int[]]$Stages=@(11,12),[int[]]$Caps=@(7,16),[string]$Tag='pilot',[switch]$Crisis)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
if($Tag -notmatch '^[a-zA-Z0-9_-]+$'){throw 'Plain evidence tag required'}
$root="TestResults/v0250-$Variant-$Tag"
if(Test-Path $root){throw 'Preserve previous candidate evidence'}
New-Item -ItemType Directory -Path $root|Out-Null
foreach($file in Get-ChildItem TestResults/v0250-baseline/Assets/Scripts/Core -Filter '*.cs'){Copy-Item $file.FullName (Join-Path $root $file.Name)}
if($Variant -ne 'baseline'){
    $path=Join-Path $root BattlefieldLayouts.cs;$text=[IO.File]::ReadAllText($path)
    $before='float[] portions = { .72f, .43f, .88f, .2f, .57f, .32f, .8f };'
    # Original ten sites remain fixed; extra investments reach earlier parts of each road.
    $text=$text.Replace($before,$before+"`n            float[] forwardPortions = { .16f, .34f, .50f, .65f, .82f };")
    $text=$text.Replace('portions[(n / lanes.Length) % portions.Length]','((stage==10 || stage==11) && n>=OriginalSocketCount(stage) ? forwardPortions[((n-OriginalSocketCount(stage))/lanes.Length)%forwardPortions.Length] : portions[(n / lanes.Length) % portions.Length])')
    if($Variant -in @('asymmetric','pressure','selected')){
        $text=$text.Replace('L("marker-twin-u-courts",3,','L("marker-offset-training-courts",3,').Replace('P(23,4,16,4,16,-4,7,-4,7,2,2,2,0,0))','P(23,-1,16,-1,16,-6,7,-6,7,1,0,1,0,0))')
    }
    [IO.File]::WriteAllText($path,$text)
}
if($Variant -in @('escort','asymmetric','pressure','selected')){
    $path=Join-Path $root DefenseSimulation.cs;$text=[IO.File]::ReadAllText($path)
    $needle='if(g.Kind==EnemyKind.Boss) { hp=Rules!=null && Rules.Crisis ? 1100 : 800; speed=.65f; }'
    if(([regex]::Matches($text,[regex]::Escape($needle))).Count -ne 1){throw 'Unique spawn insertion missing'}
    $text=$text.Replace($needle,'if(Rules!=null && Rules.Stage!=null && !Rules.Crisis && (Rules.Stage.Index==10 || Rules.Stage.Index==11) && g.Wave>=3 && (g.Kind==EnemyKind.Brute || g.Kind==EnemyKind.Siege))speed*=1.6f;'+"`n            "+$needle)
    if($Variant -in @('pressure','selected')){$text=$text.Replace('))speed*=1.6f;', ')){speed*=1.6f;hp*=1.6f;}')}
    if($Variant -eq 'selected'){$text=$text.Replace('hp*=1.6f;', 'hp*=Rules.Stage.Index==10 ? 1.35f : 1.6f;')}
    [IO.File]::WriteAllText($path,$text)
}
Add-Type -Path @((Get-ChildItem $root -Filter '*.cs').FullName+(Join-Path (Get-Location) 'Tests/LateInvestmentReview.cs'))
$rows=@(foreach($stage in $Stages){foreach($seed in $Seeds){foreach($cap in $Caps){foreach($policy in $Policies){[Gatefall.Tests.LateInvestmentReview]::RunMode($stage,$seed,$cap,$policy,[bool]$Crisis)}}}})
[ordered]@{created=(Get-Date -Format o);variant=$Variant;crisis=[bool]$Crisis;newModels=$rows.Count;productChanged=$false;noSkills=$true;cardPolicy='First offered, actual chosen cards logged; compare before asserting fixed-card parity';originalTenSitesPreservedExceptAsymmetricStage11=($Variant -notin @('asymmetric','pressure','selected'));rows=$rows}|ConvertTo-Json -Depth 9|Set-Content ($root+'.json')
$rows|Select-Object Stage,Seed,Cap,Policy,State,Core,@{n='Time';e={[math]::Round($_.Time,2)}},Spent,Fired,Frequent,RecentPeak|Format-Table -AutoSize
