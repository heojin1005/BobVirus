using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "2D/Tiles/AdvancedRuleTile")]
public class AdvancedRuleTile : RuleTile<AdvancedRuleTile.Neighbor>
{
    [Header("my sibling tile")]
    public List<TileBase> siblingTiles;

    [Header("Target tile")]
    public List<TileBase> targetTile1;
    public List<TileBase> targetTile2;
    public List<TileBase> targetTile3;

    public class Neighbor : RuleTile.TilingRule.Neighbor
    {
        // 0: Ignore, 1: This, 2: NotThis 는 기본 정의되어 있음
        public const int Specific1 = 3;    // 내가 지정한 특정 타일인가?
        public const int Specific2 = 4;    // 내가 지정한 특정 타일인가?
        public const int Specific3 = 5;    // 내가 지정한 특정 타일인가?
    }

    public override bool RuleMatch(int neighbor, TileBase tile)
    {
        switch (neighbor)
        {
            case Neighbor.This:
                return tile == this || siblingTiles.Contains(tile);
            case Neighbor.NotThis:
                return tile != this && !siblingTiles.Contains(tile);
            case Neighbor.Specific1:
                // 지정한 타일이 맞는지 확인
                return targetTile1.Contains(tile);
            case Neighbor.Specific2:
                // 지정한 타일이 맞는지 확인
                return targetTile2.Contains(tile);
            case Neighbor.Specific3:
                // 지정한 타일이 맞는지 확인
                return targetTile3.Contains(tile);
        }
        return base.RuleMatch(neighbor, tile);
    }
}