# Regression for the observed four-tile profile/three-tile projection mismatch.
# Generates independent legacy snapshots; no ignored fixtures, ADB or user-save writes.
param([string]$ProfilePath='')
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$out=Join-Path $project 'TestResults/qa-profile-projection'
New-Item -ItemType Directory -Path $out -Force|Out-Null
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
public static class QaLegacyProfile {
    static void Progress(BinaryWriter w,int stage) {
        bool clear=stage<13;w.Write(clear ? 3 : 0);w.Write(0);w.Write(clear ? 0f : 1f);w.Write(clear);
    }
    // Thirteen clears unlock Marker (1-10), but leave Observer (1-14) locked; four equipped families.
    public static byte[] Create(int version) {
        byte[] payload;using(var m=new MemoryStream()) {
            using(var w=new BinaryWriter(m,Encoding.UTF8,true)) {
                w.Write("GATEFALL-RAON");w.Write(version);w.Write(1394);w.Write(0);w.Write(0);w.Write(0);w.Write(false);
                if(version==2)for(int i=0;i<6;i++)Progress(w,i);
                for(int i=0;i<7;i++)w.Write(0);
                if(version==3)w.Write(263);
                w.Write("");if(version==2)w.Write(-1);else w.Write("");w.Write(false);w.Write("");w.Write(0);
                if(version==2){for(int i=6;i<14;i++)Progress(w,i);w.Write(263);}
                else {w.Write(14);for(int i=0;i<14;i++){w.Write("C01-S"+(i+1).ToString("000"));Progress(w,i);}w.Write(0);w.Write(0);}
            }payload=m.ToArray();
        }
        using(var sha=SHA256.Create()) {var all=new byte[payload.Length+32];Buffer.BlockCopy(sha.ComputeHash(payload),0,all,0,32);
            Buffer.BlockCopy(payload,0,all,32,payload.Length);return all;}
    }
}
'@
$sources=@()
if($ProfilePath){$sources+= (Resolve-Path -LiteralPath $ProfilePath).Path}
else {foreach($version in @(2,3)){
    $synthetic=Join-Path $out "synthetic-v$version.sav"
    [IO.File]::WriteAllBytes($synthetic,[QaLegacyProfile]::Create($version));$sources+=$synthetic
}}
$cases=@()
foreach($source in $sources){
$hash=(Get-FileHash -LiteralPath $source).Hash
foreach($screen in @(@{w=2400;h=1080;safe=929},@{w=1920;h=1080;safe=959},@{w=1280;h=720;safe=639})){
    $tag='qa-profile-'+$screen.w
    & ./Tools/export-qa-map-actions.ps1 -Stage 14 -Tag ($tag+'-manual') -Width $screen.w -Height $screen.h -SafeLeft 0 -SafeWidth $screen.w -SafeHeight $screen.safe -Loadout 263 -MarkerUnlocked|Out-Null
    & ./Tools/export-qa-map-actions.ps1 -Stage 14 -Tag ($tag+'-derived') -Width $screen.w -Height $screen.h -SafeLeft 0 -SafeWidth $screen.w -SafeHeight $screen.safe -ProfilePath $source|Out-Null
    $a=[IO.File]::ReadAllText((Join-Path $project ('TestResults/'+$tag+'-manual-map-actions.csv')))
    $b=[IO.File]::ReadAllText((Join-Path $project ('TestResults/'+$tag+'-derived-map-actions.csv')))
    $projection=Get-Content ('TestResults/'+$tag+'-derived-projection.json') -Raw|ConvertFrom-Json
    if($a -cne $b -or $projection.menuTiles.build -ne 4 -or -not $projection.profile.markerUnlocked -or $projection.profile.observerUnlocked -or $projection.sourceProfile.sha256 -cne $hash.ToLowerInvariant()){throw 'Derived profile differs from confirmed four-tile fixture'}
    $cases+=@{snapshot=[IO.Path]::GetFileName($source);width=$screen.w;same24Coordinates=$true;buildTiles=4}
}
$conflict=$false
try{& ./Tools/export-qa-map-actions.ps1 -Stage 14 -Tag qa-profile-conflict -ProfilePath $source -Loadout 135|Out-Null}catch{$conflict=$_.Exception.Message -match 'do not mix manual profile flags'}
if(-not $conflict){throw 'Conflicting manual profile override accepted'}
if((Get-FileHash -LiteralPath $source).Hash -cne $hash){throw 'Read-only profile projection changed snapshot'}
}
$invalid=Join-Path $project 'TestResults/qa-profile-invalid.sav'
[IO.File]::WriteAllBytes($invalid,[byte[]]@(0,1,2,3))
$rejected=$false
try{& ./Tools/export-qa-map-actions.ps1 -Stage 14 -Tag qa-profile-invalid -ProfilePath $invalid|Out-Null}catch{$rejected=$true}
if(-not $rejected -or (Test-Path TestResults/qa-profile-invalid-projection.json)){throw 'Invalid snapshot produced coordinates'}
[ordered]@{screens=$cases;conflictingFlagsRejected=$conflict;invalidSnapshotRejected=$rejected;inputSnapshotUnchanged=$true;nativeBasis='Observed2400four-tile1-14menu;1920/1280coordinate equality is a projection check,not new native menu clicks';scope='QA tool only; no product,APK,AVD or gameplay changes'}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $out 'result.json')
"PASS $($sources.Count) legacy snapshots, three screens/72 coordinate rows each, ambiguous flags and corrupt snapshots rejected, input saves unchanged"
