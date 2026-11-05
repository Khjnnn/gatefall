# Findings

## Current design revision — 2026-10-26
- User now explicitly requests in-game recipe fusion as well as upgrades. Earlier suggestions to defer consuming-tower fusion are superseded for the next expansion.
- Existing Gatefall v0.2.0 has a single tower, no level/type/recipe inventory, six sockets and a 180-second run; source remains unchanged in this design task.
- Reference analysis separates construction, upgrades, research and permanent progression; it does not establish a generally active merge system in Realm Defense. Our recipe system is a new design.
- Working design: base Lv1→Lv2; two different base Lv2 towers + currency + one fusion core → hybrid Lv1; hybrid Lv1→Lv2; hybrid Lv2 + the third base Lv2 + currency + one additional core → capstone.
- Make all six recipe routes visible from run start. Core delivery at 60/120 seconds gives deterministic opportunities; recipe access must not rely on random card draws.
- Preserve material instance multiplicity, paid currency and invested cores. Anchor uses one material's socket, the other becomes empty; transaction must validate both IDs and resources before committing.
- Saved revision in Docs/Design/GATEFALL_COMBINATION_DESIGN_V2.md and combination-balance-v2.json. Names, visuals, economics and crafting recipes are Gatefall proposals; reference assets are not copied into the game.
- Base Lv2 cumulative costs: autocannon70, binding85, rupture120. First fusions cost195/235/250; each upgrades for60. Three capstone routes are normalized to455 total credits and two invested cores.
- Proposed economy: start140, five supplies40, enemy rewards total197; ideal run income537. First cheap fusion195 is budget-feasible from guaranteed supplies220 at60s. This is not proof of survival or runtime purchase timing.
- Sale proposal returns floor(actual invested credits×0.75) and invested cores, not the original material towers. Core quantity limits simultaneous field power; it is not a lifetime craft counter.
- Validation:100 checks passed for IDs/levels/acyclic tier progression, material references, all alternative capstone totals, wave arithmetic, resource ceilings, portfolio costs, refund arithmetic and local document links. First inexpensive fusion is affordable from fixed supply at60s; actual survival/kill timing are untested. Proposed three-minute combat HP total11988 must be tested with map uptime and target selection.
- Canonical revision: Docs/Design/GATEFALL_COMBINATION_DESIGN_V2.md. Older 2026-10-25 notes below and the external initial design report remain historical, particularly their delayed-fusion recommendation and earlier economy/map alternatives.

- No existing Unity project or applicable AGENTS.md found in the workspace/checked ancestors.
- Root planning files belong to unrelated completed work and remain untouched.
- User wants both direct hunter movement and tower placement, in a modern fantasy urban setting.
- User confirmed no installed Unity Editor, so actual Editor import/build/play-mode verification is currently unavailable.
- Official Unity 6.3 LTS announcement: https://unity.com/blog/unity-6-3-lts-is-now-available
- Official Hub editor installation guide: https://docs.unity.com/en-us/hub/add-editor
- RuntimeInitializeOnLoadMethod supports runtime initialization on scene load and editor Play mode: https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/runtimeinitializeonloadmethodattribute

## Verification approach
Compile and run the actual simulation source using PowerShell Add-Type, exercise time, movement, targeting, costs, damage, pause/end state, and complete runs. Provide an Editor validation/build entry point for checking integration once Unity is installed. These checks do not substitute for a real Unity playtest.

- Verified Unity 6000.3.27f1 release directly: https://unity.com/releases/editor/whats-new/6000.3.27f1 . ProjectVersion is pinned to this version.
- Korean font source: https://github.com/google/fonts/tree/main/ofl/nanumgothic . Bundle the unmodified regular TTF with its OFL license.
- Initial executable validation: 25 checks passed, including 30/60/144 Hz time behavior, pause, building, shots, defeat precedence, reset and seeded spawning.
- Scripted builder won 12/12 seeds with full health; idle automatic shooter lost 12/12. This baseline deliberately favors first-run success and is not a human difficulty/playability evaluation.

## Mobile follow-up
- Installed Unity 6000.6.7f1 compiles and builds the desktop mobile-UI preview successfully. Prior Windows-native checks also passed.
- Android and iOS platform modules are absent; APK, Xcode export, IPA, and physical device tests have not been run.
- Official Android modules: Android Build Support, Android SDK & NDK Tools, OpenJDK. https://docs.unity.com/en-us/engine/6000.5/manual/platform-specific/android/getting-started/sdksetup/install-dependencies
- Official iOS flow: Unity exports an Xcode project; final local app build uses macOS/Xcode (or supported cloud build). https://docs.unity.com/en-us/engine/6000.0/manual/platform-specific/iphone/ios-building-and-delivering/build-process
- Screen.safeArea is bottom-left-origin; UI input/drawing uses a shared top-left-origin conversion. https://docs.unity.com/en-us/engine/6000.7/script-reference/unityengine/screen/safearea
- 16 new executable checks passed for safe-area math, phone/tablet bounds, finger identity, simultaneous movement/building, cancellation, state/rotation reset and integration with construction costs.
- Desktop preview verified by actual window: touch-style Start button, Build button (80 -> 40), joystick mouse drag movement/release, Range toggle, Pause button. Saved a delivery screenshot under Docs/mobile-preview.jpg. This is not a hardware touch test.

## Android installation follow-up
- Official Unity CLI is installed at AppData/Local/Unity/bin/unity.exe. Local `install-modules --help` confirms `--editor-version`, `--module`, `--child-modules`, `--accept-eula` and `--non-interactive`.
- Official Hub CLI reference: https://docs.unity.com/en-us/hub/hub-cli-reference . Standalone CLI supports this existing Hub-managed Editor.
- Installed Editor metadata selects Android support 6000.6.7f1, OpenJDK 17.0.18+8, NDK r27c, SDK Build Tools 36.0.0, platform tools 37.0.1, platforms 34/36/37 and command line tools 16.0.
- Native CLI single-stream download was slow (about 88 MB after several minutes). Cancelled it before installation and confirmed its two processes exited. Fetching the exact official metadata URLs in parallel byte ranges into a temporary ignored folder, checking supplied hashes and ZIP CRCs, and populating the Unity installer cache before resuming installation.
- Official installer reused verified cached files and completed all 11 components. First Android build succeeded with no game-code changes. Automatic Target SDK resolved to API 36 despite platform 37 also being installed. The APK passes signature and ZIP alignment verification; no Android device is connected for runtime testing.

## 플레이테스트 이후 확장 기획 — 2026-10-25

### 확인된 현재 동작
- 사용자는 PC에서 직접 플레이했고 헌터로 적 진군을 막는 현상을 발견했다. 이를 반드시 제거할 버그로 보지는 않으며, 더 넓은 맵·어려운 방어·타워 조합/강화를 원한다.
- 현재 몸막기는 적과 헌터의 물리 충돌로 길이 막히는 구조가 아니다. UpdateEnemies에서 거리 2.5 안에 들어온 적이 헌터를 목표로 바꾸고 거리 0.64에서 멈춰 공격한다.
- 헌터 피격 후 0.65초 무적이 모든 적에게 공통 적용된다. 적은 무적 중 공격해도 공격 쿨다운 1.1초가 시작된다. 다수 적을 유인해도 피해가 단순 합산되지 않아 버티기에 유리할 수 있다. 이를 제거하기 전에 실제 탱킹 시간을 측정해야 한다.
- 현재 모든 적 종류는 같은 목표 선택을 사용한다. 빠른 적/큰 적도 헌터에게 유인된다. 건물은 장식이고 실제 도로 경로/적 간 회피가 없다.
- 현재 맵은 25.2 × 13, 게이트 3개, 건설 지점 6개, 고정 카메라다. 건설 완료 뒤 경제 지출처가 없으며 강화/조합/런 중 성장 선택이 없다.

### 제안의 중심
헌터가 위급한 전선을 직접 지키고, 타워 조합이 다른 전선을 맡는 도시 방어. 매 순간 '어디로 이동할지', '지금 지을지 강화할지', '이번 판 어떤 조합을 완성할지'를 선택하게 한다. 아래 수치와 기능은 초안이며 승인된 구현 요구나 검증 결과가 아니다.

### 1. 넓어진 맵의 역할
- 첫 실험 크기 36 × 22: 현재 대비 면적 약 2.4배. 건설 지점 12개, 진입로 3개, 중앙 결계 1개, 외곽 보급 거점 2개를 배치하는 안.
- 바깥은 사거리를 오래 활용하는 공격적 배치, 안쪽은 여러 진입로를 보조하는 최후 방어선. 중앙 집중과 외곽 분산 모두 장단점을 갖게 한다.
- 카메라는 헌터를 따라가며 맵 경계에서 멈춘다. 결계 체력·피격 방향·미니맵은 항상 보인다. 화면 축소로 캐릭터와 버튼을 작게 만드는 방식은 피한다.
- 길/장애물은 실제 경로 규칙과 일치해야 한다. 먼저 고정된 도로 경로를 만들고 경로 위 소프트 회피를 구현한다. 자유 미로 건설과 절차적 맵은 후속 검토로 둔다.
- 보급은 이동의 선택적 보상이다. 결계 방어를 포기해야만 기본 자원을 얻는 강제 심부름이 되지 않도록 처치 수입/기본 보급을 유지한다.

### 2. 몸막기와 난이도
- 일반병은 헌터에게 유인되어 잠깐 전선을 붙들 수 있다. 적들이 한 점에 겹쳐 완전히 멈추는 현상은 분산/회피 규칙으로 줄인다.
- 돌파병은 결계를 우선 목표로 삼고 헌터를 비켜 간다. 원거리병은 뒤에서 사격하고 장판/강공격을 예고한다. 중장갑 정예는 헌터를 밀어내는 공격을 갖는다.
- 같은 몬스터를 때로 무시시키는 숨은 확률 대신 외형/아이콘으로 행동 유형을 구별한다. 몸막기를 무효화하는 적만 출현시키지 않는다.
- 난이도는 다중 전선, 적 역할의 혼합, 강화 예산의 경쟁으로 만든다. HP/공격력 증가만으로 조정하지 않는다.
- 다음 침공 방향과 위협 유형을 약 8초 전에 표시한다. 고정 시간대별 적 예산과 전선별 상한을 두어 랜덤 시드가 갑작스러운 불가능 조합을 만들지 않게 한다.
- 헌터 부상은 회복을 포함한 비용으로 남긴다. 초기판에서는 헌터/결계 중 하나가 쓰러지면 패배하는 기존 규칙을 유지한다. 회복량·무적시간 변경은 실제 피격 간격을 기록한 뒤 결정한다.

### 3. 타워의 강화·진화·연계
- 기본 3종: 기관포(단일 지속 피해), 냉기탑(감속/길목 제어), 전격탑(연쇄/다수 처리).
- 공통 강화 Lv1~3은 마석으로 구입하고 위력/사거리/효과를 높인다. 이미 강화한 타워의 외형과 UI에 레벨이 드러나야 한다.
- 후속 진화는 Lv3 타워와 해당 런의 설계도 조건을 충족하면 두 갈래 중 하나를 선택하는 안이다. 기관포→관통포/속사포, 냉기탑→빙결탑/감속장, 전격탑→연쇄탑/집중포. 이름과 세부 수치는 임시다.
- 조합은 첫 단계에서 타워를 소비하지 않는 전투 연계로 구현한다. 냉각 표적+기관포→방어 약화, 냉각 표적+전격→소규모 파쇄, 기관포 표식+전격→표식 대상 추가 타격. 모든 표식은 지속시간/발동간격/중첩 상한을 명시한다.
- 모든 냉기 효과가 빙결을 일으키지 않게 하고, 정예/보스에게 감속·빙결 지속 상한을 둔다. 3종 중 어느 하나가 필수 정답이 되는지는 조합별 승률/투자 대비 기여도로 점검한다.
- 조합 도감은 처음부터 조건을 보여주고, 현재 보유한 타워로 가능한 연계를 강조한다. 합성으로 새 타워를 만드는 별도 시스템은 연계의 재미를 확인한 뒤 소수의 공개 레시피로 검토한다.

### 4. 로그라이크와 경제
- 처치/보급 마석은 건설·강화·제한된 결계 수리에 공통 사용한다. 수리는 횟수나 회복량 상한을 두어 무한 버티기가 되지 않게 한다.
- 일정 웨이브 종료마다 세 장 중 하나의 성장 선택: 기관포 관통, 냉기 지속, 전격 분기, 헌터 이동/유인 보조 등. 초안에서 무작위 선택이 필수 타워 자체를 못 얻게 하지 않는다.
- 선택 화면은 게임을 멈추고 읽을 시간을 준다. 보유 타워와 관련된 선택지를 제공하는 규칙과 제한된 재추첨을 검토한다.
- 영구 성장은 새로운 설계도/헌터/난이도 선택 해금을 우선 검토한다. 반복 플레이로 공격력 수치를 쌓아야 기본 난이도를 넘는 구조는 초기 검증에 넣지 않는다.
- 조합형 본편의 목표 런 길이는 약 6~8분을 가정하되, 첫 확장 검증은 기존 3분을 유지한다. 보스 등장·종료 조건은 실제 성장 속도를 확인한 다음 확정한다.

### 5. 모바일 조작과 결과 피드백
- 왼손 이동, 오른손 상황별 건설/강화. 가까운 건설 지점 또는 선택한 타워의 정보/사거리/가격/변경 효과를 하나의 패널에 표시한다.
- 건설/강화 선택 중 일시정지하는 방식부터 검증한다. 가까운 지점까지 이동해야 하는 위치 선택의 의미는 유지한다. 패널을 닫을 때 누르던 손가락을 해제해 갑작스러운 이동/오작동을 막는다.
- 현재 선택된 타워를 터치로 명확히 구별하고 UI 터치를 월드 선택과 중복 처리하지 않는다. 드래그 합성은 첫 확장에서 제외한다.
- 결과 화면은 전선별 누수, 결계 피해 원인, 타워별 피해·제어 기여, 몸막기로 끈 시간, 자원 사용처를 보여준다. 다음 시도에서 고칠 한 가지가 보이도록 한다.

### 개발/검증 순서 제안
1. 전장 검증: 확대된 맵/카메라/미니맵, 실제 진입 경로, 일반·돌파·원거리 적, 침공 예고. 같은 3분에서 이동과 전선 분담이 의미 있는지 먼저 확인한다.
2. 성장 검증: 기본 타워 3종, Lv1~3, 마석 경제, 강화 패널, 판매/되돌리기 정책. 오배치 부담을 줄이는 제한적 환불을 검토한다.
3. 조합 검증: 기본 연계 3개, 런 중 선택, 진화, 정예/보스, 더 긴 런. 단계별 PC/Android 빌드를 제공한다.
4. 반복 동기: 설계도/헌터 해금, 다른 맵, 도전 규칙. 광고·결제·장비 파밍·협동은 핵심 전투 검증 후 별도 기획한다.

### 플레이테스트 판단 기준
- 사람이 플레이한 동일 시드에서 최소 두 가지 배치/조합으로 승리할 수 있는지 확인한다. 고정 자리 몸막기만으로 전선을 해결할 수는 없어야 한다.
- 적이 많다는 이유로 경로가 영구 정지하거나 화면 밖 공격으로 원인 모를 패배가 발생하지 않아야 한다.
- 미니맵/침공 예고를 보고 대응 방향을 선택할 시간이 있고, 실패 후 원인을 설명할 수 있어야 한다.
- 방어가 실패해 처치 수입이 감소해도 재구축의 여지가 남는지 확인한다. 초기 숫자 증액보다 이런 구조의 검증을 우선한다.
- 휴대폰에서 조합 조건과 강화 효과를 읽을 수 있는지, 메뉴 닫기/두 손가락 조작/성능을 확인한다. 아직 이러한 확장 기능은 구현하거나 실기기 검증하지 않았다.

## v0.3.0 implementation and runtime findings
- Original six-image art set integrated. PNG importer must explicitly use Texture2D; Resources.Load validation now blocks invalid imports.
- Tower and fusion mechanics match the approved numeric design in106 comparisons; automated25 combat/18 input checks pass.
- Actual Android UI completed A2+B2 -> Arc1 -> Arc2 at normal resources/time, confirming atomic consumption, costs, anchor slot and displayed cumulative investment.
- API35 emulator requires host GPU here. Swappy Vulkan native initialization crashed with optimized frame pacing enabled; disabling it fixed startup. Physical-device performance remains unmeasured.
- Full-display clear camera added after detecting stale HUD pixels outside the inset arena viewport.


## Realm-to-campaign scope evidence
- Local 3.4.47 APK analysis confirms fixed placement/role-based towers, in-battle upgrades and tech research, separate permanent star research with respec, branch specializations, staged/subwave schedules, manual first-wave start, and campaign/Legendary variants.
- The local report did not establish exact current hero progression, tutorials, story flow, or star-rating formulas. Any Gatefall equivalents must be labeled original design; do not invent Realm findings.
- Gatefall keeps the user-selected city campaign + resetting in-battle roguelike growth. The new campaign must preserve existing consumptive six-route fusion, instead of replacing it with Realm same-slot specialization.


- Official Google Play listing reviewed2026-10-26 confirms hero abilities, multiple worlds/towers, bosses, power items, daily awakening progression and tournaments. It does not define a star-rating formula or the exact hero economy; those remain Gatefall design choices. Source: https://play.google.com/store/apps/details?id=com.babeltimeus.legendstd&hl=en
- Campaign direction chosen: one controlled hunter per battle; fixed route/socket strategy plus separate permanent research and story unlocks; existing fusion remains central. First chapter proposes6 stages, with gradual feature introduction and a distinct sandbox that retains all v0.3.0 content.

## Raon setting supersedes the initial campaign fiction
- The user's attachment establishes 라온시/라온강, seven black towers, seven years since the fall, 강윤/백도하/유채린/하늬/금태혁/나은재, 제7방위대/금관 길드/잔광 교단, three acts and the sealing ending. Earlier 해온시 cast and six-district story are archived.
- Preserve consuming fusion as 공명 융합; add bounded 인접 공명 separately. Four base families now include a new Barrier. Spear replaces Autocannon's concept and needs actual attack/art retuning; this is not a runtime rename in this turn.
- Stars now record peak erosion only (10%/30% proposed boundaries), not optional tasks. Magic cores fund both permanent research and hunter progression; stars are not spent. Battle catalysts remain separate.
- 강윤 stays 미분류 while stabilization advances; 백도하 recruits atB and 하늬 atC. Numeric costs/chapter gates and no random permanent loss are implementation proposals to reconcile the setting with playability.
- v2 design validation passed208 checks. First-chapter magic-core income90, available research24 and Kang Yoon stabilization10, remainder56. All six fusion routes still total455/two catalysts for capstone. No new runtime, assets or APK built in this revision.


## Raon runtime v0.4 findings
- Stage enemy HP uses0.90+0.05*stageIndex times archetype HP; crisis multiplies1.25. The final boss has800 normal/1100 crisis HP. First two tutorials still permit scripted idle survival; stages3–6 idle fail at99.9/92.4/86.8/69.4seconds. This is onboarding tuning, not established human difficulty.
- Ten distinct legal-command scenarios win all six stages (spread6, fusion4). Final-stage core256/254 for spread/fusion; fusionstage5core215. No persistent growth was required by these scripts.
- Preparation movement,100% refunds, locks, catalyst/card filtering, capped resonance, fusion ownership, research snapshots, finite barriers, boss deadline, paused skills, erosion thresholds, first/repeat rewards, duplicate outcome, failed-save retry, growth gates, crisis ledger and checksum recovery pass18 checks. Runtime matches100 stage/unlock/price fields from v2 design.
- Windows file replacement had one transient IOException during the synthetic corruption-recovery fixture. A diagnostic rerun and next tuning run passed; live transactions catch IO failures and retain in-memory currency until persistence succeeds. Do not bypass checks or overwrite corrupt profiles.
- Android v0.4 ARM64 APK builds and installs successfully; signaturev2/16KB ZIP alignment verified. Actual mobile city screen renders Korean text and six stage locks correctly.

## v0.5 continuation findings
- Actual tower panel showed resonance-adjusted current damage but a non-resonance upgrade preview (15→22 although upgraded damage is23). Added a non-mutating tower/level stats query that applies adjacency using the preview level; HUD now shares it. Regression checks include three affected families, research/cards and unchanged transaction state. Rebuild and actual preview confirmation required.
- Native startup crash trace names API35 ARM translation and vulkan.ranchu vkSetDebugUtilsObjectNameEXT/libunity Image.SetLabel; subsequent start survived. Android build settings now explicitly select OpenGLES3 (pending rebuilt runtime validation). Unity API reference: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PlayerSettings.SetGraphicsAPIs.html and SetUseDefaultGraphicsAPIs.html. This is a compatibility choice for the current 2D renderer, not physical-device performance evidence.
- Actual research purchase changed36cores to34, Spear rank0to1. Capture growth-purchase-verified.png. Relaunch/refund still pending.
- Aspect testing found4:3 city stage1-4/1-5/1-6 nodes obscured by briefing, caused by cover-scaled map. Changed map to fit the available left viewport. Growth portraits expanded with height and squeezed text to a few characters: cap portrait size, reserve label/button regions, position research rank indicators away from buttons. Before captures preserved; rebuild and after validation pending.
- Follow-up verification supersedes the pending notes above: OpenGLES starts/play succeeded, research36→34→36 and relaunch restored correct ranks/currency;4:3/16:9 fixes rendered and nodes selectable. Actual1-4/1-5/1-6 completed3stars; chapter18stars/90cores. Finalc533APK preview15→23 thenactual23 confirmed; finalpause2:54unchanged8seconds andbackgroundreturn2:52paused. Fresh-profile tutorial points at westernslot2; twoLv2towers built with140startingcredits. Finaltutorialresult/performance capture still underway.
- Final verification supersedes that underway status: fresh1-1Won180/core300/kills45,3stars/+10/nextunlock;originalchapter18stars/90coresrestoredbyexactsavehash. FinalAPK3coldstartsnormal,installedhashmatches,finalandchapterperformanceJSONsavedwithscope. OriginalcomparisonPNG1800×1858rendersall6actualscreenshotsreadably. Fullcompletionauditrecorded;onlythedeclaredphysical-device/iOSlimitsremain.

## 2026-10-27 v0.6 start
User requests a comprehensive original visual replacement based on reference design principles, including AI-generated imagery, and a polished representative stage with satisfying controls and combat. Current v0.5 comparison shows schematic city blocks, flat gray board and geometric sprites; readability is established but its art lacks place and character. Preserve v0.5 saves and playable mechanics. No original third-party assets or code in the release.

- 2026-10-27: v0.5 art direction analysis confirmed three distinct road geometries; backgrounds must retain coordinates per layout to avoid enemies walking off the painted roads. The first newly generated crossroads has legible 2:1 geometry and recognizable storefront roofs; output dimensions and visual alignment still require actual Unity validation. Existing generated art is not assumed gameplay-ready merely because it looks polished.
- `Tools/art/gatefall_art.py` referenced by older documentation is absent; actual generator is `Tools/art/generate_all.py` and its modules. No original game art has been imported.

## 2026-10-27 v0.6 artwork integration
The v0.6 art direction and original image-generation source sheets are recorded in Docs/Design/GATEFALL_ART_DIRECTION_V06.md and Docs/Art/V06_GENERATION.md. Three road layouts were redrawn with preserved reference geometry; six operations use those three images cyclically. The district map, three hunters, five enemy kinds, eight tower families/levels, barrier, pad and rift are replaced. New 9-slice panel/button art is generated in prepare_v06.py. V5 assets remain as fallback for small icons and some effect sprites; this is not yet the user's requested complete replacement of every image. Siege/grunt silhouettes still need differentiation. Real phone readability, multitouch, sound, thermal performance, and external fun/balance testing remain open.
Earlier candidate 690444b4... was installed and used to inspect 1-1 construction, 1-3 two material towers, upgrade, card and 60-second catalyst. The 1-3 run was left unattended and lost before an actual fusion was performed; do not label that run a fusion success. A newer final candidate a2b3bbd5... includes new pad/rift/UI, larger actors, impact audio and fusion effect; Android install and normal city startup were observed. Final actual fusion testing is in progress.

- v0.6 final update: all 78 artwork keys now have V6 replacements, including freshly drawn small icons and effects; previous in-progress statements that UI/FX fall back to V5 are superseded. V5 remains a code fallback only. One additional tested candidate after a2b3bbd5 changed icon/effect resources; final 77867a5a further changed the empty pad plus to white. Final Android screenshots are in Docs/QA/v06, and the actual fusion evidence is explicitly the earlier a2b3bbd5 candidate. Real-device and human fun testing remains outstanding.
- v0.7 campaign test first failed `wide player movement`: Ready state intentionally clamps each Tick to 0.1 s. Changed the test to send normal 0.1 s frames; it was a test assumption, not a movement fix. Expanded bounds and gameplay scope are being verified separately.

## v0.7 확인 사항
1-4 후보 화면에서 추가 지점이 타이머와 겹쳐 위치를 옮겼고, 배경 바리케이드는 열려 있는 진입로와 충돌해 원본 자체 이미지를 수정했다. 4:3 추적 배율은 여백을 줄이도록 수정했고 전체 보기의 위아래 여백은 전장 전체 표시를 위해 유지한다. 최종 APK에서 시작/건설/경고는 확인했으나 인간 플레이에 의한 난이도 평가는 아직 필요하다.

## 2026-10-28 v0.8 발견
일반 창 4개만으로 이전 후보 3별/무피해. 출현 간격만 좁혀도 완주 난이도를 올렸다고 증명되지 않아 후반 총 적 18마리 추가. 처치 재화도 함께 늘어나므로 자동 승리만으로 조절 완료라 볼 수 없다. 실제 조합은 촉매 제공 뒤 88.75초에 실행됐고 동쪽이 비면서 결계 피해가 발생했다. 초기 no-window/소프트웨어 에뮬레이터는 Unity 그래픽 초기화 실패; 호스트 GPU 사용 시 정상 실행.

- 최종 v0.8.1 실제 분산 배치는 결계 103까지 떨어져 1별, 이른 조합과 재배치 재시도는 결계 263/2별. 성장 카드와 입력 조건이 달라 인과 비교는 불가능하다. 일반 창 4개는 여전히 3별이므로 밸런스 확정이 아니다. 4:3 전체 보기에서 가장 먼 서쪽 지점 메뉴와 실제 건설이 가능했다.

## v0.9.0 추가 발견
- 1-3 최종 APK UI에서 뇌격 조합은 동작한다. 다만 한쪽에만 건설해 조합 메뉴를 확인하는 동안 반대쪽의 정예가 결계를 빠르게 손상시켰다. 이 관찰은 조작 중 방어 공백 가능성을 보여 주지만 통제된 공략 비교가 아니다.
- 자동 확장 창 전략이 1-2~1-5를 무피해로 통과하므로 여섯 스테이지 각각이 서로 다른 공략을 필요로 한다는 완료 기준은 아직 검증되지 않았다.

## 챕터 1 후속 밸런스 시작
v0.9의 창은 캠페인에서 사거리 6.2, 초당 피해 약 20/31.4로 조정되어 교차점에서 여러 입구를 겹쳐 막기 쉽다. 이전 west 정책은 중앙 지점도 포함하며 빠른 자동 구매이므로 순수 서쪽 집중 전략은 아니다. 먼저 기존 결과를 보존하고 사거리와 역할별 투자 정책을 대조한다.

## 2026-10-28 v0.9.1 시험
창 사거리 6.2→5.2. 1-5 4~6구간은 기존 적 총수를 유지하고 세 마리씩 같은 진입로로 보내며 양쪽을 교대한다. 시드 7/19/41에서 서쪽 편중 창 확장은 결계 245/256/238, 같은 배치 순서의 두 번째 타워를 붕괴로 바꾼 정책은 287/263/263. 두 정책은 타워 비용과 업그레이드 시점이 다르므로 엄밀한 단일 변수 비교는 아니다. 1-4에서는 각각 287/287/274 대 300/300/300, 1-6에서는 붕괴 혼합이 한 시드에서 실패해 상황에 따른 교환이 나타났다. 이전 v0.9 결과는 chapter-battle-review-v090.txt에 보존.

최종 v0.9.1은 동시 출현을 0.25초 간격으로 완화한 버전. 1-5 결계(시드 7/19/41)는 창 276/245/252, 붕괴 혼합 276/252/252로 이점이 작다. API35 최종 APK 1-5 100초 화면에서 군집이 일부 여전히 겹침. 기존 저장 복원 해시 확인, 테스트 에뮬레이터 종료. 최종 빌드 6작전 전체 실전 완주 및 실제 휴대폰 테스트는 미수행.

## 표식 확장 조사
프로필은 버전 1 및 6개 고정 배열, 월드 지도는 6개 고정 노드, 타워 enum·사격 집계·아트 키도 고정이다. 계획대로 1-10과 1-14 이후 해금하려면 1-7~1-14 플레이, 저장 버전 이행 및 맵 페이징이 함께 필요하다. 기존 1-6 최초 봉인과 기존 결과/해금을 보존해야 한다.

편성 결과 미리보기는 선택된 기본 계열부터 조합 경로를 순회해 중간 결과와 종결까지 표시한다. 관측창은 1-14 클리어 전에는 표시하지 않는다. 표시는 재료 종류와 해금만 의미하고 실제 전투의 Lv2·마석·촉매 조건을 대신하지 않는다. 캠페인28·모바일20·소스 검사 통과. Android 빌드와 실제 화면 검사 진행.

표식 효용 비교: 동일한 지점 선택과 2초 구매 시점에서 창만, 표식 혼합, 관측창 조합을 비교하는 `Tools/review-marker-battle.ps1`을 추가했다. 카드 없음 1회 및 카드 첫 선택 3시드이며, 비용·보드 변화·수입은 정책마다 달라 단일 변수 실험은 아니다. 1-11~1-13의 관측창은 1-14 클리어 후 재도전을 가정한다. 1-12·1-14에서 단순 표식 혼합은 같은 자동 정책의 창 중심보다 불리했고, 관측창은 1-11·1-14에서 도움이 되었으나 1-12에서는 실패했다. 표식 추가 피해를 3/5→5/8로 높인 별도 시험에서 차이가 작아 즉시 밸런스 변경하지 않고 원래 수치로 복원했다. 런타임 집계에 실제 체력을 깎은 표식 추가 피해와 표식 대상 피격 횟수를 추가해 추가 원인을 분석한다. 실 플레이나 최적 전략을 대체하지 않는다.

표식·관측창 비교에서 보너스의 단순 증가(+3/+5→+5/+8)는 선택된 자동 정책의 승패를 근본적으로 개선하지 않았다. 원 수치로 복원했다. 재료 재배치·타깃 집중·카드가 다르면 결과가 달라질 수 있어 추가 실험과 사람 플레이가 필요하다. 분석 설계와 사례는 Docs/Design/GATEFALL_MARKER_BALANCE_REVIEW.md.

표식 표적을 다른 공격 타워의 사거리에 들어오는 적 중 선두로 선택하고, 해당 적이 없으면 기존처럼 선두 적을 선택하는 시험을 구현했다. 나머지 공격 타워는 가동 중이어야 하고 방벽/표식은 제외한다. 기존 1-11~1-14 정책 비교 64건 가운데 보너스/별/승패가 달라진 것은 3건으로 모두 1-13이며 승패는 그대로다. 카드 없는 1-13 표식 정책의 보너스 483→503·결계 61→79. 카드가 있는 두 건은 보너스가 늘었지만 결계가 각각 179→166, 118→113으로 감소했다. 자동 정책으로 다수 상황에 개선을 주장하지 않는다. 해당 범위 회귀 테스트는 통과했다.

1-11은 적 총수 102를 유지하면서 3~6구간의 j%6을 정예로 바꾸어 양쪽 정예를 더 자주 제공한다(일부 선두 공성은 정예로 교체). 사거리 겹침을 안내. 카드 없는 시드7 자동 분산 창/표식/관측창 정책은 각각 결계 300/92/287로 모두 승리하고, 집중 배치는 210/161/17. 관측창은 이미 1-14를 깬 재도전 가정이다. 각 정책의 소비·선택 시점·총 타워 수가 달라 차이는 표식 단독 인과가 아니다. 오히려 기본 창만으로 무피해도 가능하므로 이 변경으로 고유 공략의 필요성이 입증된 것은 아니다.

v0.10.8 16:9 1-11에서 배속 버튼이 오른쪽 위 설치 지점을 덮었다. 상단 바의 시계와 재화 사이 간격으로 옮긴 v0.10.9에서 해당 지점을 선택해 창을 건설하고 1×/2× 양방향 전환을 에뮬레이터에서 확인했다. 버튼/타이머/두 재화 칩/보스 경고 간격은 모델 검사, 1-4 전체 보기와의 분리는 에뮬레이터 표시 확인. 다른 화면 비율의 실제 휴대폰은 미검증.


2026-10-28: v0.10.9의 1-11 독립 Android 에뮬레이터에서 결계 300을 유지한 채 헌터 부활 대기와 부활 알림을 확인했다. 모델 재현은 10 전투초를 검증한다. ADB 조작이며 실기기·사람 플레이는 미검증. 현행 경제의 표식 정책 자동 결과는 TestResults/marker-battle-review.txt에 갱신했으며, 서로 다른 지출·배치의 비교이므로 표식 자체의 효용을 확정하지 못한다.

2026-10-29: 최종 v0.10.9 Android 에뮬레이터에서 건설·타워 정보 패널 중 전투 진행, 성장 카드 대기와 명시적 일시정지 시 시간 정지 및 재개를 직접 확인했다. 조합 짝 선택·앱 이탈과 실제 터치는 미검증.

2026-10-29: v0.10.9 Android 1-3에서 조합 가능한 짝이 하나일 경우 짝 선택 화면을 건너뛰고 즉시 뇌격을 만든다. 복수 후보 선택과 진행 여부는 아직 직접 확인하지 못했다.

2026-10-29: 복수 후보가 있는 조합 짝 선택 화면에서 두 창이 표시되고 선택 중 시계가 1:57→1:50으로 진행되는 것을 v0.10.9 Android 에뮬레이터에서 확인. 이후 성장 카드가 우선 표시되어 선택 중단. 실제 짝 탭 후 조합은 미검증.

2026-10-29: 복수 조합 화면에서 “이 자리에”의 위치가 모호해 주황색 시작 지점과 선택한 재료 지점의 결과를 명시했다. Android 에뮬레이터에서 두 후보 중 하나를 선택해 결과 위치·나머지 타워 유지를 관찰. 실기기 및 실제 사람의 이해도는 미검증.

2026-10-29: 현행 1-1에서 두 창만 준비한 에뮬레이터 자동 조작이 결계 무피해 승리하여 추가 배치가 필수라는 안내를 교정했다. 14작전 분산 역할 정책은 다수 무피해이나 이는 최적 전략이나 사람의 재미를 입증하지 않는다. 세부 TestResults/runtime-v01011.md.

2026-10-29: 1-11 첫 해금 합성 프로필에서 Lv1 창 둘·표식 하나 고정 ADB 시도는 139초 패배했으며 표식 유효 보너스 146.8을 기록했다. 동일 지점 창 셋도 3시드 모델에서 패배하므로 더 짓고 강화한 전투와 사람의 판단을 따로 검증해야 한다. TestResults/runtime-v01011.md.

2026-10-29: 1-11 ADB 자동 전투에서 첫 해금 표식과 창 둘을 세운 후 창을 강화하고 창 하나를 추가한 별도 시도는 결계 97.8로 180초 승리했다. 강화만 한 다른 시도는 166.7초 패배했으나 시드·카드 통제는 없으므로 인과 비교가 아니다. 실제 사람과 기기 검증이 필요하다.

2026-10-29 주간: 1-1만 완료한 합성 상태의 1-2 Android 자동 입력에서 창 둘을 동·서 분산하면 결계 258 승리, 서쪽 집중 별도 시도는 177.98초 패배. 시드·카드가 통제되지 않았고 실제 초보자의 이해도와 재미는 미검증이다. 초반은 쉽게 유지하고 1-3을 다음 검토 대상으로 삼는다.

2026-10-29 주간 반복 1: 합성 첫 해금 1-3 Android ADB 시도에서 66초 뇌격 조합 후 추가 배치 없이 결계 153으로 승리했다. 다른 비조합 시도는 227 승리했지만 시드와 카드가 다르므로 조합 효용의 인과적 결론은 불가하다. 초보자가 빈 지점을 이해하고 보강하는지 사람 플레이가 필요하다.

2026-10-29 주간 반복 2: 조합 완료 문구의 화면상 없는 지점 번호를 빈 자리에 다시 건설 가능하다는 안내로 변경하고 v0.10.12 Android 에뮬레이터에서 실제 재건설을 확인했다. 사람의 이해도·실기기는 미검증.

2026-10-29: 성장 카드가 보급과 같은 시점에 열리면 기존 HUD가 보급 메시지를 가리고 카드 선택이 이를 대체했다. v0.10.14에서 카드 선택 후 보급을 다시 보여 주며 독립 Android 30초 화면 확인. `TestResults/runtime-v01014.md`.

2026-10-29: 독립 Android에서 기록된 입력으로 조합·재건설 후 1-4를 결계239로 승리했다. 비조합은 추가 지출이 적어 170초 패배했으므로 두 결과로 조합 강도를 확정할 수 없다. `TestResults/runtime-v01014.md`.

2026-10-29: 1-4의 북쪽 화면 밖 적 태그와 중앙 알림이 겹쳤다. v0.10.15에서 좌표를 분리하고 Android에서 북쪽 적1과 건설 알림이 동시에 보임을 확인했다. `TestResults/runtime-v01015.md`.

2026-10-29: 첫 작전 시작 시 이미 본 것으로 기록되어 패배 재도전에 안내가 사라졌다. v0.10.16은 첫 클리어 전 재도전에서 안내를 유지하며 합성 1-3 재도전 Android 화면에서 표시를 확인했다. `TestResults/runtime-v01016.md`.

2026-10-29: 1-3의 뇌격은 창·봉인 Lv2와 촉매 외에도 마석40이 필요하지만 기존 준비 안내에서 비용이 빠져 있었다. v0.10.17에서 비용을 포함하고 Android 화면 확인. `TestResults/runtime-v01017.md`.

2026-10-29 재개: 1-5 한 고정 지점에서 창과 붕괴의 결계 결과는 같고, 붕괴는 피해를 기록했다. 지점별 범위 공격 효용을 더 확인해야 하며 한 정책의 승패만으로 타워를 상향하지 않는다. `TestResults/stage5-role-review-v01017.md`.

2026-10-29 14:20 KST: 1-5의 지점6 붕괴는 카드 없는 모델에서4609.6 피해·결계300 승리, 같은 지점 창 정책은결계263 승리였다. 모델54회와 별도 Android 보강 정책의180초·결계269 승리로 붕괴를 활용할 수 있는 배치를 확인했다. 가격·후속 배치·카드 차이 때문에 단일 타워 효과나 사람의 재미로 일반화하지 않는다. 수치는 유지한다. `TestResults/stage5-role-review-v01017.md`.

2026-10-29 14:37 KST: 1-6 정기 보강·조합 후 보충 모델은 보스를 출현8~9초 후 처치하고 이후 웨이브에서 주로 결계 손상이 발생했다. Android 보강은 창6개 중5개 강화로 결계256 승리, 보상·1-7·백도하 해금 저장 확인. 보스 처치 시 기존 HUD 체력 표시가 사라진 뒤 남은 방어 목표 안내가 없어 다음 표현 개선 대상으로 삼았다. 사용자의 실제 혼동과 재미는 미검증이다. `TestResults/stage6-review-v01017.md`.

2026-10-29 15:05 KST: v0.10.18의 보스 처치 완료·남은 결계 방어 안내를 Android에서 확인했다. 건설 알림과 분리되고 시계14→10→6초 동안 유지됐다. `ConfigureMobile()`이 버전 번호를 고정하던 중복 설정도 제거해 프로젝트 버전18/31을 보존한다. 실제 사람의 이해도·실기기 가독성은 미검증이다. `TestResults/runtime-v01018.md`.

2026-10-29 주간: 출전 칩은 백도하 선택 시 B급만 표시해 근접 방호 역할을 바로 설명하지 않았다. v0.10.19는 기존 역할 설명을 표시하고 세 헌터의 교체 글자 간격을 Android에서 확인했다. 백도하1-7 보강 정책은 실제 창4개·강화3회로 결계214 승리했다. 빠진 강화의 원인은 게임 입력이 아니라 자동 좌표가 타일 밖에 있었던 것으로 확인했다. 사람의 역할 이해도·재미는 미검증이다. `TestResults/runtime-v01019.md`.

2026-10-29: 확보한Realm `newinitcampaign.lua.txt`의 OnWaveDone/OnEnemyCountChanged는 모든 웨이브 종료·적 수0·생명 양수에서 승리한다. 기존 Gatefall의180초 남은 적 퇴각 설정은 자체 초기 생존 프로토타입 규칙이었다. 사용자 요청으로v0.11.0부터 캠페인은 실제 전멸 승리로 바꾸고 보급 상한은 유지했다. 후보Android1-7에서3:04에도 적3마리와 전투가 남고190.57초 마지막 처치 후 승리, 최종1-1은183.42초66/66·무피해 승리했다. 원본 코드 이식은 없으며 사람의 재미는 미검증이다. `TestResults/runtime-v0110.md`.

2026-10-29 Realm/Gatefall 진행 대조(진행 중): 확보 APK3.4.47의 finishline.lua는 출구 도달 시 life_cost 차감 후 적 제거, Gatefall RaonCombat은 중앙 결계를 반복 타격한다. Realm OnWaveDone은 스폰 완료이며 다음 호출과 생존 적이 중첩될 수 있다. v0.11.0은 전멸 승리만 일치하고 Gatefall의 30초 고정 슬롯·자동 보급은 남아 있다. Realm 일반 캠페인 에너지 차감과 RegularLevelPopup 검사 경로는 주석 처리되어 있어 캠페인 입장 스태미나를 단정하지 않는다. Realm3별은 max_life-2 이상,2별은 절반 이상; Gatefall은 전투 중 최소 결계90%/70%를 사용한다. 공식 Play 설명은 현재300+ levels(모드 포함), 각 월드4타워이며 정확한 최신 일반맵 수와 구분한다. WorldUnlockConfig의 오래된 주석보다 실행 함수 조건을 우선한다. 분석만 수행하며 게임·APK·사용자 저장을 변경하지 않는다.

2026-10-29 상세 대조 완료: Docs/Design/GATEFALL_REALM_GAMEPLAY_DIFF_2026-10-29.md에100항목과상세수치·증거·우선순위 저장. Realm 호출은 골드뿐 아니라 HeroSpellButton 기술 재충전과 HeroCombatPortrait 부활 보충에 연결된다. Gatefall ChapterReached는1-6후2가 최대여서 신규 세희 첫성장(3챕터)/백도하(5챕터)이 현재 막힌다. HasResonance/공명+5% UI는 표식에도 참이 될 수 있지만 ApplyAdjacency에표식효과가없어 표기 불일치 후보(이번Android미재현). 관측창은현재마지막1-14보상으로다음신규일반맵없음,재도전가능. 별갱신은추가별보상없고 반복마핵2만지급. 현재캠페인창은카탈로그10/16·.5초가아닌RawStats14/22·.7초. 구240문서의6작전/3분/헌터생존문장은최신규칙과다름. 100항목/40로컬링크/32해시검사통과. 분석제안을확정구현으로취급하지않고 기존119검증을남긴다.

2026-10-29 자동Android1-3: 현행v0.11.0 첫해금합성조건에서창Lv2/봉인Lv2→67.1초뇌격(북쪽@5)→71.4초빈서쪽@0창보충→125초강화로187.85초90/90·결계300·3별을확인했다. Lv2가되면강화버튼이빠져메뉴열수가바뀌므로Lv1자동좌표를재사용하면다른버튼을누른다. 첫판매탭은확인만요구해잘못된판매는발생하지않았다. 앞선예정행동미수행은게임결함·외부불명입력으로분류하지않고자체자동입력실수로기록. 조합보충의단일인과효과/사람재미는미검증. 이번AVD정리완료,다음119의1-4확인.

2026-10-29 비교서 개선 1차: v0.11.1에서세희·백도하첫성장을실제해금이정표에연결(12/18마핵),강윤기존첫봉인후3회차유지·이전성장보존. 긴잠금이유가UI에서잠김으로축약되어최종은추가성장예정/1-6봉인후로표시. 인접표식은실효공명이없어판정에서제외했고Android거리5.59의두표식메뉴에오표시없음확인. 첫6작전의현재성장/연구총예산은72로90중18남음. 1-4최종실행190.37초128/128·무피해3별,정리중잔여13과북쪽경고동시표시확인. 반복개선다음우선은웨이브명시/예고이며원본전체이식·새운영모드추가없음. 모델/자동플레이의시드·카드차이는인과분석에서분리한다.

2026-10-29 v0.11.2: 단일ActiveGate 안내는다방향실편성을누락했다. 실제Schedule에서방향별/종류별수·출현구간을읽어예고에쓰고경고소속WaveID를별도로전달하면출현표를중복저작하지않고지연입구중복/147초보스슬롯혼동을막는다. 일반/위기28스케줄해시동일·168예고합계검사와Android1-4 4:3의시작/카드후다음예고각1회로확인. 사람에게유용한지/실기기판독성은미검증. 수동호출은고정경제/카드상한과함께별도기획해야함.

2026-10-29 호출후보모델: 실제출현간격8초(보스직전3초)에서전장완전비움조건은216조건중호출0회였다.10초당김은위기1-6/1-14즉시호출각0/9승.6초상한과13초선택창으로줄이고보급선지급을유지한최종후보는일반36/36승/평균약30초단축,위기24/36승(미호출30/36)이었다.자원선지급은조합을앞당기지만잔여적과겹쳐피해가증가할수있어위기노출과자동권장은보류.모바일에서읽을시간/잔여적/선지급안내가필수이며공급시점변경후진행막대도기존Elapsed/30에서바꿔야한다.46불변/전멸·적수/5-3-2상한/Unity컴파일확인,현재API기본false로아직APK미반영.

2026-10-29 v0.11.3 Android 완료: 수동호출이후같은웨이브경고가공급Toast를덮으므로버튼에서본예고는silent로기록하고경고시중복Toast를제외했다.입구VFX/보스공격경고는유지.제목의Ink색글자에어두운그림자를더하면겹쳐읽기어려워그림자도제거. 최종5회30초당김·162.17초전멸무피해3별·5/3/2상한·저장보상확인.시드/카드/지출이달라옛190초전투와단일효과비교는안함.카드직후타워메뉴제목과일시안내근접은후속조정대상. Android Back을Pause로가정하면모바일가드로동작하지않으므로실제화면버튼을사용해야한다.다음116의1-8역할검토.

2026-10-29 1-8 모델: 봉인 감속 자체는 발생하지만 현행 정예/공성 체력과 봉인 가격 때문에 창 대체가 더 유리하다고 확인할 수 없었다. 적을 빠른 무리로 바꾸면 고정 세 타워도 전승하여 후보 폐기. 종류/체력 보존 군집도 개선 근거 없음. 숫자를 임의로 바꾸지 않고 화력 우선·감속 보조 안내로 정정. 봉인 투자 가치의 추가 검토는 남는다. 총3456조건/8집합 원본과짝비교,기획 GATEFALL_STAGE8_ROLE_REVIEW_V1.md. 일반 일시알림은 실제 타워패널과 충돌할 때만 이동하고 보스 공격 경고 위치는 유지한다.
2026-10-29T21:12:43.1274322+09:00 Android v0.11.4 준비 화면:1-8 지점3/4/2 창3개(120/140마석)를 건설했다. 북쪽/중앙 건설 메뉴와 직전 건설Toast가 동시에 보일 때 일반Toast가 메뉴 아래로 이동하여 제목과 겹치지 않는 실제 화면 확인. Docs/QA/v050/v0114-stage8-build4.png 및 build2.png.
2026-10-29T21:35:56.1548789+09:00 127검증계획:마지막호출만137초대실행하면보스출현150→144/예고141로이동,현재웨이브출현/배우타이머유지. 보스자체첫공격예고는출현약5초뒤·1.5초windup으로별도확인해야하며웨이브예고와구분한다.합성기존QA프로필은유효/첫5작전2별·70마핵·연구성장0·강윤·Pending없음.
2026-10-29T22:00:27.4951339+09:00 127분리검증:새포트5572/화면없는GatefallBossIsolated/swiftshader로API35부팅완료.출처불명5558의종료로그는186.40초148전멸/결계256·호출0/추가@7/6건설이므로마지막호출성공이나예정구매정책검증으로사용불가.5558저장/AVD/로그보존,기존5556은외부에서이미종료된것으로관찰했으나원인미확정/본작업종료명령없음.
2026-10-29T22:24:49.5291530+09:00 task127분리Android확인:139.65초마지막호출index5·6초당김·보급선지급/카드3/촉매2,144초대보스등장체력바.연속windup-01실제화면151초대에서문지기공격예고문구/붉은위험원·결계278 확인.보스경고를웨이브예고와구분해직접관찰했다.
2026-10-29T22:54:11.3722140+09:00 128짝비교보완결론:같은위치·구매순서로비교하면봉인높은결계0/같음84/낮음132.초기서쪽봉인의우위6은참조창의다른위치때문인혼선으로기각.지출상한동일과실제지출동일을구분하고카드off시세시드동일결과한계도기록.봉인효용미해결·수치/APK조정보류.마감전전달/실설치APK4개·모델6파일해시및보고서정합성통과.
2026-10-30T10:06:22.6116717+09:00 맵유사성원인:ArenaView의stage%3 및고정배경선택과RaonCombat.ConfigureRaon의3레이아웃재사용.보스는0,6에고정생성되어북도로와도어긋날수있음.새경로데이터와도로렌더공통화,도로위지점금지/실제활성경로사거리검사를계속유지할필요.표시만줄이고지점터치영역/전투사거리/저장ID는보존한다.

2026-10-30 맵 개편 결론: 같은경로데이터로노면/시뮬레이션을그리면입구/길/지점불일치를구조적으로줄일수있다.14개별경로와139지점의이격/사거리/보스도로상출현검사통과. 1-4는바깥배치+늦은투자로패배했으나합류부를선점/보급때보강/나중조합·보충한시도는7타워·204.07초무피해전멸.카드/구매시점도달라위치만의인과증거는아님.초반1-1은창3Lv1/120만으로최종APK184.97초무피해로쉽게유지. 기존튜토리얼의고정지점2/3참조도새맵과불일치하여도로분율기반으로교정. 타워메뉴는의도대로전투진행/정지버튼미노출이므로QA검토중메뉴밖닫기→명시정지를확실히해야함.준비안내의글자분할은129후속.새에셋원본/생성프롬프트보존·Realm직접데이터복제없음.

2026-10-30 전투템포 참고: Random Dice 공식Google Play 소개(https://play.google.com/store/apps/details?id=com.percent.royaldice)는빠른전투/합치기/레벨업/보스웨이브를설명한다. C:/dev/rea_project/random_dice_work/reports/RANDOM_DICE_STATIC_ANALYSIS_2026-10-26.md는정확한적/주사위수치를확인하지못한정적구조보고서다.따라서정확한원본속도/체력을복제했다고주장하지않고Gatefall의새목표로설계한다.공식YouTube영상본문취득은실패하여영상동작을관찰했다고주장하지않는다. 이전0.12.1코어/표현/설계JSON을TestResults/pace-v0130-baseline에복사해같은정책비교기준으로보존했다. 전체REA파일검색은출력이과대해보고서단위로범위를줄였다.

전투템포 모델: 창 Lv1 고정2거리 일반 적 첫발사→처치 1-1 2.23→.53초,1-4 2.94→.53초,1-13 4.36→1.00초. 일반 적 추가에도 초반27 능동정책은 결계300. 조합보충1-8/1-12 실패가 남음. 테스트 .sav는 바이너리이므로 텍스트 Get-Content로 읽지 않고 해시/코어 파서를 사용해야 함. API35 새AVD는 ADB 연결만으로 설치 준비 완료가 아니며 sys.boot_completed=1 확인 필요.

최종0.13.0 Android: 1-1 창3Lv1/120지출로132전멸·무피해.1-4 뇌격1+창6모두Lv2/675지출·194.82초256전멸/무피해.조합91.7초·보충93.3초로실제동작.추가일반적의수입으로후반보강재화확보.1-4효과최대66/220로상한포화는관찰하지못함.평균프레임16.77ms는hostGPU ARM변환/개발APK에서만관측,실기기성능이나청감확인아님. 24대결이며이전48문구는계수오류로교정.현재소스/보고서/최종APK해시확인필요.

0.13 일반 적 한 기당루트+SpriteRenderer7개를보유하고타깃Targets는매쿼리FindAll/Sort/클로저를만든다. 붕괴는정렬비교마다전체Cluster를다시계산한다. 물량5~8배에는해상도축소만으로충분하지않아군집메시와탐색개선을함께검토. 원본자체PNG grunt/runner200x150,brute220x200,siege264x180,boss360x360. Unity TextureImporter.maxTextureSize와Mesh.SetTriangles/MarkDynamic공식문서를참고(https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/textureimporter , https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/mesh/settriangles). 실제기기성능보장은후속이며새픽셀그림생성없이기존자체이미지의Unity임포트크기를줄일예정.

핵심CPU원인확인: UpdateRaonEnemies가모든일반적마다Enemies.FindAll로헌터/방벽에게묶인수를다시계산했다.스텝당한번의집계와증감카운트로교체, 표적후보List재사용/붕괴Cluster점수1회계산·발사중치명피해예약으로중복사격줄임.고정체력/위치12타워의90스텝·768적스트레스는동일피해1686.7998/체력합38398313.15625/투사체0으로구버전435.8ms·22.31MB할당→현재25.0ms·42KB할당(호스트.NET/병렬실행영향포함,AndroidFPS아님).

Unity .meta의맨위maxTextureSize2048은이전직렬화필드로남을수있다. DefaultTexturePlatform.maxTextureSize64/Android.overridden0 및실제Android mobTex64x48을확인해야한다. 현재hostCPU100%/python15개동시실행으로적0준비화면도60~140ms라과거CPU여유시의0.13 16.77ms와직접FPS비교불가. 이번독립VM프로세스만우선순위변경을기록하여검증하며외부작업은건드리지않음.

0.14.0초반330실플레이는185.58초/3창Lv1/120지출/무피해3별/보상10·Valid저장으로확인. 범람카드는사거리/사거리/빛창이며이전0.13순서와다름. AboveNormal이후만분리한1-1실행창4956프레임/평균18.15ms/최악100ms/33ms초과101,최대창끝37적. 이전CPU포화준비/일시정지진단구간은별도원시로그로남김. 0.14.1은코어동일/그리기버퍼최적화만수정,최종Android8배검증예정.

현재 효과는 비뇌격 피격의 금색 원/3파편이 공통이며 대부분한발인일반적은몸체HitFlash를보기도전에사라진다. 발사/피격 위치도 고정+.6/1.4 높이로작은몹/축소타워와차이있어실제그림높이에맞출필요. 공식Play참고URL https://play.google.com/store/apps/details?id=com.percent.royaldice . 영상본문fetch실패를영상관찰로간주하지않음.

공식참고이미지는web URL메타만취득/직접그림표시불가,CUA와node커널초기화는os error3로실패. 원본이펙트프레임/정확한지속시간을관측한것은아님.이번타이밍은Gatefall자체설계. 자체Flash셰이더는UnitySprites.cginc의SpriteVert/텍스처알파/Flip 경로사용,일반군집메시구조변경없음.

새 효과 확인: 1배속실전12초 영상에서봉인탄선/초록교차와감속링,작은적화이트파열/타워발사섬광을관찰. 효과최대35/220·재활용0(초반부분범위). 전체전투/사람평가아님.

최종1-14: 사거리/빛창카드,봉인/창Lv2로뇌격실제조합/촉매소비·90초에Rupture@6+Binding@1실제보충. 계측포격7발/33피격·뇌격16발/44피격·효과70/220·재활용0을부분구간에서확인. 영상17.286초/후속MP4종료검증,별도최종통계는완주후확정.

최종효과검증:0.14.1코어6파일해시불변으로수치/난이도논리유지. 최종1-14혼합7타워(뇌격/봉인/붕괴Lv2+창3Lv2+창1Lv1)로944전멸/2별·269결계/710지출. 창/봉인/붕괴/뇌격발사758/637/52/37,명중754/629/261/74. 95효과피크/220·재활용0/116관측수일치. 실제중력/종결/표식발사0이므로후속Android검증대상. 빛과색을구분하는자체중립알파셰이더는SpriteVert로Flip/외부알파경로를유지한다. 기존PNG청록×금색이초록이되는문제는최종특수탄재질로교정. 호스트경합성능·사람재미한계분리/3녹화정상종료검증/임시VM정리완료.

2026-10-31 v0.16.0: 지점만늘려서는초기140/총보급300에서실제타워수가적게남는다. 공간308/기존139좌표보존과시작240/280/320·총보급400/500을함께적용. 전후동일126조건씩평균타워9→14/혼합39→42승·초반결계300,최근3초사격최대9→10. Android16개운용188.63초944전멸/무피해3별,초반6개181.53초330전멸/무피해3별로확인. 전방포격이먼저정리해후방뇌격은유휴였으나기존판매/재건설로전방재조합후실제발사확인. 타워수를늘리는것과모든타워의동시발사는다르며사람재미판단은보류. 비확장카메라y=.35를투영에빼먹으면약15px오차로중앙경계메뉴방향이달라질수있다. 후보/최종/실제성공기록분리·사용자저장/보호AVD보존·새VM정리완료.
2026-10-31: 네 방향 설계는1-10부터작게검증. 두길/세길외에사방33.61~39.61긴접근로로같은예산모델의사격타워7~11→12~13/최근3초최대8→11. 새패턴효과가난이도폭증아닌참여분산으로나타남. Warning.Value3은기존보스에서도쓰므로Wave태그로구분필수. V2정규화프로퍼티JSON재귀는XY만투영해차단.
0.17 Android: 이벤트Value3은남쪽/보스공유라모든이용자가Wave태그를검사해야함. 후보실화면오보발견→공통IsEntranceWarning/IsBossWarning. 전체E/F단순0가정금지: 부팅AssetPackManager1/EGL협상4로그보존·ES3.1실제생성/전투중추가없음. 네길북쪽8,11은배속버튼과일부겹쳐다음가독성대상. 사용자추가시스템없이16타워·864전멸·기존보급500으로조합/보충가능.
01:38 맵비교 기존126모델 재사용:1-7 양측길35.83/35.83·평균발사5.33/14·최근3초최대6(결계300),1-12는38/29.83으로이미비대칭·8.44/16이므로단순대칭후보로부른것은교정. 다음맵실험은1-7이우선. 길길이·모델참여차이는사람재미증거가아님.
02:09 1-7세후보:공유목2길은24공간부족으로폐기/지점요구유지. 긴3길9조건발사8.11·빈번7.56·185.06초,짧은남쪽3길8.44·8.33·180.40초(기준5.33·4.33·183.24). 모두9승무피해,정책별조합유지/기준·채택평균지출990동일. 추가물량/속도/파형변경없이짧은남쪽채택. 남게이트−13,-11은하단호출·회피와겹침을줄인다. UI준비예산280에서봉인포함7개285불가라6개245부터시작한다.
0.18.0 새1-7은실제14개·552전멸188.6무피해이나후방봉인/뇌격은0발이었다. 같은전투에서판매191+전방@11재조합/보충후뇌격6발12명중으로위치의효과를관찰. 배치수를늘리는것·승리·조합실행과공격참여는별개이며봉인투자가치는아직미해결. 영상은이재조합이전이라효과영상으로오인금지. 다음은160포기저장정합후161위치안내/같은총량파형의작은검토.
160원인은CurrentRunId만비우는지도복귀. 해결은현재ID의명시적포기트랜잭션/성공후화면이동,실패는Pause+ID/원본저장유지. 종료결과는Complete우선으로보상손실방지/기존저장자동청소없음. Android에서새AVD만정확한.tmp경로의빈디렉터리로저장실패를주입하고기존파일이있으면중단할계획.
2026-10-31 task160: 저장 성공 이전 화면 이동을 막는 것이 핵심이다. 새 빈 임시경로 디렉터리로 실제 File.WriteAllBytes 실패를 유도해 Pause/현재 ID/기존 저장 해시 유지와 원인 제거 후 같은 버튼 재시도를 Android에서 확인했다. 정상 포기와 기존 보상/해금은 바이트 단위로 독립 검증했다. 과거 pending 자동 청소와 저장 형식/백업 정책은 건드리지 않았다.
03:49 조합 안내 실검증 교훈: 상대1개 즉시조합 분기는 범위 미리보기를 우회했고, 기존Toast는 새 FusionPick 패널을 회피대상으로 받지 않아 겹쳤다. 두 실제 후보 실패를 보존하고 단일 상대 선택 통일/기존NoticeBox 재사용으로 해결했다. 도로덮음 길이만으로 투자위치 추천은 부적절: 후방덮음이 더 길어도 선행 처치로 뇌격0발, 같은 예산의 전방은93~109발. 봉인0은별도후속. 촉매 전 비활성 조합 버튼 뒤 지점선택은 새 입력 후속162.
2026-10-31 04:06 패널 차단은 활성버튼 뒤/필드지점 앞의 None 영역으로 해결할 수 있었다. 개별 비활성 버튼만 막으면 제목·타일틈이 계속 통과하므로 패널 전체를 막고 활성버튼 우선순위를 유지한다. 기존 입력 코어 수정 없이 실제 Android 준비·LIVE 전후 검증과 활성/외부 행동 보존을 확인했다.
2026-10-31 04:45 봉인0발은능력치만의문제가아니다. 같은정책·예산에서재료/보충위치까지전방으로옮기면64~404발과강한/빠른적감속표본이생겼다. 모든후보가이미무피해여서승리나투자우위를단정하지않고수치변경은보류했다. 대상상한을기존메뉴에표시하고동16실제78발78명중/감속고리로사격가능성만확인했다. 다음에는동일조합후보충종류만바꾸면레시피변경혼선을피하고45대40의실제5마석기회비용을분리할수있다.
2026-10-31 05:11 보충 한개의 선택만 바꿔도 발사 수와 실제 피해/정리 속도는 반대로 움직였다. 동16 봉인은242~257발·감속을 주지만 창132~136발이 더 큰 실효피해와4.6~4.78초 빠른 정리를 만들었다. 다만양쪽이미무피해라모든상황의우열/재미결론은아니다. 이전지표에없던ID별피해는읽기전용코어관측과원본동등성·한조건재구성검증으로보완했다. 제품수치변경을보류하고현재APK보스후속을우선한다.
2026-10-31 05:50 현행1-6은 보스가 죽은 뒤에도 잔여 적 HUD와 전투가 유지되고180초에2마리가 남아180.75초에전멸했다. 최종 호출은보급/카드를앞당겨도적중첩·배우타이머를건너뛰지않는실제흐름을확인했다.14타워940지출에서보스결계22피해/헌터32피해·3별이며회피입력성공은주장하지않는다. 한정정책의정상완주가모든전략균형/사람재미근거는아니므로수치변경은보류하고남은UI가독성후속으로간다.
2026-10-31 06:19 준비 안내의 내용이나 글자 크기를 줄이지 않고 표시할 때 문장 구분만 개행해16:9의끝글자다.만남는문제를해결했다.넓은화면/1280x720도두문장완전표시를확인했다.완료범위는1-8준비/짧은입력이며다른도시문구/전투전체/사람가독성으로확대하지않는다.이후맵후보선정은과거0.17.0결과만집계해1-9발사7.22/14가낮음을확인했고,현행조건정합을확인하기전새검증으로쓰지않는다.
2026-10-31 06:39 1-9의 긴 북쪽 합류로는 조합 정책의 발사 지점만 늘었고 다른 두 정책/빈번 발사와 정리가 나빠졌다. 길이/합류/최대동시참여 하나로 채택하면 사용자 목표와 어긋날 수 있어 폐기했다. 남쪽 공유 도로는 기존24지점 간격/사거리 제약을 유지할 수 없어 수를 줄이지 않고 기하 단계에서 폐기했다. 다음에는 경로를 고정해 같은624총량의 후반 도착 밀도를 분리 검토한다.

2026-10-31 task168: 길을 늘린 후보가 참여를 낮춰 폐기한 뒤,경로를 고정하고 일반1-9후반 도착만22→12초로 모았다.평균발사7.22→8.11/빈번6.67→7.33·압력표본최대29~32→66~70이지만 몰려온 뒤 빈 구간과 조합 구매순서 변화도 생겼다.빠른 정리의 일부는 마지막 출현이 앞당겨진 효과다.현재 Android14타워/조합2·624전멸174.18초/무피해·보상/재실행 저장까지 확인했으나 사람의 템포·투자효용 평가와는 다르다.LIVE메뉴를 긴 시간 검토하면 카드와 입력이 겹칠 수 있어 시도A를 정상 포기로 제외하고 확인 전에 명시Pause한 시도B만 완주로 기록했다.150초7타워무피해를 저투자 승리로 일반화하지 않는다.다음은 같은 카드/스킬·배치정책을 맞춘 저투자 대 보강의 제한 비교다.

2026-10-31 08:12 task169모델: 같은mixed초기6/첫카드/자동스킬·3시드에서270초기유지6개와380상한7개모두624전멸·결계300/접근표본0·피해이벤트0.6개192.37~196.65초/잔여784,7개187.38~191.97초/잔여674.기존14개1040지출174.87~178.33초무피해와대비.보강은정리속도효용이있으나이조건생존필수는아님.선택seed7전체관측1은기존결과·원장·지점별사격까지일치.정적예산군에재화를삭제하지않았고자동스킬은단순보호뿐아니라강윤이동도포함하므로배치/정책범위를넘겨일반화안함.

2026-10-31T08:40:38.3071738+09:00 task170첫후보: 후반정예/공성속도1.25는초기6개seed7·41패배177.95/178.05,seed19결계228승리195.95/14개mixed3시드모두결계300승리175.90~179.25.1.40은seed7·41패배153.40/153.45로압력이더이르고seed19결계112.8승리197.78·14개mixed모두결계300.아직제품채택전이며1.25의7개제한보강/다른14개정책과생성범위정합을추가확인한다.

2026-10-31T09:12:19.4101059+09:00 task170: 물량·HP·재화를그대로두고후반기존정예/공성12마리의속도25%만늘려6개/7개/14개투자차이를만들었다.6개1승2패·7개3승222~300·14개9조건무피해로중간보강의여유가남는다.중첩관측은14개seed7에서오히려129.85→122로줄어보편중첩증가가아닌결계압력차이로해석한다.고정구매순서는같고시각만-1~+2초변했다.실제새APK14타워1조합·624전멸173.42초무피해/보상·해금·재실행저장을확인했다.QA빈공간2200,820이회피와겹치는사실을발견해2250,400으로교정/이전무스킬주장도철회했다.원시결과와입력정책정확성을분리기록한다.

2026-10-31T12:07:21.4304851+09:00 현행1-9에서조합이단순히공격하지않는문제가아님을확인:뇌격0이49~71발이어도기준보다생존/정리가개선되지않았고동쪽창9·서쪽창20/붕괴강화가효율적이었다.전선별투자배분을분리하기전조합수치를올리지않는다.코어CanFuse는재료의두위치중Anchor를선택할뿐두재료간거리제약이없으므로같은재료의12앵커/다른재료위치9앵커를구분검토할수있다.

2026-10-31 0.20.0에서긴길만추가한서/북우회보다동쪽입구추가가참여/빈구간에유리했다.하지만실제14타워1-8에서뒤쪽뇌격12와봉인은0발/0피해였고창904발이대부분을처리했다.지점수가늘고조합생성/강화가성공해도투자가치가보장되지는않는다.앞쪽재료/배치선택을같은정책·상한으로먼저비교하고0.20의모든조합을버프하지않는다.도로중심선2는연석이붙어보일수있어최종3분리/실제Android확인을함께사용한다.

2026-10-31 task194:1-12실제완주에서전방21뇌격5377.307피해/표식118연계와붕괴22피해0을동시에관찰.16개건설이나개인유효피해만으로투자우위/모두사격을주장하지않는다.명시첫카드/시드가없는Native를정확한모델재현으로쓰지않고위치교환정책으로원인을후속분리한다.QA투영은1-4외카메라Y=.35와실제safe-left132를반영해야하며중간선근처메뉴위/아래반전이가장큰오류였다.30초카드정지중필드좌표는카드선택이될수있어공급임계전후에는입력배치를나눈다.

2026-10-31 task195/196:같은카드·28구매1130/32잔여·3시드에서붕괴22는0발,입구6은73~74발3919~4032/정리.87~1.80초단축이나전체최근사격일부감소.초기창도교환하므로단일붕괴좌표인과아님.코어버프없이기존1-12Lesson만구체화했다.새Native약57초에전방붕괴13발48명중/LIVE·업데이트/정상포기저장동일을확인했으나16T전체완주는0.20.2자료/새0.20.3검증과분리한다.

2026-10-31 tasks197/198:북입구와상단표시충돌은상단최대경고고정가드와150폭대체배치로56기하/세실제화면에서개선했다.140폭의Pillow143추정과Unity139.3125실측은구분해야한다.안내열람기록이0인합성보스저장은StartRun에서TutorialMask32가정상저장되므로포기뒤초기바이트완전동일을요구하면잘못된실패다.실제수정필드전수비교로다른필드동일/포기재실행동일을확인했다.초기Androidframework재시작/앱재생성은게임앱Fatal원인으로단정하지말고PID/boot_progress/시스템helper와hostColorBuffer원문을별도보존한다.

## task199 — Android 재시작 원시 진단 (제품 변경 없음)

2026-10-31 18시대. `v0204-north-resume-diagnostic-logcat.txt`의12444~12471행에서08:43:19.228UTC Watchdog가 system_server577을 종료한 직접 경로를 확인했다. android.fg가68초 막혔고 GNSS native_stop/reportStatus→GnssStatusProvider→ListenerMultiplexer.deliverToListeners에서Object0x0115c7ca 잠금을 기다리고 있었다. 직후SIG9/새system_server7490이므로 단순 게임 앱 크래시라는 분류를 사용하지 않는다. 잠금 소유자·유발 클라이언트/게임과의 간접 관계·실기기 영향은 여전히 미확정이다.

실제0.20.4 APK `aapt dump permissions`는INTERNET하나이며 위치 권한이 없다. 제품C#의Input.location/LocationService 참조도0이다. 이것만으로 간접 상호작용까지 배제하지 않는다. 호스트gfxstream/WHPX/AMD RX9060XT와타임스탬프없는ColorBuffer4쌍·context복원1건은 별도이며 GNSS잠금과 인과를 연결할 수 없다. helper실패를vold/keystore2사망으로 해석하지 않는다.

Tools/review-framework-restart-v0204.ps1 PASS로원시해시·Watchdog완전스택/타임라인·실제APK권한·18제품소스/APK불변을확인했다. 새게임없는AVD대조는 실행하지 않았다. 직접 재시작 경로가 확보되어 맹목적리사이즈 대조·모듈추가/새APK를 하지 않고 upstream원인미확정으로 남긴다. 소유기기는종료됐고 전용폴더의41/14430바이트로그2개잠금만정리보류다. 다른프로세스핸들·보호기기와저장은건드리지않았다. 기능 검증198과 전체안정성/실기기한계를 분리한다.
2026-10-31T19:07:50.8429479+09:00 1-13/203-204:초기425원장후같은40마석의동쪽창·동쪽뇌격은결계피해를바꾸지않았으나북13창은3시드모두무피해였다.조합공격수/개인피해와현재취약전선보강을분리해야한다.방향선택만으로낮은지출무피해가가능한것은유효한공략이며일괄너프근거가아니다.표식북7은180발이상이어도연계보너스0인시드가2개여서사격수만으로지원역할판정금지.
2026-10-31T19:59:26.8911837+09:00 209:해금후1-13의같은195재료·조합/총1050·16타워에서뇌격과관측창의피해/표식연계구성은달라도결계·시간은같았다.현재병목이그전선밖에있으면개인피해/연계효과개선이전역정리시간으로전환되지않는다.초기봉인→표식과후속강화종류도함께바꾸는전체구매정책이라는한계를유지한다.
2026-10-31T20:13:25.2525656+09:00 211:동일패배결과라도한정책의피해출처를직접읽기전에는약한갈래를단정하지않는다.관측된seed41 observer13은동쪽공성187.2+정예112.8=실제300피해였다.이사례에서북조합개인연계보너스증가가결계를지키지못한이유는동쪽공백이었다.원시CoreHurt.Value는마지막overkill도포함해요청306.8이므로체력차감실효300과구분한다.
2026-10-31T20:27:34.1611471+09:00 task212: 결계 Source 메타데이터만으로 기존 알림에 피해 방향을 넣었다. 카드/기존Notice우선 유지·모델 결과동등/실제북쪽표시 및 4저장동일 PASS. 한 이벤트 방향이지 동시 모든 전선의 위협 요약은 아님. 버전0.20.6/63/0237b910… 최신검증 전달, 전투수치 변경없음.
2026-10-31T20:52:10.0270598+09:00 task213/214: 최소폭 경고를 해금/공명 가능성과 구분해 표식 false positive를 제외했다. 실제 좁은 초기4타워의 줄바꿈은 최소폭352로 해결했고 문구·폰트·전투는 유지.0.20.7/64/f44822fe…·봉인6Native메뉴 및짧은Lv2LIVE/입력/4저장동일·원장85/1T·전체전투아님.
2026-10-31T21:06:53.1001799+09:00 215/216: 충분한16T1050는즉시호출30초단축/압력증가도무피해였고,좋은북13보강9T465는선택41에피해24만발생·모두승리/잔여0호출은4.8~5.2초단축하며무피해.호출은시간/중첩선택으로동작하나고정구매정책·세시드한정/모든배치안전·투자우위나재미평가로일반화하지않음/제품수치그대로.

2026-11-01 task237:같은게이트에서서쪽우회길만39.83→45로늘려도지형순위구매정책의앞선처치/후방참여는개선되지않았다.세정책중혼합사격10→8/최근8→7·정리약4.92초지연으로폐기했다.단순길이증가를재미/참여증가로가정하지않고겹치는공통사거리·접근구조를다음후보의독립근거로삼는다.
2026-11-03 현재메뉴6타일은908ref·2400safe929에서58.6%,1920safe959에서75.6%가로를차지한다. LIVE이므로정보메뉴와명시Pause를분리하고조합목록을기본3액션에서열도록축소한다. 기존1-12 cap7/16은동일카드·구매prefix에서모두승리/630추가투자정리11.4~16.1초만단축,현재기준을다시정합한뒤후반접근·타워기여를검토한다.
2026-11-03 265:현재기준8조건확인/전방추가지점8은16창발사수+2/+4지만7창仍무피해. escort1.6속도단독8/비대칭합성8도7창무피해·16발사14/13으로참여개선하나보강압력미해결. 전체HP상향대신후반기존정예/공성에만1.6HP/1.6속도로동행압력한후보를분리검토한다.기본군집/초기3파·총량/경제유지.
2026-11-03 265선택36모델/기준36:16상한세정책×3시드×두작전18조건모두300.7창은1-11 129.2~286/1-12 132~228로버티고16창300.7표식은4/6패배·7뇌격6/6승160~286/204~276. 저투자실패만으로채택하지않고보강승리/사격참여·조합대안함께확인.16창발사1-11 12→15/1-12 8~10→14·10발이상13/12~13. 실제고정카드·좌표인과아님/카드실값원장보존. 전체보통몹수치·경제보존·후반정예/공성만속도1.6/HP1-11 1.35·1-12 1.6 후보선택.
2026-11-03 실제0250Ready에서큰초록사각받침이아트와어긋나고중앙상단랜드마크가웨이브헤더뒤에놓임을발견.첫후보원문/PNG/APK를보존하고받침제거·상단중앙앵커제외로교정.지형/압력/메뉴코드는같고0.25.1/76으로실전검증할최종후보를구분한다.
2026-11-03 최종후보0251실Ready는경로동남진입/도로밑훈련코트·지점24를확인했고초록받침삭제·상단중앙헤더뒤랜드마크제거를확인. 기본3액션세화면/복원조합목록촬영실행·현재최종첫전투시작전. UI모드와실화면은캡처로순서확인한다.
2026-11-03 검증정정:1-11의90카드임계가LIVE조합선택리뷰를덮어첫조합취소/뒤입력선택·카드정체미기록.첫Range/150Ballistic/실816승리와분리.1-12는임계무입력으로3카드전부확인/실880승리.두판1050/16개/2조합이라도동시16사격·동일카드정밀모델재현이아님.새25.1 장식교정/단일요청5개선전달.8/12개ActiveCombat창 끝적18/35는최대동시전체/96FX스트레스가아님.성능JSON의NoRecording라벨잘못사용을원시그대로재요약해녹화포함으로정정/Stage12StartupNotCaptured명시·제품/전투변경0.정리2잠긴작은로그는외부핸들종료없이보존.
2026-11-04 269. 기존 전달 검증 PASS. 전투 시뮬레이션은 이미 1/60초 단위로 처리하고 2배속은 전투에만 적용한다. 기본 입력 배속을 1.25로 바꾸고 HUD를 함께 표시한다.
Task270. 카드가 타워 메뉴 상태를 유지한 채 입력을 가리고, 카드 적용 직후 조합 상대와 사거리를 새로 계산한다. 실제 감시망 확장 직후 추적 범위 5.2→5.7과 Lv3 재료 선택을 확인했다. 초기 최대 강화 범위는 모델로 검증했고 후반 투자 효율은 후속 평가 범위다.
271 조사. Random Dice 공식 스토어와 공개 전투 캡처에서 경로 위 숫자와 간결한 대비, 공격 계열 구분을 방향으로 참고했다. 원본 프레임의 효과 지속시간은 확인하지 않았다. Gatefall은 일반 적을 메시로 묶어 체력 표기가 없고 상세 적은 피해 후 작은 바만 나타난다. 기존 타워 반동은 수직이며 새 추적 종류가 효과 카운터 고정 상한에서 제외된다.
271 최종. 일반 적 체력바를 공유 메시로 표시하며, 정예와 공성 숫자는 최대 8개와 실제 HUD 예약 영역으로 제한한다. Android 1-11에서 서쪽 위 정예 HP를 읽었고, 1-6은 보스 체력 감소와 처치 충격파·잔여 전멸 승리까지 확인했다. 기존 공격 예고 토스트의 짧은 잔류를 후속 문제로 기록했다.
272. 문지기 공격은 생존 보스의 BossWindup 상태로 판단하면 실제 붉은 원과 같은 시기에 해제된다. 2배속 전투에서 준비·공격 직후·처치 이후를 구분했다. 대표 근거 runtime-v0272.md.
273. 1280×720 안전영역 높이639에서 1-11 양쪽 중량 HP와 일반 적 바를 확인했다. 확인 범위는 1:20 부분 전투다. runtime-v0272-small.md.
274. 1-11의 비교에서 조합 후 빈 자리를 메운 조건의 결계가 세 시드에서 좋아졌다. 가격·자리·촉매 차이가 있는 제한 결과다. beacon-choice-v0272-review.md.
275. Android 1280×720에서 추적의 Lv3 미리보기 및 빈 재료 자리 안내가 메뉴 안에 표시됐다. 1:46과 1:47은 부분 전투이며 모델의 정확 재현이 아니다. 화면 반대편의 정예도 메뉴를 열면 숫자가 사라진 점은 Task276에서 개선한다.
276. 메뉴 상태의 실제 패널 경계만 사용하면 반대편 중량 적의 HP를 유지할 수 있다. 1280과 2400의 대표 캡처를 runtime-v0274.md에 기록했다.
277. 2배속 밀집 화면에서 HP와 피격·폭발이 확인됐다. 녹화 전 긴 프레임을 관찰했다. 현행 효과는 태어난 프레임에도 이전 시간 최대50ms를 공제한다. 이 수치의 영향은 소스에서 계산했으며 해당 녹화 프레임에 원인을 입증한 범위는 아니다.
278. 풀 효과가 생성 이전 시간으로 초기 감쇠하는 동작을 고쳤다. Android 영상에서 효과·정지·합성 저장을 확인했으며 프레임 단위 차이와 폰 체감은 미계측이다.
279. 첫 1-1에서 세 창과 추천 자리 한 강화로 승리했다. 창만 가능한 단계에서 빛창 카드가 잠긴 조합 네 종을 언급했고 각성자 가속 설명이 실제 간격식과 달랐다. runtime-v0275-stage1.md.
280. 1-1과 창 없는 1-11에서 카드가 실제로 가능한 결계주를 설명했다. 두 Android 창에는 제어 카드가 없어 제어 후보는 호스트 회귀로 확인했다.
281. 기본 135에서 존재하지 않는 표식 조합 지시를 재현했고, 1-11 안내를 실제 편성에 맞춰 분리했다. 정규 전투 능력치 조정 근거로 사용하지 않는다.
283. 1-3 뇌격의 보상까지 확인했다. 상대 선택 배너가 북쪽 봉인12를 덮고, 가린 패드를 누르면 동작했다. Task284는 같은 실제 판에서 가시성과 선택을 재검증한다.
284. 네 화면 투영과 1-3 Android 두 화면에서 재료 탭 영역을 보존했다. 새 버전에서는 부분 전투만 수행했다. 285. 같은 도시의 1-4 작전 브리핑에서 마지막 다.가 홀로 다음 줄에 표시됐다.
285. 이전 14개 중 10개가 단일 계획 행 폭을 초과했다. 문장 단위 줄 구성을 하고 최종 14개 최대 폭 324.74/344·높이31.59/50을 측정했다. Android 전수 재현은 하지 않고 처음 확인한 1-4 두 화면을 검증했다. 제한 환경의 ADB 실패는 호스트의 소유 기기 연결 성공과 구분한다.
286. 홈 화면 복귀로 열린 레시피가 사라져 다음 입력이 건설에 도달했다. 호출 후 다음 성장 시각은 고정 90/150이 아닌 84.37/144.37이었고 84.37초 카드가 QA 재개 좌표로 선택됐다. 카드 정체는 미기록이며 외부 입력이 아니다.
287. 앱 홈에서 실제 정지 후 레시피와 상대 메뉴가 복원됐고, 판매 확인은 풀렸다. 288. 준비 안내판을 하단 한곳으로 옮기는 투영도 여러 지점을 가린다. 상·하 고정안은 채택하지 않는다.
288. 432×64 이하 축소를 강제하기보다 후속 UI 설계에서 전용 영역과 카메라 영향을 같이 다룬다. 64/90은 투영 후보의 보수적 기준이다.
289. 방벽 LIVE 메뉴의 범위 적 수·0발로는 실제로 누가 멈췄는지 알 수 없었다. 코어 읽기 모델은 2정예 저지와 공성 이동/재충전 상태를 분리 확인했다.
290. 재충전 중 적이 지나가는 이유와 Lv2의 90내구·6초 재충전을 메뉴에서 확인했다. 1-5 일부 전투이며 제품 수치 변경은 없다.
291. 첫 1-6 마지막 성장 카드에서 보스 HP 막대와 성장 제목이 닿았다. 실제 PNG 기준이며 공격 예고/전멸은 별도 영상/원장으로 검증했다.
292. 카드 뒤 1280 LIVE 창 메뉴가 보스 HP 왼쪽을 가리는 별도 문제를 보존했다. 293. 페이지1에서 여전히1-7 출전 안내, 위기1-7에 일반 첫 보상22를 표시, 페이지3 전체 잠김에도 이동 가능함을 Android로 재현했다.
293. 위기 준비 저장은 실제1-7/index6이었다. 일반 첫 +22와 위기 +2는 이제 실제 지급과 같은 계산이다. Android 보상 수령은 미실행이다.
294. 보스 체력·경고와 LIVE 패널을 함께 읽을 수 있었다. 위기 결과 저장은 위기3별과 일반18별을 분리하지만 도시 상단 표시는 아직 일반 별만 보였다.
Task295. 위기 지도 총계에 일반 별점이 나오던 것을 분리했고, 실제 위기 승리 뒤 3→6별/92→94마핵·일반 기록 유지까지 저장에서 대조했다. 위기 실패 화면은 별도.
Task296. 첫1-7의 방호 직후 실제 버튼 12초는 보호 기간이 아니라 재사용 대기였다. 두 값을 UI에서 분리하는 근거로 v02715-stage7-baek-shield.png를 보존했다.
Task297. 첫1-8 포기의 기존 TutorialMask 반영은 예상된 저장 차이였다. 업데이트 전 저장에 그 비트 하나만 반영한 해시가 포기·백도하 복구·재실행과 일치했다.
Task298. 동액 창 추가와 뇌격 조합은 다른 결과 타워 수와 사격을 만들었지만 주어진 저압 모델의 승리·정리는 같았다. 발사 지점과 개인 피해만으로 투자 우위를 판단하지 않는다.
Task299. 남쪽 첫 출현 고정, 마지막 출현이10초 빨라지는 집중을 채택했다. 모델 최대 생존 적 +6~7, 실제10타워·두 조합 승리. 다른 입력/카드인 이전 APK와의 정리 차이를 파형의 인과 효과로 쓰지 않는다.
Task300. 첫1-8 준비 안내판이 북쪽 지점13과 방벽 몸체를 가렸지만 실제 탭으로 건설을 열 수 있었다. 위치를 옮기는 기존 후보의 충돌보다 시야/브리핑 분리를 검토한다.
Task301. 지점을 찾을 수 있는 준비 화면과 출전 전 안내를 분리했다. 전수 Android 판독이나 사람 가독성은 아직 검증하지 않았다.
Task304. 실제 첫1-11은 90초 직후 서쪽에 강화가 집중됐고 124.14초부터 동쪽 결계 피해 표본이 있다. 동쪽 건설131.6/133.6초는 너무 늦었던 후보이며 같은 예산의 순서 비교로 분리한다.
Task305. 동일 비용·지점의 구매 시각을 바꾼 모델에서 동쪽 선보강으로 누수가 사라졌다. 후반 카드와 승리 이후는 분기된 관측이며 이 배치 외 최적화가 아니다.
Task306. Android 선보강 후 이전 누수 구간을 무피해로 통과했다. 첫 카드 순서·후속 투자 차이 때문에 실제 두 런만으로 인과를 분리하지 않으며 Task305의 동액 통제 비교와 구분한다.
Task330. 전장과 건설·편성은 V6 기본 타워 텍스처를 공유한다. 표식/관측창은 기존 별도 생성 경로, 추적은 표식 텍스처를 공유하므로 기본 표식 변경을 전장 효과와 구분한다.
Task330. 기존 원본은 창/표식의 금색 첨탑과 봉인/방벽의 청록·청색이 축소 시 유사했다. 자체 V6에서 금색/옥색/주홍/청색/자주색 변형과 표식 조준기 헤드를 만들고 V12에 우선 배치했다. 기존 추적은 원래 키를 유지한다. 첫 AVD 스크립트 복용의 PSScriptRoot 오류는 작업 경로를 명시해 재시도한다.
Task330. 1280×720 실제 편성 전후와 전장 이미지에서 금색 창/자주색 표식, 옥색 봉인/청색 방벽 구분을 확인했다. 두 작전 부분 검사와 Unity 리소스 검사이며 실기기 색감 및 사람의 식별성 평가는 미검증이다.
Task331. 방벽 파괴 순간 두 적 해제와 35%/1.6초 사거리 감속. 봉인과 표식은 기존 효과가 실제로 일어나는 지점을 사슬과 섬광으로 보여준다. 1-5 단일 방벽 시험은 화면 확인 중 전투가 진행되어 46.72초 패배했다. Lv2는 별도 준비에서 검증했으며 새 제품 밸런스 전체 승리와 분리한다.
Task332. 전투 자원 마석/공명 촉매와 영구 자원 마핵은 별개다. 현행 연구 최대2(1-5 이후), 강윤 최대3(1-6 이후)·세희/백도하 추가1이며 1-14까지 마핵을 쓰는 다음 성장 구간이 없다. 종결 네 경로는 실제 가격과 단계·보급으로 재계산한다.
Task332. 현행 1-14 진입 286마핵 중 모든 연구·헌터 성장 상한은 72다. 9기680 동일 정책의 1-14 계정 성장 1/3→3/3이나 1-13까지 격차 작음. 북쪽 재료4/1/7 종결51발/모두300이지만 동액 창 둘도 무피해·더 빠른 정리. 배치 검증·종결 효용·성장 격차를 분리한다.
