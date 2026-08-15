# BobVirus 타일 작업 핵심 지침

## 적용 범위

이 지침은 BobVirus의 타일 스프라이트, Tile/RuleTile 에셋, TileList, Tilemap, 타일 기반 씬 생성기를 수정할 때 적용한다.

타일 작업 전에는 docs/tiles/tile-system-reference.md에서 해당 작업의 상세 구조와 장애 대응 항목을 읽는다. 실제 씬, 에셋, 코드가 최종 기준이며 문서와 다르면 임의로 맞추지 말고 차이를 먼저 보고한다.

Hospital 구현은 별도 로컬 브랜치에만 존재할 수 있다. 현재 브랜치에서 관련 파일이 없으면 추측으로 새로 만들지 말고 올바른 브랜치나 원본 파일이 필요한지 알린다.

## 변경 불가 원칙

- 맵 지형을 개별 SpriteRenderer GameObject로 늘어놓지 않는다. Grid 아래의 Tilemap에 TileBase로 배치한다.
- 기본 Tilemap은 바닥용 Floor와 벽·문·정적 가구용 Wall이다.
- 서로 이웃을 판정하는 벽, 문, 가구는 특별한 교차 Tilemap 로직이 없는 한 같은 Wall Tilemap에 둔다.
- 정적 맵 요소의 m_DefaultGameObject는 null로 유지한다.
- 사용자의 검수 전에는 Tile Palette를 생성·삭제·재배치하거나 등록 내용을 변경하지 않는다.
- 기존 경로와 에셋 이름의 Funiture, Assets_TIles, outterWall 철자를 임의로 고치지 않는다.
- 기존 GUID를 복제하지 않는다. 기존 토폴로지를 재사용할 때는 slice, pivot, PPU만 보존하고 새 에셋은 고유 GUID를 사용한다.

## 기본 규격

- Grid Cell Size는 1 × 1, Tilemap Anchor는 (0.5, 0.5, 0)이다.
- 기본 논리 셀은 16 × 16 픽셀이며 Pixels Per Unit은 16이다.
- Filter Mode는 Point, Mip Maps는 Off, Compression은 Uncompressed, Wrap Mode는 Clamp를 사용한다.
- 한 셀을 채우는 바닥은 정확히 16×16이며 불필요한 투명 여백이 없어야 한다.
- 벽과 큰 가구는 기준 스프라이트의 slice 크기와 pivot을 따른다.
- 스프라이트 시트는 실제 시작 좌표, stride, 셀 크기를 측정한 뒤 자른다. 눈대중으로 index * 16을 적용하지 않는다.

## RuleTile 및 관계 규칙

- 연결형 타일은 프로젝트의 AdvancedRuleTile과 기존 TileList 관계를 먼저 조사한다.
- 새 규칙을 설계하기 전에 기준 에셋의 규칙 수, 순서, Sibling/Null/Target 목록과 스프라이트 역할을 확인한다.
- 복잡한 벽과 문은 일반적인 4방향 RuleTile로 축약하지 않는다. 검증된 에셋의 토폴로지를 복제하고 스프라이트만 역할에 맞게 치환한다.
- 구체적인 문 접점·대각선·Target 규칙을 일반 연결·기본 규칙보다 먼저 둔다.
- 각 Rule ID는 고유하게 유지한다.
- 새 타일은 역할에 맞게 InnerWall.asset, outterWall.asset, Door.asset, Funiture.asset에 중복 없이 등록한다.

## 배치 규칙

- 가로 벽은 연속된 2개 행, 세로 벽은 연속된 2개 열을 채운다.
- 문 하나는 동일 Door RuleTile 두 셀로 구성한다. 가로 벽의 문은 위·아래, 세로 벽의 문은 좌·우로 배치한다.
- 문 두 셀이 들어갈 위치에는 벽 타일을 남기지 않는다.
- 큰 가구는 앵커 셀만 보지 말고 전체 sprite.bounds와 벽·문·다른 가구의 점유 영역을 검사한다.
- 큰 단일 스프라이트를 RuleTile 회전만으로 돌리지 않는다.
- 기본 실내 바닥은 하나의 seamless 타일을 반복한다. 경계가 다른 장식은 기본 Random 변형에 섞지 않는다.

## 작업 순서

1. 실제 사용 중인 Tile asset, GUID, RuleTile 규칙 순서와 TileList 참조를 찾는다.
2. 원본 스프라이트와 .meta의 slice, pivot, PPU를 확인한다.
3. 씬 좌표에서 구조물의 실제 셀 점유를 확인한다.
4. 필요한 연결 상태와 방향별 스프라이트를 먼저 목록화한다.
5. 기존 구조를 유지하는 최소 변경으로 에셋과 배치를 수정한다.
6. TileList 등록, 리임포트, 타일 Refresh, 씬 저장을 완료한다.
7. 검증 항목을 확인하고 결과를 보고한다.

## 필수 검증

- PPU 16, Point 필터, Mipmap Off를 확인한다.
- 벽의 가로·세로·내외부 모서리, T자, 십자와 문·가구 접점을 확인한다.
- 벽 2셀 두께와 문 2셀 조립을 대표 좌표에서 확인한다.
- 바닥이 셀 전체를 채우고 반복 경계가 끊기지 않는지 확인한다.
- 수정한 맵 요소 아래에 불필요한 SpriteRenderer가 없는지 확인한다. 씬 전체 개수를 0으로 가정하지 않는다.
- 가구 Bounds가 벽과 문을 침범하지 않는지 프로젝트 또는 생성기의 허용 오차 기준으로 확인한다.
- C# Editor 코드를 변경했다면 컴파일을 확인한다.
- 씬 생성기를 변경했다면 해당 브랜치의 버전 정책과 저장 성공 여부를 확인한다.
- 사용자의 요청 없이 Tile Palette를 변경하지 않았는지 확인한다.

## 예외

애니메이션, 런타임 상태, 독립 Collider, 상호작용 컴포넌트가 필요한 문이나 가구는 정적 TileBase 원칙의 예외가 될 수 있다. 기존 구조에서 확인되지 않은 예외를 임의로 도입하지 말고 사용자와 구현 방식을 합의한다.

상세 에셋 경로, AdvancedRuleTile 판정표, 코드 예제, Hospital 생성기, 전체 문제 해결 절차와 체크리스트는 docs/tiles/tile-system-reference.md를 참조한다.