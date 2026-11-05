# 표식·관측창 이미지 생성 기록

2026-10-28, built-in imagegen 사용. 스타일 참고는 Gatefall 자체 `Assets/Resources/Art/V6/tower-spear-1.png`이며 다른 게임의 소스나 이미지는 사용하지 않았다.

- 표식: 기존 탑의 카메라 각도와 회화적 질감을 참고해, 어두운 청동·금색 받침과 청록 조준 표식이 있는 독립형 타워 한 개를 투명 배경으로 생성.
- 관측창: 같은 시점의 길고 날카로운 금빛 창 타워 위에 청록 조준점이 있는 독립형 타워 한 개를 투명 배경으로 생성.

생성 원본은 `Docs/Art/marker-source/marker.png`, `observer.png`, 프로젝트 리소스는 `Assets/Resources/Art/V6/tower-marker-1.png`, `tower-marker-2.png`, `tower-observer-1.png`, `tower-observer-2.png`. `Tools/art/prepare_marker.py`는 원본의 투명 영역에 맞춰 크기와 캔버스를 조정하고 Unity 메타데이터를 만든다. 각 타워의 Lv1과 Lv2는 같은 이미지를 사용하며 레벨별 차별화는 후속 작업이다.

원본 프롬프트:

- Marker: “Create a new original transparent game sprite asset for an isometric Korean urban fantasy tower defense, matching the provided Gatefall tower sprite in painterly detailed isometric rendering, dark bronze and warm gold materials, strong readable silhouette and same viewpoint. This is a new MARKING tower: a compact bronze pedestal with a prominent glowing cyan target reticle suspended above it, small amber crystal details, no text, no ground shadow outside the base, centered single tower. Reference existing tower only for camera and art style, not its silhouette. Transparent background, isolated object.”
- Observer: “Create an original transparent isometric Korean urban fantasy tower defense game sprite, matching reference's painted detailed rendering and viewpoint. The hybrid OBSERVER SPEAR tower should have a gold and dark bronze spear-shaped tower with a glowing cyan sighting reticle near the top, visually suggesting precision long-range attacks. It must have a distinct narrow silhouette from the marking tower, compact stone base, little red cloth, no text or people, centered isolated single tower, transparent background.”
