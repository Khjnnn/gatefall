function Assert-QaAndroidReady {
    param([Parameter(Mandatory=$true)][string]$Serial,[Parameter(Mandatory=$true)][string]$Adb,[switch]$BootCompleted)
    # Read-only probes only: no reconnect/reset/kill-server and no fallback device selection.
    for($taskAttempt=0;$taskAttempt -lt 3;$taskAttempt++) {
        $taskState='';$taskExit=-1
        try {
            $taskOutput=@(& $Adb -s $Serial get-state 2>&1)
            $taskExit=$LASTEXITCODE
            $taskState=($taskOutput|Out-String).Trim()
        } catch { $taskState=$_.Exception.Message }
        if($taskExit -eq 0 -and $taskState -eq 'device'){
            if(-not $BootCompleted){return}
            try {
                $taskBoot=@(& $Adb -s $Serial shell getprop sys.boot_completed 2>&1)
                $taskBootExit=$LASTEXITCODE
                if($taskBootExit -eq 0 -and ($taskBoot|Out-String).Trim() -eq '1'){return}
            } catch { $taskState=$_.Exception.Message }
        }
        if($taskAttempt -lt 2){Start-Sleep -Milliseconds 500}
    }
    $taskRequirement=if($BootCompleted){'connected with Android boot completed'}else{'connected'}
    throw "QA device $Serial is not $taskRequirement after3 read-only probes. No input/extraction started. Inspect the owned process and current device state."
}
