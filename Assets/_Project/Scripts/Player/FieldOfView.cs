using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FieldOfView : MonoBehaviour
{
    [Header("Range Settings")]
    public float nearRadius = 3f;   // 근처 360도 시야
    public float farRadius = 12f;   // 전방 부채꼴 시야
    [Range(0, 360)]
    public float farAngle = 90f;    // 전방 부채꼴 각도

    [Header("Target Detection")]
    public LayerMask targetMask;
    public LayerMask obstacleMask;

    [Header("Resolution")]
    public float meshResolution = 1f;
    public int edgeResolveIterations = 4;
    public float edgeDstThreshold = 0.5f;

    [Header("References")]
    public MeshFilter viewMeshFilter;
    private Mesh viewMesh;

    // 저번 프레임에 보였던 좀비들 (끄기 위해 저장)
    private List<SpriteRenderer> lastFrameVisibleRenderers = new List<SpriteRenderer>();

    private void Start()
    {
        viewMesh = new Mesh();
        viewMesh.name = "View Mesh";
        viewMeshFilter.mesh = viewMesh;

        StartCoroutine(FindTargetsWithDelay(0.05f));
    }

    private void LateUpdate()
    {
        DrawFieldOfView();
    }

    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }

    void FindVisibleTargets()
    {
        // 1. 저번에 보였던 애들 싹 끄기
        foreach (var r in lastFrameVisibleRenderers)
        {
            if (r != null) r.enabled = false;
        }
        lastFrameVisibleRenderers.Clear();

        Collider2D[] targetsInRadius = Physics2D.OverlapCircleAll(transform.position, farRadius, targetMask);

        foreach (var col in targetsInRadius)
        {
            Transform target = col.transform;
            SpriteRenderer sr = target.GetComponentInChildren<SpriteRenderer>();
            if (sr == null) continue;

            // 2. [물리 검사] 벽에 가려졌는지는 오직 '발밑' 기준으로 딱 1번만 검사!
            Vector3 feetPos = target.position;
            Vector3 dirToFeet = (feetPos - transform.position).normalized;
            float dstToFeet = Vector3.Distance(transform.position, feetPos);

            if (Physics2D.Raycast(transform.position, dirToFeet, dstToFeet, obstacleMask))
            {
                continue; // 벽에 가려지면 무조건 안 보임 (통과)
            }

            // 3. [시야 검사] 스프라이트의 진짜 '발, 가슴, 머리' 3개의 점 좌표 추출
            Vector3 topPos = new Vector3(sr.bounds.center.x, sr.bounds.max.y, 0); // 머리
            Vector3 centerPos = sr.bounds.center; // 가슴

            // 4. 셋 중 하나라도 부채꼴 안에 걸치면 켜기! (상하좌우 완벽한 대칭 체감)
            if (IsPointInCone(feetPos) || IsPointInCone(centerPos) || IsPointInCone(topPos))
            {
                sr.enabled = true;
                lastFrameVisibleRenderers.Add(sr);
            }
        }
    }

    // 부채꼴 범위 안에 점이 있는지 순수하게 수학적으로만 검사하는 함수
    bool IsPointInCone(Vector3 targetPos)
    {
        float dstToTarget = Vector3.Distance(transform.position, targetPos);
        if (dstToTarget > farRadius) return false;
        if (dstToTarget <= nearRadius) return true; // 근접 360도 반경

        Vector3 dirToTarget = (targetPos - transform.position).normalized;
        float angleToTarget = Mathf.Atan2(dirToTarget.y, dirToTarget.x) * Mathf.Rad2Deg;
        float myRotation = transform.eulerAngles.z;
        
        float angleDiff = Mathf.DeltaAngle(angleToTarget, myRotation);

        return Mathf.Abs(angleDiff) < farAngle / 2;
    }

    // 각도에 따른 사거리 반환 (메쉬와 로직의 공통 기준)
    float GetRadiusForAngle(float globalAngle)
    {
        // 내 현재 회전 각도(Z)
        float myRotation = transform.eulerAngles.z;
        
        // 내 정면(회전각)과 타겟 각도의 차이 계산 (-180 ~ 180)
        // Mathf.DeltaAngle을 써야 359도와 1도의 차이를 2도로 정확히 계산함
        float angleDiff = Mathf.DeltaAngle(globalAngle, myRotation);

        // 이전 코드 수정: RotateToMouse는 Y축(Up)이 아니라 X축(Right) 기준일 수 있음.
        // 하지만 여기선 DeltaAngle로 차이만 보므로,
        // 각도 차이의 절댓값이 (시야각/2)보다 작으면 부채꼴 안임.
        
        // 참고: FOV_Far가 90도라면, 좌우 45도씩 허용
        // 주의: 사용자의 RotateToMouse가 -90도 보정을 썼다면 여기서 기준이 달라질 수 있음.
        // 현재 로직: 오브젝트의 Z회전 방향 = 부채꼴의 중심 방향
        
        if (Mathf.Abs(angleDiff) < farAngle / 2)
        {
            return farRadius;
        }
        return nearRadius;
    }

    // --- Draw Mesh Logic (기존 유지) ---

    void DrawFieldOfView()
    {
        List<Vector3> viewPoints = new List<Vector3>();
        ViewCastInfo oldViewCast = new ViewCastInfo();

        // 360도 대신 해상도에 맞춰 스텝 계산
        int stepCount = Mathf.RoundToInt(360 * meshResolution);
        float stepAngleSize = 360f / stepCount;

        for (int i = 0; i <= stepCount; i++)
        {
            // 현재 오브젝트의 회전을 기준으로 360도를 돔
            float angle = transform.eulerAngles.z - 180 + stepAngleSize * i;
            
            float currentRadius = GetRadiusForAngle(angle);
            ViewCastInfo newViewCast = ViewCast(angle, currentRadius);

            if (i > 0)
            {
                bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDstThreshold;
                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded))
                {
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast, currentRadius);
                    if (edge.pointA != Vector3.zero) viewPoints.Add(edge.pointA);
                    if (edge.pointB != Vector3.zero) viewPoints.Add(edge.pointB);
                }
            }

            viewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }

        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = Vector3.zero;
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

    // --- Helper Functions ---

    ViewCastInfo ViewCast(float globalAngle, float radius)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, radius, obstacleMask);

        if (hit.collider != null) return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        else return new ViewCastInfo(false, transform.position + dir * radius, radius, globalAngle);
    }

    EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast, float radius)
    {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < edgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo newViewCast = ViewCast(angle, radius);

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

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal) angleInDegrees += transform.eulerAngles.z;
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
            hit = _hit; point = _point; dst = _dst; angle = _angle;
        }
    }

    public struct EdgeInfo
    {
        public Vector3 pointA;
        public Vector3 pointB;
        public EdgeInfo(Vector3 _pointA, Vector3 _pointB)
        {
            pointA = _pointA; pointB = _pointB;
        }
    }

    // [추가] 디버그용: 씬 뷰에서 실제 감지 범위를 선으로 보여줍니다.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        // Near 범위 그리기
        Gizmos.DrawWireSphere(transform.position, nearRadius);

        // Far 범위(부채꼴) 그리기
        Vector3 angle01 = DirFromAngle(-farAngle / 2, false);
        Vector3 angle02 = DirFromAngle(farAngle / 2, false);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + angle01 * farRadius);
        Gizmos.DrawLine(transform.position, transform.position + angle02 * farRadius);
        
        // 부채꼴 호 그리기 (대략적으로)
        Vector3 previousPos = transform.position + angle01 * farRadius;
        for(int i=1; i<=10; i++)
        {
            float step = farAngle / 10;
            Vector3 nextDir = DirFromAngle((-farAngle / 2) + (step * i), false);
            Vector3 nextPos = transform.position + nextDir * farRadius;
            Gizmos.DrawLine(previousPos, nextPos);
            previousPos = nextPos;
        }
    }
}