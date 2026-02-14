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

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (mainCam == null) mainCam = Camera.main;
    }

    // Input System: Move (WASD)
    public void OnMove(InputValue value)
    {
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

        // [수정] 매 프레임 현재 마우스의 화면 좌표를 가져와서 월드 좌표로 변환
        // Mouse.current가 null이 아닐 때만 실행
        if (Mouse.current == null) return;
    
        Vector2 screenPos = Mouse.current.position.ReadValue();
        mousePos = mainCam.ScreenToWorldPoint(screenPos);

        // 1. 마우스 방향 및 각도 계산
        Vector2 lookDir = (mousePos - (Vector2)weaponPivot.position).normalized;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;

        // 2. 무기 피벗 회전 (Z축 기준)
        weaponPivot.rotation = Quaternion.Euler(0, 0, angle);

        // 3. 무기 뒤집기 (Y축 스케일 조정)
        Vector3 scale = weaponPivot.localScale; // 무기의 현재 크기(Scale)를 가져옴

        // 각도의 절댓값이 90보다 크면(왼쪽을 보고 있으면)
        if (Mathf.Abs(angle) > 90)
        {
            // Y축을 -1로 만들어서 뒤집음 (단, 기존 크기 비율은 유지)
            scale.y = -1f * Mathf.Abs(scale.y); 
        }
        else
        {
            // 오른쪽을 보면 원래대로 돌림
            scale.y = 1f * Mathf.Abs(scale.y);
        }

        // 변경된 크기 적용
        weaponPivot.localScale = scale;

        // 4. 몸통 좌우 반전
        if (bodySprite != null)
        {
            bodySprite.flipX = mousePos.x < transform.position.x;
        }

        if (weaponRenderer != null && bodySprite != null)
        {
            if (bodySprite.flipX)
            {
                weaponRenderer.sortingOrder = bodySprite.sortingOrder - 1; // 몸통 뒤로
            }
            else
            {
                weaponRenderer.sortingOrder = bodySprite.sortingOrder + 1; // 몸통 앞으로
            }
            
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