function Receive-QaScreenshot {
    param(
        [Parameter(Mandatory=$true)][string]$Serial,
        [Parameter(Mandatory=$true)][string]$Adb,
        [Parameter(Mandatory=$true)][string]$Destination
    )
    if($Serial -in @('emulator-5556','emulator-5558') -or $Serial -notmatch '^emulator-[0-9]+$') { throw 'Explicit non-protected QA emulator required' }
    # Retry only the already captured remote file. Never replay a tap or take another screenshot.
    for($taskTransferAttempt=1;$taskTransferAttempt -le 3;$taskTransferAttempt++) {
        & $Adb -s $Serial pull /sdcard/gatefall-qa.png $Destination
        if($LASTEXITCODE -eq 0) { return }
        if(Test-Path -LiteralPath $Destination) {
            $taskPartial=Join-Path (Split-Path -Parent $Destination) (
                [IO.Path]::GetFileNameWithoutExtension($Destination)+'-pull-failed-'+[Guid]::NewGuid().ToString('N')+[IO.Path]::GetExtension($Destination))
            Move-Item -LiteralPath $Destination -Destination $taskPartial -ErrorAction Stop
            Write-Warning ('Failed screenshot transfer preserved: '+$taskPartial)
        }
        if($taskTransferAttempt -lt 3) { Start-Sleep -Milliseconds 200 }
    }
    throw 'ADB screenshot pull failed after3 transfers of the same capture. Partial files retained; no input or capture was replayed. Inspect the current screen before further input.'
}
