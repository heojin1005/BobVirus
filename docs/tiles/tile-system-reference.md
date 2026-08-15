# BobVirus 타일셋 기술 참고서

이 문서는 BobVirus 타일셋의 상세 구조, 제작·등록·배치 방법, 문제 해결 절차를 보존한 참고 자료이다. 모든 작업에 항상 적용할 핵심 지침은 저장소 루트의 AGENTS.md를 따른다.

Hospital 구현은 별도 로컬 브랜치에만 존재할 수 있다. Hospital 관련 작업을 시작하기 전에 현재 체크아웃한 브랜치에서 씬, 생성기, 에셋의 실제 존재 여부를 확인한다. 파일이 보이지 않는다는 이유로 이 기준을 폐기하거나 추측으로 다시 만들지 않는다.

## 1. 프로젝트 타일셋 구조

### 1.1 기본 원칙

- 이 프로젝트의 맵은 개별 `SpriteRenderer` 오브젝트를 늘어놓아 구성하지 않는다.
- 맵 구성 요소는 `Grid` 아래의 `Tilemap`에 `TileBase` 에셋으로 칠한다.
- Main 씬의 핵심 Tilemap은 `Floor`와 `Wall`이다.
- 벽, 문, 가구는 서로의 이웃을 판정해야 하므로 기본적으로 같은 `Wall` Tilemap에 배치한다.
- 바닥은 별도의 `Floor` Tilemap에 배치한다.
- 씬에서 선택·이동할 수 있는 단위는 개별 가구 GameObject가 아니라 Tilemap이다.
- Tile Palette 등록과 Tile/RuleTile 에셋 생성은 별개 작업이다. 사용자의 검수 전에는 Palette를 임의로 변경하지 않는다.

### 1.2 주요 경로

```text
Assets/_Project/Art/Sprite/Tilesets/
├─ Wall/
├─ Floor/
├─ door/
└─ Funiture/

Assets/_Project/Tiles/Assets_TIles/
├─ Wall/
├─ Floor/
├─ Door/
├─ Funiture/
├─ InnerWall.asset
├─ outterWall.asset
├─ Door.asset
├─ Funiture.asset
├─ AdvancedRuleTile.cs
└─ TileList.cs
```

프로젝트에 `Funiture`라는 철자가 이미 경로 및 에셋 이름으로 사용되고 있다. 기존 참조를 깨지 않도록 임의로 `Furniture`로 변경하지 않는다.

### 1.3 Grid와 Tilemap

Main 씬과 HospitalTest 씬의 기본 Grid 규격은 다음과 같다.

```text
Grid Cell Size: 1 × 1
Tilemap Anchor: (0.5, 0.5, 0)
Pixels Per Unit: 16
기본 논리 셀: 16 × 16 픽셀
```

Tilemap 역할은 다음과 같다.

| Tilemap | 역할 | 일반적인 Collider |
|---|---|---|
| `Floor` | 바닥, 보행 영역 | 없음 |
| `Wall` | 벽, 문, 가구 | `TilemapCollider2D` + `CompositeCollider2D` |

`Wall` Tilemap에는 정적 `Rigidbody2D`, `TilemapCollider2D`, `CompositeCollider2D`를 사용한다. 개별 타일이 GameObject를 생성하게 만들지 않는다.

### 1.4 AdvancedRuleTile

프로젝트의 연결형 타일은 기본 `RuleTile`이 아니라 다음 클래스를 사용한다.

```text
Assets/_Project/Tiles/Assets_TIles/AdvancedRuleTile.cs
```

`AdvancedRuleTile`은 기본 이웃 코드에 프로젝트 전용 대상을 추가한다.

| 값 | 의미 |
|---:|---|
| `0` | Ignore |
| `1` | This |
| `2` | NotThis |
| `3` | Target1 |
| `4` | Target2 |
| `5` | Target3 |

판정 의미는 다음과 같다.

- `This`: 현재 RuleTile 자체, `siblingTiles`, `siblingTileList`에 포함된 타일
- `NotThis`: 빈 셀, `nullTiles`, `nullTileList`에 포함된 타일
- `Target1~3`: 해당 `targetTile` 배열 또는 `targetList`에 포함된 타일

이 구조 때문에 타일 에셋의 규칙만 복사해서는 충분하지 않다. 연결 대상이 올바른 `TileList`에 등록되어 있어야 한다.

### 1.5 TileList 그룹

`TileList.cs`는 `List<TileBase> Tiles`를 가진 ScriptableObject이다. 주요 그룹은 다음과 같다.

| TileList | 목적 |
|---|---|
| `InnerWall.asset` | 실내 벽끼리 `This`로 연결하거나 문·가구가 실내 벽을 찾을 때 사용 |
| `outterWall.asset` | 외벽 그룹 |
| `Door.asset` | 문 그룹 |
| `Funiture.asset` | 가구 그룹 |

새 병원 타일처럼 기존 그룹과 상호작용해야 하는 타일을 별도의 고립된 TileList에만 넣으면 안 된다. 예를 들어 병원 벽은 `InnerWall`, 병원 문은 `Door`, 병원 가구는 `Funiture`에도 반드시 등록해야 한다.

### 1.6 벽 구조

벽은 1셀 외곽선이 아니다. Main 씬의 실제 직렬화 데이터를 기준으로 다음과 같이 배치한다.

- 가로 벽은 연속된 2개 행을 채운다.
- 세로 벽은 연속된 2개 열을 채운다.
- 모서리는 2×2 논리 셀 영역이 자연스럽게 교차한다.
- 일반적인 벽 스프라이트는 `16×32`, PPU 16이므로 시각적으로도 세로 2셀 높이를 가진다.
- 논리적인 2셀 두께와 스프라이트의 2셀 높이는 서로 다른 개념이며 둘 다 지켜야 한다.

예시:

```csharp
for (int x = minX; x <= maxX; x++)
{
    wall.SetTile(new Vector3Int(x, topY, 0), wallTile);
    wall.SetTile(new Vector3Int(x, topY - 1, 0), wallTile);
}

for (int y = minY; y <= maxY; y++)
{
    wall.SetTile(new Vector3Int(leftX, y, 0), wallTile);
    wall.SetTile(new Vector3Int(leftX + 1, y, 0), wallTile);
}
```

기존 `Wall_CreamWallPaper.asset`은 4방향 16마스크가 아니라 8방향 조건, 대각선, 문 접점 전용 규칙을 포함한 28개 규칙을 사용한다. 새 실내 벽을 만들 때는 이를 기준 템플릿으로 사용하는 것이 안전하다.

### 1.7 문 구조

문 한 개는 Door RuleTile 한 셀이 아니다. Main 씬의 `Close_Door.asset`은 동일 Door RuleTile 두 셀을 조합해 방향을 판정한다.

- 가로 벽을 통과하는 문: 동일 문 타일을 위·아래 2셀로 배치
- 세로 벽을 통과하는 문: 동일 문 타일을 좌·우 2셀로 배치

예시:

```csharp
// 가로 벽의 문
wall.SetTile(new Vector3Int(x, y, 0), doorTile);
wall.SetTile(new Vector3Int(x, y + 1, 0), doorTile);

// 세로 벽의 문
wall.SetTile(new Vector3Int(x, y, 0), doorTile);
wall.SetTile(new Vector3Int(x + 1, y, 0), doorTile);
```

`Close_Door.asset`은 8개 규칙을 사용하며 `Door.asset`, `InnerWall.asset`, `outterWall.asset`을 참조한다. 문 규칙을 단순한 가로/세로 2규칙으로 축약하지 않는다.

벽 쪽 RuleTile도 문을 `Target2`로 인식한다. 문 전용 규칙은 일반 벽 규칙보다 먼저 평가되어야 한다.

### 1.8 가구 구조

- 가구도 GameObject가 아니라 TileBase/AdvancedRuleTile 에셋이다.
- 기존 프로젝트의 일부 가구는 같은 가구 타일을 여러 셀에 칠하고 `This` 이웃과 `InnerWall Target1`을 이용해 조립한다.
- Hospital 가구는 현재 큰 단일 스프라이트를 한 앵커 셀에 배치하는 형태도 사용한다.
- 큰 가구는 이미지 크기와 피벗에 따라 여러 논리 셀 위에 시각적으로 걸쳐 보인다.

예를 들어 `32×48`, PPU 16 스프라이트는 2×3셀 크기이다. 앵커 셀 하나만 확인해서 벽 바로 옆에 배치하면 나머지 이미지 영역이 벽을 관통할 수 있다.

가구를 벽과 연결할 때는 다음 두 방향을 모두 고려한다.

1. 벽 RuleTile이 `Funiture.asset`을 `NotThis`로 인식해 적절한 끝 모양을 출력하는가?
2. 가구 RuleTile이 `InnerWall.asset`을 `Target1`로 인식해 올바른 방향·변형을 선택하는가?

큰 단일 스프라이트를 RuleTile 회전만으로 돌리는 것은 피한다. 피벗을 중심으로 2×3셀 Bounds 전체가 회전하여 건물 밖이나 벽 안으로 들어갈 수 있다. 방향별 가구가 필요하면 다음 중 하나를 사용한다.

- 방향별 스프라이트와 방향별 규칙을 제작한다.
- 기존 가구처럼 16×16 조각으로 분해한 다중 셀 RuleTile을 만든다.
- 회전하지 않는 기본 방향으로 배치하고 실제 Bounds가 들어갈 공간을 확보한다.

### 1.9 바닥 구조

Main의 실내 바닥인 `Floor_Wood.asset`은 연결 규칙을 남발하지 않고 하나의 seamless 스프라이트를 반복한다. `Sidewalk.asset`은 Road와의 접점을 찾는 별도 용도의 RuleTile이다. 실내 바닥과 보도 규칙을 혼용하지 않는다.

Hospital 바닥은 다음 파일을 사용한다.

```text
Assets/_Project/Art/Sprite/Tilesets/Floor/Hospital_Floor_Seamless.png
```

규격은 정확히 `16×16`, PPU 16, 투명 여백 없음이다. 한 스프라이트가 Grid 한 셀을 완전히 채운다.

바닥 변형을 무작위로 섞을 때 각 변형의 줄눈과 경계 픽셀이 정확히 같지 않으면 셀이 연결되지 않은 것처럼 보인다. 기본 바닥은 단일 seamless 타일을 사용하고, 얼룩·균열은 별도 데칼 타일이나 동일 경계를 보장하는 변형으로 추가한다.

## 2. 타일셋 제작 및 적용 방법

### 2.1 기존 구조 조사

새 타일을 만들기 전에 다음 순서로 기준 타일을 조사한다.

1. Main 씬의 대상 Tilemap에서 실제 사용 중인 Tile asset GUID를 찾는다.
2. 해당 Tile/AdvancedRuleTile의 규칙 수와 순서를 확인한다.
3. `siblingTileList`, `nullTileList`, `targetList1~3`를 확인한다.
4. 각 규칙이 참조하는 스프라이트 원본과 `.meta`의 slice, pivot, PPU를 확인한다.
5. 씬의 `m_Tiles` 좌표를 확인해 한 구조물이 실제로 몇 셀을 점유하는지 확인한다.

시각적으로 비슷해 보인다는 이유만으로 4방향 RuleTile을 새로 설계하지 않는다. 기존 동작이 복잡하면 검증된 RuleTile을 템플릿으로 복제한다.

### 2.2 스프라이트 제작 규격

권장 임포트 설정:

```text
Texture Type: Sprite
Sprite Mode: Single 또는 Multiple
Pixels Per Unit: 16
Filter Mode: Point
Mip Maps: Off
Compression: Uncompressed
Wrap Mode: Clamp
Alpha Is Transparency: On
```

피벗 기준:

- 바닥 16×16: `(0.5, 0.5)`
- 16×32 벽: 기존 벽 `.meta`의 피벗을 그대로 복제
- 큰 가구: 실제 바닥 접점이 Tilemap 셀 중심에 오도록 설정
- 기존 토폴로지를 재사용할 때는 이미지뿐 아니라 `.meta`의 slice와 pivot도 보존

스프라이트 시트에 여백이 있으면 눈으로 16×16처럼 보여도 `index * 16`으로 자르면 안 된다. 먼저 실제 불투명 영역, 시작 좌표, stride, 셀 크기를 측정한다.

### 2.3 검증된 RuleTile 복제

벽이나 문처럼 규칙이 복잡한 타일은 `EditorUtility.CopySerialized`를 사용할 수 있다.

```csharp
AdvancedRuleTile template =
    AssetDatabase.LoadAssetAtPath<AdvancedRuleTile>(templatePath);
AdvancedRuleTile target = LoadOrCreateAsset<AdvancedRuleTile>(targetPath);

EditorUtility.CopySerialized(template, target);
target.name = "NewTileName";
```

이후 각 `m_DefaultSprite`, `m_TilingRules[i].m_Sprites`를 새 스프라이트로 치환한다. 치환 대상 스프라이트는 다음이 동일해야 한다.

- slice 크기
- 피벗
- PPU
- 규칙에서 요구하는 방향/역할

규칙을 복제한 다음 기존 `TileList` 참조를 유지하거나 새 타일을 해당 목록에 추가한다.

### 2.4 RuleTile 규칙 작성

규칙은 위에서 아래로 평가된다. 구체적인 규칙을 먼저 둔다.

권장 순서:

1. 문 접점과 대각선 문 규칙
2. 특정 대상 `Target1~3` 규칙
3. 모서리·T자·십자 규칙
4. 일반 연결 규칙
5. 고립/기본 규칙

정확한 스프라이트가 없는 상태에서 여러 연결 상태를 같은 이미지로 임시 처리하지 않는다. 세로, 내부 모서리, 외부 모서리, T자, 십자, 문 좌우/상하 접점 스프라이트가 필요한지 먼저 목록화한다.

각 `m_Id`는 고유하게 유지한다. 중복 ID가 당장 렌더링을 막지 않더라도 Inspector 편집과 직렬화 안정성을 해칠 수 있다.

### 2.5 TileList 등록

에셋 생성 후 프로젝트 공용 목록에 중복 없이 추가한다.

```csharp
private static TileList AddTilesToProjectList(
    string path,
    IEnumerable<TileBase> tiles)
{
    TileList list = AssetDatabase.LoadAssetAtPath<TileList>(path);
    foreach (TileBase tile in tiles)
    {
        if (tile != null && !list.Tiles.Contains(tile))
            list.Tiles.Add(tile);
    }
    EditorUtility.SetDirty(list);
    return list;
}
```

공용 목록 등록은 Tile Palette 등록이 아니다. RuleMatch 관계를 형성하기 위한 런타임/에디터 데이터 등록이다.

### 2.6 바닥 제작

1. 원본 타일의 불투명 픽셀 Bounds를 측정한다.
2. 시트 여백과 stride를 확인한다.
3. Grid 한 셀용 이미지는 최종적으로 정확히 16×16, PPU 16으로 만든다.
4. 알파가 필요한 디자인이 아니라면 셀 가장자리에 투명 픽셀이 없도록 한다.
5. 좌·우 및 상·하 경계 픽셀이 반복될 때 자연스럽게 이어지는지 확인한다.
6. 기본 실내 바닥은 하나의 seamless 스프라이트를 반복한다.
7. 변형을 사용한다면 모든 변형의 줄눈 위치와 경계색을 동일하게 유지한다.

### 2.7 벽과 문 배치

벽 배치 코드는 논리 2셀 두께를 명시적으로 만든다. 문이 들어갈 두 셀은 벽을 칠하지 않고 Door RuleTile 두 셀로 대체한다.

벽과 문을 다른 Tilemap에 분리하면 같은 Tilemap 이웃을 읽는 RuleTile 판정이 작동하지 않는다. 특별한 교차 Tilemap 판정 코드를 추가하지 않는 한 같은 `Wall` Tilemap을 사용한다.

### 2.8 가구 배치 및 Bounds 검증

가구 배치는 스프라이트 크기와 피벗으로 실제 월드 Bounds를 계산한 후 벽·문 논리 셀과 교차 검사한다.

```csharp
Vector3 center = tilemap.GetCellCenterWorld(position);
float minX = center.x + sprite.bounds.min.x;
float maxX = center.x + sprite.bounds.max.x;
float minY = center.y + sprite.bounds.min.y;
float maxY = center.y + sprite.bounds.max.y;
```

벽 셀의 월드 사각형과 X/Y 양쪽에서 유의미한 교차가 발생하면 배치를 거부한다. Hospital 생성기는 0.01보다 큰 교차를 오류로 처리한다.

가구 배치 시 확인할 항목:

- 외벽과 겹치지 않는가?
- 내부 2셀 벽과 겹치지 않는가?
- 문 두 셀과 겹치지 않는가?
- 회전 후 Bounds도 안전한가?
- 방의 실제 내부 높이가 가구 높이보다 큰가?
- 큰 가구끼리 과도하게 겹치지 않는가?

### 2.9 씬 생성기

Hospital 씬 생성기는 다음 파일에 있다.

```text
Assets/_Project/Editor/HospitalTestSceneGenerator.cs
```

생성기 버전은 `GeneratorVersion`과 `HospitalTest.unity.meta`의 `userData`로 관리한다. 생성 로직이나 배치가 바뀌면 버전을 증가시켜 자동 재생성을 유도한다.

열려 있는 HospitalTest 씬에 새 additive 씬을 만들어 같은 경로로 저장하면 Unity가 저장을 거부한다. 대상 씬이 이미 열려 있으면 기존 root를 정리하고 그 씬을 제자리에서 재구성한다. 새로 연 씬만 저장 후 닫는다.

`EditorSceneManager.SaveScene`의 반환값을 반드시 확인한다.

```csharp
if (!EditorSceneManager.SaveScene(scene, ScenePath))
    throw new InvalidOperationException("Scene save failed: " + ScenePath);
```

## 3. 예상 문제와 대처 방법

### 3.1 벽이 가로로만 연결되고 세로·모서리가 깨짐

원인:

- 8방향 기존 규칙을 4방향 16마스크로 축약함
- 대각선이나 문 접점 전용 스프라이트가 없음
- 잘못 제작된 연결 스프라이트의 가장자리 픽셀이 서로 다름

대처:

- `Wall_CreamWallPaper.asset`의 28개 규칙을 기준으로 비교한다.
- 규칙 순서와 이웃 좌표를 그대로 복제한다.
- 스프라이트 토폴로지, slice, pivot을 보존하고 색상/재질만 변경한다.

### 3.2 벽이 한 줄짜리 외곽선처럼 보임

원인:

- 벽을 한 행 또는 한 열에만 배치함
- 16×32 스프라이트만 보고 논리 셀 두께를 무시함

대처:

- 가로 벽은 2개 행, 세로 벽은 2개 열을 채운다.
- 대표 지점을 생성 후 검사해 두 셀 모두 Wall Tile인지 확인한다.

### 3.3 문이 벽과 연결되지 않음

원인:

- 문을 한 셀만 배치함
- 문이 `Door.asset`에 등록되지 않음
- 벽이 `InnerWall` 또는 `outterWall`에 등록되지 않음
- 문과 벽을 서로 다른 Tilemap에 배치함

대처:

- 동일 Door RuleTile을 방향에 맞춰 2셀 배치한다.
- 문은 `Door.asset`, 벽은 적절한 벽 TileList에 등록한다.
- `Close_Door.asset`의 8개 규칙과 Target 목록을 기준으로 검증한다.
- 두 타일을 같은 `Wall` Tilemap에 배치한다.

### 3.4 가구 옆 벽이 잘못된 모양을 선택함

원인:

- 병원 가구가 `Funiture.asset`에 등록되지 않음
- 벽의 `nullTileList`가 다른 목록을 보고 있음
- 별도 Hospital 전용 목록만 만들어 기존 그룹과 격리함

대처:

- 병원 가구 전부를 공용 `Funiture.asset`에 추가한다.
- 기준 벽의 `nullTileList`가 `Funiture.asset`인지 확인한다.
- 중복 없는 공용 그룹 등록 함수를 사용한다.

### 3.5 가구가 벽 밖으로 삐져나감

원인:

- 앵커 셀만 검사하고 2×3셀 크기의 스프라이트 Bounds를 무시함
- 큰 스프라이트를 RuleTile 회전으로 돌림
- 방 내부 크기가 가구 높이보다 작음
- 피벗이 가구의 바닥 접점과 맞지 않음

대처:

- `sprite.bounds` 전체와 모든 벽·문 셀을 교차 검사한다.
- 큰 가구의 자동 회전을 금지하거나 방향별 스프라이트를 만든다.
- 방 크기를 확장하거나 가구를 안쪽으로 옮긴다.
- 임포트 피벗을 검증한다.
- 겹침이 발견되면 씬 저장을 실패시키는 자동 검증을 둔다.

### 3.6 바닥이 작아서 셀 전체를 채우지 못함

원인:

- 스프라이트 시트의 실제 시작점과 여백을 무시하고 `index * 16`으로 자름
- 원본 도트가 18×18인데 16×16 구간을 잘못 추출함
- 스프라이트 가장자리에 투명 여백이 있음
- PPU가 16이 아님

대처:

- 원본의 불투명 픽셀 run과 stride를 측정한다.
- 실제 타일 영역을 추출한 뒤 16×16로 변환한다.
- 최종 PNG의 알파 Bounds가 0..15 전체인지 검사한다.
- PPU 16, pivot `(0.5, 0.5)`를 적용한다.
- Hospital은 `Hospital_Floor_Seamless.png`를 사용한다.

### 3.7 바닥 무늬가 셀마다 끊겨 보임

원인:

- 경계가 다른 스프라이트를 Random 출력으로 섞음
- 균열, 경고선, 파이프 타일을 기본 바닥 변형으로 사용함

대처:

- 기본 바닥은 하나의 seamless 타일로 반복한다.
- 장식은 별도 Tile/Tilemap 또는 동일 경계의 데칼로 처리한다.
- 변형을 추가하기 전에 상하좌우 경계 픽셀이 동일한지 자동 비교한다.

### 3.8 타일이 개별 GameObject처럼 선택됨

원인:

- SpriteRenderer GameObject를 생성해 배치함
- Tile의 `m_DefaultGameObject`를 설정함

대처:

- 모든 구성물을 Tilemap의 TileBase로 배치한다.
- `m_DefaultGameObject = null`을 유지한다.
- 씬에서 `SpriteRenderer` 개수를 검사해 0인지 확인한다.

### 3.9 규칙은 맞는데 적용되지 않음

원인:

- 새 Tile이 공용 TileList에 없음
- 잘못된 GUID 또는 sprite fileID를 참조함
- `.meta`를 새 이미지에 그대로 복사하면서 GUID를 변경하지 않음
- Unity가 아직 리임포트하지 않음
- 씬 생성기 버전이 증가하지 않음

대처:

- TileList와 GUID를 YAML 또는 Inspector에서 확인한다.
- 복제 이미지의 `.meta` GUID는 반드시 새로 만든다.
- slice의 internalID와 RuleTile sprite 참조를 함께 검증한다.
- `AssetDatabase.Refresh` 후 타일을 Refresh한다.
- 생성 로직 변경 시 `GeneratorVersion`을 올린다.

### 3.10 열린 테스트 씬이 재생성되지 않음

원인:

- 이미 열린 씬과 같은 경로에 새 additive 씬을 저장하려 함
- `SaveScene` 실패를 무시하고 버전만 갱신함

대처:

- `SceneManager.GetSceneByPath`로 열린 씬을 먼저 찾는다.
- 열려 있으면 해당 씬을 제자리에서 재구성한다.
- `SaveScene`이 false이면 예외를 발생시키고 버전을 갱신하지 않는다.

### 3.11 Collider가 이상하거나 통로가 막힘

원인:

- 바닥에 Collider가 있음
- 문 셀이 벽 Collider와 중복됨
- 벽을 2셀로 확장하면서 문 구멍을 두 셀 모두 비우지 않음

대처:

- Floor Collider는 `None`으로 설정한다.
- 문 두 셀이 들어갈 벽 두께 전체를 제거하고 Door Tile로 대체한다.
- `CompositeCollider2D` 결합 설정과 Tile Collider Type을 확인한다.

## 4. 필수 검증 체크리스트

새 타일셋 작업을 완료하기 전에 다음을 전부 확인한다.

### 에셋

- [ ] 모든 Sprite의 PPU가 16이다.
- [ ] Filter Mode가 Point이다.
- [ ] Mipmap이 꺼져 있다.
- [ ] 필요한 Sprite에 투명 여백이 없다.
- [ ] 벽 Sprite가 기준 타일과 동일한 slice/pivot을 사용한다.
- [ ] Rule ID가 중복되지 않는다.
- [ ] `m_DefaultGameObject`가 null이다.

### 관계

- [ ] 실내 벽이 `InnerWall.asset`에 등록되어 있다.
- [ ] 외벽이 `outterWall.asset`에 등록되어 있다.
- [ ] 문이 `Door.asset`에 등록되어 있다.
- [ ] 가구가 `Funiture.asset`에 등록되어 있다.
- [ ] 벽의 Sibling/Null/Target 목록이 기준 벽과 일치한다.
- [ ] 문과 가구가 올바른 벽 Target 목록을 참조한다.

### 씬

- [ ] 벽이 가로 2행, 세로 2열로 배치되어 있다.
- [ ] 문 한 개가 동일 Door Tile 2셀로 조립되어 있다.
- [ ] 벽, 문, 가구가 같은 Wall Tilemap에 있다.
- [ ] 바닥이 Floor Tilemap에 있다.
- [ ] 개별 SpriteRenderer 오브젝트가 없다.
- [ ] 가구 Bounds와 벽·문 셀의 겹침이 0이다.
- [ ] 바닥이 Grid 셀 전체를 채운다.
- [ ] 가로, 세로, 내·외부 모서리, T자, 십자 연결을 확인했다.
- [ ] 문+벽, 가구+벽 접점을 확인했다.

### 생성 및 저장

- [ ] C# Editor 컴파일이 통과한다.
- [ ] 생성기 버전을 증가시켰다.
- [ ] 열린 씬과 닫힌 씬 양쪽에서 재생성이 가능하다.
- [ ] `SaveScene` 반환값을 확인한다.
- [ ] 재생성 후 Tilemap 셀 수와 대표 좌표를 검증한다.
- [ ] 사용자의 요청 없이 Tile Palette를 변경하지 않았다.

## 5. 현재 Hospital 구현 기준

현재 병원 테스트 구현의 기준 파일은 다음과 같다.

```text
Assets/_Project/Scenes/HospitalTest.unity
Assets/_Project/Editor/HospitalTestSceneGenerator.cs
Assets/_Project/Tiles/Assets_TIles/Wall/Hospital/Hospital_Wall.asset
Assets/_Project/Tiles/Assets_TIles/Door/Hospital/Hospital_GlassDoor.asset
Assets/_Project/Tiles/Assets_TIles/Floor/Hospital/Hospital_Floor.asset
Assets/_Project/Art/Sprite/Tilesets/Floor/Hospital_Floor_Seamless.png
Assets/_Project/Tiles/Assets_TIles/Funiture/Hospital/
```

Hospital 벽은 `Wall_CreamWallPaper`의 28규칙 토폴로지를, Hospital 문은 `Close_Door`의 8규칙 토폴로지를 기준으로 한다. 벽은 2셀 두께, 문은 2셀 조립, 바닥은 16×16 seamless 반복을 사용한다.

이 기준을 바꾸려면 먼저 Main 씬의 실제 타일 좌표와 기준 RuleTile을 다시 대조하고, 변경 이유와 검증 결과를 이 문서에 함께 갱신한다.
