$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$apk=Join-Path (Get-Location) 'Builds/Android/Gatefall-v0.24.3.apk'
$sha=(Get-FileHash $apk).Hash.ToLowerInvariant()
if($sha -ne '3799e52f68104b58692898c8ddcf473576eb04af1bf95f8e4c164d36c6ad794c'){throw 'Audit is limited to the verified current APK'}
$manifest=Get-Content TestResults/v0243-manifest-inspection.txt -Raw
$cert=Get-Content TestResults/v0243-signing-cert-inspection.txt -Raw
$scope=Get-Content TestResults/v0243-playable-scope.json -Raw|ConvertFrom-Json
if($manifest -notmatch 'android:debuggable[^\r\n]*0xffffffff' -or $cert -notmatch 'CN=Android Debug' -or $scope.implementedNormalStages -ne 14){throw 'Expected test build/scope changed; review current evidence'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::OpenRead($apk)
try{
    $native=@(foreach($entry in $zip.Entries|Where-Object {$_.FullName -match '^lib/[^/]+/[^/]+\.so$'}){
        $stream=$entry.Open()
        try{
            $header=[byte[]]::new(64);$read=0
            while($read -lt 64){$n=$stream.Read($header,$read,64-$read);if($n -le 0){throw 'Truncated ELF'};$read+=$n}
            if($header[0] -ne 127 -or $header[1] -ne 69 -or $header[2] -ne 76 -or $header[3] -ne 70 -or $header[4] -ne 2 -or $header[5] -ne 1){throw ('Expected little-endian ELF64: '+$entry.FullName)}
            $phoff=[BitConverter]::ToUInt64($header,32)
            $entsize=[BitConverter]::ToUInt16($header,54)
            $count=[BitConverter]::ToUInt16($header,56)
            $limit=$phoff+[uint64]$entsize*$count
            if($phoff -lt 64 -or $entsize -lt 56 -or $limit -gt 1048576 -or $limit -gt $entry.Length){throw 'Unexpected ELF program-header bounds'}
            $bytes=[byte[]]::new([int]$limit);[Array]::Copy($header,$bytes,64);$read=64
            while($read -lt $bytes.Length){$n=$stream.Read($bytes,$read,$bytes.Length-$read);if($n -le 0){throw 'Truncated ELF table'};$read+=$n}
            $loads=@(for($i=0;$i -lt $count;$i++){
                $offset=[int]($phoff+$i*$entsize)
                if([BitConverter]::ToUInt32($bytes,$offset) -ne 1){continue}
                $fileOffset=[BitConverter]::ToUInt64($bytes,$offset+8)
                $virtual=[BitConverter]::ToUInt64($bytes,$offset+16)
                $alignment=[BitConverter]::ToUInt64($bytes,$offset+48)
                [ordered]@{index=$i;fileOffset=$fileOffset;virtualAddress=$virtual;alignment=$alignment;alignmentAtLeast16KB=($alignment -ge 16384);offsetVirtualCongruentAt16KB=(($fileOffset%16384) -eq ($virtual%16384))}
            })
            if($loads.Count -eq 0){throw 'No ELF LOAD segment'}
            [ordered]@{entry=$entry.FullName;abi=($entry.FullName -split '/')[1];bytes=$entry.Length;loadSegments=$loads;allLoadsAligned16KB=(@($loads|Where-Object {-not $_.alignmentAtLeast16KB -or -not $_.offsetVirtualCongruentAt16KB}).Count -eq 0)}
        }finally{$stream.Dispose()}
    })
}finally{$zip.Dispose()}
if($native.Count -eq 0 -or @($native|Where-Object abi -ne 'arm64-v8a').Count){throw 'Unexpected native library set'}
if((Get-FileHash $apk).Hash.ToLowerInvariant() -ne $sha){throw 'APK changed during read-only audit'}
$bad=@($native|Where-Object {-not $_.allLoadsAligned16KB})
[ordered]@{created=(Get-Date -Format o);task=261;version='0.24.3';code=74;sha256=$sha;readOnlyAudit=$true;apkDebuggable=$true;signer='Android Debug';testApkNotStoreRelease=$true;minSdk=28;targetSdk=36;nativeLibraries=$native;nativeElfAllLoadSegments16KB=($bad.Count -eq 0);zip16KBAlignment='verified separately in existing APK delivery';implementedNormalStages=14;chapterNamesDefined=7;planned240StagesImplemented=$false;newBuilds=0;newAvd=0;newNativeBattles=0;physicalDeviceTested=$false;iosTested=$false;storeReadinessClaim=$false;limits='ELF program headers and ZIP alignment do not establish physical-device, OS/vendor, rendering, input, heat, audio or store compliance. Release signing/App Bundle and user-owned publishing decisions remain outside this test APK.'}|ConvertTo-Json -Depth 9|Set-Content TestResults/android-readiness-v0243-review.json
if($bad.Count){'AUDIT: test APK; one or more native LOAD segment alignments below16KB. See report.'}else{"AUDIT: test APK/debug signer; $($native.Count) ARM64 ELF files/all LOAD16KB; implemented14 stages,7chapter names/240plan. No release/store/phone claim."}
