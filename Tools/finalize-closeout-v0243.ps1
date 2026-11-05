$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$deadline=[datetimeoffset]'2026-11-01T08:00:00+09:00'
if([datetimeoffset]::Now -lt $deadline){throw 'Do not close or claim automation stop before08:00KST'}
$stop=Get-Content TestResults/automation-stop-v0243.json -Raw|ConvertFrom-Json
if($stop.automation5 -ne 'PAUSED' -or $stop.automation11 -ne 'PAUSED' -or -not $stop.toolResponsesVerified){throw 'Both actual automation responses required'}
foreach($id in @('automation-5','11')){
    $text=Get-Content ('C:/Users/Pro/.codex/automations/'+$id+'/automation.toml') -Raw
    if($text -notmatch '(?m)^status = "PAUSED"\s*$'){throw ('Saved automation is not PAUSED: '+$id)}
}
$r=Get-Content TestResults/closeout-v0243.json -Raw|ConvertFrom-Json
if(-not $r.passed -or $r.version -ne '0.24.3'){throw 'Prepared closure evidence missing'}
foreach($file in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.24.3.apk','TestResults/v0243-installed.apk','TestResults/v0243-double-installed.apk')){if((Get-FileHash $file).Hash -ne $r.sha256){throw 'Delivery changed before closure'}}
foreach($file in (Get-Content TestResults/source-hashes-v0243.json -Raw|ConvertFrom-Json)){if((Get-FileHash $file.path).Hash -ne $file.sha256){throw 'Product changed before closure'}}
$census=Get-Content TestResults/qa-owned-process-census-v0243.json -Raw|ConvertFrom-Json
if($census.runningOwnedEmulators -ne 0 -or $census.runningGatefallBatchBuilds -ne 0 -or ([datetimeoffset]::Now-[datetimeoffset]$census.created).TotalMinutes -gt 5){throw 'Need a fresh read-only activity census'}
$stamp=Get-Date -Format o
$current=Get-Content HANDOFF_CURRENT.md -Raw
$current=$current.Replace('# Gatefall 현재 인수인계 — 2026-11-01 07:00 KST 준비본','# Gatefall 현재 인수인계 — 2026-11-01 08:00 KST 마감')
$current=$current.Replace('상태: 제품 변경은 끝났고 문서·검증을 마감 정리 중이다. **아직08시 최종 보고나 자동화 중지 완료는 아니다.** 사용자 새 지시가 우선이다. 07:30부터 큰 변경·새AVD를 시작하지 않고,08시 이후 신규 개발 없이 결과를 보고하며 automation-5와 마감11 모두PAUSED인지 확인한다.','상태:08시 마감 검증과 automation-5·마감11 모두PAUSED 확인을 완료했다. 새 개발·빌드·AVD를 시작하지 않는다. 최종보고 내용은 Docs/QA/GATEFALL_CLOSEOUT_2026-11-01.md와 closeout-v0243.json을 따른다. 사용자 새 재개 지시가 있으면 그 지시를 우선한다.')
[IO.File]::WriteAllText((Join-Path (Get-Location) 'HANDOFF_CURRENT.md'),$current)
$doc=Get-Content Docs/QA/GATEFALL_CLOSEOUT_2026-11-01.md -Raw
$doc=$doc.Replace('상태: **준비본.08시 최종보고와 반복 중지는 아직 완료하지 않았다.** 실제 중지 결과는 `TestResults/automation-stop-v0243.json`과 최종 `closeout-v0243.json`을 확인한다. 마감 전에는 제품을 더 바꾸거나 새APK/AVD를 시작하지 않는다.','상태:08시 마감 검증 및 automation-5·11 모두PAUSED 확인 완료. 실제 중지 결과는 `TestResults/automation-stop-v0243.json`과 `closeout-v0243.json`에 기록했다. 신규 개발·APK·AVD를 시작하지 않으며 이 내용으로 최종 응답한다.')
[IO.File]::WriteAllText((Join-Path (Get-Location) 'Docs/QA/GATEFALL_CLOSEOUT_2026-11-01.md'),$doc)
$r.status='closed-verification-and-automations-paused'
$r.automationStopConfirmed=$true
$r.ownedCensus=$census
$r|Add-Member -NotePropertyName closedAt -NotePropertyValue $stamp -Force
$r|Add-Member -NotePropertyName finalReportReady -NotePropertyValue $true -Force
$r.PSObject.Properties.Remove('finalReportSent')
$r|Add-Member -NotePropertyName reportState -NotePropertyValue 'Final-channel response follows this verification; use the conversation to confirm delivered text, not a client delivery assertion.' -Force
$r|Add-Member -NotePropertyName automationStopRecord -NotePropertyValue ([ordered]@{path='TestResults/automation-stop-v0243.json';sha256=(Get-FileHash TestResults/automation-stop-v0243.json).Hash.ToLowerInvariant()}) -Force
foreach($entry in $r.evidence){$entry.sha256=(Get-FileHash $entry.path).Hash.ToLowerInvariant();$entry.bytes=(Get-Item $entry.path).Length}
$r|ConvertTo-Json -Depth 14|Set-Content TestResults/closeout-v0243.json
Add-Content task_plan.md "`n263.08시최종마감검증·automation-5와11모두PAUSED실응답/저장상태확인·현재APK/23제품/소유활동0재확인/미완료분리 [완료:$stamp;최종응답에서보고]"
Add-Content progress.md ($stamp+' 263마감검증/반복중지완료:0.24.3/74/3799e52f…97406753·두자동화PAUSED실응답및저장상태확인·제품23/4APK해시같음·소유AVD/배치빌드0/보호미조작.사람·실기기/iOS/청감·진단근본/1-11지형·투자효용·후반영상/세번째카드미완료분리.새제품/빌드/AVD/전투없음·최종응답준비.')
'CLOSED:deadline reached;actual automation-5/11 PAUSED;verified APK/source/evidence and fresh owned-process0. Final response ready.'
