using UnityEngine;
using System.Collections.Generic;
public class EnemyPerception : MonoBehaviour
{
    [Header("Vision Settings")]
    public float viewRadius = 8f;       // 시야 거리
    [Range(0, 360)]
    public float viewAngle = 110f;      // 시야각 (좀비는 앞만 봄)
    
    public LayerMask targetMask;        // 플레이어 레이어
    public LayerMask obstacleMask;      // 벽 레이어

    // 이 AI가 적대하는 태그 목록 (좀비면 "Player", "Shooter" / 슈터면 "Player", "Zombie")
    public List<string> enemyTags = new List<string> { "Player" };

    public Transform currentTarget { get; private set; } // 발견한 플레이어

    // 시야 내의 적을 찾아 반환하는 함수 (Start에서 미리 찾지 않음)
    public Transform GetVisibleTarget()
{
    Collider2D[] targetsInViewRadius = Physics2D.OverlapCircleAll(transform.position, viewRadius, targetMask);
    
    Transform closestTarget = null;
    float minDistance = float.MaxValue;

    Vector3 facingDir;
    var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    var sr = GetComponentInChildren<SpriteRenderer>();

    if (agent != null && agent.velocity.sqrMagnitude > 0.1f)
        facingDir = agent.velocity.normalized;
    else if (sr != null)
        facingDir = sr.flipX ? Vector3.left : Vector3.right;
    else
        facingDir = transform.localScale.x > 0 ? Vector3.right : Vector3.left;

    foreach (var hit in targetsInViewRadius)
    {
        // [수정 1] root 검사 대신 IsChildOf를 사용하여 '진짜 내 몸뚱어리'만 정확히 무시합니다!
        if (hit.transform == this.transform || hit.transform.IsChildOf(this.transform)) continue;

        // --- [디버깅 1] ---
        // Debug.Log($"[슈터 시야] 1. 레이더에 포착됨: {hit.name}");

        bool isEnemy = false;
        Transform actualEnemyBody = hit.transform; 

        // [수정 2] 히트박스(자식)부터 부모로 올라가며 진짜 적 태그가 있는지 확인
        Transform currentObj = hit.transform;
        while (currentObj != null)
        {
            if (enemyTags.Contains(currentObj.tag))
            {
                isEnemy = true;
                actualEnemyBody = currentObj; // 태그가 달려있는 진짜 본체
                break;
            }
            currentObj = currentObj.parent;
        }

        if (!isEnemy)
        {
            // Debug.Log($"[슈터 시야] 2. 적 태그가 아니라서 무시: {hit.name}");
            continue;
        }

        // 각도와 거리는 진짜 본체(actualEnemyBody)를 기준으로 계산합니다.
        Vector3 dirToTarget = (actualEnemyBody.position - transform.position).normalized;
        float distToTarget = Vector3.Distance(transform.position, actualEnemyBody.position);

        if (Vector3.Angle(facingDir, dirToTarget) >= viewAngle / 2)
        {
            // Debug.Log($"[슈터 시야] 3. 시야각 밖이라서 무시: {hit.name}");
            continue;
        }

        RaycastHit2D hitObstacle = Physics2D.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask);
        if (hitObstacle.collider != null)
        {
            // Debug.Log($"[슈터 시야] 4. 장애물에 가려짐! 가린 물체: {hitObstacle.collider.name}");
            continue;
        }

        // Debug.Log($"[슈터 시야] 5. 완벽하게 적 발견!!: {actualEnemyBody.name}");
        if (distToTarget < minDistance)
        {
            minDistance = distToTarget;
            closestTarget = actualEnemyBody;
        }
    }
    
    currentTarget = closestTarget;
    return closestTarget;
}

    /*
    private void Start()
    {
        // 게임 시작 시 플레이어를 미리 찾아둠 (성능 최적화)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTarget = player.transform;
    }*/

    // 플레이어가 시야 내에 있는지 검사하는 함수
    /*
    public bool CanSeePlayer()
    {
        if (playerTarget == null) return false;

        // 1. 거리 체크
        float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        if (distToPlayer > viewRadius) return false;

        // 2. 각도 체크
        Vector3 dirToPlayer = (playerTarget.position - transform.position).normalized;
        // transform.up이 정면이라고 가정 (스프라이트 회전 기준)
        // 만약 RotateToMouse 안 쓰고 Flip만 쓴다면, Looking Direction을 따로 계산해야 함.
        // 여기서는 NavMeshAgent의 이동 방향이나, 바라보는 방향을 기준으로 함.
        
        // 시야의 '정면'을 결정하는 로직
        Vector3 facingDir;

        // 1. 움직이는 중이면 -> 이동 방향이 정면
        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.velocity.sqrMagnitude > 0.1f)
        {
            facingDir = agent.velocity.normalized;
        }
        // 2. 멈춰 있으면 -> 스프라이트가 보는 방향(좌/우)이 정면
        else
        {
            facingDir = transform.localScale.x > 0 ? Vector3.right : Vector3.left;
        }

        // 각도 계산
        if (Vector3.Angle(facingDir, dirToPlayer) < viewAngle / 2)
        {
            if (!Physics2D.Raycast(transform.position, dirToPlayer, distToPlayer, obstacleMask))
            {
                return true;
            }
        }
        return false;

    }
    */

    // 에디터에서 시야 범위 확인용
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        // 게임 중이면 이동 방향, 아니면 스프라이트 방향
        Vector3 facingDir;
        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        // 게임 실행 중이고 + NavMeshAgent가 있고 + 실제로 움직이고 있다면?
        if (Application.isPlaying && agent != null && agent.velocity.sqrMagnitude > 0.1f)
        {
            facingDir = agent.velocity.normalized;
        }
        else
        {
            // 멈춰있거나 에디터 상태면 스프라이트 Flip 기준
            facingDir = transform.localScale.x > 0 ? Vector3.right : Vector3.left;
        }
        
        // 부채꼴 라인 그리기
        Vector3 leftBoundary = Quaternion.Euler(0, 0, viewAngle / 2) * facingDir;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -viewAngle / 2) * facingDir;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * viewRadius);
    }
}