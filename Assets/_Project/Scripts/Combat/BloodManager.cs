using UnityEngine;

public class BloodManager : MonoBehaviour
{
    public static BloodManager Instance { get; private set; }

    [Header("Blood Settings")]
    [SerializeField] private GameObject bloodParticlePrefab; // 피 튀기는 파티클
    [SerializeField] private GameObject[] bloodDecalPrefabs; // 바닥에 남을 핏자국 스프라이트들 배열
    [SerializeField] private Transform decalContainer;       // 하이어라키 정리용 부모 객체

    private void Awake()
    {
        Instance = this;
    }

    // 피격 위치(hitPoint)와 피격 방향(hitNormal: 맞은 반대 방향)을 받아 실행
    public void SpawnBlood(Vector2 hitPoint, Vector2 hitDir)
    {
        // 1. 피 파티클 생성 (방향에 맞춰 회전)
        if (bloodParticlePrefab != null)
        {
            float angle = Mathf.Atan2(hitDir.y, hitDir.x) * Mathf.Rad2Deg;
            Quaternion rot = Quaternion.Euler(0, 0, angle);
            GameObject particle = Instantiate(bloodParticlePrefab, hitPoint, rot);
            Destroy(particle, 1.5f); // 1.5초 뒤 파티클 삭제 (나중엔 풀링으로 변경)
        }

        // 2. 바닥 핏자국 데칼 생성
        if (bloodDecalPrefabs != null && bloodDecalPrefabs.Length > 0)
        {
            GameObject randomDecal = bloodDecalPrefabs[Random.Range(0, bloodDecalPrefabs.Length)];
            
            // 약간의 랜덤 오프셋과 회전을 줘서 자연스럽게
            Vector2 randomOffset = Random.insideUnitCircle * 0.3f;
            
            GameObject decal = Instantiate(randomDecal, hitPoint + randomOffset, Quaternion.identity, decalContainer);            
            // 데칼 레이어는 바닥(Ground) 타일맵 바로 위로 설정해야 함 (Sorting Layer 설정 필요)
            // 너무 많이 쌓이면 렉 걸리므로 10초 뒤 삭제 (최적화 뼈대)
            Destroy(decal, 10f); 
        }
    }
}