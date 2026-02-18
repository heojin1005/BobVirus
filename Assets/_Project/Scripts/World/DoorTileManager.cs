using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
// 반드시 이 네임스페이스가 있어야 합니다!
using UnityEngine.InputSystem;

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
        // Mouse.current를 사용하는 것이 새로운 Input System의 방식입니다.
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // 1. 마우스의 스크린 좌표를 가져옵니다.
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

            // 2. 카메라를 통해 월드 좌표로 변환합니다. (Z축 보정 필수)
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, -Camera.main.transform.position.z));

            // 3. 타일맵 좌표로 변환합니다.
            Vector3Int cellPos = doorTilemap.WorldToCell(mouseWorldPos);
            TileBase clickedTile = doorTilemap.GetTile(cellPos);

            // 문 열기/닫기 로직
            if (clickedTile == closedRuleTile)
            {
                List<Vector3Int> doorParts = GetConnectedDoorTiles(cellPos, closedRuleTile);
                StartCoroutine(DoorRoutine(doorParts, openingAnimTile, openRuleTile));
            }
            else if (clickedTile == openRuleTile)
            {
                List<Vector3Int> doorParts = GetConnectedDoorTiles(cellPos, openRuleTile);
                StartCoroutine(DoorRoutine(doorParts, closingAnimTile, closedRuleTile));
            }
        }
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