# BobVirus 타일 시스템 참고서

이 문서는 타일 작업에 필요한 디자인 불변식, 프로젝트 구조, 확인 절차를 정리한다. 직렬화 파일은 구현 상태를 확인하는 근거지만, 단일 씬 사례가 명시된 디자인 불변식을 대체하지 않는다.

## 1. 확인된 프로젝트 구조

### 1.1 주요 경로

- 스프라이트: Assets/_Project/Art/Sprite/Tilesets/
- 타일 에셋: Assets/_Project/Tiles/Assets_TIles/
- 기준 씬: Assets/_Project/Scenes/Main.unity
- 확장 RuleTile: Assets/_Project/Tiles/Assets_TIles/AdvancedRuleTile.cs
- 목록 형식: Assets/_Project/Tiles/Assets_TIles/TileList.cs
- 공용 목록: InnerWall.asset, outterWall.asset, Door.asset, Funiture.asset

Funiture, Assets_TIles, outterWall은 현재 참조에 사용되는 이름이다. 별도 마이그레이션 없이 철자를 고치면 경로와 참조가 깨질 수 있다.

### 1.2 Main 씬의 Grid와 Tilemap

현재 Main 씬에서 확인한 값은 다음과 같다.

| 항목 | 확인값 |
|---|---|
| Grid Cell Size | 1 × 1 |
| Floor Anchor | (0.5, 0.5, 0) |
| Wall Anchor | (0.5, 0.5, 0) |
| Floor Collider | 없음 |
| Wall Collider | TilemapCollider2D + CompositeCollider2D |
| Wall Rigidbody2D | Static |

Main 씬에는 Floor와 Wall 두 Tilemap이 있다. Floor는 바닥을, Wall은 벽·문·정적 가구를 담는다. Wall의 연결형 타일은 같은 Tilemap의 이웃을 판정하므로, 근거 없이 서로 다른 Tilemap으로 분리하지 않는다.

### 1.3 스프라이트 임포트 현황

Assets/_Project/Art/Sprite/Tilesets 아래 PNG 35개의 .meta를 확인한 결과가 모두 다음과 같았다.

| 설정 | 확인값 |
|---|---|
| Pixels Per Unit | 16 |
| Filter Mode | Point |
| Mip Maps | Off |
| Compression | Uncompressed |
| Wrap U/V | Clamp |

이는 현재 기준값이지 모든 미래 이미지의 크기나 피벗이 같다는 뜻은 아니다. 새 스프라이트는 대응하는 기준 에셋의 slice 크기와 pivot까지 비교한다. 시트에 여백이 있으면 시작 좌표와 stride를 측정한 뒤 자른다.

### 1.4 AdvancedRuleTile 판정

AdvancedRuleTile.RuleMatch의 실제 판정은 다음과 같다.

| 값 | 이름 | 일치 대상 |
|---:|---|---|
| 0 | Ignore | 기본 RuleTile 처리 |
| 1 | This | 자기 자신, siblingTiles, siblingTileList |
| 2 | NotThis | 빈 셀, nullTiles, nullTileList |
| 3 | Target1 | targetTile1, targetList1 |
| 4 | Target2 | targetTile2, targetList2 |
| 5 | Target3 | targetTile3, targetList3 |

규칙을 복사해도 목록 참조가 다르면 결과가 달라진다. m_TilingRules뿐 아니라 sibling, null, target 참조를 함께 확인한다.

### 1.5 공용 TileList

TileList는 List<TileBase> Tiles를 가진 ScriptableObject다. 현재 목록은 다음과 같다.

| 목록 | 등록 수 | 중복 GUID |
|---|---:|---:|
| InnerWall.asset | 18 | 0 |
| outterWall.asset | 9 | 0 |
| Door.asset | 4 | 0 |
| Funiture.asset | 12 | 0 |

공용 TileList 등록은 RuleMatch 관계를 위한 데이터 변경이며 Tile Palette 등록과는 별개다. 새 타일은 필요한 목록에만 한 번 등록한다.

### 1.6 대표 RuleTile 관계

현재 직렬화 데이터를 기준으로 확인한 대표 관계는 다음과 같다.

| 에셋 | 규칙 | 주요 관계 |
|---|---:|---|
| Wall_CreamWallPaper.asset | 28개, ID 0–27 | sibling InnerWall, null Funiture, Target2 Door |
| Close_Door.asset | 8개, ID 0–7 | sibling Door, Target1 outterWall, Target2 InnerWall |
| Sidewalk.asset | 8개 | Target1이 Road.asset을 직접 참조 |
| Floor_Wood.asset | 0개 | 하나의 기본 스프라이트를 반복 |

Wall_CreamWallPaper의 이웃 조건에는 대각선 좌표가 포함된다. 이를 일반적인 4방향 16마스크로 바꾸면 기존 접점 표현을 잃을 수 있다.

Floor_Wood은 woodfloor.png의 16×16 slice 중 하나를 기본 스프라이트로 사용한다. 반복 경계가 시각적으로 자연스러운지는 직렬화 값만으로 확정할 수 없으므로 Unity 화면에서 별도로 확인한다.

### 1.7 벽과 문의 불변 배치 규칙

다음 규칙은 현재 씬의 특정 배치 사례보다 우선하는 최상위 디자인 제약이다.

- 벽은 항상 논리적으로 2셀 두께다.
- 가로 벽은 연속된 2개 행을 채운다.
- 세로 벽은 연속된 2개 열을 채운다.
- 모서리는 두 방향의 2셀 두께가 끊기지 않게 교차한다.
- 문 하나는 동일 Door RuleTile을 인접한 2셀에 배치해 구성한다.
- 가로 벽을 통과하는 문은 위·아래의 세로 2셀로 배치한다.
- 세로 벽을 통과하는 문은 좌·우의 가로 2셀로 배치한다.
- 문이 차지하는 두 셀에는 벽 타일을 남기지 않는다.

스프라이트가 한 셀보다 커 보일 수 있으므로 논리 셀 점유와 sprite.bounds를 구분한다. 큰 가구는 앵커 셀만 보지 말고 전체 Bounds가 벽, 문, 다른 가구와 겹치는지 확인한다.

### 1.8 가구와 기본 프리팹

Funiture 폴더의 RuleTile 에셋 12개를 확인했다.

- 6개는 This 이웃 조건으로 같은 가구 조각을 조립한다.
- 11개는 Target1에서 InnerWall 목록을 참조한다.
- 모든 가구가 같은 조립 방식이나 벽 관계를 쓰는 것은 아니다.

Assets_TIles 아래 46개 Tile·RuleTile 에셋의 m_DefaultGameObject는 모두 null이다. 정적 맵 요소에 프리팹 생성을 추가하려면 런타임 상태나 상호작용 같은 명시적 이유가 필요하다.

## 2. 변경 절차

### 2.1 기준 에셋 조사

변경 전에 다음을 확인한다.

1. Main 씬 또는 대상 씬에서 실제 Tile asset GUID를 찾는다.
2. 에셋의 규칙 수, 순서, ID, 기본 스프라이트를 확인한다.
3. siblingTileList, nullTileList, targetList1–3과 직접 참조 배열을 확인한다.
4. 참조 스프라이트의 .meta에서 slice, pivot, PPU, 임포트 설정을 확인한다.
5. 씬의 m_Tiles 좌표와 m_TileAssetArray를 연결해 실제 점유 셀을 확인한다.

시각적으로 비슷하다는 이유만으로 새 토폴로지를 설계하지 않는다. 기준 에셋의 동작을 유지해야 한다면 구조를 복제하고 필요한 스프라이트 참조만 바꾼다.

### 2.2 스프라이트와 RuleTile 수정

- 한 셀용 스프라이트는 기준 셀 크기와 투명 여백을 실제 픽셀로 확인한다.
- 큰 스프라이트는 기준 에셋의 slice와 pivot을 유지한다.
- 규칙마다 필요한 방향과 역할의 스프라이트를 먼저 목록화한다.
- 각 규칙 ID는 에셋 안에서 고유하게 유지한다.
- 대각선, 문 접점, Target 조건을 삭제하거나 단순화하기 전에 사용 장면을 확인한다.
- 큰 단일 스프라이트를 회전할 때는 회전 후 Bounds와 피벗을 다시 검증한다.

### 2.3 TileList와 Palette

- TileList에는 필요한 에셋만 중복 없이 등록한다.
- 새 에셋은 고유 GUID를 사용한다.
- 기존 GUID를 복사하거나 수동 재사용하지 않는다.
- TileList 변경과 Tile Palette 변경을 혼동하지 않는다.
- 사용자가 요청하지 않았다면 Palette는 그대로 둔다.

### 2.4 배치

- 서로를 이웃으로 판정해야 하는 벽, 문, 가구는 같은 Tilemap에 둔다.
- 별도 Tilemap을 사용하려면 교차 Tilemap 판정 코드가 있는지 먼저 확인한다.
- 가로·세로 벽은 각각 2개 행·2개 열을 채우고, 문은 벽 방향에 맞는 인접 2셀로 벽을 대체한다.
- 큰 가구는 sprite.bounds를 월드 좌표로 환산해 벽, 문, 다른 가구와 교차 검사한다.
- 바닥은 Floor Tilemap에 두고, 반복 경계와 투명 여백은 렌더링 결과로 확인한다.

## 3. 검증 기준

### 3.1 정적 검증

- 수정한 경로와 GUID 참조가 모두 유효한가?
- RuleTile의 규칙 수, ID, 이웃 조건, 스프라이트 배열이 의도와 일치하는가?
- TileList에 누락이나 중복이 없는가?
- .meta의 PPU, 필터, Mipmap, 압축, Wrap, slice, pivot이 기준과 맞는가?
- 정적 타일의 m_DefaultGameObject가 null인가?
- 요청하지 않은 Palette 변경이 없는가?

### 3.2 씬 검증

- 직선, 모서리, 분기, 문·가구 접점에서 올바른 스프라이트가 선택되는가?
- 벽이 모든 구간에서 2셀 두께를 유지하는가?
- 문이 방향에 맞는 2셀을 차지하고 해당 위치의 벽을 정확히 대체하는가?
- 가구의 전체 점유 영역이 벽, 문, 통로를 침범하지 않는가?
- Floor에는 불필요한 Collider가 없고 Wall Collider가 의도한 영역만 막는가?
- 수정한 맵 요소 아래에 불필요한 SpriteRenderer가 생기지 않았는가?
- 바닥 반복 경계, 투명 여백, 정렬을 Unity 화면에서 확인했는가?

### 3.3 코드와 저장 검증

- C#을 변경했다면 Unity 컴파일 오류가 없는가?
- 에셋을 리임포트하고 Tilemap을 Refresh했는가?
- 씬 저장 성공을 확인했는가?
- 변경 파일이 요청 범위를 벗어나지 않았는가?

## 4. 팩트 체크 범위

이번 확인은 다음 파일과 직렬화 데이터를 대상으로 했다.

- Main.unity의 Grid, Tilemap, Collider, Rigidbody2D, 타일 좌표와 에셋 배열
- AdvancedRuleTile.cs와 TileList.cs
- Wall_CreamWallPaper.asset, Close_Door.asset, Floor_Wood.asset, Sidewalk.asset
- InnerWall.asset, outterWall.asset, Door.asset, Funiture.asset
- Funiture 폴더의 RuleTile 12개
- Tilesets 아래 PNG 35개의 .meta
- Assets_TIles 아래 Tile·RuleTile 에셋의 m_DefaultGameObject

직렬화 검사는 구조와 참조를 확인할 수 있지만 최종 픽셀 품질, 연결 모양, Collider 체감은 보장하지 않는다. 이 항목은 Unity 에디터 또는 Play Mode에서 시각적으로 확인한다.
