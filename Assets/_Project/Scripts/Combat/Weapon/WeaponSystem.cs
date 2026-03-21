using UnityEngine;
using System.Collections;   
using System;

public class WeaponSystem : MonoBehaviour
{
    [Header("Melee Polish")] // [추가] 타격감 관련 변수
    [SerializeField] private TrailRenderer meleeTrail; // 검기 이펙트
    [SerializeField] private float hitStopDuration = 0.05f; // 역경직 시간


    [Header("Settings")]
    public WeaponData weaponData; // 데이터 파일 넣는 곳
    public Transform muzzlePoint; // 총구 위치 넣는 곳

    public event Action <int, int> OnAmmoChanged; // 현재 탄약, 최대 탄약
    public SpriteRenderer weaponRenderer; // 무기 스프라이트 렌더러 (이미지 변경용)

    private float nextFireTime;
    private int currentAmmo;
    private float currentSpread;
    private bool isReloading = false;
    

    
    public bool IsCurrentModeAuto { get; private set; } // 현재 발사 모드 저장 (외부 접근용) -> 연발단발 전환할때 쓰는건데 아직 안만듦

    public bool IsSwinging { get; private set; } // 공격 중인지 여부 (근접 무기 스윙 모션 체크, 외부 접근용)
    public bool IsAltSwing { get; private set; } // 휘두르기 모션 체크

    private void Awake()
    {
        
    }

    private void Start()
    {
        if (weaponData != null)
        {
            InitializeWeapon();
        }
    }

    public void InitializeWeapon()
    {
        currentAmmo = weaponData.maxAmmo; // 초기 탄약 설정
        currentSpread = weaponData.baseSpread; // 초기 탄 퍼짐 설정

        if (weaponData.weaponSprite != null)
        {
            weaponRenderer.sprite = weaponData.weaponSprite;
        }

        if (muzzlePoint != null)
        {
            muzzlePoint.localPosition = weaponData.muzzleOffset;
        }

        OnAmmoChanged?.Invoke(currentAmmo, weaponData.maxAmmo);
        IsCurrentModeAuto = weaponData.isAutomatic; // 초기화 시 발사모드 가져오기
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
        if (weaponData.type != WeaponType.Melee)
        {
            if (currentAmmo <= 0)
            {
                StartCoroutine(Reload());
                return;
            }
        }

        // 연사 속도 체크 (쿨타임)
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + weaponData.fireRate;
        Attack();
    }

    private void Attack()
    {

        // 타입에 따른 분기
        switch (weaponData.type)
        {
            case WeaponType.Gun:
                FireGun();
                break;
            case WeaponType.Melee:
                StartCoroutine(SwingMelee());
                break;
            case WeaponType.Throwable:
                ThrowGrenade();
                break;
        }
    }

    private void FireGun()
    {
        currentAmmo--; // 탄약소모
        OnAmmoChanged?.Invoke(currentAmmo, weaponData.maxAmmo);
        //Debug.Log($"남은 탄약: {currentAmmo} / {weaponData.maxAmmo}"); // 탄약 상태 출력
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
                projectile.Initialize(weaponData.damage, weaponData.targetLayers, weaponData.projectileSpeed, weaponData.bulletLifeTime);
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
        if (weaponData.type == WeaponType.Melee) yield break; // 근접 무기는 재장전 없음

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

    // 근접 공격 로직
    private IEnumerator SwingMelee()
    {
        IsSwinging = true;
        IsAltSwing = !IsAltSwing;

        if (meleeTrail != null) meleeTrail.emitting = true;

        // 1. 소리 & 카메라 쉐이크
        NoiseManager.MakeNoise(transform.position, weaponData.noiseRange);
        if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.05f, 0.1f);

        // 무기가 45도 들려있든 말든, 판정은 마우스 쪽으로 부채꼴을 그려야 함
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
        mousePos.z = 0;
        Vector2 aimDir = (mousePos - transform.position).normalized; // 플레이어 -> 마우스 방향

        // 2. [시각 효과] 휘두르는 이펙트 생성 (칼 자체가 돌아가는 것보다 훨씬 타격감 좋음)
        // 이펙트가 "슉!" 하고 나타났다 사라짐
        GameObject prefabToSpawn = weaponData.projectilePrefab; // 기본은 1타 이펙트
        
        // 만약 2타(역방향) 차례이고, 데이터에 2타용 프리팹이 들어있다면? 그것으로 교체!
        if (!IsAltSwing && weaponData.altProjectilePrefab != null)
        {
            prefabToSpawn = weaponData.altProjectilePrefab;
        }

        // 결정된 프리팹(prefabToSpawn)으로 이펙트 생성!
        if (prefabToSpawn != null) 
        {
            // 정직하게 마우스 방향으로 각도 세팅
            float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            Quaternion slashRotation = Quaternion.Euler(0, 0, angle);

            // 복잡한 계산 없이 그냥 muzzlePoint.position에서 생성!
            GameObject slashObj = Instantiate(prefabToSpawn, muzzlePoint.position, slashRotation);
            
            // 검기 크기 및 왼쪽 볼 때 상하(Y) 반전
            Vector3 finalScale = weaponData.spriteScale;
            if (aimDir.x < 0)
            {
                finalScale.y *= -1f; 
            }
            slashObj.transform.localScale = finalScale;        
        }
        // 3. [딜레이] 칼을 휘두르는 모션 시간만큼 잠깐 대기 (0.1초)
        // 이 시간이 있어야 "휘두르고 -> 맞았다" 느낌이 남
        yield return new WaitForSeconds(0.1f); 

        

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(muzzlePoint.position, weaponData.attackRadius, weaponData.targetLayers);

        bool hasHit = false; // 적중 여부 체크용 플래그

        foreach (var hit in hitColliders)
        {
            Vector2 dirToTarget = (hit.transform.position - transform.position).normalized;

            // [핵심 변경] muzzlePoint.right 대신 aimDir(마우스 방향) 사용
            if (Vector2.Angle(aimDir, dirToTarget) < weaponData.attackArc / 2)
            {
                IDamageable target = hit.GetComponent<IDamageable>();
                if (target != null)
                {
                    // 넉백도 마우스 방향으로
                    target.TakeDamage(weaponData.damage, hit.transform.position, aimDir);
                    hasHit = true; // 적중 성공!
                }
            }
        }

        if (hasHit)
        {
            // 0.05초 동안 게임이 멈춤 (타격감 극대화)
            StartCoroutine(HitStopRoutine(0.05f)); 
        }
        if (meleeTrail != null) meleeTrail.emitting = false;

        IsSwinging = false;
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.05f; // 시간을 거의 멈춤 (0.0으로 하면 애니메이션이 아예 끊길 수 있어 0.05 추천)
        
        // TimeScale이 0에 가깝기 때문에 WaitForSecondsRealtime을 써야 합니다!
        yield return new WaitForSecondsRealtime(duration); 
        
        Time.timeScale = 1f; // 시간 원래대로 복구
    }
    
    // 기즈모로 공격 범위 확인 (디버그용)
    private void OnDrawGizmosSelected()
    {
        if (weaponData == null) return;

        // 1. 근접 무기 (부채꼴)
        if (weaponData.type == WeaponType.Melee)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, weaponData.attackRadius);

            Vector3 aimDir = Vector3.right;

            // [핵심 수정] 플레이 중일 때
            if (Application.isPlaying && Camera.main != null && UnityEngine.InputSystem.Mouse.current != null)
            {
                // 1. 스크린 좌표 가져오기
                Vector2 screenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                
                // 2. 월드 좌표로 변환 (중요: Z 거리를 카메라와 플레이어 차이만큼 줌)
                // 이렇게 하면 정확히 플레이어가 서 있는 Z=0 평면상의 좌표를 얻습니다.
                float distanceToScreen = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distanceToScreen));
                
                // 3. Z축 완전 제거 (Vector2로 계산)
                // 여기서 Z값이 섞이는 것을 원천 차단합니다.
                Vector2 playerPos2D = new Vector2(transform.position.x, transform.position.y);
                Vector2 worldPos2D = new Vector2(worldPos.x, worldPos.y);
                Vector2 dir2D = (worldPos2D - playerPos2D).normalized;

                // 4. 다시 Vector3로 복구
                aimDir = new Vector3(dir2D.x, dir2D.y, 0);
            }
            else
            {
                // 에디터 모드일 때
                if (muzzlePoint != null) aimDir = muzzlePoint.right;
            }

            // 부채꼴 그리기
            Quaternion leftRot = Quaternion.Euler(0, 0, weaponData.attackArc / 2);
            Quaternion rightRot = Quaternion.Euler(0, 0, -weaponData.attackArc / 2);

            Vector3 leftDir = leftRot * aimDir;
            Vector3 rightDir = rightRot * aimDir;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + leftDir * weaponData.attackRadius);
            Gizmos.DrawLine(transform.position, transform.position + rightDir * weaponData.attackRadius);
        }
        // 2. 투척 무기 (사거리 표시)
        else if (weaponData.type == WeaponType.Throwable)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, weaponData.maxRange);
        }
    }

    // 투척 공격 로직
    private void ThrowGrenade()
    {
        if (weaponData.projectilePrefab == null) return;

        // 1. 목표 지점 계산 (핵심)
        // 마우스 위치 (월드 좌표)
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
        mousePos.z = 0;

        // 내 위치에서 마우스까지의 벡터
        Vector3 direction = mousePos - muzzlePoint.position;
        float distance = direction.magnitude;

        // 사거리 제한 (데이터에 range가 설정되어 있다고 가정)
        // 만약 마우스가 사거리보다 멀면, 사거리 끝지점으로 보정
        float actualDistance = Mathf.Min(distance, weaponData.maxRange);
        Vector3 targetPos = muzzlePoint.position + (direction.normalized * actualDistance);

        // 2. 수류탄 생성
        GameObject grenadeObj = Instantiate(weaponData.projectilePrefab, muzzlePoint.position, Quaternion.identity);
        grenadeObj.transform.localScale = weaponData.spriteScale;
        Grenade grenade = grenadeObj.GetComponent<Grenade>();

        if (grenade != null)
        {
            // [수정] WeaponData의 값을 넘겨줌
            grenade.Initialize(
                weaponData.damage,              // 데미지
                weaponData.explosionRadius,     // 폭발 반경
                weaponData.grenadeFuseTime,     // 퓨즈 시간 (2초)
                weaponData.targetLayers,        // 타겟 레이어
                targetPos,                      // 목표 지점
                weaponData.grenadeArcHeight,    // 곡사 높이 (2.0f)
                weaponData.explodeOnArrival     // 즉시 폭발 여부 (false)
            );
        }
    }

    // 모드 전환 함수 -> 연발단발 전환할때 쓰는건데 아직 안만듦
    public void ToggleFireMode()
    {
        IsCurrentModeAuto = !IsCurrentModeAuto;
    }
}