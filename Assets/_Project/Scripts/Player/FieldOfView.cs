using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FieldOfView : MonoBehaviour
{
    [Header("Range Settings")]
    public float nearRadius = 3f;   // 근거리 360도 시야
    public float farRadius = 12f;   // 원거리 부채꼴 시야

    [Range(0, 360)]
    public float farAngle = 90f;    // 원거리 시야각

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

    // 시야에 들어온 렌더러들 기억
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
        // 1. [초기화] 이전 프레임에 보였던 모든 렌더러 끄기
        foreach (var r in lastFrameVisibleRenderers)
        {
            if (r != null) r.enabled = false;
        }
        lastFrameVisibleRenderers.Clear();

        // 2. 반경 내(FarRadius) 타겟 스캔
        Collider2D[] targetsInRadius = Physics2D.OverlapCircleAll(transform.position, farRadius, targetMask);
        for (int i = 0; i < targetsInRadius.Length; i++)
        {
            Transform target = targetsInRadius[i].transform;
            
            // [수정] 단수형(GetComponent)이 아니라 복수형(GetComponents)으로 변경!
            // 몸통(Body)과 무기(Weapon)의 렌더러를 배열로 한 번에 다 가져옵니다.
            SpriteRenderer[] targetRenderers = target.GetComponentsInChildren<SpriteRenderer>();
            
            if (targetRenderers.Length == 0) continue;

            // 3. 시야 안에 있다면?
            if (IsPointInView(target.position))
            {
                // [핵심] 찾아낸 모든 렌더러(몸통, 총)를 전부 다 켜줍니다!
                foreach (SpriteRenderer renderer in targetRenderers)
                {
                    renderer.enabled = true;
                    lastFrameVisibleRenderers.Add(renderer);
                }
            }
        }
    }

    // 대상이 실제 시야(복합 범위 + 장애물) 내에 있는지 판별
    bool IsPointInView(Vector3 targetPos)
    {
        Vector3 dirToTarget = (targetPos - transform.position).normalized;
        float dstToTarget = Vector3.Distance(transform.position, targetPos);

        // 1. 타겟을 향한 절대 각도 (Global Angle)
        float angleToTarget = Mathf.Atan2(dirToTarget.y, dirToTarget.x) * Mathf.Rad2Deg;

        // 2. 해당 각도에서의 최대 시야 거리 계산
        float visibleRadiusAtAngle = GetRadiusForAngle(angleToTarget);

        // 3. 거리 안에 있는지 확인
        if (dstToTarget <= visibleRadiusAtAngle)
        {
            // 4. 장애물에 가려지지 않았는지 확인
            if (!Physics2D.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
            {
                return true;
            }
        }
        return false;
    }

    // 각도에 따른 시야 반경 반환 (Near vs Far)
    float GetRadiusForAngle(float globalAngle)
    {
        float myRotation = transform.eulerAngles.z;
        float angleDiff = Mathf.DeltaAngle(globalAngle, myRotation);

        if (Mathf.Abs(angleDiff) < farAngle / 2)
        {
            return farRadius;
        }
        return nearRadius;
    }

    // --- Draw Mesh Logic (시야 그리기) ---
    void DrawFieldOfView()
    {
        List<Vector3> viewPoints = new List<Vector3>();
        ViewCastInfo oldViewCast = new ViewCastInfo();

        int stepCount = Mathf.RoundToInt(360 * meshResolution);
        float stepAngleSize = 360f / stepCount;

        for (int i = 0; i <= stepCount; i++)
        {
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, nearRadius);

        Vector3 angle01 = DirFromAngle(-farAngle / 2, false);
        Vector3 angle02 = DirFromAngle(farAngle / 2, false);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + angle01 * farRadius);
        Gizmos.DrawLine(transform.position, transform.position + angle02 * farRadius);

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