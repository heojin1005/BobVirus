using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "2D/Tiles/Advanced Pattern Rule Tile")]
public class AdvancedPatternRuleTile : AdvancedRuleTile // 기본 RuleTile 대신 직접 만든 클래스를 상속!
{
    [Header("패턴 설정")]
    public int moduloX = 1;
    public int moduloY = 1;
    public Sprite[] patternSprites;

    [Tooltip("이 스프라이트가 매칭되었을 때만 패턴으로 변환합니다. (비워두면 조건 없이 모두 덮어씌움)")]
    public Sprite defaultFillSprite;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        // 1. 부모(AdvancedRuleTile)의 로직을 먼저 실행합니다. 
        // 여기서 Target1, 2, 3 및 Sibling 조건에 따른 타일 매칭이 전부 계산됩니다.
        base.GetTileData(position, tilemap, ref tileData);

        if (patternSprites == null || patternSprites.Length == 0) return;

        // 2. [핵심] 테두리(코너 등)는 AdvancedRuleTile의 결과를 유지하고, 
        // 꽉 찬 내부 타일(Fill)에만 패턴을 넣기 위한 예외 처리입니다.
        if (defaultFillSprite != null && tileData.sprite != defaultFillSprite)
        {
            return; // 매칭된 스프라이트가 기본 스프라이트가 아니라면, 패턴을 덮어씌우지 않고 부모의 결과를 씁니다.
        }

        // 3. 패턴 계산 및 덮어씌우기
        int targetX = (position.x % moduloX + moduloX) % moduloX;
        int index = targetX % patternSprites.Length;

        if (patternSprites[index] != null)
        {
            tileData.sprite = patternSprites[index];
        }
    }
}