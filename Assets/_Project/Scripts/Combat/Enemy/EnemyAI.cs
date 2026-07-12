using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    // FSM 상태 정의
    public enum State { Idle, Investigate, Chase, Attack, Panic }
    
    [Header("State Info")]
    [SerializeField] private State currentState; // 현재 상태 (인스펙터 확인용)

    [Header("Movement")]
    private NavMeshAgent agent;
    private Vector3 startPos;       // 원래 있던 자리 (복귀용, 혹은 배회 기준점)
    private float wanderRadius = 3f; // 배회 반경
    private float footstepTimer = 0f; // 발소리 타이머

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
        // 태그로 찾은 플레이어는 '추적 대상'이 아니라 '참조용'임 -> 팩션 체계 도입 삭제
        //GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        //if (playerObj != null) target = playerObj.transform;
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
            case State.Panic:
                // 패닉(발악) 중에도 시야 레이더는 계속 돌아갑니다!
                Transform visibleTargetInPanic = perception.GetVisibleTarget();
                if (visibleTargetInPanic != null)
                {
                    target = visibleTargetInPanic; // 놈을 찾았다!
                    StopAllCoroutines();           // 뛰고 있던 PanicRoutine을 즉시 멈춤
                    ChangeState(State.Chase);      // 즉각 추격 개시
                }
                break;
        }

        // 시각적 회전 (Flip) 처리
        HandleSpriteFlip();

        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= 0.5f) // 발소리 간격 (0.5초마다)
            {
                NoiseManager.MakeNoise(transform.position, 5f, this.gameObject); // 걷는 소리 알림
                footstepTimer = 0f;
            }
        }
        
    }

    // --- State Logic ---

    private void ChangeState(State newState)
    {
        Debug.Log($"<color=orange>[상태 변경]</color> {currentState} -> {newState}");

        State previousState = currentState;
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
                if (previousState != State.Chase)
                {
                    NoiseManager.MakeNoise(transform.position, 10f, this.gameObject); // 추적 시 적에게 알림
                }
                break;
            case State.Chase:
                agent.isStopped = false;
                agent.speed = chaseSpeed; // 빠르게 뛰기
                timeSinceLastSawPlayer = 0f; // 기억력 타이머 리셋

                // [개선점 2] 방금 전까지 평화로웠는데(Idle/Investigate), 방금 플레이어를 발견했다면?
                if (previousState != State.Chase)
                {
                    // 괴성을 질러 반경 10f 내의 다른 좀비들을 수색(Investigate) 모드로 깨움!
                    // (주의: NoiseManager의 실제 이벤트 발생 함수명에 맞춰 수정해주세요. ex: GenerateNoise)
                    NoiseManager.MakeNoise(transform.position, 10f, this.gameObject);
                }
                break;
            case State.Attack:
                agent.ResetPath(); // 공격할 땐 멈춤
                break;
            case State.Panic:
                 Debug.Log("<color=red>==== [패닉 진입 확인] 패닉 상태가 시작되었습니다! ====</color>");    
                // 0.1초마다 상태 추적하는 코루틴 실행
                StartCoroutine(PanicStateTracker());
            
                StartCoroutine(PanicRoutine());
                break;
        }
    }

    // 1. 대기 상태: 주변을 어슬렁거리거나 가만히 있음
    private void IdleUpdate()
    {
        // 시각 체크: 플레이어가 눈에 보이면 추적 시작
        Transform visibleTarget = perception.GetVisibleTarget();
        if (visibleTarget != null)
        {
            target = visibleTarget; // 타겟 갱신
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
        Transform visibleTarget = perception.GetVisibleTarget();
        if (visibleTarget != null)
        {
            target = visibleTarget;
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
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            ChangeState(State.Investigate);
            return;
        }

        if (IsTargetInAttackRange())
        {
            ChangeState(State.Attack);
            return;
        }

        // 시야에서 놓쳤는가?
        Transform visibleTarget = perception.GetVisibleTarget();
        if (visibleTarget != null)
        {
            // 보이면 기억력 리셋하고 계속 쫓음
            target = visibleTarget; // 타겟 갱신
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
    // [핵심 추가] 팔을 휘두르는 중이 아닐 때는, 눈앞의 시체를 버리고 더 가깝고 '살아있는' 적을 찾습니다.
    if (!isAttacking)
    {
        Transform visibleTarget = perception.GetVisibleTarget();
        if (visibleTarget != null)
        {
            target = visibleTarget; // 새 타겟 갱신!
        }
        
        // 타겟이 죽었거나(비활성화) 사라졌다면 미련 없이 수색 모드로 돌아감
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            ChangeState(State.Investigate);
            return;
        }
    }

    // 사거리 밖으로 나가면 다시 쫓아갑니다.
    if (!IsTargetInAttackRange())
    {
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
    private void OnHeardNoise(Vector3 noisePos, float range, GameObject source)
    {
        // 이미 쫓고 있거나 공격 중이면 무시
        if (currentState == State.Chase || currentState == State.Attack) return;

        if (source != null)
        {
            if (source.CompareTag("Zombie") || source.transform.root.CompareTag("Zombie"))
            {
                return; // 소스가 좀비이면 무시
            }
        }

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

    public void OnAttacked(GameObject attacker)
    {
        if (attacker.CompareTag("Zombie") || attacker.transform.root.CompareTag("Zombie")) return;

        // 공격자와의 거리 계산
        float dist = Vector2.Distance(transform.position, attacker.transform.position);

        // 공격자가 시야에 안 보였다면 패닉 상태로!
        if (perception.GetVisibleTarget() == null)
        {
            // 이미 쫓고 있거나 공격 중이 아닐 때만 패닉
            if (currentState != State.Chase && currentState != State.Attack)
            {
                ChangeState(State.Panic);
            }
        }
        else
        {
            // 가까우면 맞은 즉시 타겟으로 삼고 뜀
            target = attacker.transform;
            ChangeState(State.Chase);
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        if (spriteRenderer) spriteRenderer.color = attackColor;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        yield return new WaitForSeconds(attackWindup);

        // 공격 판정 시점
        if (IsTargetInAttackRange())
        {
         // [수정 핵심] 타겟 본인뿐만 아니라 부모 오브젝트의 IDamageable도 찾습니다!
            var damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage, transform.position, Vector2.zero, this.gameObject);
                //Debug.Log($"[좀비] {target.name}에게 {attackDamage} 데미지 적중!"); // 성공 로그
            }
            else
            {
                //Debug.LogWarning($"[버그] {target.name}이(가) 사거리에 있지만 IDamageable 스크립트가 없습니다!"); // 실패 로그
            }
        }

        if (spriteRenderer) spriteRenderer.color = originalColor;
        nextAttackTime = Time.time + attackRate;
        isAttacking = false;

        if (currentState == State.Attack) agent.isStopped = false;
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

    private IEnumerator PanicRoutine()
    {
        agent.speed = chaseSpeed;
    
        // 랜덤하게 1번 ~ 3번 발악합니다.
        int panicCount = UnityEngine.Random.Range(1, 4); 

        for (int i = 0; i < panicCount; i++)
        {
            // 랜덤 방향으로 1f ~ 3f 사이의 짧은 거리 계산
            Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
            float randomDist = UnityEngine.Random.Range(1f, 3f);
            Vector3 targetPos = transform.position + (Vector3)(randomDir * randomDist);

            // [안전장치] 벽 안으로 들어가지 않게 NavMesh 위인지 검사!
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(targetPos, out hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
        }

            // 아주 짧은 시간(0.2초 ~ 0.5초) 동안 무작정 뛰고 다음 방향으로 틉니다.
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.2f, 0.5f));
        }

        // 발악이 끝나면 멍청하게 다시 배회(Idle) 모드로 돌아갑니다.
        ChangeState(State.Idle);
    }

    private void HandleSpriteFlip()
    {
        Vector3 targetDir = Vector3.zero;

        // 1. 움직이는 중이면 -> 이동 방향을 봄
        if (agent.velocity.sqrMagnitude > 0.1f) 
        {
            targetDir = agent.velocity;
        }
        // 2. 멈춰있고 + 추적/공격 상태이고 + 타겟이 존재할 때만 -> 타겟을 봄
        else if ((currentState == State.Chase || currentState == State.Attack) && target != null)
        {
            targetDir = target.position - transform.position;
        }

        // 방향 전환 적용 (스프라이트가 아닌 Transform 스케일 뒤집기)
        if (targetDir.x != 0)
        {
            Vector3 scale = transform.localScale;
            // 절대값을 사용하여 꼬임 방지 (왼쪽이면 -1, 오른쪽이면 1 곱하기)
            scale.x = Mathf.Abs(scale.x) * (targetDir.x < 0 ? -1 : 1);
            transform.localScale = scale;
        }
    }

    private IEnumerator PanicStateTracker()
{
    float timer = 0f;
    
    // 2초 동안 0.1초 간격으로 계속 로그 찍기
    while (timer <= 2.0f)
    {
        Debug.Log($"[패닉 추적] {timer:F1}초 경과 | 현재 상태: {currentState} | 이동 속도: {agent.velocity.magnitude}");
        yield return new WaitForSeconds(0.1f);
        timer += 0.1f;
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