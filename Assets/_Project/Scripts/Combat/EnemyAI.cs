using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    // FSM 상태 정의
    public enum State { Idle, Investigate, Chase, Attack }
    
    [Header("State Info")]
    [SerializeField] private State currentState; // 현재 상태 (인스펙터 확인용)

    [Header("Movement")]
    private NavMeshAgent agent;
    private Vector3 startPos;       // 원래 있던 자리 (복귀용, 혹은 배회 기준점)
    private float wanderRadius = 3f; // 배회 반경

    [Header("Wander Settings")]
    [SerializeField] private float minWanderWaitTime = 2f; // 배회 시 멈춰있는 최소 시간
    [SerializeField] private float maxWanderWaitTime = 5f; // 배회 시 멈춰있는 최대 시간
    private float currentWaitTime = 0f; // 도착 후 대기 시간
    private float WaitTimer = 0f; // 배회 시 멈춰있는 시간

    [Header("Chase Settings")] // [추가] 추적 관련 설정
    [SerializeField] private float patrolSpeed = 2f;    // 평소 걷는 속도
    [SerializeField] private float chaseSpeed = 4.5f;   // 발견 시 뛰는 속도
    [SerializeField] private float memoryDuration = 2f; // 시야에서 사라져도 쫓는 시간 (기억력)
    private float timeSinceLastSawPlayer = 0f; // 마지막으로 본 지 얼마나 지났나

    [Header("Combat")]
    [SerializeField] private float attackRangeX = 0.8f;
    [SerializeField] private float attackRangeY = 1.2f;
    [SerializeField] private Vector2 centerOffset = new Vector2(0, 0.5f);
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRate = 1.5f;
    [SerializeField] private float attackWindup = 0.5f;

    [Header("Components")]
    [SerializeField] private EnemyPerception perception; // 시각 모듈
    [SerializeField] private SpriteRenderer spriteRenderer;

    // 내부 변수
    private Transform target;
    private Vector3 noiseLocation;
    private float nextAttackTime;
    private bool isAttacking;
    private Color originalColor;
    private Color attackColor = Color.green;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        
        if (perception == null) perception = GetComponent<EnemyPerception>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer) 
        {
            originalColor = spriteRenderer.color;
            //spriteRenderer.enabled = false; // 시작 시 은신 -> 테스트할땐 주석처리
        }

        startPos = transform.position;
        // 태그로 찾은 플레이어는 '추적 대상'이 아니라 '참조용'임
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) target = playerObj.transform;
    }

    private void OnEnable()
    {
        // 소리 이벤트 구독 (귀 열기)
        NoiseManager.OnNoiseGenerated += OnHeardNoise;
        
        // 초기 상태 설정
        ChangeState(State.Idle);
    }

    private void OnDisable()
    {
        // 소리 이벤트 구독 해제 (귀 닫기)
        NoiseManager.OnNoiseGenerated -= OnHeardNoise;
    }

    private void Update()
    {
        if (target == null) return;

        // 상태 머신 실행
        switch (currentState)
        {
            case State.Idle:
                IdleUpdate();
                break;
            case State.Investigate:
                InvestigateUpdate();
                break;
            case State.Chase:
                ChaseUpdate();
                break;
            case State.Attack:
                AttackUpdate();
                break;
        }

        // 시각적 회전 (Flip) 처리
        HandleSpriteFlip();
    }

    // --- State Logic ---

    private void ChangeState(State newState)
    {
        currentState = newState;
        
        // 상태 진입 시 초기화 로직
        switch (newState)
        {
            case State.Idle:
                agent.ResetPath();
                agent.speed = patrolSpeed; // 느리게 걷기
                startPos = transform.position; // 배회 기준점 갱신 (도착한 곳에서 다시 배회) -> 원래 위치로 고정하고 싶으면 이 줄 제거
                currentWaitTime = Random.Range(minWanderWaitTime, maxWanderWaitTime);
                WaitTimer = 0f;
                break;
            case State.Investigate:
                agent.isStopped = false; // 소리 난 위치로 이동 명령
                agent.speed = chaseSpeed; // 빠르게 뛰기
                agent.SetDestination(noiseLocation);
                WaitTimer = 0f;
                break;
            case State.Chase:
                agent.isStopped = false;
                agent.speed = chaseSpeed; // 빠르게 뛰기
                timeSinceLastSawPlayer = 0f; // 기억력 타이머 리셋
                break;
            case State.Attack:
                agent.ResetPath(); // 공격할 땐 멈춤
                break;
        }
    }

    // 1. 대기 상태: 주변을 어슬렁거리거나 가만히 있음
    private void IdleUpdate()
    {
        // 시각 체크: 플레이어가 눈에 보이면 추적 시작
        if (perception.CanSeePlayer())
        {
            ChangeState(State.Chase);
            return;
        }

        // 목적지 도착 체크
        if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)
        {
            // 타이머 체크
            if (WaitTimer < currentWaitTime)
            {
                WaitTimer += Time.deltaTime;
            }
            else
            {
                // 이동 시작!
                Vector3 randomPoint = GetRandomPoint(startPos, wanderRadius);
                agent.SetDestination(randomPoint);
                
                // [핵심] 다음 대기 시간 랜덤 설정 및 타이머 리셋
                currentWaitTime = Random.Range(minWanderWaitTime, maxWanderWaitTime);
                WaitTimer = 0f;
            }
        }
    }

    // 1-1 랜덤 위치 구하는 함수 (NavMesh 위에서 유효한 점 찾기)
    private Vector3 GetRandomPoint(Vector3 center, float range)
    {
        for (int i = 0; i < 30; i++)
    {
        // 1. 랜덤 좌표 생성
        Vector2 randomPos2D = UnityEngine.Random.insideUnitCircle * range;
        Vector3 randomPos = center + new Vector3(randomPos2D.x, randomPos2D.y, 0);

        // 2. NavMesh 위 유효한 좌표인지 확인
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPos, out hit, 1.0f, NavMesh.AllAreas))
        {
            // [핵심] 3. NavMesh.Raycast 사용
            // "내 위치(center)에서 목표점(hit.position)까지 직선으로 갈 수 있는가?"
            // 중간에 벽(NavMesh가 끊긴 곳)이 있으면 true를 반환하여 hit.mask에 걸림
            
            NavMeshHit rayHit;
            // Raycast가 false를 반환해야 장애물이 없다는 뜻입니다.
            if (!NavMesh.Raycast(center, hit.position, out rayHit, NavMesh.AllAreas))
            {
                return hit.position; // 장애물 없이 직진 가능! 채택
            }
        }
    }
    return center; // 실패 시 제자리
    }
    
    // 수색 상태: 소리 난 곳으로 가봄
    private void InvestigateUpdate()
    {
        // 1. 이동 중에라도 플레이어를 눈으로 보면 -> 즉시 추적
        if (perception.CanSeePlayer())
        {
            ChangeState(State.Chase);
            return;
        }

        // 2. 소리 난 위치에 도착했는가?
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            // [추가] 바로 집에 안 가고, 좀 기다리면서 두리번거림
            WaitTimer += Time.deltaTime;
            
            // 도착해서 3초 동안은 대기 (두리번거리는 연출 가능)
            if (WaitTimer > 3f) 
            {
                ChangeState(State.Idle); // 3초 뒤에 포기하고 배회
            }
        }
    }

    // 2. 추적 상태: 플레이어를 향해 뛰어감
    private void ChaseUpdate()
    {
        // 공격 사거리 안에 들어왔나?
        if (IsTargetInAttackRange())
        {
            ChangeState(State.Attack);
            return;
        }

        // 시야에서 놓쳤는가?
        if (perception.CanSeePlayer())
        {
            // 보이면 기억력 리셋하고 계속 쫓음
            timeSinceLastSawPlayer = 0f;
            agent.SetDestination(target.position);
        }
        else
        {
            // 안 보이면? 바로 포기하지 않고 기억력 테스트
            timeSinceLastSawPlayer += Time.deltaTime;

            if (timeSinceLastSawPlayer > memoryDuration)
            {
                // 기억력(2초)이 다 되면 그제서야 "놓쳤다" 판단하고 수색 모드
                noiseLocation = target.position; // 마지막 본 위치 기억
                ChangeState(State.Investigate);
            }
            else
            {
                // 아직 기억 속에 남아있음 -> 마지막으로 본 위치(or 예상 경로)로 계속 이동
                agent.SetDestination(target.position);
            }
        }

        // 추적
        agent.SetDestination(target.position);
    }

    // 3. 공격 상태: 때림
    private void AttackUpdate()
    {
        // 사거리 밖으로 나가면 다시 추적
        if (!IsTargetInAttackRange())
        {
            // 공격 중이 아닐 때만 전환 (공격 모션 캔슬 방지)
            if (!isAttacking) ChangeState(State.Chase);
            return;
        }

        // 쿨타임 체크 후 공격
        if (Time.time >= nextAttackTime && !isAttacking)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    // --- Events & Helpers ---

    // 소리를 들었을 때 호출됨 (이벤트)
    private void OnHeardNoise(Vector3 noisePos, float range)
    {
        // 이미 쫓고 있거나 공격 중이면 무시
        if (currentState == State.Chase || currentState == State.Attack) return;

        // 소리가 내 귀에 들리는 거리인가?
        float dist = Vector3.Distance(transform.position, noisePos);
        if (dist <= range)
        {
            // 소리난 쪽으로 이동하게 하거나, 바로 추적 상태로 전환
            // 여기서는 심플하게 바로 플레이어 추적 모드로 전환 (소리 = 플레이어 위치라 가정)
            noiseLocation = noisePos;
            //Debug.Log("소리 들음! 추적 시작"); 
            ChangeState(State.Investigate);
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        if (spriteRenderer) spriteRenderer.color = attackColor;

        yield return new WaitForSeconds(attackWindup);

        // 공격 판정 시점
        if (IsTargetInAttackRange())
        {
            var damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage, transform.position, Vector2.zero);
            }
        }

        if (spriteRenderer) spriteRenderer.color = originalColor;
        nextAttackTime = Time.time + attackRate;
        isAttacking = false;
    }

    private bool IsTargetInAttackRange()
    {
        if (target == null) return false;
        Vector2 myCenter = (Vector2)transform.position + centerOffset;
        Vector2 targetCenter = (Vector2)target.position; // 타겟은 발바닥 기준 or 오프셋 통일

        float dx = Mathf.Abs(targetCenter.x - myCenter.x);
        float dy = Mathf.Abs(targetCenter.y - myCenter.y);

        return (dx <= attackRangeX && dy <= attackRangeY);
    }

    private void HandleSpriteFlip()
    {
        // NavMeshAgent가 이동 중이면 이동 방향, 아니면 타겟 방향
        Vector3 targetDir = Vector3.zero;
        
        // 1. 움직이는 중이면 -> 이동 방향을 봄
        if (agent.velocity.sqrMagnitude > 0.1f) 
        {
            targetDir = agent.velocity;
        }
        // 2. 멈춰있는데 추적(Chase)이나 공격(Attack) 상태면 -> 플레이어를 봄
        else if ((currentState == State.Chase || currentState == State.Attack) && target != null)
        {
            targetDir = target.position - transform.position;
        }
        // (Idle 상태일 때는 targetDir가 0이므로 아무것도 안 함 -> 마지막 보던 방향 유지)

        // 방향 전환 적용
        if (targetDir.x != 0)
        {
            Vector3 scale = transform.localScale;
            // Sign: 양수면 1, 음수면 -1 반환
            // 절대값(Abs)을 써서 꼬임 방지
            scale.x = Mathf.Abs(scale.x) * (targetDir.x < 0 ? -1 : 1);
            transform.localScale = scale;
        }
    }
    
    // 공격 범위 기즈모
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 centerPos = transform.position + (Vector3)centerOffset;
        Gizmos.DrawWireCube(centerPos, new Vector3(attackRangeX * 2, attackRangeY * 2, 0));
    }
}