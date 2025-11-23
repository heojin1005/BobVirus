using UnityEngine;
using UnityEngine.Tilemaps;

public class CityGenerator : MonoBehaviour
{
    [Header("맵 설정")]
    public int cityWidth = 5;  // 가로 방향으로 들어갈 구역(건물)의 수
    public int cityHeight = 5; // 세로 방향으로 들어갈 구역(건물)의 수

    [Header("구역 및 도로 크기 설정 (타일 단위)")]
    public int plotSize = 8;     // 건물 하나가 차지할 공간의 크기 (기본: 8x8 타일)
    public int roadWidth = 2;    // 도로의 폭 (기본: 2칸)

    [Header("오브젝트 및 타일 설정")]
    public Tilemap roadTilemap;        // 도로를 그릴 타일맵 (인스펙터에서 연결)
    public TileBase roadTile;          // 도로에 사용할 타일 (인스펙터에서 연결)
    public GameObject[] buildingPrefabs; // 랜덤으로 배치할 건물 프리팹 배열 (인스펙터에서 연결)
    public Transform buildingParent;   // 생성된 건물을 정리할 부모 오브젝트 (옵션)

    void Start()
    {
        GenerateCity();
    }

    void GenerateCity()
    {
        // 1단계: 도시의 뼈대가 되는 도로망 깔기
        DrawRoads();

        // 2단계: 도로 사이의 빈 구역에 건물 배치하기
        PlaceBuildings();
    }

    // 1. 도로망을 그리는 함수
    void DrawRoads()
    {
        // 전체 맵의 타일 크기 계산
        int totalWidth = cityWidth * (plotSize + roadWidth);
        int totalHeight = cityHeight * (plotSize + roadWidth);

        // 맵 전체를 순회하며 도로 타일 찍기
        for (int x = -roadWidth; x < totalWidth; x++)
        {
            for (int y = -roadWidth; y < totalHeight; y++)
            {
                // 현재 위치 (x, y)가 도로에 해당하는지 판별
                // (plotSize + roadWidth) 크기의 블록을 기준으로, plotSize를 넘어가는 부분이 도로가 됩니다.
                bool isInternalRoadX = x >= 0 && (x % (plotSize + roadWidth) >= plotSize);
                bool isInternalRoadY = y >= 0 && (y % (plotSize + roadWidth) >= plotSize);

                // 가로 도로이거나 세로 도로이면
                if (x < 0 || y < 0 || isInternalRoadX || isInternalRoadY)
                {
                    // 해당 위치에 도로 타일 그리기
                    roadTilemap.SetTile(new Vector3Int(x, y, 0), roadTile);
                }
            }
        }
    }

    // 2. 건물을 배치하는 함수
    void PlaceBuildings()
    {
        // 각 구역(plot)을 순회
        for (int x = 0; x < cityWidth; x++)
        {
            for (int y = 0; y < cityHeight; y++)
            {
                // 건물이 배치될 위치 계산 (각 구역의 왼쪽 아래 모서리)
                float posX = roadWidth + x * (plotSize + roadWidth); 
                float posY = roadWidth + y * (plotSize + roadWidth);
                Vector3 position = new Vector3(posX, posY, 0);

                // 건물 프리팹 배열에서 무작위로 하나 선택
                int randomIndex = Random.Range(0, buildingPrefabs.Length);
                GameObject selectedBuilding = buildingPrefabs[randomIndex];

                // 선택된 건물을 계산된 위치에 생성
                GameObject buildingInstance = Instantiate(selectedBuilding, position, Quaternion.identity);

                // (옵션) 생성된 건물을 buildingParent 자식으로 넣어 하이어라키를 깔끔하게 정리
                if (buildingParent != null)
                {
                    buildingInstance.transform.parent = buildingParent;
                }
            }
        }
    }
}