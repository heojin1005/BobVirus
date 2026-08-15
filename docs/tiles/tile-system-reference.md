# BobVirus 타일 시스템 참고서

이 문서는 타일 디자인 불변식, 검증된 이미지 양식, RuleTile 구조와 작업 절차를 정리한다. 디자인 불변식은 단일 씬 사례보다 우선한다. 구현 수치와 파일 현황은 2026-08-16의 현재 브랜치를 기준으로 확인했다.

## 1. 디자인 불변식

### 1.1 Tilemap 역할

- 맵 구성 요소는 Grid 아래 Tilemap에 TileBase로 배치한다.
- Floor는 바닥을 담당한다.
- Wall은 벽, 문, 정적 가구, 동적 가구를 담당한다.
- 서로 이웃을 판정하는 벽·문·가구는 별도 교차 Tilemap 로직이 없다면 같은 Wall Tilemap에 둔다.
- 개별 SpriteRenderer GameObject를 늘어놓아 맵을 구성하지 않는다.
- 정적 타일은 m_DefaultGameObject와 모든 규칙의 m_GameObject를 null로 유지한다.
- 동적 요소에 GameObject가 필요하면 명시적 예외로 취급하고 이웃 판정과 Collider를 함께 검증한다.

### 1.2 벽과 문 점유

- 벽은 항상 논리적으로 2셀 두께다.
- 가로 벽은 연속된 2개 행을 채운다.
- 세로 벽은 연속된 2개 열을 채운다.
- 모서리는 두 방향의 2셀 두께가 끊기지 않게 교차한다.
- 문 하나는 동일 Door RuleTile의 인접한 2셀이다.
- 가로 벽의 문은 위·아래의 세로 2셀이다.
- 세로 벽의 문은 좌·우의 가로 2셀이다.
- 문이 차지하는 두 셀에는 벽 타일을 남기지 않는다.

벽·문 스프라이트는 16×32로 시각적으로 두 셀 높이지만, 이것이 논리적 2셀 두께를 대신하지 않는다. 배치 셀과 스프라이트 Bounds를 각각 검증한다.

### 1.3 가구 방향

- 방향이 있는 가구는 항상 상·좌·우·하 네 방향을 제작한다.
- 한 방향 스프라이트를 RuleTile 회전으로 돌려 다른 방향을 대체하지 않는다.
- 방향성 조립형 가구는 네 방향마다 시작·중간·끝 또는 두 조각 등 필요한 전체 세트를 만든다.
- 방향은 InnerWall Target 조건으로 선택하고, 조립형 가구는 같은 가구의 This 조건을 함께 사용한다.
- 큰 스프라이트는 앵커 셀뿐 아니라 전체 sprite.bounds가 벽, 문, 통로, 다른 가구와 겹치는지 확인한다.
- table처럼 3×3 연결 상태를 사용하는 가구는 방향 규칙과 별도로 9개 연결 토폴로지를 보존한다.

### 1.4 에셋과 Palette

- TileList 등록과 Tile Palette 등록은 서로 다른 작업이다.
- 새 타일은 필요한 TileList에 중복 없이 등록한다.
- 사용자 검수 전에는 Tile Palette를 변경하지 않는다.
- Funiture, Assets_TIles, outterWall은 현재 참조에 사용되는 이름이므로 임의로 바꾸지 않는다.
- 새 에셋은 고유 GUID를 사용한다.

## 2. 확인된 프로젝트 구조

### 2.1 주요 경로

- 스프라이트: Assets/_Project/Art/Sprite/Tilesets/
- 타일 에셋: Assets/_Project/Tiles/Assets_TIles/
- 기준 씬: Assets/_Project/Scenes/Main.unity
- 확장 RuleTile: Assets/_Project/Tiles/Assets_TIles/AdvancedRuleTile.cs
- 패턴 RuleTile: Assets/_Project/Tiles/Assets_TIles/AdvancedPatternRuleTile.cs
- 목록 형식: Assets/_Project/Tiles/Assets_TIles/TileList.cs
- 공용 목록: InnerWall.asset, outterWall.asset, Door.asset, Funiture.asset

### 2.2 Main 씬

| 항목 | 확인값 |
|---|---|
| Grid Cell Size | 1 × 1 |
| Floor Anchor | (0.5, 0.5, 0) |
| Wall Anchor | (0.5, 0.5, 0) |
| Floor Collider | 없음 |
| Wall Collider | TilemapCollider2D + CompositeCollider2D |
| Wall Rigidbody2D | Static |

Main 씬에는 Floor와 Wall 두 Tilemap이 있다. 현재 씬에 어떤 가구가 배치됐는지와 무관하게 Wall의 설계 역할에는 정적·동적 가구가 모두 포함된다.

### 2.3 공통 임포트 양식

Tilesets 아래 PNG 35개의 .meta는 모두 다음 설정을 사용한다.

| 설정 | 확인값 |
|---|---|
| Texture Type | Sprite |
| Pixels Per Unit | 16 |
| Filter Mode | Point |
| Mip Maps | Off |
| Default Compression | Uncompressed |
| Wrap U/V | Clamp |
| Alpha Is Transparency | On |

벽·문과 가구에는 역할과 pivot에 따라 서로 다른 투명 여백이 있다. 기준 에셋을 변경하는 작업이 아니라면 이를 일괄 제거하지 않는다. 바닥 slice는 현재 불투명 픽셀의 alpha bounds가 16×16 slice 전체에 닿는다.

## 3. 이미지 파일 양식

### 3.1 벽

Wall 폴더의 PNG 16개, slice 115개를 확인했다. 모든 벽 slice는 다음 규격이다.

- Slice: 16×32
- PPU: 16
- Alignment: Custom
- Pivot: (0.5, 0.25)

실내 벽의 전체 제작 세트는 다음과 같다.

| 역할 | 이미지 크기 | Slice 수 | Slice 규격 |
|---|---:|---:|---:|
| 기본 연결 시트 | 80×96 | 13 | 16×32 |
| 문 접점 | 48×64 | 6 | 16×32 |
| 문 모서리 접점 | 32×64 | 4 | 16×32 |
| 측면 문 접점 | 32×96 | 6 | 16×32 |
| 측면 문 모서리 접점 | 32×64 | 4 | 16×32 |
| 변형 시트 | 128×32 | 8 | 16×32 |

CreamWallpaper와 DirtyWallpaper가 이 전체 양식을 사용한다. RedBrick은 현재 기본 연결 13, 문 접점 6, 측면 문 접점 6, 변형 8 slice를 사용한다.

벽 제작 규칙:

1. 기본 연결 시트의 slice 위치와 internalID 역할을 기준 벽에서 그대로 대응시킨다.
2. 직선, 끝, 내·외부 모서리, T자, 십자 역할을 먼저 목록화한다.
3. 문 접점은 위·아래·좌·우와 대각선·모서리 조건을 별도 스프라이트로 만든다.
4. 논리적 2셀 두께와 16×32 스프라이트 높이를 각각 확인한다.
5. 투명 여백은 기존 역할별 alpha bounds를 따른다. 일괄 제거하지 않는다.
6. 새 실내 벽은 Wall_CreamWallPaper 또는 Wall_DirtyWallPaper의 28개 규칙을 기준으로 한다.
7. 외벽은 Wall_RedBrick의 20개 규칙과 실제 제공 스프라이트 역할을 기준으로 한다.

### 3.2 문

door 폴더의 PNG 4개, slice 32개를 확인했다. 모든 문 slice는 16×32, PPU 16, pivot (0.5, 0.25)이다.

| 파일 | 이미지 크기 | Slice 수 |
|---|---:|---:|
| Door_pull.png | 64×64 | 8 |
| Door_push.png | 64×64 | 8 |
| Door_pull_side.png | 128×32 | 8 |
| Door_push_side.png | 128×32 | 8 |

Close_Door.asset은 8개 규칙과 동일 Door Tile 두 셀의 This 조건으로 가로·세로 방향을 판정한다. Door.asset, InnerWall.asset, outterWall.asset 관계와 두 셀 조립을 함께 유지한다.

### 3.3 바닥

Floor 폴더의 모든 slice는 16×16, PPU 16이며 불투명 픽셀의 alpha bounds가 셀 전체에 닿는다.

| 파일 | 이미지 크기 | Slice 수 | 용도 |
|---|---:|---:|---|
| road sidewalk.png | 128×128 | 64 | 도로·보도 연결 역할 |
| sidewalk.png | 16×16 | 1 | Sidewalk 기본 |
| woodfloor.png | 48×16 | 3 | 목재 바닥 변형 |

Floor_Wood.asset은 m_TilingRules와 patternSprites가 비어 있어 woodfloor_0 기본 스프라이트 하나를 반복한다. Sidewalk.asset은 8개 규칙에서 Road.asset을 Target1으로 직접 참조한다.

현재 바닥의 반대편 경계 픽셀은 모든 slice에서 동일하지 않다. 전체 바닥에 단순한 좌우·상하 픽셀 동일성 검사를 강제하지 않는다. 반복용 단일 타일은 Unity 화면에서 실제 이음새와 줄눈을 확인하고, 경계가 다른 장식은 기본 Random 변형이 아니라 별도 데칼 또는 의도된 연결 규칙으로 처리한다.

### 3.4 가구

Funiture 폴더의 PNG 12개와 slice 85개를 확인했다. 방향성 가구는 Target1 벽 방향을 상·좌·우·하 네 방향으로 구분한다.

| 파일 | 이미지 크기 | Slice | Slice 규격 | Pivot | 구성 |
|---|---:|---:|---:|---:|---|
| bed.png | 128×96 | 8 | 32×48 | (0.5, 0.5) | 4방향 × 2조각 |
| bigChiffonier.png | 128×48 | 4 | 32×48 | (0.5, 0.33333334) | 4방향 × 1조각 |
| bookcase.png | 384×48 | 12 | 32×48 | (0.5, 0.33333334) | 4방향 × 3조각 |
| chiffonier.png | 128×48 | 4 | 32×48 | (0.5, 0.33333334) | 4방향 × 1조각 |
| kitchen.png | 384×48 | 12 | 32×48 | (0.5, 0.33333334) | 4방향 × 3조각 |
| kitchen2.png | 384×48 | 12 | 32×48 | (0.5, 0.33333334) | 4방향 × 3조각 |
| refrigerator.png | 128×48 | 4 | 32×48 | (0.5, 0.33333334) | 4방향 × 1조각 |
| table.png | 48×96 | 9 | 16×32 | (0.5, 0.25) | 3×3 연결 토폴로지 |
| toilet.png | 128×48 | 4 | 32×48 | (0.5, 0.33333334) | 4방향 × 1조각 |
| TV.png | 256×48 | 8 | 32×48 | (0.5, 0.33333334) | 4방향 × 2조각 |
| wardrobe.png | 128×48 | 4 | 32×48 | (0.5, 0.33333334) | 4방향 × 1조각 |
| washstand.png | 128×48 | 4 | 32×48 | (0.5, 0.33333334) | 4방향 × 1조각 |

단일·3조각 벽 부착형 가구의 sprite index 묶음은 다음 순서를 따른다.

- 벽이 위: 첫 묶음
- 벽이 왼쪽: 두 번째 묶음
- 벽이 오른쪽: 세 번째 묶음
- 벽이 아래: 네 번째 묶음

4-slice 에셋은 0 / 1 / 2 / 3, 8-slice TV는 0–1 / 2–3 / 4–5 / 6–7, 12-slice 에셋은 0–2 / 3–5 / 6–8 / 9–11로 묶인다. bed는 네 모서리 방향마다 두 셀을 조립하며 sprite 쌍은 0·4 / 1·5 / 2·6 / 3·7이다.

table은 네 벽 방향 Target 세트가 아니라 This·NotThis로 3×3 연결 상태를 선택한다. 이 토폴로지는 새 방향성 가구의 4방향 제작 원칙을 대체하지 않는다.

## 4. RuleTile 구조

### 4.1 AdvancedRuleTile 판정

| 값 | 이름 | 일치 대상 |
|---:|---|---|
| 0 | Ignore | 기본 RuleTile 처리 |
| 1 | This | 자기 자신, siblingTiles, siblingTileList |
| 2 | NotThis | 빈 셀, nullTiles, nullTileList |
| 3 | Target1 | targetTile1, targetList1 |
| 4 | Target2 | targetTile2, targetList2 |
| 5 | Target3 | targetTile3, targetList3 |

m_TilingRules만 복사하고 목록 참조를 빠뜨리면 같은 결과가 나오지 않는다.

### 4.2 규칙 순서

설치된 Tilemap Extras 4.1.0의 RuleTile은 m_TilingRules를 직렬화 순서대로 검사하고 첫 일치 규칙에서 멈춘다.

- 문 접점, Target, 대각선처럼 구체적인 규칙을 먼저 둔다.
- 모서리·T자·십자 규칙을 일반 직선·기본 규칙보다 먼저 둔다.
- m_Id는 우선순위가 아니다.
- m_Id를 기준으로 규칙 배열을 정렬하지 않는다.
- 기존 토폴로지를 복제할 때 규칙 순서와 이웃 좌표를 함께 보존한다.
- 각 m_Id는 에셋 안에서 고유하게 유지한다.

### 4.3 공용 TileList

| 목록 | 등록 수 | 중복 GUID |
|---|---:|---:|
| InnerWall.asset | 18 | 0 |
| outterWall.asset | 9 | 0 |
| Door.asset | 4 | 0 |
| Funiture.asset | 12 | 0 |

모든 목록 GUID는 현재 실제 에셋으로 해석된다.

### 4.4 대표 관계

| 에셋 | 규칙 | 주요 관계 |
|---|---:|---|
| Wall_CreamWallPaper.asset | 28개 | sibling InnerWall, null Funiture, Target2 Door |
| Wall_DirtyWallPaper.asset | 28개 | 실내 벽 28규칙 토폴로지 |
| Wall_RedBrick.asset | 20개 | 외벽 20규칙 토폴로지 |
| Close_Door.asset | 8개 | sibling Door, Target1 outterWall, Target2 InnerWall |
| Sidewalk.asset | 8개 | Target1 Road 직접 참조 |
| Floor_Wood.asset | 0개 | 기본 스프라이트 반복 |

현재 m_TilingRules 필드를 가진 에셋은 46개다. 이 중 규칙이 하나 이상인 44개 에셋은 각 에셋 내부에서 규칙 ID가 중복되지 않는다. 46개 에셋의 m_DefaultGameObject와 모든 규칙의 m_GameObject도 null이다.

## 5. 변경 절차

### 5.1 기준 조사

1. 대상 씬에서 실제 Tile asset GUID와 Tilemap을 찾는다.
2. 규칙 배열의 직렬화 순서, ID, 이웃 조건, 출력 스프라이트를 확인한다.
3. sibling, null, target의 직접 참조와 TileList를 확인한다.
4. 원본 .meta의 slice 위치, internalID, pivot, PPU를 확인한다.
5. 이미지에서 방향별·조각별 스프라이트가 모두 있는지 확인한다.
6. m_Tiles와 m_TileAssetArray를 연결해 실제 점유 셀을 확인한다.

### 5.2 스프라이트 제작

- 벽·문은 16×32와 pivot (0.5, 0.25)을 유지한다.
- 바닥은 16×16로 셀 전체를 채운다.
- 가구는 기존 대응 에셋의 크기와 pivot을 기준으로 네 방향을 모두 제작한다.
- 조립형 가구는 각 방향에 필요한 모든 조각을 완성한다.
- 스프라이트 시트의 실제 시작 좌표와 stride를 측정한다.
- alpha bounds가 의도된 역할과 일치하는지 slice별로 확인한다.
- 한 방향 스프라이트를 회전해 다른 방향을 만들지 않는다.

### 5.3 에셋 적용

- 복잡한 벽·문은 검증된 기준 에셋의 규칙 순서와 목록 관계를 보존한다.
- 새 스프라이트를 동일한 역할의 m_Sprites 위치에 연결한다.
- slice internalID와 RuleTile sprite fileID가 일치하는지 확인한다.
- 새 Tile을 필요한 공용 TileList에 중복 없이 등록한다.
- 새 에셋에는 고유 GUID를 사용한다.
- 사용자 요청이 없으면 Tile Palette를 변경하지 않는다.

### 5.4 배치

- 벽 2셀 두께와 문 방향별 2셀 대체를 먼저 만족시킨다.
- 벽·문·가구 이웃 판정은 같은 Wall Tilemap에서 수행한다.
- 가구는 선택한 방향의 sprite 세트와 규칙을 사용한다.
- 큰 가구는 sprite.bounds를 월드 좌표로 환산해 벽, 문, 통로, 다른 가구와 교차 검사한다.
- 바닥은 Floor Tilemap에 배치하고 반복 경계를 Unity 화면에서 확인한다.

## 6. 검증 기준

### 6.1 에셋

- 경로와 GUID 참조가 유효한가?
- slice 크기, 위치, internalID, pivot, PPU가 양식과 일치하는가?
- RuleTile 순서, ID, 이웃 조건, 출력 스프라이트가 의도와 일치하는가?
- TileList에 누락이나 중복이 없는가?
- 정적 타일의 기본 및 규칙별 GameObject가 모두 null인가?
- 요청하지 않은 Palette 변경이 없는가?

### 6.2 디자인과 씬

- 벽이 모든 구간에서 논리적 2셀 두께인가?
- 벽 스프라이트가 16×32와 올바른 pivot을 유지하는가?
- 직선, 끝, 내·외부 모서리, T자, 십자 스프라이트가 올바른가?
- 문이 방향에 맞는 동일 Door Tile 2셀로 벽을 대체하는가?
- 문·벽 접점과 대각선 접점이 올바른가?
- 가구가 네 방향을 모두 갖추고 각 방향의 조각이 완전한가?
- 가구·벽 Target 접점과 가구 This 조립이 올바른가?
- 가구 Bounds가 벽, 문, 통로, 다른 가구를 침범하지 않는가?
- Floor 반복 경계, 줄눈, 투명 여백, 정렬이 올바른가?
- Floor Collider가 없고 Wall CompositeCollider가 의도한 영역만 막는가?

### 6.3 실행과 변경 범위

- 영향받은 에셋을 리임포트하고 필요한 Tilemap만 Refresh했는가?
- C# 변경 시 Unity 컴파일 오류가 없는가?
- 씬 변경 시 저장 성공을 확인했는가?
- 변경 파일이 사용자 요청 범위를 벗어나지 않았는가?

## 7. 팩트 체크 범위와 한계

이번 검사는 다음을 대상으로 했다.

- Main.unity의 Grid, Tilemap, Collider, Rigidbody2D
- AdvancedRuleTile.cs, AdvancedPatternRuleTile.cs, TileList.cs
- Tilemap Extras 4.1.0의 RuleTile.cs
- Wall·door·Floor·Funiture의 PNG 35개와 모든 .meta slice
- 벽 기준 RuleTile 3개, Close_Door, Floor_Wood, Sidewalk
- 가구 PNG 12개와 RuleTile 12개
- 공용 TileList 4개
- Assets_TIles 아래 Tile·RuleTile의 ID와 GameObject 필드

직렬화와 픽셀 검사는 구조, 참조, 크기, pivot, alpha bounds를 확인할 수 있다. 최종 픽셀 품질, 타일 연결 모양, Collider 체감은 Unity 에디터 또는 Play Mode에서 확인한다.
