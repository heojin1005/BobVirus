using UnityEngine;
using System.Collections;

public class Grenade : MonoBehaviour
{
    private float damage;
    private float explosionRadius;
    private LayerMask targetLayer;
    private bool explodeOnArrival; // WeaponData에서 받아옴

    // 벽 감지용 (Initialize에서 받거나, 여기서 설정)
    // "Wall" 레이어를 꼭 설정해줘야 벽을 인식합니다.
    [SerializeField] private LayerMask wallLayer; 

    [Header("Visual")]
    [SerializeField] private Transform spriteObject; // 자식(이미지) 연결 필수!
    private float arcHeight; // WeaponData에서 받아옴

    public void Initialize(float damage, float radius, float fuseTime, LayerMask targetLayer, Vector3 targetPos, float height, bool explodeOnContact)
    {
        this.damage = damage;
        this.explosionRadius = radius;
        this.targetLayer = targetLayer;
        this.arcHeight = height;
        this.explodeOnArrival = explodeOnContact;

        // 물리 간섭 끄기
        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.simulated = false; 

        // 이동 시작
        StartCoroutine(MoveRoutine(targetPos));

        // 시한신관 (도착 여부 무관하게 시간 되면 터짐)
        if (!explodeOnArrival)
        {
            Invoke(nameof(Explode), fuseTime);
        }
    }

    private IEnumerator MoveRoutine(Vector3 destination)
    {
        Vector3 startPos = transform.position;
        float totalDistance = Vector3.Distance(startPos, destination);
        
        // 거리에 따른 비행 시간 (최소 0.4초 ~ 최대 1.0초)
        float flightDuration = Mathf.Clamp(totalDistance * 0.15f, 0.4f, 1.0f); 
        float timer = 0f;

        while (timer < flightDuration)
        {
            float deltaTime = Time.deltaTime;
            timer += deltaTime;
            float t = timer / flightDuration;

            // 1. 다음 프레임에 갈 위치 예상
            Vector3 nextPos = Vector3.Lerp(startPos, destination, t);
            Vector3 direction = (nextPos - transform.position).normalized;
            float moveDist = Vector3.Distance(transform.position, nextPos);

            // 2. [핵심] 벽 충돌 감지 (Raycast)
            // 현재 위치에서 다음 위치까지만 레이를 쏴서 벽이 있는지 검사
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, moveDist, wallLayer);
            
            if (hit.collider != null)
            {
                // 벽에 맞았다!
                transform.position = hit.point; // 벽 앞에서 멈춤
                
                // 시각적 처리: 공중에 떠 있던 스프라이트 바닥으로 떨구기
                if (spriteObject != null)
                {
                    StartCoroutine(DropSpriteRoutine());
                }

                // 즉시 폭발 모드라면 여기서 터짐
                if (explodeOnArrival) Explode();
                
                yield break; // 이동 코루틴 종료 (멈춤)
            }

            // 3. 이동 (벽 없으면 계속 이동)
            transform.position = nextPos;

            // 4. 곡사 연출 (자식만 위로)
            if (spriteObject != null)
            {
                // Sin 그래프 (0 -> 1 -> 0)
                float height = Mathf.Sin(t * Mathf.PI) * arcHeight;
                spriteObject.localPosition = new Vector3(0, height, 0);
            }

            yield return null;
        }

        // 목적지 도착 완료
        transform.position = destination;
        if (spriteObject != null) spriteObject.localPosition = Vector3.zero;

        if (explodeOnArrival) Explode();
    }

    // 벽에 부딪혔을 때 스프라이트가 자연스럽게 떨어지는 연출
    private IEnumerator DropSpriteRoutine()
    {
        if (spriteObject == null) yield break;

        Vector3 startLocalPos = spriteObject.localPosition;
        float dropTime = 0.2f; // 0.2초 만에 바닥으로
        float timer = 0f;

        while (timer < dropTime)
        {
            timer += Time.deltaTime;
            float t = timer / dropTime;
            // 현재 높이에서 0(바닥)으로 Lerp
            spriteObject.localPosition = Vector3.Lerp(startLocalPos, Vector3.zero, t);
            yield return null;
        }
        spriteObject.localPosition = Vector3.zero;
    }

    private void Explode()
    {
        // 중복 폭발 방지
        CancelInvoke(nameof(Explode));

        // 폭발 범위 및 데미지 처리
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius, targetLayer);
        foreach (var hit in hitColliders)
        {
            hit.GetComponent<IDamageable>()?.TakeDamage(damage, transform.position, Vector2.zero);
        }

        // 소리 및 카메라 쉐이크
        NoiseManager.MakeNoise(transform.position, 30f);
        if (CameraFollow.Instance) CameraFollow.Instance.Shake(0.3f, 0.5f);
        
        // 여기에 폭발 이펙트 생성 코드 추가 (Instantiate...)

        Destroy(gameObject);
    }
    
    // 폭발 범위 기즈모 (요청하신 6번)
    // 게임 실행 중에는 안 보이고, 터지는 순간에만 잠깐 그리기는 어려우므로
    // "폭발 예정 범위"를 미리 보여줍니다.
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0.5f, 0, 0.4f); // 주황색 반투명
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}