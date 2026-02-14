using UnityEngine;
using System.Collections;   
using System;

public class WeaponSystem : MonoBehaviour
{
    [Header("Settings")]
    public WeaponData weaponData; // 데이터 파일 넣는 곳
    public Transform muzzlePoint; // 총구 위치 넣는 곳

    public event Action <int, int> OnAmmoChanged; // 현재 탄약, 최대 탄약

    private float nextFireTime;
    private int currentAmmo;
    private float currentSpread;
    private bool isReloading = false;


    private void Awake()
    {
        
    }

    private void Start()
    {
        if (weaponData != null)
        {
            currentAmmo = weaponData.maxAmmo; // 초기 탄약 설정
            currentSpread = weaponData.baseSpread; // 초기 탄 퍼짐 설정
            OnAmmoChanged?.Invoke(currentAmmo, weaponData.maxAmmo);

            if (muzzlePoint != null)
            {
                muzzlePoint.localPosition = weaponData.muzzleOffset;
            }
        }
    
    }

    public void Update()
    {
        // 사격을 멈추면 탄 퍼짐이 서서히 회복됨
        if (weaponData != null)
        {
            // Lerp를 사용하여 부드럽게 기본 값으로 돌아감
            if (Time.time >= nextFireTime) // 사격 쿨타임이 끝난 후에만 회복 시작
            {
                currentSpread = Mathf.Lerp(currentSpread, weaponData.baseSpread, Time.deltaTime * weaponData.spreadRecovery);
            }
            
        }
    } 

    public void TryFire()
    {
        if (isReloading) return; // 재장전 중이면 발사 불가

        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        // 연사 속도 체크 (쿨타임)
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + weaponData.fireRate;
        Shoot();
    }

    private void Shoot()
    {
        currentAmmo--; // 탄약소모
        //Debug.Log($"남은 탄약: {currentAmmo} / {weaponData.maxAmmo}"); // 탄약 상태 출력

        OnAmmoChanged?.Invoke(currentAmmo, weaponData.maxAmmo);

        NoiseManager.MakeNoise(transform.position, weaponData.noiseRange); // 소음 발생 알림

        // 카메라 쉐이크 호출 (0.1초 동안 0.2의 강도)
        if (CameraFollow.Instance != null) 
            CameraFollow.Instance.Shake(0.1f, 0.2f);


        // 1. 탄 퍼짐 각도 계산
        float randomSpreadAngle = UnityEngine.Random.Range(-currentSpread, currentSpread);
        Quaternion spreadRotation = Quaternion.Euler(0, 0, randomSpreadAngle);

        // 2. 최종 발사 각도 (총구 각도 + 탄퍼짐)
        // muzzlePoint.rotation에 spreadRotation을 더해줍니다.
        Quaternion finalRotation = muzzlePoint.rotation * spreadRotation;

        // 투사체 생성
        if (weaponData.projectilePrefab != null)
        {
            GameObject bulletObj = Instantiate(weaponData.projectilePrefab, muzzlePoint.position, finalRotation);
            
            // 투사체 초기화 (데미지, 레이어, 속도 전달)
            Projectile projectile = bulletObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(weaponData.damage, weaponData.targetLayers, weaponData.bulletSpeed, weaponData.bulletLifeTime);
            }
        }
        else
        {
            Debug.LogError("총알 프리팹이 WeaponData에 할당되지 않았습니다!");
        }

        // 반동 적용
        currentSpread = Mathf.Clamp(currentSpread + weaponData.spreadPerShot, weaponData.baseSpread, weaponData.maxSpread);
    }

    public IEnumerator Reload()
    {
        if (isReloading || currentAmmo == weaponData.maxAmmo)
        yield break; // 이미 재장전 중이거나 탄약이 가득 찼으면 무시
                 
        //Debug.Log("재장전 ...");   
        isReloading = true;

        yield return new WaitForSeconds(weaponData.reloadTime);

        currentAmmo = weaponData.maxAmmo;
        isReloading = false;
        //Debug.Log("재장전 완료!");
        currentSpread = weaponData.baseSpread; // 재장전 시 퍼짐 초기화

        OnAmmoChanged?.Invoke(currentAmmo, weaponData.maxAmmo);
    }


    public float GetCurrentSpread()
    {
        return currentSpread;
    }
}