using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FieldOfView_BackUp : MonoBehaviour
{
    [Header("Settings")]
    public float viewRadius = 10f;      // 시야 거리
    [Range(0, 360)]
    public float viewAngle = 360f;      // 시야각 (360도면 주변 전체, 90도면 부채꼴)

    [Header("Target Detection")] // [추가] 적 탐지 설정
    public LayerMask targetMask; // 적(Enemy) 레이어
    public LayerMask obstacleMask; // 벽(Wall) 레이어
    
    // 탐지된 적들을 저장할 리스트 (매 프레임 갱신)
    public List<Transform> visibleTargets = new List<Transform>();

    [Header("Resolution")]
    public float meshResolution = 0.5f; // 레이캐스트 정밀도 (높을수록 부드럽지만 연산량 증가)
    public int edgeResolveIterations = 4; // 모서리 부분을 더 부드럽게 다듬는 횟수
    public float edgeDstThreshold = 0.5f; // 모서리 판정 거리

    [Header("References")]
    public MeshFilter viewMeshFilter;   // 만들어진 메쉬를 보여줄 필터
    private Mesh viewMesh;

    private void Start()
    {
        viewMesh = new Mesh();
        viewMesh.name = "View Mesh";
        viewMeshFilter.mesh = viewMesh;

        StartCoroutine("FindTargetsWithDelay", 0.2f); // 0.2초마다 적 탐지
    }

    private void LateUpdate()
    {
        // 플레이어가 움직인 후 시야를 계산하기 위해 LateUpdate 사용
        DrawFieldOfView();
    }

    // [추가] 주기적으로 적 탐지 코루틴
    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }

    // [추가] 실제 적 탐지 로직 (핵심)
    void FindVisibleTargets()
    {
        visibleTargets.Clear();
        
        // 1. 반경 내의 모든 타겟(좀비)을 일단 수집
        Collider2D[] targetsInViewRadius = Physics2D.OverlapCircleAll(transform.position, viewRadius, targetMask);

        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;

            // 2. 각도 안에 들어왔는가? (부채꼴)
            if (Vector3.Angle(transform.up, dirToTarget) < viewAngle / 2) // transform.up은 2D에서 Y축(위쪽) 기준
            {
                float dstToTarget = Vector3.Distance(transform.position, target.position);

                // 3. 벽에 가려지지 않았는가? (레이캐스트)
                if (!Physics2D.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                {
                    // 시야에 보임!
                    visibleTargets.Add(target);
                    ToggleTargetVisibility(target, true);
                }
                else
                {
                    // 각도는 맞는데 벽 뒤에 있음
                    ToggleTargetVisibility(target, false);
                }
            }
            else
            {
                // 각도 밖임
                ToggleTargetVisibility(target, false);
            }
        }
    }

    // [추가] 좀비의 모습을 켜고 끄는 함수
    void ToggleTargetVisibility(Transform target, bool isVisible)
    {
        // 스프라이트 렌더러를 끄면 게임엔 존재하지만 눈엔 안 보임
        var renderer = target.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.enabled = isVisible;
        }
        
        // (선택 사항) Canvas(체력바) 등도 같이 꺼야 함
    }

    private void DrawFieldOfView()
    {
        int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
        float stepAngleSize = viewAngle / stepCount;
        
        List<Vector3> viewPoints = new List<Vector3>();
        ViewCastInfo oldViewCast = new ViewCastInfo();

        for (int i = 0; i <= stepCount; i++)
        {
            // 현재 각도 계산 (플레이어가 보고 있는 방향 기준)
            float angle = transform.eulerAngles.z - viewAngle / 2 + stepAngleSize * i;
            ViewCastInfo newViewCast = ViewCast(angle);

            // [최적화 & 퀄리티 업] 모서리(Edge) 처리 로직
            if (i > 0)
            {
                bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDstThreshold;
                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded))
                {
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
                    if (edge.pointA != Vector3.zero) viewPoints.Add(edge.pointA);
                    if (edge.pointB != Vector3.zero) viewPoints.Add(edge.pointB);
                }
            }

            viewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }

        // 3. 정점(Vertices)을 이용해 메쉬 생성
        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = Vector3.zero; // 중심점 (플레이어 위치, 로컬 좌표라 0,0)
        
        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }

    // 특정 각도로 레이를 쏘는 함수
    ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, viewRadius, obstacleMask);

        if (hit.collider != null)
        {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }
        else
        {
            return new ViewCastInfo(false, transform.position + dir * viewRadius, viewRadius, globalAngle);
        }
    }

    // 모서리를 찾아내는 정밀 레이캐스트 (벽 끝부분이 뭉개지는 현상 방지)
    EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
    {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < edgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo newViewCast = ViewCast(angle);

            bool edgeDstThresholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > edgeDstThreshold;
            if (newViewCast.hit == minViewCast.hit && !edgeDstThresholdExceeded)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }

        return new EdgeInfo(minPoint, maxPoint);
    }

    // 각도를 벡터로 변환 (Unity의 0도는 오른쪽이 아니라 위쪽 기준일 수 있으므로 보정)
    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.z;
        }
        // Unity 2D 각도계 (오른쪽 0도, 반시계 +) -> sin, cos 변환
        return new Vector3(Mathf.Cos(angleInDegrees * Mathf.Deg2Rad), Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0);
    }

    public struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float dst;
        public float angle;

        public ViewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle)
        {
            hit = _hit;
            point = _point;
            dst = _dst;
            angle = _angle;
        }
    }

    public struct EdgeInfo
    {
        public Vector3 pointA;
        public Vector3 pointB;

        public EdgeInfo(Vector3 _pointA, Vector3 _pointB)
        {
            pointA = _pointA;
            pointB = _pointB;
        }
    }
}