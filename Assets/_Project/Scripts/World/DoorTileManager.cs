using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using NavMeshPlus.Components;

public class DoorTileManager : MonoBehaviour
{
    public static DoorTileManager Instance { get; private set; }

    [SerializeField] private Tilemap doorTilemap;

    [SerializeField] private TileBase closedRuleTile;
    [SerializeField] private TileBase openRuleTile;

    [SerializeField] private TileBase openingAnimTile;
    [SerializeField] private TileBase closingAnimTile;

    [SerializeField] private float animDuration = 0.4f;
    [SerializeField] private NavMeshSurface surface2D;


    private void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }


    public bool TryToggleNearbyDoor(Vector3 centerPos, float radius)
    {
        Vector3Int centerCell = doorTilemap.WorldToCell(centerPos);
        int cellRadius = Mathf.CeilToInt(radius); // 1.5m 반경이면 주변 2칸 정도를 탐색

        float minDistance = float.MaxValue;
        Vector3Int closestDoorCell = Vector3Int.zero;
        TileBase closestDoorTile = null;
        bool foundDoor = false;

        // 플레이어 주변 타일(격자)을 모두 스캔합니다.
        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                Vector3Int cellPos = centerCell + new Vector3Int(x, y, 0);
                TileBase tile = doorTilemap.GetTile(cellPos);

                // 해당 타일이 문(열림/닫힘)이라면?
                if (tile == closedRuleTile || tile == openRuleTile)
                {
                    // 타일의 실제 월드 중심 좌표를 가져와 플레이어와의 거리를 잽니다.
                    Vector3 cellWorldPos = doorTilemap.GetCellCenterWorld(cellPos);
                    float dist = Vector2.Distance(centerPos, cellWorldPos);

                    // 반경 안에 들어오고, 지금까지 찾은 문보다 더 가깝다면 갱신
                    if (dist <= radius && dist < minDistance)
                    {
                        minDistance = dist;
                        closestDoorCell = cellPos;
                        closestDoorTile = tile;
                        foundDoor = true;
                    }
                }
            }
        }

        // 가장 가까운 문을 찾았다면 작동시킵니다.
        if (foundDoor)
        {
            if (closestDoorTile == closedRuleTile)
            {
                List<Vector3Int> doorParts = GetConnectedDoorTiles(closestDoorCell, closedRuleTile);
                StartCoroutine(DoorRoutine(doorParts, openingAnimTile, openRuleTile));
                return true;
            }
            else if (closestDoorTile == openRuleTile)
            {
                List<Vector3Int> doorParts = GetConnectedDoorTiles(closestDoorCell, openRuleTile);
                StartCoroutine(DoorRoutine(doorParts, closingAnimTile, closedRuleTile));
                return true;
            }
        }

        return false; // 주변에 문이 없음
    }

    IEnumerator DoorRoutine(List<Vector3Int> positions, TileBase animTile, TileBase finalTile)
    {
        foreach (var pos in positions)
        {
            doorTilemap.SetTile(pos, animTile);
        }

        yield return new WaitForSeconds(animDuration);

        foreach (var pos in positions)
        {
            doorTilemap.SetTile(pos, finalTile);
        }
        yield return new WaitForEndOfFrame(); // 타일맵이 완전히 갱신된 후 NavMesh를 빌드하도록 보장

        if (surface2D != null)
        {
            surface2D.BuildNavMesh(); // MavMesh 갱신
        }
    }

    private List<Vector3Int> GetConnectedDoorTiles(Vector3Int startPos, TileBase targetTile)
    {
        List<Vector3Int> connectedTiles = new List<Vector3Int> { startPos };
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        foreach (var dir in directions)
        {
            Vector3Int neighborPos = startPos + dir;
            if (doorTilemap.GetTile(neighborPos) == targetTile)
            {
                connectedTiles.Add(neighborPos);
            }
        }

        return connectedTiles;
    }
}