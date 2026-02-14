using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Combat/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Info")]
    public string weaponName = "Test Rifle";
    public float damage = 10f;
    public int maxAmmo = 30;     // 최대 탄창 크기
    public float reloadTime = 2.0f;  // 재장전 시간
    public LayerMask targetLayers; // 맞출 수 있는 레이어

    [Header("Projectile")]
    public GameObject projectilePrefab; // 발사할 총알 프리팹
    public float bulletSpeed = 20f;     // 탄속 (빠를수록 히트스캔에 가까워짐)
    public float bulletLifeTime = 5f; // 총알이 사라지기 전 최대 생존 시간 (성능 관리용 or 사거리 제한용)

    [Header("Fire Mode")]
    public bool isAutomatic = true; // true면 연사, false면 단발
    public float fireRate = 0.1f;  // 연사 속도 (0.1초마다 발사)
    public float noiseRange = 15f; // 발사 시 발생하는 소음의 반경



    [Header("Spread")]
    public float baseSpread = 0.5f; // 기본 탄퍼짐 
    public float maxSpread = 5.0f;  // 최대 탄퍼짐
    public float spreadPerShot = 1.0f; // 한 발 쏠 때마다 퍼짐 증가량
    public float spreadRecovery = 2.5f; // 초당 퍼짐 회복량
    
    // 산탄총(Shotgun)의 경우 pellets(탄환 수) 변수를 추가하여 for문으로 여러 발 발사하면 됩니다.

    [Header("Visuals")]
    public Vector2 muzzleOffset;
}