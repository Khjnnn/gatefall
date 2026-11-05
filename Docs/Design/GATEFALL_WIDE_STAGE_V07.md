# Gatefall v0.7 — 확장 전장 실험

1-4 방위대 임시초소에만 적용하는 모바일 프로토타입. 목표는 떨어진 진입로를 나누어 방어하고, 결계주를 새로 세울지 강화·조합할지 선택하게 하는 것이다. 다른 작전은 기존 크기와 진행을 유지한다.

| 항목 | 기존 1-4 | 시험안 |
|---|---|---|
| 이동 가능 영역 | 36×20 | 48×24 (면적 1.6배) |
| 건설 지점 | 8 | 12 |
| 최초 마석 | 140 | 200 |
| 진행 | 3분, 서·북·동, 6개 웨이브 | 동일, 진입 위치와 길을 확장 |
| 카메라 | 전장 전체 고정 | 각성자 추적, 전체 보기 전환 |
| 조합 | Lv2 재료 2개 소비 | 동일. 짝을 선택해야 할 때 자동으로 전체 보기 |

그림 좌표에 맞춰 서쪽과 동쪽의 도로를 바깥으로 늘렸고, 북쪽 진입로는 기존 도로의 연장선에서 시작한다. 세 길을 막는 것처럼 보였던 바리케이드는 1-4 배경에서 제거했다. 12곳 모두 탭해서 건설·강화할 수 있으며, 추가 자리를 억지로 채울 필요는 없다. 화면 밖에 살아 있는 적이 있으면 방향과 수를 표시하고 진입 전에는 방향 알림을 띄운다.

## 테스트 계획 및 기준

- 같은 크기의 캐릭터를 보면서 이동할 수 있고, 좁은 화면에서도 전체 보기에서 먼 지점을 선택할 수 있어야 한다.
- 9~12번 지점도 설치·강화·소비형 조합을 지원해야 하며, 게임의 다른 타워 계열·저장 데이터에 영향을 주지 않아야 한다.
- 1-4 시작 보급 +60과 3개 길의 적 수·간격은 시험값이다. 자동 시뮬레이션에서 방치 시 패배하고 분산/조합 전략의 성공 가능성을 확인하되, 재미나 난이도를 사람의 플레이 없이 확정하지 않는다.
- 실기기에서 조작, 화면 밖 안내, 작은 터치 대상과 성능을 확인한다. 특히 4:3의 전체 보기에서는 전체 지도를 보여 주기 때문에 게임 화면 위아래에 여백이 생긴다.

## 아트 출처

`Docs/Art/v07-source/stage-3-open.png`는 v0.6 자체 제작 1-4 이미지를 내장 image_gen으로 편집한 원본이다. 프롬프트: “Remove only the two orange-white roadblock barricades and their attached yellow bollards ... reconstruct continuous dark asphalt and appropriate yellow dashed road markings ... preserve ... position, shape, perspective and layout of all roads, central circle, buildings, trees and the dark tower ... left, upper and right roads ... open and continuous.” 결과를 `Assets/Resources/Art/V6/stage-3.png`로 크기만 맞춰 사용한다. `Tools/art/prepare_v06.py`가 재생성 시 이 원본을 유지한다.
