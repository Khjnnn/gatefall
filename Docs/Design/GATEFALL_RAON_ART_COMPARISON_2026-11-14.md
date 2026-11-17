# 라온역 전투 아트 비교 · 2026-11-14

같은 3-6 준비 화면의 0.32.3 자체 제작본과 ImageGen 시안을 비교했다. 시안은 `TestResults/rail-art-comparison-20261114/raon-ai-concept.png`에 있다.

| 요소 | 0.32.3 화면 | 0.32.4 적용 |
| --- | --- | --- |
| 콘크리트 | 일정한 바닥색과 얇은 줄눈 | 반복 타일에 넓은 마모 무늬와 따뜻한 회색을 넣고 입자 대비를 낮춤 |
| 선로 | 직선 끝마다 레일이 끊기고 침목이 촘촘함 | 굽이의 레일을 잇고 침목 간격을 넓힘 |
| 터널 | 밝고 평평한 아치 | 깊은 터널 내부와 마모된 콘크리트 테두리 |
| 철도 소품 | 단순한 색면 | 화물차 바퀴와 보강대, 차양 골조, 개찰구와 신호기의 재질이 축소 화면에서도 드러나도록 보완 |

AI 시안은 공통 색과 재질을 고르는 데 썼다. 시안의 그림에는 교차부 연결과 시설 면적이 달라진 곳이 있다. 게임은 원래 도로 데이터로 선로를 그리고 건설 지점, 입구, HUD는 기존 게임 좌표로 표시한다. 바닥과 소품은 `python Tools/art/rail.py`로 재생성할 수 있다. 기존 0.32.3의 아트 파일과 APK도 보존했다.

## 0.32.4 검증

| 작전 | 에뮬레이터 해상도 | 확인한 내용 |
| --- | --- | --- |
| 3-6 | 1280×720 | 세 입구와 연속 레일, 붕괴 결계주 건설 및 첫 웨이브 |
| 3-7 | 1280×576 | 화면 아래 남쪽 입구, 굽은 선로, 붕괴 결계주 건설 및 첫 웨이브 |
| 3-8 | 2400×1080 | 북쪽 입구에서 시작하는 긴 루프, 준비 및 첫 웨이브 |

세 작전에서 포기 후 진행 중인 전투가 저장에 남지 않았으며, 포기 직후와 앱 재실행의 저장 바이트가 일치했다. 0.32.3에서 0.32.4로 업데이트한 1,213바이트 합성 저장도 그대로였다. APK는 `Builds/Android/Gatefall-v0.32.4.apk`(코드 151, 109,019,803바이트)이며 설치본 추출 해시가 원본과 일치했다. Unity 빌드 전 회귀, v2 서명, ZIP 및 ARM64 ELF 16KB 정렬도 통과했다. 캡처와 해시는 `TestResults/rail-art-comparison-20261114/`에 있다.

이번 캡처는 준비 및 전투 시작 약 8초까지 다룬다. 전체 전투와 실기기는 이 버전에서 검증하지 않았다. 실제 소품은 AI 시안보다 단순한 형태를 유지한다. 홈 일러스트와 전투 아트의 통일은 남은 과제다.

## 시안 생성 프롬프트

내장 ImageGen에 0.32.3의 1280×720 3-6 준비 화면을 제공했다.

> Use case: stylized-concept. Edit this Gatefall gameplay screenshot for an original Raon railway station battle art direction comparison. Preserve the exact camera view, 16:9 framing, junction and route curves, three rift positions, tower sockets, center crystal, hunter, start button and other HUD geometry and text. Change only the environmental art: cool worn concrete platform slabs, low-contrast sleeper ties and clean twin steel rails, thin dull ochre platform safety lines, painted teal and graphite freight car, signal pole, canopy and equipment at the outside margins, dark recessed tunnels behind the three existing rifts. Hand-painted 2.5D Korean urban fantasy, strong clean silhouettes, soft north-west light, cold diffuse overcast, warm muted weathered brass. Dark graphite rails with only occasional soft edge highlight; stony gray slightly warm concrete with broad tonal wear rather than speckles. Respect screen legibility: reduce visual energy at board center, shadows stay under scenery, surfaces never engulf any socket, no large vignette, no new routes, no extra gameplay elements, no logos. Keep the Korean labels undisturbed where possible. This is a gameplay art mockup with exact lane shapes and player interactions kept.
