using UnityEngine;
using System.Collections;   
using System;

public class WeaponSystem : MonoBehaviour
{
    [Header("Settings")]
    public WeaponData weaponData; // 데이터 파일 넣는 곳
    public Transform muzzlePoint; // 총구 위치 넣는 곳

    [Header("Visuals")]
    [SerializeField] private LineRenderer lineRenderer; // 총알 궤적 그리기용

    public event Action <int, int> OnAmmoChanged; // 현재 탄약, 최대 탄약

    private float nextFireTime;
    private int currentAmmo;
    private bool isReloading = false;


    private void Awake()
    {
        // 라인 렌더러가 없으면 자동으로 추가해주는 안전장치
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.05f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.yellow;
            lineRenderer.endColor = Color.clear;
            lineRenderer.enabled = false;
        }
    }

    private void Start()
    {
        if (weaponData != null)
        {
            currentAmmo = weaponData.maxAmmo; // 초기 탄약 설정
            OnAmmoChanged?.Invoke(currentAmmo, weaponData.maxAmmo);
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

        // 총구 방향(오른쪽)으로 레이캐스트 발사
        Vector2 direction = muzzlePoint.right;
        RaycastHit2D hit = Physics2D.Raycast(muzzlePoint.position, direction, weaponData.range, weaponData.targetLayers);

        // 맞은 위치 계산 (맞은 게 없으면 최대 사거리 끝점)
        Vector2 targetPos = hit.collider != null ? hit.point : (Vector2)muzzlePoint.position + direction * weaponData.range;

        // 3. 시각 효과 (총알 궤적 0.05초간 표시)
        StartCoroutine(ShowTrail(targetPos));

        // 4. 로그 출력
        if (hit.collider != null)
        {
            //Debug.Log($"[명중] {hit.collider.name}을 맞췄습니다!");
            var target = hit.collider.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(weaponData.damage, hit.point, hit.normal);
            }
        }
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
        OnAmmoChanged?.Invoke(currentAmmo, weaponData.maxAmmo);
    }

    private IEnumerator ShowTrail(Vector2 targetPos)
    {
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, muzzlePoint.position);
        lineRenderer.SetPosition(1, targetPos);
        yield return new WaitForSeconds(0.05f); // 0.05초 뒤에 꺼짐
        lineRenderer.enabled = false;
    }
}