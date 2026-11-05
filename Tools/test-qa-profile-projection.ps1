# Regression for the observed four-tile profile/three-tile projection mismatch.
# Reads a previously extracted synthetic snapshot; no ADB, app input or user-save writes.
param([string]$ProfilePath='TestResults/night-balance-20261105/update-before-v0288.sav')
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$source=(Resolve-Path -LiteralPath $ProfilePath).Path
$hash=(Get-FileHash -LiteralPath $source).Hash
$cases=@()
foreach($screen in @(@{w=2400;h=1080;safe=929},@{w=1920;h=1080;safe=959},@{w=1280;h=720;safe=639})){
    $tag='qa-profile-'+$screen.w
    & ./Tools/export-qa-map-actions.ps1 -Stage 14 -Tag ($tag+'-manual') -Width $screen.w -Height $screen.h -SafeLeft 0 -SafeWidth $screen.w -SafeHeight $screen.safe -Loadout 263 -MarkerUnlocked|Out-Null
    & ./Tools/export-qa-map-actions.ps1 -Stage 14 -Tag ($tag+'-derived') -Width $screen.w -Height $screen.h -SafeLeft 0 -SafeWidth $screen.w -SafeHeight $screen.safe -ProfilePath $source|Out-Null
    $a=[IO.File]::ReadAllText((Join-Path $project ('TestResults/'+$tag+'-manual-map-actions.csv')))
    $b=[IO.File]::ReadAllText((Join-Path $project ('TestResults/'+$tag+'-derived-map-actions.csv')))
    $projection=Get-Content ('TestResults/'+$tag+'-derived-projection.json') -Raw|ConvertFrom-Json
    if($a -cne $b -or $projection.menuTiles.build -ne 4 -or -not $projection.profile.markerUnlocked -or $projection.profile.observerUnlocked -or $projection.sourceProfile.sha256 -cne $hash.ToLowerInvariant()){throw 'Derived profile differs from confirmed four-tile fixture'}
    $cases+=@{width=$screen.w;same24Coordinates=$true;buildTiles=4}
}
$conflict=$false
try{& ./Tools/export-qa-map-actions.ps1 -Stage 14 -Tag qa-profile-conflict -ProfilePath $source -Loadout 135|Out-Null}catch{$conflict=$_.Exception.Message -match 'do not mix manual profile flags'}
if(-not $conflict){throw 'Conflicting manual profile override accepted'}
$invalid=Join-Path $project 'TestResults/qa-profile-invalid.sav'
[IO.File]::WriteAllBytes($invalid,[byte[]]@(0,1,2,3))
$rejected=$false
try{& ./Tools/export-qa-map-actions.ps1 -Stage 14 -Tag qa-profile-invalid -ProfilePath $invalid|Out-Null}catch{$rejected=$true}
if(-not $rejected -or (Test-Path TestResults/qa-profile-invalid-projection.json)){throw 'Invalid snapshot produced coordinates'}
if((Get-FileHash -LiteralPath $source).Hash -cne $hash){throw 'Read-only profile projection changed snapshot'}
[ordered]@{screens=$cases;conflictingFlagsRejected=$conflict;invalidSnapshotRejected=$rejected;inputSnapshotUnchanged=$true;nativeBasis='Observed2400four-tile1-14menu;1920/1280coordinate equality is a projection check,not new native menu clicks';scope='QA tool only; no product,APK,AVD or gameplay changes'}|ConvertTo-Json -Depth 5|Set-Content TestResults/night-balance-20261105/qa-profile-projection-v0288.json
'PASS three screen profiles/72 coordinate rows, ambiguous flags and corrupt snapshots rejected, input save unchanged'
