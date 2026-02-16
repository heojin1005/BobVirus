using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class DoorTileManager : MonoBehaviour
{
    [SerializeField] private Tilemap doorTilemap;

    [SerializeField] private TileBase closedRuleTile;
    [SerializeField] private TileBase openRuleTile;

    [SerializeField] private TileBase openingAnimTile;
    [SerializeField] private TileBase closingAnimTile;

    [SerializeField] private float animDuration = 0.4f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPos = doorTilemap.WorldToCell(mouseWorldPos);

            TileBase clickedTile = doorTilemap.GetTile(cellPos);

            // 1. 클릭한 타일이 문 세트의 일부인지 확인
            if (clickedTile == closedRuleTile)
            {
                // 인접한 문 타일들을 모두 찾아서 같이 연다
                List<Vector3Int> doorParts = GetConnectedDoorTiles(cellPos, closedRuleTile);
                StartCoroutine(DoorRoutine(doorParts, openingAnimTile, openRuleTile));
            }
            else if (clickedTile == openRuleTile)
            {
                // 인접한 문 타일들을 모두 찾아서 같이 닫는다
                List<Vector3Int> doorParts = GetConnectedDoorTiles(cellPos, openRuleTile);
                StartCoroutine(DoorRoutine(doorParts, closingAnimTile, closedRuleTile));
            }
        }
    }

    // 공용 루틴: 여러 개의 좌표를 동시에 애니메이션 시키고 최종 타일로 교체
    IEnumerator DoorRoutine(List<Vector3Int> positions, TileBase animTile, TileBase finalTile)
    {
        // 모든 부위 애니메이션 시작
        foreach (var pos in positions)
        {
            doorTilemap.SetTile(pos, animTile);
        }

        yield return new WaitForSeconds(animDuration);

        // 모든 부위 최종 상태로 교체
        foreach (var pos in positions)
        {
            doorTilemap.SetTile(pos, finalTile);
        }
    }

    // 클릭한 타일과 연결된(상하좌우) 같은 종류의 문 타일을 찾는 함수
    private List<Vector3Int> GetConnectedDoorTiles(Vector3Int startPos, TileBase targetTile)
    {
        List<Vector3Int> connectedTiles = new List<Vector3Int> { startPos };

        // 문이 2칸 세트이므로 상하좌우 한 칸씩만 검사해도 충분합니다.
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