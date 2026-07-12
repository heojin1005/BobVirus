using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "2D/Tiles/AdvancedRuleTile")]
public class AdvancedRuleTile : RuleTile<AdvancedRuleTile.Neighbor>
{
    [Header("my sibling tile")]
    [SerializeField] private List<TileBase> siblingTiles;
    [SerializeField] private TileList siblingTileList;
    [SerializeField] private List<TileBase> nullTiles;
    [SerializeField] private TileList nullTileList;

    [Header("Target tile")]
    [SerializeField] private List<TileBase> targetTile1;
    [SerializeField] private TileList targetList1;
    [SerializeField] private List<TileBase> targetTile2;
    [SerializeField] private TileList targetList2;
    [SerializeField] private List<TileBase> targetTile3;
    [SerializeField] private TileList targetList3;

    public class Neighbor : RuleTile.TilingRule.Neighbor
    {
        // 0: Ignore, 1: This, 2: NotThis 는 기본 정의되어 있음
        public const int Target1 = 3;
        public const int Target2 = 4;
        public const int Target3 = 5;
    }

    public override bool RuleMatch(int neighbor, TileBase tile)
    {
        switch (neighbor)
        {
            case Neighbor.This:
                return tile == this || siblingTiles.Contains(tile) || (siblingTileList != null && siblingTileList.Tiles.Contains(tile));
            case Neighbor.NotThis:
                return tile == null || nullTiles.Contains(tile) || (nullTileList != null && nullTileList.Tiles.Contains(tile));
            case Neighbor.Target1:
                return targetTile1.Contains(tile) || (targetList1 != null && targetList1.Tiles.Contains(tile));
            case Neighbor.Target2:
                return targetTile2.Contains(tile) || (targetList2 != null && targetList2.Tiles.Contains(tile));
            case Neighbor.Target3:
                return targetTile3.Contains(tile) || (targetList3 != null && targetList3.Tiles.Contains(tile));
        }
        return base.RuleMatch(neighbor, tile);
    }
}