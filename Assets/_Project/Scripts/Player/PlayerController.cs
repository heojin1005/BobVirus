using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform weaponPivot;       // 무기가 회전할 축 (빈 오브젝트)
    [SerializeField] private SpriteRenderer bodySprite;   // 캐릭터 몸통 (좌우 반전용)
    [SerializeField] private Camera mainCam;
    [SerializeField] private WeaponSystem weaponSystem;
    [SerializeField] private SpriteRenderer weaponRenderer; // 무기 스프라이트 레이어 조정용

    [SerializeField] private float footstepNoiseRange = 3f; // 발소리 반경 3미터
    [SerializeField] private float stepInterval = 0.5f;     // 0.5초마다 소리 발생
    private float nextStepTime = 0f;
    private Vector2 moveInput;
    private Vector2 mousePos;
    private bool isFiring;

    private bool  isTriggerReady = true; // 단발 사격시 버튼을 뗐는지 체크용

    private bool isFacingRight = true; // 처음에 오른쪽 보는 걸로 시작

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (mainCam == null) mainCam = Camera.main;
    }

    // Input System: Move (WASD)
    public void OnMove(InputValue value)
    {
        //Debug.Log("<color=yellow>키보드 입력 들어옴!</color> 값: " + value.Get<Vector2>());
        moveInput = value.Get<Vector2>();
    }

    /* Input System: Look (Mouse Position)
       Input Action Map에서 Look 액션을 Value - Vector2 - Mouse Position으로 설정해야 함
    public void OnLook(InputValue value)
    {
        Vector2 screenPos = value.Get<Vector2>();
        mousePos = mainCam.ScreenToWorldPoint(screenPos);
    }*/

    public void OnAttack(InputValue value)
    {
        // Debug.Log($"클릭 입력 들어옴! 값: {value.isPressed}"); // 클릭 인식 되는지 디버그
        // 버튼을 누르면 true, 떼면 false가 됨
        isFiring = value.isPressed;
    }

    public void OnReload(InputValue value)
    {
        if (value.isPressed && weaponSystem != null)
        {
            StartCoroutine(weaponSystem.Reload());
        }
    }

    public void Update()
    {
        HandleAiming();
        HandleShooting();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        HandleFootsteps();
    }

    private void HandleMovement()
    {
        // MovePosition 대신 velocity 사용 -> 넉백/반동과 호환성 확보
        rb.linearVelocity = moveInput * moveSpeed;
    }

    private void HandleFootsteps()
    {
        if (rb == null) return;

        // 발소리 로직
        // 1. 실제로 움직이고 있는가? (속도가 0.1 이상)
        if (rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            // 2. 시간이 되었는가?
            if (Time.time >= nextStepTime)
            {
                // 소리 발생 (반경 3m)
                NoiseManager.MakeNoise(transform.position, footstepNoiseRange);
                nextStepTime = Time.time + stepInterval;
            }
        }
    }

    private void HandleAiming()
    {
        if (weaponPivot == null) return;
        if (mainCam == null) mainCam = Camera.main;

        // 1. 마우스 월드 좌표 갱신 (Z축 0으로 평면화)
        if (Mouse.current != null)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f)); // 카메라 Z거리 보정
            mousePos = new Vector2(worldPos.x, worldPos.y); 
        }

        // 2. [핵심] 떨림 방지 (Dead Zone Logic)
        // 마우스와 내 몸의 X축 거리 차이
        float xDiff = mousePos.x - transform.position.x;
        float deadZone = 0.05f; // 화면 가운데 작은 사각형 영역 (0.05는 화면의 5% 정도로 조절 가능)

        if (isFacingRight)
        {
            // 오른쪽 보는 중인데, 마우스가 왼쪽 데드존 밖으로 나갔다면? -> 왼쪽 보기
            if (xDiff < -deadZone) isFacingRight = false;
        }
        else
        {
            // 왼쪽 보는 중인데, 마우스가 오른쪽 데드존 밖으로 나갔다면? -> 오른쪽 보기
            if (xDiff > deadZone) isFacingRight = true;
        }
        
        // 이제부터 모든 로직은 isFacingRight 변수 하나만 믿고 갑니다.
        bool isLookingLeft = !isFacingRight;

        // 3. 몸통 스프라이트 반전
        if (bodySprite != null) bodySprite.flipX = isLookingLeft;


        // 4. 무기 회전 로직
        // 공통: 마우스 방향 각도 계산
        Vector2 lookDir = (mousePos - (Vector2)weaponPivot.position).normalized;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;

        if (weaponSystem == null || weaponSystem.weaponData == null)
        {
            weaponPivot.rotation = Quaternion.Euler(0, 0, angle);
            Vector3 pivotDirectionScale = Vector3.one;
            if (isLookingLeft) pivotDirectionScale.y = -1f;
            weaponPivot.localScale = pivotDirectionScale;
            return;
        }

        // 데이터에서 크기(Scale) 가져오기 (없으면 1,1,1)
        Vector3 baseScale = Vector3.one;
        if (weaponSystem.weaponData != null) baseScale = weaponSystem.weaponData.spriteScale;

        if (weaponSystem != null && weaponSystem.weaponData != null && 
            (weaponSystem.weaponData.type == WeaponType.Melee || weaponSystem.weaponData.type == WeaponType.Throwable))
        {
            float angleOffset = weaponSystem.weaponData.holdAngleOffset;
            Vector3 posOffset = weaponSystem.weaponData.holdPosOffset;

            // 1. [논리] 피벗(WeaponPivot)과 총구는 마우스를 향해 정상적으로 조준합니다.
            if (isFacingRight)
            {
                weaponPivot.rotation = Quaternion.Euler(0, 0, angle + angleOffset);
                weaponPivot.localPosition = new Vector3(posOffset.x, posOffset.y, 0);
                weaponPivot.localScale = Vector3.one; 
            }
            else
            {
                weaponPivot.rotation = Quaternion.Euler(0, 0, angle - angleOffset);
                weaponPivot.localPosition = new Vector3(-posOffset.x, posOffset.y, 0);
                weaponPivot.localScale = new Vector3(1, -1, 1); 
            }

            // 2. [시각] 스프라이트(방망이 이미지)만 210도 까딱까딱 돌려줍니다!
            if (weaponRenderer != null && weaponRenderer.transform.parent != null)
            {
                // 회전시킬 대상(SpritePivot)을 명확히 잡습니다.
                Transform spritePivot = weaponRenderer.transform.parent;

                if (weaponSystem.weaponData.type == WeaponType.Melee && weaponSystem.IsAltSwing)
                {
                    // 역방향: 부모를 -210도로 회전
                    spritePivot.localRotation = Quaternion.Euler(0, 0, -210f);
                }
                else
                {
                    // 정방향: 동일한 대상(부모)을 다시 0도로 복구!
                    spritePivot.localRotation = Quaternion.Euler(0, 0, 0f);
                }
            }
        }
        else 
        {
            // [총]
            weaponPivot.rotation = Quaternion.Euler(0, 0, angle);
            Vector3 posOffset = weaponSystem.weaponData.holdPosOffset;

            if (isFacingRight)
            {
                // 오른쪽: 설정된 오프셋 그대로
                weaponPivot.localPosition = new Vector3(posOffset.x, posOffset.y, 0);
            }
            else
            {
                // 왼쪽: X축 반전 (몸통 기준 대칭 이동)
                weaponPivot.localPosition = new Vector3(-posOffset.x, posOffset.y, 0);
            }

            // 3. 피벗 스케일: 방향(반전)만 담당
            Vector3 pivotDirectionScale = Vector3.one;
            if (isLookingLeft) pivotDirectionScale.y = -1f; // 왼쪽 볼 땐 Y반전으로 총이 뒤집히지 않게 함
            weaponPivot.localScale = pivotDirectionScale;
        }

        // 실제 무기 크기(baseScale)는 그림(Renderer)에 직접 적용!
        if (weaponRenderer != null)
        {
            weaponRenderer.transform.localScale = baseScale;
        }

            // 5. 레이어 정리
        if (weaponRenderer != null && bodySprite != null)
        {
            // 근접무기는 몸에 가려지게, 아니면 뒤에/앞에 상황따라
            bool isMelee = (weaponSystem.weaponData.type == WeaponType.Melee);
            weaponRenderer.sortingOrder = bodySprite.sortingOrder + (isMelee ? -1 : (isLookingLeft ? -1 : 1));
        }
    }

    private void HandleShooting()
    {
        if (weaponSystem == null) return;
        if (weaponSystem.weaponData == null) return;

        bool isAuto = weaponSystem.weaponData.isAutomatic;

        if (isFiring)
        {
            if (isAuto)
            {
                // 연사 모드: 버튼 누르고 있는 동안 계속 발사 시도
                weaponSystem.TryFire();
            }
            else
            {
                // 단발 모드: 버튼을 눌렀다가 뗄 때 한 번 발사
                if (isTriggerReady)
                {
                    weaponSystem.TryFire();
                    isTriggerReady = false; // 다음 발사를 위해 버튼을 뗄 때까지 기다림
                }
            }
        }
        else
        {
            // 버튼이 떼어졌을 때 단발 모드에서 다시 발사할 수 있도록 준비
            if (!isAuto)
            {
                isTriggerReady = true;
            }
        }
    }
}