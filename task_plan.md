# Gatefall Unity prototype

## Goal
Improve the Android mobile campaign through the approved Realm/Gatefall comparison, preserving saves, easy opening operations and the wave-clear victory rule. Plan, implement and verify small changes continuously until 2026-11-01 08:00 KST; from 07:30 prioritize closing verification. Earlier deadlines are history. Human play and physical-device checks remain deferred; historical prototype phases below are retained as history.

## Phases
1. Inspect workspace and available Unity tooling. [complete]
2. Create an isolated Unity project and deterministic combat model. [complete]
3. Implement procedural arena, actor visuals, input, sound, and Korean HUD. [complete]
4. Run executable combat checks and review Unity integration; document unavailable verification. [complete]
5. Package project, controls, setup instructions, and validation evidence. [complete]
6. Verify the installed Unity Editor, fix integration failures, and build Windows player. [complete]
7. Inspect actual runtime visuals/input and refresh delivery artifacts/documentation. [complete: Windows smoke checked; follow-up superseded by mobile target]
8. Convert controls and HUD for landscape Android/iOS, safe areas, multitouch and app lifecycle. [complete]
9. Add mobile build/export settings and test input/layout plus Unity compilation. [complete]
10. Refresh the mobile project archive and document device/build prerequisites. [complete]
11. Install Android Build Support and matching SDK, NDK and OpenJDK using the official Unity installer. [complete]
12. Build, inspect and deliver an ARM64 development APK, documenting device-test status. [complete]
13. Review the user's PC playtest feedback against current enemy, map and economy rules. [complete]
14. Propose a coherent expansion for map, defense difficulty, tower upgrades/combinations and mobile controls, separating proposals from approved requirements. [complete]
15. Revise the design around the user's required in-run recipe fusion, using independently designed content and existing Gatefall constraints. [complete]
16. Specify tower progression, recipes, fusion transactions, mobile UX, wave pressure and an affordable three-minute economy. [complete]
17. Save the revised design and structured planning data; verify recipe reachability, purchase budgets and documentation links. [complete]

## Decisions
- User has now installed Unity 6000.6.7f1. Continue with that installed version, recording the upgrade from the initially pinned 6000.3.27f1. Only claim validations actually performed.
- Use a dedicated gatefall-unity folder; preserve all unrelated existing workspace work.
- Built-in 2D rendering, procedural graphics, no paid assets, and no extra gameplay packages.
- Keep combat in engine-independent C# for meaningful automated checks with PowerShell Add-Type.
- Use a 180-second run, WASD/arrows, nearby construction sockets, automatic shooting, pause and restart.
- Single tower type; upgrades and persistent progression are outside this initial scope.
- User clarified Android and iOS are the target. Use landscape for the existing wide defense map, touch joystick, large construction/pause/range/audio buttons, and keyboard only as an Editor fallback.
- Installed Editor currently has Windows and WebGL support only; Android/iOS modules are absent. Do all available implementation and validation without claiming APK/IPA/device tests.
- User explicitly authorized installing the required Android modules and creating an APK. Install the modules matching 6000.6.7f1 and verify the resulting package.
- 2026-10-25 playtest feedback: user can hold enemies with the hunter; considers that acceptable, wants a larger map, harder defense, tower combinations/upgrades and broader feature planning.
- This follow-up is design work. Expansion details and tuning values in findings.md are proposals; the delivered executable/APK remain version 0.2.0.
- 2026-10-26 revision: recipe-based consumption of towers to create stronger towers is now required in the next playable expansion. It is not deferred behind passive combat synergies. Tower strengthening remains a separate action, including upgrading fusion results.
- Keep 180 seconds for the next validation build. Plan three base towers, three fusion results and one capstone with three recipe routes. Numeric balance and exact content are proposed implementation inputs, not already implemented features.
- Store the revised product specification in Docs/Design, outside Assets. Read the existing reference analysis for principles; create new game data, names, visual direction and code rather than importing extracted source/assets. Do not claim a legal clearance or clean-room process.
- Revision completed: Docs/Design/GATEFALL_COMBINATION_DESIGN_V2.md is the current expansion design; combination-balance-v2.json contains proposed numerical inputs, and combination-validation-v2.json records arithmetic/structural checks. Runtime code, scenes and builds remain unchanged.

## Errors / constraints
- 2026-10-26: The skill completion checker expects `### Phase` headings and reported16/0 for this existing numbered-list plan. Read the checker and separately validated all17 numbered phase statuses as complete; preserved the established plan format. This is a checker-format mismatch, not unfinished design work.
- No Unity Editor/Hub at standard locations, confirmed by user.
- dotnet executable is present but no SDK appears in --list-sdks. Use the available C# compiler through PowerShell.
- Download of the open-license Korean font failed due to sandbox DNS restrictions. Retrying the narrow font/license download through approved network escalation.
- Scoped download escalation succeeded; unmodified NanumGothic regular and OFL bundled and verified.
- Initial mobile patch was rejected because it repeated one update target; combined into one target operation and applied successfully.
- Tablet coordinate test exposed float roundoff yielding a reference width just below 960. Clamp reference dimensions to their minimums; all 16 mobile checks now pass.
- Unity modules.json contains keys differing only by case; read it with ConvertFrom-Json -AsHashtable.

## Approved implementation — v0.3.0
18. Inspect existing source, design and build tooling. [complete]
19. Implement tower catalog, upgrade/fusion/sale transactions, attacks, six waves, cards and finite engagement. [complete]
20. Implement touch-safe build/upgrade/recipe menus, camera/map warnings and distinct tower visuals. [complete]
21. Verify transactions, targeting, economy, input and scripted combat; fix failures. [complete]
22. Build and smoke-test Windows and Android; verify package and update delivery instructions. [complete]

User approved the v2 specification for implementation. Existing one-tower and design-only decisions above are historical. Use original Gatefall runtime code and procedural assets; reference existing Realm analysis for uncertain behavior. No original extracted asset/code import.

- Patch tool rejected delete and add of the same file in one patch; applied sequential operations using the source backup. Core now compiles with PowerShell.

23. Generate and integrate original modern-fantasy artwork, preserving combat readability and documenting prompts. [complete]
- Automatic approval review failed for an escalated ADB device query due to account usage limit. No ADB action executed. Continue sandboxed edits/assets/tests; build verification remains subject to tool availability.

- Runtime art QA found blank art: minimal generated metadata imported PNGs as Cube textures (textureShape2). Fixed importer to explicitly Texture2D and added loadability assertion before build.


## Campaign foundation revision — user-selected direction
24. Map verified Realm progression principles to an original Gatefall campaign and record the first-chapter scope. [complete]
25. Specify story, onboarding, district stages, pre-battle choices, in-run growth, permanent progression, rewards and failure/retry rules with consistent unlock/economy data. [complete]
26. Validate campaign dependencies/rewards and publish the implementation blueprint; keep the current v0.3.0 build clearly separate. [complete]

The user selected sequential city liberation plus roguelike growth in each battle, and explicitly wants Realm's broader foundations considered. Existing v0.3.0 delivery remains required. The campaign revision defines the next implementation baseline; do not label unimplemented campaign features as present in the current APK.

- Delivery completed: PC ZIP and ARM64 APK v0.3.0, 25 combat/18 input/106 runtime-design checks; Android actual Arc fusion and result upgrade, final PC restart and menu clearing observed. Physical-device tests remain future work.
- Approval review recovered. Android emulator startup required host GPU and optimizedFramePacing=false after a Swappy native crash; final builds verified. Full-display clear camera fixed persistent HUD margins.
- Campaign design completed in Docs/Design/GATEFALL_CAMPAIGN_FOUNDATION.md with structured first-chapter data. All 100 dependency/reward arithmetic checks passed; these are design checks, not campaign gameplay verification.

## Raon world-setting revision
27. Read the user-supplied Raon setting and reconcile its story/system changes against the existing campaign. [complete]
28. Revise the active world/campaign specification and versioned data for seven towers, new cast, ward pillars, resonance, erosion ratings and magic-core growth. [complete]
29. Check names, progression dependencies, fusion preservation, reward/grade arithmetic and document links; deliver the revised concept with runtime scope explicit. [complete]

The supplied Raon text replaces the earlier city and cast. This turn revises the campaign design, not the v0.3.0 executable/APK. Keep both required consuming-tower fusion and the supplied proximity resonance as separate mechanics. Treat numeric tuning and newly filled design gaps as proposals, not statements from the user's source.

Current design: GATEFALL_RAON_WORLD.md, GATEFALL_CAMPAIGN_FOUNDATION.md and campaign-foundation-v2.json. All208 design checks passed. Reference copy matches the attachment exactly; current document links checked. No runtime or build edits in this concept revision.

## Raon campaign implementation v0.4.0
30. Inspect runtime and Realm analysis; establish backward-compatible campaign interfaces and source backup. [complete]
31. Implement campaign stages, persistent profile, rewards, erosion ratings, research and hunter growth. [complete]
32. Integrate preparation, stage routes/waves/boss, ward families, adjacency, finite barrier blocking and hunter skills. [complete]
33. Implement Raon city map, briefing/story, workshop/hunter screens, tutorials/results and original ward visuals. [complete]
34. Verify simulation, persistence, progression, inputs and Unity; build PC/APK and smoke-test delivery. [complete]

User explicitly authorized development from the revised concept, using Realm as reference. Deliver the first six-stage chapter and existing lab; later six chapters appear as story roadmap rather than fabricated playable content. Implement Gatefall code and new presentation from the analyzed design principles.

## Claude / Codex CLI handoff
35. Write an internal handoff covering implementation, builds, evidence, remaining checks and CLI collaboration. Correct unconfirmed runtime claims. [complete]

User requested the handoff before further gameplay QA. HANDOFF_CLAUDE.md is the current entry point. Manual research refund button verification remains pending; automated refund checks passed. No game source or binaries changed for the handoff.

36. Prepare the requested Claude /goal prompt for mobile-only redesign and simplify the scope using verified Realm/Random Dice reference reports. [complete]

GOAL_MOBILE_REDESIGN.txt defines the next development task. The user now excludes PC product development/build delivery, prioritizes Android, and wants coherent original art plus fewer visible systems/actions. HANDOFF_CLAUDE.md records this superseding direction. This phase completes prompt preparation only; the redesign itself has not started.

## v0.5 continuation from Claude — 2026-10-26
37. Inspect current files/device, preserve temporary evidence and installed/candidate saves; take over the stopped implementation. [complete]
38. Convert delivery to Android only, verify generated metadata and the current source/APK; fix observed defects. [complete]
39. Play operations1-2 through1-6 via the Android UI, including1-3 fusion and1-6 boss; capture outcomes. [complete]
40. Verify research/refund/persistence, pause/background, aspect ratios and measured emulator performance. [complete]
41. Update current docs/handoff, save actual before/after comparison and perform complete delivery audit. [complete]

The user explicitly resumes Claude's stopped work. Codex now owns these edits; earlier single-editor notes describe the previous session. No parallel agent work. Physical-device multitouch remains a separately disclosed hardware validation limitation; do not claim emulator results are physical-device tests.

- Intermediate checkpoint (superseded by completion below): OpenGLES3candidate9bbb4cc6…e2b98built afternativeVulkanlabelcrashes. Research/refund/relaunch,joystick,map/growthaspectfixesverified;lateroperationsandfinalauditwerependingatthatpoint.
- Build temporarily disconnected ADB; first install attempt found no device and made no changes. Rechecked the same emulator after daemon registration, then installation succeeded. Do not recreate emulators for this transient condition.
- Operations1-4/1-5/1-6 completed with3stars.1-6actualboss and180secondwin recorded(core300/kills139);city18stars/90corespreserved. Upgradepreviewadjacencydefectfixedand21campaigncheckspassed. FinalAPKc5331fbf…73de30built/installed;newprofile1-1completed,15→23previewmatchedactual23,pause/background/aspects/3coldstartsverified. Earlier9bbbAPKplayevidenceremainsexplicitlycandidateevidence.
- Finalaudit:installed/baseandtwoAPKdeliverycopiesmatch,13builtAssetsC#hashesunchanged,16C#/106metaverified,6actualwinreceiptsand30requiredscreenshotschecked. Original.sav/.bakrestoredexactly;v0.4hashpreserved. ComparisonPNG1800×1858renderedandvisuallyinspected. Docs/runtime/handoff/progressupdated. Physicalmultitouch/heat/performanceandiOSremainexplicitlyout-of-environment,notclaimedcomplete.
- Renderer sandbox launch failed with Edge temporary-profile IPC/file-permission errors; verified process terminated, then narrowhostrender succeeded. Empty crash-buffer pipeline initially produced nofile; explicitly wroteemptyper-PIDfilesandstartup-auditJSON. Neither error affectedtheAPK.




## v0.6 visual overhaul — 2026-10-27
42. Audit v0.5 screen, references, assets and mobile interaction. [in progress]
43. Establish original Korean urban fantasy art direction and generate production assets. [pending]
44. Integrate the cohesive visual replacement and improve the representative battle, touch feedback and first fusion experience. [pending]
45. Build Android, perform relevant checks and actual screen inspection, document results and remaining device limits. [pending]

## v0.6 delivery status
42. Audit v0.5 screen, references, assets and mobile interaction. [complete]
43. Establish original art direction and generate new V6 assets for all 78 existing texture keys. [complete]
44. Integrate V6 art, representative battle feedback and fusion cost guidance. [complete; human assessment of fun remains open]
45. Build Android, verify resources/input/simulation and inspect actual Android emulator screens; document scope and limitations. [complete; real phone and iOS unavailable]

## v0.7 expanded 1-4 prototype — 2026-10-27
46. Audit stage geometry, socket limits, camera, input and existing progression. [complete]
47. Implement a 48×24 stage 1-4 with 12 sockets and compatible camera, clear offscreen warnings, art and economy. [complete]
48. Run regression tests, build Android, test actual stage 1-4 on emulator and update handoff. [complete; final emulator run is a partial battle, real-phone and human balance validation remain]

## v0.8 전투·모바일 개선 — 2026-10-28
49. 1-4 세 전략의 에뮬레이터 전투와 저지/돌파 및 경제를 비교하고 기록한다. [완료; 실제 손 조작 평가 미검증]
50. 근거에 따라 웨이브·조작·적 시인성을 개선하고 회귀 검증한다. [완료; 난이도와 표시 가독성은 실기기 평가 필요]
51. Android 빌드와 에뮬레이터 검증 및 인수인계를 갱신한다. [완료; 실기기/iOS 미검증]

## 2026-10-28 챕터 1 전투 완성
52. 기존 여섯 작전의 경로·출현·성장 해금을 점검하고 각 작전의 공략 목표와 대조 전략을 정의한다. [완료]
53. 전투를 간결하게 유지하며 스테이지별 출현과 안내를 개선하고 전략별 결과를 비교한다. [시험 구현 완료; 일부 창 전략의 범용성 해결 및 사람의 난이도 검증 필요]
54. 회귀 검사와 Android 빌드, 실제 에뮬레이터 검증 후 한계와 후속 작업을 인수인계한다. [후보 빌드 및 1-1/1-2 완료; 1-3~1-6 최종 빌드 직접 플레이와 실기기 미완료]

## 2026-10-28 챕터 1 후속 밸런스
55. 창의 사거리·성장과 경로 집중을 점검하고 대조 전략으로 균형을 측정한다. [완료]
56. 기존 적과 타워 역할 중심으로 실험하고 승리 가능성과 안내·회귀를 확인한다. [시험 조정 완료; 1-5 붕괴 이점과 6개 작전 고유 공략 검증은 미완료]
57. Android 후보를 빌드하고 에뮬레이터·인수인계 자료에 결과와 한계를 기록한다. [완료; 최종 APK 1-5 100초 스모크만 진행, 전 작전 3분 실전 및 실기기 검증 미완료]

## 2026-10-28 표식·관측창 및 편성 구현
58. 저장·캠페인·타워 UI 고정 범위 및 기존 1-1~1-6 호환 점검. [완료]
59. 1-7~1-14 시험 작전, 버전 이행, 4종 편성과 표식·관측창 구현. [완료; 무료 대여·도감·편성 조합 미리보기 미구현]
60. 신규 기능·기존 저장/전투 회귀, Android 빌드와 에뮬레이터 확인, 인수인계. [완료; 실제 신규 작전 순차 완주와 사람/휴대폰 검증 미수행]

## 야간 개발 2026-10-28 ~ 2026-10-29 07:00 KST
61. 기획: 4종 편성의 조합 결과 미리보기와 해금 조건을 정의한다. [완료]
62. 구현: 현재 편성과 해금 상태에서 만들 수 있는 조합 결과를 표시한다. [완료; 전투 재료·촉매는 별도 조건]
63. 검증: 재료 누락·해금 전후·종결 경로를 검사하고 Android 화면을 확인한다. [완료; 물리 기기 미검증]

## 야간 반복 2 — 표식·관측창 비교
64. 기획: 1-11~1-14에서 같은 위치·행동 주기의 창 중심, 표식 혼합, 표식 조합 정책을 정의한다. [완료]
65. 구현: 전략별 시간·결계·지출·융합·타워 및 표식 보너스 피해 기록기를 작성하고 비교한다. [완료]
66. 검증: 각 정책의 사용 재료와 융합, 시드/카드 영향을 기록하고 Android에서 진단 기록을 확인한다. [완료; 실제 플레이어와 엄밀한 인과·실기기 미검증]

## 야간 반복 3 — 표식 대상 선택
67. 기획: 표식의 보조 역할을 살리기 위해 공격 범위가 겹치는 동료 타워의 표적을 우선할지 실험한다. [완료]
68. 구현: 사거리 내 동료 화력과 표적 우선순위를 결정적으로 계산하고 기존 선두 선택을 대체 경로로 유지한다. [완료]
69. 검증: 표적 우선/동료 부재 회귀, 전후 3분 전략과 Android 패널을 확인한다. [완료; Android 전투의 표적 비교·사람/실기기 성능 미검증]

## 야간 반복 4 — 1-11 표식 훈련 전투
70. 기획: 표식 훈련의 양쪽 길·정예 위험과 적 역할을 확인하고 비교 기준을 정한다. [완료]
71. 구현: 적 수와 기본 규칙을 유지하며 1-11의 정예 등장과 작전 설명을 구체화한다. [완료; 난이도 상승은 미확정]
72. 검증: 기존/표식/조합 전략, 별도 회귀 및 Android 표시를 확인한다. [완료; 에뮬레이터 첫 시도 143.75초 패배, 사람·실기기와 순차 완주 미검증]

## 사용자 요청 — 전투 2배속
73. 전투 화면에 1×/2× 전환을 추가하고 메뉴·성장 선택은 정지, 새 전투는 1×로 시작한다. [완료]
74. 터치·기존 전투 회귀, Android 빌드 및 에뮬레이터 속도/일시정지를 확인한다. [부분 완료: 빌드·입력 검사 및 1×→2× UI 확인. 다른 조작이 관찰돼 2×→1×·정지 실측 보류]

## 사용자 요청 — 타워 메뉴 중 전투 진행
75. 건설·강화·조합 패널과 조합 짝 선택 중 전투를 계속하고, 일시정지·성장 카드·앱 이탈은 정지한다. [완료]
76. 시간 흐름·정지 회귀 검증과 Android APK를 갱신한다. 진행 중인 에뮬레이터 사용을 보존한다. [완료; 최종 APK 에뮬레이터 화면과 실제 기기 미검증]

## 사용자 요청 — 경로·설치 지점과 웨이브 보상 개선
77. 1~14작전의 배경 도로·활성 경로·설치 지점과 재화 흐름을 대조하고 변경 기준을 정한다. [완료]
78. 경로에 맞는 지점, 설치 수 확대와 전투 보급을 조정하고 안내를 갱신한다. [완료; 사람의 재미·난이도 검증 필요]
79. 경로 거리·경제·전략 회귀와 Android 빌드를 확인하고 현재 사용 중인 저장을 보존한다. [완료; 에뮬레이터 일부 화면, 실제 기기·전 작전 완주 미검증]

## 사용자 요청 — 헌터 부활
80. 헌터 사망 시 10초 후 결계 근처에서 체력을 회복해 부활하고, 대기 중 전투·건설이 이어지도록 구현한다. [완료]
81. 부활 대기·일시정지·적 저지 해제·결계 패배와 Android 빌드를 검증한다. [완료; 부활 완료는 모델 검사, 최종 APK Android 화면·실기기 실제 부활 미검증]

## 야간 반복 5 — 2배속 버튼과 전장 지점 충돌
82. 1-11에서 2배속 버튼이 설치 지점을 덮는 문제를 해결하고 상단 UI와 터치 영역의 간격을 검증한다. [완료]
83. 회귀 검사·Android 빌드와 독립 에뮬레이터에서 변경된 배치와 지점 접근을 확인한다. [완료; 실기기 터치 미검증]

## 야간 반복 6 — 헌터 부활 전투 재검증
84. 결계를 방어하면서 헌터가 쓰러지고 부활하는 재현 가능한 전투를 모델에서 설계한다. [완료]
85. 최종 APK Android 에뮬레이터에서 쓰러짐과 부활 완료를 직접 확인하고 결과 및 한계를 기록한다. [완료; 실기기와 사람 플레이 미검증]


## 야간 반복 7 — 타워 메뉴 시간 Android 검증
86. 최신 APK에서 건설 및 타워 패널 중 진행, 성장 카드·명시적 일시정지와 재개를 독립 Android 에뮬레이터로 확인한다. [완료; 조합 짝 선택·앱 이탈·실기기 미검증]


## 야간 반복 8 — 현행 APK 조합 재검증
87. 독립 Android 에뮬레이터에서 합성 해금 프로필로 강화와 조합 및 전투 재개를 확인한다. [완료; 후보가 하나라 짝 선택 화면과 시간 흐름, 실기기 미검증]


## 야간 반복 9 — 복수 재료 선택 중 시간
88. 여러 짝 후보가 있을 때 선택 화면과 전투 진행을 독립 Android에서 확인한다. [완료; 실제 짝 탭 후 조합·실기기 미검증]


## 야간 반복 10 — 조합 결과 위치 명확화
89. 복수 후보 선택 화면에서 결과 생성 위치와 재료 소모 위치를 짧게 설명한다. [완료]
90. 회귀 검사와 Android 빌드 후 독립 에뮬레이터에서 후보 선택, 결과 위치와 빈 지점을 확인한다. [완료; 실기기 및 사람의 이해도 미검증]


## 야간 반복 11 — 1-1 난이도와 안내 교정
91. 1~14작전의 현행 자동 전략을 다시 비교하고 1-1 두 타워 고정 배치를 Android에서 검증한다. [완료; 사람의 밸런스 평가 미검증]
92. 검증된 사실과 다른 첫 안내를 수정하고 회귀·Android 빌드 및 표시를 확인한다. [완료; 실기기 미검증]


## 야간 반복 12 — 1-11 표식 첫 배치 비교
93. 첫 해금 상태의 제한 배치로 Android 전투와 표식 기여 기록을 확인한다. [완료; 단일 자동 시도]
94. 같은 지점에서 표식과 창 배치를 3시드 모델로 비교하고 해석 한계를 기록한다. [완료; 추가 건설·강화 및 사람 플레이 미검증]


## 야간 반복 13 — 표식과 보급 지출 전투
95. 1-11 첫 해금 조건에서 초기 표식 배치 이후 창 강화와 추가 배치를 Android 에뮬레이터로 검증한다. [완료; 자동 조작 별도 실행, 실기기와 사람 플레이 미검증]
## 주간 반복 — 초반 난이도와 재미 조정
96. 1-1~1-3 첫 진입의 난이도와 실제 조작 부담을 확인하고 쉬운 초반 기준을 기록한다. [부분 완료: 1-1~1-3 자동 검증, 사람 플레이 미검증]
97. 초기 전투에서 필요한 결정을 확인하고 차별화·후속 난이도 실험을 설계한다. [진행: 1-2 분산·집중 및 1-3 첫 조합 사례 확보; 1-4 이후 비교 필요]


## 주간 반복 2 — 첫 조합 후 빈 자리 안내
98. 조합 후 비워진 지점에 새 타워를 건설할 수 있다는 짧은 안내를 추가한다. [완료]
99. 회귀·Android 빌드 후 합성 첫 진입에서 안내와 실제 재건설을 확인한다. [완료; 사람의 이해도·실기기 미검증]

## 1-4 현행 밸런스 재검토
100. 현행 코드에서 무건설·고정·분산·강화·조합 정책과 3개 시드를 비교한다. [완료: 자동 모델 한정]
101. 현행 APK 첫 진입의 세 전략을 실제 화면 입력으로 시험한다. [부분 완료: 강화 전략만 180초 승리, 분산·조합은 도중 패배; 조건 통제 비교와 사람 피드백은 미완료]
102. 1-4의 세 방향과 보급 후 보강 안내를 개선하고 Android 빌드 및 화면을 확인한다. [완료; 실제 사람의 이해도는 미검증]
103. 같은 초반 배치의 빠른 조합·보충과 비조합 보강을 세 시드로 모델 비교한다. [완료; 인과·사람 플레이 검증 아님]
104. 현행 APK에서 62초 전후 조합·보충과 후속 보강을 실제 화면 입력으로 확인한다. [완료: 64.8초 조합·67.6초 보충 후 180초 승리; 사람·실기기는 미검증]

## 사용자 요청 — 우선순위 순차 개선
105. 독립 Android에서 1-4 조합·보충 및 보강 전투를 재현하고 결과를 기록한다. [완료: 두 시도 입력·구매 기록 대조; 지출·카드 차이로 인과 비교는 미완료]
106. 전투 가독성과 보강 판단에 대한 구체적 문제를 수정·검증한다. [완료: 성장 카드 뒤 보급 안내와 북쪽 화면 밖 경고 겹침 수정·Android 확인; 실기기·사람 평가 보류]
107. 초반 학습 안내와 타워 선택 가치를 자동 검증 가능한 범위에서 점검한다. [완료: 첫 실패 재도전 안내·1-3 조합 비용 수정, 창·봉인 모델·Android 제한 비교; 사람 이해도 보류]
108. 모바일 실기기와 실제 사람 플레이는 보류하고 미검증으로 기록한다. [보류: 사용자 요청]
109. 첫 챕터 차별화의 남은 문제를 검토하고 근거 있는 소규모 개선을 진행한다. [완료: 자동 검증 범위의 역할·보강·첫 보스 검토 및 처치 후 목표 안내; 실제 사람의 재미 평가는 보류]
110. 1-2·1-3에서 동일 지점의 창과 봉인 대체를 여러 시드로 비교하고 해석 한계를 기록한다. [완료: 모델·Android 두 시도; 인과·사람 플레이 미검증]
111. 1-5 동일 초기 배치·구매 시점에서 창·붕괴·방벽을 비교하고 피해 기여를 기록한다. [완료: 18회 모델, 일반 우열 미확정]
112. 1-5 역할 타워의 위치별 효용과 Android 첫 진입 보강 전투를 확인한다. [완료: 모델54회와 독립 Android 180초·결계269 승리; 카드·지출 차이로 인과·사람 재미 미확정]
113. 1-6 보스전의 첫 진입 조건과 보강·조합 선택을 모델·독립 Android로 검토한다. [완료: 모델18회, Android 창6개·강화5회로180초 결계256 승리 및 보상·해금 저장 확인; 조합 Android·사람·실기기 미검증]
114. 1-6 보스 처치 후 남은 결계 방어 목표를 기존 HUD 공간에 안내하고 Android에서 확인한다. [완료: v0.10.18 전체 회귀·APK·독립 Android 표시/시간 진행/알림 분리/180초 승리 확인]
115. 첫 보스 이후 1-7 전환과 잔여 범람의 난이도·보강 선택을 모델과 독립 Android에서 검토한다. [완료: 모델36회, v0.10.19 역할 안내·저장 업데이트 보존, 백도하 창4개·강화3회로180초 결계214 승리·1-8 전환 확인; 사람·실기기·조합 Android 미검증]
116. 1-8의 빠른 마수와 봉인 활용·보강 시점을 모델 및 독립 Android에서 확인한다. [검토완료:v0.11.4/37.3456조건에서봉인대체투자의상대우위미확인/미해결로유지,적후보폐기·수치/28스케줄유지.화력우선안내·타워알림분리·호출중첩문구.독립Android창5+봉인1/강화4/375지출·186.45초102전멸/결계300/3별·24보상/1-9해금/저장보존.전체회귀/서명정렬/실설치통과·전용AVD삭제.사람/실기기보류]

## 사용자 우선 요청 — 웨이브 전멸 승리
117. 캠페인 승리를 180초 생존에서 전체 웨이브 출현 완료·잔여 적0·보스 처치·결계 생존으로 바꾸고 HUD·기획을 갱신한다. [완료: v0.11.0·6웨이브/잔여 적 HUD, 제한시간 승리 제거]
118. 180초 이후 전투·보급 상한·미출현 적·보스·패배 우선·저장 회귀와 초반 난이도, Android 전멸 승리를 검증한다. [완료: campaign39/252모델, 후보1-7 190.57초·92/92 전멸·정리 중 건설, 최종1-1 183.42초·66/66·무피해, 보상 저장 확인; 사람·실기기·전체 Android 미검증]
119. 새 승리 규칙에서1-3 조합 후 보충과1-4 확장 전장의 정리 전투·화면 밖 경고를 독립 Android로 추가 확인한다. [완료: v0.11.0 1-3 조합보충·187.85초90/90·무피해,최종v0.11.1 1-4 조합보충·190.37초128/128·무피해3별/보상16·1-5해금.잔여13과북쪽경고 동시 및 전체보기정리안내 확인.사람/실기기는미검증]

## 사용자 우선 요청 — Realm / Gatefall 게임 진행 전체 대조
120. 확보한 Realm 3.4.47의 실행 규칙·정적 데이터와 Gatefall v0.11.0 구현을 대조해 전투·경제·성장·해금·반복 콘텐츠 차이를 근거와 함께 문서화한다. [완료:100개 비교 항목·수치·원본/현재/장기계획 구분·진행 문제6종·우선순위·미확정 범위. 보고서/스냅샷/문서검사 저장,100항목·40로컬링크·32근거해시 확인. 코드·APK·저장 변경 없음; 원본 전맵·서버·사람/실기기 새 검증 아님]

121. 사용자 분석 이후 기존119의1-3 조합·보충 전멸을 독립Android에서 확인하고 기록한다. [완료: 3시도 실제구매/화면/전멸/저장 대조,교정 시도 무피해3별.앞2시도는자동좌표·카드겹침으로보충미실행해분리기록.이번AVD만삭제,v0.11.0·사용자저장유지.사람/실기기보류]

## 사용자 요청 — 비교서 기반 개선 반복
122. 비교서의 첫 봉인/미래 챕터 혼선과 해금 헌터 첫 성장 잠금을 정리하고, 효과 없는 표식 공명 표시를 수정한다. [완료: v0.11.1/34,세희12·백도하18 첫성장·기존성장보존·첫봉인이정표·짧은상태표시·표식공명제외.장기/기초문서·예산정리]
123. 최종 Android에서119의 남은1-4 전멸·추적/전체 보기·북쪽 경고와122의 성장/표식 표시, 기존 저장 호환을 검증한다. [완료: 전체회귀/빌드/서명/16KB,모델6회,최종실설치해시/성장구매112→82·B/A급/표식인접메뉴·업데이트저장바이트동일·1-4전멸/저장확인.새AVD만삭제,사람/실기기보류]
124. 결과를 비교서 적용 기록·인수인계·반복 자동화에 반영하고 다음 작은 개선을 선정한다. [완료: runtime-v0111.md/적용기획서/README/HANDOFF,자동화5를최신비교서개선우선으로갱신.기존30분·오늘23시KST종료유지]
125. 현재 작전의 웨이브 편성과 다음 위협의 사전 정보를 명시적으로 정리하고, 기존 HUD 공간을 활용해 예고/판단을 소규모 개선한다. [완료: v0.11.2/35.실제Schedule의WaveBriefing·방향별수/주요위협알림·경고소속WaveID.28스케줄전후동일/168예고·회귀25/23/45/106/102·Unity Android/서명/정렬통과.독립5558 실제APK4:3의1-4시작/카드뒤다음예고·각1회로그·저장해시보존확인.38초UI검증이며사람/실기기/보스Android미검증,전용AVD정리완료.외부저작데이터전면전환아님]
126. 수동 다음 웨이브 호출과 조기 보상의 적용 범위를 먼저 기획하고 호출·보급/카드/촉매·잔여 적·중복보상 관계를 검증한 뒤 작은 범위로 구현한다. [완료:v0.11.3/36/72ba585f. 일반1-4이후 최대6초 호출·선지급/진행 막대/촉매 안내·표시 웨이브 ID 입력. 호출49/모바일24/기존회귀·Unity Android/서명16KB·실설치/저장보존 통과. 독립 Android 호출5회30초·53.72조합/54.5보충·4:3/휴대폰/2x·162.17초128전멸/결계300/3별/보상16/1-5해금. 원래150초 후에도 보급5회300/카드3/촉매2 상한. 사람/실기기/전체14작전·보스Android 미검증,위기 비노출. 기존5556/사용자 저장 보존·전용AVD 삭제. 새 화폐/모드 없음]
127. 현행APK에서1-6의마지막호출·문지기공격예고/처치·잔여전멸을독립Android로확인하고보상저장까지검사한다. [완료:현행0.11.4/37유지·독립headless5572.139.65초최종호출6초/선지급·보스본체/공격경고/붉은원/처치후목표·182초잔여2→184.58초148전멸/보스처치/결계256/2별.창6Lv2/420지출·5회300/카드3/촉매2·보상20/70→90·백도하/1-7해금/Valid·설치/로드해시동일.첫5558출처불명입력시도는제외/기기저장보존,최종5572만삭제.사람/실기기/모든메뉴동시배치미검증]
128. 1-8 봉인 투자의 상대가치를 동일예산·다른구매정책에서 검토하고 근거가 있을 때만 작은 조정을 한다. [모델검토완료/제품조정보류:지출상한240/280/320/360·3구매순서·3위치·호출off/on·카드off.초기240조건의위치혼선후동일위치432조건216쌍재검토·봉인높은결계0/동일84/낮음132,창만승리15/봉인만승리0.3시드동일결과로무작위강건성주장안함.투자가치미해결·수치/APK유지.사람/카드onAndroid/실기기미검증]
129. 준비안내의긴끝글자줄바꿈등남은문구가독성을작게정리한다. [준비안내범위166완료:0.18.4의1-8/16:9 마지막다.재현→0.18.5문장별두줄/세화면확인·원문/글자/패널유지.다른도시문구전수검사는아님]

## 2026-10-29 23시 마감

새 개발을 시작하지 않고 v0.11.4 최종 APK·검증·미완료를 확인했다. 반복automation-5/마감11 모두PAUSED 확인.129와봉인투자가치·사람/실기기/iOS등은미완료/보류로남기며완료로간주하지않는다.마감보고 `TestResults/closeout-2026-10-29.md`.사용자재개지시전자동개발종료.

## 2026-10-30 사용자 요청 — 맵 다양화·개체 표시 축소 (수동 작업 재개)
130. 현재 반복 맵과 Realm의 길목 원리를 대조하고14작전 개별 경로·배치/시각 크기 기준을 설계한다. [완료: GATEFALL_MAP_REDESIGN_V0120.md, Realm 좌표/에셋 복사 없음]
131. 경로와 같은 데이터로 도로를 렌더링하고 작전별 굴곡·합류·우회/타워 지점을 구현한다. [완료:14경로/139지점/48×24,보스 실제 북도로]
132. 자체 바탕/도시 소품을 새로 생성하고 타워·적·보스 표시를 줄이며 터치/체력바/효과를 맞춘다. [완료:V12자체PNG3/소품4셀, 타워.70·일반.68·공성.75·보스.60,터치/사거리유지]
133. 전 작전의 경로/배치·진행·초반 접근성을 모델로 검증하고 필요한 조정만 한다. [완료:45캠페인/14경로/139지점,14×6정책×3시드252조건.분산/강화84승,조합전략일부실패·후속명시]
134. Android APK를 빌드·서명/정렬 확인하고 분리 기기에서 대표 맵·작은 개체·터치·전투·저장 호환을 확인한 뒤 인수인계한다. [완료:0.12.1/39·v2/16KB·실설치/업데이트저장동일·0.12.0의14준비/1-1·1-4전멸조합보충/실패기록·최종1-1완주/1-8가상4:3건설·5574정리·문서화.사람/실기기/iOS·14전투완주·축소보스는미검증]

이번 사용자 지시는 새 수동 작업의 재개이며 이전23시 마감은 해당 야간 예약에만 적용한다. automation-5/11/7은PAUSED를유지하고자동반복은재개하지않는다. 출처불명5558/BossCall·이전5556/사용자저장은보존한다.

## 2026-10-30 사용자 요청 — Random Dice 참고 전투 속도·물량·처치감
135. 기존 구현/보관된 참고 보고서/공식 소개를 검토하고 빠른 일반 적 처치·추가 물량·초반 접근성·표현 예산을 설계한다. [완료: GATEFALL_COMBAT_PACE_V0130.md, 자체 수치/자체 효과]
136. 일반 적 물량/이동·처치 시간과 연사/탄속을 함께 조정하고 경로/전멸/보급·저장 규칙은 보존한다. [완료: 추가 일반 적으로 약2배/정예·보스 개수 유지, 탄속28/30, 창 .46초]
137. 짧은 처치 파열/잔상·타격음·공격 반동·이동 보간과 표현 풀 상한을 적용한다. [완료: 220효과풀/이동보간/처치잔상·파편/자체타격음/1x·2x Android 녹화·관측효과최대66.청감미검증]
138. 이전 코어와 같은 정책/시드의 모델을 비교해 초반/보스/다방향 전투와 보상·성능 부하를 검증하고 조정한다. [모델완료: 전후168+168조건/24대결,초반27결계300/분산강화42승씩·조합27→36승·1-8/12 조합실패 유지.기기부하1-4평균16.77ms·호스트GPU한정]
139. Android APK를 빌드·서명/정렬 검사하고 새 독립AVD에서 실제 전투·처치 효과/조합·저장/프레임 기록을 확인 후 인수인계한다. [완료:0.13.0/40·v2/16KB/실설치해시/업데이트저장동일·1-1 132전멸/1-4 256전멸·조합보충/결계300·보상저장·전용5576정리.사람재미·실기기·iOS/청감·14작전Android전수는미검증]
새 사용자 요청 범위의 수동 작업이다. 이전 예약을 재개하지 않고, 기존5558/5556과 사용자 저장을 보존한다. 타 게임의 코드·에셋/검증하지 못한 정확한 수치를 가져오지 않는다.




## 2026-10-30 사용자 요청 — 원래 물량의5~8배·작은 저해상도 군집
140. 2배와 같은 원래 기준에서 초반5/중반6/후반8배를 설계하고0.13.0 코어·표현·전달본을 보존한다. [완료: GATEFALL_SWARM_V0140.md / baseline보존]
141. 편성/체력/분할 전투재화를 맞추고 대량 표적 탐색·발사 연산을 줄여 초기 쉬운 난이도와 전멸·보급/카드/촉매·저장을 보존한다. [완료:5/6/8배·추가4처치1마석/피해1·중복사격예약·집계/탐색버퍼·상한/저장규칙유지]
142. 일반 적64px/작은 표시·묶음 렌더링/오브젝트 절감·효과 예산과 부하 계측을 구현한다. [완료:64x48/.42표시·24깊이행/고정용량재사용·효과220·Android113+군집/조합표시확인,최종944전멸/관측134마리확인완료]
143. 동일정책/시드 전후 모델과 군집 스트레스·경로/타깃/재화 회귀로 난이도·성능을 확인하고 필요한 범위를 조정한다. [완료:168정책조건/초반27무피해/고정1-2패배·조합35승7패기록,24대결/84편성/85,879길샘플최대.320·90스텝768적할당22MB→42KB.호스트CPU경합한계별도]
144. Android 최종 APK/서명·정렬·독립AVD의초반/대량전투·조합·저장·프레임을 검증하고 인수인계한다. [완료:0.14.1/42·서명16KB/실설치해시·저장업데이트동일.0.14.0초반330/최종8배944전멸·조합보충/9타워·무피해·보상해금저장/64x48·134적관측·효과82/220.호스트CPU경합한계/사람·실기기·iOS보류.전용5578정리·우선순위복원]
배수는0.12.1의 원래 물량 기준이다(0.13.0보다다시5~8배가아님).140~144는새수동요청이며예약재개없음/기존5556·5558·사용자저장보존.




## 2026-10-30 사용자 요청 — Random Dice 참고 공격·피격 효과
145. 공식 시각 참고자료와 현행 효과를 비교해 타워별 발사·명중·처치 표현과 군집 효과 예산을 정한다. [완료: 자체 효과 기획/원본 동영상·브라우저 직접 관찰은 도구 오류로 제한]
146. 자체 코드/기존 자체 에셋으로 선명한 탄선·역할별 명중·짧은 처치 파열을 구현하고 기존 전투 수치/저장을 유지한다. [완료: 탄선/역할별 명중·파열/흰 실루엣 셰이더·220상한/코어 불변·Android관찰후특수탄색교정]
147. Android 빌드·서명/정렬·분리 AVD에서 다수 적/타워별 효과·상한·프레임·저장 호환을 확인하고 기록한다. [완료:0.15.1/44·v2/16KB/실설치해시·각업데이트저장동일.1-14 206.65초944전멸/결계269·2별/보상36·Valid/7타워710지출/4계열실발사·효과95/220·재활용0·3녹화.5580정상정리·우선순위복원/문서화.사람·실기기·청감·iOS·보스/전조합효과는미검증]


## 2026-10-30 사용자 요청 — 더 많은 건설 지점과 동시 전투
148. 기존10개 지점/7타워 녹화의 공간·재화 병목을 분석하고, 초반 쉽게/후반12~16타워 운용을 목표로 배치와 전투 경제를 설계한다. [완료: 기존10지점/7타워 병목·308지점/단계재화 설계]
149. 도로 밖 유효 사거리의 건설 지점을 늘리고 시작 재화/웨이브 보급·모바일 안내를 맞춘다. [완료: 기존139좌표유지/308지점·시작240/280/320·보급80/100·HUD현재/전체 수·기록정합]
150. 여러 정책/시드와 공간·재화/호출 회귀로 초반/보스/후반 난이도·타워 수·동시 발사를 검증한다. [완료: 전후126+126조건·평균약9→14타워·초반27조건최소결계300·139원좌표/308건설/단계재화·기존회귀통과.최근3초발사최대9→10이며사람재미/16개동시렌더아님]
151. Android APK를 빌드·서명/정렬·독립 기기의 다수 타워·전투/조합·저장·프레임/효과 상한을 검증하고 인수인계한다. [완료:0.16.0/45·서명16KB/실설치해시·업데이트저장동일.1-14 16타워/2조합·188.63초944전멸/무피해3별·500보급·보상저장.1-1 시작240의창6Lv1·181.53초330전멸/무피해·400보급/저장.2원본/22초편집영상·효과111/220/재활용0·5582정상정리·문서화.실기기/사람/청감/iOS보류]



## 2026-10-31 사용자 요청 — 오전10시까지 경로·전투 재미 반복 개선
152. 기존 반복/마감 자동화를 오전10시 KST까지30분 간격으로 갱신하고 최신 검증 기준을 보존한다. [완료: automation-5 ACTIVE/마감11 ACTIVE/7 PAUSED 유지,10시 마감·정리]
153. 첫 작은 작업으로4진입 공통 처리와1-10의 긴4방향 경로를 기획·구현한다. [완료:0.17.0후보/서북동남33.61이상·24지점·공통배열/예고/보스경고구분]
154. 남쪽 경로·예고·보스 경고 구분/기하·재화·전투·타워 사격 참여를 이전과 같은 정책으로 검증한다. [모델완료:24경고864출현/남쪽211641샘플·129원좌표308건설/각126조건전후모두승·초반결계300·1-10Fired7~11→12~13/최근3초최대8→11.사람평가아님]
155. 필요한 Android 빌드·서명/정렬·분리 기기에서4방향 실제 전투/모바일/저장을 검증하고 기록한다. [완료:0.17.0/46·서명16KB/설치해시·두단계업데이트저장동일·남쪽오보교정·16타워/185.50초864전멸무피해/보상28·표식/1-11Valid저장·13.65초영상·5584정리.시작진단5줄·북쪽HUD일부겹침/사람실기기보스최종재생미검증]
156. 이후 실행에서는 최신 미완료·실제 결과를 바탕으로 다음 작은 개선을 선정하고,09:30 이후에는 큰 변경보다 마감 검증을 우선한다. [반복대기;오전10시 이후 새개발 금지/최종보고/중지]



157. 북쪽균열(8,11)과배속버튼의일부겹침을검토하고, 남은대칭맵의접근시간/사격참여를비교해가독성또는1-7/1-12등한곳의비대칭·공유목을작게개선한다. [완료:0.17.1/47배속참조274,8·4화면비율/Android1↔2/북쪽/정지·실설치/합성완료저장동일·5586정리.기존모델맵프로필검토·다음1-7우선.전체전투재완주아님]
158. 시작AssetPackManager클래스탐색실패1줄/EGL3.2→3.1진단과최종보스경고Android재생을후속검토한다. 전투완주정상과구분하며원인확인없이불필요한모듈추가금지. [보스Android후속은165완료/시작진단원인·실기기영향은미확정;실기기·사람확인은보류]



159. 다음맵실험은1-7을우선:동일길35.83/35.83·평균발사5.33/14·최근3초최대6.3진입/다른합류구조를기존552적·재화·같은정책/시드로비교하고효과있는작은후보만적용·Android검증한다.1-12는이미비대칭으로후순위.갈래증가만으로성공판정금지/필요파형은분리후보/초반유지. [완료:0.18.0/48·3후보/각9모델후택·18경고/552예산/기하308/회귀·서명16KB/실설치/업데이트저장동일.새5588 14타워188.60초552전멸무피해3별·보상22/1-8Valid저장.후방뇌격판매191/전방재조합6발확인·봉인0은미해결/총1195순1004·영상은재조합전·5588정리.사람재미아님]
160. 정상포기후Valid·무보상이나PendingRun잔존한저장정합을검토한다.Begin은새ID덮어써즉시차단증거없음.사용자저장보존/원본합성pending·after자료유지. [완료:0.18.1/49·현재ID의복사저장성공후지도복귀/실패Pause재시도·종료결과Complete우선·캠페인48/전투5해시불변·새5590준비포기/6초진행중저장실패주입→해시불변/재시도·재실행원본324바이트동일·62마핵15별과과거보상22/1-8유지·서명16KB/설치해시·5590정리.전체전투재완주/사람실기기평가아님]



161. 1-7후방봉인/뇌격0발과전방재조합뒤뇌격6발을근거로위치·범위안내또는동일552총량의후반파형을작은후보로검토한다.모든14타워를항상발사시키도록강요하지말고추가시스템없이투자효용/시각적밀도·재화·승리곡선을비교한다.초반유지/160다음. [완료:0.18.2/50·같은552/940지출/14타워·6위치모델후뇌격0→107/109/93·모두무피해/봉인0유지. 결과범위/위치/사거리·취소후전방선택안내·단일상대선택통일·알림회피.최종5592 UI약80초·후방/전방미리보기·취소·실제전방생성/사거리5.9·저장동일·서명16KB/설치해시/소유정리.전체전투재완주·사람재미아님/봉인효용미해결]

162. 촉매 부족으로 비활성인 조합 버튼을 누르면 뒤의 다른 지점이 선택되는 실제5592 관찰을 재현하고 모바일 입력의 패널 차단을 작게 수정·검증한다. Disabled 버튼과 메뉴 안 빈 공간이 뒤 지점을 누르지 않도록 하되 메뉴 밖 지점 전환/닫기·LIVE 전투를 유지한다. [완료:0.18.3/51·활성버튼→패널None차단→필드지점/밖닫기·모바일27/코어6해시불변·새5594이전APK준비오류재현/최종준비·LIVE동일입력차단/활성강화·건설·외부지점전환/0:05→0:09·정상포기원본저장동일·서명16KB/설치해시·소유정리.약10초입력검사/전투완주아님]
163. 봉인 위치를 앞쪽 후보 몇 곳으로만 옮기는 동일 구매/예산·3시드 비교로 선행 처치와 역할을 구분한다. 뇌격 위치 모델과 다른 새 질문이며, 비용·감속 기여·사격·전멸 곡선을 보고 근거가 있을 때만 작은 제품 변경을 택한다. 새 시스템/무조건적인 수치 상향은 하지 않는다. [위치비교완료:3기준재사용/9후보동일552·940지출14타워·모두결계300/최종120·전방봉인64~404발.동16강한적4/Runner8감속표본·수치/파형보류.0.18.4/52메뉴최대3→4명표현·코어6불변·새5596 약44초/6타워285지출·Native78발78명중/감속고리·Lv1/2문구·서명16KB/설치해시·저장동일/소유정리.상대투자가치는미해결/사람재미·전체Native완주아님]

164. 조합 전 정책/재료는 동일하게 유지하고78.9초의 보충만 같은 자리 봉인45 대 창40으로 바꿔 피해·감속·정리·최저 결계/재화를 비교한다.163의9조건은 기준 재사용하며 예산 상한940·뇌격11·후속 구매·3시드를 고정한다. 창 실제지출935/잔여5 차이를 분리하고 재투자/무료 가격 보정은 하지 않는다. 초기 재료를 창으로 바꿔 레시피를 깨지 않도록 한다. [제한비교완료:9기준재사용/새창9+관측동등성1·봉인ID계측보완1·초기정책/조합/카드동일·양쪽552전멸결계300.창실효피해높음·동16정리4.60~4.78초단축/서남동일·봉인감속손실/실제935·125잔여분리.분석코어읽기관측원본일치/피해재구성검증·제품/APK불변·새APK/AVD없음.다른압력/사람재미로일반화금지·수치유지]

165. 158 중 미완료인 현행0.18.4 APK의1-6 최종 호출·문지기 경고/처치·잔여 전멸/보상 저장을 독립 Android에서 확인한다. 이전0.11.4의148적 검증과 현재883적·확장된 지점/효과 검증을 구분한다. 선택 작전 실제 좌표·새 합성 저장/소유AVD로 수행하고 제품 변경이 없으면 새 APK를 만들지 않는다. 시작 진단 원인·실기기 영향은 별도 미확정이다. [완료:5598/14타워940지출·141.37초호출1/5.63초·경고문구/붉은원·처치후HUD·180초잔여2→180.75초883전멸/결계278헌터68/3별보상20·60마핵13별/백도하·위기자격/1-7Valid·지도/재실행324바이트동일/소스APK불변·소유AVD정리.시작진단5줄원인·실기기·사람재미미검증]

166. 129의 긴 준비 안내/끝글자 줄바꿈을 기존 모바일 UI 안에서 정리한다. 실제 긴 문구와 작은 화면 투영을 먼저 확인하고 짧은 문구/레이아웃 중 작은 해결을 택한다. 전투·경제·저장·기존LIVE메뉴/입력차단을 유지하고 변경 시에만 필요한 Android 빌드·서명16KB·실설치/저장 호환을 검증한다. [완료:0.18.5/53·MobileHud표시문장개행한줄+버전외제품불변/6코어동일.기존1-8/1920끝다.재현·최종1920/2400/1280세화면두문장완전표시/약11초·창1개/메뉴LIVE04→09/정상포기.서명16KB·설치해시/업데이트·포기·재실행324바이트동일/62마핵14별과보상22ID보존/5600정리.전체Native전투·사람가독성·실기기아님]

167. 다음 맵 차별화/타워 전투 참여 후보로1-9를 검토한다. 과거0.17.0의 기존9조건 평균발사7.22/14·최근3초최대7은 후보선정 지표이며 새 검증이 아니다. 먼저 현행1-9의 경로·624적·구매정책/재화·기존조건 정합을 확인하고, 긴 합류/엇갈린 우회 등의 작은 후보를 같은시드/정책으로 비교한다. 전체308지점/초반/공급·카드·촉매 상한과 승리 규칙/저장을 유지하고 모든맵4길화/발사수만으로자동채택/새시스템을 피한다. 유효할 때만 제품 변경과 필요한 독립Android를 진행한다. [제한검토완료/불채택:현행기준9가과거9와일치·남쪽공유24지점실패/전투0·북합류새9조건624전멸결계300/14타워동일지출이나평균발사7.22→6.89/빈번6.67→4.78·정리길어짐.조합정책만의발사증가/최대8도보존·제품소스/APK0.18.5유지/새APK·AVD없음/사람재미일반화금지]

168. 기준1-9 지형을 그대로 두고 후반 웨이브 인덱스3~5의 도착 구간22초만16/12초로 모으는 작은 후보를 분리 비교한다.624총량·종류/갈래별 수·초기3웨이브·시드/구매규칙·재화/카드/촉매 상한을 유지한다. [완료:12초 제한채택/v0.19.0·54/161dc085…/모델 각9조건·평균발사7.22→8.11/빈번6.67→7.33·최대최근3초8/모두무피해.일부정적구간·조합순서변화는한계.56초기스케줄대조/갈래첫출현고정.새5602시도B의14타워·26구매1070지출/조합2·174.18초624전멸/무피해3별26보상·1-10해금/업데이트·재실행저장동일.시도A정상포기·완주근거제외.서명16KB/설치해시/제품3파일변경·4코어및Presentation불변/소유AVD정리·반복검사PASS.사람재미·실기기/iOS보류]

169. 현행0.19.0 일반1-9에서 저투자 대비 보강 필요성을 제한 비교한다. [분석완료/제품유지:동일초기mixed6개/3시드·첫카드/자동스킬에서새270·380각3조건모두624전멸·결계300/결계접근0·피해0.6개192.37~196.65초/잔여784·7개187.38~191.97초/잔여674.기존14개1040의3조건재사용·선택seed7보완1은결과/원장/사격까지동일.6→14는17.37~18.32초단축이나이조건생존필수아님·7개봉인12는0발.제품18파일/6코어/기존APK정합·하네스역변환/기록재검사PASS.새APK/AVD/Android없음·사람재미일반화금지]

170. 일반1-9후반의기존정예6/공성6만이동속도1.25/1.40배후보로제한비교한다. [완료:1.25채택/v0.19.1·55/SHA39614319…97346491바이트.21새조건/기준15재사용·선택관측2/28초기스케줄쌍·840생성셀각대조/6셀속도만변경.6개1승2패·7개3승222~300/14개다수·강화·조합9조건무피해·지출980/1040동일.중첩지표보편증가아님/1.40불채택.출시회귀·서명16KB/실설치해시·2제품파일외불변.새5604실제14타워22구매915지출·조합1/173.42초624전멸/무피해3별26보상·1-10해금/업데이트·재실행저장동일.간접회피입력포함/QA안전좌표정정·이전무스킬주장철회.소유5604/QEMU25724정상정리/반복검사PASS.사람재미·실기기/iOS·청감보류]

171. 최종 검증자료/인수인계와QA입력정정의정합을확인하고오전10시마감자료를준비한다. [준비완료:09:18·APK3개/제품18파일·후보21/기준15집계·실제원장/보상·재실행저장·입력정정·증거파일·소유정리/보호폴더정합PASS.제품/새전투·모델실행/빌드·AVD변경없음.closeout-v0191.json/Docs/QA/GATEFALL_CLOSEOUT_2026-10-31.md작성.당시대기하던실제마감은172에서완료]

172. 오전10시 이후 새 개발 없이 최종 전달물과 미검증 사항을 보고하고 반복을 중지한다. [완료:10:02KST automation-5/11모두PAUSED확인·0.19.1/code55 APK2개/제품18파일검증해시동일·미검증제품변경없음·최종마감기록갱신.새개발/전투·모델/빌드·AVD/보호기기입력없음.후속은사용자새지시대기]

## 사용자 재개 — 2026-11-01 오전8시까지 완료 직후 연속 개선

173. 기존5/11을재사용해연속개선/1분재개확인·11월1일08시마감으로갱신한다.실행중에는각작업완료즉시다음작업을이어가고예약을의무대기시간으로삼지않는다.완료트리거전용예약의지원은미확인이므로같은의미라고과장하지않는다. [완료:5/11 ACTIVE·1분재개확인/11월1일08시마감·중복생성없음]

174. 현행1-9의동일초기6개/3시드·첫카드/자동스킬에서중간예산성장선택을비교한다. [분석완료:기준380/3재사용·선택1동등성/새6조건·모두624전멸.460서쪽뇌격69/49/71발이어도결계275/300/222와정리가380기준동일.455강화는300/300/275·2.80~3.25초빠름.차액5·실제원장보존·제품/APK/AVD변경없음.다음위치변수검토]

175. 서쪽뇌격0결과를재사용하고같은460예산/7개에서뇌격생성위치를재료12또는동쪽9로바꾸는제한비교를진행한다.12는같은재료/60초이전원장을그대로두고빈0에창을보충한다.동9는창0↔9의재료강화/조합위치만교환하고봉인12·보충12를유지하되60초이전위치차이를한계로구분한다.같은3시드/카드·스킬과실제원장·결계/전멸·사격/감속·지출을보고조합수치상향전에전선배치효용을분리한다. [분석완료:새6/기준3재사용·같은460/7개.12는기준과같음/동9는결계모두300·186.98/185.30/188.40초/101~108발.60초전강화위치차이분리·조합수치유지.다음178안내개선]

177. 175이후근거있는작은제품개선→적절한회귀/Android검증→기록을끝내고다음우선문제를바로선정한다.불채택후보와완료검증을반복하지않고남은맵참여/가독성·시작진단을필요에따라검토한다. [연속반복]

176. 11월1일07:30부터마감검증우선/08:00이후새개발금지.최종검증APK와미검증후보·실기기/사람재미한계를분리보고하고5/11을PAUSED로중지한다. [기한대기]

178. 조합 안내의 전방 권유를 다른 길 보강/그쪽 재료 재선택으로 바꾸고0.19.2/code56을 Android 빌드·설치/문구·실제재선택/조합·LIVE/취소/저장호환으로 검증한다. 코어6·맵/경제/저장/입력은 유지한다. [완료:0.19.2/code56/f5776743…97346499바이트·2제품파일만/6코어유지·표준회귀/서명16KB/실설치/업데이트저장동일·세화면문구/취소/실제동9조합과포기후저장동일·소유5606정리PASS.시도A/B·실패화면/잔존원장분리·Surface3추가E원문보존]

179. 1-8의현재경로/접근시간·22지점/612출현총수·시드/구매규칙정합을확인하고서쪽긴우회등작은후보를제품밖에서한변수씩비교한다.기준은재사용근거를확인하고필요한선택작전만보완한다.사격/정리·결계·밀도/실제지출·도로안전성을같이검토하고효과없으면폐기한다. [완료:기준9과거정합/우회각9검토·서우회불채택/북우회절충·원장/밀도보완]

180. 179의같은612적/22지점기준9와두우회후보18을재사용하고서·북경로를그대로둔동쪽세번째입구후보9를추가비교한다.이는입구추가/갈래배분의묶음후보로분리하며종류·총량/출현시각·경제와다른작전은고정한다.일반/위기가지형·갈래를공유하므로위기영향도정적으로확인한다. [완료:최종세입구일반9·위기mixed3/612적22지점·전체308/26다른구성유지·후보0시각문제별도보존]

181. 세입구1-8후보0.20.0/code57의표준회귀/Android빌드·서명16KB/설치해시·기존합성저장업데이트/새22지점·세길실제이동/14타워전멸·보상저장을독립Android로확인한다.0.19.2를검증롤백본으로보존한다. [완료:최종0.20.0·57/d4d376…/101223776바이트.회귀/서명16KB/설치해시/업데이트·보상저장/14T27구매1065지출·182.03초612전멸3별24보상·소유5608정리PASS.뇌격0피해/Surface12등한계보존]

182. LIVE화면검토중전투가끝나는QA실수를줄이기위해qa-android에선택적BackgroundAfterShot을추가한다.사진뒤HOME/기존포커스정지로검토중진행을막고동일1-8런에서재개전시간·상태보존을검증한다.게임/저장코드변경이나새시스템은없다. [완료:같은1-8런에서촬영후HOME/8초뒤복귀시Pause·8타워/35마석/촉매0유지.촬영1:14→포커스정지1:15의짧은전이는있으며원자적같은틱정지라고하지않음.상품/APK변경없음]

183. 0.20.0의1-8에서실제뇌격12발사/피해0의원인을고정된구매규칙·지출상한/시드로분리한다.제품밖관측하네스에서12↔9/13/17의모든건설·강화·조합앵커·보충위치를일관되게치환한다.초기배치/60초전강화차이를명시하고종류별발사/피해·최저결계/전멸·실제원장/지출·카드·잔여를비교한다.기준을재사용하고누락관측보완의동등성을확인한다. [분석완료:기준3관측동등/후보7·상한1040실제1040/모두무피해.동17의99~116발/2851.9~3210피해·전체발사지점유지/정리차이.05~.16초.서9는개인피해높아도참여2감소.버프보류·184작은상황안내]

184. 기존타워정보행과조합안내에현재사거리안생존적수를중립적으로표시하고0.20.1/code58을회귀/Android빌드·서명16KB/업데이트·설치해시/실제뒤0·전방양수/조합·취소/LIVE/정상포기저장보존으로검증한다.6코어·능력치·경제/저장은유지한다. [완료:0.20.1/58/f0bb1b…/97345723바이트.6코어동일·회귀/서명16KB/설치/업데이트·약97초후방0·동17양수/취소·실제조합3발7명중/정상포기·재실행저장동일.완주아님/Surface18·ANR0/소유기기종료·로그2개잠금후속]

185. 1-12의북쪽우회한후보를현행기준9조건과같은시드/구매규칙·적총량/출현·경제로제한비교한다.기준과과거지표의동등성을확인하며사격참여·유휴·밀도/전멸·결계를함께보고같은월드위치/시각실험으로쓰지않는다.효과없으면제품불변/새APK·AVD금지.184의잠긴리다이렉트로그는자기프로세스0/전용절대경로·재분석검사후에만재정리한다. [분석완료:북쪽길연장불채택/기준9과거동등+후보9·제품불변/새APK·AVD없음]

186. 대칭쌍U자1-11의동쪽입구를y4→-6으로낮춘다른귀환길한후보를제품밖에서비교한다.서쪽유지/같은종류·수/시각·경제·3시드/구매규칙.일반기준9의과거동등/지점도달·참여/정리·밀도·결계를검토하며좋을때만위기/시각/제품후속검증. [분석완료:동쪽귀환불채택/기준9과거동등+후보9·제품불변/새APK·AVD없음]

187. 1-11지형을유지하고일반후반4~6웨이브도착22→16/12초만제품밖에서비교한다.186의기준9재사용/초기3웨이브와갈래첫출현·다른27구성/종류·총수·경제고정.발사/빈번·유휴·밀도/결계/전멸과원장·실제구매시각차이를기록한다.표준정책은표식미편성이므로표식역할/투자가치로일반화하지않는다. [모델완료:기준9재사용/16·12초각9·16초참여개선후보선택/189실제Android후속]

188. 187의16초파형을채택하기전에1-11첫해금조건(표식허용/관측창미해금·성장연구0)의기존표식모델을선택작전만보완한다.분산/군집의창대표식·같은3시드/카드첫선택·기존구매기회정책/무스킬.표식45·강화40대창40·30의실제지출/시각차이를보존하고표식추가실효피해와전멸/결계·원장을비교한다.전투총량816/경제고정·위험근거있으면제품적용보류. [모델완료:12기준+12후보·816전멸/300·표식연계효과유지/같은예산·투자우위아님]

189. 일반1-11후반16초파형만0.20.2/code59에적용해기존0.20.1을보존한다.후보와제품코어/28정적범위/초기경로·경제정합,표준회귀·호스트Android/서명16KB·새합성저장업데이트/실설치해시·실제표식편성전투/잔여전멸·보상저장을새소유기기로검증한다. [완료:0.20.2/59/6a05295a…/16T1065지출·186.07초816전멸무피해3별30보상·저장/서명16KB/설치·역할/소유정리PASS]

190. 현재1-11첫표식해금에서건설상한7개만둔창대표식/분산·군집/3시드12조건을기존16타워12조건과제한비교한다.원장prefix/카드·실제지출490대505·남는재화/전멸·최저결계/표식연계를구분하며동일가격·동일시각인과실험으로쓰지않는다.제품변경근거가없으면APK/AVD는추가하지않는다. [분석완료:새7타워12/이전16타워12재사용·원장prefix동일/모두승리·분산표식누수·군집무피해.제품과APK불변]

191. 1-12에서표식첫해금/관측창미해금·성장연구0·기존4정책/첫카드·무스킬·3시드의7대16타워건설제한을각12조건비교한다.같은구매prefix/카드·실제지출·남는재화/전멸·결계/연계피해를분리하고기존1-11·다른정책의1-12를새결과로재사용하지않는다.근거없으면제품/APK/AVD불변. [분석완료:새24조건/카드·prefix동일·모두880전멸/7개군집표식1표본276외300.추가630투자정리11.4~16.1초/제품불변]

192. 일반1-12후반기존정예6·공성6만속도1.15/1.25제품밖후보로비교한다.현재속도확인/초기3파·다른13일반/모든위기·880총량/종류HP·출현/경로/경제보존.191기준24재사용·각후보seed7/41×창/군집표식×7/16상한8조건파일럿후유효한후보만보완.압력/승리·기여/정리·실제원장/카드와구매시각변화를보고근거없이조정하지않는다. [분석완료:2후보각8파일럿/기준관측8동등·속도6셀/실제12명만.7창압력동일·군집표식피해추가/빈번참여동일로둘다폐기·제품불변]

193. 속도/지형을유지하고일반1-12후반의갈래별2/3묶음출현순서시간만재배열한다.종류·갈래별수/첫2묶음·갈래첫출현/전체마지막출현·880총량/경제를보존.192기준관측8/191기준24를재사용하고후보각8파일럿에서사격·밀도·표식/결계·정리를함께검토한다.효과없으면폐기/제품과APK불변. [분석완료:각8파일럿·Start64/84셀만/수·종류·갈래·HP속도·경제보존.사격참여동일/정리악화로둘다폐기·제품불변]

194. 새수치후보보다현행0.20.2의아직미확인1-12를독립Android로우선검증한다.기존1-11완료합성저장130마핵33별·편성263을새소유기기에넣고북쪽입구/HUD·경로·전방조합/보강·16타워전멸·32보상/1-13해금/재실행저장범위를검사한다.새APK없이현재전달본설치/소스해시를확인한다.문제가있을때만작게수정하고사람재미/실기기주장금지. [완료:같은0.20.2/16T·17건설10강화1조합1130지출/180.28초880전멸300·100/3별32·162마핵36별/1-13열림·저장0ce7cb…동일/설치·소스·증거·소유정리PASS.첫카드정체미기록·Rup22피해0/북입구막대부분겹침후속]

195. 194의붕괴22피해0을수치상향전에위치변수로검토한다.제품밖하네스에서실제원장의구매규칙·종류/가격·시각을사용하되Native첫카드/시드가미확인이므로동일Native재현이라고쓰지않는다.명시카드정책과3시드에서22↔6/10의모든건설·강화위치를교환하고전방뇌격21/표식7·총880/경로/경제/제품코어를유지한다.개인·팀피해/사격·정리/결계·자원과초기창위치변화를함께보고근거있을때만작게적용한다.새APK/AVD강제금지. [분석완료:9조건/동일카드·28구매1130/잔여32·모두880전멸300.붕괴6은73~74발/3919~4032피해·정리.87~1.80초단축/최근동시일부감소.10은3~8발·참여감소일부.수치불변/초기창위치도교환한정책비교]

196. 195의입구범위공격근거로기존1-12준비Lesson만두짧은문장으로바꾼0.20.3/code60후보를검토한다.메뉴·능력치·경제·진행/저장·초반/다른작전은유지하고동일전투정적·기존5코어/Presentation해시를확인한다.필요한회귀/Android빌드·서명16KB/설치·업데이트합성저장동일·세화면안내와짧은실제전방붕괴사격/정상포기를검증한다.전체전투완주는0.20.2자료와구분한다. [완료:0.20.3/60/c4cfa052…/97345747·안내/버전만·세화면완전표시/약57초7T385재구성·붕괴13발48명중/LIVE·포기·업데이트재실행324동일/서명16KB설치·소유정리PASS]

197. 194의북쪽균열/웨이브막대부분겹침을렌더의실제확대범위와현재카메라·SafeArea/3화면·다른북쪽입구로먼저계측한다.정보·배속/보스HUD/정지·메뉴입력과전투코어는유지하고작은기존레이아웃후보만검토한다.한스테이지에임의좌표를붙이거나모든HUD를무조건옮기지말고다른진입을가리는회귀를피한다.효과불명확하면제품불변/새APK·AVD강제금지. [분석완료:42기하/불투명bbox측정·기존겹침11/경계여백포함12변경/30불변.140폭예비폰트위험·150gap6좁은보스실패폐기/150gap4후보42통과·Android후속]

198. 197의일반적인최대경고가드기반상단웨이브배치를0.20.4/code61후보에적용한다.원래중앙이안겹치면그대로두고필요시자리이동/150폭만허용하며경과시간·6분할진행·보스바·기존버튼/게임플레이는유지한다.31모바일검사/42기하·실제Unity폰트4자리폭검사/회귀·Android서명16KB·설치/업데이트저장/1-12세화면LIVE·메뉴·배속·1-6보스HUD후속·포기저장을확인한다.전체전투재완주로쓰지않는다. [기능검증완료:0.20.4/61/140fe4b1…/56기하·14위기쌍·31모바일/Unity폰트·서명설치·북입구3화면/복구LIVE·보스명체력·예고/정상포기저장PASS.초기A프레임워크재시작원인미확정/원시보존·B회복/잠긴2로그정리보류·전체안정성완료아님]

199. 198의Android프레임워크재시작/타임스탬프없는호스트invalid ColorBuffer4쌍의관계를제품수정전에분리한다.기존system/events/crash/exit-info/host로그·버전·가상화설정을먼저읽고가설과관측한계를적는다.유용할때만소유분리기기의게임없는같은리사이즈대조등제한검사를설계한다.대조가조용해도게임원인배제/해결로쓰지않고모듈/APK를근거없이추가하지않는다.9296/36556생존0·전용절대경로/재분석을재확인한뒤잠긴본인2로그만정리재시도한다. [원시진단완료:Watchdog/GNSS잠금68초후system_server종료확정·상류원인/게임인과/실기기미확정·18제품/APK불변·새AVD0·검사PASS/소유로그2개보류]

200. 현행1-13의경로/첫해금·적총량/진행과기존모델·실제Android검증범위를먼저대조한다.기존자료로대답할수있는검사를반복하지않고첫출전에서의편성/조합투자·전투참여에가장큰미검증한곳을작게선택한다.제품/새APK·AVD는근거없이늘리지않는다. [점검완료:1-13첫출전미검증/관측창1-14후해금·201선정]

201. 기존0.20.4APK를새소유기기/1-12완료합성저장으로설치하여1-13의세진입경로와24지점·분산/집중투자·표식연계/한전방조합·예약전멸·34보상/1-14열림·재실행저장을검증한다.모델과정확히같은입력/사람재미라고쓰지않으며문제가있을때만제품수정한다.관측창해금은1-14완료후다. [완료:0.20.4유지·16T1050/188.57초928전멸무피해3별34·1-14열림/944dc975저장동일·설치검사PASS/소유종료·2로그보류]

202. 201의24구매1050/첫Blast·후Range우선/3시드에서표식14와기존창7또는22의모든위치사용만치환한제한9모델을비교한다.초기표식/창위치또는후반창건설위치도같이바뀌므로단일표식좌표의인과/Native정확재현이라고쓰지않는다.뇌격17/재료16·스케줄928·재화/다른코어는고정하고원장·카드·지연/사격참여·실효연계·결계·정리를함께검토한다.근거없이표식수치를올리거나새APK·AVD를만들지않는다. [분석완료:9조건/1050·16T·카드동일·모두300/북발사증가도보너스0~52·서보너스증가하지만참여감소/제품유지]

203. 202의3기준고투자결과를재사용하고1-13의초기7/315와34.9초까지강화·창1개보충한8개/425원장을공유한다.71.5초에같은40마석으로동쪽23창추가(9개465)대17뇌격조합(7개465),그리고425유지를3시드씩제한비교한다.추가구매/차액재투자없이동일카드규칙·재화/전투/별과실제지연·잔여/개별사격·정리·결계를보며재미/보편조합우위로일반화하지않는다.근거없으면수치/APK/AVD유지. [분석완료:새9/고투자3재사용·동쪽465창/조합과425유지결계·정리동일·제품불변]

204. 203의동쪽23창/17뇌격·425유지3시드원시를재사용하고71.5초추가창위치만북13/서9로바꾸는각3조건을비교한다.실제40마석·465총지출/9타워·공통425원장prefix/같은카드와전체928/경로·경제를유지한다.기존3정책코드역변환일치검사후확장하며방향·생존/정리·사격·미사용재화를함께본다.통과한한곳을무조건전역추천하지않고근거있는기존안내만개선후보로삼는다. [분석완료:추가위치만북13/서9각3·북465/9개모두300·원장/카드동일·제품불변]

205. 기존0.16의1-14완주/관측창해금·저장근거는재사용하고전체1-14전투는반복하지않는다.현재코어의관측창게이트/기존모델은있지만실제Android관측창조합·편성/넓어진타워메뉴근거가없으므로,과거완료합성저장을새소유기기에넣어현행0.20.4에서편성전환/해금표시·Lv2창+표식조합·관측창사격과강화/LIVE·정상포기/이전보상보존을짧게검증한다.새APK/밸런스변경은문제가있을때만진행한다. [기능완료:실제편성/관측창생성·Lv2·39발39명중·포기저장보존/긴메뉴겹침206수정]

206. 205의실제관측창Lv1/2메뉴설명줄바꿈/범위행겹침·강화피해누락을MobileHud의관측창능력치/역할행만짧게분리해고친다.실제유효피해현재→강화·사거리와표식우선3초/현재범위적/공명은유지하고글자크기/패널/입력·전투6코어는유지한다.실제Unity14/13폰트240폭검사·필요회귀/Android서명16KB/같은소유5622업데이트저장호환·세화면Lv1/2/LIVE·정상포기보존을검증한다.관측창기능검사205와UI수정206범위를분리하고미검증APK는완료로쓰지않는다. [완료:0.20.5/62/0414ad9c…·6메뉴/폰트/LIVE·서명설치/포기저장동일·소유종료/2로그보류]

207. 206에서확인한ADB일시미연결/하위스크립트실패후계속진행하는QA호출문제를작게보완한다.명시Serial연결상태를짧게재확인하고미연결이면입력·추출을시작하지않는다.원래보호Serial/증거경로제약을유지하며공유ADB서버/다른프로세스를종료하지않는다.같은프로세스의직접스크립트호출과카드임계전후의현재화면확인을QA문서에적고중요실패가후속명령을막는제한검사를한다.제품/APK/새AVD추가없음. [완료:모의연결3사례/실패전파·보호가드·기록불변/실제재연결·제품변경없음]

208. 206의좁은화면좌표를수동으로계산한원인을없애기위해QA맵투영도구에현재Width/Height를명시하고FitCamera의Max(12.6,24.8/aspect)를반영한다.기존2400기본출력24지점동일과실제검증한관측창20의1920/1280좌표를대조한다.메뉴종류별타일가정은그대로명시하고범용터치성공/새Android검증으로주장하지않는다.제품/APK/새AVD없음. [완료:24기본출력동일/세화면현재투영·기존Native좌표대조/후보0보존·제품불변]

209. 관측창해금후1-13재도전조건에서동일195재료·조합비/24구매1050의뇌격17대관측창17을제한비교한다.202기준3을재사용하되해금flag가다른선택seed19뇌격1개로정합을확인한다.새관측창3시드는초기재료16의봉인→표식/레시피0→6만바꾸며재료전투단계도달라짐을분리한다.같은카드·실제가격·원장시각/참여·표식연계·전멸/결계를보고현재첫1-13출전에는관측창미해금임을유지한다.재도전분석을첫캠페인/보편투자우위나Native재현으로쓰지않고제품/APK/AVD는근거없으면유지한다. [분석완료:선택control1동등/새관측창3·기준3재사용·동일1050/16T·무피해/시간동일·재도전한정/제품불변]

210. 209의충분한16타워조건은병목을가리지못하므로관측창해금후재도전의같은7타워465예산에서북13앵커뇌격대관측창을선택시드7/41부터제품밖파일럿으로비교한다.201원장초기창17과강화를13으로옮기고71.5초조합도13/재료16으로고정한다.파트너16의봉인대표식차이와초기동쪽창공백도함께달라지는구매정책임을명시한다.양쪽가격195/후속구매없음·동일카드규칙/총928·경로재화·저장·제품은유지하고검증효용이있을때만seed19를추가한다.원장/실제지연·결계/전멸·사격과초기재료기간의기여를같이본다.부족한전선과다른전선손실을확인하며수치상향/새APK·AVD를강제하지않는다. [파일럿완료:4조건/465·7개·카드동일·두종류각시드같은결과·seed19생략/제품불변]

211. 210의약한전선/조합효용을추측하지않도록현행CoreHurt·진입경고피드백과갈래출처계측가능성을읽기부터확인한다.필요하면제품밖코어복사본의피해발생지점에읽기콜백만추가한선택seed41/한정책1조건으로기존결과·원장·사격/종류피해가동일함을먼저확인한다.추가계측은피해갈래/종류/시점과총피해합정합만측정하고다른원시를새관측으로쓰지않는다.기존피드백에부족함이확인될때만기존진입표시/결계피격안내를작게보완하는후보를기획한다.새게임시스템·가격/수치/저장변경이나새APK·AVD를강제하지않는다. [완료:선택1동등/동쪽공성187.2·정예112.8=유효300·요청306.8/제품불변]

212. 결계피해이벤트에캠페인진입갈래Source만전달하고기존Notice가비어있으며카드화면이아닐때만방향피해안내를표시한다.기존웨이브/보급/보스경고·카드안내를덮어쓰거나새HUD/전투수치/경제/저장을추가하지않는다.진입/보스경고Source계약을유지하고3갈래실제피해이벤트검사·기존선택모델동등성/필요회귀·Android빌드서명16KB·독립합성저장/실설치/피해방향과정상포기를검증한다.필요검증전에는후보로만기록한다. [완료:0.20.6/63·Source메타데이터/기존알림만·3갈래49검사/선택1동등·Android북쪽실표시·4저장동일·전체완주아님]

213. 관측창에서실제발견했던설명겹침이타워다른종류에도있는지현재StatLine/역할행과실제Unity폰트·해금별메뉴최소폭부터점검한다.최소240폭의한계검사는실제각메뉴폭과구분하며카드/레벨·범위적자리수·공명을선택표본으로검토한다.제품은먼저유지하고실제도달가능한문제가확인될때만기존정보행을간결하게보완/필요Android검증한다.새시스템·전투수치·저장변경은하지않는다. [감사완료:37표본/실제해금4좁은메뉴254.5~258.3>240·352후보262폭은1행/표식공명은불가능조건제외/제품불변]

214. 213의실제가능한1-2봉인/1-3뇌격/1-4파쇄·중력Lv1좁은메뉴줄바꿈을최소패널330→352만으로수정한다.기존글자크기/모든문구/버튼과행높이·LIVE입력·전투6코어·경제저장은유지한다.독립Android에서0.20.6의1-2봉인메뉴겹침을먼저재현하고새APK세화면/Lv1·강화Lv2·LIVE/패널차단·업데이트/포기저장과설치해시를검증한다.후보는검증완료전전달로쓰지않는다. [완료:0.20.7/64·352폭1상수/버전2파일·6코어불변·봉인6메뉴/LIVE/입력·4저장/설치서명16KB·전체전투아님]

215. 현행1-13/928적의동일24목표구매1050정책에서호출없음기준202의3시드를재사용하고선택seed19의WaveCallsEnabled=true/호출없음control1로정합을확인한다.새시드7/19/41×가능즉시/잔여0일때만호출의6모델을비교한다.원장목표시각은같아도전투조기종료로일부구매가미완료일수있고재화/실제시각·최종개수차이를분리한다.호출5/최대30초·보급5회500/카드3/촉매2상한과전멸/결계·피해출처/참여·압력을함께검토한다.완료전체런을다시하지않고근거없으면제품/APK/AVD를유지한다. [분석완료:control1동등/새6·기준3재사용·즉시5회30초단축/최대생존41~48·전부300·1050/16T·제품불변]

216. 215의충분한16개조건만으로호출이항상안전하다고보지않도록기존204북13창보강9타워465정책의seed41부터호출활성/없음control과즉시/잔여0후호출파일럿을비교한다.초기공통10구매425뒤71.5초창13하나40만추가하며나머지재화는보존한다.기준3을재사용하고control의원장/결과/카드/피해·사격동등성을확인한후구분되는효과가있을때만시드7/19를보완한다.상한·전멸/최저결계·누수/정리·적중첩·가격·카드와실제구매시각을분리하고새수치/APK/AVD를강제하지않는다. [분석완료:control41동등·새후보6/기준3재사용·즉시41만피해24/나머지300·전부928전멸/465·9T·제품불변]

217. 사용자요청인더많은타워참여를직접검토하기위해현행1-13의동일초기315/7타워·최종1050예산에서기존16개강화·조합정책과24개기본타워중심정책을비교한다.초기7구매는같고붕괴Lv2강화55+남은17지점창40씩으로1050을정확히쓴다.기존추가건설지점우선·나머지표준도로순서/기존후속목표시각에160.2한시각추가로단순정책을명시하며구매시각·지연/카드/배치도달라져순수개수인과가아님을유지한다.현재215의호출없음control정합과202기준3을재사용/새3시드만실행하고전멸/결계·실제최대건설·지점별사격/최근참여·압력·지출잔여를함께본다.근거없이제품변경/새APK·AVD·모든24상시사격을강제하지않는다. [분석완료:새3·1050/24건설/928전멸300·발사약25%증가/사격지점·최근최대는증가안함/9~10유휴·제품불변]

218. 217의많은타워정책에기존조합을같이담을수있는지1-13첫11원장465/7개·뇌격17을유지한후남은17지점창40씩을보충해24개/총1145목표로검토한다.1050정책과예산95차이는명시하고가격보정/재화삭제/수치변경은하지않는다.기존후속시각13개+160.2/162/164/166네시각에건설목표를두되실제수입지연·완료개수·조합후사격·투자유휴·정리/결계를본다.기준16/1050과24/1050은재사용하고새3시드만실행한다.사격참여가늘지않으면패드/강화추가를강제하지않고제품/APK/AVD는검증근거가필요할때만사용한다. [분석완료:새3·1145/24개/조합1/28구매·928전멸300·36잔여·마지막171.3~173.4·제품불변]

219. 기존실제1-13은16개조건이므로현행0.20.7의24개+기존뇌격병행을독립Android합성저장으로한번검증한다.218의초기/조합·보충원장과실제좌표를참고하되현재타워종류별메뉴폭·카드·재화·시각을보고행동한다.0.20.7동일APK/실설치해시·사용자저장보존·예정24개실제건설/조합·LIVE/가독성·전멸/보상저장을보고기록한다.모델과카드/입력/구매시각이다르면정확재현이나비교인과로쓰지않는다.모든24사격/사람재미/실기기·성능벤치마크를주장하지않고새제품/빌드는추가하지않는다. [완료:동일0.20.7/24개·28구매1145·183.87초928전멸300/34보상·3실제저장동일/소유정리·2로그보류]

220. 실제219의24건설성공을모두사격/보편투자우위로확대하지않고기존218의세시드24개/1145원시에서후방유휴·건설순서/시각·현재타워와소모재료를구분한다.우선재집계만하고현재도로접근시간과비교해단순건설수부족/사거리버그추정을피한다.유휴비용을회수가능손실이나팀피해손실로단정하지않으며신뢰할만한작은위치/구매순서후보가있을때만기준재사용/선택표본을실행한다.새제품/APK/AVD는강제하지않는다. [완료:기준3재사용/후보2·22조기건설개인발사증가에도참여2개감소·불채택/19생략·제품불변]



221. 이미차별화한1-7/1-8/1-10과실패후보들을반복하지않고,아직서동거울형인1-5외곽중정의한쪽우회/북쪽공유목작은후보를제품밖코어복사본에서검토한다.현행기준선택9조건을과거0.17의1-5와대조하고동일스케줄·24지점/초기재화·카드/구매/자동스킬규칙으로후보를비교한다.28일반/위기정적비교에서선택1-5의지형외다른변경이없음을확인하고초반1-1~1-3은그대로유지한다.길을늘리는것/공유목/최근사격하나로채택하지않고사격참여·정리/압력·지출·최저결계를같이본다.실패는폐기/제품APKAVD불변,유효후보만후속Native를기획한다. [완료:기준9정합/후보9+파일럿3·참여/빈번감소로둘다폐기·다른시드생략/제품불변]

222. 설정의버전이실제0.20.7인데고정v0.5.0으로표시되는문제를실제기존APK에서확인한뒤Application.version으로표시한다.기존패널/문구위치·글자/입력과전투6코어·경제·저장은유지하고다음Android버전0.20.8/65를빌드한다.새소유5630의합성저장으로이전표시재현·업데이트직전/직후실제추출·설정현재버전과세화면·도시복귀/재실행저장동일을확인한다.실제확인전후보로만기록하며전체전투를반복하지않는다. [완료:0.20.8/65·세설정화면/실설치·4저장동일/6코어불변·소유종료·2로그보류]


223. ADBdevice와부팅완료가다름을실제222실패에서확인해qa-device-ready에선택BootCompleted읽기검사를추가한다.기존연결모드/Serial보호·후속실패중단을유지하고모의ready/booting/회복/잘못된exit1/연결전용/offline/flaky7사례를검증한다.새옵션실제positive는222설치앞에서확인/모의회복을실제재연결추가검증으로쓰지않는다. [완료:도구만변경/7모의검사·제품/APK변경없음]
224. 태블릿/짧은화면의해금후최대타워메뉴가실제잘리는지설계읽기와현행MobileLayout으로먼저점검한다.최소reference960/스케일정의와도달가능한레시피·레벨/로드아웃을반영하여단순화면폭나누기추정을기각한다.Gravity ShortRole 흡인등은실제호출가능분기인지확인하고도달불가표현때문에제품변경을하지않는다.유효문제가있을때만짧은실제Android재현/수정계획을선정하며새빌드/AVD강제없음. [완료:정적표본최대908+24<최소960/넘침가설기각·Gravity문구도달불가/제품불변]


225. 현재선택기본타워로생산할수없는상위재료를요구하는조합버튼이전투중노출되는지점검한다.1-13창만편성은중력을못만들지만현재FusionOptions는중력의작전해금만봐서종결버튼을보여줄수있다.기존편성결과의도달성원칙과실제보유타워를반영하는읽기조회로불가능버튼만숨기는후보를검토하되재화/레벨/촉매부족등미래가능한조합은유지한다.연구/밸런스/기존전투/저장형식은유지하고명시편성사례·잠금/실제보유재료예외를검증한다.현재APK에서독립합성저장준비메뉴를먼저재현한뒤유효할때만다음APK로적용/회귀·서명16KB·설치·저장호환/실제메뉴를확인한다. [완료:0.20.9/66·3Native편성/가능조합·부족안내/LIVE·4+2저장·설치서명16KB/소유정리·2로그보류]


226. 새읽기조회와기존편성화면LoadoutResults의도달성표시가다른지후반유효편성30종×관측창해금전후에서정적대조한다.추가제품변경을강제하지않고일치하면그대로두며,제품/Android메뉴225완료범위를확대하지않는다. [완료:유효30×해금2×모드2/120정적일치·제품불변]


227. 새가족조회가매프레임복수타워에서호출되는비용을호스트워밍측정으로만확인한다.0/8/16/24타워query전용fixture·각1만조회/할당과시간을기록하고상태불변을검사한다.프레임전체/Android성능이나무료인게임배치로확대하지않고하드웨어시간문턱을회귀조건으로삼지않는다. [완료:4표본할당0/10000회3.60~4.76ms·제품APK불변]
228. 도시/일시정지/HOME시간이Running로그에섞이는현재PERF측정공백을점검한다.기존10초로그·효과/적카운터는유지하면서집중된연속전투프레임만측정하고준비/일시정지/카드·포커스상실의시간및경계프레임을제외한다.샘플0은na로명시하고실제전투중긴프레임을일괄잘라내지않는다.단위시간선/고정버퍼검사와요약도구의legacy혼합자료구분·필요짧은독립Android정지/재개관측을검증한다.전투수치·게임속도/입력·저장/새시스템은추가하지않으며에뮬레이터측정을사람재미/실기기성능으로쓰지않는다. [완료:0.21.0/67·Ready/Pause na·2Active구간944표본·4실제저장/설치해시·소유정리/잠긴로그2]


229. QA screenshot pull 실패 때 동일 캡처만 제한 재전송하고 부분 파일을 보존하여 입력 재실행을 예방한다. 명시 serial/보호기기/실패전파와 기존 HOME 순서를 유지하고 탭·촬영이 재시도되지 않는 모의 검사를 한다. 제품/APK/새AVD 없이 도구만 검토한다. [완료:도구3파일/7모의사례·입력촬영재실행없음/부분보존·실패전파/제품APK불변]

230. 일반1-4의 현행 물량에서도 첫 본격 전투가 수동 스킬 사용에 과도하게 의존하는지 제한 비교한다. 과거0.17의 같은1-4 자동스킬 기준9는 재사용하고 현행mixed/seed7 control1만 정합 확인한다. 첫 카드/구매정책·시드·위치 규칙을 유지한 채 UseSkill만 끈 seed7/41×3정책6파일럿을 관측하고 결계/전멸·영웅자동공격·참여/재화·실제구매시각을 구분한다. 사람의 초보플레이/무입력과 동일하다고 쓰지 않으며 근거없으면 수치·APK·AVD를 유지한다. [완료:control1과거정합/6수동스킬없는파일럿도14개300전멸·동일지표/추가seed19생략·제품불변]

231. 일반1-4의 같은 초기6개(창5+봉인1)/245지출에서 총395 동예산의 확장(창3+창강화1), 강화(창5강화), 조합(두재료강화·뇌격·창보충)을 비교한다. 같은3시드·첫카드·무수동스킬/구매목표시각을 기록하고 재료·카드·수입·실제지연을 보존한다. 실제가격/남는재화를 그대로 두고 모든조건이 무피해면 너프근거로 쓰지 않는다. 모델은 인간판단/Native가 아니며 제품·AVD를 강제하지 않는다. [완료:9조건모두395/768전멸·확장결계250~287/강화172~216/서조합120.6~156·동쪽피해/제품불변]

232. 231의395서쪽조합3을재사용하고 초기6종류/위치·봉인19는그대로둔채강화/조합재료창12→동14로선택만바꾸는3조건을비교한다.65초조합/80초봉인자리창보충은유지하고15초창강화위치도바뀌는정책차이를분리한다.하네스확장control1동등/카드·초기원장·395지출검사후동쪽누수/뇌격피해/정리를보며근거없는버프·새APK/AVD는하지않는다. [완료:저장control1동등/후보3·동14결계253·300·229/개인4973~5182·기존안내유지/제품불변]

233. 최종1-14의 최신스웜/효과와 새ActiveCombat 계측에서 진행·밀집구간 및 실제 첫완료관측창해금/새도달필터후UI가 정합한지 같은0.21.0APK로 독립Android를 검증한다. 이전1-14완주의 단순반복이 아니라 새scope 실제 전투표본/1-14→해금저장→새도달필터Native의 합쳐진 공백만 검사한다. 기본편성263/사용자저장없는합성1-13완료에서 실제입력·카드/건설을기록하고 전멸/보상·재실행저장·해금뒤조합메뉴를확인한다. 앱PID/프레임표본은에뮬레이터저부하진단과구분하고실기기/FPS성능보장은하지않는다. 제품·새APK는만들지않으며한소유AVD로끝낸다. [완료:동일0.21.0/1-14 187.12초944전멸300·16개1060/36보상관측창해금·4저장동일/해금Ready메뉴·입력차단/실설치·소유정리/로그2보류]
234. 233 새기기실제safe-height944/originY136을확인하여기존좌표도구가safe-left만받아세로메뉴좌표를틀리게투영하는공백을수정한다.실제MobileLayout 생성과SafeBottom/Width/Height를사용하고기존전체높이기본출력좌표를대조한다.월드패드는전체카메라/메뉴는safe좌표로분리·현재스테이지메뉴첫실화면으로검증한다.제품·새APK/AVD는없고233소유기기는도시HOME대기. [완료:실제안전영역/기본24좌표동일·현재Native3건설125/195마석·제품APK불변]


235. 실제1-14 원장27구매/1060·16타워에서 표식13 대 미사용 전방21/23 위치만 바꿔 역할을 비교한다. Native 시각/카탈로그 가격을 재구성한 제품 밖 정책·시드7/41/30·90 Blast우선/150 Ballistic우선(제시없으면첫카드)으로 기준2+후보4부터 관측한다. 실제카드/구매지연·초기위치 차이를 기록하고 Native 난수·정확입력재현/개인피해의 팀추가합산/보편투자우위로 쓰지 않는다. 유효할 때만19보완·제품수치/APK/AVD변경강제없음. [완료:Native첫23원장940으로범위분리·기준3+후보6/9모델·전방표식연계증가/모두300·시간참여동일·수치불변]

236. 1-14 첫 전투 준비 안내를 현재 선택한 표식의 입구 창 겹침과 실제 관측창 해금 보상으로 정리한다. 표식을 편성하지 않은 경우도 존중하는 조건 문구를 쓰고, 다른13작전·전투/경로경제·저장/UI입력은 유지한다. Campaign.Lesson 한 문자열과0.21.1/code68버전만 수정·회귀/Android서명16KB·독립합성저장 업데이트/세화면문구·정상포기재실행저장을 확인한다. 기존0.21.0 전체1-14완주는별도이력으로유지·새버전전체전투주장금지. [완료:0.21.1/68·Ready3화면/0초·4실제저장동일·설치서명16KB·소유정리/로그2보류]

237. 1-14 서쪽만 엇갈린 우회 후보를 제품 밖에서 비교한다. 동쪽/게이트·944총량·웨이브경제·24지점/전체308과 다른13맵은 유지한다. 선택현행control1과 과거정합을 먼저 확인하고 기준9는 근거있게 재사용/후보seed7 세정책부터 참여·압력·누수·정리·구매차이를 비교한다. 지형변경은 월드좌표/구매시각을 바꾸는 정책실험이며 단일타워 인과아님. 공간검사 실패/참여악화면 폐기·유효하면 나머지시드/공유위기범위를 보완한뒤만 제품 적용한다. [완료:control1정합/후보3·참여10→8/정리악화로폐기·제품APK불변]

238. 237에서 단순 우회 증가가 실패했으므로 서쪽 길을 북쪽 중앙으로 돌려 동쪽의 마지막 목(6,2→2,2→결계)을 공유하는 별도 후보를 제품 밖에서 검토한다. 두 게이트·동쪽원형·944출현/경제/24지점은 고정하고 선택기준control1/과거9를 재사용한다. 파일럿seed7 세정책에서 참여·지출·정리/압력을 함께 비교하며 효과없으면 보완실험·제품수정없이 폐기한다. [완료:9조건·상단y8도로/HUD겹침으로미채택/하위239로분리]

239. 238공유목 후보의 실제상단투영 충돌을 없애기 위해 중앙연결 y8→6만 낮춘 별도 제품밖 후보를 검토한다. 원본동쪽/2게이트·944종류HP출현시각·경제와24지점 고정. 3화면 도로중심/외곽1.42 투영을 기존HUD와 대조하고 seed7세정책부터 유효할때만19/41 및 공유위기검증을 보완한다. 빈번사격·지출·압력·정리도 함께 비교·근거있을때만 제품 적용/필요Android검증. [분석완료:일반9/위기6·초기7개6/세화면투영0·맵단독보류→240]

240. 239 맵단독은 사격참여가 늘지만 초기7개310 무보강도3승(결계180~300)·212~216.6초로보강필요성이줄어제품적용보류다. 같은 후보맵을 고정하고 일반1-14후반4~6파도착22→16/12초의 별도제품밖후보를 비교한다. 초기3파/같은갈래첫출현·944종류HP·재화·다른13일반/모든위기스케줄은유지. 기존맵단독9와7개3를재사용/선택조건부터압력·보강/무보강·참여/정리·실제구매와카드차이를본뒤만채택한다. 두효과를혼동하지않고효과없으면맵수정도보류한다. [분석완료:16초파일럿4/12초9+저투자3·선택후241제품Native계속]

241. 239/240선택지형+일반1-14후반12초를0.22.0/code69후보로적용한다. BattlefieldLayouts1개/Raon조건2곳·버전과경로선언외불변을감사하고표준회귀/Android서명16KB·이전0.21.1업데이트/합성저장보존/3화면공유목·밀집전투PERF·944전멸/보상해금재실행저장·자기기기정리를완료한다. 실패하면이전검증0.21.1을최종으로유지하고새전달완료주장금지. [완료:0.22.0/69·20T1080·183.62초944전멸300/36보상·3저장동일/설치서명16KB·소유정리/로그2보류]

242. 0.22.0검증본을 유지한 채 1-14 북쪽 짧은 갈고리 진입 후보를 제품 밖에서 검토한다. 기존 서·동/944종류HP수·각생성셀시각/경제는 고정하고 세 번째 북쪽 갈래로 배분만 바꾸어 상단 타워 참여·보강 판단을 본다. 북gate(7,11)→(7,7)→(10,7)→(10,4)→(6,4) 뒤 기존공유목을 합류하는 한 후보다. 준비/Running 게이트 잉크·HUD/도로/24지점부터 계측,선택현재6코어와240원시동등성을확인해기준9재사용·seed7세정책파일럿부터 유효할때만보완한다. 새로운시스템/무조건4갈래/수치버프·새APKAVD를강제하지않고 효과없으면폐기한다. [완료:갈고리기하폐기·단순북17.33/24지점/12HUD투영·일반9/저투자2/위기3·선택후243]

243. 선택 북쪽 짧은 진입을0.23.0/code70후보로 적용한다. 기존 서·동/944생성셀 종류·HP·시각/재화/다른13작전은 고정하되1-14 양모드의 갈래 배분·첫 갈래시간/지점은 바뀐다. Campaign갈래/주의안내2문구·Layout·Raon시각보존조건·버전/경로선언만변경. 회귀Android서명16KB·이전0.22실저장업데이트동일·3화면/북경고·전멸/보상재실행저장·자기기기정리를검증한다. 검증전최종은0.22버전별보존/실패후보전달주장금지. [완료:0.23.0/70·20T1140·175.60초944전멸300·36보상/저장3동일·설치서명16KB·소유정리/작은로그2보류]

244. 새1-14 실제28구매1140/20타워의 표식21은71.16개인피해·23연계/48보너스로작았다. 제품밖에서같은원장/카드규칙·3시드의현재21 대 미사용전방후보를 위치만 치환해 제한 비교한다. 재료/보충/후속창 충돌과 실제지출·구매시각을감사하고 연계·피해·현재ID사격/정리·결계를함께본다. 우선7/41에서유효할때만19보완·개인보너스중복합산/무조건수치버프·새APKAVD강제금지. [분석완료:9모델·20지원기여상승/전역결과동일·0은41일부개선/연계0·제품유지]

245. 244표식21기준3시드를재사용하며다른모든28원장/1140·카드·재료/보충·표식21을고정한다.98.3초창12한구매만미사용0으로옮겨북지름길후방지원을시험한다.Range부재41의결계압력과7/19의無피해/사격/정리·카드/실구매정합을함께본다.먼저7/41후유효할때만19·필요한피해갈래읽기관측한조건으로제한한다.제품변경강제금지. [분석완료:새후보3/기준3재사용·41읽기관측2동등/북피해184.2→37·제품유지]

246. 245북창0보강후보를기준으로123.9초의추가동창13건설40한번만북창0강화30으로바꾼다.1140동일상한/실제1130·잔여56·최종19타워로10가격차이를보존·재투자하지않는다.같은시드카드·나머지27구매/시각·초기7/재료/표식21을고정하고7/41부터승리결계·정리·동쪽방어/참여를비교한다.효과유효할때만19보완·근거없는제품수치변경이나새APKAVD금지. [분석완료:새3/기준3재사용·19T1130/차액10유지·41結界263→287/다른2무피해·참여일부악화/제품유지]

247. 1-5의거울형외곽도로는서41.24/동40.24·24지점780적이다.서쪽과스케줄/경제를고정하고동쪽만남쪽긴우회(23,5→18,5→18,-8→6,-8→6,-3→-9,-3→기존서쪽꼬리)로바꾸는제품밖후보를검토한다.다른13작전·초기/종류HP수시각·24지점/안전거리를보존·현재기준seed7세정책3만새로확인하고후보3과사격/빈번/현재ID최근·빈구간/압력·정리/결계·실구매정책차이를본다.효과없으면추가시드·Native없이폐기,유효할때만보완한다.모든맵4갈래화/제품자동채택금지. [완료:현재기준3/긴동우회3·기하24/28정적일치/조합참여악화·정리지연폐기·제품유지]
248. 247의긴동우회는조합참여악화/정리지연으로폐기했다.같은현행기준3을재사용하고동원형고정·서길도41.24길이유지한채(-9,5)→(3,5)→(3,3)에서동쪽마지막꼬리를합류하는별도후보를검토한다.780종류수HP/전체스케줄·경제/24지점·다른13/동길은보존·기하통과시seed7세정책3부터.효과없으면추가실험/제품없이폐기하고유효할때만보완. [분석완료:기준3재사용/새3·길이동일/24지점/780·참여혼합·단독적용보류/249파형분리검토]

249. 1-5후반은일반22초arrivalWindow가아니라3개교대묶음span15.5+(위상0/.25/.5)와Swarmstep.25/multiplier특수식이최종적용된다.그구조/780종류수HP·갈래첫출현·초기3파/모든위기·다른13/경제를보존하고일반후반묶음span12/10만제품밖에서원형/248길이동일서합류각각분리비교한다.현재기준3/서합류3재사용·각선택seed7세정책파일럿부터유효할때만시드보완.지형효과와파형효과를혼동하지않고참여/압력/정리·실구매·낮은투자보강필요성/가독성을함께보고채택자동화금지. [분석완료:새30/기존3재사용·4후보112정적·원형10선택보류→250/강화9무피해·초기7모두승리·제품불변]
250. 249선택원형10초묶음을고정하고일반1-5후반의기존정예8/공성11만속도1.15/1.25배를제품밖에서비교한다.현재.9775/.88·HP242/440를먼저실측하고780/나머지761·초기3파·다른13·모든위기·스케줄/길·경제를보존.기준강화9/초기유지3재사용·각후보7/41×다수/조합/7창유지6부터·효과있을때만빠진강화2/19세정책+초기19총6보완.정리/기여·결계압력·HP/속도840생성셀6만차이를확인하고무조건너프/채택·제품APKAVD강제금지. [분석완료:새18/기준12재사용·115보강9무피해/초기7모두승180/180/112.8·125초기2패폐기·6속도셀19명/선택후251제품검증]
251. 249/250 선택인 원형1-5의 후반 교대무리span10·기존정예8/공성11속도1.15를0.24.0/code71후보로적용한다. Raon/Defense/버전3제품파일외17파일·다른4코어/Presentation·맵/780HP수/초기3파·다른13/위기/경제/저장을보존한다. 최종0.23.0버전별보관·이전실저장업데이트동일/새소유AVD합성첫1-5·분산건설/강화/조합·780전멸/보상/1-6해금·무주입재실행저장·서명16KB/설치해시/대표성능/자기정리를완료한다. 실패시후보와이전검증전달을분리. [완료:0.24.0/71·18T1095·2조합/173.77초780전멸300·18보상/저장3동일·서명16KB/설치해시·소유디스크정리/작은로그2보류]

252. 251 Native 실제26구매1095/18타워의 고정 시각·위치·카드 규칙을 제품 밖에서 재생해 종류별/지점별 사격·직접피해와 후반 확장/조합 기여를 검토한다. 우선seed7 한조건의 원장/가격·카드 정합부터 확인하고 유효한 한정 비교가 필요할 때만 41/19 또는 한 구매 후보를 보완한다. Native seed미기록/수동일시정지·카드 차이를 정확재현이나 사람재미로 주장하지 않고 완료전체런/9기준·새APKAVD를 강제하지 않는다. [분석완료:신규기준3/마지막1구매후보1·전체780/300·최종18중10사격/후보.67초단축/제품유지]


253. 현행14작전의 경로·진입 수/접근시간·지점 구성을 정적으로 비교해 아직 유사한 대표 한 곳을 고른다. 초반1-1~1-3와 최근 완료1-5/1-14전체런은 유지하고 새 시스템 없이 기존 길의 굴곡/합류를 바꾸는 작은 후보를 기하/가독성부터 검사한다. 전체308/스케줄·경제를 보존하고 효과 없는 후보는 전투 전에 폐기한다. [완료:기록28정적프로필재사용·1-11동남공유목후보/기준3+후보3·참여악화로폐기/제품유지]

254. 253의동남긴공유길은참여악화로폐기했다.서쪽/두게이트와816스케줄·재화는고정하고동쪽만얕은S자(23,4→16,4→16,-2→10,-2→10,4→3,4→3,2→0)의별도후보를제품밖검토한다.현재37.83에가까운37.61길이로과도한우회지연을피하고기존253control3/이전선택9를재사용한다.24패드·전체308/타13정합후seed7세정책참여/압력·구매차이를보고유효할때만보완한다. [완료:기준3재사용/후보3·정리단축에도참여악화로폐기·제품유지]

255. 실제1-5화면에서굴곡도로의점선만으로이동방향을읽기어려워기존도로표식을작은방향쐐기로보완하는시각후보를검토한다.새전투시스템/경로·수치·저장변경없이공통도로표현만작게수정·진입방향/결계로향함/합류·사거리선과색구분을확인한다.유효하면회귀/Android서명16KB·새소유합성저장/3화면과대표2·4진입Ready/LIVE·정상포기재실행/자기정리를검증한다.현행0.24.0전체1-5완주를새버전완주로쓰지않는다. [완료:0.24.1/72·두/세/네진입Ready/3화면·4타워약24초LIVE/저장7동일·서명16KB/설치해시·소유디스크정리/작은로그2보류]
256. 255의완료합성프로필1-10실제SpearLv1메뉴는관측창해금으로6타일이지만QA좌표도구는5고정이었다.제품/새APK변경없이Loadout/표식·관측창해금플래그와Core가족도달성으로건설/창Lv1·Lv2/뇌격Lv1패널폭을계산한다.기존기본출력동일·실제해금6메뉴의강화좌표로Ready정확1강화/돈·Lv2/정상포기저장동일을현재5646재사용검증한다.정렬/파트너준비는여전히실화면필수이며도구가자동보장한다고하지않는다. [완료:기본24좌표동일·263해금6타일실제생성강화30/Lv2/250·같은5646/제품변경0]

257. 동일 돌바닥 반복으로 맵 외형이 비슷한 문제를 줄이기 위해 자체 기존 텍스처를 화풍 참고로 삼아 표식 훈련로(1-11) 전용 차분한 콘크리트 바닥재 한 장을 imagegen 기본 도구로 새 생성한다. 새 장애물/길/아이콘/글자/과한 발광을 넣지 않고 모바일512수입·기존아스팔트/패드·적 대비를 유지한다. 생성물을 먼저 검토하고 유효할 때만 한 작전에 적용·기존6코어/경제저장유지·회귀Android/합성업데이트·세화면/짧은LIVE/정상포기저장·소유정리를검증한다. 실패자산/후보는 검증전달과 분리한다. [완료:자체새바닥실512x256·0.24.2중간Ready3/짧은LIVE후258과최종0.24.3전달·8저장동일·설치/서명16KB·소유정리]
258. 257의밝은콘크리트실제LIVE에서경과11글자Muted무그림자가배경에묻히는문제를확인했다.새배경판/이동없이기존경과Text한줄만Paper/기존Shadow로보강한다.0.24.2는아트중간후보로보존하고0.24.3/code74로묶어전달·6코어/지형/파형경제저장유지·통과회귀반복없이UnityAndroid·서명16KB/실업데이트와세화면Running타이머/정상포기저장/현재5648재사용정리를검증한다.0.24.2준비3화면과새LIVE를구분한다. [완료:0.24.3/74·경과Paper+shadow 한줄/Running3화면·원래1-10바닥유지·8저장동일/설치서명16KB·5648정상정리]

259. 최종0.24.3의새바닥/HUD는짧은LIVE만검사했고부하표본도121프레임·적0이라2배속후반밀집가독성/전멸·재도전보상저장공백이남는다.새제품/APK없이새소유5650합성완료232/42/263으로1-11재도전한번만2배속/창·표식·관측창조합과보강을검증한다.실제카드/원장·최종타워수/816전멸·재도전보상(예상2)/별불변·무주입재실행동일·대표부하표본의한계를기록한다.0.20.2첫클리어16T완주/현재짧은검사와분리하고사람재미/폰벤치나투자단독인과로주장하지않는다.07:30이후큰변경/새AVD금지·08시마감준수. [완료:아래실제259결과와QA정정을우선]

259. 최종0.24.3 2배속 재도전·저장 검증 [완료:10T670/Observer1·187.87초816전멸300·+2/42별유지·3저장a956…동일/소유5650정리; QA빈좌표오류·150카드/후반영상/계획파쇄미완료범위명시]
260. 2250,400 전역빈좌표 가정을 폐기하고 기존 HOME포커스정지를 이용하는 리뷰 도구·정적 입력 경계 검사·인수인계를 정리한다. 제품/새APK/새AVD 변경 없이 실제 발견된 메뉴차단/숨은Pause 원인을 확인한다. [완료:96패널bounds/기존24+기본24CSV동일·실반례입력8PASS·제품/새Native/APK/AVD0]

261. 최종APK의manifest/서명인증서·ELF LOAD정렬/현재구현범위를읽기로감사한다. 실제ARM64 ELF3개의16KB정렬과ZIP정렬은구분하며debuggable/Debug서명테스트APK·14일반작전/7챕터이름/240장기기획을명시한다. [완료:읽기감사/제품·새빌드·AVD0/스토어출시·실기기주장없음]
262. 최종증거색인·미완료범위·현재실행중소유기기여부·08시마감준비를정리한다.이미완료된Native/모델/빌드반복이나새제품변경없이검증일관성을확인한다. [준비완료:07시대최종4APK/source23·14링크·소유활동0·증거색인검사PASS;08시실마감263은아직]

263.08시최종마감검증·automation-5와11모두PAUSED실응답/저장상태확인·현재APK/23제품/소유활동0재확인/미완료분리 [완료:2026-11-01T08:00:42.2448906+09:00;최종응답에서보고]

## 2026-11-03 — 사용자 승인한 다섯 개선 / v0.25.1/code76 전달
264. LIVE 타워 메뉴를 강화/조합/판매3칸으로 축소하고 조합 목록은 별도 펼침으로 만든다. 메뉴 안에서도 명시Pause가 바로 열리며 기본LIVE/기존조합선택·사거리·입력차단/저장을 유지한다. [완료:기본464ref/2열최대4조합·3화면·LIVE직접Pause·현재타워발사22표시/비활성3입력차단]
265. 1-11~1-12의 보강 효용·사격 참여를 현재 코어의 제한된 공통 구매/카드/시드 기준으로 비교한다. 초기1-1~1-3/308지점·총적수/보급·카드·촉매·저장은 보존하고, 후반 접근/지점·역할을 조정해 보강의 이득을 만든다. 실패후보는 폐기하며 전체적HP일괄상향·전 타워상시발사는 강제하지 않는다. [완료:기준/선택각36·카드36쌍/보강18전부300·위기각12전부300·28/840정적중12셀만능력치변화·1-11동남경로/두작전전방추가지점·창16발사지점12→15/8~10→14·1-11최근참여증가아님]
266. 자체 기존 아트와 코드 렌더를 사용해 1-11/1-12 중심 지형실루엣·랜드마크·도로 주변 재질을 구분한다. 전체맵 네갈래화나 작은적/HUD가 묻히는 장식 남발은 피한다. [완료:자체아틀라스랜드마크·4테마저채도바닥/훈련코트·하역표시·0250잘못된초록받침수정후0251Native]
267. 적/타워 전투효과의 밀집 가독성을 조정하고 Android를 빌드한다. 적절한 회귀/모델·서명/ZIP+ELF16KB·이전저장업데이트/독립 소유AVD·2배속 대표 영상/3카드/조합·전멸·보상재실행/정상정리를 검증한다. 사람재미·실기기/iOS/청감은 보류. [완료:0251/76/c6a80eac…97419469·제품25/설치SHA·서명16KB·최종1-11/12各16T1050·2조합·174.78초816/180.28초880전멸300·3별30/32·6저장/재실행동일·영상11.555/11.208초·1-12Range/Ballistic/Blast3카드확인/1-11중간90정체미기록·5652정상종료/디스크정리·잠긴작은로그2만사본과보존]
268. 실제 결과/한계·APK/인수인계를 갱신하고 이번 단일 요청을 마무리한다. 반복예약 재개는 요청되지 않았으므로 그대로 둔다. [완료:verify-improvement-delivery PASS·runtime-v0251/새설계·README/HANDOFF_CURRENT·기존문서이력표시·사람/실기기/iOS/청감·시작진단근본미해결유지]

이번읽기오류: Tests/MarkerCapReview.cs는존재하지않았으므로생성/덮어쓰기없이중단. 실제하네스는TestResults/marker-support-v0201-review/MarkerBattleReview.cs에서읽는다.

269. v0.25.1의 기존 개선 검증 기록을 대조하고, 기본 전투 배속을 1.25배로 높인다. HUD, 일시정지, 카드, 배속 전환 및 재시작을 Android에서 확인한다. [완료. v0.25.2/code77, 정규 회귀와 설치 해시, 1-11 초반 조작, 업데이트·포기 저장 일치, 소유 에뮬레이터 정상 종료]

270. 성장 카드가 가린 타워·조합 화면을 복원하고, 전환 직후 탭에 입력 완충을 적용한다. 1-11부터 Lv3 강화와 표식+봉인 추적 조합을 추가했다. 회귀와 최종 Android 1-11 부분 전투, 설치·저장 검증 완료. [완료]
271. 전투 피드백을 개선했다. 적 체력 표시와 처치, 헌터 이동과 공격, 고정 결계주의 조준·반동을 구현했다. 최종 0.27.1 Android의 1-11 2배속 부분 전투와 1-6 883마리 전멸·문지기 처치·재실행 저장을 확인했다. [완료]
272. 2026-11-05 10:00 KST까지 연속 개선을 재개한다. 보스 공격 예고가 실제 붉은 범위와 같은 기간에만 보이도록 수정하고 최종 Android 1-6에서 전후를 확인한다. [완료. v0.27.2/code82, 2배속 185.64초 883전멸과 합성 저장 확인]
273. 현행 Android 1280×720에서 정예·공성 HP, 밀집 일반 적, 배속과 일시정지의 가독성을 확인한다. 문제를 관찰하면 기존 HUD에서 작은 수정 후보를 비교한다. [완료. 1-11 1:20 부분 전투, 2×/1.25×, HP, 정지, 포기 저장]
274. 현행 1-11 공통 배치에서 재료 유지·Lv3 강화·추적 조합의 비용과 전투 기여를 비교한다. 선택 전 동일 구매와 카드, 실제 가격, 촉매와 소모 자리, 최저 결계·정리·표식·사격을 분리해 기록한다. 유효한 근거가 있을 때만 제품에 적용한다. [완료. 1-11 제한 36조건, 추적 단독·빈 자리 보충의 차이 확인, 제품 수치 유지]
275. 추적 조합 선택 전에 표식·감속 역할과 빈 재료 자리를 안내한다. 추적 메뉴의 Lv2→3 표식 피해·사거리 변화를 기존 줄에 드러낸다. 동일 Android 소유 기기와 합성 저장으로 작은 화면에서 조합·두 강화·정상 포기를 검증한다. [완료. v0.27.3/code83, 1280×720 1:47·조합·Lv3·빈 자리 보충과 합성 저장 확인]
276. LIVE 건설·타워·레시피 메뉴를 열면 화면 전체의 정예 HP 숫자가 사라진다. 기존 패널 밖에서만 HP 숫자를 유지하는 소규모 HUD 변경을 적용하고 화면 크기, 정지·카드, 패널과 글자 겹침을 Android에서 확인한다. [완료. v0.27.4/code84, 두 화면과 카드·정지·합성 저장]
277. 소유 5660과 v0.27.4를 재사용해 1-11 2배속 한쪽 저투자 상황의 밀집 HP·피격·처치 가독성과 활성 전투 프레임을 확인한다. 카드·정지·녹화가 섞인 구간과 표본 한계를 분리하고 발견한 문제만 다음 후보로 넘긴다. [완료. 별도 2배속 7타워 부분 관측, 2:08 포기, 4 활성 창과 영상 분리]
278. 짧은 전투 효과가 생성 프레임부터 이전 시간만큼 줄어드는 동작을 바로잡는다. 새 효과의 첫 표시를 보존하고 다음 프레임부터 기존 감쇠·이동을 적용한다. 일시정지 시 기존 감쇠, 풀 상한과 기존 색·형태를 유지하며 기존 Android에서 확인한다. [완료. v0.27.5/code85, 2배속 1-11 1:32 및 대표 영상·합성 저장]
279. 현행 APK를 유지하고 0진척 합성 저장의 첫 1-1에서 기본 1.25배, 건설·강화 안내, 적 전멸 승리, 첫 보상 및 1-2 해금·재실행을 확인한다. 사람 난이도 평가와 후반 2배속 검증을 구분한다. [완료. 첫 1-1 1.25×·3창150·177.14초330/결계300·첫보상10·1-2 해금과 저장]
280. 성장 카드 효과와 실제 사용 가능한 결계주를 일치시킨다. 첫 작전에서는 창만 설명하고, 창 계열을 만들 수 없는 편성에는 피해 카드가 나오지 않도록 한다. 기존 타워 태그/도달 조회를 재사용해 효과 문구를 정리하고, 회귀와 Android 초기·후반 부분 검증을 진행한다. [완료. v0.27.6/code86, 새 회귀, 1-1/창 없는 1-11 부분 카드·적용·저장]
281. 1-11의 편성에서 추적 재료를 사용할 수 없을 때 준비 안내가 혼선을 주는지 Android에서 확인한다. 실제 재현되면 기존 두 문장 길이 안에서 길목·Lv3·지도 편성 안내로 조정하고, 재료가 있는 편성의 기존 안내도 확인한다. [완료. v0.27.7/code87·135/263 두 안내·짧은 LIVE·저장]
282. 이전 첫 1-1 클리어 합성 저장에서 1-2 양방향 방어와 봉인 건설을 현재 v0.27.7 Android로 확인한다. 1-3 해금, 첫 보상과 재실행 저장을 검사하고 초반 경험 문제는 재현 후에만 다음 후보로 분리한다. [완료. 4타워225·187.50초460전멸·300·3별12보상/1-3·저장]
283. 1-2를 첫 완료한 같은 합성 저장으로 1-3의 서·북 방어, 60초 촉매, 뇌격 조합과 빈 자리 보충, 전멸·보상·1-4 해금 및 재실행을 검증한다. 완료 전투를 반복하지 않는다. [완료. 181.38초450·4타워315·뇌격1/3별14·세희와1-4/저장, 상대 선택 배너 가림 발견]
284. 실제 1-3에서 조합 상대 선택 배너가 북쪽 봉인과 터치 영역을 가린다. 공통 위치 계산으로 앵커와 상대의 실제 투영 영역을 피하고, 모든 14작전의 대표 화면 좌표와 동일 1-3 부분 Android에서 확인한다. [진행]
284 완료. v0.27.8/code88, 14×4 투영·1-3 2400/1280·90초 카드 복귀·취소·뇌격과 포기/재실행 합성 저장. 첫 1-3 완주는 v0.27.7의 기록이다.
285. 도시의 고정폭 작전 브리핑을 전수 점검하고 줄 끝 낱글자가 생기는 내용을 정리한다. 안쪽 전투 준비 안내와 구분하고 기존 배치·규칙을 유지한다. 회귀와 화면 증거가 있는 곳을 Android에서 검증한다.
285 완료. v0.27.9/code89 도시 14폰트 및 1-4 두 화면, 실제 빌드와 설치·업데이트 저장. 앱 전투는 이 검사 범위에 없었다.
286. v0.27.9 첫 1-4에서 세희·전체/추적 보기·붕괴·새 조합·일반 웨이브 호출을 검증하고 3갈래 전체 전멸/보상·1-5/방벽 해금 및 재실행을 확인한다. 관찰한 가독성이나 동작 결함은 다음 회차에 좁혀 검토한다.
286 완료. 최종0.27.9 첫1-4·세희·12타워730·호출1/파쇄1·179.47초768전멸300/3별16·1-5열림과 합성 저장 일치. 초기 레시피 상태 이탈 및 호출이 당긴 두 번째 카드의 QA 입력을 기록했다.
287. 명시 정지와 앱 이탈 뒤에 기존 건설/타워/레시피/조합 상대 선택을 복원한다. 판매 확인은 해제하고 조합 상대를 재계산한다. 전투·구매·카드·포기는 기존 동작을 유지하고 독립 Android의 실제 조작과 저장으로 확인한다.
287 완료. v0.27.10/code90 1-3 준비/전투 건설·창·판매해제·HOME레시피/직접·HOME상대복원/카드·뇌격·포기/업데이트·재실행 합성저장. 실제 전투 완주는 별도.
288. 준비 안내판과 건설 지점의 투영을 비교한다. 실제 첫1-4의 가림을 재현 근거로 두고, 경로·보급·시작 버튼·터치 영역을 유지하는 작은 기존 HUD 후보를 선택한다. 유효할 때만 구현하고 2화면 Android에서 확인한다.
288 완료. 70정적/90후보 투영에서 고정 이동·간단한 위치 탐색은 작은1-4 가림 해소에 부족하여 적용 보류. 제품/APK 변화 없음.
289. 현재 합성 1-4 완료의 첫1-5에서 방벽 저지와 공성 통과·두 촉매의 종결 조합·재배치·전멸 및1-6해금을 확인한다. 실제 비용·투자·카드와 오입력을 구분한다.
289 완료. 현행0.27.10 첫1-5·세희·12타워955/파쇄+종결·178.42초780전멸300·3별18/1-6·재실행 저장. 정예2/공성 통과는 별도 제한 모델로 계측.
290. 방벽의 발사수 줄을 실제 내구/최대·저지 중 수·파괴 후 재충전으로 대체한다. 방벽 강화 효과와 보수적으로 정확한 단계 표시를 기존 메뉴 두 줄에서 확인한다. 회귀, Android 두 화면 및 합성 저장.
290 완료. v0.27.11/code91 1-5 두 Android 화면, 내구와 저지·재충전 단계, Lv2와 카드 복귀·0:41정지/포기·업데이트/재실행합성저장.
291. 합성 순차 진행의 첫1-6에서 문지기 공격 예고/처치 및 전멸, 첫 보상·백도하·위기·1-7 해금과 재실행을 검증한다. 이전 재도전과 분리한다.
291 완료. 첫1-6·세희·14타워855·185.64초883전멸/보스·결계300/헌터53·3별20·1-7/백도하/위기 및 재실행 저장. 전체 영상/사람 실기기는 별도.
292. 보스 생존 중 성장 카드가 열릴 때 보스 체력바와 카드 제목을 분리한다. 일반 성장 화면 및 카드 복귀·실제 터치, 1-6 150초 두 화면, 정상 포기·저장을 확인한다.
292 완료. v0.27.12/code92 1-6 두 화면의 보스 체력/카드 분리·작은 화면 터치·LIVE 창 복귀·포기와 업데이트/재실행 저장. LIVE 창의 보스 HP 왼쪽 가림은 후속.
293. 도시 지도 페이지 변경 시 해당 페이지의 열린 작전을 선택하고 전부 잠긴 다음 페이지를 비활성화한다. 위기 모드의 보상2를 현재 지급 규칙과 같은 경로로 표시하고 두 화면 및 실제 출전/포기 저장을 확인한다.
293 완료. v0.27.13/code93 지도 페이지/위기 +2와 실제 1-7 위기 준비·포기/재실행 합성저장. 위기 전멸과 지급은 Android에서 미검증.
294. 작은 화면의 LIVE 타워 패널이 보스 HP 막대에 겹치는 경우 보스 최종 웨이브의 고정 경고 구역을 예약한다. 기존 메뉴·판매·카드·전투는 유지하며 보스 경고 및 필요 시 백도하 위기를 확인한다.
294 완료. v0.27.14/code94 LIVE 보스HP·예고 분리/백도하 위기1-6·15T810·185.49초883전멸·결계300·위기3별2·일반18별 유지·재실행 저장.
295. 도시 지도 상단과 결과에서 일반·위기 별점을 구분한다. 최신 합성 위기 기록과 간단한 Android 위기 결과를 이용해 레이블·보상·일반 저장을 검증한다.
295 완료. v0.27.15/code95 일반·위기 별점과 결과 분리/Android 두 화면·위기1-1 330전멸 3별+2/일반18별 유지·위기6·재실행 저장.
296. 첫 일반1-7에서 백도하, 짧은 남쪽 선배치·동서 보강·전방 조합과 재배치, 실제 전멸·첫 보상/1-8·재실행 저장을 검증한다.
296 완료. 첫1-7 백도하·14T910/뇌격과 보충·180.12초552전멸300·3별22/1-8·재실행 합성저장. 파쇄 시도는 성립하지 않음.
297. 방호와 질주의 실제 활성 시간이 남은 동안 스킬 버튼에 효과명과 남은 초를 표시하고 이후 재사용 대기로 복귀시킨다. 게임 규칙·입력·쿨다운 불변, 모델 타이머와 Android 둘을 검증한다.
297 완료. v0.27.16/code96 방호4·질주3과 쿨다운/사용 가능 전환 및 질주 조이스틱을 두 Android 화면에서 확인. 합성 업데이트·정상 포기·재실행과 기존 첫 안내 비트만 변경.
298. 첫1-7의 실제 380마석 9구매 뒤 유지, 남쪽 창 추가40과 전방 뇌격40을 3시드 같은 카드 정책에서 비교한다. 소모 수·누수·사격·정리·잔여와 모델 한계를 기록한다.
298 완료. 공통380 뒤 유지/창420/뇌격420 ×3시드 모두 552전멸·결계300·같은 시드 정리 동일. 창3~6발/뇌격개인2786~2832. 제품 변경 없음.
299. 일반1-7의 후반 남쪽만 무리 도착 간격을 모으는 제품 밖 후보를 비교한다. 첫 출현·총량·갈래·초기/위기 보존. 298과 같은 시드/구매·카드에서 압력·투자·정리와 방어 여유를 보고 적용 여부를 정한다.
299 완료. v0.27.17/code97 일반1-7 후반 남쪽만22→12초·18후보/기준9·스케줄28, Android 10T895/뇌격·파쇄/177.15초552전멸300·3별2·재실행 저장.
300. 첫 일반1-8에서 짧은 북쪽 방벽과 교차점 붕괴, 서·동 보강/전방 조합 후 보충을 확인하고 실제 전멸·첫 보상·1-9 해금과 재실행 저장을 검증한다.
300 완료. 첫1-8 백도하·12T930/파쇄·종결·보충,179.70초612전멸300·3별24·1-9 및 재실행 저장. 방벽 저지 효용 미확인.
301. 실제 첫1-8에서 준비 안내판의 북쪽 지점 가림을 확인했다. 1-4 이후 배치 힌트를 도시 출전 브리핑의 여백에 옮겨 전투 지도 시야를 확보한다. 1-1~1-3 안내와 1-11 편성별 문구를 보존하고 폰트 및 두 Android 화면·저장을 검증한다.
301 완료. v0.27.18/code98 1-4 이후 도시 배치 힌트/준비 가림 해소. 글꼴14/힌트와 1-11 두 편성, Android1-9 두 화면 상단 창 건설 및 포기·업데이트/재실행 저장.
302. 첫 일반1-9에서 후반 정예·공성에 서쪽 파쇄와 동쪽 중력으로 대응하고 실제 분산 배치·강화·보충/전멸·첫 보상·1-10 및 재실행 저장을 확인한다.
302 완료. 첫1-9 백도하·12T950/서파쇄·동중력과 보충·172.02초624전멸300·3별26/1-10 및 재실행 저장.
303. 첫 일반1-10의 사방 분산·선택 호출·후반 보강을 실제 Android에서 진행한다. 864 전멸·첫 보상·표식 및 1-11 해금과 재실행 저장을 구분해 검증한다.
303 완료. 첫1-10 15T1070·호출1/2.16초·파쇄+뇌격/178.27초864전멸300·3별28·표식/1-11 및 재실행 저장.
304. 첫1-11에서 표식 해금 이후 편성135→263, 배치 힌트의 실제 문구 전환, 표식·봉인 추적 조합과 재배치·Lv3·전멸/첫 보상·1-12/재실행 저장을 검증한다.
304 검증 종료. 편성135→263·1-11 힌트/추적Lv2·연계205 확인, 동쪽 누수로137.47초 패배/보상0·1-12 잠김. 파쇄/Lv3/완주는 후속.
305. 첫1-11의 패배 원장에서 94/95.9초 서쪽 강화 두 건과 131.6/133.6초 동쪽 건설 두 건의 순서를 바꾸는 3시드 비교. 같은 최종 예산과 배치 목표, 같은 편성·카드 우선순위, 실제 누수/지출·실행 및 한계를 기록하고 효과가 있으면 실제 후속을 한다.
305 완료. 3시드×2 동일18구매/795·11기에서 서쪽 먼저 3패, 동쪽 먼저 3승·816전멸300/누수0. 제품 변경 없음.
306. 실제 패배와 제한 모델 근거로 1-11 도시 배치 힌트 두 편성에 4웨이브 동남쪽 보강 시점을 반영한다. Android 새 빌드에서 실제 선보강·추적Lv3/추가 조합·전멸·1-12와 저장을 검증한다.
306 완료. v0.27.19/code99 4웨이브 동남쪽 힌트/실제 동쪽 선보강, 추적Lv3·파쇄/13T1055·181.80초816전멸300·3별30/1-12·재실행 저장.
307. 1-11 패배에서 일반 문구로만 남았던 진입 피해의 방향을 실제 적용 피해 기준으로 안내한다. 보호막·과잉 피해·저장 형식을 확인하고 독립 Android 두 화면의 실제 패배·재실행을 검증한다.
307 완료. v0.27.20/code100 패배 방향 안내/실제 피해 원장·캠페인53. 별도 Android 동쪽 QA 패배46.50초/피해299.9997·두 화면·기존 해금/재실행 저장.
308. 첫1-12에서 짧은 북쪽과 긴 서쪽의 표식·붕괴/파쇄·추가 보강을 확인한다. 전멸·첫 보상·1-13/재실행 저장과 실제 진입 피해를 구분한다.
308 완료. Android14기980·북쪽 파쇄/서쪽 뇌격·184.62초880전멸300·3별32·1-13과 합성 재실행 저장.
309. 첫1-13에서 서·북 일부 합류와 동쪽 별도 진입에 배치하고 동쪽 뇌격 및 후속 반대편 보강을 확인한다. 전멸·첫 보상·1-14 및 합성 저장을 검증한다.
309 완료. Android15기1100·동쪽 뇌격/서북 파쇄 Lv2·188.12초928전멸300·3별34·1-14와 합성 재실행 저장.
310. 첫1-14에서 짧은 북쪽과 긴 서·동 진입을 분산 보강해 전멸·첫 보상·관측창 해금 및 합성 저장을 검증한다.
310 완료. Android16기1115·북쪽 파쇄/서쪽 뇌격 Lv2·175.29초944전멸300·3별36·관측창 해금과 합성 재실행 저장.
311. 레시피 준비 상태가 바뀔 때 기존 카드가 재배열되는 실제 문제를 검증한다. 기존 카드·입력 조건을 유지하며 타일 위치를 고정한 뒤 Android 두 화면, 촉매 전후·관측창 생성/강화와 포기 저장을 확인한다.
311 완료. v0.27.21/code101 카드 위치 고정. 실제 촉매 전후·두 화면 관측창 Lv3/최대 관측33발과 정상 포기·재실행 저장, 회귀·서명·정렬·설치.
312. 첫 1-14 완료 후에도 도시가 해금 예정으로 표시한 안내를 바로잡는다. 해금 전과 후, 현재 편성의 표식/창 유무를 검토하고 실제 Android 두 화면과 저장을 확인한다.
312 완료. v0.27.22/code102 해금·편성에 맞춘 브리핑. Android 두 화면과 표식 비트 복원/합성 재실행, 회귀·글꼴16·서명·설치.
313. 굽은 길의 선두 적을 직선거리로 고르는 사례를 고립 재현한다. 남은 경로거리 후보와 충분/제한 투자, 1-11 선보강 검증을 같은 규칙·시드로 비교한다. 초기 난이도·후반 투자 선택·사격 참여·성능을 함께 보고 적용 여부를 분리한다.
313 완료. 투사체 고립 재현과 후보 126/48쌍씩, 1-11 고정 순서 6쌍. 경로 우선의 일부 저투자 생존 개선과 사격 참여 감소를 비교해 두 전역 후보 보류. 제품 유지.
314. 2배속 긴 프레임에서 발사 이벤트와 같은 프레임에 헌터/결계주 반동이 모두 감쇠되는 경우를 확인한다. 첫 표시 프레임을 살리고 후속 감쇠·전투 수치를 유지한다. Unity 실계산과 Android 대표 전투, 업데이트/저장을 검증한다.
314 완료. v0.27.23/code103 새 반동·건설 후 첫 발, Unity 계산과 Android 헌터 이동·14타워1110/관측창 Lv3·뇌격/176.98초944전멸300·재도전+2/합성재실행 저장.
315. 실제 1-14 재도전에서 이미 연 관측창의 첫 해금 문구가 반복된다. 첫 승리/재도전/위기를 분리하는 결과 안내를 만들고, 새 보상 및 이전 해금·저장을 검증한다.
315 완료. v0.27.24/code104 세희 14타워1090·176.10초944전멸300·3별+2/두 화면 수정 결과·재실행, 캠페인54 및 설치.
316 완료. 관측창+붕괴 종결 경로의 동일855/1075 3시드·기준12/후보24. 굽이 종결1의 생존과 입구/사격 참여의 차이, 기존 정책 동등 확인.
317. 관측창 Lv2와 붕괴 Lv2로 기존 종결을 만드는 경로를 관측창 해금 후에 추가한다. 첫 완료 전 노출·2촉매·투자455·회귀·Android 편성/조합/후속 전투·합성 저장을 검증한다.
317 완료. v0.27.25/code105 봉인 없는261·두 촉매·관측창→종결/14기1060·175.86초944전멸300·3별+2/합성재실행, 편성복원과 회귀55/서명/설치.
318. 파쇄와 종결의 실제 직격·주변 폭발 피해를 강화 줄에서 구분한다. 카드·강화로 바뀐 수치와 글꼴 폭, 실제 Android 타워 메뉴·LIVE/합성 저장을 확인한다.
318 완료. v0.27.26/code106 파쇄 두 강화 표시/카드 복귀와 종결120·60 두 화면·부분 포기 저장, Unity8 글꼴·회귀55/설치.
319. HUD 그리기 명령을 Refresh마다 객체로 할당하는 범위를 실제 Unity 메서드로 고립 계측한다. 재사용 가능한 값 타입 후보, Android 정적 화면/실제 입력·합성저장을 검증하고 프레임률로 일반화하지 않는다.
319 완료. v0.27.27/code107 같은 65,536명령 Unity 할당 샘플 65,536→0, 도시 PNG 동일·1-1 소형 건설/LIVE/포기/합성 재실행·회귀/설치.
320. 조합 선택에 결과 앵커와 함께 상대 재료 자리가 빈다는 사실을 모든 레시피에 표시한다. 기존 범위·취소 안내, 화면 터치와 합성 저장을 검증한다.
320 완료. v0.27.28/code108 두 화면 상대 자리·1-3 실제 뇌격/빈 자리 봉인 재건설/부분 포기 저장·회귀/설치.
321. 편성의 현재 개수를 표시하고, 4개에서 추가와 1개에서 해제를 탭 전에 막고 이유를 알려준다. 기존 저장 제한·조합 미리보기·합성 업데이트와 편성 원상복구를 검증한다.
321 완료. v0.27.29/code109 두 화면 4/4·1/4 탭 차단 및 방벽 추가와 263 복원, 합성 8개 바이트 대조·회귀/설치.
322. 2배속 다중 틱에서 피격 직후 표시가 렌더 전에 사라질 수 있는 조건을 고립 확인한다. 적의 첫 피격 프레임과 헌터·결계의 색 변화를 살리고, 전투 수치를 보존한 채 Unity와 Android에서 검사한다.
322 완료. v0.27.30/code110 Unity 첫 피격·감쇠와 Android1-6 193.64초883전멸/문지기·북피해88/결계212·2별+2/합성 저장·설치.
323. 터치가 없는 모바일 Update에서 입력 전 HUD 갱신을 건너뛴다. 입력이 있는 프레임과 렌더 전 갱신은 유지하고 실제 시작·조이스틱·카메라·카드 입력과 합성 저장을 검증한다.
323 완료. v0.27.31/code111 Android 1-4 두 화면·추적 이동/건설·호출과 카드/부분 포기 합성 저장, 회귀·설치.
324. 1-4 호출 직후 같은 화면의 빠른 다음 탭이 카드를 선택하는 기존 동작을 재현했다. 자동 보급 및 호출을 한 전환으로 처리하고, 표시 직후 .35초 보호 뒤 정상 선택을 검증한다.
324 완료. v0.27.32/code112 1-4 호출 직후 동일 입력의 카드 유지와 별도 선택·1-1 자동 카드, 두 포기/합성 재실행·회귀/설치.
325. 왼쪽을 보던 적의 사망 잔상이 기본 방향으로 바뀌는 지점을 확인한다. 처치 대상 ID로 마지막 표시 위치와 방향을 이어받고, 동일 보상·종류 및 풀 재사용·Android 처치 표현을 검증한다.
325 완료. v0.27.33/code113 Unity 실제 위치/방향·폴백·풀, 24결투 ID. Android 1-2 443처치·동쪽 피해 패배/대표 영상/저장 보상0만 반영·회귀/설치.
326. 1-2의 4기 패배와 이전 승리를 카드/헌터를 분리해 48조건 비교했다. 4·6웨이브 동쪽 추가 보강 안내를 만들고 백도하 5기 실전과 합성 저장을 검증한다.
326 완료. v0.27.34/code114 두 도시 화면·실제 백도하5기265/176.96초460전멸·동0/결계178·1별+2/재실행·설치.
327. 실제 1-2 보강 판의 카드·시점과 백도하를 고정한다. 동쪽3 창을 유지하고 서쪽 보강 1기의 위치별 효과를 분리해 모델로 검증한다.
327 완료. 15조건 모두 전멸, 5기265 결계195·서105, 서쪽 4후보의 6기305 모두 결계300. 실제 5기 결계178과의 차이 보존. 제품 유지.
328. 같은 검증 APK의 실제 1-2에 동쪽3와 서쪽6을 단계적으로 보강해 두 전선의 결계와 재도전 저장을 확인한다. 제품 변경과 새 AVD 없이 짧은 최종 검증을 수행한다.
328 완료. v0.27.34 백도하6기305/176.11초460전멸·결계300·3별+2, 합성340마핵·기존 별/재실행. 실제 카드·시각 차이 명시.
329. 09:30 이후 전달본·서명·검증·합성 저장을 대조하고 소유 테스트 기기만 정리한다. 10:00 KST 마감 및 automation-5 PAUSED를 확인한다.
329 완료. v0.27.34/code114 별칭/설치 SHA와 v2·ZIP/ELF16KB 재검사, 합성 저장과 소유5660 정상 정리 확인. 10:00 KST 마감 후 automation-5 PAUSED 저장 확인.
330. 기본 결계주 다섯 종의 작은 화면 실루엣과 색을 개선한다. 공용 전장/편성/건설 스프라이트, 대표 화면·회귀·Android 전달을 확인한다.
330 완료. v0.27.35/code115 Unity 5×2 리소스/크기, Android 1280/2400 편성·건설/1-14 4기·1-5 방벽 Lv2/정상 포기·합성 재실행, v2/16KB/설치 해시.
331. 방벽 파괴 충격파와 봉인·표식의 사슬 및 연계 표현을 구현한다. 세 타워 신규 실루엣, 기존 편성/조합/저장과 회귀 및 Android 검증을 확인한다.
331 완료. v0.27.36/code116 충격문·구속닻·추격등. Android 1-14 사슬7/연계1, 1-5 충격파1/별도 1280 Lv2, 합성 진행/업데이트·재실행·회귀·설치.
332. 계정 성장 경제와 스테이지별 기준 강화, 일반/상위 조합의 비용·공간·완성 시점·기여를 현행에서 계측하고 챕터별 설계와 우선 적용 범위를 문서화한다.
332 완료. v0.27.36의 14작전 207조건/계정 성장·동액 종결·맵 검토. 다음 후보 1-11 성장 격차와 1-14 북쪽 종결 가치 및 Android.
333. GitHub Khjnnn/gatefall에 현행 Unity 소스·자체 에셋·설계와 필요한 검증 자료를 초기 업로드하고 원격 커밋을 대조한다.
