using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyShooterAI : MonoBehaviour
{
    public enum State { Idle, Investigate, Chase, Combat }
    
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
    // [추가] 조준점 미세 조절 (총구가 살짝 위를 보면 Y값을 -0.2 정도로 내려줍니다)
    [SerializeField] private Vector2 aimOffset = new Vector2(0, -0.2f); 
    
    [Header("Weapon References")]
    [SerializeField] private Transform muzzlePoint; 
    [SerializeField] private Transform weaponPivot; 
    [SerializeField] private SpriteRenderer weaponRenderer; 

    private float nextFireTime; 
    private int currentAmmo; 
    private bool isReloading = false;
    private Vector3 initialPivotPos; // 무기 원래 높이 기억용

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
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) target = playerObj.transform;
    }

    private void Start()
    {
        if (weaponData != null) currentAmmo = weaponData.maxAmmo;
        if (weaponPivot != null) initialPivotPos = weaponPivot.localPosition;
    }

    private void Update()
    {
        if (target == null) return;

        switch (currentState)
        {
            case State.Idle: IdleUpdate(); break;
            case State.Investigate: InvestigateUpdate(); break;
            case State.Chase: ChaseUpdate(); break;
            case State.Combat: CombatUpdate(); break;
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
        }
    }

    private void IdleUpdate()
    {
        if (perception.CanSeePlayer()) ChangeState(State.Chase);
    }

    private void InvestigateUpdate()
    {
        if (perception.CanSeePlayer()) ChangeState(State.Chase);
    }

    private void ChaseUpdate()
    {
        if (Vector2.Distance(transform.position, target.position) <= shootingRange && perception.CanSeePlayer())
        {
            ChangeState(State.Combat);
            return;
        }
        agent.SetDestination(target.position);
    }

    private void CombatUpdate()
    {
        if (Vector2.Distance(transform.position, target.position) > shootingRange || !perception.CanSeePlayer())
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

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        yield return new WaitForSeconds(weaponData.reloadTime); 
        currentAmmo = weaponData.maxAmmo;
        isReloading = false;
    }

    // [핵심 추가] 타겟의 위치에 영점 조절 오프셋을 더해주는 함수입니다!
    private Vector3 GetTargetPos()
    {
        if (target == null) return transform.position;
        return target.position + (Vector3)aimOffset;
    }

    private void ShootAtPlayer()
    {
        if (weaponData.projectilePrefab == null || muzzlePoint == null) return;

        currentAmmo--; 

        // [수정] target.position 대신 GetTargetPos() 사용
        Vector2 aimDir = (GetTargetPos() - muzzlePoint.position).normalized;
        
        float randomSpread = Random.Range(-weaponData.baseSpread, weaponData.baseSpread);
        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg + randomSpread;
        Quaternion fireRotation = Quaternion.Euler(0, 0, angle);

        GameObject bulletObj = Instantiate(weaponData.projectilePrefab, muzzlePoint.position, fireRotation);
        Projectile projectile = bulletObj.GetComponent<Projectile>();
        
        if (projectile != null)
        {
            projectile.Initialize(weaponData.damage, enemyTargetLayers, weaponData.projectileSpeed, weaponData.bulletLifeTime);
        }

        NoiseManager.MakeNoise(transform.position, weaponData.noiseRange);
    }

    private void HandleAimAndFlip()
    {
        Vector3 aimPos;

        // [수정] 1. 전투/추적 중이면 플레이어(오프셋 적용)를 봅니다.
        if ((currentState == State.Combat || currentState == State.Chase) && target != null)
        {
            aimPos = GetTargetPos();
        }
        else
        {
            // [수정] 2. 가만히 있을 때 밑을 쳐다보며 파닥거리는 문제 해결
            if (agent.velocity.sqrMagnitude > 0.1f)
            {
                aimPos = weaponPivot.position + agent.velocity;
            }
            else
            {
                float facingX = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
                aimPos = weaponPivot.position + new Vector3(facingX, 0, 0);
            }
        }

        // [수정] 3. 무기 피벗 기준으로 방향 계산
        Vector2 aimDir = Vector2.zero;
        if (weaponPivot != null)
        {
            aimDir = (aimPos - weaponPivot.position).normalized; 
        }
        else
        {
            aimDir = (aimPos - transform.position).normalized;
        }
        
        if (aimDir.sqrMagnitude == 0) return;

        bool isFacingRight = aimDir.x >= 0;

        if (spriteRenderer != null) 
        {
            spriteRenderer.flipX = !isFacingRight;
        }

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