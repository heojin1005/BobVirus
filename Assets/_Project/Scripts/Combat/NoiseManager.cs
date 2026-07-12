using UnityEngine;
using System;

public class NoiseManager : MonoBehaviour
{
    // 소리가 났을 때 구독자(좀비들)에게 알리는 이벤트
    // 파라미터: 소리 발생 위치, 소리 크기(반경), 소리의 강함
    public static event Action<Vector3, float, GameObject> OnNoiseGenerated;

    public static void MakeNoise(Vector3 position, float range, GameObject source = null)
    {
        // 1. 디버그용: 씬 뷰에 소리 범위를 잠깐 그려줌
        int segments = 20;
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            // 현재 각도와 다음 각도 계산
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            // 원 둘레의 점 좌표 계산 (Sin, Cos)
            Vector3 p1 = position + new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0) * range;
            Vector3 p2 = position + new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0) * range;

            // 선 긋기 (Cyan 색상, 1초 유지)
            Debug.DrawLine(p1, p2, Color.cyan, 1f);
        }

        // 2. 이벤트 발생! 듣고 있는 모든 좀비에게 알림
        OnNoiseGenerated?.Invoke(position, range, source);
    }
}