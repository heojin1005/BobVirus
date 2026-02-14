using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Combat/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "Test Rifle";
    public float damage = 10f;
    public float fireRate = 0.1f;  // 연사 속도 (0.1초마다 발사)
    public float range = 20f;      // 사거리
    public int maxAmmo = 30;     // 최대 탄창 크기
    public float reloadTime = 2.0f;  // 재장전 시간

    public float noiseRange = 15f; // 발사 시 발생하는 소음의 반경
    public LayerMask targetLayers; // 맞출 수 있는 레이어
}