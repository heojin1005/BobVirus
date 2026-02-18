using UnityEngine;

public enum WeaponType
{
    Gun,
    Melee,
    Throwable
}

[CreateAssetMenu(fileName = "New Weapon", menuName = "Combat/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Baisc Info")]
    public WeaponType type;            // 무기 타입
    public string weaponName = "Weapon";
    public GameObject projectilePrefab; // 총알, 근접 이펙트, 수류탄 등
    public LayerMask targetLayers;      // 타격 가능한 레이어

    [Header("Combat Stats")]
    public float damage = 10f;          // 데미지
    public float fireRate = 0.5f;       // 공격 속도 (연사 속도 or 휘두르는 속도)
    public float noiseRange = 15f;      // 소음 반경
    public float maxRange = 10f;        // 사거리 (총: 탄 소멸 거리, 수류탄: 투척 최대 거리)

    [Header("Visuals & Positioning")]
    public Sprite weaponSprite;         // 무기 이미지
    public Vector3 spriteScale = Vector3.one;  // 무기 이미지 크기 조절
    public Vector3 holdPosOffset = new Vector3(0.5f, -0.2f, 0);  // 몸에서 얼마나 떨어트릴지 (X, Y)
    public float holdAngleOffset = 0f;  // 마우스 방향에서 몇 도 꺾을지
    public Vector2 muzzleOffset;        // 총구 위치 (총, 수류탄 나가는 기준 위치)

    [Header("Gun Specifics")]
    public bool isAutomatic = true;     // 연사 여부
    public int maxAmmo = 30;            // 장탄수
    public float reloadTime = 2.0f;     // 재장전 시간
    public float projectileSpeed = 20f; // 탄속 (수류탄의 경우 던지는 힘 throwForce로 사용 가능)
    public float bulletLifeTime = 5f;   // (옵션) 탄 지속 시간
    
    [Header("Spread (Gun Only)")]
    public float baseSpread = 0.5f;     // 기본 탄 퍼짐
    public float maxSpread = 5.0f;      // 최대 탄 퍼짐
    public float spreadPerShot = 1.0f;  // 한 발 쏠 때마다 퍼짐 증가량
    public float spreadRecovery = 2.5f; // 퍼짐 회복 속도 (총 안 쏘고 있을 때 초당 퍼짐 감소량)

    [Header("Melee Specifics")]
    public float attackRadius = 1.5f;   // 판정 원 크기
    public float attackArc = 120f;      // 판정 부채꼴 각도

    [Header("Throwable Specifics")]
    // projectileSpeed를 throwForce로 같이 써도 되지만, 헷갈리면 따로 둬도 됨
    public float throwForce = 15f;      // 던지는 힘
    public float grenadeArcHeight = 2f; // 곡사 높이
    public float explosionRadius = 5f;  // 폭발 반경
    public bool explodeOnArrival = false; // 즉발 여부
    public float grenadeFuseTime = 3f;  // 폭발 시간
}