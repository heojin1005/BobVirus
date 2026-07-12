using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyShooterAI : MonoBehaviour
{
    public enum State { Idle, Investigate, Chase, Combat, Return }
    
    [Header("State Info")]
    [SerializeField] private State currentState;

    [Header("Movement")]
    private NavMeshAgent agent;
    private Vector3 startPos;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4.5f;
    
    [Header("Combat / Shooting")]
    [SerializeField] private float shootingRange = 7f; 
    [SerializeField] private WeaponData weaponData; 
    [SerializeField] private LayerMask enemyTargetLayers; 
    [SerializeField] private Vector2 aimOffset = new Vector2(0, -0.2f); 
    
    [Header("Weapon References")]
    [SerializeField] private Transform muzzlePoint; 
    [SerializeField] private Transform weaponPivot; 
    [SerializeField] private SpriteRenderer weaponRenderer; 

    private float nextFireTime; 
    private int currentAmmo; 
    private bool isReloading = false;
    private Vector3 initialPivotPos; 
    private Vector3 startPosition; // 리턴 상태에서 돌아갈 위치
    private float investigateTimer = 0f; // 수색 대기 시간 측정용
    private Vector2 lastAimDir = Vector2.right; // 마지막 조준 방향 저장용

    [Header("Chase Settings")]
    [SerializeField] private float loseTargetTime = 4f; // 4초 동안 안 보이면 추격 포기 (에러에서 찾으신 이름)
    private float timeSinceLastSawTarget = 0f;          // 시야에서 사라진 시간 측정용 타이머

    [Header("Components")]
    [SerializeField] private EnemyPerception perception;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Transform target;
    private Vector3 noiseLocation;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        
        if (perception == null) perception = GetComponent<EnemyPerception>();

        startPos = transform.position;
        
        startPosition = transform.position; // 초기 위치 저장
    }

    private void OnEnable()
    {
     // 귀 열기
     NoiseManager.OnNoiseGenerated += OnHeardNoise;
    }

    private void OnDisable()
    {
       // 귀 닫기
       NoiseManager.OnNoiseGenerated -= OnHeardNoise;
    }

    // 슈터의 청각 처리
    private void OnHeardNoise(Vector3 noisePos, float range, GameObject source)
    {
        // 이미 싸우고 있거나 추격 중이면 주변 소음 무시
        if (currentState == State.Chase || currentState == State.Combat) return;

        // [핵심 추가] 소리를 낸 놈이 나랑 같은 'Shooter(아군)'라면 무시한다!
       if (source != null)
       {
           if (source.CompareTag("Shooter") || source.transform.root.CompareTag("Shooter")) return;
       }

      // 소리가 들리는 반경 안인지 체크
      if (Vector2.Distance(transform.position, noisePos) <= range)
      {
         noiseLocation = noisePos;
         ChangeState(State.Investigate); // 총을 겨누고 소리 난 곳으로 수색 이동!
        }
    }

    public void OnAttacked(GameObject attacker)
    {
        // 아군(Shooter) 오사 무시
       if (attacker.CompareTag("Shooter") || attacker.transform.root.CompareTag("Shooter")) return;

       // 타겟을 나를 때린 놈으로 즉시 갱신!
       target = attacker.transform;

      // 평화로운 상태에서 기습을 당했다면?
      if (currentState == State.Idle || currentState == State.Investigate || currentState == State.Return)
      {
           // 거리가 사거리 안이면 바로 맞대응(Combat), 멀면 일단 추격(Chase)
           float dist = Vector2.Distance(transform.position, target.position);
           if (dist <= shootingRange)
           {
               ChangeState(State.Combat);
           }
           else
           {
               ChangeState(State.Chase);
           }
       }
    }

    private void Start()
    {
        if (weaponData != null) currentAmmo = weaponData.maxAmmo;
        if (weaponPivot != null) initialPivotPos = weaponPivot.localPosition;
    }

    private void Update()
    {
        switch (currentState)
        {
            case State.Idle: IdleUpdate(); break;
            case State.Investigate: InvestigateUpdate(); break;
            case State.Chase: ChaseUpdate(); break;
            case State.Combat: CombatUpdate(); break;
            case State.Return: ReturnUpdate(); break;
        }

        HandleAimAndFlip();
    }

    private void ChangeState(State newState)
    {
        currentState = newState;
        switch (newState)
        {
            case State.Idle:
                agent.ResetPath();
                agent.speed = patrolSpeed;
                break;
            case State.Investigate:
                agent.speed = chaseSpeed;
                agent.SetDestination(noiseLocation);
                break;
            case State.Chase:
                agent.speed = chaseSpeed;
                break;
            case State.Combat:
                agent.ResetPath(); // 사격 시 정지
                break;
            case State.Return:
                agent.speed = patrolSpeed;
                agent.SetDestination(startPosition);
                break;
        }
    }

    // [수정됨] CanSeePlayer() 대신 GetVisibleTarget()으로 타겟 갱신
    private void IdleUpdate()
    {
        Transform visibleTarget = perception.GetVisibleTarget();
        if (visibleTarget != null) 
        {
            target = visibleTarget;
            ChangeState(State.Chase);
        }
    }

    private void InvestigateUpdate()
{
    Transform visibleTarget = perception.GetVisibleTarget();
    if (visibleTarget != null) 
    {
        target = visibleTarget;
        ChangeState(State.Combat);
        return;
    }

    // 소리가 난 위치(목표지점)와의 실제 거리 계산
    float distToNoise = Vector2.Distance(transform.position, noiseLocation);
    
    bool hasReached = !agent.pathPending && distToNoise <= agent.stoppingDistance + 0.5f;

    if (hasReached)
    {
        investigateTimer += Time.deltaTime;
        if (investigateTimer >= 2f) 
        {
            investigateTimer = 0f;
            ChangeState(State.Return); 
        }
    }
    else
    {
        investigateTimer = 0f; 
    }
}

    private void ChaseUpdate()
{
    if (target == null || !target.gameObject.activeInHierarchy)
    {
        ChangeState(State.Investigate);
        return;
    }

    Transform visibleTarget = perception.GetVisibleTarget();
    if (visibleTarget != null) 
    {
        target = visibleTarget;
        timeSinceLastSawTarget = 0f; // [추가] 눈에 보이면 타이머를 0으로 초기화!
    }
    else
    {
        timeSinceLastSawTarget += Time.deltaTime; // [추가] 시야에서 사라졌을 때만 타이머 증가!
    }

    if (Vector2.Distance(transform.position, target.position) <= shootingRange && visibleTarget != null)
    {
        ChangeState(State.Combat);
        return;
    }
    
    agent.SetDestination(target.position);

    // 4초 이상 못 찾으면 포기
    if (timeSinceLastSawTarget >= loseTargetTime)
    {
        noiseLocation = target.position; 
        ChangeState(State.Investigate);
        return;
    }
}

    private void CombatUpdate()
{
    if (target == null || !target.gameObject.activeInHierarchy)
    {
        ChangeState(State.Investigate);
        return;
    }

    Transform visibleTarget = perception.GetVisibleTarget();

    if (visibleTarget != null)
    {
        target = visibleTarget;
        timeSinceLastSawTarget = 0f; // [추가] 교전 중에도 눈에 보이면 타이머 계속 초기화
    }

    if (Vector2.Distance(transform.position, target.position) > shootingRange || visibleTarget == null)
    {
        ChangeState(State.Chase);
        return;
    }

    if (isReloading) return;

    if (currentAmmo <= 0)
    {
        StartCoroutine(ReloadRoutine());
        return;
    }

    if (Time.time >= nextFireTime && weaponData != null && weaponData.type == WeaponType.Gun)
    {
        ShootAtPlayer(); 
        nextFireTime = Time.time + weaponData.fireRate;
    }
}

    private void ReturnUpdate()
{
    Transform visibleTarget = perception.GetVisibleTarget();
    if (visibleTarget != null) 
    {
        target = visibleTarget;
        ChangeState(State.Combat);
        return;
    }

    // 실제 내 위치와 시작 위치의 거리를 비교하여 도착 여부 판단
    float distToStart = Vector2.Distance(transform.position, startPosition);
    
    // 에이전트가 아직 경로 계산 중이 아닐 때 거리 체크
    if (!agent.pathPending && distToStart <= agent.stoppingDistance + 0.5f)
    {
        ChangeState(State.Idle); 
    }
}

    private IEnumerator WaitAndReturn()
    {
        // 제자리에서 잠시 대기 (경계)
        yield return new WaitForSeconds(2f);
        ChangeState(State.Return);
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        yield return new WaitForSeconds(weaponData.reloadTime); 
        currentAmmo = weaponData.maxAmmo;
        isReloading = false;
    }

    private Vector3 GetTargetPos()
{
    if (target == null) return transform.position;

    Collider2D coll = target.GetComponentInChildren<Collider2D>();
    
    if (coll != null)
    {
        return coll.bounds.center + (Vector3)aimOffset;
    }

    return target.position + (Vector3)aimOffset;
}

    private void ShootAtPlayer()
    {
        if (weaponData.projectilePrefab == null || muzzlePoint == null) return;

        currentAmmo--; 

        Vector2 aimDir = (GetTargetPos() - muzzlePoint.position).normalized;
        
        float randomSpread = Random.Range(-weaponData.baseSpread, weaponData.baseSpread);
        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg + randomSpread;
        Quaternion fireRotation = Quaternion.Euler(0, 0, angle);

        GameObject bulletObj = Instantiate(weaponData.projectilePrefab, muzzlePoint.position, fireRotation);
        Projectile projectile = bulletObj.GetComponent<Projectile>();
        
        if (projectile != null)
        {
            projectile.Initialize(weaponData.damage, enemyTargetLayers, weaponData.projectileSpeed, weaponData.bulletLifeTime, this.gameObject);
        }

        NoiseManager.MakeNoise(transform.position, weaponData.noiseRange, this.gameObject);
    }

    private void HandleAimAndFlip()
    {
        Vector2 aimDir = lastAimDir; // 기본적으로 방금 전까지 보던 방향 유지!

        // [1순위] 전투/추격 중일 때는 무조건 타겟 방향 계산
        if ((currentState == State.Combat || currentState == State.Chase) && target != null)
        {
            Vector3 aimPos = GetTargetPos();
            Vector3 pivotPos = weaponPivot != null ? weaponPivot.position : transform.position;
            aimDir = (aimPos - pivotPos).normalized;
        }
        // [2순위] 걷고 있을 때는 이동 방향 계산
        else if (agent.velocity.sqrMagnitude > 0.1f)
        {
            aimDir = agent.velocity.normalized;
        }
        // (멈춰있을 때는 아무 계산도 안 하므로 위에서 가져온 lastAimDir이 그대로 유지됨!)

        if (aimDir.sqrMagnitude == 0) return;

        lastAimDir = aimDir; // 계산된 방향을 뇌에 저장

        bool isFacingRight = aimDir.x >= 0;

        // 1. 몸통 뒤집기
        if (spriteRenderer != null) 
        {
            spriteRenderer.flipX = !isFacingRight;
        }

        // 2. 무기 회전 및 뒤집기
        if (weaponPivot != null && weaponData != null)
        {
            float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            weaponPivot.rotation = Quaternion.Euler(0, 0, angle);

            float targetX = isFacingRight ? Mathf.Abs(initialPivotPos.x) : -Mathf.Abs(initialPivotPos.x);
            weaponPivot.localPosition = new Vector3(targetX, initialPivotPos.y, initialPivotPos.z);
            weaponPivot.localScale = isFacingRight ? new Vector3(1, 1, 1) : new Vector3(1, -1, 1);

            if (weaponRenderer != null)
            {
                weaponRenderer.transform.localScale = weaponData.spriteScale;
            }
        }
    }
}