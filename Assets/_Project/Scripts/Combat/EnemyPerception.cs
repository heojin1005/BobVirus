using UnityEngine;

public class EnemyPerception : MonoBehaviour
{
    [Header("Vision Settings")]
    public float viewRadius = 8f;       // 시야 거리
    [Range(0, 360)]
    public float viewAngle = 110f;      // 시야각 (좀비는 앞만 봄)
    
    public LayerMask targetMask;        // 플레이어 레이어
    public LayerMask obstacleMask;      // 벽 레이어

    public Transform playerTarget { get; private set; } // 발견한 플레이어

    private void Start()
    {
        // 게임 시작 시 플레이어를 미리 찾아둠 (성능 최적화)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTarget = player.transform;
    }

    // 플레이어가 시야 내에 있는지 검사하는 함수
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