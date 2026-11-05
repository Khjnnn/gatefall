# V12 자체 생성 전투 배경

2026-10-30. 내장 imagegen 도구를 사용해 Gatefall용으로 새로 생성했다. 타 게임 이미지·코드·에셋을 입력/복제하지 않았다. PNG 원본은 그대로 복사했고 픽셀 편집은 하지 않았다. 도시 소품은 런타임 Sprite.Create의 사각형으로4개 셀을 참조한다. 검은색 미리보기 배경과 달리 실제 PNG 알파 채널은 투명하다.

|프로젝트 파일|생성 원본|용도|
|---|---|---|
|Assets/Resources/Art/V12/district-ground.png|C:/Users/Pro/.codex/generated_images/01a16b41-6b49-79d1-8d27-9610f5bde295/exec-be494bc9-f7e0-4ccd-b08f-137e210acf7f.png|1774×887 바닥|
|Assets/Resources/Art/V12/district-blocks.png|C:/Users/Pro/.codex/generated_images/01a16b41-6b49-79d1-8d27-9610f5bde295/exec-1a6b840e-b47c-488d-8932-d40f106cff99.png|1536×1024 RGBA, 2×2 상점/창고/정원/지하철 입구|
|Assets/Resources/Art/V12/asphalt.png|C:/Users/Pro/.codex/generated_images/01a16b41-6b49-79d1-8d27-9610f5bde295/exec-2c30f173-2b16-4fbb-9f08-9c3f478ad3fe.png|1254×1254 노면|

## 생성 프롬프트

### Ground (transparent_background=false)
Create a finished original 2D mobile tower-defense ground texture for Gatefall, a modern Korean fantasy city. Landscape exactly 2:1 composition. Strict near-vertical top-down view, hand-painted polished game illustration with restrained detail, muted warm grey stone, dusty olive seams, worn small rectangular paving stones, subtle cracks and a few sparse tiny weeds. This is a continuous reusable GROUND PLATE under independently rendered roads and buildings: every part of the canvas is flat paving, no buildings, no trees, no road or asphalt, no circles, no central emblem, no props, no text, no characters, no tower, no borders, no UI. Gentle even daylight, no perspective horizon, no strong cast shadows, no vignette, no spotlight. Small paving scale and medium-low contrast so small combat sprites and dark asphalt read instantly. Production-ready detailed subtle surface, no concept-sheet labels. Original art only.

### Blocks (transparent_background=true)
Create an original transparent sprite atlas for a premium 2D mobile tower-defense game in a quiet modern Korean old-town fantasy setting. Exactly FOUR isolated urban scenery clusters arranged in a precise TWO BY TWO grid with generous equal transparent gutters and no overlap between cells. Each object stays fully inside its own quadrant with transparent background and soft short ground shadow, all viewed from nearly vertical top-down with a slight front facade visible, hand-painted crisp game illustration, muted understated colors. Top-left: small block of two low-rise old-town shops with teal corrugated roofs, rooftop AC units and pale plaster walls. Top-right: compact brick warehouse block, slate-blue roof, skylights and loading edge. Bottom-left: tiny urban pocket garden with two broadleaf trees, stone planter border and one small bench. Bottom-right: old concrete metro entrance and low brick utility structure, dusty terracotta roof and shallow stairs. No roads, no characters, no monsters, no towers, no text, no numbers, no logos, no UI, no labels, no checkerboard. Clear economical silhouettes, restrained detail that remains readable when small. Exactly four substantial coherent clusters, not many scattered objects. Maintain equal visual scale and consistent soft daylight. Real alpha transparency, original art only.

### Asphalt (transparent_background=false)
An original production-ready seamless game texture, square top-down dark desaturated blue-grey asphalt, for a polished hand-painted 2D modern city tower defense. Fine subtle stone aggregate, a few very faint worn hairline cracks, uniform soft daylight, entirely flat, low contrast. No road markings, no lane lines, no curbs, no objects, no text, no borders, no dramatic grunge, no center focus. A quietly detailed material surface that can repeat under small readable units.
