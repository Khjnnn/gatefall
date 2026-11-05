param([switch]$UseRecorded,[string]$EditorPath='C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root=Join-Path $project 'TestResults/night-balance-20261105'
$output=Join-Path $root 'startup-route-v0284.json'
if(-not $UseRecorded){
    if(Test-Path $output){throw 'Use -UseRecorded for the completed audit'}
    $apk=(Resolve-Path 'Builds/Android/Gatefall-v0.28.4.apk').Path
    $apkHash=(Get-FileHash $apk).Hash.ToLowerInvariant()
    if($apkHash -cne '12d801bffb65c7c65168499e7e112d7b2faf6352152074a567baec6008b32806'){throw 'Expected preserved0.28.4 APK'}
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Text;
public static class GatefallQaDex {
    public static string[] Classes(byte[] data) {
        if(data.Length<112 || Encoding.ASCII.GetString(data,0,4)!="dex\n")throw new Exception("Invalid dex");
        uint strings=BitConverter.ToUInt32(data,60),types=BitConverter.ToUInt32(data,68),count=BitConverter.ToUInt32(data,96),defs=BitConverter.ToUInt32(data,100);
        var names=new List<string>();
        for(uint i=0;i<count;i++) {
            uint type=BitConverter.ToUInt32(data,checked((int)(defs+32*i)));
            uint str=BitConverter.ToUInt32(data,checked((int)(types+4*type)));
            int pos=checked((int)BitConverter.ToUInt32(data,checked((int)(strings+4*str))));
            int prefix=0;while((data[pos++]&128)!=0)if(++prefix>4)throw new Exception("Invalid uleb128");
            int end=pos;while(data[end]!=0)end++;
            names.Add(Encoding.UTF8.GetString(data,pos,end-pos));
        }
        return names.ToArray();
    }
}
'@
    $zip=[IO.Compression.ZipFile]::OpenRead($apk)
    try{
        $classes=[Collections.Generic.List[string]]::new();$dexEntries=@()
        foreach($entry in $zip.Entries|Where-Object FullName -Match '^classes\d*\.dex$'){
            $stream=$entry.Open();$memory=[IO.MemoryStream]::new()
            try{$stream.CopyTo($memory);$names=[GatefallQaDex]::Classes($memory.ToArray());$classes.AddRange([string[]]$names);$dexEntries+=[ordered]@{file=$entry.FullName;definedClasses=$names.Count}}finally{$memory.Dispose();$stream.Dispose()}
        }
        $embeddedAssets=@($zip.Entries|Where-Object FullName -Like 'assets/bin/Data/*').Count
    }finally{$zip.Dispose()}
    $wrapper='Lcom/unity3d/player/PlayAssetDeliveryUnityWrapper;';$manager='Lcom/google/android/play/core/assetpacks/AssetPackManager;'
    if(-not $classes.Contains($wrapper) -or $classes.Contains($manager)){throw 'Unexpected current APK class availability'}
    $jar=(Resolve-Path 'Library/Bee/Android/Prj/IL2CPP/Gradle/unityLibrary/libs/unity-classes.jar').Path
    $javaBin=Join-Path (Split-Path $EditorPath) 'Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin'
    $dump=Join-Path $root 'pad-wrapper-javap-v0284.txt'
    & (Join-Path $javaBin 'javap.exe') -classpath $jar -p -c com.unity3d.player.PlayAssetDeliveryUnityWrapper|Set-Content $dump -Encoding utf8
    if($LASTEXITCODE -ne 0){throw 'Local wrapper disassembly failed'}
    $bytecode=Get-Content $dump -Raw
    foreach($text in @('Class java/lang/ClassNotFoundException','loadClass:','logError:','playCoreApiMissing','AssetPackManager API is not available!')){if(-not $bytecode.Contains($text)){throw ('Missing expected wrapper evidence '+$text)}}
    $logPath=Join-Path $root 'logcat-v0284-final-dump.txt'
    $pidLines=@(Get-Content $logPath|Where-Object{$_ -match '^\S+\s+\S+\s+4980\s+'})
    $asset=@($pidLines|Where-Object{$_ -match 'E Unity\s+: java.lang.ClassNotFoundException:.*com.google.android.play.core.assetpacks.AssetPackManager'})
    $no32=@($pidLines|Where-Object{$_ -match 'ES3.2 not supported'})
    $created31=@($pidLines|Where-Object{$_ -match 'Creating OpenGL ES 3.1 graphics device'})
    $perf=@($pidLines|Where-Object{$_ -match 'GATEFALL PERF'})
    if($asset.Count -ne 1 -or $no32.Count -lt 1 -or $created31.Count -ne 1 -or $perf.Count -lt 1){throw 'Preserved startup sequence differs'}
    $refs=@(Get-ChildItem Assets/Scripts -Recurse -File -Filter '*.cs'|Select-String -Pattern 'AndroidAssetPacks|AssetPackManager|PlayAssetDelivery|Google\.Play\.AssetDelivery')
    [ordered]@{apkSha256=$apkHash;dex=$dexEntries;wrapperDefined=$true;assetPackManagerDefined=$false;embeddedUnityAssetEntries=$embeddedAssets;applicationPadReferences=$refs.Count;
        localJar=$jar;localJarSha256=(Get-FileHash $jar).Hash.ToLowerInvariant();localWrapperDump=Split-Path $dump -Leaf;wrapperDumpSha256=(Get-FileHash $dump).Hash.ToLowerInvariant();
        retainedPid=4980;assetError=$asset;es32Unsupported=$no32;es31Created=$created31;retainedPerfCount=$perf.Count;nativeRunRepeated=$false;
        conclusion='The local Unity wrapper probes AssetPackManager by ClassLoader.loadClass, catches ClassNotFoundException and logs toString before returning; its unavailable API guard would throw if asset-pack methods were called. The APK defines the wrapper but not the Google manager and embeds Unity data. This supports the probe/missing optional dependency explanation for the observed one-line startup log, not a new runtime trace proving the native caller. The same retained PID explicitly fails the ES3.2 probe then creates ES3.1 and reaches application PERF. No module, settings, logging filter or APK change was made.';
        limitations='No new Android/physical device test. PAD/AAB/downloaded-pack operation is not verified and cannot be declared usable while the API is missing. No blanket harmlessness, all-errors-zero, root cause for Surface messages, performance gain or physical-device fix. Local Java bytecode is from the current generated0.28.4 Gradle project, not a complete DEX control-flow equivalence proof.'
    }|ConvertTo-Json -Depth 8|Set-Content $output -Encoding utf8
}
$r=Get-Content $output -Raw|ConvertFrom-Json
$r|Select-Object apkSha256,wrapperDefined,assetPackManagerDefined,embeddedUnityAssetEntries,applicationPadReferences,retainedPid,retainedPerfCount,nativeRunRepeated|Format-List
Write-Output 'PASS preserved startup-route audit; no product change, no new native run.'
