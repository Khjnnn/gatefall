# Gatefall 인수인계 — Claude / Codex CLI

작성일: 2026-10-26 · 현재 구현/배포 버전: **v0.4.0**

프로젝트 루트: `C:\Users\Pro\Documents\ChatGPT\react\gatefall-unity`

## 최신 사용자 방향 — 모바일 전용 디자인 개선

2026-10-26 후속 요청: **PC 제품 개발/배포를 중단하고 Android/iOS 모바일에 집중한다. 이번 실행 산출물은 Android APK다.** Unity Editor·자동 검사·Android 에뮬레이터를 PC에서 사용하는 것은 계속 가능하다. 아래 v0.4 PC 빌드/검증 내용은 과거 이력이며 새 작업의 완료 조건이 아니다.

최우선 문제는 임시 도형·버튼·설명문을 나열한 듯한 디자인과 복잡한 게임 구성이다. Realm과 랜덤다이스의 분석을 참고해 기능을 늘리지 않고 전장/아트/터치/UI/강화·조합의 완성도를 높인다. 라온 세계관과 핵심 전투를 유지하되 중복 메뉴·화면·조작은 통합하거나 단순화한다. 기존 기능을 모두 전면 노출하라는 요구로 해석하지 않는다.

Claude `/goal`에 전달할 상세 작업 범위와 완료 기준은 [GOAL_MOBILE_REDESIGN.txt](GOAL_MOBILE_REDESIGN.txt)에 저장했다. 아직 이 개선 작업을 구현하거나 APK를 새로 빌드한 것은 아니다.

랜덤다이스 참고 보고서: `C:\dev\rea_project\random_dice_work\reports\RANDOM_DICE_STATIC_ANALYSIS_2026-10-26.md`. 1차 정적 분석으로 소환/병합 관련 식별자는 확인됐으나 세부 수치·확률·게임 로직 본문은 미확인이다. 전체 소스가 복원됐다고 가정하지 않는다.

## 1. 지금 어디까지 왔는가

사용자는 Android/iOS용 현대판타지 디펜스를 개발 중이며 Android를 먼저 테스트한다. 캐릭터 이동·자동 공격·고정 지점 타워 건설·강화·조합을 함께 운용한다. 전체 방향은 **도시 구역을 차례로 해방하는 캠페인 + 매 전투 로그라이크 성장**이다. Realm Defense의 준비, 웨이브, 역할 분담, 영구 연구 구조를 분석해 참고했다.

현재 **종현동 첫 챕터 6작전**의 지도 → 브리핑 → 배치 준비 → 3분 전투 → 별점/보상 → 다음 작전 해금과 영구 저장을 구현했다. 공방 연구·환급, 각성자 선택·성장, 기록, 전체 기능을 열어 둔 전투 실험실도 있다. PC와 Android APK를 실제 빌드하고 실행했다. 나머지 6챕터는 설정/로드맵이며 플레이 가능한 콘텐츠가 아니다.

이 인수인계는 **프로젝트 안에 인수인계서를 남기고 Claude와 협업하며 Codex CLI로도 이어가기** 요청으로 작성했다. 이후 모바일 전용 디자인 개선 방향은 위 최신 절에 추가했다. 인수인계/프롬프트 작성 과정에서는 기능을 추가하거나 게임을 다시 빌드하지 않았다. Claude 세션을 대신 시작하거나 외부에 소스를 전송하지 않았다.

## 2. 먼저 읽을 파일과 기준

1. 이 문서와 [README.md](README.md): 현재 실행 가능한 범위와 조작.
2. [사용자가 제공한 세계관 원문](Docs/Design/RAON_WORLD_USER_REFERENCE.txt).
3. [라온 세계관](Docs/Design/GATEFALL_RAON_WORLD.md), [캠페인 기획](Docs/Design/GATEFALL_CAMPAIGN_FOUNDATION.md).
4. [실제 v0.4 실행 검증](TestResults/runtime-v040.md).
5. 작업에 관련된 C# 소스와 테스트.
6. `task_plan.md`, `progress.md`, `findings.md`의 최신 v0.4 절. 앞부분은 과거 작업 기록이므로 현재 상태와 구분한다.

`Docs/Design/campaign-foundation-v2.json`은 설계 입력 스냅샷이다. `status: design_not_runtime`은 남겨 두었으며 실제 구현 검사는 `Tools/verify-campaign-runtime.ps1`에서 한다. 오래된 v0.2/v0.3 문서, 외부 Realm 인수인계의 “구현 전” 표현, 이전 인물/도시는 현재 구현 기준이 아니다. 이후 변경이 있으면 이 문서의 버전/미완료 항목도 갱신한다.

## 3. 세계관과 구현 방향

- 배경은 **라온시·라온강**. 7년 전 빛 축제 중 일곱 검은 탑이 떨어졌다.
- 주인공은 미분류 귀환자 **강윤**. 백도하는 B급 방위대 지휘관, 하늬는 C급 정찰자, 유채린은 비전투 결계 기술자다.
- 금태혁/금관길드, 나은재/잔광교단 등 세부 설정과 반전 공개 순서는 세계관 문서를 따른다.
- 일곱 탑의 지역은 종현동 → 다솜동 → 라온역 → 항만 → 산업단지 → 마천루 → 시청이다.
- 장르 골격을 참고하고 Gatefall 규칙과 표현을 자체 구현한다. **Realm에서 추출한 코드·이미지·음원을 Gatefall 소스/배포물에 넣지 않았다.**
- 도시·헌터·마수 등은 이전에 자체 생성한 이미지다. 새 결계주·검은 탑·공명 연결·보스 공격 예고는 절차형 도형이다. 캐릭터별 완성 일러스트/애니메이션은 아직 없다. 생성 기록은 `Docs/Art/GENERATION.md`.

## 4. 현재 플레이 규칙

### 캠페인과 해금

| 작전 | 지점 / 진입 방향 | 예정 일반 적 수 | 첫 마핵 | 클리어 후 주요 해금 |
|---|---|---:|---:|---|
| 1-1 종현 사거리 | 5 / 서 | 45 | 10 | 봉인, 공방, 인접 공명 |
| 1-2 구도심 지하보도 | 6 / 서·동 | 65 | 12 | 뇌격, 촉매 |
| 1-3 종현 배급소 | 6 / 서·북 | 90 | 14 | 붕괴, 파쇄/중력, 하늬 |
| 1-4 방위대 임시초소 | 8 / 세 방향 | 110 | 16 | 방벽, 종결 |
| 1-5 잿빛 탑 외곽 | 8 / 서·동 | 130 | 18 | 연구 2단계 |
| 1-6 잿빛 탑 전면광장 | 8 / 세 방향 | 147 + 보스 | 20 | 백도하, 위기 작전, 다음 챕터 예고 |

전장은 36×20 범위, 3가지 기본 배치 변형을 사용한다. 첫 2작전은 튜토리얼 성격이다. 준비 중 시간은 흐르지 않는다. 전투는 180초, 성장 카드는 30/90/150초. 메뉴·카드·포커스 이탈 시 전투를 정지한다. 1-6은 150초 보스 등장 후 제한 시간까지 생존과 보스 처치를 모두 요구한다.

### 건설·강화·조합·공명

- 기본 4종: 창 / 봉인 / 붕괴 / 방벽. 건설 비용은 40/45/65/40, Lv2 비용은 30/40/55/35마석.
- 결과 4종: 뇌격 / 파쇄 / 중력 / 종결. 총 8종, 공개 조합식 6경로. 융합 결과의 Lv2 비용은 60.
- 아래 재료는 **모두 Lv2**이며, 서로 다른 두 타워를 소비해 선택한 한 지점에 결과를 남긴다. 거래 검증 전에는 돈/촉매/타워를 변경하지 않는다.

| 조합 | 추가 마석 | 촉매 |
|---|---:|---:|
| 창 + 봉인 → 뇌격 | 40 | 1 |
| 창 + 붕괴 → 파쇄 | 45 | 1 |
| 봉인 + 붕괴 → 중력 | 45 | 1 |
| 뇌격 + 붕괴 → 종결 | 80 | 1 |
| 파쇄 + 봉인 → 종결 | 75 | 1 |
| 중력 + 창 → 종결 | 75 | 1 |

- 종결 제작까지 세 경로 모두 누적 455마석·촉매 2개. 준비 판매는 실제 투자액 100%, 전투 판매는 75%와 투자 촉매 전량 반환.
- 시작 마석 140, 30/60/90/120/150초마다 +40. 촉매는 해금된 작전(1-3부터)의 60/120초에 1개씩. 처치 보상도 별도로 있다.
- **인접 공명은 소비형 조합과 별개**: 같은 기본 계열이 거리 6 이내에 있으면 +5%, 중첩 최대 1회. 창/붕괴는 피해, 봉인은 감속 지속, 방벽은 저지 지속에 적용. 융합 결과는 대상에서 제외.
- 방벽은 공격하지 않고 일반 적 최대 2명, 3초 저지(공명 시 3.15초). 해제 후 적에게 공유 재저지 면역 4초. 공성 적과 보스는 무시한다. 방벽 HP 55/90, 재충전 8/6초.
- 각성자의 몸으로 막기도 최대 2명·3초와 같은 면역 규칙을 적용한다. 무한 길막을 허용하지 않는다.

### 영구 성장과 저장 불변 조건

- 별점은 **전투 중 최대 결계 침식**: 10% 이하 3개 / 30% 이하 2개 / 나머지 승리 1개 / 패배 0개. 회복해도 이미 발생한 최대 침식은 줄지 않는다.
- 첫 보상 합계 90마핵, 재클리어 2, 패배 0. 최고 별점만 갱신한다. 전투 마석/촉매와 영구 마핵을 구분한다.
- 연구 4계열, 단계 비용 2/4/6. 1단계는 1-1 이후, 2단계는 1-5 이후, 3단계는 후속 챕터 조건으로 잠금. 환급은 **실제 연구 지출분만** 전액 돌려준다.
- 각성자 등급 F/E/D/C/B/A/S. 승급 비용 4/6/8/12/18/26와 챕터 조건이 있다. 하늬는 C, 백도하는 B로 합류. 강윤은 계속 미분류로 표시한다. 유료 성장당 HP +2.5%, 피해 +2%.
- 고유 스킬: 강윤 회피 / 하늬 질주 / 백도하 방호, 재사용 12초. 공통 응급 방호는 5초간 결계 피해 60% 감소, 재사용 35초.
- 저장은 버전 있는 바이너리 + SHA256 무결성 검사 + 원자적 교체 + 이전 정상 `.bak`. 사본에 변경 → 저장 성공 → 실제 상태 반영 순서다. 실행 ID로 중복 보상 방지.
- 기본 파일 손상이면 백업을 읽는다. 둘 다 손상이면 원본을 보존하고 덮어쓰기를 차단하며 실험실을 이용할 수 있다. 체크섬은 암호화나 강한 변조 방지 기능이 아니다.
- **판중 이어하기는 미구현**. `PendingRun`은 진행 중 판 식별용이며 재실행 시 전투를 복원하지 않는다. `last-run.txt`는 기록용.

## 5. 소스 구조와 수정 주의점

| 파일 | 책임 |
|---|---|
| `Assets/Scripts/Core/Campaign.cs` | 작전/스토리/해금, 프로필, 저장소/서비스, 보상, 연구/각성자 성장 |
| `Assets/Scripts/Core/RaonCombat.cs` | 캠페인별 배치/웨이브/능력치, 공명/방벽, 각성자 스킬, 보스 |
| `Assets/Scripts/Core/DefenseSimulation.cs` | 공통 전투 루프, 경제/건설/판매/융합 거래, 공격/적/승패 |
| `Assets/Scripts/Core/TowerCatalog.cs` | 기본 종류/가격/능력치, 조합식, 성장 카드 |
| `Assets/Scripts/Core/MobileInput.cs` | 손가락 ID/조이스틱/터치 영역/준비 단계 입력 |
| `Assets/Scripts/Presentation/CampaignController.cs` | 화면 전환, UI에서 캠페인 서비스 호출 |
| `Assets/Scripts/Presentation/CampaignHud.cs` | 도시 지도/브리핑/공방/각성자/기록/준비/결과 화면 |
| `Assets/Scripts/Presentation/GatefallGame.cs` | Unity 입력·시간 진행·일시 정지·카메라·UI 행동 연결 |
| `Assets/Scripts/Presentation/MobileHud.cs` | 전투 HUD, 건설/강화/조합서/카드/지도 |
| `Assets/Scripts/Presentation/ArenaView.cs` | 스프라이트/절차형 도형, 길/결계주/공명/보스 예고 |
| `Assets/Editor/PrototypeTools.cs` | 임포트/씬/폰트 검증, 설정과 빌드 진입점 |
| `Tests/CampaignChecks.cs` | 새 캠페인/저장 검사와 정상 명령 자동 전략 |
| `Tests/SimulationChecks.cs`, `Tests/MobileChecks.cs` | 기존 전투/거래/입력 회귀 검사 |

코어는 Unity 없이 C# 테스트에서 실행할 수 있다. UI는 서비스/전투 명령을 통해 상태를 바꾸고 그 결과를 그린다. 소스·씬을 바꾸면 `.meta`와 GUID를 보존한다.

기존 enum과 저장 호환성을 주의한다. 창은 내부 이름 `Autocannon`을 유지하며 방벽은 기존 종류 뒤에 추가한 값 7이다. 기존 `TowerCatalog` 창 수치(피해 10/16, 간격 .5, 사거리 5.2)와 **라온 캠페인의 `RawStats` 재정의(14/22, .7, 6.2)**를 혼동하지 않는다. 어느 플레이 모드의 수치를 고치는지 먼저 확인한다.

현재 적 기본 HP는 일반 60 / 러너 32 / 브루트 220 / 공성 400. 라온 배율은 `.90 + .05 * stageIndex`(0부터), 위기 작전은 추가 ×1.25. 최종 보스 HP 800(위기 1100), 속도 .65, 예고 1.5초 후 각성자 피해 32, 결계 피해 22/1.5초, 저지 무시. 이 수치는 확정 밸런스가 아닌 테스트 기준이다.

## 6. 실행물과 개발 환경

- Unity **6000.6.7f1**: `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe`
- 같은 버전 Android Build Support / SDK / NDK / OpenJDK 설치 완료.
- 시작 씬: `Assets/Scenes/Gatefall.unity`. 맵/UI는 런타임에 생성한다.
- PC 실행: `Builds/MobilePreview/Gatefall.exe` (Data와 DLL을 함께 유지).
- PC 배포: `Builds/Gatefall-PC-v0.4.0.zip`, **38,502,820바이트**.
- Android: `Builds/Android/Gatefall.apk`, **19,425,501바이트**. `com.gatefall.prototype`, versionName 0.4.0 / versionCode 4, ARM64 IL2CPP, min API28 / target36, 개발용 디버그 서명.
- PC ZIP SHA256: `15949c7dd5edf63d84aa4806a862c407b3b4d6401f3d021730e1b6aee1e36e8e`
- APK SHA256: `5b00962809de380163f78000f33439d674d958526dfc0fa8f1f21be4b81dd590`
- 정확한 기록: `TestResults/windows-build-results.json`, `android-apk-results.json`, `v040-source-hashes.json`.

프로젝트 루트의 PowerShell에서 전체 검증/빌드:

```powershell
pwsh -NoProfile -File .\Tools\build-delivery.ps1
```

또는 `Build-Gatefall.cmd` 실행. 자동 검사 → PC → Android → APK 검증 → PC ZIP 순서다. 같은 프로젝트를 연 Unity Editor와 이전 PC 실행 파일을 먼저 닫아 잠금을 방지한다. Unity 라이선스 IPC 때문에 제한 환경에서는 멈췄고 호스트 권한 실행으로 성공한 이력이 있다. 승인 서비스 제한은 이후 복구되어 최종 빌드를 마쳤다. 이전의 “승인 복구 대기”를 현재 차단 상태로 해석하지 않는다.

개별 검사:

```powershell
pwsh -NoProfile -File .\Tools\test-simulation.ps1
pwsh -NoProfile -File .\Tools\test-mobile.ps1
pwsh -NoProfile -File .\Tools\test-campaign.ps1
pwsh -NoProfile -File .\Tools\verify-source.ps1
pwsh -NoProfile -File .\Tools\verify-design.ps1
pwsh -NoProfile -File .\Tools\verify-campaign-runtime.ps1
```

개별 빌드는 `Tools/verify-unity.ps1 -Target Preview` 또는 `-Target Android`, APK 검사는 `Tools/verify-apk.ps1`. `verify-design.ps1`은 이전 전투/조합 모델 106개, `verify-campaign-runtime.ps1`은 현재 라온 구현 100개 대조다. `verify-campaign-design.ps1`의 208개는 별도 설계 검사이며 실제 플레이 증거가 아니다. 버전을 올리면 빌드 스크립트의 ZIP 파일명과 설정/보고서도 함께 맞춘다.

## 7. 검증한 것과 아직 확인할 것

### 통과/관찰한 내용

- 행동 검사 총 61개: 기존 전투 25 + 모바일 입력 18 + 캠페인/저장 18.
- 라온 구현 대조 100, 기존 모델 대조 106, 소스 15개·메타데이터 29개 확인. Unity 컴파일/씬/폰트/아트 로드 검증 및 PC/APK 빌드 성공.
- 정상 명령 자동 전략 10경로: 분산 배치 6작전, 뇌격 융합 4작전(1-3~1-6) 모두 승리. 최종 작전 결계 HP 분산 256 / 융합 254. 1-5 융합 215.
- 무건설 제자리 전략은 첫 2튜토리얼에서 승리, 1-3~1-6에서 각각 99.9/92.4/86.8/69.4초 패배. 난이도 조정 후보이며 사람 플레이 평가를 대신하지 않는다.
- PC 후보 빌드에서 1-1 실제 180초 완주, 별 3개·마핵 10·1-2 해금. 마지막 수치/화면 조정 후 최종 빌드에서 진행 복원과 창 연구 구매(10→8마핵, 연구 0→1), 다음 연구 단계 잠금 확인.
- **무료 연구 환급은 자동 검사 통과. 실제 최종 PC 화면의 환급 버튼 성공은 확인하지 못했다.** 클릭 시도 후 화면은 8마핵/연구 1 상태였으며, 사용자 인수인계 요청으로 추가 UI 검증을 중단했다. 기존 기록의 성공 문구를 정정했다.
- Android 최종 APK를 API35 에뮬레이터에서 실행. 도시/브리핑/준비/건설/각성자/공방 UI, 터치 건설·이동·자동 공격, 무음 저장·백업 생성·강제 종료 후 복원 확인. 추출한 프로필을 실제 디코더로 읽어 유효 체크섬과 저장값 확인.
- PC ZIP 155개 항목, APK v2 서명·16KB ZIP 정렬 확인. 실제 16KB 기기 검증을 의미하지 않는다.

스크린샷은 `Docs/pc-v040-*.png`, `Docs/android-v040-*.png`; Android 로그/저장 확인은 `TestResults/android-v040-runtime.log`, `android-v040-save-results.json`.

### 남은 일 / 알려진 한계

1. 실제 PC 연구 **환급 버튼 → 마핵 8→10 / 연구 1→0 → 재실행 복원** 확인. 작업 전 현재 저장/진행을 다시 확인하고 임의 초기화하지 않는다.
2. 최종 Android UI의 전체 180초, 실제 손 조작 융합/종결, 보스전은 미완주. 코어 자동 검사나 과거 v0.3 UI 검증과 구분한다.
3. 실제 Android 휴대폰의 멀티터치·안전영역·발열·성능, 전체 사람 난이도/재미 테스트. iOS 빌드/실행은 별도 Mac/Xcode/서명 환경이 필요하며 미완료.
4. 후속 6챕터(총 42작전 계획 중 나머지 36개), 도감, 캐릭터별 완성 아트/애니메이션/보이스, 판중 복원, 온라인·광고·결제는 미구현.
5. Android 로그에 선택적 Play Asset Delivery `AssetPackManager` 탐색의 비치명적 `ClassNotFoundException`이 있다. 관찰한 실행은 정상 진행했고 게임플레이 관리 코드 예외/치명적 크래시는 발견하지 않았다. 출시 전 필요 여부 정리.
6. 과거 host GPU에서 Swappy 네이티브 크래시가 있어 **optimizedFramePacing=false**를 유지한다. 변경하려면 실제 재검증 필요.
7. 저장 손상 테스트의 `File.Replace`에서 일시적인 파일 교체 IOException 1회가 있었고 진단 재실행/후속 검사는 통과했다. 서비스는 실패 시 메모리의 재화 변경을 확정하지 않는다. 재발 시 로그를 보존해 원인을 확인하고 실제 사용자 저장을 지워 해결하지 않는다.

## 8. 로컬 저장·에뮬레이터·백업

PC 저장:

```text
C:\Users\Pro\AppData\LocalLow\Gatefall Lab\Gatefall - Last District\raon-profile.sav
```

`.bak`은 이전 정상 저장. 마지막 화면 관찰 기준 1-1 별3 / 1-2 해금 / 마핵8 / 창 연구1이다. 이후 사용자가 플레이할 수 있으므로 현재값으로 단정하지 말고 다시 확인한다. 테스트 진행은 PC 로컬에만 있으며 ZIP/APK에 넣지 않았다. 초기 플레이 확인은 별도 테스트 프로필/백업을 사용한다.

기존 에뮬레이터: AVD `RealmDefense_API35`, serial `emulator-5560`, API35 Google Play x86_64 + ARM64 번역. 도구 경로:

```text
C:\dev\rea_project\realm_defense_work\tools\android-sdk\emulator\emulator.exe
C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe
```

이전에 성공한 GPU는 `-gpu host`이다. 현재 실행 여부는 `adb devices`로 확인한다. AVD 위치 등은 외부 Realm 인수인계에 기록되어 있다. Unity 빌드가 ADB daemon을 종료할 수 있으므로 빌드 후 다시 확인한다. Android 저장 경로는 `/sdcard/Android/data/com.gatefall.prototype/files/raon-profile.sav`, 실행 activity는 `com.gatefall.prototype/com.unity3d.player.UnityPlayerActivity`. Google 계정은 사용자 로그인 상태를 유지하고 계정 정보를 수집하거나 변경하지 않는다.

프로젝트 백업: `Backups/pre-raon-v040-20261026.zip`, `Backups/Gatefall-v030.apk`; 이전 PC ZIP도 보존되어 있다. 백업 복원은 필요한 파일 범위를 확인한 뒤 진행한다.

## 9. Realm 참고 자료

- `C:\dev\rea_project\HANDOFF_realm_defense.md`
- `C:\dev\rea_project\realm_defense_work\reports\REALM_DEFENSE_GAMEPLAY_ANALYSIS_2026-10-26.md`
- `C:\dev\rea_project\realm_defense_work\reports\GATEFALL_DESIGN_PROPOSAL_2026-10-26.md`

확보/정적 분석은 끝나 있다. 다시 APK 확보나 Google 로그인부터 시작할 필요가 없다. 보고서는 기본 전투 데이터/웨이브/연구 등의 근거이며 완전한 원본 C# 소스 복원은 아니다. 불확실한 부분은 보고서의 확인 범위를 따르고 자체 규칙과 원본 확인값을 분리한다. 외부 인수인계의 오래된 Gatefall 구현 상태보다 본 프로젝트의 최신 코드/문서를 우선한다.

## 10. Claude / Codex CLI 협업

이 PC에서 `codex --version`으로 **codex-cli 0.163.0**을 확인했고 `claude.exe`도 설치 경로가 조회됐다. CLI 로그인/사용 한도나 Claude 실제 세션 실행까지 검사한 것은 아니다. Codex 도움말은 프로젝트 경로 옵션 `-C`와 초기 프롬프트를 지원한다.

PowerShell에서 Codex 시작 예:

```powershell
Set-Location -LiteralPath 'C:\Users\Pro\Documents\ChatGPT\react\gatefall-unity'
codex 'HANDOFF_CLAUDE.md와 README.md를 읽고 현재 구현과 미검증 항목을 파악해. 이후 내가 지정하는 작업을 이어서 진행해.'
```

Claude도 같은 프로젝트 폴더에서 `claude`로 시작하고 아래 요청을 전달하면 된다. 새 CLI 대화가 이 앱 대화의 전체 내용을 자동으로 공유한다고 가정하지 않는다. 인수인계 파일과 실제 소스/검증 기록이 공통 기준이다.

> HANDOFF_CLAUDE.md, README.md, Docs/Design/GATEFALL_RAON_WORLD.md, TestResults/runtime-v040.md를 읽고 Gatefall v0.4.0의 현재 상태를 파악해. 라온시 세계관과 기존 강화·소비형 융합·인접 공명을 유지해. 먼저 이번에 담당할 파일 범위와 검증 방법을 정리하고, 내가 지정한 기능/수정 작업을 이어가. 구현 완료와 설계만 있는 범위, 자동 검증과 실제 플레이 확인을 구분해. 작업 종료 시 인수인계서와 progress.md를 갱신해.

협업 운영 제안:

- 같은 시간에는 **같은 파일을 두 도구가 수정하지 않기**. 예: Claude가 기획/UI를 맡는 동안 Codex는 읽기 검토/코어 테스트를 담당. 양쪽에 적용할 작업 분담은 사용자가 전달한다.
- Unity 임포트/빌드와 생성 결과물 갱신은 한 쪽에서 순차 수행. 빌드 폴더·검증 로그를 동시에 덮어쓰지 않는다.
- 인수인계 시 담당 파일, 변경 내용, 통과 검사, 남은 항목을 `progress.md`와 이 문서에 남긴다. 동시 편집할 때는 문서 기록도 차례로 한다.
- 확인 시점에 상위 Git 저장소에서 이 프로젝트는 **`?? gatefall-unity/` 미추적 상태**였다. 이 작업에서 커밋/브랜치를 만들지 않았다. 두 도구가 독립 브랜치/worktree로 개발하려면 먼저 프로젝트 범위의 Git 기준점을 마련한다. 상위 `react`의 다른 프로젝트까지 일괄 추가하지 않는다. 빌드/Library/임시 로그를 소스와 구분한다.
- 권장 재개 순서: 최신 파일 확인 → 담당 범위 합의 → 환급 UI 미검증 건 확인 또는 사용자 지정 작업 → 관련 코어 검사 → 필요한 경우 Unity/플랫폼 검증 → 기록 갱신. 기존 통과 항목을 새 수정의 근거 없이 전부 재구현할 필요는 없다.
