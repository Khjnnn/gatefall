# v0.10.7 열린 도로 배경

기존 자체 V6 배경 `stage-1.png`과 `stage-2.png`를 입력으로 Codex 내장 imagegen 편집을 사용했다. 원본은 보존하고 `Assets/Resources/Art/V6/stage-1-open.png`, `stage-2-open.png`을 추가했다. 모델 출력은 1774×887이며 표시 시 원래 배경과 동일한 48×24 월드 크기로 맞춘다. 1-8 북쪽 열린 길에는 첫 번째, 1-6·1-9 동쪽 열린 길에는 두 번째를 사용한다. 1-7·1-10·1-13은 기존 바리케이드 없는 `stage-3.png`를 재사용한다.

편집 프롬프트: “Remove only the white and yellow striped barricade across the upper road. Restore continuous asphalt and dashed center line under it. Preserve all other geometry, road bends, plaza, camera, buildings, trees, colors and framing exactly; no new objects. This is a game background and exact alignment is essential.” 두 번째 이미지는 같은 지시에서 제거 대상을 “the right horizontal road”로 바꿨다.

수치 좌표를 실제 배경 위에 겹친 검토 그림 `TestResults/board-layout-v0107.png`와 Android 에뮬레이터의 1-8·1-6 준비 화면을 확인했다. 이미지 생성 특성상 세부 질감과 소품은 원본과 완전히 동일하지 않으며, 모든 스테이지의 실제 기기 화면 검증은 남아 있다.
